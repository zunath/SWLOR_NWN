"""Synchronize the five authored crafting perk trees without recompressing untouched XLSX members."""
from pathlib import Path
import copy, io, re, struct, zipfile, hashlib, xml.etree.ElementTree as ET
from xml.sax.saxutils import escape

ROOT = Path(__file__).resolve().parents[1]
WORKBOOK = ROOT / 'design/bible/SWLOR Design Bible - Combat Upgrade.xlsx'
NS = {'s':'http://schemas.openxmlformats.org/spreadsheetml/2006/main'}

def raw_members(data):
    end = data.rfind(b'PK\x05\x06')
    assert end >= 0 and end + 22 + struct.unpack_from('<H', data, end + 20)[0] == len(data)
    disk, cd_disk, disk_count, count, size, offset = struct.unpack_from('<4H2I', data, end + 4)
    assert disk == cd_disk == 0 and disk_count == count and offset + size == end, 'Only ordinary single-disk ZIP archives are supported'
    records, cursor = [], offset
    for _ in range(count):
        assert data[cursor:cursor+4] == b'PK\x01\x02'
        name_len, extra_len, comment_len = struct.unpack_from('<3H', data, cursor+28)
        length = 46 + name_len + extra_len + comment_len
        header = data[cursor:cursor+length]
        local = struct.unpack_from('<I', header, 42)[0]
        records.append((header, local)); cursor += length
    assert cursor == end
    boundaries = sorted([local for _,local in records] + [offset])
    segments = {local:data[local:boundaries[i+1]] for i,local in enumerate(boundaries[:-1])}
    return records, segments, data[end:], data[:boundaries[0]]

def replace_members(data, replacements):
    original = zipfile.ZipFile(io.BytesIO(data))
    records, segments, ending, prefix = raw_members(data)
    assert len(records) == len(original.infolist())
    out = bytearray(prefix); central = []
    for info,(header,local) in zip(original.infolist(),records):
        new_offset = len(out)
        if info.filename in replacements:
            buffer=io.BytesIO()
            with zipfile.ZipFile(buffer,'w') as single:
                single.writestr(copy.copy(info), replacements[info.filename])
            replacement_records,replacement_segments,_,_ = raw_members(buffer.getvalue())
            header,replacement_offset = replacement_records[0]
            out.extend(replacement_segments[replacement_offset])
        else:
            # Copy the complete original local record and compressed bytes verbatim.
            out.extend(segments[local])
        header=bytearray(header); struct.pack_into('<I',header,42,new_offset); central.append(header)
    cd_offset=len(out)
    for header in central:out.extend(header)
    cd_size=len(out)-cd_offset
    ending=bytearray(ending);struct.pack_into('<2I',ending,12,cd_size,cd_offset);out.extend(ending)
    result=bytes(out)
    rebuilt=zipfile.ZipFile(io.BytesIO(result));new_records,new_segments,_,_=raw_members(result)
    for info,(_,old_offset),(_,new_offset) in zip(original.infolist(),records,new_records):
        expected=replacements.get(info.filename, original.read(info.filename))
        assert rebuilt.read(info.filename)==expected, info.filename
        if info.filename not in replacements:
            assert segments[old_offset]==new_segments[new_offset], 'Untouched raw member changed: '+info.filename
    assert rebuilt.testzip() is None
    return result

def col(n):
    result=''
    while n:n,r=divmod(n-1,26);result=chr(65+r)+result
    return result

