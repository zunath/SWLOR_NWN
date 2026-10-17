"""Run under Blender4.0 for local thin-body repair checks; no GPU/model weights."""
import importlib.util
import json
import hashlib
import tempfile
from types import SimpleNamespace
from pathlib import Path
import unittest
from unittest.mock import patch
import numpy as np

TOOLS=Path(__file__).resolve().parents[1]
spec=importlib.util.spec_from_file_location('shield_surface',TOOLS/'TrellisShieldSurface.py')
surface=importlib.util.module_from_spec(spec);spec.loader.exec_module(surface)
try:
    import bpy
    HAS_BLENDER=True
except ImportError:HAS_BLENDER=False


def box(center=(0,0,0),size=(.6,1.,.03)):
    vertices=np.asarray([[-1,-1,-1],[1,-1,-1],[1,1,-1],[-1,1,-1],[-1,-1,1],[1,-1,1],[1,1,1],[-1,1,1]],np.float32)
    vertices=vertices*np.asarray(size,np.float32)/2+np.asarray(center,np.float32)
    faces=np.asarray([[0,2,1],[0,3,2],[4,5,6],[4,6,7],[0,1,5],[0,5,4],[1,2,6],[1,6,5],[2,3,7],[2,7,6],[3,0,4],[3,4,7]],np.int32)
    return vertices,faces


def combine(*meshes):
    vertices=[];faces=[];offset=0
    for v,f in meshes:vertices.append(v);faces.append(f+offset);offset+=len(v)
    return np.concatenate(vertices),np.concatenate(faces)


