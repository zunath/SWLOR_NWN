"""Scripted live smoke run; see SWLOR.Game.Server/Readmes/InEngineVisualVerification.md ("Smoke run")."""
import sys

from nwn_live_client.smoke import main

if __name__ == '__main__':
    sys.exit(main())
