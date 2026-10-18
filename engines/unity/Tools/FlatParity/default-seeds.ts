/**
 * Emits the C# flat-skin default color tables for SettingsFactory's
 * restore-default-colors button: shade-400 palette seeds per note type,
 * snapshotted from cytoid-design's defaultPalette (createPalette() over
 * tokens.hue).
 *
 *   cd <cytoid-design>/packages/notes
 *   npx tsx <this-repo>/Tools/FlatParity/default-seeds.ts
 * (CYTOID_DESIGN_DIR overrides where the cytoid-design checkout lives.)
 *
 * Paste the printed dictionaries into SettingsFactory and regenerate
 * whenever tokens.hue changes upstream.
 */
import { join } from 'node:path'
import { pathToFileURL } from 'node:url'
import { existsSync } from 'node:fs'

// Root of the cytoid-design checkout; defaults to a sibling of this
// repository, override with CYTOID_DESIGN_DIR.
const designRoot = process.env.CYTOID_DESIGN_DIR ?? join(import.meta.dirname, '..', '..', '..', 'cytoid-design')
if (!existsSync(designRoot)) {
  console.error(`cytoid-design checkout not found at ${designRoot}`)
  console.error('Clone it as a sibling of this repository, or point CYTOID_DESIGN_DIR at an existing checkout.')
  process.exit(1)
}
const designSrc = (spec: string) => pathToFileURL(join(designRoot, 'packages', 'notes', 'src', spec)).href

const { defaultPalette } = await import(designSrc('palette.ts'))
const { FAMILIES } = await import(designSrc('tokens.ts'))

// ESM marker: top-level await needs module scope.
export {}

/** Mirrors FlatNoteRendererBase.FamilyFor (Cytoid note type -> design family). */
const NOTE_TYPES_OF_FAMILY: Record<(typeof FAMILIES)[number], string[]> = {
  click: ['Click'],
  hold: ['Hold'],
  'long-hold': ['LongHold'],
  drag: ['DragChild', 'DragHead'],
  'click-drag': ['CDragChild', 'CDragHead'],
  flick: ['Flick'],
  'drop-click': ['DropClick'],
  'drop-drag': ['DropDrag'],
}

for (const direction of ['Up', 'Down'] as const) {
  console.log(`private static readonly Dictionary<NoteType, string> FlatDefaultFill${direction} = new Dictionary<NoteType, string>`)
  console.log('{')
  for (const family of FAMILIES) {
    const hex = defaultPalette.family(family, direction.toLowerCase())[400]
    for (const noteType of NOTE_TYPES_OF_FAMILY[family])
      console.log(`    {NoteType.${noteType}, "${hex}"},`)
  }
  console.log('};')
  console.log()
}

// tokens.ring — neutral 50, deliberately not pure white (see tokens.ts).
console.log(`private const string FlatDefaultRingColor = "${defaultPalette.ring}";`)
