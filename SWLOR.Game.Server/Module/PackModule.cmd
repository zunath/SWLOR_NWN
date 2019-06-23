IF NOT EXIST "./packing" mkdir packing

del .\packing\*.* /s /q

for %%v in (./are/*.are.json) do nwn_gff -i ./are/%%v -o ./packing/%%v -k gff
for %%v in (./dlg/*.dlg.json) do nwn_gff -i ./dlg/%%v -o ./packing/%%v -k gff
for %%v in (./fac/*.fac.json) do nwn_gff -i ./fac/%%v -o ./packing/%%v -k gff
for %%v in (./gic/*.gic.json) do nwn_gff -i ./gic/%%v -o ./packing/%%v -k gff
for %%v in (./git/*.git.json) do nwn_gff -i ./git/%%v -o ./packing/%%v -k gff
for %%v in (./ifo/*.ifo.json) do nwn_gff -i ./ifo/%%v -o ./packing/%%v -k gff
for %%v in (./itp/*.itp.json) do nwn_gff -i ./itp/%%v -o ./packing/%%v -k gff
for %%v in (./jrl/*.jrl.json) do nwn_gff -i ./jrl/%%v -o ./packing/%%v -k gff
for %%v in (./utc/*.utc.json) do nwn_gff -i ./utc/%%v -o ./packing/%%v -k gff
for %%v in (./utd/*.utd.json) do nwn_gff -i ./utd/%%v -o ./packing/%%v -k gff
for %%v in (./ute/*.ute.json) do nwn_gff -i ./ute/%%v -o ./packing/%%v -k gff
for %%v in (./uti/*.uti.json) do nwn_gff -i ./uti/%%v -o ./packing/%%v -k gff
for %%v in (./utm/*.utm.json) do nwn_gff -i ./utm/%%v -o ./packing/%%v -k gff
for %%v in (./utp/*.utp.json) do nwn_gff -i ./utp/%%v -o ./packing/%%v -k gff
for %%v in (./uts/*.uts.json) do nwn_gff -i ./uts/%%v -o ./packing/%%v -k gff
for %%v in (./utt/*.utt.json) do nwn_gff -i ./utt/%%v -o ./packing/%%v -k gff
for %%v in (./utw/*.utw.json) do nwn_gff -i ./utw/%%v -o ./packing/%%v -k gff

xcopy ".\ncs\*.ncs" ".\packing\" /E /Y /C /i
xcopy ".\nss\*.nss" ".\packing\" /E /Y /C /i

cd packing
forfiles /S /M *.json /C "cmd /c rename @file @fname"
cd ../

nwn_erf -e MOD -c "./packing/" -f "Star Wars LOR.mod"

del .\packing\*.* /s /q
RMDIR ".\packing" /S /Q