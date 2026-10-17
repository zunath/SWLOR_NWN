"""Command line for the live-server client stack; see stack.py and InEngineVisualVerification.md."""
from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path

from .stack import LiveStackError, Stack, capture, create, destroy, log, play, project_resources, start


def parser() -> argparse.ArgumentParser:
    root = argparse.ArgumentParser(
        prog='NwnLiveClient.py',
        description='Private SWLOR server plus a Linux NWN:EE client in Docker for live-server client '
                    'evidence. Every command after "up" takes the run directory that "up" prints.')
    commands = root.add_subparsers(dest='command', required=True)

    up = commands.add_parser('up', help='Create a run directory, start the server, then the client.')
    up.add_argument('--character', help='Player .bic placed in the server vault (player mode).')
    up.add_argument('--dm-avatar', help='DM avatar .bic copied into the client dmvault (DM mode, -dmc).')
    up.add_argument('--public-cdkey', help='8-character public CD key (or SWLOR_LIVE_PUBLIC_CDKEY). Names '
                                           'the server-vault folder and, for DM runs, SWLOR_SUPER_ADMIN_CD_KEY.')
    up.add_argument('--cdkey', help='cdkey.ini to copy into the run profile (default: Documents\\Neverwinter '
                                    'Nights\\cdkey.ini). The copy is deleted at teardown.')
    up.add_argument('--name', help='Project label; the project is always "swlor-live-<label>".')
    up.add_argument('--run-dir', help='Directory to create outside the repository; it must not exist yet '
                                      '(default: %%TEMP%%\\swlor-live\\<project>).')
    up.add_argument('--dotnet', help='Server build output (default: SWLOR.Game.Server/bin/<configuration>/net10.0).')
    up.add_argument('--configuration', default='Release', help='Build configuration for the default --dotnet.')
    up.add_argument('--module', help='.mod file (default: <deploy home>/modules/Star Wars LOR v2.mod).')
    up.add_argument('--hak-dir', help='Deployed hak folder (default: debugserver/hak of the main checkout).')
    up.add_argument('--tlk-dir', help='Deployed TLK folder (default: debugserver/tlk of the main checkout).')
    up.add_argument('--client-install', help='NWN:EE install containing bin/linux-x86/nwmain-linux '
                                             '(default: NWN_LINUX_CLIENT_INSTALL or the Steam install).')
    up.add_argument('--server-env-file', help='Base server env file (default: SWLOR.Game.Server/Docker/swlor.env).')
    up.add_argument('--server-env', action='append', metavar='KEY=VALUE',
                    help='Extra server environment, for example an environment-gated test driver switch.')
    up.add_argument('--player-name', default='SwlorLive', help='Client player (login) name.')
    up.add_argument('--no-seed', action='store_true', help='Do not seed the Player record for --character.')
    up.add_argument('--play', action='store_true', help='Click Play at character select after the client starts.')
    up.add_argument('--settle', type=float, default=45,
                    help='With --play: seconds without a new area transition before returning.')
    up.add_argument('--server-timeout', type=float, default=900, help='Seconds to wait for the module to load.')
    up.add_argument('--max-minutes', type=float, default=120,
                    help='Lifetime limit after which the server and client stop themselves.')
    up.add_argument('--keep-on-failure', action='store_true',
                    help='Leave a failed stack running for inspection instead of tearing it down.')

    run_dir = argparse.ArgumentParser(add_help=False)
    run_dir.add_argument('run_dir', type=Path, help='Run directory printed by "up".')

    played = commands.add_parser('play', parents=[run_dir], help='Click Play until the character enters the module.')
    played.add_argument('--settle', type=float, default=45,
                        help='Seconds without a new area transition before returning.')

    ui = commands.add_parser('ui', parents=[run_dir],
                             help='Send input on the private display: move X Y, click X Y, rightclick X Y, '
                                  'doubleclick X Y, drag X1 Y1 X2 Y2, key KEYSYM, type TEXT.')
    ui.add_argument('action', choices=('move', 'click', 'rightclick', 'doubleclick', 'drag', 'key', 'type'))
    ui.add_argument('arguments', nargs='*')

    shot = commands.add_parser('capture', parents=[run_dir], help='Save <run>/evidence/<label>.png.')
    shot.add_argument('label')

    console = commands.add_parser('console', parents=[run_dir], help='Send one nwserver console command.')
    console.add_argument('text', nargs='+')

    commands.add_parser('status', parents=[run_dir], help='Show containers, joins and the run state.')
    commands.add_parser('down', parents=[run_dir],
                        help='Boot the player, remove the project, then delete the key, profile and home.')
    return root


def main(argv: list[str] | None = None) -> int:
    options = parser().parse_args(argv)
    try:
        if options.command == 'up':
            stack = create(options)
            try:
                start(stack, options.server_timeout)
                if options.play:
                    play(stack, options.settle)
            except BaseException:
                if options.keep_on_failure:
                    log(f'Start-up failed; stack left running. Tear down with: '
                        f'python tools/NwnLiveClient.py down "{stack.run_dir}"')
                else:
                    log('Start-up failed; tearing the stack down')
                    destroy(stack)
                raise
            log(f'Ready. Run directory: {stack.run_dir}')
            log(f'Tear down with: python tools/NwnLiveClient.py down "{stack.run_dir}"')
            return 0

        stack = Stack(options.run_dir)
        if options.command == 'play':
            play(stack, options.settle)
        elif options.command == 'ui':
            stack.ui(options.action, *options.arguments)
        elif options.command == 'capture':
            capture(stack, options.label)
        elif options.command == 'console':
            stack.console(' '.join(options.text))
        elif options.command == 'status':
            print(json.dumps({
                'project': stack.project,
                'mode': stack.state['mode'],
                'server_running': stack.running('swlor-server'),
                'client_running': stack.running('client'),
                'players_joined': stack.joined_players(),
                'docker_resources': {kind: len(ids) for kind, ids in project_resources(stack.project).items()},
            }, indent=2))
        elif options.command == 'down':
            report = destroy(stack)
            print(json.dumps(report, indent=2))
            return 0 if report['clean'] else 1
        return 0
    except LiveStackError as error:
        print(f'[nwn-live] ERROR: {error}', file=sys.stderr)
        return 1


if __name__ == '__main__':
    sys.exit(main())
