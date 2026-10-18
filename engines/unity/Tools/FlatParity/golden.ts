/**
 * Golden-value generator for the Flat notes C# port.
 *
 *   cd <cytoid-design>/packages/notes
 *   npx tsx <this-repo>/Tools/FlatParity/golden.ts

 * CYTOID_DESIGN_DIR overrides where the cytoid-design checkout lives
 * (default: a sibling of this repository). golden.json is a generated
 * artifact and is gitignored — regenerate it before running FlatParity.
 *
 * Writes golden.json next to this file. Every entry carries its inputs plus
 * the expected output, so the C# checker in Tools/FlatParity/Program.cs can
 * replay the exact same inputs against the ported math.
 */
import { existsSync, writeFileSync } from 'node:fs'
import { dirname, join } from 'node:path'
import { pathToFileURL, fileURLToPath } from 'node:url'

// Root of the cytoid-design checkout (the TypeScript sources of truth).
// Defaults to a sibling of this repository; override with CYTOID_DESIGN_DIR.
const designRoot = process.env.CYTOID_DESIGN_DIR ?? join(import.meta.dirname, '..', '..', '..', 'cytoid-design')
if (!existsSync(designRoot)) {
  console.error(`cytoid-design checkout not found at ${designRoot}`)
  console.error('Clone it as a sibling of this repository, or point CYTOID_DESIGN_DIR at an existing checkout.')
  process.exit(1)
}
const designSrc = (spec: string) => pathToFileURL(join(designRoot, 'packages', 'notes', 'src', spec)).href

// Annotation-only structural mirrors of the design repo's exported types
// (tsx strips types without checking them).
type Oklch = { L: number, C: number, H: number }
type NotePalette = Record<string | number, string>
type DrawContext = Record<string, any>
type SceneNode = Record<string, any>

const { hexToOklch, mix, oklchToRgb, parseHex, toHex } = await import(designSrc('core/color.ts'))
const {
  bump,
  clamp,
  clamp01,
  cubicBezier,
  inCubic,
  inExpo,
  inOutCubic,
  inOutQuad,
  inOutSine,
  inQuad,
  inSine,
  lerp,
  linear,
  outBack,
  outCubic,
  outExpo,
  outQuart,
  outQuad,
  outSine,
  ranged,
  riseSettleCurve,
  rng,
  seg,
} = await import(designSrc('core/ease.ts'))
const { createPalette, derivePalette, tone, ringTone } = await import(designSrc('palette.ts'))
const { hold, holdSize, holdWake, HOLD_WAKE } = await import(designSrc('notes/hold.ts'))
const { bodyReveal } = await import(designSrc('notes/bodies.ts'))
const { FAMILIES, tokens, GRADES } = await import(designSrc('tokens.ts'))
const { exitAlpha, exitRewind } = await import(designSrc('notes/effects.ts'))
const { drawText, judgementCapHeight, judgementClips, JUDGEMENT_TEXT, measureText } = await import(designSrc('notes/judgement.ts'))
const { drawTimingLabel, renderTiming, showsTiming, timingCapHeight, timingOf, timingOffset, TIMINGS, TIMING_COLOR, TIMED_GRADES } = await import(designSrc('notes/timing.ts'))

// Sampling density: the port-time red/green grids were exhaustive (every word
// x 5 time points x both slots). For regression duty a representative subset
// carries the same invariants — glyph coverage, semantics and palette pins
// stay full; mesh comparisons shrink to representative words/grades/times.
export {}

const out = join(dirname(fileURLToPath(import.meta.url)), 'golden.json')

const palette = createPalette()
const hexSeeds = ['#35A7FF', '#FF5964', '#39E59E']

const rampOf = (p: NotePalette) => {
  const shades: Record<string, string> = {}
  for (const s of [50, 100, 200, 300, 400, 500, 600, 700, 800, 900, 950])
    shades[String(s)] = p[s]
  return { ...shades, ring: p.ring, ink: p.ink, hue: p.hue }
}

const ramps: Record<string, ReturnType<typeof rampOf>> = {}
for (const fam of FAMILIES)
  for (const dir of ['up', 'down'] as const)
    ramps[`${fam}/${dir}`] = rampOf(palette.family(fam, dir))
for (const seed of hexSeeds)
  ramps[`seed:${seed}`] = rampOf(derivePalette(seed))

