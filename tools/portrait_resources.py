"""Resolve historical TGA audit references to verified retained resources."""
import csv
import hashlib
from pathlib import Path, PureWindowsPath
import re


# Independent of the CSV, so a partial asset checkout cannot silently pass.
EXPECTED_CONVERSION_COUNT = 8109


def safe_name(name, suffix):
    return (bool(name) and Path(name).name == name
            and PureWindowsPath(name).name == name
            and ':' not in name and name.lower().endswith(suffix))


class PortraitResources:
    def __init__(self, portraits, manifest=None, *, expected_count=EXPECTED_CONVERSION_COUNT):
        self.root = Path(portraits).resolve()
        self.conversions = {}
        manifest = Path(manifest) if manifest else self.root.parent / 'portrait_dds_conversions.csv'
        if not manifest.exists():
            return
        with manifest.open(newline='', encoding='utf-8') as stream:
            rows = list(csv.DictReader(stream))
        if len(rows) != expected_count:
            raise ValueError(f'Incomplete DDS conversion manifest: expected {expected_count} rows, found {len(rows)}')
        outputs = set()
        for row in rows:
            name, output = row['file'], row['output']
            if (not safe_name(name, '.tga') or not safe_name(output, '.dds')
                    or name.lower() in self.conversions or output.lower() in outputs
                    or Path(name).stem.lower() != Path(output).stem.lower()):
                raise ValueError(f'Unsafe or duplicate DDS conversion resource: {name} -> {output}')
            for field in ('source_sha256', 'output_sha256'):
                if not re.fullmatch('[0-9a-f]{64}', row[field]):
                    raise ValueError(f'Invalid {field}: {name}')
            self.conversions[name.lower()] = row
            outputs.add(output.lower())

    def path(self, name):
        if not (safe_name(name, '.tga') or safe_name(name, '.dds')):
            raise ValueError(f'Unsafe portrait resource: {name}')
        path = self.root / name
        if path.resolve().parent != self.root:
            raise ValueError(f'Portrait resource escapes directory: {name}')
        return path

    def historical_digest(self, name):
        """Return a TGA digest only after checking the actual retained bytes."""
        path = self.path(name)
        if path.exists():
            return hashlib.sha256(path.read_bytes()).hexdigest()
        row = self.conversions.get(name.lower())
        if row is None:
            raise ValueError(f'Missing portrait: {name}; no recorded DDS conversion')
        output = self.path(row['output'])
        if not output.exists() or hashlib.sha256(output.read_bytes()).hexdigest() != row['output_sha256']:
            raise ValueError(f'DDS portrait changed or missing: {row["output"]}')
        return row['source_sha256']

    def verify(self, name, expected):
        if self.historical_digest(name) != expected:
            raise ValueError(f'Portrait source hash changed: {name}')
