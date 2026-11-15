#!/bin/sh
# Re-measures the domain order (README.md). Usage: run_all.sh <out folder> [repository root] [configuration]
# The server must be built in <configuration> (default Release). Writes refs.tsv (about 100 MB) and the json into <out>.
set -e
if [ -z "$1" ]; then echo "Usage: run_all.sh <out folder> [repository root] [configuration]" >&2; exit 2; fi
here="$(cd "$(dirname "$0")" && pwd)"
mkdir -p "$1"
export DOMAIN_ORDER_OUT="$(cd "$1" && pwd)"
export DOMAIN_ORDER_REPO="$(cd "${2:-$here/../../..}" && pwd)"
dotnet run -c Release --project "$here/EdgeExtract" -- "$DOMAIN_ORDER_REPO" "$DOMAIN_ORDER_OUT" "${3:-Release}"
cd "$here/py"
python guard_figures.py   # today's pairs and upward references as the guard counts them (guard.json)
python order_cost.py      # moves for the recorded order, its cost and the cheapest order (moves.json)
python pair_steps.py      # pairs at each step (stages.json)
python upward.py          # upward edges after the moves, with fix and item (upedges.json)
python appendix_c.py      # Appendix C table (appendixC.md)
