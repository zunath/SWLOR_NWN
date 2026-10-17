"""Run Python unittest discovery; Blender 4.0 additionally covers clipping and icon pixels."""
import copy
import hashlib
import importlib.util
import json
from pathlib import Path
import struct
import tempfile
import unittest

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
        (table / 'baseitems.2da').write_text('2DA V2.0\n\nItemClass ModelType InvSlotWidth InvSlotHeight\n12 wdbsw 2 2 5\n59 wthsh 0 1 1\n')
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

    def test_material_rejects_silent_appearance_loss(self):
        for change in ({'alphaMode':'BLEND'}, {'doubleSided':True}, {'occlusionTexture':{'index':0}},
                       {'extensions':{'KHR_materials_unlit':{}}}, {'normalTexture':{'scale':float('nan')}},
                       {'pbrMetallicRoughness':{'roughnessFactor':2}}):
            with self.subTest(change=change), self.assertRaises(ValueError): exporter.validate_material(change)
        for metal, expected in [(0,.2), (.5,1.1/3), (1,1.6/3)]:
            self.assertAlmostEqual(exporter.metallic_to_specularity(metal),expected)

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
        finally:
            for ob in objects:bpy.data.objects.remove(ob,do_unlink=True)

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
class NativeWeaponNormalTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        spec=importlib.util.spec_from_file_location('weapon_normals',ROOT/'tools/TrellisWeaponNormals.py')
        cls.helper=importlib.util.module_from_spec(spec);spec.loader.exec_module(cls.helper)

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
