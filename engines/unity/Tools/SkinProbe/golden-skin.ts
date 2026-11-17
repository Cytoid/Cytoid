/**
 * Golden scene corpus for the C# skin-runtime port.
 *
 *   cd /tmp/cytoid-design/packages/notes
 *   npx tsx <cytoid-repo>/Tools/SkinProbe/golden-skin.ts
 *
 * CYTOID_DESIGN_DIR overrides the cytoid-design checkout (default
 * /tmp/cytoid-design). Parses the flat skin document, compiles it through the
 * TS reference interpreter (parseSkin → LoadedSkin → compileSkin) and dumps,
 * for every slot × driver value × context, the fully-resolved SceneNode tree
 * returned by clip.draw. The C# harness replays the identical grid through the
 * port and compares node-by-node. golden-skin.json is a generated artifact —
 * regenerate it before running the comparison.
 *
 * Contexts follow skin-flat's createContext (see packages/skin-flat/src/
 * render.ts): unit 128, ringStroke 128·0.085, hairStroke 128·0.022,
 * size 128·tokens.size(kind). The palette is built with the library's
 * derivePalette from the skin document's meta palette (the same construction
 * as packages/skin-flat/src/palette.ts createPalette — only the glue is
 * inlined here because importing the skin-flat sources requires a built
 * @cytoid/notes dist; all palette math stays in the imported library).
 */
import { readFileSync, writeFileSync } from 'node:fs'
import { dirname, join } from 'node:path'
import { fileURLToPath, pathToFileURL } from 'node:url'

const designRoot = process.env.CYTOID_DESIGN_DIR ?? '/tmp/cytoid-design'
const notesSrc = (spec: string) => pathToFileURL(join(designRoot, 'packages', 'notes', 'src', spec)).href

// Annotation-only structural mirrors of the design repo's exported types
// (tsx strips types without checking them).
type Direction = 'up' | 'down'
type NoteKind = string
type NotePalette = Record<string | number, string>
type Palette = {
  note: (kind: NoteKind, direction: Direction) => NotePalette
  family: (family: string, direction: Direction) => NotePalette
  grade: Record<string, string>
  ring: string
}
type Transform = { x?: number, y?: number, rotate?: number, scale?: number, scaleX?: number, scaleY?: number }
type StrokeStyle = { stroke?: string, strokeWidth?: number, cap?: string, join?: string, dash?: number[], dashOffset?: number }
type SceneNode = Record<string, any>
type Clip = { mode: string, duration: number, draw: (x: number, ctx: DrawContext) => SceneNode }
type DrawContext = Record<string, any>

const { parseSkin } = await import(notesSrc('skin/schema.ts'))
const { compileSkin } = await import(notesSrc('skin/compile.ts'))
const { LoadedSkin } = await import(notesSrc('skin/resources.ts'))
const { DEFAULT_RING_COLOR, derivePalette } = await import(notesSrc('palette.ts'))
const { FAMILY_OF, NOTE_KINDS } = await import(notesSrc('tokens.ts'))

const out = join(dirname(fileURLToPath(import.meta.url)), 'golden-skin.json')

// ---------------------------------------------------------------- skin ----
const document = JSON.parse(readFileSync(join(designRoot, 'packages', 'skin-flat', 'src', 'flat.skin.json'), 'utf8'))
const skin = parseSkin(document)
const designs = compileSkin(new LoadedSkin(skin, new Map()))

// ------------------------------------------------------------- palette ----
// Glue identical to skin-flat's createPalette, sourcing hues/chroma/grades
// from the skin document's meta palette. derivePalette (the math) is imported.
const metaPalette = skin.meta?.palette
const hueTable: Record<string, Record<Direction, number>> = metaPalette?.hue ?? {}
const chroma = metaPalette?.chroma ?? 0.165
const grades: Record<string, string> = { ...(skin.meta?.grades ?? {}), ...(metaPalette?.grade ?? {}) }
if (Object.keys(grades).length !== 5)
  throw new Error('skin meta must define all five grade colours')

function buildPalette(ring?: string): Palette {
  const cache = new Map<string, NotePalette>()
  const family = (fam: string, dir: Direction): NotePalette => {
    const key = `${fam}:${dir}:${ring ?? ''}`
    let p = cache.get(key)
    if (!p) {
      p = derivePalette(hueTable[fam]?.[dir] ?? 247, { chroma, ring })
      cache.set(key, p)
    }
    return p
  }
  return {
    family,
    note: (kind, dir) => family(FAMILY_OF[kind as keyof typeof FAMILY_OF], dir),
    grade: grades,
    ring: ring ?? DEFAULT_RING_COLOR,
  }
}
const basePalette = buildPalette()

