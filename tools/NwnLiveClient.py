"""Run a private SWLOR server with a Linux client in Docker; see InEngineVisualVerification.md."""
import sys

from nwn_live_client.__main__ import main

if __name__ == '__main__':
    sys.exit(main())
