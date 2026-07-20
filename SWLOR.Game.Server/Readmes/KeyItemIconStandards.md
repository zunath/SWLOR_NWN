# Key Item Icon Standards

Key Item icons use the `iki_` prefix, are stored in `SWLOR_Haks/sw_item`, and are displayed at 40 x 40 pixels in the Key Items window. Final assets are uncompressed 64 x 64 true-color TGA files and must stay within NWN's 16-character resource-name limit.

## Shared Art Direction

- Polished hand-painted science-fantasy game icon.
- Dark navy-black background with restrained cyan rim lighting.
- One centered subject with a bold silhouette and generous padding.
- Subtle inset octagonal "galactic archive" frame.
- No readable text, letters, numbers, logos, watermarks, UI labels, character portraits, or tiny peripheral clutter.
- The composition must remain readable when reduced to 40 x 40 pixels.

Category accents are amber-gold for Maps, violet-magenta for Quest Items, ivory-gold for Documents, red-orange for Keys, and emerald-green for Field Notes.

The empty Key Items view uses the neutral archive placeholder `iki_default` instead of an empty resref, which NWN renders as its error icon.

`tools/GetKeyItemIconPrompts.ps1` produces the complete prompt for every Key, Quest Item, and Document icon from its canonical `KeyItemType` name and description.

## Reuse Rules

Keys, Quest Items, and Documents receive unique artwork and a unique `iki_NNNN` resref derived from their `KeyItemType` value.

Maps reuse six semantic icons:

- Orbit chart: `iki_map_orbit`
- Wilderness topography: `iki_map_wild`
- Settlement plan: `iki_map_settle`
- Facility blueprint: `iki_map_facility`
- Cavern network: `iki_map_cavern`
- Ruins or temple survey: `iki_map_ruins`

Field Notes reuse six document icons:

- Published field guide: `iki_fn_guide`
- Licensed research datapad: `iki_fn_datapad`
- Handwritten discovery journal: `iki_fn_journal`
- Holographic observation log: `iki_fn_holo`
- Sealed boss dossier: `iki_fn_dossier`
- Encrypted restricted report: `iki_fn_restrict`

Field Note icons must depict documents or data records only. Do not depict a beast, creature silhouette, anatomy, tracks, eggs, specimens, DNA, enzymes, reagents, or laboratory apparatus. Beast names, `AppearanceType` identifiers, 2DA labels, and assigned portraits are not reliable visual references.
