"""Run Python unittest discovery; Blender 4.0 additionally covers clipping and icon pixels."""
import copy
import hashlib
import importlib.util
import json
import math
import os
import re
from pathlib import Path
import struct
import sys
import tempfile
from types import SimpleNamespace
import unittest
import unittest.mock

ROOT = Path(__file__).resolve().parents[2]
spec = importlib.util.spec_from_file_location('trellis_export', ROOT / 'tools/TrellisWeaponExport.py')
exporter = importlib.util.module_from_spec(spec)
spec.loader.exec_module(exporter)
try:
    import numpy as np
    exporter.np = np
except ImportError:
    np = None
try:
    exporter.load_blender()
    HAS_BLENDER = True
except ImportError:
    HAS_BLENDER = False


def make_glb(document, binary=b'1234'):
    raw = json.dumps(document).encode(); raw += b' ' * (-len(raw) % 4)
    chunks = struct.pack('<II', len(raw), 0x4E4F534A) + raw
    chunks += struct.pack('<II', len(binary), 0x004E4942) + binary
    return struct.pack('<III', 0x46546C67, 2, len(chunks)+12) + chunks


class TrellisWeaponExportTests(unittest.TestCase):
    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory()
        self.addCleanup(self.temporary.cleanup)
        self.root = Path(self.temporary.name)
        table = self.root / 'SWLOR_Haks/sw_2da'; table.mkdir(parents=True)
        (table / 'baseitems.2da').write_text('2DA V2.0\n\nItemClass ModelType InvSlotWidth InvSlotHeight\n12 wdbsw 2 2 5\n56 AShLw 0 2 3\n59 wthsh 0 1 1\n')
        self.source = self.root / 'input.glb'
        self.document = {'asset': {'version': '2.0'}, 'buffers': [{'byteLength': 4}],
                         'materials': [{'pbrMetallicRoughness': {}}], 'meshes': [{'primitives': [
                             {'attributes': {'POSITION': 0, 'NORMAL': 1, 'TEXCOORD_0': 2}, 'material': 0}]}]}
        self.source.write_bytes(make_glb(self.document))
        self.asset = {'asset': 'field_twin', 'source': 'input.glb', 'sha256': hashlib.sha256(self.source.read_bytes()).hexdigest(),
                      'base_item': 12, 'model': 'wdbsw_m_011', 'texture': 'field_twin', 'texture_size': 1024,
                      'scale': .5, 'orientation': [[1,0,0],[0,1,0],[0,0,1]],
                      'source_grip': [0,0,0], 'target_grip': [0,0,0], 'icon_size': [64,256],
                      'attachment': {'review_notes': 'Explicit native reference axes', 'axis_pairs': [
                          {'source': [0,1,0], 'target': [0,1,0]}, {'source': [1,0,0], 'target': [1,0,0]}]}}
        self.asset['parts'] = [{'model':'wdbsw_b_011','clip_max_y':-.1},
                               {'model':'wdbsw_m_011','clip_min_y':-.1,'clip_max_y':.1},
                               {'model':'wdbsw_t_011','clip_min_y':.1}]
        self.manifest = self.root / 'manifest.json'

    def validate(self, asset=None, **extra):
        config = {'schema_version': 1, 'assets': [asset or copy.deepcopy(self.asset)], **extra}
        exporter.validate_manifest(config, self.root, self.manifest, 'export')
        return config

    def test_native_canvas_is_distinct_from_logical_art_area(self):
        config = self.validate()
        self.assertEqual(config['assets'][0]['_icon_art_size'], [64,160])
        self.assertEqual(config['assets'][0]['icon_size'], [64,256])
        for wrong in ([64,160], [32,128]):
            asset = copy.deepcopy(self.asset); asset['icon_size'] = wrong
            with self.subTest(size=wrong), self.assertRaises(ValueError): self.validate(asset)

    def test_source_hash_and_resource_family_are_checked(self):
        for key, value in [('sha256','0'*64), ('model','wswgs_m_011'), ('model','../wdbsw_m_011'), ('scale',True)]:
            asset = copy.deepcopy(self.asset); asset[key] = value
            with self.subTest(key=key), self.assertRaises(ValueError): self.validate(asset)

    def test_empty_parts_reject_composite_slots_even_when_unoccupied(self):
        for role in ('b', 'm', 't'):
            with self.subTest(role=role), self.assertRaisesRegex(ValueError, 'Composite weapons require real'):
                self.validate(empty_parts=[{'model':f'wdbsw_{role}_021','base_item':12,'icon_size':[64,256]}])

    def test_simple_empty_parts_cannot_overwrite_real_geometry(self):
        asset = copy.deepcopy(self.asset)
        asset.update(base_item=59, model='wthsh_011', icon_size=[32,32]); asset.pop('parts')
        with self.assertRaisesRegex(ValueError,'occupied'):
            self.validate(asset, empty_parts=[{'model':'wthsh_011','base_item':59,'icon_size':[32,32]}])

    def test_empty_parts_require_the_baseitems_logical_icon_area(self):
        for size in ([16,16],[32,16],[16,32]):
            with self.subTest(size=size),self.assertRaisesRegex(ValueError,'logical inventory area'):
                self.validate(empty_parts=[{'model':'wthsh_012','base_item':59,'icon_size':size}])
        for size in ([32,32],[64,64]):self.validate(empty_parts=[{'model':'wthsh_012','base_item':59,'icon_size':size}])

    def test_large_shield_uses_one_native_model_and_padded_inventory_canvas(self):
        asset=copy.deepcopy(self.asset)
        asset.update(base_item=56,model='ashlw_255',icon_size=[64,128]);asset.pop('parts')
        asset['attachment_dummies']=[{'name':name,'position':[0,0,0],'quaternion':[0,0,0,1]}
                                     for name in ('imp_shield_1','imp_shield_2')]
        checked=self.validate(asset)['assets'][0]
        self.assertEqual(checked['_item_class'],'ashlw')
        self.assertEqual(checked['_model_type'],0)
        self.assertEqual(checked['_icon_art_size'],[64,96])
        for key,value in [('model','ashlw_m_255'),('model','wthsh_255'),('icon_size',[64,64])]:
            invalid=copy.deepcopy(asset);invalid[key]=value
            with self.subTest(key=key,value=value),self.assertRaises(ValueError):self.validate(invalid)

    def test_shield_export_requires_both_impact_hooks_even_with_aliases(self):
        for aliases in ([], ['ashlw_032']):
            asset=copy.deepcopy(self.asset)
            asset.update(base_item=56,model='ashlw_031',icon_size=[64,128],aliases=aliases)
            asset.pop('parts')
            with self.subTest(aliases=aliases),self.assertRaisesRegex(ValueError,'both native shield impact'):
                self.validate(asset)
        with self.assertRaisesRegex(ValueError,'both native shield impact'):
            exporter.attachment_dummy_nodes({'_item_class':'ashlw','_model_type':0},'ashlw_031')

    def test_shield_inspection_accepts_minimal_source_without_export_fields(self):
        asset={key:self.asset[key] for key in ('asset','source','sha256')}
        asset['base_item']=56
        config={'schema_version':1,'assets':[asset]}
        exporter.validate_manifest(config,self.root,self.manifest,'inspect')
        self.assertEqual(asset['_item_class'],'ashlw')
        self.assertNotIn('attachment_dummies',asset)
        self.assertNotIn('icon_size',asset)

    def test_shield_rejects_other_power_of_two_icon_canvases(self):
        asset=copy.deepcopy(self.asset)
        asset.update(base_item=56,model='ashlw_031',icon_size=[64,128]);asset.pop('parts')
        asset['attachment_dummies']=[{'name':name,'position':[0,0,0],'quaternion':[0,0,0,1]}
                                     for name in ('imp_shield_1','imp_shield_2')]
        self.validate(asset)
        for size in ([128,128], [64,256], [128,256]):
            invalid=copy.deepcopy(asset);invalid['icon_size']=size
            with self.subTest(size=size),self.assertRaisesRegex(ValueError,r'exactly \[64, 128\]'):
                self.validate(invalid)

    def test_weapon_exports_keep_optional_hooks_and_generic_icon_canvas_rules(self):
        composite=copy.deepcopy(self.asset);composite['icon_size']=[128,256]
        simple=copy.deepcopy(self.asset)
        simple.update(base_item=59,model='wthsh_011',icon_size=[128,128]);simple.pop('parts')
        for asset in (simple,composite):
            with self.subTest(model=asset['model']):
                checked=self.validate(asset)['assets'][0]
                self.assertEqual(exporter.attachment_dummy_nodes(checked,checked['model']),'')
                self.assertEqual(checked['icon_size'],asset['icon_size'])

    def test_shields_reject_every_partial_surface_selection(self):
        for selection in ({'clip_min_y':0}, {'clip_max_y':0}, {'plane_y':0},
                          {'clip_side':'below'}, {'plane_y':0,'clip_side':'below'}):
            asset=copy.deepcopy(self.asset)
            asset.update(base_item=56,model='ashlw_031',icon_size=[64,128])
            asset['parts']=[{'model':'ashlw_031',**selection}]
            with self.subTest(selection=selection),self.assertRaisesRegex(ValueError,'complete surface'):
                self.validate(asset)

    def test_shields_reject_empty_resources_even_when_slot_is_unoccupied(self):
        with self.assertRaisesRegex(ValueError,'Shields require real geometry'):
            self.validate(empty_parts=[{'model':'ashlw_032','base_item':56,'icon_size':[64,128]}])

    def test_shield_support_does_not_admit_other_armor_or_composite_shields(self):
        table=self.root/'SWLOR_Haks/sw_2da/baseitems.2da'
        table.write_text(table.read_text()+'60 AShLw 2 2 3\n61 AHe 0 2 2\n62 AShLw 1 2 3\n')
        for base_item in (60,61,62):
            with self.subTest(base_item=base_item),self.assertRaisesRegex(ValueError,'baseitem'):
                exporter.base_item_row(self.root,base_item)

    def test_shield_impact_hooks_validate_family_identity_and_transform(self):
        asset=copy.deepcopy(self.asset)
        asset.update(base_item=56,model='ashlw_031',icon_size=[64,128]);asset.pop('parts')
        hooks=[{'name':name,'position':[0,0,0],'quaternion':[0,0,0,1]} for name in ('imp_shield_1','imp_shield_2')]
        asset['attachment_dummies']=hooks;self.validate(asset)
        invalid=copy.deepcopy(self.asset);invalid['attachment_dummies']=hooks
        with self.assertRaisesRegex(ValueError,'simple large shield'):self.validate(invalid)
        for key,value in [('name','unknown'),('name','imp_shield_2'),('position',[0,float('nan'),0]),
                          ('quaternion',[0,0,0,0]),('quaternion',[0,0,0,2]),('quaternion',[0,False,0,1])]:
            invalid=copy.deepcopy(asset);invalid['attachment_dummies'][0][key]=value
            with self.subTest(key=key,value=value),self.assertRaises(ValueError):self.validate(invalid)
        for value in (None,[],hooks[:1]):
            invalid=copy.deepcopy(asset);invalid['attachment_dummies']=value
            with self.subTest(hooks=value),self.assertRaises(ValueError):self.validate(invalid)

    @unittest.skipUnless(HAS_BLENDER,'Run Blender 4.0 for shield dummy export')
    def test_shield_export_preserves_native_impact_hooks_and_alias_parents(self):
        exporter.bpy.ops.wm.read_factory_settings(use_empty=True)
        asset=copy.deepcopy(self.asset)
        asset.update(base_item=56,model='ashlw_031',icon_size=[64,128]);asset.pop('parts')
        asset['attachment_dummies']=[
            {'name':'imp_shield_1','position':[-.0883441,-.1016,.0776264],'quaternion':[0,.043619405,0,.99904823]},
            {'name':'imp_shield_2','position':[-.0698349,.0248255,-.052034],'quaternion':[0,-.130526,0,.9914449]}]
        config=self.validate(asset)['assets'][0]
        helper=exporter.load_module('shield_test_writer',ROOT/'tools/ImportBlasterModel.py')
        mesh=([(0,0,0),(0,1,0),(0,0,1)],[(0,0),(1,0),(0,1)],[(0,1,2)],[(1,0,0)]*3)
        material=exporter.bpy.data.materials.new('shield_mat')
        exporter.write_parts(config,self.root,helper,[mesh],['shield_mat'],{'shield_mat':material})
        text=(self.root/'ashlw_031.mdl').read_text()
        alias=exporter.alias_model_identifiers(text,'ashlw_031','ashlw_032')
        for native in asset['attachment_dummies']:
            block=re.search(r'node dummy '+native['name']+r'\n(.*?)endnode',text,re.S).group(1)
            self.assertIn('parent ashlw_031',block)
            position=[float(v) for v in re.search(r'position ([^\n]+)',block).group(1).split()]
            self.assertEqual(position,native['position'])
            orientation=[float(v) for v in re.search(r'orientation ([^\n]+)',block).group(1).split()]
            actual=exporter.Quaternion(exporter.Vector(orientation[:3]),orientation[3])
            q=native['quaternion'];expected=exporter.Quaternion([q[3],*q[:3]]).normalized()
            self.assertAlmostEqual(abs(actual.dot(expected)),1.,places=6)
            alias_block=re.search(r'node dummy '+native['name']+r'\n(.*?)endnode',alias,re.S).group(1)
            self.assertIn('parent ashlw_032',alias_block)
            self.assertEqual(block.replace('ashlw_031','ashlw_032'),alias_block)
        self.assertIn('bitmap shield_mat',text)

    @unittest.skipUnless(HAS_BLENDER and os.environ.get('CLEANMODELS'),'Set CLEANMODELS under Blender for native package validation')
    def test_native_shield_hooks_and_aliases_survive_compilation_and_detect_corruption(self):
        exporter.bpy.ops.wm.read_factory_settings(use_empty=True)
        asset=copy.deepcopy(self.asset)
        asset.update(base_item=56,model='ashlw_031',icon_size=[64,128]);asset.pop('parts')
        asset['attachment_dummies']=[
            {'name':'imp_shield_1','position':[-.0883441,-.1016,.0776264],'quaternion':[0,.043619405,0,.99904823]},
            {'name':'imp_shield_2','position':[-.0698349,.0248255,-.052034],'quaternion':[0,-.130526,0,.9914449]}]
        config=self.validate(asset)['assets'][0]
        resources=self.root/'resources';resources.mkdir()
        helper=exporter.load_module('native_shield_writer',ROOT/'tools/ImportBlasterModel.py')
        mesh=([(0,0,0),(0,1,0),(0,0,1)],[(0,0),(1,0),(0,1)],[(0,1,2)],[(1,0,0)]*3)
        material=exporter.bpy.data.materials.new('shield_mat')
        exporter.write_parts(config,resources,helper,[mesh],['shield_mat'],{'shield_mat':material})
        source=resources/'ashlw_031.mdl'
        (resources/'ashlw_032.mdl').write_text(exporter.alias_model_identifiers(source.read_text(),'ashlw_031','ashlw_032'))
        (resources/'shield_mat.mtr').write_text('renderhint NormalAndSpecMapped\ntexture0 shield_mat\ntexture1 shield_mat_n\n')
        compiler=exporter.load_module('native_shield_compiler',ROOT/'tools/CompileBlasterModels.py')
        compiled=self.root/'compiled'
        compiler.compile_models(os.environ['CLEANMODELS'],resources,compiled,[])
        for model in ('ashlw_031','ashlw_032'):
            report=exporter.validate_native(compiled/(model+'.mdl'),resources/(model+'.mdl'),resources)
            self.assertEqual({dummy['node'] for dummy in report['dummies']},{model,'imp_shield_1','imp_shield_2'})
            self.assertTrue(all(dummy['parent']==model for dummy in report['dummies'] if dummy['node']!=model))
        target=compiled/'ashlw_031.mdl';original=target.read_bytes()
        hook=original.index(b'imp_shield_1')-32
        key_offset,key_count,_=struct.unpack_from('<III',original,hook+84)
        data_offset=struct.unpack_from('<I',original,hook+96)[0]
        for controller in (8,20):
            for index in range(key_count):
                kind,_,_,start,columns=struct.unpack_from('<IHHHBx',original,12+key_offset+index*12)
                if kind!=controller:continue
                changed=bytearray(original);address=12+data_offset+start*4
                value=struct.unpack_from('<f',changed,address)[0];struct.pack_into('<f',changed,address,value+.01)
                target.write_bytes(changed)
                with self.subTest(controller=controller),self.assertRaisesRegex(ValueError,'dummy transform changed'):
                    exporter.validate_native(target,source,resources)
                break
            else:self.fail('Compiled shield hook has no transform controller '+str(controller))
        target.write_bytes(original)
        expected=source.read_text()
        # Test the declared hierarchy independently of transform values.
        source.write_text(re.sub(r'(node dummy imp_shield_1\n\s*parent )ashlw_031',r'\1imp_shield_2',expected))
        with self.assertRaisesRegex(ValueError,'parent changed'):exporter.validate_native(target,source,resources)
        source.write_text(expected.replace('node dummy imp_shield_1','node dummy missing_hook'))
        with self.assertRaisesRegex(ValueError,'node set changed'):exporter.validate_native(target,source,resources)

    def test_multipart_aliases_are_validated_individually(self):
        asset = copy.deepcopy(self.asset)
        asset['parts'] = [{'model':'wdbsw_m_011','clip_min_y':.2,'clip_max_y':.4,'aliases':['wdbsw_m_021']},
                          {'model':'wdbsw_b_011','plane_y':.2,'clip_side':'below'},
                          {'model':'wdbsw_t_011','clip_min_y':.4}]
        self.validate(asset)
        asset['parts'][1]['aliases'] = ['wdbsw_m_021']
        with self.assertRaisesRegex(ValueError,'part role'): self.validate(asset)
        asset['parts'][1].pop('aliases')
        asset['parts'][0]['aliases'] = ['wdbsw_m_021','wdbsw_m_021']
        with self.assertRaisesRegex(ValueError,'Repeated'): self.validate(asset)

    def test_composite_exports_require_a_complete_assembly(self):
        for parts in (None, [{'model':'wdbsw_m_011'}], self.asset['parts'][:2]):
            asset=copy.deepcopy(self.asset)
            if parts is None:asset.pop('parts')
            else:asset['parts']=parts
            with self.subTest(parts=parts), self.assertRaises(ValueError):
                self.validate(asset)

    def test_explicit_assemblies_can_group_separate_exported_parts(self):
        assets=[]
        for role in ('b','m','t'):
            asset=copy.deepcopy(self.asset);asset.update(asset='part_'+role,model='wdbsw_'+role+'_011',texture='part_'+role)
            asset['parts']=[copy.deepcopy(next(p for p in self.asset['parts'] if p['model']==asset['model']))];assets.append(asset)
        assembly={'base_item':12,'bottom':'wdbsw_b_011','middle':'wdbsw_m_011','top':'wdbsw_t_011',
                  'icon_bounds':{'min':[-1,-1,-1],'max':[1,1,1]}}
        config={'schema_version':1,'assets':assets,'composite_assemblies':[assembly]}
        exporter.validate_manifest(copy.deepcopy(config),self.root,self.manifest,'export')
        checked=copy.deepcopy(config);exporter.validate_manifest(checked,self.root,self.manifest,'export')
        self.assertTrue(all(a['_shared_icon_bounds']==assembly['icon_bounds'] for a in checked['assets']))
        for change in ('missing_bounds','different_canvas','different_camera'):
            invalid=copy.deepcopy(config)
            if change=='missing_bounds':invalid['composite_assemblies'][0].pop('icon_bounds')
            elif change=='different_canvas':invalid['assets'][1]['icon_size']=[128,256]
            else:invalid['assets'][1]['icon_direction']=[1,0,0]
            with self.subTest(change=change), self.assertRaisesRegex(ValueError,'icon_bounds|settings must match'):
                exporter.validate_manifest(invalid,self.root,self.manifest,'export')
        duplicated=copy.deepcopy(config)
        for asset in duplicated['assets']:asset['parts']=[{'model':asset['model']}]
        with self.assertRaisesRegex(ValueError,'same source geometry selection'):
            exporter.validate_manifest(duplicated,self.root,self.manifest,'export')
        duplicated['assets'][1].update(source_grip=[0,1,0],target_grip=[0,.5,0])
        with self.assertRaisesRegex(ValueError,'same source geometry selection'):
            exporter.validate_manifest(duplicated,self.root,self.manifest,'export')
        for key,value in [('top','wdbsw_t_021'),('bottom','wdbsw_m_011')]:
            invalid=copy.deepcopy(config);invalid['composite_assemblies'][0][key]=value
            with self.subTest(key=key), self.assertRaisesRegex(ValueError,'requires exported'):
                exporter.validate_manifest(invalid,self.root,self.manifest,'export')

    def test_mixed_source_assemblies_reject_overlap_within_each_shared_source(self):
        other=self.root/'other.glb';other.write_bytes(make_glb({**self.document,'extras':{'source':'other'}}))
        for roles in [('b','m'),('b','t'),('m','t')]:
            assets=[]
            for role in ('b','m','t'):
                asset=copy.deepcopy(self.asset);asset.update(asset='part_'+role,model='wdbsw_'+role+'_011',texture='part_'+role)
                part={'model':asset['model']}
                if role==roles[0]:part['clip_max_y']=.2
                elif role==roles[1]:part.update(clip_min_y=.1,clip_max_y=.3)
                else:asset.update(source='other.glb',sha256=hashlib.sha256(other.read_bytes()).hexdigest())
                asset['parts']=[part];assets.append(asset)
            assembly={'base_item':12,'bottom':'wdbsw_b_011','middle':'wdbsw_m_011','top':'wdbsw_t_011',
                      'icon_bounds':{'min':[-1,-1,-1],'max':[1,1,1]}}
            config={'schema_version':1,'assets':assets,'composite_assemblies':[assembly]}
            with self.subTest(roles=roles),self.assertRaisesRegex(ValueError,'must not overlap'):
                exporter.validate_manifest(copy.deepcopy(config),self.root,self.manifest,'export')
            for boundary in (.2,.25):
                valid=copy.deepcopy(config)
                next(a for a in valid['assets'] if a['asset']=='part_'+roles[1])['parts'][0]['clip_min_y']=boundary
                exporter.validate_manifest(valid,self.root,self.manifest,'export')

    def test_four_partitioned_regions_cannot_implicitly_supply_two_primary_middles(self):
        asset=copy.deepcopy(self.asset);asset['parts']=[{'model':'wdbsw_b_011','clip_max_y':-.2},
            {'model':'wdbsw_m_011','clip_min_y':-.2,'clip_max_y':0},
            {'model':'wdbsw_m_021','clip_min_y':0,'clip_max_y':.2},
            {'model':'wdbsw_t_011','clip_min_y':.2}]
        with self.assertRaisesRegex(ValueError,'Composite export requires real'):self.validate(asset)
        assemblies=[{'base_item':12,'bottom':'wdbsw_b_011','middle':model,'top':'wdbsw_t_011',
                     'icon_bounds':{'min':[-1,-1,-1],'max':[1,1,1]}} for model in ['wdbsw_m_011','wdbsw_m_021']]
        with self.assertRaisesRegex(ValueError,'must cover that source'):
            self.validate(asset,composite_assemblies=assemblies)

    def test_multi_part_source_cannot_duplicate_a_whole_weapon_or_drop_regions(self):
        cases=[ [{'model':'wdbsw_'+role+'_011'} for role in ('b','m','t')],
                [{'model':'wdbsw_b_011','clip_max_y':-.2},{'model':'wdbsw_m_011','clip_min_y':-.1,'clip_max_y':.1},{'model':'wdbsw_t_011','clip_min_y':.1}],
                [{'model':'wdbsw_b_011','clip_max_y':0.},{'model':'wdbsw_m_011','clip_min_y':-.1,'clip_max_y':.1},{'model':'wdbsw_t_011','clip_min_y':.1}]]
        for parts in cases:
            asset=copy.deepcopy(self.asset);asset['parts']=parts
            with self.subTest(parts=parts), self.assertRaisesRegex(ValueError,'partition the Y axis'):
                self.validate(asset)

    def test_top_level_aliases_cannot_fake_composite_part_coverage(self):
        asset=copy.deepcopy(self.asset);asset.pop('parts');asset['aliases']=['wdbsw_b_011','wdbsw_t_011']
        assembly={'base_item':12,'bottom':'wdbsw_b_011','middle':'wdbsw_m_011','top':'wdbsw_t_011',
                  'icon_bounds':{'min':[-1,-1,-1],'max':[1,1,1]}}
        with self.assertRaisesRegex(ValueError,'top-level aliases'):
            self.validate(asset,composite_assemblies=[assembly])

    def test_clip_bounds_reject_nonfinite_reversed_and_ambiguous_intervals(self):
        for update in ({'clip_min_y':float('nan')},{'clip_min_y':.2,'clip_max_y':.1},
                       {'plane_y':0.,'clip_side':'above'}):
            asset=copy.deepcopy(self.asset);asset['parts'][1].update(update)
            with self.subTest(update=update), self.assertRaises(ValueError):self.validate(asset)

    def test_rotation_rejects_mirror_scale_and_nonfinite_values(self):
        for matrix in ([[1,0,0],[0,1,0],[0,0,-1]], [[2,0,0],[0,1,0],[0,0,1]], [[float('nan'),0,0],[0,1,0],[0,0,1]]):
            with self.subTest(matrix=matrix), self.assertRaises(ValueError): exporter.validate_orientation(matrix)
        exporter.validate_orientation([[0,-1,0],[1,0,0],[0,0,1]])

    def test_glb_checks_full_chunk_bounds_and_external_dependencies(self):
        exporter.glb_document(self.source)
        raw = self.source.read_bytes()
        for data in (raw[:8], raw[:-1], raw[:12]+struct.pack('<I', 0xfffffffc)+raw[16:]):
            self.source.write_bytes(data)
            with self.assertRaises(ValueError): exporter.glb_document(self.source)
        self.document['images'] = [{'uri':'unhashed.png'}]
        self.source.write_bytes(make_glb(self.document))
        with self.assertRaisesRegex(ValueError,'self-contained'): exporter.glb_document(self.source)

    def test_glb_document_parses_a_snapshot_of_bytes_not_the_path(self):
        raw = self.source.read_bytes()
        from_path = exporter.glb_document(self.source)
        self.source.write_bytes(b'changed after validation')
        self.assertEqual(exporter.glb_document(raw), from_path)

    def test_fit_manifest_hash_comes_from_the_parsed_snapshot(self):
        raw='\ufeff'.encode('utf-8')+json.dumps({'schema_version':1,'assets':[]}).encode()
        self.manifest.write_bytes(raw)
        real=Path.read_bytes;calls=[]
        def read_once(path):
            calls.append(path);data=real(path);path.write_bytes(b'changed after read');return data
        with unittest.mock.patch.object(Path,'read_bytes',read_once):
            config,digest=exporter.load_manifest(self.manifest)
        self.assertEqual(len(calls),1);self.assertEqual(config,{'schema_version':1,'assets':[]})
        self.assertEqual(digest,hashlib.sha256(raw).hexdigest())

    def test_package_snapshot_holds_only_the_verified_resource_bytes(self):
        export=self.root/'export';(export/'01_a/resources').mkdir(parents=True);(export/'empty_resources').mkdir()
        files={'01_a/resources/wdbsw_m_011.mdl':b'model','01_a/resources/field.tga':b'pixels','empty_resources/wthsh_012.mdl':b'empty'}
        for key,data in files.items():(export/key).write_bytes(data)
        manifest={'resources':{key:hashlib.sha256(data).hexdigest() for key,data in files.items()}}
        paths=exporter.snapshot_export(export,manifest,self.root/'package/verified_export')
        for key,data in files.items():(export/key).write_bytes(b'changed after verification')
        self.assertEqual({key:path.read_bytes() for key,path in paths.items()},files)
        self.assertTrue(all(path.is_relative_to(self.root/'package/verified_export') for path in paths.values()))
        with self.assertRaisesRegex(ValueError,'Changed export'):exporter.snapshot_export(export,manifest,self.root/'again')
        (export/'01_a/resources/extra.tga').write_bytes(b'x')
        with self.assertRaisesRegex(ValueError,'resource set'):exporter.snapshot_export(export,manifest,self.root/'third')

    def test_normal_basis_must_come_from_the_loaded_helper_snapshot(self):
        path=self.root/'helper.py';path.write_text('VALUE = 1')
        helper=exporter.load_module_snapshot('snapshot_helper_test',path);path.write_text('VALUE = 2')
        self.assertEqual(helper.VALUE,1);self.assertEqual(helper.SNAPSHOT_SHA256,hashlib.sha256(b'VALUE = 1').hexdigest())
        exporter.require_basis_producer({'producer_sha256':helper.SNAPSHOT_SHA256},helper.SNAPSHOT_SHA256)
        for record in ({},{'producer_sha256':hashlib.sha256(path.read_bytes()).hexdigest()}):
            with self.subTest(record=record),self.assertRaisesRegex(ValueError,'different TrellisWeaponNormals'):
                exporter.require_basis_producer(record,helper.SNAPSHOT_SHA256)

    def test_basis_producer_hash_names_the_loaded_snapshot(self):
        path=ROOT/'tools/TrellisWeaponNormals.py';helper=exporter.load_module_snapshot('normals_snapshot_test',path)
        self.assertEqual(helper.producer_sha256(),hashlib.sha256(path.read_bytes()).hexdigest())
        helper.SNAPSHOT_SHA256='f'*64;self.assertEqual(helper.producer_sha256(),'f'*64)

    def test_compiler_wrapper_and_helper_identities_name_the_loaded_bytes(self):
        wrapper,identity=exporter.load_compiler(ROOT)
        self.assertIs(wrapper.CleanModels,sys.modules['CleanModels'])
        self.assertEqual(identity['wrapper']['sha256'],hashlib.sha256((ROOT/'tools/CompileBlasterModels.py').read_bytes()).hexdigest())
        self.assertEqual(identity['wrapper_helper']['sha256'],hashlib.sha256((ROOT/'SWLOR_Haks/tools/CleanModels.py').read_bytes()).hexdigest())
        self.assertEqual(wrapper.SNAPSHOT_SHA256,identity['wrapper']['sha256']);self.assertEqual(wrapper.CleanModels.SNAPSHOT_SHA256,identity['wrapper_helper']['sha256'])

    def test_compiler_snapshot_is_verified_after_compilation(self):
        tool=self.root/'cleanmodels.exe';tool.write_bytes(b'compiler-one');seen=[]
        def compile_models(copy,ascii_dir,output,names):seen.append(Path(copy).read_bytes())
        snapshot=exporter.compile_with_snapshot(SimpleNamespace(compile_models=compile_models),tool,self.root,self.root/'first')
        self.assertEqual(seen,[b'compiler-one']);self.assertEqual(snapshot['sha256'],hashlib.sha256(b'compiler-one').hexdigest())
        def swapped(copy,ascii_dir,output,names):Path(copy).write_bytes(b'compiler-two')
        with self.assertRaisesRegex(ValueError,'changed during compilation'):
            exporter.compile_with_snapshot(SimpleNamespace(compile_models=swapped),tool,self.root,self.root/'second')

    def test_tools_run_from_an_immutable_snapshot(self):
        tool=self.root/'crunch.exe';tool.write_bytes(b'one')
        copy,identity=exporter.snapshot_tool(tool,self.root/'snapshot');tool.write_bytes(b'two')
        self.assertEqual(copy.read_bytes(),b'one');self.assertEqual(identity['sha256'],hashlib.sha256(b'one').hexdigest())
        self.assertNotEqual(exporter.tool_identity(tool)['sha256'],identity['sha256'])

    def test_tool_snapshots_keep_the_source_mode(self):
        tool=self.root/'cleanmodels';tool.write_bytes(b'#!/bin/sh\nexit 0\n')
        if os.name=='posix':os.chmod(tool,0o755)
        copy,_=exporter.snapshot_tool(tool,self.root/'snapshot')
        self.assertEqual(os.stat(copy).st_mode&0o7777,os.stat(tool).st_mode&0o7777)
        if os.name=='posix':self.assertTrue(os.access(copy,os.X_OK))

    @unittest.skipUnless(os.name=='posix','POSIX execute bits')
    def test_tool_snapshot_mode_comes_from_the_handle_that_read_the_bytes(self):
        tool=self.root/'crunch';tool.write_bytes(b'one');os.chmod(tool,0o755);real_open=open
        def swapping_open(path,*args,**kwargs):
            handle=real_open(path,*args,**kwargs)
            if Path(path)==tool.resolve():
                # Replace the configured path after it was opened: the snapshot must keep the opened file.
                replacement=self.root/'replacement';replacement.write_bytes(b'two');os.chmod(replacement,0o644);os.replace(replacement,tool)
            return handle
        with unittest.mock.patch('builtins.open',swapping_open):copy,identity=exporter.snapshot_tool(tool,self.root/'snapshot')
        self.assertEqual(copy.read_bytes(),b'one');self.assertTrue(os.access(copy,os.X_OK))
        self.assertEqual(identity['sha256'],hashlib.sha256(b'one').hexdigest())

    @unittest.skipIf(np is None,'numpy required')
    def test_materials_clipped_from_every_part_emit_no_maps_or_normal_provenance(self):
        resources=self.root/'resources';resources.mkdir();names={0:'kept',1:'gone'};material_dims={};mapdims={}
        for index,name in names.items():
            material_dims[index]={key:[4,4] for key in (name,name+'_n',name+'_s',name+'_r')};mapdims.update(material_dims[index])
            (resources/(name+'.mtr')).write_text('texture0 '+name)
            for key in material_dims[index]:(resources/(key+'.tga')).write_bytes(b'tga')
        basis_path=self.root/'source_basis.npz'
        np.savez_compressed(basis_path,**{name+'__'+key:np.zeros((1,3,3)) for name in names.values() for key in ('positions','normals','tangents','signs','uv')})
        basis={'sha256':'old','materials':['gone','kept']}
        self.assertEqual(exporter.drop_clipped_materials(resources,names,material_dims,mapdims,{'kept'},basis_path,basis),['gone'])
        self.assertEqual(names,{0:'kept'});self.assertEqual(set(mapdims),{'kept','kept_n','kept_s','kept_r'})
        self.assertEqual(sorted(p.name for p in resources.iterdir()),['kept.mtr','kept.tga','kept_n.tga','kept_r.tga','kept_s.tga'])
        with np.load(basis_path) as arrays:self.assertEqual({key.split('__')[0] for key in arrays.files},{'kept'})
        self.assertEqual(basis,{'sha256':hashlib.sha256(basis_path.read_bytes()).hexdigest(),'materials':['kept']})
        before=basis_path.read_bytes()
        self.assertEqual(exporter.drop_clipped_materials(resources,names,material_dims,mapdims,{'kept'},basis_path,basis),[])
        self.assertEqual(basis_path.read_bytes(),before)

    def test_material_rejects_silent_appearance_loss(self):
        for change in ({'alphaMode':'BLEND'}, {'doubleSided':True}, {'occlusionTexture':{'index':0}},
                       {'extensions':{'KHR_materials_unlit':{}}}, {'normalTexture':{'scale':float('nan')}},
                       {'pbrMetallicRoughness':{'roughnessFactor':2}}):
            with self.subTest(change=change), self.assertRaises(ValueError): exporter.validate_material(change)

    @unittest.skipIf(np is None,'numpy required')
    def test_metallic_to_specularity_matches_nwn_material_response(self):
        for metal, expected in [(0,.06), (.5,1.1/3), (1,1.6/3)]:
            self.assertAlmostEqual(exporter.metallic_to_specularity(metal),expected)

    @unittest.skipUnless(HAS_BLENDER,'Run Blender 4.0 to write staged TGA maps')
    def test_material_maps_apply_the_nwn_metal_response(self):
        response=exporter.material_response()
        doc={'materials':[{'pbrMetallicRoughness':{'baseColorFactor':[.08,.06,.05,1],'metallicFactor':1,'roughnessFactor':.5}},
                          {'pbrMetallicRoughness':{'baseColorFactor':[.08,.06,.05,1],'metallicFactor':0,'roughnessFactor':.5}}]}
        with tempfile.TemporaryDirectory() as folder:
            _,metal=exporter.material_maps(doc,b'',0,Path(folder),{},Path(folder),'metal',64)
            _,paint=exporter.material_maps(doc,b'',1,Path(folder),{},Path(folder),'paint',64)
        lum=lambda rgb:float(np.mean(exporter.linear(np.asarray(rgb))@response.LUMINANCE))
        self.assertGreaterEqual(lum(metal['metal']),response.METAL_LUMINANCE_FLOOR-1e-3)
        self.assertAlmostEqual(lum(paint['paint']),lum(exporter.srgb(np.array([.08,.06,.05]))),places=4)
        self.assertAlmostEqual(float(np.mean(metal['metal_s'])),1.6/3,places=4)
        self.assertAlmostEqual(float(np.mean(paint['paint_s'])),response.DIELECTRIC_SPECULARITY,places=4)
        self.assertLessEqual(float(np.mean(metal['metal_r'])),response.METAL_ROUGHNESS_RANGE[1])
        self.assertAlmostEqual(float(np.mean(paint['paint_r'])),.5,places=4)

    @unittest.skipUnless(HAS_BLENDER,'Run Blender 4.0 to write staged TGA maps')
    def test_constant_base_color_keeps_metal_mask_detail(self):
        response=exporter.material_response()
        doc={'materials':[{'pbrMetallicRoughness':{'baseColorFactor':[.08,.06,.05,1],
                                                   'metallicRoughnessTexture':{'index':0}}}]}
        mask=np.zeros((64,64,4),np.float32);mask[...,3]=1;mask[...,1]=.5;mask[30:32,30:32,2]=1  # one 2x2 metal spot
        with tempfile.TemporaryDirectory() as folder, \
                unittest.mock.patch.object(exporter,'read_image',return_value=mask):
            _,maps=exporter.material_maps(doc,b'',0,Path(folder),{},Path(folder),'spot',64)
        base=np.asarray(maps['spot']);self.assertEqual(base.shape[:2],(64,64))
        lum=exporter.linear(base)@response.LUMINANCE
        self.assertGreater(float(lum[30:32,30:32].min()),float(lum[0,0])+.05)
        # A coarser base texture is corrected at the metal map's size too.
        doc['materials'][0]['pbrMetallicRoughness']['baseColorTexture']={'index':1}
        coarse=np.full((4,4,4),.1,np.float32);coarse[...,3]=1
        with tempfile.TemporaryDirectory() as folder, \
                unittest.mock.patch.object(exporter,'read_image',side_effect=[coarse,mask]):
            _,maps=exporter.material_maps(doc,b'',0,Path(folder),{},Path(folder),'coarse',64)
        lum=exporter.linear(np.asarray(maps['coarse']))@response.LUMINANCE
        self.assertEqual(lum.shape,(64,64))
        self.assertGreater(float(lum[30:32,30:32].min()),float(lum[0,0])+.05)

    def test_texture_samplers_reject_unsupported_wrapping_filtering_and_indices(self):
        doc=copy.deepcopy(self.document);doc['textures']=[{'source':0,'sampler':0}]
        doc['samplers']=[{'wrapS':10497,'wrapT':10497,'magFilter':9729,'minFilter':9987}]
        exporter.validate_glb_geometry(doc)
        for key,value in [('wrapS',33071),('wrapT',33648),('magFilter',9728),('minFilter',9984),('extensions',{'custom':{}})]:
            invalid=copy.deepcopy(doc);invalid['samplers'][0][key]=value
            with self.subTest(key=key), self.assertRaisesRegex(ValueError,'sampler'):
                exporter.validate_glb_geometry(invalid)
        for index in (-1,1,False):
            invalid=copy.deepcopy(doc);invalid['textures'][0]['sampler']=index
            with self.subTest(index=index), self.assertRaisesRegex(ValueError,'sampler index'):
                exporter.validate_glb_geometry(invalid)
        doc.pop('samplers');doc['textures'][0].pop('sampler');exporter.validate_glb_geometry(doc)

    def test_vertex_color_and_morphs_cannot_be_silently_dropped(self):
        for change in ('COLOR_0','JOINTS_0','TEXCOORD_1'):
            doc = copy.deepcopy(self.document); doc['meshes'][0]['primitives'][0]['attributes'][change]=3
            with self.subTest(attribute=change), self.assertRaises(ValueError): exporter.validate_glb_geometry(doc)

    def test_compression_roles_follow_material_slots_not_names(self):
        mdl=self.root/'wdbsw_m_011.mdl'; mdl.write_text('placeholder')
        mtr=self.root/'field_s.mtr';mtr.write_text('texture0 field_s\ntexture1 arbitrary\ntexture2 shininess\ntexture3 gloss\n')
        self.assertEqual(exporter.texture_roles([mdl,mtr]), {'iwdbsw_m_011':'icon','field_s':'color','arbitrary':'normal','shininess':'scalar','gloss':'scalar'})

    def test_dds_payload_size_and_native_dimensions_are_checked(self):
        header=bytearray(128);header[:4]=b'DDS ';struct.pack_into('<II',header,12,256,64);struct.pack_into('<I',header,28,1);header[84:88]=b'DXT5'
        data=bytes(header)+bytes(16*64*16)
        exporter.dds_header(data,(64,256),1,'DXT5')
        for bad in (data[:-1], data+b'\0'):
            with self.assertRaisesRegex(ValueError,'payload'): exporter.dds_header(bad,(64,256),1,'DXT5')
        with self.assertRaisesRegex(ValueError,'dimensions'): exporter.dds_header(data,(64,160),1,'DXT5')

    @unittest.skipIf(np is None,'numpy required')
    def test_open_gl_normals_preserve_green_direction_and_renormalize(self):
        result=exporter.normalize_normal(np.array([[[.6,.8,0.]],[[0.,0.,4.]]]))
        np.testing.assert_allclose(result[0,0],[.6,.8,0])
        np.testing.assert_allclose(result[1,0],[0,0,1])
        with self.assertRaises(ValueError): exporter.normalize_normal(np.zeros((1,1,3)))
        with self.assertRaisesRegex(ValueError,'negative tangent-space Z'):
            exporter.normalize_normal(np.array([[[0.,0.,-1.]]]))

    @unittest.skipIf(np is None,'numpy required')
    def test_normal_source_texels_are_validated_before_downsampling_or_scale(self):
        for invalid in ([.5,.5,0.],[.5,.5,.5]):
            pixels=np.broadcast_to(np.array([.5,.5,1.]),(2,2,3)).copy();pixels[0,0]=invalid
            with self.subTest(invalid=invalid), self.assertRaises(ValueError):
                exporter.prepare_normal_map(pixels,1,1)
        pixels=np.broadcast_to(np.array([.5,.5,1.]),(2,2,3)).copy();pixels[0,0]=[1.,.5,.5]
        with self.assertRaises(ValueError):exporter.prepare_normal_map(pixels,0,1)
        valid=np.broadcast_to(np.array([.6,.8,1.]),(2,2,3)).copy();actual=exporter.prepare_normal_map(valid,2,1)
        expected=np.array([.4,1.2,1.]);expected/=np.linalg.norm(expected)
        np.testing.assert_allclose(actual[0,0],expected)

    @unittest.skipIf(np is None,'numpy required')
    def test_scaled_normals_are_averaged_as_unit_directions(self):
        # Two texels: one tilted fully in +X (after scale) and one straight up. Averaging unit
        # vectors gives the [1,0,1] direction, not the [2,0,1] of unequal-length vectors.
        pixels=np.array([[[1.,.5,.5+1e-4],[.5,.5,1.]]]);actual=exporter.prepare_normal_map(pixels,2,1)
        expected=np.array([1.,0.,1.]);expected/=np.linalg.norm(expected)
        np.testing.assert_allclose(actual[0,0],expected,atol=1e-3)

    @unittest.skipUnless(HAS_BLENDER,'Run Blender 4.0 for source surface proof')
    def test_faces_sharing_a_vertex_still_count_when_they_cross_elsewhere(self):
        Vector=exporter.Vector;e=1e-6
        v=[Vector(p) for p in [(0,0,0),(1,0,0),(0,1,0),(.4,.4,1),(.4,.4,-1),(-1,0,0),(0,-1,0),(e,e,1),(e,e,-1),(-e,-e,1),(-e,-e,-1),(.5,.5,0),(.5,-.5,0),(1,1,0),(2,0,0),(1,-1,0)]]
        crosses=lambda *faces:exporter.shell_crosses_itself(v,list(faces))
        self.assertTrue(crosses((0,1,2),(0,3,4)))    # shared corner, second face pierces the first
        self.assertTrue(crosses((0,1,2),(0,7,8)))    # a narrow face piercing right beside the shared corner
        self.assertFalse(crosses((0,1,2),(0,9,10)))  # the same sliver leaving the corner the other way
        self.assertFalse(crosses((0,1,2),(0,5,6)))   # shared corner only
        self.assertFalse(crosses((0,1,2),(1,0,6)))   # shared edge only
        self.assertTrue(crosses((0,1,2),(1,0,11)))   # coplanar faces folded onto the same side of their edge
        self.assertTrue(crosses((0,1,2),(0,1,13)))   # coplanar wedges at a shared corner overlap
        self.assertFalse(crosses((0,1,2),(0,12,1)))  # coplanar faces sharing an edge, on opposite sides
        self.assertTrue(crosses((0,1,2),(0,14,15)))  # sharing only the corner but touching along a common edge ray

    @unittest.skipIf(np is None,'numpy required')
    def test_shallow_crossings_of_large_triangles_still_touch(self):
        exporter.np=np
        big=np.array([[[-10,-10,0],[10,-10,0],[0,10,0]]],float)
        for angle in (5e-10,1e-7,1e-3):
            # A second large triangle tilted by `angle` about the x axis passes through the first one's plane.
            c,s_=math.cos(angle),math.sin(angle)
            tilted=np.array([[[-9,-9,-1e-9],[9,-9,-1e-9],[0,9,0]]],float)
            tilted[...,1],tilted[...,2]=tilted[...,1]*c-tilted[...,2]*s_,tilted[...,1]*s_+tilted[...,2]*c
            tilted[...,2]+=1e-12
            self.assertTrue(exporter.triangles_touch(big,tilted)[0],angle)
        apart=big+np.array([0,0,1e-3]);self.assertFalse(exporter.triangles_touch(big,apart)[0])
        side=np.array([[[30,0,-1],[31,0,1],[30,1,0]]],float);self.assertFalse(exporter.triangles_touch(big,side)[0])

    @unittest.skipUnless(HAS_BLENDER,'Run Blender 4.0 for source surface proof')
    def test_dense_fans_are_checked_in_bounded_batches(self):
        # A bipyramid: every face of each half shares one apex, so every pair of them is a candidate.
        segments=400;ring=[(math.cos(2*math.pi*k/segments),math.sin(2*math.pi*k/segments),0) for k in range(segments)]
        points=[exporter.Vector(p) for p in ring+[(0,0,1),(0,0,-1)]];top,bottom=segments,segments+1
        faces=[(k,(k+1)%segments,top) for k in range(segments)]+[((k+1)%segments,k,bottom) for k in range(segments)]
        seen=[];real=exporter.batch_crosses
        def spy(*args):
            seen.append(len(args[4]));return real(*args)
        with unittest.mock.patch.object(exporter,'batch_crosses',side_effect=spy):
            self.assertFalse(exporter.shell_crosses_itself(points,faces,batch=1000))
        self.assertGreater(len(seen),40);self.assertLessEqual(max(seen),1000)
        faces[0]=(0,1,bottom)  # move a top face onto the bottom half, coinciding with an existing face
        self.assertTrue(exporter.shell_crosses_itself(points,faces,batch=1000))

    @unittest.skipUnless(HAS_BLENDER,'Run Blender 4.0 for source surface proof')
    def test_shell_volume_survives_a_large_translation(self):
        bpy=exporter.bpy
        bpy.ops.mesh.primitive_cube_add(size=.02,location=(3e4,-3e4,2e4));ob=bpy.context.object
        try:self.assertEqual(exporter.closed_surface_report([ob])['inward_shells_validated_as_internal'],0)
        finally:bpy.data.objects.remove(ob,do_unlink=True)

    @unittest.skipUnless(HAS_BLENDER,'Run Blender 4.0 for source surface proof')
    def test_closed_surface_policy_rejects_a_self_intersecting_shell(self):
        bpy=exporter.bpy;objects=[]
        bottom=[(1,1,0),(-1,1,0),(-1,-1,0),(1,-1,0)]
        quads=[(0,3,2,1),(4,5,6,7)]+[(i,(i+1)%4,4+(i+1)%4,4+i) for i in range(4)]
        try:
            # A square prism twisted 30 degrees is a clean closed shell. At 90 degrees faces that share a corner also
            # cross elsewhere, and at 160 opposite sides cross, while every edge still pairs up with opposite
            # directions and the signed volume stays positive.
            for twist in (30,90,160):
                r=math.radians(twist);top=[(x*math.cos(r)-y*math.sin(r),x*math.sin(r)+y*math.cos(r),2) for x,y,_ in bottom]
                tris=[t for q in quads for t in ((q[0],q[1],q[2]),(q[0],q[2],q[3]))]  # explicit split: the diagonals that cross
                mesh=bpy.data.meshes.new('shell');mesh.from_pydata(bottom+top,[],tris);mesh.update()
                ob=bpy.data.objects.new('shell',mesh);bpy.context.scene.collection.objects.link(ob);objects.append(ob)
                bpy.context.view_layer.update()
                if twist>30:
                    with self.assertRaisesRegex(ValueError,'intersects itself'):exporter.closed_surface_report([ob])
                else:self.assertEqual(exporter.closed_surface_report([ob])['inward_shells_validated_as_internal'],0)
        finally:
            for ob in objects:bpy.data.objects.remove(ob,do_unlink=True)

    @unittest.skipUnless(HAS_BLENDER,'Run Blender 4.0 for source surface proof')
    def test_closed_surface_policy_allows_enclosed_cavity_but_not_exposed_inversion(self):
        bpy=exporter.bpy
        objects=[]
        for size,reverse in [(2,False),(.5,True)]:
            bpy.ops.mesh.primitive_cube_add(size=size)
            ob=bpy.context.object;objects.append(ob)
            if reverse:
                for polygon in ob.data.polygons:polygon.flip()
            ob.data.update()
        try:
            report=exporter.closed_surface_report(objects)
            self.assertEqual(report['inward_shells_validated_as_internal'],1)
            objects[1].location.x=4
            bpy.context.view_layer.update()
            with self.assertRaisesRegex(ValueError,'not proven enclosed'):exporter.closed_surface_report(objects)
            # A cavity nested inside another cavity sees aggregate winding zero, not one.
            objects[1].location.x=0
            bpy.ops.mesh.primitive_cube_add(size=.25);nested=bpy.context.object;objects.append(nested)
            for polygon in nested.data.polygons:polygon.flip()
            nested.data.update();bpy.context.view_layer.update()
            with self.assertRaisesRegex(ValueError,'not proven enclosed'):exporter.closed_surface_report(objects)
        finally:
            for ob in objects:bpy.data.objects.remove(ob,do_unlink=True)

    def make_mixed_surface_glb(self,missing_face=False):
        bpy=exporter.bpy
        bpy.ops.mesh.primitive_cube_add(size=2)
        cube=bpy.context.object;cube.data.calc_loop_triangles()
        triangles=np.array([[cube.data.vertices[i].co[:] for i in t.vertices] for t in cube.data.loop_triangles],np.float32)
        bpy.data.objects.remove(cube,do_unlink=True)
        # The unrelated open sheet shares a point, but no edge, with the cube.
        sheet=np.array([[triangles[0,0],[-4,-3,-2],[-4,-2,-3]]],np.float32)
        document={'asset':{'version':'2.0'},'buffers':[{}],'bufferViews':[],'accessors':[],
                  'materials':[{'name':'duplicate','doubleSided':True},{'name':'duplicate'}],
                  'meshes':[{'primitives':[]}],'nodes':[{'mesh':0}],'scenes':[{'nodes':[0]}],'scene':0}
        binary=bytearray()
        def add(values,kind):
            values=np.asarray(values,dtype='<f4');offset=len(binary);binary.extend(values.tobytes())
            view=len(document['bufferViews']);document['bufferViews'].append({'buffer':0,'byteOffset':offset,'byteLength':values.nbytes})
            accessor={'bufferView':view,'componentType':5126,'count':len(values),'type':kind}
            if kind=='VEC3':accessor.update(min=values.min(axis=0).tolist(),max=values.max(axis=0).tolist())
            index=len(document['accessors']);document['accessors'].append(accessor);return index
        for faces,material,color in [(triangles[:2],0,False),(triangles[2:-1] if missing_face else triangles[2:],1,True),(sheet,1,False)]:
            positions=faces.reshape(-1,3);normal=np.cross(faces[:,1]-faces[:,0],faces[:,2]-faces[:,0]);normal/=np.linalg.norm(normal,axis=1)[:,None]
            attributes={'POSITION':add(positions,'VEC3'),'NORMAL':add(np.repeat(normal,3,axis=0),'VEC3'),
                        'TEXCOORD_0':add(np.tile([[0,0],[1,0],[0,1]],(len(faces),1)),'VEC2')}
            if color:attributes['COLOR_0']=add(np.ones((len(positions),4)),'VEC4')
            document['meshes'][0]['primitives'].append({'attributes':attributes,'material':material})
        document['buffers'][0]['byteLength']=len(binary)
        return document,bytes(binary)

    @unittest.skipUnless(HAS_BLENDER,'Run Blender 4.0 for material-scoped closure proof')
    def test_closed_surface_policy_uses_identity_and_complete_affected_components(self):
        bpy=exporter.bpy;document,binary=self.make_mixed_surface_glb()
        exporter.tag_import_materials(document);path=self.root/'mixed.glb';exporter.write_glb(path,document,binary)
        before=set(bpy.data.objects);bpy.ops.import_scene.gltf(filepath=str(path));objects=[ob for ob in bpy.data.objects if ob not in before and ob.type=='MESH']
        try:
            materials={material for ob in objects for material in ob.data.materials}
            # Vertex-color and non-vertex-color variants retain the same source ID.
            self.assertEqual(sorted(exporter.imported_material_index(mat,2) for mat in materials),[0,1,1])
            for mat in materials:mat.name='renamed_by_blender'
            report=exporter.closed_surface_report(objects,{0},2)
            self.assertEqual(report['triangles'],12);self.assertEqual(report['shells'],1);self.assertEqual(report['unaffected_triangles'],1)
            with self.assertRaisesRegex(ValueError,'exactly closed'):exporter.closed_surface_report(objects,{1},2)
            # A neighboring single-sided face still participates in winding proof.
            for ob in objects:
                polygon=next(p for p in ob.data.polygons if exporter.imported_material_index(ob.data.materials[p.material_index],2)==1)
                polygon.flip();ob.data.update();break
            with self.assertRaisesRegex(ValueError,'consistently wound'):exporter.closed_surface_report(objects,{0},2)
            mat=next(iter(materials));del mat[exporter.SOURCE_MATERIAL_INDEX]
            with self.assertRaisesRegex(ValueError,'material identity'):exporter.imported_material_index(mat,2)
        finally:
            for ob in objects:bpy.data.objects.remove(ob,do_unlink=True)

    @unittest.skipUnless(HAS_BLENDER,'Run Blender 4.0 for export closure integration')
    def test_export_scopes_closure_to_double_sided_materials_and_rejects_affected_holes(self):
        for missing_face in (False,True):
            with self.subTest(missing_face=missing_face):
                document,binary=self.make_mixed_surface_glb(missing_face);path=self.root/f'source-{missing_face}.glb'
                for primitive in document['meshes'][0]['primitives']:primitive['attributes'].pop('COLOR_0',None)
                exporter.write_glb(path,document,binary);out=self.root/f'export-{missing_face}'
                # Stop immediately after the closure report: no rendering or runtime assets.
                expected='exactly closed' if missing_face else 'Explicit positive uniform scale'
                with self.assertRaisesRegex(ValueError,expected):
                    exporter.export_one({'double_sided_policy':'closed_surface','scale':0},path,self.root,out,'export',None,None)
                if not missing_face:
                    report=json.loads((out/'report.json').read_text())['surface_conversion']
                    self.assertEqual(report['triangles'],12);self.assertEqual(report['unaffected_triangles'],1)

    @unittest.skipUnless(HAS_BLENDER,'Run Blender 4.0 for unaffected cavity enclosure')
    def test_affected_inward_shell_accepts_valid_single_sided_outer_shell(self):
        bpy=exporter.bpy;objects=[];materials=[]
        for size,reverse,index in [(2,False,0),(.5,True,1)]:
            bpy.ops.mesh.primitive_cube_add(size=size);ob=bpy.context.object;objects.append(ob)
            mat=bpy.data.materials.new('shell');mat[exporter.SOURCE_MATERIAL_INDEX]=index;materials.append(mat);ob.data.materials.append(mat)
            if reverse:
                for polygon in ob.data.polygons:polygon.flip()
            ob.data.update()
        try:
            report=exporter.closed_surface_report(objects,{1},2)
            self.assertEqual(report['triangles'],12);self.assertEqual(report['inward_shells_validated_as_internal'],1)
            objects[1].location.x=4;bpy.context.view_layer.update()
            with self.assertRaisesRegex(ValueError,'not proven enclosed'):exporter.closed_surface_report(objects,{1},2)
        finally:
            for ob in objects:bpy.data.objects.remove(ob,do_unlink=True)
            for mat in materials:bpy.data.materials.remove(mat)

    def test_alias_keeps_same_name_material_even_when_alias_has_another_valid_material(self):
        helpers=exporter.load_module('alias_test_importer',ROOT/'tools/ImportBlasterModel.py')
        primary='wdbsw_m_011';alias='wdbsw_m_021';path=self.root/(primary+'.mdl')
        mesh=([(-1,0,0),(1,0,0),(0,1,0)],[(0,0),(1,0),(.5,1)],[(0,1,2)],[(0,0,1)]*3)
        helpers.write_mdl(path,primary,primary,[mesh])
        source=path.read_text().replace('newmodel '+primary,'newmodel '+primary+' # retain '+primary)
        source+='# bitmap, material, mesh and controller data for '+primary+' remain authored\n'
        (self.root/(primary+'.mtr')).write_text('texture0 original_albedo\ntexture1 original_normal\n')
        (self.root/(alias+'.mtr')).write_text('texture0 unrelated_albedo\ntexture1 unrelated_normal\n')
        result=exporter.alias_model_identifiers(source,primary,alias)
        (self.root/(alias+'.mdl')).write_text(result)
        for statement in ('newmodel','setsupermodel','beginmodelgeom','node dummy','endmodelgeom','donemodel'):
            self.assertIn(statement+' '+alias,result)
        self.assertIn('  parent '+alias+'\n',result);self.assertIn('  parent NULL\n',result)
        self.assertIn('  bitmap '+primary+'\n',result);self.assertIn('  materialname '+primary+'\n',result)
        material=re.search(r'^\s*materialname\s+(\S+)',result,re.M)[1]
        self.assertIn('texture0 original_albedo',(self.root/(material+'.mtr')).read_text())
        self.assertNotIn('unrelated_albedo',(self.root/(material+'.mtr')).read_text())
        self.assertIn('# retain '+primary,result)
        self.assertEqual(source.splitlines()[-1],result.splitlines()[-1])
        # The full generated mesh payload, including its label and controllers,
        # stays byte-identical apart from the one parent reference.
        original_mesh=source[source.index('node trimesh'):source.index('endmodelgeom')]
        aliased_mesh=result[result.index('node trimesh'):result.index('endmodelgeom')]
        self.assertEqual(aliased_mesh.replace('  parent '+alias+'\n','  parent '+primary+'\n'),original_mesh)

    @unittest.skipUnless(HAS_BLENDER,'Run Blender 4.0 for coplanar split ownership')
    def test_split_assigns_whole_coplanar_face_to_above_once(self):
        mesh=([(-1,0,0),(1,0,0),(0,0,1)],[(0,0),(1,0),(.5,1)],[(0,1,2)],[(0,-1,0)]*3)
        above=exporter.clip_mesh_y(mesh,0,'above');below=exporter.clip_mesh_y(mesh,0,'below')
        self.assertEqual(len(above[2]),1);self.assertEqual(below,([],[],[],[]))
        actual=[(above[0][i],above[1][i],above[3][i]) for i in above[2][0]]
        expected=[(mesh[0][i],mesh[1][i],mesh[3][i]) for i in mesh[2][0]]
        self.assertTrue(any(actual==expected[shift:]+expected[:shift] for shift in range(3)))
        a,b,c=[exporter.Vector(above[0][i]) for i in above[2][0]]
        self.assertEqual(tuple((b-a).cross(c-a)),(0,-2,0))

    @unittest.skipUnless(HAS_BLENDER,'Run Blender 4.0 for bounded coplanar ownership')
    def test_bounded_middle_owns_lower_plane_and_top_owns_upper_plane(self):
        vertices=[];uvs=[];faces=[];normals=[]
        for y in (-1,-.5,0,.5):
            first=len(vertices);vertices.extend([(-1,y,0),(1,y,0),(0,y,1)])
            uvs.extend([(0,0),(1,0),(.5,1)]);normals.extend([(0,-1,0)]*3);faces.append((first,first+1,first+2))
        mesh=(vertices,uvs,faces,normals)
        parts=[exporter.clip_part_mesh(mesh,part) for part in (
            {'clip_max_y':-.5},{'clip_min_y':-.5,'clip_max_y':.5},{'clip_min_y':.5})]
        self.assertEqual([[part[0][face[0]][1] for face in part[2]] for part in parts],[[-1],[-.5,0],[.5]])
        self.assertEqual(sum(len(part[2]) for part in parts),len(faces))
        for part in parts:
            for position,uv,normal in zip(part[0],part[1],part[3]):
                original=vertices.index(position)
                self.assertEqual(uv,uvs[original]);self.assertEqual(normal,normals[original])

    @unittest.skipUnless(HAS_BLENDER,'Run Blender 4.0 for split geometry')
    def test_split_keeps_shared_seam_uv_normals_winding_and_area(self):
        mesh=([(-1,-1,0),(1,-1,0),(1,1,0),(-1,1,0)],[(0,0),(1,0),(1,1),(0,1)],[(0,1,2),(0,2,3)],[(0,0,1)]*4)
        parts=[exporter.clip_mesh_y(mesh,0,side) for side in ('above','below')]
        def seam(part): return {(tuple(v),tuple(uv),tuple(n)) for v,uv,n in zip(part[0],part[1],part[3]) if v[1]==0}
        self.assertEqual(seam(parts[0]),seam(parts[1]))
        area=0
        for vertices,uvs,faces,normals in parts:
            for face in faces:
                a,b,c=[exporter.Vector(vertices[i]) for i in face]
                cross=(b-a).cross(c-a);self.assertGreater(cross.z,0);area+=cross.length/2
            for position,uv,normal in zip(vertices,uvs,normals):
                self.assertAlmostEqual(uv[0],(position[0]+1)/2)
                self.assertAlmostEqual(uv[1],(position[1]+1)/2)
                self.assertAlmostEqual(exporter.Vector(normal).length,1)
        self.assertAlmostEqual(area,4)

    @unittest.skipUnless(HAS_BLENDER,'Run Blender 4.0 for bounded middle clipping')
    def test_middle_clip_bounds_preserve_uv_normals_winding_and_area(self):
        mesh=([(-1,-1,0),(1,-1,0),(1,1,0),(-1,1,0)],[(0,0),(1,0),(1,1),(0,1)],[(0,1,2),(0,2,3)],[(0,0,1)]*4)
        vertices,uvs,faces,normals=exporter.clip_part_mesh(mesh,{'clip_min_y':-.25,'clip_max_y':.25})
        self.assertAlmostEqual(min(v[1] for v in vertices),-.25)
        self.assertAlmostEqual(max(v[1] for v in vertices),.25)
        area=0.
        for face in faces:
            a,b,c=[exporter.Vector(vertices[i]) for i in face];cross=(b-a).cross(c-a)
            self.assertGreater(cross.z,0);area+=cross.length/2
        self.assertAlmostEqual(area,1.)
        for position,uv,normal in zip(vertices,uvs,normals):
            self.assertAlmostEqual(uv[0],(position[0]+1)/2);self.assertAlmostEqual(uv[1],(position[1]+1)/2)
            self.assertAlmostEqual(exporter.Vector(normal).length,1)

    @unittest.skipUnless(HAS_BLENDER,'Run Blender 4.0 for interpolated seam normals')
    def test_two_plane_middle_normals_match_the_independently_clipped_top(self):
        mesh=([(-1,-1,0),(1,-1,0),(0,1,0)],[(0,0),(1,0),(.5,1)],[(0,1,2)],[(.8,0,.6),(0,.6,.8),(0,0,1)])
        middle=exporter.clip_part_mesh(mesh,{'clip_min_y':-.5,'clip_max_y':.5})
        top=exporter.clip_part_mesh(mesh,{'clip_min_y':.5})
        def seam(part):
            return {tuple(round(x,6) for x in p):(uv,n) for p,uv,n in zip(part[0],part[1],part[3]) if abs(p[1]-.5)<1e-6}
        a,b=seam(middle),seam(top);self.assertEqual(set(a),set(b));self.assertEqual(len(a),2)
        for key in a:
            np.testing.assert_allclose(a[key][0],b[key][0],atol=1e-7)
            np.testing.assert_allclose(a[key][1],b[key][1],atol=1e-7)

    @unittest.skipUnless(HAS_BLENDER,'Run Blender 4.0 for shared icon projection')
    def test_separate_parts_use_the_same_icon_frame_and_reject_clipped_geometry(self):
        vector=exporter.Vector;frame={'min':[-1,-2,-.5],'max':[1,2,.5]}
        bottom=exporter.shared_icon_frame([vector((0,-1,0)),vector((.1,-.5,.1))],frame)
        top=exporter.shared_icon_frame([vector((0,1,0)),vector((-.1,.5,-.1))],frame)
        self.assertEqual(bottom,top)
        self.assertEqual(tuple(bottom[1]),(0,0,0));self.assertEqual(bottom[2],4)
        self.assertEqual(len(bottom[0]),8)
        with self.assertRaisesRegex(ValueError,'outside shared'):
            exporter.shared_icon_frame([vector((0,3,0))],frame)

    @unittest.skipUnless(HAS_BLENDER,'Run Blender 4.0 for icon pixels')
    def test_icon_padding_keeps_top_art_pixels_and_transparent_bottom(self):
        bpy=exporter.bpy;path=self.root/'icon.tga'
        image=bpy.data.images.new('padding-source',width=4,height=4,alpha=True)
        pixels=np.ones((4,4,4),np.float32);pixels[:,:,:3]=[1,0,0]
        image.pixels.foreach_set(pixels.ravel());image.filepath_raw=str(path);image.file_format='TARGA_RAW';image.save();bpy.data.images.remove(image)
        exporter.pad_icon(path,(4,8))
        image=bpy.data.images.load(str(path),check_existing=False)
        try:
            data=np.array(image.pixels[:]).reshape(8,4,4)
            np.testing.assert_array_equal(data[:4],0)
            np.testing.assert_array_equal(data[4:],pixels)
        finally:bpy.data.images.remove(image)





