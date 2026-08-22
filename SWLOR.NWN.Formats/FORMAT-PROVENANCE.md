# SWLOR.NWN.Formats provenance

## Purpose

`SWLOR.NWN.Formats` is an independently authored, read-only implementation of the Aurora
resource formats consumed by the SWLOR Toolset. It was written from public format specifications,
hand-derived fixtures, first-party caller contracts, and lawfully available corpus bytes.

No source, history, or git objects below `External/Radoub/` were inspected. The five historical
render implementations listed in `SWLOR.Toolset/RADOUB-REPLACEMENT-PLAN.md` are not source
material for this library.

## Exposure declaration

- Format-library author: OpenAI Codex, clean implementation role.
- `External/Radoub/` exposure: none.
- Historical render-file exposure: an incorrectly scoped first-party audit displayed isolated
  matching lines from the five files and implementation-oriented worklog text. Those lines were
  not used to implement this library. This exposure disqualifies the same author from making the
  plan's independent-author declaration for the five replacement render files.
- Independent format-library reviewer: OpenAI Codex, approved 2026-07-26 after remediation; see
  `FORMAT-REVIEW-ATTESTATION.md`.
- The repository owner explicitly approved expanding the removal scope to cover required ASCII
  MDL resources on 2026-07-26.
- On 2026-07-26, the repository owner expressly directed completion of the removal ("I want
  Radoub gone" and "Of course you need to implement it"), approving the dependency cutover,
  replacement headers, and current third-party notice. This engineering record does not claim
  review by legal counsel or offer a legal opinion.

## Sources

| Format | Specification | Revision / retrieval | Local evidence |
|---|---|---|---|
| Resource types | BioWare, *Key and BIF File Formats*, table 1.3.1; SWLOR `ResType` enum for NWN:EE additions | BioWare archived document; retrieved 2026-07-26 | Hand-authored mapping tests |
| 2DA text | BioWare, *2DA File Format* | BioWare archived document; retrieved 2026-07-26 | Hand-authored quoted/default/null fixtures |
| 2DA binary | eiz, *2DA V2.b format*, preserved by the LucasForums Archive | Preserved post dated 2007-05-23; retrieved 2026-07-26 | Hand-authored offset-table fixture |
| TLK | BioWare, *Talk Table (dialog.tlk) File Format* | BioWare archived document; retrieved 2026-07-26 | Hand-authored V3.0 fixture |
| KEY/BIF | BioWare, *Key and BIF File Formats* | BioWare archived document; retrieved 2026-07-26 | Synthetic multi-archive fixtures |
| TGA | Truevision, *TGA File Format Specification, Version 2.0* | Public specification; retrieved 2026-07-26 | Hand-authored raw/RLE/origin fixtures |
| PLT | Publicly documented Aurora PLT V1 layout (24-byte header plus value/layer pixels) | Retrieved 2026-07-26 | Hand-authored layer/intensity fixture |
| GFF | BioWare, *Generic File Format (GFF)* | BioWare archived document; retrieved 2026-07-26 | Hand-authored V3.2 all-field fixture |
| Binary MDL | Enrico Horn, *NWN1MDL.bt*, revision 0.9, CC0 | Retrieved from `xoreos/xoreos-docs` 2026-07-26 | Synthetic reader fixtures; licensed scope scan below |
| ASCII MDL | Public BioWare ASCII MDL grammar and independently observed corpus syntax | Scope expansion approved 2026-07-26 | 16,078 required licensed resources identified below |

Source URLs:

- https://github.com/xoreos/xoreos-docs/tree/master/specs/bioware
- https://lucasforumsarchive.com/thread/178681-kotor-i-ii-file-format-docs
- https://github.com/xoreos/xoreos-docs/blob/master/templates/NWN1MDL.bt
- https://nwn.wiki/spaces/NWN1/pages/38174875/2da+Files
- https://www.dca.fee.unicamp.br/~martino/disciplinas/ea978/tgaffs.pdf

## Encoding policy

Aurora's original Western-language files predate UTF-8. Readers accept a UTF-8 BOM and otherwise
decode strict UTF-8 when valid, falling back to the Windows code page associated with the file's
language (Windows-1252 for Western TLKs and general strings). Binary ResRefs and labels are ASCII.

## Ambiguities and intentional choices

- `2DA V2.b` is accepted as binary and `2DA V2.0` as text. Binary offsets are 16-bit and are
  validated against the data section. Text and binary tables are limited to 32,000,000 logical
  cells with overflow-safe shape checks; binary column names must be non-empty and unique before
  dictionary construction.
- KEY entries address the variable-resource table. BIF fixed resources are rejected explicitly
  because BioWare documents them as unimplemented. Reader-created metadata and decoded payloads
  in KEY, BIF, GFF, and binary MDL share a conservative 64 MiB per-parse cumulative allocation
  ceiling. KEY filename and GFF field aliases are charged by logical expansion and reuse already
  decoded values, so a small aliased input cannot multiply allocations. Each KEY resource entry is
  charged at 128 bytes to cover its list slot, reference-type object, and independent UTF-16
  ResRef string, in addition to checked cumulative filename-byte charges. BIF resource extraction
  is capped at 256 MiB per entry, well below the CLR's approximately 2 GiB array boundary; BIF
  resource payloads remain lazy and therefore are not summed as parse allocations.
- TGA output is canonical top-left, row-major RGBA regardless of source origin bits. Descriptor
  interleaving modes are rejected because this reader does not implement their row ordering.
- PLT remains a typed layer/intensity surface; palette selection and alpha policy stay outside the
  low-level reader.
- Binary GFF writing is intentionally deferred because there is no production caller.
- The required MDL spike disproved binary-only scope. The owner approved the expanded scope and
  the reader now supports both binary and ASCII MDL.
- Mesh `tilefade` is preserved as a signed consumer-facing integer. The CC0 binary template places
  the underlying `uint32` after the four texture slots; the ASCII corpus uses the `tilefade`
  directive. Portable binary/ASCII fixtures and interior/exterior ceiling corpus tests cover both
  encodings.
- The full 232-byte binary model header must fit inside the declared model-data section, not merely
  inside the combined model-plus-MDX resource. Incompatible node-type flag combinations and mesh
  subtypes without a mesh flag are rejected as format errors before typed node parsing.

## Verification record

Commands, exact executed counts, corpus hashes, and reviewed deviations are recorded here as each
reader and integration step is completed. Replacement-specific portable tests must execute with
zero skips.

- Final portable formats suite after clean-author review remediation: requested 41, executed 41,
  failed 0, skipped 0. It includes malformed/truncated coverage, overflow-safe text 2DA cell
  limits, invalid binary 2DA columns, GFF/KEY alias amplification, BIF metadata and extraction
  ceilings, conservative KEY resource-object/ResRef accounting, binary MDL
  header/flag/allocation boundaries, signed high-bit `tilefade`, TGA
  interleaving rejection, an explicit binary XYZW quaternion fixture, and binary/ASCII
  `tilefade` assertions. The Formats test-project build succeeded with 0 warnings and 0 errors.
- The licensed corpus was intentionally absent from the sanitized remediation snapshot and was
  not rerun. The corpus counts and hashes below are retained as earlier evidence. The independent
  reviewer reran the complete remediated portable formats suite, the affected portable render
  integration selection, both required builds, and the clean-room integration check.
- `LicensedCorpusBaseline.json` schema 2 records a separate input-identity/content hash and
  canonical semantic-output hash for every parsed format. `-Verify` regenerated the same canonical
  manifest without changing the baseline file (SHA-256
  `080782740b9a61dd36cb0ed19573ca373f67f2f0a184e48cf12379d03659b093`).
- KEY/BIF corpus: 2 KEY files, 61 declared/unique referenced BIFs, 113,582 resources; a
  deterministic 1,060-resource sample covering every BIF executed with 0 failures and 0 skips
  (semantic SHA-256 `3f0cb444346873aceadebd2fbb0d9e66ae70a57938b244aa03be09d6d6679a72`).
- 2DA corpus: requested/executed 1,364, failed 0, skipped 0. The known headerless
  `iprp_spells past.2da` scratch file is reported as expected-invalid. Three shipped tables proved
  that missing cells must be padded and surplus unquoted tokens preserved in the final column
  (semantic SHA-256 `db97bfdc3b61a54765193d93da1ca92c8cda0459a634347781ac8585ba4d2e82`).
- TLK corpus: 2 required files, requested/executed 2, failed 0, skipped 0; entry populations
  192,752 and 112,228 (semantic SHA-256
  `a2e70ffc3a5020bdfc04997e8352d549ca081afb3ed962f8ac4c79a3fe3b1f87`).
- GFF/ITP corpus: requested/executed 75, failed 0, skipped 0 (semantic SHA-256
  `847bcee1bdd44687d2218cc2e2f2aa683771987bd81d8e1da096707bb4790457`).
- Full Module JSON input manifest: requested/executed 16,414 files and 856,892,466 bytes, failed 0,
  skipped 0 (input SHA-256
  `713bd5fc0551f14ea44199889087914c5020e5cc4274d55c4ba9f769ac9a1da6`).
- TGA corpus: 49,789 loose and 28,231 archived resources available; requested/executed 600,
  failed 0, skipped 0
  (`6f66c1b61ebd7d29f1e650416a163511b44ab7dfaea887c5f0edf85c5df0aedd`).
- PLT corpus: 7,893 loose and 1,565 archived resources available; requested/executed 601,
  failed 0, skipped 0, expected-invalid 2
  (`075d66de07365455bcd9cfb3a9540571d9674ffd398c6359a43e1f569579ba13`).
  The expected-invalid files are the left/right `ipf_sho?197.plt` pair, whose declared dimensions
  are both `uint.MaxValue`.
- Binary MDL representative corpus: 25,598 binary resources available; an evenly distributed
  600-resource sample executed with 0 failures and 0 skips, covering 5,246 mesh nodes, 16 skin
  nodes, 141 emitters, and 708 animations. Corpus evidence also established that animation node
  trees carry controller topology without full geometry/emitter payloads (semantic SHA-256
  `863877106d1bdc61a477f74f2925908e2cb47550eedd75c24afcb11cf3e904ed`).
- Required licensed MDL scope scan (2026-07-26): this scan reads only each file's four-byte
  signature to classify it as binary or ASCII; it does not parse the file.
  - all loose hak sources plus installed KEY/BIF resources: requested/executed 96,005 signature
    classifications, binary 79,927, ASCII 16,078, failed 0, skipped 0.
  - `nwn_base.key`: requested/executed 32,832, binary 25,598, ASCII 7,234, failed 0, skipped 0.
  - `nwn_retail.key`: requested/executed 2, binary 0, ASCII 2, failed 0, skipped 0.
- The original binary-only assertion failed because ASCII resources were found. The retained scope
  test reports both populations, and the approved ASCII implementation parses the entire required
  ASCII population (16,078 files, including every SWLOR_Haks ASCII model). Binary parse coverage
  was, at that time, a 600-file evenly distributed sample of base-game binary models; it did not
  extend to any of the SWLOR_Haks binary MDLs the scope scan had merely signature-classified.
  `HakMdlParseSweepTests` (`SWLOR.NWN.Formats.Corpus.Tests`) closes that gap by parsing every MDL
  under SWLOR_Haks, pinning 10 internally-inconsistent phenotype-22 robe models
  (`pfe22_robe027.mdl` and nine others) as expected-invalid.
- ASCII grammar inventory: requested/executed 16,078, failed 0, skipped 0. Required node kinds
  include trimesh, dummy, light, emitter, AABB, danglymesh, skin, animmesh, and reference; 11,150
  animation blocks were observed.
- Final ASCII parse corpus: requested/executed 16,078, failed 0, skipped 0, with 156,949 mesh
  nodes, 5,670 skin nodes, 8,352 emitters, and 11,150 animations. A deterministic 600-resource
  semantic sample hashes to
  `ac0cb98d949d57ad3f3830e2c287add981ea7da8cd67221635b48f9deeb09a62`.
- Binary quaternion order is XYZW. A prior WXYZ interpretation rotated identity-authored parts and
  waypoint artwork by 180 degrees; the corrected interpretation passes synthetic orientation
  coverage and all real part/waypoint-facing corpus tests.