def main():
    original=WORKBOOK.read_bytes();z=zipfile.ZipFile(io.BytesIO(original))
    shared=[]
    if 'xl/sharedStrings.xml' in z.namelist():
        shared=[''.join(si.itertext()) for si in ET.fromstring(z.read('xl/sharedStrings.xml'))]
    def text(c):
        if c.get('t')=='s':return shared[int(c.find('s:v',NS).text)]
        return ''.join(c.find('s:is',NS).itertext()) if c.find('s:is',NS) is not None else (c.findtext('s:v','',NS))
    replacements={}
    for skill,sheet in [('Smithery',31),('Engineering',32),('Fabrication',33),('Agriculture',35),('Espionage',30)]:
        source=(ROOT/f'SWLOR.Game.Server/Feature/PerkDefinition/{skill}CraftingPerkDefinition.cs').read_text()
        entries=[]
        for section in re.split(r'builder\.Create\(',source)[1:]:
            name=re.search(r'\.Name\("([^"]+)"\)',section).group(1)
            levels=section.split('.AddPerkLevel()')[1:]
            lines=re.search(r'\.RequirementAnyCompletedLine\(([^)]+)\)',section)
            for i,level in enumerate(levels):
                description=re.search(r'\.Description\("([^"]+)"\)',level).group(1)
                price=int(re.search(r'\.Price\((\d+)\)',level).group(1))
                rank=int(re.search(r'\.RequirementSkill\(SkillType\.\w+, (\d+)\)',level).group(1))
                gate=f'{skill} {rank}'
                additional='Complete any one crafting line at rank II.' if lines else '-'
                entries.append((name+(' '+['I','II'][i] if len(levels)>1 else ''),description,price,gate,additional))
        assert len(entries)==7 and sum(e[2] for e in entries)==20
        member=f'xl/worksheets/sheet{sheet}.xml';xml=z.read(member).decode('utf-8');tree=ET.fromstring(xml)
        header=next(row for row in tree.findall('s:sheetData/s:row',NS) if row.get('r')=='8')
        headers={re.sub(r'\d','',c.get('r')):text(c).strip() for c in header if text(c).strip()}
        authored=[]
        for row in tree.findall('s:sheetData/s:row',NS):
            if any(text(c).strip() for c in row):authored.append(int(row.get('r')))
        existing_start = next((int(row.get("r")) for row in tree.findall("s:sheetData/s:row",NS) if any(c.get("r").startswith("C") and text(c)==entries[0][0] for c in row)), None)
        start=existing_start or max(authored)+2;subtotal=start+7
        for i,(name,desc,price,gate,additional) in enumerate(entries):
            index=start+i
            existing=re.search(r'<row\b[^>]*\br="'+str(index)+r'"[^>]*>.*?</row>',xml,re.S)
            assert existing, f'{skill} missing styled row {index}'
            cells=[]
            values={'Style':'Saboteur' if skill=='Espionage' else 'Crafting','SP Price':price,'Perk Name':name,'Skill Reqs.':gate,'Char. Type':'Standard' if skill=='Espionage' else 'All','Type':'Trait','Description':desc,'Primary Stat':'None','Secondary Stat':'None','Scaling Source':'Crafting Stats (skill scoped)','Dev Status':'Implemented','Additional Requirements':additional,'Notes':additional}
            for column,label in headers.items():
                value=values.get(label,'-');style=re.search(r'<c\b[^>]*\br="'+column+str(index)+r'"[^>]*\bs="(\d+)"',existing.group())
                style_attr=' s="'+style.group(1)+'"' if style else ''
                cells.append(f'<c r="{column}{index}"{style_attr}><v>{value}</v></c>' if isinstance(value,int) else f'<c r="{column}{index}"{style_attr} t="inlineStr"><is><t>{escape(value)}</t></is></c>')
            xml=xml[:existing.start()]+f'<row r="{index}">'+''.join(cells)+'</row>'+xml[existing.end():]
        existing=re.search(r'<row\b[^>]*\br="'+str(subtotal)+r'"[^>]*>.*?</row>',xml,re.S);assert existing
        xml=xml[:existing.start()]+f'<row r="{subtotal}"><c r="A{subtotal}" t="inlineStr"><is><t>Crafting subtotal</t></is></c><c r="B{subtotal}"><f>SUM(B{start}:B{subtotal-1})</f><v>20</v></c></row>'+xml[existing.end():]
        total=re.search(r'<c\b[^>]*\br="D4"[^>]*>.*?</c>',xml,re.S);assert total
        old=total.group();cached=int(float(re.search(r'<v>([^<]+)</v>',old).group(1)))
        formula=re.search(r'<f[^>]*>(.*?)</f>',old,re.S)
        # D4 can be a hand-entered override; retain that cell's original mode.
        new=old
        if not existing_start:
            new=re.sub(r'<v>[^<]+</v>',f'<v>{cached+20}</v>',old)
            if formula:new=re.sub(r'<f[^>]*>.*?</f>',f'<f>({formula.group(1)})+B{subtotal}</f>',new,flags=re.S)
        xml=xml[:total.start()]+new+xml[total.end():]
        # Existing profession prices were exported as numeric strings. Normalize these dependencies
        # so the total SP formula actually includes the retained Droidcraft and research investments.
        if skill in ('Engineering','Fabrication'):
            first,last,old_subtotal=(9,13,14) if skill=='Engineering' else (9,17,18)
            total_price=0
            for row_number in range(first,last+1):
                cell=re.search(r'<c\b[^>]*\br="B'+str(row_number)+r'"[^>]*>.*?</c>',xml,re.S);assert cell
                parsed=ET.fromstring('<wrapper xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">'+cell.group()+'</wrapper>')[0];price=int(float(text(parsed)));total_price+=price
                opening=re.sub(r' t="[^"]+"','',cell.group().split('>')[0])+'>'
                xml=xml[:cell.start()]+opening+f'<v>{price}</v></c>'+xml[cell.end():]
            for reference,cache in [(f'B{old_subtotal}',total_price),('D4',total_price+20)]:
                cell=re.search(r'<c\b[^>]*\br="'+reference+r'"[^>]*>.*?</c>',xml,re.S);assert cell
                changed=re.sub(r'<v>[^<]+</v>',f'<v>{cache}</v>',cell.group())
                xml=xml[:cell.start()]+changed+xml[cell.end():]
        ET.fromstring(xml);replacements[member]=xml.encode('utf-8')

        print(skill, start, subtotal, '7 ranks, 20 SP')
    # A:L is the historical chance model. Add a separate, cached example of the manual evaluator.
    calc_member='xl/worksheets/sheet50.xml';calc=z.read(calc_member).decode('utf-8')
    engineering=ET.fromstring(replacements['xl/worksheets/sheet32.xml'])
    description_style=engineering.find(".//s:c[@r='G16']",NS).get('s','0')
    values={
        'N1':'Manual crafting (rules v2)', 'O1':'Runtime reference: CraftWorkEvaluator.cs. Columns A:L retain the historical chance model.',
        'N2':'Example and scope', 'O2':'Unconditioned gains before action efficiency, profile, conditions, preparations and perks. Live previews also clamp gains to remaining targets.',
        'N6':'Skill rank', 'O6':50, 'N7':'Recipe level', 'O7':50, 'N8':'Craftsmanship', 'O8':30, 'N9':'Control', 'O9':29, 'N10':'Equipment CP', 'O10':37,
        'N11':'Profile rules', 'O11':'Sturdy: synthesis durability x0.75, touch quality and target x0.9. Delicate: touch quality and target x1.1, Rapid spends 5 extra durability. Calibrated: alternate successful work for x1.2 gain; progress target x1.1.',
        'N12':'Basic progress (unconditioned)', 'N13':'Basic quality (unconditioned)', 'N14':'Maximum CP', 'N15':'Basic Touch during Fine',
        'N16':'Conditions', 'O16':'Workable: +25% synthesis progress. Fine: +50% touch quality. Economical: 25% CP discount. Reinforced: 50% work durability discount. Combined discounts cap at 50%, rounded up.',
    }
    formulas={
        'O12':('MAX(0,INT((10+IF(O6>=20,21,0)+O8*0.65)*MAX(0,1+0.05*(O6-O7))))',50),
        'O13':('MAX(0,INT((10+IF(O6>=40,70,0)+O9*0.75)*IF(O6<O7,MAX(0,1+0.05*(O6-O7)),1)))',101),
        'O14':('MAX(0,INT(O10+O6*0.75)+IF(O6>=25,31,0))',105), 'O15':('INT(O13*1.5)',151),
    }
    for reference in list(values)+list(formulas):
        index=int(re.sub(r'\D','',reference));row=re.search(r'<row\b[^>]*\br="'+str(index)+r'"[^>]*>.*?</row>',calc,re.S);assert row
        current=re.search(r'<c\b[^>]*\br="'+reference+r'"[^>]*(?:/>|>.*?</c>)',row.group(),re.S)
        style=f' s="{description_style}"'
        if reference in formulas:
            formula,cached=formulas[reference];cell=f'<c r="{reference}"{style}><f>{escape(formula)}</f><v>{cached}</v></c>'
        elif isinstance(values[reference],int):cell=f'<c r="{reference}"{style}><v>{values[reference]}</v></c>'
        else:cell=f'<c r="{reference}"{style} t="inlineStr"><is><t>{escape(values[reference])}</t></is></c>'
        changed=row.group()[:current.start()]+cell+row.group()[current.end():] if current else row.group().replace('</row>',cell+'</row>')
        calc=calc[:row.start()]+changed+calc[row.end():]
    replacements[calc_member]=calc.encode('utf-8')
    result=replace_members(original,replacements)
    temporary=WORKBOOK.with_suffix('.xlsx.tmp');temporary.write_bytes(result);temporary.replace(WORKBOOK)
    print(f'Updated {len(replacements)} worksheets; every other compressed ZIP member is byte-for-byte identical.')

if __name__=='__main__':main()
