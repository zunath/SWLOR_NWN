"""Exercise the actual icon-family reader after canonical feat rows move."""

import json
from pathlib import Path
import shutil
import subprocess
import tempfile
import unittest


ROOT = Path(__file__).resolve().parents[2]
POWERSHELL = shutil.which("pwsh") or shutil.which("powershell")


@unittest.skipUnless(POWERSHELL, "PowerShell is required by the icon generator")
class GameplayIconFamilyTests(unittest.TestCase):
    def read_families(self, rows):
        with tempfile.TemporaryDirectory(prefix="swlor-icon-families-") as scratch:
            directory = Path(scratch)
            feats = directory / "feat.2da"
            feats.write_text("2DA V2.0\n\nLABEL FEAT DESCRIPTION ICON\n" + "\n".join(rows), encoding="utf-8")
            driver = directory / "read.ps1"
            driver.write_text(r'''
param([string]$Source, [string]$Feats)
$ErrorActionPreference = "Stop"
$ast = [System.Management.Automation.Language.Parser]::ParseFile($Source, [ref]$null, [ref]$null)
foreach ($name in @("Get-RankFromText", "Get-AbilityRows")) {
    $definition = $ast.FindAll({ param($node)
        $node -is [System.Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -eq $name
    }, $true) | Select-Object -First 1
    Invoke-Expression $definition.Extent.Text
}
$Feat2daPath = $Feats
ConvertTo-Json -InputObject @(Get-AbilityRows $Feats 2000 4000) -Compress
''', encoding="utf-8")
            result = subprocess.run(
                [POWERSHELL, "-NoProfile", "-ExecutionPolicy", "Bypass", "-File", str(driver),
                 "-Source", str(ROOT / "tools/UpdateGameplayIconStandards.ps1"), "-Feats", str(feats)],
                check=True, capture_output=True, text=True, timeout=30,
            )
            return json.loads(result.stdout)

    def test_relocated_jab_retains_its_existing_manifest_identity(self):
        labels = {"Jab1", "Jab2", "Jab3", "Jab4", "AutoCastJab1", "DirtyJab1"}
        rows = [line for line in (ROOT / "SWLOR_Haks/sw_2da/feat.2da").read_text().splitlines()
                if len(line.split()) > 1 and line.split()[1] in labels]
        families = {row["IconResRef"]: row for row in self.read_families(rows)}
        self.assertEqual("Jab1", families["ife_jab"]["Key"])
        self.assertEqual(1, families["ife_jab"]["Rank"])
        self.assertEqual("DirtyJab1", families["ife_dirtyjab"]["Key"])

    def test_alias_before_canonical_cannot_name_the_icon_family(self):
        families = self.read_families([
            "2000 AutoCastExample1 1 2 ife_example",
            "2001 Example2 1 2 ife_example",
            "2002 Example1 1 2 ife_example",
        ])
        self.assertEqual(1, len(families))
        self.assertEqual("Example1", families[0]["Key"])
        self.assertEqual(1, families[0]["Rank"])


if __name__ == "__main__":
    unittest.main()
