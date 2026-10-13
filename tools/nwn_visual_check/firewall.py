"""Read effective firewall state through structured, language-neutral properties."""
from pathlib import Path
import json
import os
import subprocess


def require_firewall():
    """Require all three effective firewall profiles without parsing display text."""
    powershell=Path(os.environ['SystemRoot'])/'System32/WindowsPowerShell/v1.0/powershell.exe'
    command="$ErrorActionPreference='Stop'; @(Get-NetFirewallProfile -PolicyStore ActiveStore | ForEach-Object { [int]$_.Enabled -eq 1 }) | ConvertTo-Json -Compress"
    result=subprocess.run([str(powershell),'-NoProfile','-NonInteractive','-Command',command],
                          check=True,capture_output=True,text=True,timeout=30,
                          creationflags=subprocess.CREATE_NO_WINDOW)
    states=json.loads(result.stdout)
    if not isinstance(states,list) or len(states)!=3 or any(state is not True for state in states):
        raise RuntimeError('All Windows firewall profiles must enforce network isolation')