@unittest.skipIf(np is None, 'NumPy required for native normal basis math')
class GltfIndexTests(unittest.TestCase):
    def test_references_must_be_existing_nonnegative_indices(self):
        items=['a','b']
        self.assertEqual(exporter.list_index(1,items,'image'),1)
        for value in (-1,2,True,1.0,None,'0'):
            with self.subTest(value=value),self.assertRaisesRegex(ValueError,'Invalid glTF image index'):
                exporter.list_index(value,items,'image')
        with self.assertRaisesRegex(ValueError,'Invalid glTF image index'):
            exporter.image_bytes({'images':[{'bufferView':0}],'bufferViews':[{'byteOffset':0,'byteLength':1}]},b'x',-1,Path('source.glb'))


class ImportedLightTests(unittest.TestCase):
    @unittest.skipUnless(HAS_BLENDER,'Run Blender 4.0 for imported light removal')
    def test_imported_lights_and_cameras_are_removed_before_rendering(self):
        bpy=exporter.bpy;bpy.ops.wm.read_factory_settings(use_empty=True)
        scene=bpy.context.scene
        for kind,data in (('LIGHT',bpy.data.lights.new('source_light','POINT')),('CAMERA',bpy.data.cameras.new('source_camera'))):
            scene.collection.objects.link(bpy.data.objects.new('source_'+kind.lower(),data))
        mesh=bpy.data.objects.new('weapon',bpy.data.meshes.new('weapon'));scene.collection.objects.link(mesh)
        exporter.remove_imported_lights()
        self.assertEqual([ob.type for ob in scene.objects],['MESH'])


