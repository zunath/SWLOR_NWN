"""Find and batch-extract resources from packed HAKs."""
import subprocess
from collections import defaultdict
from pathlib import Path

REPO = Path(__file__).resolve().parents[2]
ERF = REPO / "tools/SWLOR.CLI/nwn_erf.exe"


class HakIndex:
    """Resource name -> HAK; module HAK order decides which copy wins (first listed)."""

    def __init__(self, hak_dir: Path, hak_order):
        self.where = {}
        self.missing = [name for name in hak_order if not (hak_dir / f"{name}.hak").is_file()]
        for name in hak_order:
            hak = hak_dir / f"{name}.hak"
            if not hak.is_file():
                continue
            listing = subprocess.run([str(ERF), "-t", "-f", str(hak)], capture_output=True, text=True, check=True).stdout
            for resource in listing.split():
                self.where.setdefault(resource.lower(), hak)

    def exists(self, resname: str) -> bool:
        return resname.lower() in self.where

    def extract(self, resnames, dest: Path, chunk=300):
        """Extract every name that exists into dest; returns the extracted names."""
        dest.mkdir(parents=True, exist_ok=True)
        by_hak = defaultdict(list)
        for name in {n.lower() for n in resnames}:
            if name in self.where and not (dest / name).exists():
                by_hak[self.where[name]].append(name)
        for hak, names in by_hak.items():
            for i in range(0, len(names), chunk):
                subprocess.run([str(ERF), "-x", "-f", str(hak), *names[i:i + chunk]], cwd=dest,
                               capture_output=True, check=True)
        return [n for n in {n.lower() for n in resnames} if (dest / n).exists()]
