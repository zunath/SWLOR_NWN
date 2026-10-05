from pathlib import Path
import os
import subprocess
import unittest
import yaml


class WorkflowTests(unittest.TestCase):
    def setUp(self):
        path = Path('/repo/.github/workflows/request-production-deployment.yml')
        self.workflow = yaml.load(path.read_text(), Loader=yaml.BaseLoader)
        self.script = self.workflow['jobs']['authorize']['steps'][0]['run']
        self.env = dict(os.environ, REQUEST_REPOSITORY='zunath/SWLOR_NWN', REQUEST_ACTOR='zunath',
                        TRIGGERING_ACTOR='zunath', REQUEST_BRANCH='refs/heads/master',
                        RELEASE_COMMIT='a'*40, NWSYNC_HASH='b'*40)

    def test_required_inputs_and_credential_free_signal(self):
        self.assertEqual(self.workflow['permissions'], {})
        inputs = self.workflow['on']['workflow_dispatch']['inputs']
        self.assertEqual(set(inputs), {'release_commit','nwsync_hash'})
        self.assertTrue(all(value['required'] == 'true' for value in inputs.values()))
        self.assertEqual(self.workflow['run-name'],'Deploy production ${{ inputs.release_commit }} ${{ inputs.nwsync_hash }}')

    def test_valid_owner_request_passes(self):
        result = subprocess.run(['bash','-c',self.script],env=self.env,capture_output=True)
        self.assertEqual(result.returncode,0,result.stderr)

    def test_other_actors_branches_repos_and_malformed_inputs_fail(self):
        for key,value in (('REQUEST_ACTOR','someone-else'),('TRIGGERING_ACTOR','someone-else'),
                          ('REQUEST_BRANCH','refs/heads/feature'),('REQUEST_REPOSITORY','other/repo'),
                          ('RELEASE_COMMIT','master'),('NWSYNC_HASH','b'*40+'; reboot')):
            result = subprocess.run(['bash','-c',self.script],env=dict(self.env,**{key:value}),capture_output=True)
            self.assertNotEqual(result.returncode,0,key)


if __name__ == '__main__': unittest.main(verbosity=2)