// ------------------------------------------------------------ context ----
const UNIT = 128
// tokens.size table (packages/skin-flat/src/tokens.ts) — data, ported verbatim
const SIZE: Record<NoteKind, number> = {
  'click': 1,
  'hold': 1,
  'long-hold': 1,
  'drag-head': 0.675,
  'drag-child': 0.375,
  'flick': 1.125,
  'click-drag-head': 1,
  'click-drag-child': 0.375,
  'drop-click': 1,
  'drop-drag': 0.8,
}
const DEFAULT_HEADING = 0.3

interface ContextSpec {
  direction: Direction
  approach: number
  bodyDirection?: Direction
  heading?: number
  palette?: NotePalette
  colors?: Palette
  seed?: string
}

function makeContext(kind: NoteKind, spec: ContextSpec): DrawContext {
  return {
    kind,
    ringStroke: UNIT * 0.085,
    hairStroke: UNIT * 0.022,
    palette: spec.palette ?? basePalette.note(kind, spec.direction),
    grades: basePalette.grade,
    colors: spec.colors ?? basePalette,
    direction: spec.direction,
    bodyDirection: spec.bodyDirection ?? spec.direction,
    heading: spec.heading ?? DEFAULT_HEADING,
    approach: spec.approach,
    size: UNIT * SIZE[kind]!,
    unit: UNIT,
  }
}

// -------------------------------------------------------------- grids ----
const dedupe = (xs: number[]) => [...new Set(xs)]
const NORM_XS = dedupe([
  0, 0.001,
  ...Array.from({ length: 30 }, (_, k) => k / 29),
  0.499999, 0.5, 0.500001, 0.7, 0.86, 0.949999999, 0.999, 1,
])
const timeXs = (duration: number) => dedupe([
  0, 0.005, 0.02, 0.05, 0.1, 0.15, 0.21, duration * 0.5, duration * 0.99, duration,
])
const xsFor = (clip: Clip): number[] => {
  if (clip.mode === 'normalized' || clip.mode === 'progress')
    return NORM_XS
  if (clip.mode === 'static')
    return [0]
  return timeXs(clip.duration)
}
const midX = (xs: number[]) => xs[Math.floor(xs.length / 2)]!

const APPROACHES = [0.5, 1.2, 3]
const DIRECTIONS: Direction[] = ['up', 'down']
// seed variants + the ring override, applied to a few representative slots
const CUSTOM_SEEDS = ['#FF0080', '#00AA55', '#123456']
const RING_SEED = '#88DDFF'
const VARIANT_SLOTS = new Set(['click:enter', 'hold:enter', 'flick:enter', 'click:clear:perfect', 'click:feedback:judgement:perfect'])
const variantsFor = (kind: NoteKind): ContextSpec[] => [
  ...CUSTOM_SEEDS.map(seed => ({
    direction: 'up' as const,
    approach: 1.2,
    // derivePalette(hex) respects the hex's own L/C/H; ctx.colors keeps the default table
    palette: derivePalette(seed, { chroma }),
    seed,
  })),
  {
    direction: 'up' as const,
    approach: 1.2,
    palette: { ...basePalette.note(kind, 'up'), ring: RING_SEED },
    seed: `ring:${RING_SEED}`,
  },
]

// ---------------------------------------------------------- serialize ----
function pickTransform(t: Transform) {
  const out: Record<string, number> = {}
  for (const key of ['x', 'y', 'rotate', 'scale', 'scaleX', 'scaleY'] as const)
    if (t[key] !== undefined)
      out[key] = t[key]
  return Object.keys(out).length ? out : undefined
}

function serializeClip(clip: Record<string, any>) {
  const out: Record<string, any> = { type: clip.type }
  if (clip.type === 'rect') {
    out.x = clip.x
    out.y = clip.y
    out.w = clip.w
    out.h = clip.h
    if (clip.rx !== undefined)
      out.rx = clip.rx
  }
  else if (clip.type === 'circle') {
    if (clip.cx !== undefined)
      out.cx = clip.cx
    if (clip.cy !== undefined)
      out.cy = clip.cy
    out.r = clip.r
  }
  else if (clip.type === 'poly') {
    out.points = clip.points
  }
  else {
    throw new Error(`unknown clip shape "${clip.type}"`)
  }
  return out
}

function serializeStroke(node: StrokeStyle, out: Record<string, any>) {
  for (const key of ['stroke', 'strokeWidth', 'cap', 'join', 'dash', 'dashOffset'] as const)
    if (node[key] !== undefined)
      out[key] = node[key]
}

function serializePaint(node: SceneNode, out: Record<string, any>) {
  if (node.opacity !== undefined)
    out.opacity = node.opacity
  if (node.blend !== undefined)
    out.blend = node.blend
}

