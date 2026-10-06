"""Import the reviewed ship perk source sheet through ImageMagick's TGA exporter."""
import argparse,json,shutil,subprocess
from pathlib import Path
from ShipOperatingPerks import base_name,ICON_NAMES
ROOT=Path(__file__).resolve().parents[1]
X=[(1,147),(151,295),(299,444),(448,593),(596,742),(745,897),(899,1048)]
Y=[(0,139),(141,280),(282,414),(416,554),(557,699),(701,840),(843,981),(984,1128),(1131,1294),(1297,1459)]
def main():
 p=argparse.ArgumentParser(description=__doc__);p.add_argument("source",type=Path);args=p.parse_args()
 target=ROOT/"design/art/ship-operating-perks.png";target.parent.mkdir(parents=True,exist_ok=True)
 if args.source.resolve()!=target.resolve():shutil.copyfile(args.source,target)
 source=json.loads((ROOT/"design/space/space-balance.json").read_text());groups={}
 for row in source["perks"]:groups.setdefault(base_name(row),[]).append(row)
 for index,rows in enumerate(groups.values()):
  x0,x1=X[index%7];y0,y1=Y[index//7]
  ranks=[1] if rows[0]["kind"]=="Trait" else [row["rank"] for row in rows]
  for rank in ranks:
   icon="ife_s"+ICON_NAMES[index][:9]+("" if rows[0]["kind"]=="Trait" else str(rank))
   out=ROOT/"SWLOR_Haks/sw_ability"/(icon+".tga")
   command=["magick",str(target),"-crop",f"{x1-x0}x{y1-y0}+{x0}+{y0}","+repage","-resize","30x30","-background","#061121","-gravity","center","-extent","32x32","-modulate",str(100+(rank-1)*8),"-alpha","set","-channel","A","-evaluate","set","100%","+channel","-flip","-orient","BottomLeft","-type","TrueColorAlpha","-define","tga:bits-per-pixel=32","-compress","None",str(out)]
   subprocess.run(command,check=True,capture_output=True)
 print("Imported 130 illustrated ship feat icons.")
if __name__=="__main__":main()
