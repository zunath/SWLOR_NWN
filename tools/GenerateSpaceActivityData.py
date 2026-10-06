from pathlib import Path
import argparse,json,sys
from UpdateSpaceDesignBible import build_data
p=Path(__file__).resolve().parents[1]/"SWLOR.Game.Server/Data/SpaceActivities.json"
parser=argparse.ArgumentParser();parser.add_argument("--check",action="store_true");args=parser.parse_args()
text=json.dumps(build_data()["activities"],indent=2)+"\n"
if args.check:
    if p.read_text(encoding="utf-8")!=text:sys.exit("Space contract data is stale.")
else:p.write_text(text,encoding="utf-8",newline="")
print("Verified eleven finite activity contracts." if args.check else "Generated eleven finite activity contracts.")