const toneSteps = [30, 50, 400, 512.3, 649.7, 700, 950, 1000]
const toneCases = ramps['click/up'] && toneSteps.map(step => ({
  ramp: 'click/up',
  step,
  hex: tone(palette.family('click', 'up'), step),
}))

const ringToneCases = [0, 0.3, 0.7, 1, 1.5].map(k => ({
  ramp: 'click/up',
  k,
  hex: ringTone(palette.family('click', 'up'), k),
}))

const oklchCases = ['#35A7FF', '#FF5964', '#39E59E', '#FFFFFF', '#000000', '#5BC0EB'].map(hex => ({
  hex,
  oklch: hexToOklch(hex) as Oklch,
}))

const toRgbCases = ([
  { l: 0.9, c: 0.3, h: 150 },
  { l: 0.705, c: 0.165, h: 247 },
  { l: 0.5, c: 0.25, h: 20 },
  { l: 0.985, c: 0.03, h: 88 },
  { l: 0.24, c: 0.04, h: 160 },
  { l: 0.75, c: 0.4, h: 300 },
] as Oklch[]).map(oklch => ({ oklch, hex: toHex(oklchToRgb(oklch)) }))

const easeFns: Record<string, (t: number) => number> = {
  linear, inQuad, outQuad, outSine, inSine, inOutQuad, inCubic, outCubic, inOutCubic, outQuart, outExpo, inExpo,
}
const easeXs = [0, 0.234, 0.5, 0.777, 1]
const easeCases = Object.entries(easeFns).flatMap(([name, fn]) =>
  easeXs.map(x => ({ fn: name, x, v: fn(x) })))
easeCases.push({ fn: 'outBack', x: 0.3, v: outBack(0.3) })
easeCases.push({ fn: 'outBack(2)', x: 0.3, v: outBack(0.3, 2) })

const bezier = cubicBezier(0.7, 0, 0.75, 1)
const bezierCases = [0, 0.2, 0.5, 0.8, 0.878, 0.9, 0.976, 1].map(x => ({ x, v: bezier(x) }))

const clickCurve = riseSettleCurve(tokens.sizeCurve.click, tokens.sizeCurve.s)
const dragHeadCurve = riseSettleCurve(tokens.sizeCurve.dragHead, tokens.sizeCurve.s)
const curveCases = [0, 0.3, 0.5, 0.766, 0.878, 0.9, 0.976, 1].flatMap(p => [
  { kind: 'click', p, v: clickCurve(p) },
  { kind: 'dragHead', p, v: dragHeadCurve(p) },
])

const helpers = [
  { fn: 'seg', args: [0.5, 0.2, 0.8], v: seg(0.5, 0.2, 0.8) },
  { fn: 'seg', args: [0.9, 0.2, 0.8], v: seg(0.9, 0.2, 0.8) },
  { fn: 'seg', args: [0.1, 0.2, 0.8], v: seg(0.1, 0.2, 0.8) },
  { fn: 'bump', args: [0.5, 0.2, 0.8], v: bump(0.5, 0.2, 0.8) },
  { fn: 'lerp', args: [3, 7, 0.25], v: lerp(3, 7, 0.25) },
  { fn: 'clamp', args: [1.5, 0, 1], v: clamp(1.5, 0, 1) },
  { fn: 'clamp01', args: [-0.2], v: clamp01(-0.2) },
  { fn: 'ranged', args: [0.6, 0.2, 0.8], v: ranged(0.6, 0.2, 0.8) },
]

// mulberry32 — dump the first N values deterministically
function rngValues(seed: number, n: number) {
  const next = rng(seed)
  return Array.from({ length: n }, () => next())
}
const rngs = [
  { seed: 12345, values: rngValues(12345, 5) },
  { seed: 7, values: rngValues(7, 3) },
]

const mixCases = [
  { a: '#FF0000', b: '#00FF00', t: 0.35, hex: mix('#FF0000', '#00FF00', 0.35) },
  { a: '#333333', b: '#FFFFFF', t: 0.35, hex: mix('#333333', '#FFFFFF', 0.35) },
  { a: '#5BC0EB', b: '#FDE74C', t: 0.62, hex: mix('#5BC0EB', '#FDE74C', 0.62) },
]

const parseCases = ['#35A7FF', '#abc', '#abcd', '#12345678'].map(hex => {
  const { r, g, b } = parseHex(hex)
  return { hex, rgb: [r, g, b] }
})

