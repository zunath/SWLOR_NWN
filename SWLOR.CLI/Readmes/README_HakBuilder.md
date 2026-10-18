# HakBuilder Configuration

## Overview
The HakBuilder tool compiles NWN hak files from source directories. It includes an optional checksum checking feature to determine if haks need to be rebuilt.

## Configuration Options

### EnableChecksumChecking
- **Type**: `boolean`
- **Default**: `true`
- **Description**: When enabled, the HakBuilder fingerprints source-folder contents and packer options to determine if haks need to be rebuilt. Legacy source-only checksum files trigger one rebuild. When disabled, all haks will be rebuilt every time.

### Usage Examples

#### Enable checksum checking (default behavior)
```json
{
  "TlkPath": "./sw_tlk/sw_tlk.tlk",
  "OutputPath": "./output/",
  "EnableChecksumChecking": true,
  "HakList": [...]
}
```

#### Disable checksum checking (for large haks or faster builds)
```json
{
  "TlkPath": "./sw_tlk/sw_tlk.tlk",
  "OutputPath": "./output/",
  "EnableChecksumChecking": false,
  "HakList": [...]
}
```

## Performance Considerations

- **Checksum checking enabled**: Faster for small haks or when few files have changed
- **Checksum checking disabled**: Faster for large haks where calculating checksums takes longer than rebuilding the hak

## Backward Compatibility

The `EnableChecksumChecking` property defaults to `true`, ensuring existing configurations continue to work without modification.

## Model compilation

HAK packaging copies MDLs as supplied. Compile changed ASCII models first with
CleanModels v4.4.2 bundled in cleanmodels-qt v1.1.7. Install it with
`SWLOR_Haks/tools/SetupModelCompiler.ps1`; `CLEANMODELS` can select another install
of the same pinned CLI. For staged rigid weapons, use
`python tools/CompileBlasterModels.py --resources <folder> --output <fresh-folder>`.
`SWLOR.CLI/compileModels.cmd` forwards these arguments to the same helper.
For HAK sources and their supermodels, use `SWLOR_Haks/tools/CompileModels.py`
and its validation report before `--apply`. See the model import and tint-map
guides for material, geometry, and skin checks. The legacy `CompileModels` field
is unused; it does not enable automatic model compilation during packaging.