function serialize(node: SceneNode): Record<string, any> {
  if (node.type === 'group') {
    const out: Record<string, any> = { t: 'g' }
    serializePaint(node, out)
    if (node.isolate !== undefined)
      out.isolate = node.isolate
    if (node.transform)
      out.tf = pickTransform(node.transform)
    if (node.clip)
      out.clip = serializeClip(node.clip)
    out.children = node.children.map(serialize)
    return out
  }
  if (node.type === 'circle' || node.type === 'arc') {
    const out: Record<string, any> = { t: node.type }
    if (node.cx !== undefined)
      out.cx = node.cx
    if (node.cy !== undefined)
      out.cy = node.cy
    out.r = node.r
    if (node.type === 'arc') {
      out.start = node.start
      out.end = node.end
    }
    else if (node.fill !== undefined) {
      out.fill = node.fill
    }
    serializeStroke(node, out)
    serializePaint(node, out)
    return out
  }
  if (node.type === 'rect') {
    const out: Record<string, any> = { t: 'rect', x: node.x, y: node.y, w: node.w, h: node.h }
    if (node.rx !== undefined)
      out.rx = node.rx
    if (node.fill !== undefined)
      out.fill = node.fill
    serializeStroke(node, out)
    serializePaint(node, out)
    return out
  }
  if (node.type === 'poly') {
    const out: Record<string, any> = { t: 'poly', points: node.points }
    if (node.closed === true)
      out.closed = true
    if (node.fill !== undefined)
      out.fill = node.fill
    serializeStroke(node, out)
    serializePaint(node, out)
    return out
  }
  if (node.type === 'line') {
    const out: Record<string, any> = { t: 'line', x1: node.x1, y1: node.y1, x2: node.x2, y2: node.y2 }
    serializeStroke(node, out)
    serializePaint(node, out)
    return out
  }
  throw new Error(`cannot serialize node type "${node.type}"`)
}

// -------------------------------------------------------------- record ----
const scenes: Record<string, any>[] = []
let errors = 0

function record(slot: string, x: number | undefined, spec: ContextSpec, kind: NoteKind, draw: () => SceneNode, options?: Record<string, any>) {
  const scene: Record<string, any> = { slot }
  if (x !== undefined)
    scene.x = x
  if (options !== undefined)
    scene.options = options
  scene.ctx = {
    approach: spec.approach,
    direction: spec.direction,
    bodyDirection: spec.bodyDirection ?? spec.direction,
    heading: spec.heading ?? DEFAULT_HEADING,
    seed: spec.seed ?? 'default',
  }
  try {
    const root = draw()
    scene.root = root == null ? null : serialize(root)
  }
  catch (error) {
    errors++
    scene.error = String((error as Error)?.message ?? error)
  }
  scenes.push(scene)
}

/** direction × approach grid (enter / exit / hold press / loop* / progress) */
function emitFullGrid(slot: string, kind: NoteKind, clip: Clip) {
  const xs = xsFor(clip)
  for (const approach of APPROACHES)
    for (const direction of DIRECTIONS)
      for (const x of xs)
        record(slot, x, { direction, approach }, kind, () => clip.draw(x, makeContext(kind, { direction, approach })))
  emitVariants(slot, kind, clip, xs)
}

/** clear / miss slots: approach 1.2, direction up for every x + one down sample */
function emitGradeSlot(slot: string, kind: NoteKind, clip: Clip) {
  const xs = xsFor(clip)
  for (const x of xs)
    record(slot, x, { direction: 'up', approach: 1.2 }, kind, () => clip.draw(x, makeContext(kind, { direction: 'up', approach: 1.2 })))
  const x = midX(xs)
  record(slot, x, { direction: 'down', approach: 1.2 }, kind, () => clip.draw(x, makeContext(kind, { direction: 'down', approach: 1.2 })))
  emitVariants(slot, kind, clip, xs)
}

function emitVariants(slot: string, kind: NoteKind, clip: Clip, xs: number[]) {
  if (!VARIANT_SLOTS.has(slot))
    return
  for (const variant of variantsFor(kind))
    for (const x of xs)
      record(slot, x, variant, kind, () => clip.draw(x, makeContext(kind, variant)))
}

