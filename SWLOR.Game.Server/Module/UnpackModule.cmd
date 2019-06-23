IF NOT EXIST "./are" mkdir are
IF NOT EXIST "./dlg" mkdir dlg
IF NOT EXIST "./fac" mkdir fac
IF NOT EXIST "./gic" mkdir gic
IF NOT EXIST "./git" mkdir git
IF NOT EXIST "./ifo" mkdir ifo
IF NOT EXIST "./itp" mkdir itp
IF NOT EXIST "./jrl" mkdir jrl
IF NOT EXIST "./ncs" mkdir ncs
IF NOT EXIST "./nss" mkdir nss
IF NOT EXIST "./utc" mkdir utc
IF NOT EXIST "./utd" mkdir utd
IF NOT EXIST "./ute" mkdir ute
IF NOT EXIST "./uti" mkdir uti
IF NOT EXIST "./utm" mkdir utm
IF NOT EXIST "./utp" mkdir utp
IF NOT EXIST "./uts" mkdir uts
IF NOT EXIST "./utt" mkdir utt
IF NOT EXIST "./utw" mkdir utw


del .\are\*.* /s /q
del .\dlg\*.* /s /q
del .\fac\*.* /s /q
del .\gic\*.* /s /q
del .\git\*.* /s /q
del .\ifo\*.* /s /q
del .\itp\*.* /s /q
del .\jrl\*.* /s /q
del .\ncs\*.* /s /q
del .\nss\*.* /s /q
del .\utc\*.* /s /q
del .\utd\*.* /s /q
del .\ute\*.* /s /q
del .\uti\*.* /s /q
del .\utm\*.* /s /q
del .\utp\*.* /s /q
del .\uts\*.* /s /q
del .\utt\*.* /s /q
del .\utw\*.* /s /q


nwn_erf -f "Star Wars LOR.mod" -x


for %%v in (./*.are) do nwn_gff -i %%v -o %%v.json -p
for %%v in (./*.dlg) do nwn_gff -i %%v -o %%v.json -p
for %%v in (./*.fac) do nwn_gff -i %%v -o %%v.json -p
for %%v in (./*.gic) do nwn_gff -i %%v -o %%v.json -p
for %%v in (./*.git) do nwn_gff -i %%v -o %%v.json -p
for %%v in (./*.ifo) do nwn_gff -i %%v -o %%v.json -p
for %%v in (./*.itp) do nwn_gff -i %%v -o %%v.json -p
for %%v in (./*.jrl) do nwn_gff -i %%v -o %%v.json -p
for %%v in (./*.utc) do nwn_gff -i %%v -o %%v.json -p
for %%v in (./*.utd) do nwn_gff -i %%v -o %%v.json -p
for %%v in (./*.ute) do nwn_gff -i %%v -o %%v.json -p
for %%v in (./*.uti) do nwn_gff -i %%v -o %%v.json -p
for %%v in (./*.utm) do nwn_gff -i %%v -o %%v.json -p
for %%v in (./*.utp) do nwn_gff -i %%v -o %%v.json -p
for %%v in (./*.uts) do nwn_gff -i %%v -o %%v.json -p
for %%v in (./*.utt) do nwn_gff -i %%v -o %%v.json -p
for %%v in (./*.utw) do nwn_gff -i %%v -o %%v.json -p

move *.are.json "./are/"
move *.dlg.json "./dlg/"
move *.fac.json "./fac/"
move *.gic.json "./gic/"
move *.git.json "./git/"
move *.ifo.json "./ifo/"
move *.itp.json "./itp/"
move *.jrl.json "./jrl/"
move *.ncs "./ncs/"
move *.nss "./nss/"
move *.utc.json "./utc/"
move *.utd.json "./utd/"
move *.ute.json "./ute/"
move *.uti.json "./uti/"
move *.utm.json "./utm/"
move *.utp.json "./utp/"
move *.uts.json "./uts/"
move *.utt.json "./utt/"
move *.utw.json "./utw/"


del *.are
del *.dlg
del *.fac
del *.gic
del *.git
del *.ifo
del *.itp
del *.jrl
del *.utc
del *.utd
del *.ute
del *.uti
del *.utm
del *.utp
del *.uts
del *.utt
del *.utw