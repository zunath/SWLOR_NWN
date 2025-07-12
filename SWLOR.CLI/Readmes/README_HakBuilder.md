# HakBuilder Configuration

## Overview
The HakBuilder tool compiles NWN hak files from source directories. It includes an optional checksum checking feature to determine if haks need to be rebuilt.

## Configuration Options

### EnableChecksumChecking
- **Type**: `boolean`
- **Default**: `true`
- **Description**: When enabled, the HakBuilder will calculate MD5 checksums of source folders and compare them with previously stored checksums to determine if haks need to be rebuilt. When disabled, all haks will be rebuilt every time.

### Usage Examples

#### Enable checksum checking (default behavior)
```json
{
  "TlkPath": "./swlor2_tlk/swlor2_tlk.tlk",
  "OutputPath": "./output/",
  "EnableChecksumChecking": true,
  "HakList": [...]
}
```

#### Disable checksum checking (for large haks or faster builds)
```json
{
  "TlkPath": "./swlor2_tlk/swlor2_tlk.tlk",
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