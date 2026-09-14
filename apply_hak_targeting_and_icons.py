import importlib.util, pathlib
root = pathlib.Path("C:/Projects/SWLOR_combat-upgrade")
spec = importlib.util.spec_from_file_location("gen", root/"tools"/"GenerateWeaponArchetypeImplementation.py")
gen = importlib.util.module_from_spec(spec); spec.loader.exec_module(gen)

# --- feat.2da: queued / self-origin-area lightsaber actives => TARGETSELF=1, HostileFeat cleared ---
feat_path = root/"SWLOR_Haks"/"sw_2da"/"feat.2da"
lines = feat_path.read_text().splitlines()
_, hdr = gen.get_2da_header(lines)
ts = hdr.index("TARGETSELF")+1; hf = hdr.index("HostileFeat")+1
# ForceSheath1-4, SunderingSweep1-3, GuardiansChallenge1-2
targets = {2773,2776,2780,2782, 2792,2793,2794, 2103,2299}
changed = 0
for i, line in enumerate(lines):
    t = line.split()
    if t and t[0].isdigit() and int(t[0]) in targets:
        if len(t) != len(hdr)+1:
            raise SystemExit(f"feat.2da row width mismatch row {t[0]}")
        t[ts] = "1"; t[hf] = "****"
        nl = gen.format_2da_row(t, gen.FEAT_COLUMN_WIDTHS)
        if nl != lines[i]:
            lines[i] = nl; changed += 1
feat_path.write_text("\n".join(lines)+"\n")
print("feat.2da rows updated:", changed)

# --- effecticons.2da: add stance effect-icon rows reusing the ability icons ---
ei_path = root/"SWLOR_Haks"/"sw_2da"/"effecticons.2da"
lines = ei_path.read_text().splitlines()
import re
# verify the ability icons we point at actually exist at this commit
ab = root/"SWLOR_Haks"/"sw_ability"
for tga in ("ife_imbuestance.tga", "ife_immovstance.tga"):
    if not (ab/tga).exists():
        raise SystemExit(f"MISSING ability icon {tga} at this commit - aborting effecticons edit")
# formatting reference: prefer an existing stance row, else fall back to any data row
ref = None
for want in ("438", "306", "152"):
    hit = [l for l in lines if l.split() and l.split()[0] == want]
    if hit:
        ref = hit[0]; break
if ref is None:
    ref = [l for l in lines[3:] if l.split() and l.split()[0].isdigit()][0]
m = list(re.finditer(r"\S+", ref))
cLabel, cIcon, cStr = m[1].start(), m[2].start(), m[3].start()
def fmt(rid, label, icon, strref):
    s = str(rid)
    s = s.ljust(cLabel)+label
    s = s.ljust(cIcon)+icon
    s = s.ljust(cStr)+str(strref)
    return s.rstrip()
existing = {int(l.split()[0]) for l in lines[3:] if l.split() and l.split()[0].isdigit()}
add = 0
if 439 not in existing:
    lines.append(fmt(439, "ImbuementStance", "ife_imbuestance", 16783262)); add += 1
if 440 not in existing:
    lines.append(fmt(440, "ImmovableStance", "ife_immovstance", 16783280)); add += 1
ei_path.write_text("\n".join(lines)+"\n")
print("effecticons.2da rows added:", add)