// Stage 1b hold entrance probes use the real TS design functions.
const holdContext: DrawContext = {
  kind: 'hold', palette: palette.family('hold', 'up'), grades: tokens.grade,
  direction: 'up', bodyDirection: 'up', heading: 0,
  approach: 1.2, size: tokens.unit, unit: tokens.unit,
}
const holdEaseCases = easeXs.map(x => ({ x, v: inOutSine(x) }))
const holdSizeCases = [0, 0.2, 0.5, 0.77, 1].map(p => ({
  p, approach: holdContext.approach, v: holdSize(p, holdContext),
}))
// bracketLandP is private in hold.ts: mirror its exact definition and probe
// holdSize at either side of landing across normal, very short and zero windows.
const holdLandingCases = [0, 0.2, 0.6, 1.2, 2.4].map(approach => {
  const land = 1 - Math.min(0.2, (4 / 60) / Math.max(1e-3, approach))
  return { approach, land, before: holdSize(land - 0.01, { ...holdContext, approach }),
    at: holdSize(land, { ...holdContext, approach }) }
})
// Read bracket motion from the real head scene (the lock group precedes the head).
const holdBracketCases = [0.2, 0.5, 0.77, 1 - (4 / 60) / holdContext.approach, 1].map(p => {
  const scene = hold.enter.draw(p, holdContext)
  if (scene.type !== 'group') throw new Error('hold enter must be a group')
  const lock = scene.children[0]!
  if (lock.type !== 'group') throw new Error('hold lock must be a group')
  const arc = lock.children[0]!
  if (arc.type !== 'arc') throw new Error('hold lock must contain arcs')
  return { p, approach: holdContext.approach, spin: (arc.start + arc.end) / 2 - Math.PI / 2,
    radius: arc.r, width: arc.strokeWidth, alpha: lock.opacity }
})
const bodyRevealCases = [0, 0.08, 0.2, 0.35, 0.5].map(appear => ({ appear, ...bodyReveal(appear) }))
const holdWakeCases = [0, HOLD_WAKE.from, 0.45, 0.575, 0.7, HOLD_WAKE.to, 1].map(p => ({ p, v: holdWake(p) }))

// Exit probes are separate from the stage 1a/1b parity cases above.
const exitTimes = [-0.1, 0, 0.05, 0.15, 0.3, 0.5]
const exitAlphaCases = (['tap', 'holdHead', 'holdBody', 'dragChild'] as const).flatMap(kind =>
  exitTimes.map(t => ({ kind, t, v: exitAlpha(t, tokens.time.exit[kind]) })))
const exitRewindCases = [1.2, 0.6, 0, 1e-8].flatMap(approach =>
  [...exitTimes, 1.2].map(t => ({ approach, t, v: exitRewind(t, { ...holdContext, approach }) })))

// Stage 1b text probes. These are source poly vertices, NOT tessellated mesh
// vertices: TS emits a scene graph, and FlatGeom owns mesh tessellation/AA.
// Retain every exact point (including invisible, unpruned end frames), so C#
// can reconstruct the expected mesh independently of the port's layout math.
function textGeometry(scene: SceneNode) {
  const nodes: { points: number[], closed: boolean, width: number, color: string, alpha: number }[] = []
  function walk(n: SceneNode, x = 0, y = 0, scale = 1, alpha = 1) {
    alpha *= n.opacity ?? 1
    if (n.type === 'group') {
      const t = n.transform
      if (t?.rotate || t?.scaleX || t?.scaleY) throw new Error('Unexpected text transform')
      for (const c of n.children)
        walk(c, x + (t?.x ?? 0) * scale, y + (t?.y ?? 0) * scale, scale * (t?.scale ?? 1), alpha)
    }
    else if (n.type === 'poly') {
      if (n.join !== 'miter' || n.cap !== 'square' || !n.stroke) throw new Error('Unexpected text stroke')
      nodes.push({ points: n.points.flatMap(([px, py]) => [x + px * scale, y + py * scale]),
        closed: n.closed ?? false, width: n.strokeWidth! * scale, color: n.stroke, alpha })
    }
    else if (n.type === 'rect' && n.fill) {
      nodes.push({ points: [x + n.x * scale, y + n.y * scale, x + (n.x + n.w) * scale, y + n.y * scale,
        x + (n.x + n.w) * scale, y + (n.y + n.h) * scale, x + n.x * scale, y + (n.y + n.h) * scale],
        closed: true, width: 0, color: n.fill, alpha })
    }
    else throw new Error(`Unexpected text primitive: ${n.type}`)
  }
  walk(scene)
  const points = nodes.flatMap(n => Array.from({ length: n.points.length / 2 }, (_, i) => n.points.slice(i * 2, i * 2 + 2)))
  return { vertexCount: points.length, samples: [...new Set([0, Math.floor(points.length / 2), points.length - 1])]
    .filter(i => i >= 0 && i < points.length).map(i => ({ i, point: points[i] })), nodes }
}
const textTimes = [0, 0.25, 1]
const words = [...Object.values(JUDGEMENT_TEXT), 'FAST', 'SLOW', 'X', '0123456789%.']
const textMeasure = [15.36, 9.216].flatMap(H => words.flatMap(text =>
  [null, H * 0.7].map(gap => ({ text, H, gap, width: measureText(text, H, gap ?? undefined) }))))
