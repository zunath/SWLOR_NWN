import json
from pathlib import Path
import shutil

source = Path('/src')
hak = source / 'SWLOR_Haks/fixture'
hak.mkdir(parents=True)
(hak / 'fixture.set').write_bytes(b'[GENERAL]\r\nName=Fixture\r\n')
tlk = source / 'SWLOR_Haks/sw_tlk'
tlk.mkdir()
shutil.copyfile('/repo/SWLOR_Haks/sw_tlk/sw_tlk.tlk', tlk / 'sw_tlk.tlk')
Path('/work/hakbuilder.json').write_text(json.dumps({
    'TlkPath': str(tlk / 'sw_tlk.tlk'), 'OutputPath': '/artifacts/',
    'EnableChecksumChecking': True,
    'HakList': [{'Name': 'fixture', 'Path': str(hak), 'CompileModels': False}]
}))
module = source / 'Module'
module.mkdir()
# Match ModulePacker's resource directories, with one real GFF JSON conversion.
for folder in ('are','bic','dlg','fac','git','gic','ifo','itp','jrl','ncs','nss','utc','utd','ute','uti','utm','utp','uts','utt','utw'):
    (module / folder).mkdir()
shutil.copyfile('/repo/Module/ifo/module.ifo.json', module / 'ifo/module.ifo.json')
