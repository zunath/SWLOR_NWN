"""Per-container cleanup records shared by later invocations and repo copies."""
from pathlib import Path
import json
import os
import re
import tempfile
from .profile import reject_links


def state_root():
    return Path(os.environ['LOCALAPPDATA'])/'SWLOR/NwnVisualChecks/cleanup'


def validate_name(name):
    if not isinstance(name,str) or not re.fullmatch('swlor-review-[0-9a-f]{32}',name):
        raise ValueError('Cleanup state must name an owned visual-verification container')


def retain_container(name):
    """Atomically retain only the tool's unique container name."""
    validate_name(name)
    root=state_root()
    reject_links(root)
    root.mkdir(parents=True,exist_ok=True)
    temporary=None
    try:
        with tempfile.NamedTemporaryFile(mode='w',encoding='utf8',dir=root,prefix='.write-',suffix='.tmp',delete=False) as stream:
            temporary=Path(stream.name)
            json.dump({'name':name},stream)
            stream.flush()
            os.fsync(stream.fileno())
        temporary.replace(root/(name+'.json'))
    finally:
        if temporary is not None:
            temporary.unlink(missing_ok=True)


def forget_container(name):
    validate_name(name)
    root=state_root()
    reject_links(root)
    (root/(name+'.json')).unlink(missing_ok=True)


def pending_containers():
    """Reject unexpected ownership records rather than deleting arbitrary profiles."""
    root=state_root()
    reject_links(root)
    if not root.exists():
        return []
    names=[]
    for record in sorted(root.glob('*.json')):
        name=json.loads(record.read_text(encoding='utf8')).get('name')
        validate_name(name)
        if record.name != name+'.json':
            raise ValueError('Cleanup record filename does not match its container')
        names.append(name)
    return names
