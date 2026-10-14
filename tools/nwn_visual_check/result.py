from dataclasses import dataclass


@dataclass
class ClientRun:
    returncode: int
    output: str
    hidden: bool
    stopped: bool = False
