#!/usr/bin/env bash
# P2-CB1 proof: sorted, whitespace-normalised, non-using lines of the unsplit Combat.cs (git rev $1) against the
# split files (Service/Combat.cs + Service/CombatParts/*.cs). Prints the multiset difference ("<" only in the old
# file, ">" only in the new files). Run from the repository root.
set -euo pipefail
norm() { tr -d '\r' | sed -E 's/^[[:space:]]+//; s/[[:space:]]+$//; s/[[:space:]]+/ /g' | grep -v '^$' | grep -v '^using ' | LC_ALL=C sort; }
git show "$1:SWLOR.Game.Server/Service/Combat.cs" | norm > "${TMPDIR:-/tmp}/combat.old.txt"
cat SWLOR.Game.Server/Service/Combat.cs SWLOR.Game.Server/Service/CombatParts/*.cs | norm > "${TMPDIR:-/tmp}/combat.new.txt"
wc -l < "${TMPDIR:-/tmp}/combat.old.txt" | sed 's/^/old lines: /'; wc -l < "${TMPDIR:-/tmp}/combat.new.txt" | sed 's/^/new lines: /'
diff "${TMPDIR:-/tmp}/combat.old.txt" "${TMPDIR:-/tmp}/combat.new.txt" | grep '^[<>]' | LC_ALL=C sort | uniq -c || true
