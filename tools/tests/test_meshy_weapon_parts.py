"""Authored native cut covers are closed, outward, and preserve GLB attributes."""
from collections import Counter
from pathlib import Path
import sys
import tempfile
import unittest

import numpy as np

sys.path.insert(0,str(Path(__file__).resolve().parents[1]))
import MeshyWeaponParts as parts
import TrellisWeaponExport as exporter
import TrellisWeaponNormals as normals
import NativeTangentSeams as seams

try:
    exporter.load_blender()
    HAS_BLENDER=True
except ImportError:
    HAS_BLENDER=False


class MeshyWeaponPartsTests(unittest.TestCase):
    joint={'y':.1,'center_xz':[.01,-.02],'radii_xz':[.022,.027],'depth':.007}

    def test_clipping_keeps_each_coincident_triangles_authored_basis(self):
        source=parts.flat_basis([[[0,0,0],[1,0,0],[0,1,0]]]*2)
        source['normals'][1]=[0,.6,.8]
        result=parts.clip_source_basis(source,[(.25,float('inf'))])
        self.assertEqual(2,len(result['positions']))
        np.testing.assert_allclose(result['normals'][0],source['normals'][0])
        np.testing.assert_allclose(result['normals'][1],source['normals'][1])
        np.testing.assert_allclose(result['positions'][0],result['positions'][1])

    def test_clipping_preserves_the_linear_uv_field_and_winding(self):
        source=parts.flat_basis([[[0,0,0],[1,0,0],[0,1,0]]])
        source['uv']=source['positions'][...,:2]*[.7,.6]+[.1,.2]
        result=parts.clip_source_basis(source,[(.2,.7)])
        np.testing.assert_allclose(result['uv'],result['positions'][...,:2]*[.7,.6]+[.1,.2])
        self.assertTrue(np.all(result['positions'][...,1]>=.2-1e-12))
        self.assertTrue(np.all(result['positions'][...,1]<=.7+1e-12))
        cross=np.cross(result['positions'][:,1]-result['positions'][:,0],result['positions'][:,2]-result['positions'][:,0])
        self.assertTrue(np.all(cross[:,2]>0))

    def test_shared_plane_faces_belong_to_the_above_part(self):
        source=parts.flat_basis([[[0,.25,0],[0,.25,1],[1,.25,0]]])
        self.assertEqual(0,len(parts.clip_source_basis(source,[(float('-inf'),.25)])['positions']))
        self.assertEqual(1,len(parts.clip_source_basis(source,[(.25,float('inf'))])['positions']))

    def test_clipping_rejects_interpolated_mixed_handedness(self):
        source=parts.flat_basis([[[0,0,0],[1,0,0],[0,1,0]]])
        source['signs'][0]=[1,-1,1]
        with self.assertRaisesRegex(ValueError,'ambiguous tangent handedness'):
            parts.clip_source_basis(source,[(.5,float('inf'))])

    def test_sleeves_are_closed_outward_and_end_at_both_native_sides(self):
        for outward in (-1,1):
            triangles=np.asarray(parts.sleeve_triangles(self.joint,.1,outward))
            edges=Counter()
            for tri in triangles:
                for a,b in zip(tri,np.roll(tri,-1,axis=0)):
                    edges[tuple(sorted((tuple(a),tuple(b))))]+=1
            self.assertTrue(all(count==2 for count in edges.values()))
            origin=triangles[0,0]
            local=triangles-origin
            volume=np.sum(np.einsum('ij,ij->i',local[:,0],np.cross(local[:,1],local[:,2])))/6
            self.assertGreater(volume,0)
            y=triangles[...,1]
            self.assertAlmostEqual(y.min(),min(.1,.1-outward*.007))
            self.assertAlmostEqual(y.max(),max(.1,.1-outward*.007))

    def test_cut_cap_winding_matches_the_outward_normal(self):
        points=[[-.1,-.2],[.1,-.2],[.1,.2],[-.1,.2],[0,0],[-.1,-.2]]
        for outward in (-1,1):
            triangles=np.asarray(parts.cap_triangles(points,.1,outward))
            cross=np.cross(triangles[:,1]-triangles[:,0],triangles[:,2]-triangles[:,0])
            self.assertTrue(np.all(cross[:,1]*outward>0))
            self.assertAlmostEqual(np.linalg.norm(cross,axis=1).sum()/2,.08)

    def test_glb_roundtrip_keeps_authored_positions_uvs_normals_and_tangents(self):
        source=parts.flat_basis(parts.sleeve_triangles(self.joint,.1,1))
        document={'asset':{'version':'2.0'},'materials':[{'pbrMetallicRoughness':{}}]}
        identity={'orientation':np.eye(3).tolist(),'scale':1,'source_grip':[0,0,0],'target_grip':[0,0,0], 'sha256':'0'*64}
        with tempfile.TemporaryDirectory() as temporary:
            path=Path(temporary)/'part.glb';basis=Path(temporary)/'basis.npz'
            parts.write_source(document,b'',[(0,source)],path,exporter)
            doc,binary=exporter.glb_document(path);exporter.validate_glb_geometry(doc)
            normals.source_basis(doc,binary,identity,{0:'metal'},basis)
            with np.load(basis,allow_pickle=False) as data:
                for key in source:np.testing.assert_allclose(data['metal__'+key],source[key],atol=1e-7)

    def test_invalid_unmeasured_or_repeated_joint_data_is_rejected(self):
        for update in ({'depth':0},{'radii_xz':[-1,.02]},{'center_xz':[0]}, {'y':float('nan')},{'unknown':1}):
            joint={**self.joint,**update}
            with self.subTest(update=update),self.assertRaises(ValueError):parts.validate_joints([joint])
        with self.assertRaises(ValueError):parts.validate_joints([self.joint,self.joint])

    @unittest.skipUnless(HAS_BLENDER,'Run Blender 4.0 for the actual glTF UV conversion')
    def test_blender_import_restores_normals_using_the_native_uv_frame_once(self):
        source=parts.flat_basis([[[0,0,0],[.1,.2,0],[0,.1,.3]]])
        source['uv']=np.array([[[.13,.21],[.82,.37],[.27,.91]]])
        document={'asset':{'version':'2.0'},'materials':[{'pbrMetallicRoughness':{}}]}
        with tempfile.TemporaryDirectory() as temporary:
            path=Path(temporary)/'source.glb';parts.write_source(document,b'',[(0,source)],path,exporter)
            bpy=exporter.bpy;bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.gltf(filepath=str(path))
            ob=next(ob for ob in bpy.context.scene.objects if ob.type=='MESH')
            helper=exporter.load_module('parts_test_geometry',Path(__file__).resolve().parents[1]/'ImportBlasterModel.py')
            mesh=helper.export_mesh(ob.data,ob.matrix_world)
            restored=seams.restore_source_normals(mesh,source,normals);indices=np.asarray(restored[2])
            np.testing.assert_allclose(np.asarray(restored[1])[indices],source['uv'],atol=1e-7)
            np.testing.assert_allclose(np.asarray(restored[3])[indices],source['normals'],atol=1e-7)


if __name__=='__main__':unittest.main(argv=[__file__] if HAS_BLENDER else None)