textMeasure.push({ text: '?🙂', H: 15.36, gap: null, width: measureText('?🙂', 15.36) })
const textDraw = ['PERFECT', 'MISS'].flatMap(text => textTimes.map(t => {
  const H = judgementCapHeight(holdContext)
  const gap = lerp(H, H * 0.32, outCubic(seg(t, 0, 0.4)))
  return { text, H, t, gap, geometry: textGeometry(drawText(text, H, '#5BC0EB', {
    gap, pop: i => outBack(seg(t, 0.02 + i * 0.035, 0.2 + i * 0.035), 2),
  })) }
}))
const textGlyphs = [...'PERFCTGAODBMISXLW0123456789%.'].map(text => ({ text,
  geometry: textGeometry(drawText(text, 27, '#123456', { gap: 4.2, stroke: 3.7 })) }))
const judgementDraw = ['perfect', 'miss'].flatMap(grade => textTimes.flatMap(t =>
  [null, '#27496D'].map(override => ({ grade, t, override,
    geometry: textGeometry(judgementClips[grade].draw(t, { ...holdContext,
      grades: override ? { ...tokens.grade, [grade]: override } : tokens.grade })) }))))
const timingSlots = (['centre', 'stacked'] as const).flatMap(slot => [128, 200].map(unit => ({
  slot, unit, H: timingCapHeight({ ...holdContext, unit }, slot),
  offset: timingOffset({ ...holdContext, unit }, slot),
})))
const timingLabels = TIMINGS.flatMap(timing => [15.36, 9.216].map(H => ({ timing, H,
  geometry: textGeometry(drawTimingLabel(timing, H, TIMING_COLOR[timing])) })))
const timingDraw = ['great'].flatMap(grade => TIMINGS.flatMap(timing =>
  (['centre', 'stacked'] as const).flatMap(slot => textTimes.map(t => ({
    grade, timing, slot, t, geometry: textGeometry(renderTiming(timing, grade, t, holdContext, slot)),
  })))))

const golden = {
  textMeasure, textDraw, textGlyphs, judgementDraw, timingSlots, timingLabels, timingDraw,
  textTokens: { capHeight: judgementCapHeight(holdContext), grades: tokens.grade,
    durations: tokens.time.clear, showsTiming: GRADES.map(grade => ({ grade, value: showsTiming(grade) })),
    timingOf: [-0.2, 0, 0.2].map(error => ({ error, value: timingOf(error) })) },
  exitAlpha: exitAlphaCases,
  exitRewind: exitRewindCases,
  holdEase: holdEaseCases,
  holdSize: holdSizeCases,
  holdLanding: holdLandingCases,
  holdBracket: holdBracketCases,
  bodyReveal: bodyRevealCases,
  holdWake: holdWakeCases,
  meta: { format: 'flat-parity/1', generatedBy: 'golden.ts' },
  oklch: oklchCases,
  toRgb: toRgbCases,
  ramps,
  tone: toneCases,
  ringTone: ringToneCases,
  ease: easeCases,
  bezier: bezierCases,
  sizeCurve: curveCases,
  helpers,
  rng: rngs,
  mix: mixCases,
  parse: parseCases,
}

writeFileSync(out, `${JSON.stringify(golden, null, 2)}\n`)
console.log(`golden.json written: ${out}`)