class ImageViewBoundsTests(unittest.TestCase):
    def test_image_views_must_be_valid_nonnegative_ranges(self):
        binary = b'\x89PNG456789'
        for views, index in (([{'byteOffset': 0, 'byteLength': -1}], 0), ([{'byteOffset': -2, 'byteLength': 2}], 0),
                             ([{'byteOffset': 4, 'byteLength': 8}], 0), ([{'byteOffset': 0, 'byteLength': 2}], -1),
                             ([{'byteOffset': 0, 'byteLength': 2}], 1), ([{'byteOffset': 0}], 0)):
            doc = {'images': [{'bufferView': index}], 'bufferViews': views}
            with self.subTest(views=views, index=index), self.assertRaisesRegex(ValueError, 'bufferView'):
                exporter.image_bytes(doc, binary, 0, Path('source.glb'))
        doc = {'images': [{'bufferView': 0}], 'bufferViews': [{'byteOffset': 0, 'byteLength': 6}]}
        self.assertEqual(exporter.image_bytes(doc, binary, 0, Path('source.glb')), (b'\x89PNG45', '.png'))


class NativeWeaponNormalTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        spec=importlib.util.spec_from_file_location('weapon_normals',ROOT/'tools/TrellisWeaponNormals.py')
        cls.helper=importlib.util.module_from_spec(spec);spec.loader.exec_module(cls.helper)

    @unittest.skipIf(np is None,'numpy required')
    def test_uniform_scale_is_judged_relative_to_the_transform_size(self):
        self.assertAlmostEqual(self.helper.uniform_scale(np.eye(3)*1e-4),1e-4)
        turn=np.array([[0.,-1,0],[1,0,0],[0,0,1]]);self.assertAlmostEqual(self.helper.uniform_scale(turn*3e-3),3e-3)
        for linear in (np.diag([1e-4,2e-4,1e-4]),np.diag([1.,1,1.001]),np.diag([1e-4,1e-4,-1e-4]),np.zeros((3,3))):
            with self.subTest(linear=linear.tolist()),self.assertRaisesRegex(ValueError,'proper uniform'):self.helper.uniform_scale(linear)

    def test_geometry_references_must_be_nonnegative_indices(self):
        document={'accessors':[{'bufferView':0}],'bufferViews':[{}],'scenes':[{}],'nodes':[{}],'meshes':[{}]}
        for key in ('accessors','bufferViews','scenes','nodes','meshes'):
            self.assertIs(self.helper.gltf_index(document,key,0),document[key][0])
            for value in (-1,1,True,None):
                with self.subTest(key=key,value=value),self.assertRaisesRegex(ValueError,'Invalid glTF '+key):
                    self.helper.gltf_index(document,key,value)
        with self.assertRaisesRegex(ValueError,'Invalid glTF bufferViews'):
            self.helper.accessor({'accessors':[{'bufferView':-1,'type':'SCALAR','componentType':5126,'count':1}],'bufferViews':[{}]},b'\0'*4,0)

    def test_accessors_stay_inside_their_buffer_view(self):
        binary=bytes(range(32))
        def doc(view,item):return {'accessors':[{'bufferView':0,'type':'SCALAR','componentType':5126,**item}],'bufferViews':[view]}
        self.assertEqual(self.helper.accessor(doc({'byteOffset':8,'byteLength':8},{'count':2}),binary,0).shape,(2,1))
        for view,item in (({'byteOffset':8,'byteLength':8},{'count':3}),({'byteOffset':8,'byteLength':8},{'count':1,'byteOffset':-4}),
                          ({'byteOffset':8,'byteLength':40},{'count':1}),({'byteOffset':0},{'count':1}),({'byteOffset':0,'byteLength':8,'byteStride':2},{'count':2})):
            with self.subTest(view=view,item=item),self.assertRaisesRegex(ValueError,'out of bounds'):
                self.helper.accessor(doc(view,item),binary,0)

    def test_uv_v_conversion_reverses_green_but_preserves_world_normal(self):
        n=np.array([[0.,0.,1.]]);t=np.array([[1.,0.,0.]])
        source=np.array([[.5,.8,.9]])
        actual=self.helper.reproject_normals(source,n,t,np.array([1.]),n,t,np.array([-1.]))
        expected=self.helper.unit(source*2-1)*[1,-1,1]
        np.testing.assert_allclose(actual,expected,atol=1e-12)

    def test_native_tangent_rotation_requires_more_than_green_flip(self):
        n=np.array([[0.,0.,1.]]);old=np.array([[1.,0.,0.]]);new=np.array([[0.,1.,0.]])
        actual=self.helper.reproject_normals(np.array([[.8,.5,.9]]),n,old,np.array([1.]),n,new,np.array([1.]))
        np.testing.assert_allclose(actual,[[0.,-.6,.8]],atol=1e-12)

    def test_negative_native_z_is_rejected_without_clamp(self):
        n=np.array([[0.,0.,1.]]);t=np.array([[1.,0.,0.]])
        with self.assertRaisesRegex(ValueError,'positive-Z'):
            self.helper.reproject_normals(np.array([[.5,.5,1.]]),n,t,np.ones(1),-n,t,np.ones(1))

    def test_subdivided_face_matches_original_uv_and_rejects_wrong_owner(self):
        p=np.array([[[0.,0.,0.],[1.,0.,0.],[0.,1.,0.]]]);uv=p[...,:2]
        weights=np.array([[1.,0.,0.],[.5,.5,0.],[.5,0.,.5]])
        source={'positions':p,'uv':uv};target={'positions':np.array([weights@p[0]]),'uv':np.array([weights@uv[0]])}
        result=self.helper.match_faces(source,target)
        self.assertEqual(result[0][0],0);np.testing.assert_allclose(result[0][1],weights)
        target['uv']+=.2
        with self.assertRaisesRegex(ValueError,'ownership'):self.helper.match_faces(source,target)

    def test_face_matching_uses_the_spatial_index_and_finds_large_owners(self):
        rng=np.random.default_rng(5);count=20000
        small=rng.uniform(0,1,(count,1,3))+rng.normal(scale=1e-3,size=(count,3,3))
        large=np.array([[[-2.,-2.,5.],[4.,-2.,5.],[-2.,4.,5.]]]);p=np.concatenate([small,large]);uv=p[...,:2]
        weights=np.array([[1.,0.,0.],[.5,.5,0.],[.5,0.,.5]])
        target={'positions':np.concatenate([p[:count:50],[weights@large[0]]]),'uv':np.concatenate([uv[:count:50],[weights@uv[-1]]])}
        with unittest.mock.patch.object(np.linalg,'norm',wraps=np.linalg.norm) as norm:
            result=self.helper.match_faces({'positions':p,'uv':uv},target)
        self.assertEqual([si for si,_ in result[:-1]],list(range(0,count,50)))
        self.assertEqual(result[-1][0],count);np.testing.assert_allclose(result[-1][1],weights,atol=1e-12)
        # Per-face centre checks only see nearby candidates, never the whole source mesh.
        self.assertLess(max(call.args[0].shape[0] for call in norm.call_args_list[2:] if call.args[0].ndim==2),100)

    def test_tga_top_origin_is_normalized_without_changing_native_uv_pixels(self):
        with tempfile.TemporaryDirectory() as temporary:
            path=Path(temporary)/'normal.tga';header=bytearray(18);header[2]=2;struct.pack_into('<HH',header,12,2,2);header[16]=24;header[17]=32
            pixels=np.array([[[10,20,30],[40,50,60]],[[70,80,90],[100,110,120]]],dtype=np.uint8)
            path.write_bytes(header+pixels[...,::-1].tobytes());original,actual=self.helper.read_tga(path)
            np.testing.assert_array_equal(actual,pixels[::-1]);self.helper.write_tga(path,original,actual)
            self.assertEqual(path.read_bytes(),original)


    def test_normal_padding_fills_the_entire_mip_footprint_without_changing_coverage(self):
        pixels=np.zeros((3,21,4),dtype=np.uint8);pixels[...,3]=77
        coverage=np.zeros((3,21),bool);coverage[1,0]=coverage[1,20]=True
        pixels[1,0,:3]=[255,128,128];pixels[1,20,:3]=[128,255,128]
        result=self.helper.pad_native_normals(pixels,coverage)
        np.testing.assert_array_equal(result[coverage],pixels[coverage]);np.testing.assert_array_equal(result[...,3],77)
        np.testing.assert_array_equal(result[0,1,:3],pixels[1,0,:3]);np.testing.assert_array_equal(result[0,19,:3],pixels[1,20,:3])
        self.assertTrue(np.all(np.any(result[...,:3]!=0,axis=-1)))
        with self.assertRaisesRegex(ValueError,'covered texels'):
            self.helper.pad_native_normals(pixels,np.zeros_like(coverage))

    def test_normal_padding_wraps_edges_and_corners_to_the_correct_native_island(self):
        for covered,target in [((3,0),(3,8)),((0,3),(8,3)),((0,0),(8,8))]:
            with self.subTest(covered=covered,target=target):
                pixels=np.zeros((9,9,4),np.uint8);pixels[...,3]=np.arange(81).reshape(9,9)
                coverage=np.zeros((9,9),bool);coverage[covered]=coverage[4,4]=True
                pixels[covered][:3]=[255,128,128];pixels[4,4,:3]=[128,255,128]
                result=self.helper.pad_native_normals(pixels,coverage)
                np.testing.assert_array_equal(result[target][:3],pixels[covered][:3])
                np.testing.assert_array_equal(result[coverage],pixels[coverage]);np.testing.assert_array_equal(result[...,3],pixels[...,3])
                self.assertTrue(np.all(np.any(result[...,:3]!=0,axis=-1)))

    def test_normal_padding_matches_brute_force_toroidal_distance(self):
        rng=np.random.default_rng(7281)
        for height,width in [(1,9),(9,1),(3,8),(8,3),(9,11)]:
            for _ in range(12):
                coverage=rng.random((height,width))<.15;coverage[rng.integers(height),rng.integers(width)]=True
                pixels=np.zeros((height,width,4),np.uint8);pixels[...,3]=rng.integers(0,256,(height,width),dtype=np.uint8)
                owners=np.argwhere(coverage)
                for index,(y,x) in enumerate(owners):pixels[y,x,:3]=[index+1,128,255]
                result=self.helper.pad_native_normals(pixels,coverage)
                for y in range(height):
                    for x in range(width):
                        dy=np.abs(owners[:,0]-y);dx=np.abs(owners[:,1]-x)
                        distance=np.minimum(dy,height-dy)+np.minimum(dx,width-dx)
                        owner=int(result[y,x,0])-1
                        self.assertGreaterEqual(owner,0);self.assertEqual(distance[owner],distance.min())
                np.testing.assert_array_equal(result[coverage],pixels[coverage]);np.testing.assert_array_equal(result[...,3],pixels[...,3])

    def test_reprojection_replaces_source_basis_padding_through_the_last_mip(self):
        positions=np.array([[[0.,0.,0.],[1.,0.,0.],[0.,1.,0.]]]);uv=np.array([[[.4,.4],[.6,.4],[.4,.6]]])
        source={'positions':positions,'uv':uv,'normals':np.array([[[0.,0.,1.]]*3]),
                'tangents':np.array([[[1.,0.,0.]]*3]),'signs':np.ones((1,3))}
        target={**source,'tangents':np.array([[[0.,1.,0.]]*3])}
        with tempfile.TemporaryDirectory() as temporary:
            path=Path(temporary)/'source.tga';out=Path(temporary)/'native.tga';header=bytearray(18)
            header[2]=2;struct.pack_into('<HH',header,12,64,64);header[16]=24
            color=np.array([204,128,230],np.uint8);pixels=np.broadcast_to(color,(64,64,3)).copy()
            path.write_bytes(header+pixels[...,::-1].tobytes());report=self.helper.reproject_texture(source,[target],path,out)
            _,result=self.helper.read_tga(out);native=result[29,29,:3].copy()
            self.assertFalse(np.array_equal(native,color));self.assertEqual(report['covered_texels']+report['dilated_texels'],64*64)
            mip=result[...,:3].astype(float)
            while True:
                np.testing.assert_allclose(mip,np.broadcast_to(native,mip.shape))
                if mip.shape[0]==1:break
                mip=mip.reshape(mip.shape[0]//2,2,mip.shape[1]//2,2,3).mean(axis=(1,3))

    def test_reprojection_rejects_a_subtexel_face_even_after_other_faces_have_coverage(self):
        positions=np.array([[[0.,0.,0.],[1.,0.,0.],[0.,1.,0.]],[[3.,0.,0.],[4.,0.,0.],[3.,1.,0.]]])
        uv=np.array([[[.1,.1],[.9,.1],[.1,.9]],[[.201,.201],[.202,.201],[.201,.202]]])
        source={'positions':positions,'uv':uv,'normals':np.array([[[0.,0.,1.]]*3]*2),
                'tangents':np.array([[[1.,0.,0.]]*3]*2),'signs':np.ones((2,3))}
        target={**source,'tangents':np.array([[[1.,0.,0.]]*3,[[0.,1.,0.]]*3])}
        with tempfile.TemporaryDirectory() as temporary:
            path=Path(temporary)/'source.tga';out=Path(temporary)/'native.tga';header=bytearray(18)
            header[2]=2;struct.pack_into('<HH',header,12,64,64);header[16]=24
            pixels=np.broadcast_to(np.array([204,128,230],np.uint8),(64,64,3));path.write_bytes(header+pixels[...,::-1].tobytes())
            with self.assertRaisesRegex(ValueError,'face 1 has no texel-center coverage'):
                self.helper.reproject_texture(source,[target],path,out)
            self.assertFalse(out.exists())

    def authored_fixture(self):
        p=np.array([[[0.,0.,0.],[1.,0.,0.],[0.,1.,0.]]]);n=np.array([[[0.,0.,1.]]*3]);t=np.array([[[1.,0.,0.]]*3])
        source={'positions':p,'uv':p[...,:2],'normals':n,'tangents':t,'signs':np.ones((1,3))}
        mesh={**source,'material':'material','vertex_indices':np.array([[0,1,2]]),'tangent_offset':32,'sign_offset':80}
        return source,mesh

    def test_authored_policy_rejects_coincident_faces_with_different_frames(self):
        source,mesh=self.authored_fixture();source={k:np.repeat(v,2,axis=0) for k,v in source.items()};source['signs'][1]*=-1
        with self.assertRaisesRegex(ValueError,'ambiguous coincident'):
            self.helper.match_faces(source,mesh)

    def test_authored_policy_changes_only_existing_tangent_sign_streams(self):
        from unittest.mock import patch
        source,mesh=self.authored_fixture()
        with tempfile.TemporaryDirectory() as temporary:
            path=Path(temporary)/'model.mdl';original=bytes(range(128));path.write_bytes(original)
            with patch.object(self.helper,'native_meshes',return_value=[mesh]):
                report=self.helper.preserve_authored_tangents(path,{'material':source})
            result=path.read_bytes();allowed=set(range(32,68))|set(range(80,92))
            self.assertTrue(all(i in allowed for i,(a,b) in enumerate(zip(original,result)) if a!=b))
            np.testing.assert_allclose(np.frombuffer(result,dtype='<f4',count=9,offset=32).reshape(3,3),source['tangents'][0])
            np.testing.assert_array_equal(np.frombuffer(result,dtype='<f4',count=3,offset=80),1)
            self.assertTrue(report['only_existing_tangent_sign_streams_changed'])

    def test_authored_policy_rejects_shared_vertex_conflict_without_writing(self):
        from unittest.mock import patch
        source,mesh=self.authored_fixture();source['tangents'][0,1]=[0,1,0];mesh['vertex_indices'][0,1]=0
        with tempfile.TemporaryDirectory() as temporary:
            path=Path(temporary)/'model.mdl';original=bytes(range(128));path.write_bytes(original)
            with patch.object(self.helper,'native_meshes',return_value=[mesh]):
                with self.assertRaisesRegex(ValueError,'conflict at compiled vertex'):
                    self.helper.preserve_authored_tangents(path,{'material':source})
            self.assertEqual(path.read_bytes(),original)


    def test_source_basis_checks_referenced_corners_not_removed_shell_vertices(self):
        document={'bufferViews':[],'accessors':[],'nodes':[{'mesh':0}],'scenes':[{'nodes':[0]}]};binary=bytearray()
        def add(values,kind,component=5126):
            data=np.asarray(values,dtype='<f4' if component==5126 else '<u4');offset=len(binary);binary.extend(data.tobytes());document['bufferViews'].append({'buffer':0,'byteOffset':offset,'byteLength':data.nbytes});document['accessors'].append({'bufferView':len(document['bufferViews'])-1,'count':len(data),'componentType':component,'type':kind});return len(document['accessors'])-1
        attributes={'POSITION':add([[0,0,0],[1,0,0],[0,1,0],[4,4,4]],'VEC3'),'NORMAL':add([[0,0,1]]*3+[[0,0,0]],'VEC3'),'TANGENT':add([[1,0,0,1]]*3+[[0,0,0,0]],'VEC4'),'TEXCOORD_0':add([[0,0],[1,0],[0,1],[0,0]],'VEC2')}
        document['meshes']=[{'primitives':[{'attributes':attributes,'indices':add([0,1,2],'SCALAR',5125),'material':0}]}]
        config={'orientation':np.eye(3).tolist(),'scale':1.,'source_grip':[0,0,0],'target_grip':[0,0,0],'sha256':'0'*64}
        with tempfile.TemporaryDirectory() as temporary:
            path=Path(temporary)/'basis.npz';self.helper.source_basis(document,bytes(binary),config,['material'],path)
            with np.load(path) as data:self.assertEqual(data['material__tangents'].shape,(1,3,3))
            document['meshes'][0]['primitives'][0]['indices']=add([0,1,3],'SCALAR',5125)
            with self.assertRaisesRegex(ValueError,'Degenerate'):
                self.helper.source_basis(document,bytes(binary),config,['material'],path)


if __name__ == '__main__':
    unittest.main(argv=['trellis-export-tests'])