class ShieldSurfaceTests(unittest.TestCase):
    def test_exact_position_weld_detects_hidden_open_and_nonmanifold_edges(self):
        vertices,faces=box()
        surface.require_closed(surface.audit_surface(vertices,faces,components=True))
        split=vertices[faces].reshape(-1,3);split_faces=np.arange(len(split)).reshape(-1,3)
        surface.require_closed(surface.audit_surface(split,split_faces))
        for invalid in (faces[:-1],np.concatenate((faces,faces[:1])),faces[:,::-1].copy()):
            if len(invalid)==len(faces):invalid[1:]=faces[1:]
            with self.subTest(faces=len(invalid)),self.assertRaisesRegex(ValueError,'closed nondegenerate'):
                surface.require_closed(surface.audit_surface(vertices,invalid))

    def test_only_whole_zero_volume_two_face_islands_are_removed(self):
        body=box();tiny=box((.45,0,0),(.01,.01,.01))
        flat=(np.asarray([[0,0,2],[.1,0,2],[0,.1,2]],np.float32),np.asarray([[0,1,2],[2,1,0]],np.int32))
        vertices,faces=combine(body,tiny,flat)
        before=surface.audit_surface(vertices,faces,components=True)
        self.assertEqual(before['zero_volume_shells'],1)
        with self.assertRaises(ValueError):surface.require_closed(before)
        v,f,removed=surface.remove_collapsed_islands(vertices,faces)
        self.assertEqual(removed,2);self.assertEqual(len(f),24)
        report=surface.audit_surface(v,f,components=True);surface.require_closed(report)
        self.assertEqual(len(report['shells']),2)
        self.assertTrue(all(shell['signed_volume']>0 for shell in report['shells']))
        with self.assertRaisesRegex(ValueError,'no volumetric geometry'):surface.remove_collapsed_islands(*flat)

    def test_quantization_preserves_every_byte_and_positive_grazing_normal(self):
        images=np.zeros((1,1,256,3),np.float32);images[0,0,:,0]=np.arange(256)/255
        images[...,1:]=.5
        result=surface.normal_unorm8(images)
        np.testing.assert_array_equal(result[0,0,:,0],np.arange(256))
        self.assertTrue((result[...,1:]==128).all())
        # The bridge's float return must survive the installed GLB byte truncation.
        restored=np.minimum((result.astype(np.float32)+.25)/255,1)
        np.testing.assert_array_equal((restored*255).astype(np.uint8),result)

    def test_invalid_normal_maps_are_rejected_before_rounding(self):
        for value in (.5,.499999,float('nan'),float('inf'),-1,2):
            image=np.asarray([[[.5,.5,value]]],np.float32)
            with self.subTest(value=value),self.assertRaises(ValueError):surface.normal_unorm8(image)
        with self.assertRaises(ValueError):surface.normal_unorm8(np.zeros((2,2,4)))

    def test_bridge_rejects_textured_source_and_detects_changed_surface_cache(self):
        spec=importlib.util.spec_from_file_location('shield_bridge',TOOLS/'trellis2_shield_nodes/__init__.py')
        bridge=importlib.util.module_from_spec(spec);spec.loader.exec_module(bridge)
        bridge.require_untextured(SimpleNamespace())
        for attribute in ('uvs','vertex_colors','texture','metallic_roughness','normal_map','material','emissive','tangents'):
            with self.subTest(attribute=attribute),self.assertRaisesRegex(ValueError,'precede UV/material'):
                bridge.require_untextured(SimpleNamespace(**{attribute:object()}))
        with tempfile.TemporaryDirectory() as temporary:
            path=Path(temporary)/'surface.npz';v,f=box();np.savez(path,vertices=v,faces=f)
            path.with_suffix('.json').write_text(json.dumps({'output_sha256':hashlib.sha256(path.read_bytes()).hexdigest()}))
            actual=bridge.cached_arrays(path)
            np.testing.assert_array_equal(actual[0],v);np.testing.assert_array_equal(actual[1],f)
            path.write_bytes(path.read_bytes()+b'changed')
            with self.assertRaisesRegex(ValueError,'Cached shield surface changed'):bridge.cached_arrays(path)

    def test_schema_discovery_rejects_missing_blender_invalid_cache_and_wrong_build(self):
        spec=importlib.util.spec_from_file_location('shield_preflight_bridge',TOOLS/'trellis2_shield_nodes/__init__.py')
        bridge=importlib.util.module_from_spec(spec);spec.loader.exec_module(bridge)
        with patch.object(bridge.shutil,'which',return_value=None):
            with self.assertRaisesRegex(ValueError,'SWLOR_BLENDER'):bridge.SWLORClosedVoxelMesh.INPUT_TYPES()
        with patch.object(bridge.shutil,'which',return_value='not-invoked'),patch.dict(bridge.os.environ,{'SWLOR_SHIELD_CACHE':'relative'}):
            with self.assertRaisesRegex(ValueError,'absolute task-owned'):bridge.SWLORClosedVoxelMesh.INPUT_TYPES()
        with tempfile.TemporaryDirectory() as temporary:
            executable=Path(temporary)/'blender';executable.write_bytes(b'test-build')
            cache=Path(temporary)/'not-created-by-preflight'
            with patch.object(bridge.shutil,'which',return_value=str(executable)),patch.dict(bridge.os.environ,{'SWLOR_SHIELD_CACHE':str(cache)}):
                with patch.object(bridge.subprocess,'run',return_value=SimpleNamespace(stdout='Blender 3.6.0')):
                    with self.assertRaisesRegex(ValueError,'Blender 4.0'):bridge.SWLORClosedVoxelMesh.INPUT_TYPES()
                with patch.object(bridge.subprocess,'run',return_value=SimpleNamespace(stdout='Blender 4.0.0')):
                    self.assertIn('mesh',bridge.SWLORClosedVoxelMesh.INPUT_TYPES()['required'])
            self.assertFalse(cache.exists())
            with patch.object(bridge.shutil,'which',return_value=str(executable)),patch.dict(bridge.os.environ,{'SWLOR_SHIELD_CACHE':str(executable/'child')}):
                with self.assertRaisesRegex(ValueError,'beneath a directory'):bridge.SWLORClosedVoxelMesh.INPUT_TYPES()

    def test_incomplete_surface_cache_pairs_are_discarded_and_can_be_rebuilt(self):
        spec=importlib.util.spec_from_file_location('shield_cache_bridge',TOOLS/'trellis2_shield_nodes/__init__.py')
        bridge=importlib.util.module_from_spec(spec);spec.loader.exec_module(bridge)
        with tempfile.TemporaryDirectory() as temporary:
            path=Path(temporary)/'closed_high.npz';report=path.with_suffix('.json');v,f=box()
            for failure in ('npz_only','report_only','partial_report','valid_empty_report'):
                with self.subTest(failure=failure):
                    if failure!='report_only':np.savez(path,vertices=v,faces=f)
                    if failure!='npz_only':report.write_text('{' if failure=='partial_report' else '{}')
                    path.with_suffix('.npz.part').write_bytes(b'interrupted')
                    report.with_suffix('.json.part').write_bytes(b'interrupted')
                    self.assertIsNone(bridge.cached_entry(path))
                    self.assertFalse(any(Path(temporary).iterdir()))
                    np.savez(path,vertices=v,faces=f)
                    report.write_text(json.dumps({'output_sha256':hashlib.sha256(path.read_bytes()).hexdigest()}))
                    actual=bridge.cached_entry(path)
                    np.testing.assert_array_equal(actual[0],v);np.testing.assert_array_equal(actual[1],f)
                    path.write_bytes(path.read_bytes()+b'changed')
                    with self.assertRaisesRegex(ValueError,'Cached shield surface changed'):bridge.cached_entry(path)
                    path.unlink();report.unlink()

    def test_blender_replacement_changes_surface_and_comfy_cache_identities(self):
        spec=importlib.util.spec_from_file_location('shield_build_bridge',TOOLS/'trellis2_shield_nodes/__init__.py')
        bridge=importlib.util.module_from_spec(spec);spec.loader.exec_module(bridge)
        with tempfile.TemporaryDirectory() as temporary,patch.object(bridge.subprocess,'run',return_value=SimpleNamespace(stdout='Blender 4.0.0')):
            executable=Path(temporary)/'blender';executable.write_bytes(b'build-one')
            first=bridge.blender_identity(executable)
            executable.write_bytes(b'build-two');second=bridge.blender_identity(executable)
            other=Path(temporary)/'other-blender';other.write_bytes(b'build-two');third=bridge.blender_identity(other)
            self.assertNotEqual(first['sha256'],second['sha256']);self.assertNotEqual(second['path'],third['path'])
            v,f=box();keys={bridge.surface_cache_key(v,f,.003,runtime) for runtime in (first,second,third)}
            self.assertEqual(len(keys),3)
            identifiers=[]
            for runtime in (first,second,third):
                with patch.object(bridge,'settings',return_value=(runtime['path'],Path(temporary),runtime)):
                    identifiers.append(bridge.SWLORClosedVoxelMesh.IS_CHANGED(None,.003,3500))
            self.assertEqual(len(set(identifiers)),3)

    def test_shield_workflow_binds_budget_and_same_post_uv_basis_to_every_bake(self):
        spec=importlib.util.spec_from_file_location('shield_generation_runner',TOOLS/'GenerateWeaponModels.py')
        runner=importlib.util.module_from_spec(spec);spec.loader.exec_module(runner)
        workflow=json.loads((TOOLS/'workflows/trellis2_shield.json').read_text())
        graph=runner.build_graph(workflow,{'seed':5,'target_faces':1750})
        self.assertEqual(graph['186']['class_type'],'SWLORClosedVoxelMesh')
        self.assertEqual(graph['186']['inputs']['target_face_count'],1750)
        self.assertEqual(graph['186']['inputs']['mesh'],['241',0])
        self.assertEqual(graph['241']['inputs']['sign_mode'],'udf')
        self.assertEqual(graph['196']['inputs']['mesh'],['186',1])
        self.assertEqual(graph['951']['inputs'],{'mesh':['196',0],'crease_angle':0})
        for node,key in [('210','mesh'),('147','mesh'),('224','low_poly')]:self.assertEqual(graph[node]['inputs'][key],['951',0])
        self.assertEqual(graph['224']['inputs']['high_poly'],['186',0])
        self.assertEqual(graph['210']['inputs']['normal_map'],['952',0])
        self.assertEqual(graph['952']['class_type'],'SWLORNormalUNorm8')

    @unittest.skipUnless(HAS_BLENDER,'Run Blender4.0 for surface reconstruction')
    def test_thin_body_raised_boss_and_rear_grip_remain_closed_after_collapse(self):
        source=combine(box(),box((0,0,.022),(.18,.25,.035)),box((0,0,-.03),(.04,.2,.04)))
        high_v,high_f,high=surface.voxel_surface(*source,voxel_size=.008)
        low_v,low_f,low=surface.decimate_surface(high_v,high_f,target_faces=500)
        surface.require_closed(high);surface.require_closed(low)
        self.assertLessEqual(len(low_f),500);self.assertEqual(len(low['shells']),1)
        self.assertGreater(low['shells'][0]['signed_volume'],0)
        self.assertLess(np.max(abs(np.asarray(low['bounds_min'])-source[0].min(axis=0))),.02)
        self.assertLess(np.max(abs(np.asarray(low['bounds_max'])-source[0].max(axis=0))),.02)
        self.assertGreater(np.ptp(low_v[:,2]),.05)

    @unittest.skipUnless(HAS_BLENDER,'Run Blender4.0 for touching surface repair')
    def test_touching_thin_solids_and_small_floating_shell_are_repaired_without_holes(self):
        source=combine(box((-.15,0,0),(.3,.6,.03)),box((.15,0,0),(.3,.6,.03)),box((.4,0,0),(.04,.04,.04)))
        self.assertGreater(surface.audit_surface(*source)['bad_edges'],0)
        high_v,high_f,high=surface.voxel_surface(*source,voxel_size=.006)
        low_v,low_f,low=surface.decimate_surface(high_v,high_f,target_faces=200)
        surface.require_closed(high);surface.require_closed(low)
        self.assertEqual(len(low['shells']),2)
        self.assertTrue(all(shell['signed_volume']>0 for shell in low['shells']))
        self.assertGreater(low_v[:,0].max(),.4)


if __name__=='__main__':unittest.main(argv=[__file__])
