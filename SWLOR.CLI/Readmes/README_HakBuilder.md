# HakBuilder Configuration

## Overview
The HakBuilder tool compiles NWN hak files from source directories. It includes an optional checksum checking feature to determine if haks need to be rebuilt.

## Configuration Options

### BeforeBuild
Optional commands run before validating or deleting any HAK/TLK build outputs.
Each entry has `Command`, an `Arguments` array, and `OutputDirectories` for
generated HAK inputs. Paths are relative to the configuration's working directory.
A failed command stops the HAK build and preserves the previous archives.
The deploy command allows these generated directories to be absent on a clean
checkout, while still skipping deployment when ordinary source inputs are missing.
The repository configuration uses this to generate the ignored female neck
binding variants automatically; Python with NumPy must be available on PATH.

### EnableChecksumChecking
- **Type**: `boolean`
- **Default**: `true`
- **Description**: When enabled, the HakBuilder will calculate MD5 checksums of source folders and compare them with previously stored checksums to determine if haks need to be rebuilt. When disabled, all haks will be rebuilt every time.

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