// ------------------------------------------------------------- sampling ----
for (const kind of NOTE_KINDS) {
  const design = designs[kind] as Record<string, any>
  emitFullGrid(`${kind}:enter`, kind, design.enter)
  for (const grade of ['perfect', 'great', 'good', 'bad'] as const)
    emitGradeSlot(`${kind}:clear:${grade}`, kind, (design.clear as Record<string, Clip>)[grade]!)
  if (design.exit)
    emitFullGrid(`${kind}:exit`, kind, design.exit)
  if (design.miss)
    emitGradeSlot(`${kind}:miss`, kind, design.miss)
  if (design.hold) {
    emitFullGrid(`${kind}:press`, kind, design.hold.press)
    if (design.hold.loopUnder)
      emitFullGrid(`${kind}:loopUnder`, kind, design.hold.loopUnder)
    if (design.hold.loopOver)
      emitFullGrid(`${kind}:loopOver`, kind, design.hold.loopOver)
    emitFullGrid(`${kind}:progress`, kind, design.hold.progress)
  }
  if (design.body) {
    const BODY_PROGRESS = [0, 0.3, 0.86, 0.95, 1]
    const BODY_T = [0, 1.3]
    const BODY_HOLD = [1.5, 3]
    const grid: Record<string, any>[] = kind === 'hold'
      ? [220, 900].flatMap(length =>
          BODY_PROGRESS.flatMap(progress =>
            BODY_T.flatMap(t =>
              BODY_HOLD.map(holdDuration => ({ length, progress, t, holdDuration, appear: 1 })))))
      : [300, 900].flatMap(top =>
          [200, 1200].flatMap(bottom =>
            BODY_PROGRESS.flatMap(progress =>
              BODY_T.flatMap(t =>
                BODY_HOLD.map(holdDuration => ({ top, bottom, progress, t, holdDuration, appear: 1 }))))))
    for (const direction of DIRECTIONS)
      for (const options of grid) {
        const spec: ContextSpec = { direction, approach: 1.2, bodyDirection: direction }
        record(`${kind}:body`, undefined, spec, kind, () => design.body.draw(options, makeContext(kind, spec)), options)
      }
  }
  if (design.chainLink) {
    const CHAIN_LENGTH = [220, 900]
    const CHAIN_T = [0, 1.3]
    for (const direction of DIRECTIONS)
      for (const length of CHAIN_LENGTH)
        for (const t of CHAIN_T)
          record(`${kind}:chainLink`, undefined, { direction, approach: 1.2 }, kind,
            () => (design.chainLink as Clip).draw({ length, t }, makeContext(kind, { direction, approach: 1.2 })),
            { length, t })
  }
}

// feedback slots — every kind owns its three groups (compile.ts skin[kind].feedback)
for (const kind of NOTE_KINDS) {
  const feedback = (designs[kind] as Record<string, any>).feedback as Record<string, any>
  for (const view of ['judgement', 'timing', 'combined'] as const) {
    const entries = feedback[view] as Record<string, any>
    for (const [grade, value] of Object.entries(entries)) {
      // judgement maps grade → Clip; timing/combined map grade → { fast: Clip, slow: Clip }
      const clips: Record<string, Clip> = typeof (value as Clip).draw === 'function'
        ? { '': value as Clip }
        : value as Record<string, Clip>
      for (const [timing, clip] of Object.entries(clips)) {
        const slot = `${kind}:feedback:${view}:${grade}${timing ? `:${timing}` : ''}`
        const xs = xsFor(clip)
        for (const x of xs)
          record(slot, x, { direction: 'up', approach: 1.2 }, kind, () => clip.draw(x, makeContext(kind, { direction: 'up', approach: 1.2 })))
        const x = midX(xs)
        record(slot, x, { direction: 'down', approach: 1.2 }, kind, () => clip.draw(x, makeContext(kind, { direction: 'down', approach: 1.2 })))
        emitVariants(slot, kind, clip, xs)
      }
    }
  }
}

// heading samples — drag-head arrows point somewhere other than "up"
{
  const head = designs['drag-head'] as Record<string, any>
  record('drag-head:enter', midX(NORM_XS), { direction: 'up', approach: 1.2, heading: 2.1 }, 'drag-head',
    () => head.enter.draw(midX(NORM_XS), makeContext('drag-head', { direction: 'up', approach: 1.2, heading: 2.1 })))
  const exitX = head.exit.duration * 0.5
  record('drag-head:exit', exitX, { direction: 'up', approach: 1.2, heading: 2.1 }, 'drag-head',
    () => head.exit.draw(exitX, makeContext('drag-head', { direction: 'up', approach: 1.2, heading: 2.1 })))
}

// -------------------------------------------------------------- output ----
const corpus = { version: 1, scenes }
writeFileSync(out, `${JSON.stringify(corpus)}\n`)
const failed = scenes.filter(scene => scene.error)
console.log(`golden-skin.json written: ${out}`)
console.log(`scenes: ${scenes.length} (errors: ${errors})`)
if (errors)
  for (const scene of failed)
    console.log(`  ${scene.slot} x=${scene.x ?? '-'} ctx=${JSON.stringify(scene.ctx)} → ${scene.error}`)
