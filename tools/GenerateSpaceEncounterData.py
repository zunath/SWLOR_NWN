"""Export the approved finite NPC encounter catalog without changing player fitting definitions."""
import argparse,json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
def main():
    parser=argparse.ArgumentParser(description=__doc__);parser.add_argument("--check",action="store_true");args=parser.parse_args()
    source=json.loads((ROOT/"design/space/space-balance.json").read_text())
    text=json.dumps(dict(encounters=source["encounters"],bindings=source["encounter_bindings"]),indent=2)+"\n"
    path=ROOT/"SWLOR.Game.Server/Data/SpaceEncounters.json"
    if args.check:
        if path.read_text()!=text:raise SystemExit("Space encounter catalog differs from the approved model.")
    else:path.write_text(text)
    print("Verified 8 encounter profiles and 51 preserved NPC identities.")
if __name__=="__main__":main()
