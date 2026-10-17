# In-engine visual verification

The shared procedure for NWN client evidence. Read it before verifying any change.

**Every change with an in-game effect needs evidence from the actual NWN:EE client before handoff, unless the user explicitly says otherwise.** A change with no in-game surface is the only exception (see the last route below). Show the screenshots to the user and describe the validation in the pull request. Static previews, coordinate audits, builds, and tests supplement this evidence; they never replace it. Pick the route by what produces the change:

- **Offline launcher** (most of this guide): anything the module and haks render without the C# server, such as placements, models, textures, equipment, VFX, and areas. Inspect every changed placement and the relevant appearances or states.
- **Live server** ("Server behavior" below): anything the C# server produces, such as combat, abilities, NUI windows, conversations, chat commands, migrations, and economy behavior.
- A change with no in-game surface (toolset app, CLI, docs, agent rules) has nothing to capture; say so in the handoff.

Sections: Hard rules · Run the launcher · Build a faithful fixture · Capture · Server behavior · Client traps · Public access · Evidence and cleanup

## Hard rules

1. **Use only the established procedures**: the repository launcher, `python tools/VerifyNwnVisuals.py` (backed by `tools/nwn_visual_check/`), for offline captures, and the containerized client in "Server behavior" for live checks. Never launch the Windows `nwmain.exe` directly, write your own launcher or AppContainer/desktop code, or copy helpers from another repository. If a procedure fails, report the error and mark the affected items unverified. There is no visible fallback.
2. **No focus or real input.** Never call `SetForegroundWindow` or `SetCursorPos`, send real mouse or keyboard events, switch desktops, or show the client window. Script camera and movement in the fixture's NWScript probe.
3. **Silence belongs to the isolated profile.** The launcher zeroes all four volume channels and disables splash and intro movies, which play audio even at zero volume. Never change the user's `settings.tml`, Windows mixer, or audio devices. If sound is heard, stop the owned run and fix its profile.
4. **Own only your process.** Record the launcher's client PID and confirm it exits on success or failure. Never terminate `nwmain.exe` by name; another agent or the user may be running a client.
5. **Keep generated files outside the repository.** Fixture sources, HAK copies, evidence, and reports go in a task-owned temp directory such as `C:\Temp\review\`. Keep the evidence after cleanup and leave `git status` clean.

## Run the launcher

Prerequisites: Windows 10+, Python 3.12+, an installed NWN:EE client, and Pillow (`python -m pip install -r tools/nwn_visual_check/requirements.txt`). The client is found in the usual Steam/GOG locations; otherwise set `NWN_CLIENT` or pass `--client <path>\bin\win32\nwmain.exe`. GFF/ERF/script tools are found in `tools/SWLOR.CLI`, `SWLOR_Haks`, then PATH, or passed with `--tools`. Every Windows firewall profile must be enabled; the launcher checks this without changing it. `python -B tools/VerifyNwnVisuals.py --help` lists the options.

```powershell
python tools/VerifyNwnVisuals.py `
  --module C:\Temp\review\visual_review.mod `
  --hak-dir C:\Temp\review\hak --tlk C:\Temp\review\sw_tlk.tlk `
  --output C:\Temp\review-evidence `
  --capture "VISUAL_CLOSE_READY=close" --capture "VISUAL_OVERVIEW_READY=overview" `
  --complete-marker VISUAL_REVIEW_COMPLETE --timeout 120
```

- `--output` must be a new directory for every run, including recaptures. `--timeout` is 1 to 600 seconds.
- `--sources <dir>` replaces `--module` and builds a flat fixture directory (`module.ifo.json`, ARE/GIT/GFF JSON, NSS, binary resources): JSON through `nwn_gff`, NSS compiled against the client with `--no-ovr`, packed with `nwn_erf`. Point it only at fixture resources, never the production module. `nwn_visual_check.module.pack_hak(source, output, tools)` packs current source HAK folders.
- Keep tileset `.set` files CRLF (the HAK repository's `*.set text eol=crlf` rule), including files read straight from Git objects. LF-only SET files overrun the native loader's stack buffer (`0xc0000409`) before the module starts; the module and HAK builders reject them before packing.
- Each `--capture` marker must match a complete `WriteTimestampedLogEntry` payload and triggers a fresh 1280x720 native screenshot. Markers and labels must be unique. Two pending uncaptured markers, a blank frame, an early exit, a missing marker, or a timeout fails the run.
- The output directory receives native PNGs, `client.log`, `isolation.json`, and `manifest.json` (input and capture hashes, PID, completion and cleanup status). The manifest proves capture and isolation, not visual correctness.
- The launcher copies inputs into a fresh key-free profile on an unused port, stops the owned process, and removes the profile when the run ends. If cleanup fails, it records the leftovers under `%LOCALAPPDATA%/SWLOR/NwnVisualChecks/cleanup` and recovers them on the next run (or via `offline_client.retry_retained_processes()`). Never delete AppContainer profiles or ACLs by hand; report unresolved cleanup errors.
- The CLI has no profile overrides, Player-DM login, or arbitrary window messages. Report DM-only UI and keyholing customization as unverified instead of editing the managed profile or building another launcher.
- Launcher self-tests: set `SWLOR_NATIVE_ISOLATION_TESTS=1`, run `python -B -m unittest discover -s tools/tests -p test_nwn_visual_check.py -v`, then clear the variable.

## Build a faithful fixture

1. Record the source commit, changed subjects, area resrefs, tags, authored XYZ and facing, and SHA-256 hashes of the inspected source files.
2. Make a flat fixture folder with `module.ifo.json`, the affected area's real `Module/are` and `Module/git` resources (plus GIC when needed), required blueprints, and the probe `.nss`. Keep the area geometry and all placed objects, and the subject's authored position, facing, appearance, and equipment. An empty test room cannot validate world placement.
3. Build or copy the HAKs named by `Module/ifo/module.ifo.json`, with the matching custom TLK, from current assets. An installed HAK may predate the change.
4. Without NWNX, an offline fixture may disable server event scripts and conversation dispatch in the temporary copies; document it. Never alter collision or move scenery to make a placement look valid. Behavior that needs the server needs a server check as well.
5. Hook the temporary module's OnClientEnter (or equivalent) to the probe; C# does not run offline, so an unhooked probe emits no markers. Keep instrumentation out of production content.

Read source JSON in its actual encoding (the module IFO uses a legacy encoding), and never rewrite production JSON through a serializer to build a fixture.

## Capture

- Set each camera after the area load settles and wait for a fresh render before emitting its marker. Timings such as camera at 4 s, marker at 7 s, overview at 10 s, marker at 13 s are examples, not proof: inspect images for loading screens, stale frames, wrong angles, and occlusion, and recapture failures.
- Emit each marker as the entire payload (`WriteTimestampedLogEntry("VISUAL_CLOSE_READY")`), hold the view a few seconds, and never emit two markers back-to-back. End with `VISUAL_REVIEW_COMPLETE` after the final view is held. Use the same strings in the launcher arguments.
- Log the subject's `GetPosition` and `GetFacing` at capture and compare them with the authored data. The engine moves creatures off blocked coordinates; an adjusted spawn does not verify the requested one. Correct the source and recapture.
- Capture at least a clear close view and a contextual overview for each moved NPC or object. Keep the native PNGs; contact sheets may resize copies but must link the originals. Do not edit away clipping or obstacles.

## Server behavior

The offline launcher cannot run C#, so server behavior needs a private server with a client connected to it. `python tools/NwnLiveClient.py` (backed by `tools/nwn_live_client/`) runs both in Docker; use only this tool for live checks. Engine tests (`EngineTesting.md`) supplement this; they are not client evidence.

Prerequisites: Docker Desktop, Python 3.12+, a Release build from your worktree (`dotnet build SWLOR.Game.Server\SWLOR.Game.Server.csproj -c Release -p:RunPostBuildEvent=Never`, or `--dotnet <dir>`), the Steam NWN:EE install (it ships `bin/linux-x86/nwmain-linux`; otherwise `--client-install`), the repacked module and deployed haks and TLK (default: the main checkout's `debugserver/`, or `--module`, `--hak-dir`, `--tlk-dir`), your `cdkey.ini`, and your 8-character public CD key from a server log "Joined as Player" line (`--public-cdkey` or `SWLOR_LIVE_PUBLIC_CDKEY`; keep it out of commits and pull requests). `--help` on each command lists the options.

```powershell
python tools/NwnLiveClient.py up --name my-check --character <test character>.bic --play
python tools/NwnLiveClient.py capture <run dir> in-area
python tools/NwnLiveClient.py ui <run dir> click 742 591   # also rightclick, doubleclick, drag, move, key, type
python tools/NwnLiveClient.py console <run dir> status      # one nwserver console line
python tools/NwnLiveClient.py down <run dir>
```

- **Server:** its own Compose project, `swlor-live-<label>`, with no published host ports (the client reaches the server on the project's network, so stacks never collide), a tmpfs Redis, `NWN_PUBLICSERVER=0`, blank passwords, webhooks and tokens, the repository `swlor.env` as the base, your build, the module, and the TLK copied into the run directory (`state.json` records their hashes and the source commit), and read-only module, hak, and TLK mounts. The haks stay shared; `down` fails if the hak folder changed during the run. A created, never-started container named `<project>-reservation` claims the project name on the Docker daemon until `down`, and the run directory must not exist yet; the tool creates it owner-only. Pass environment-gated test-driver switches with `--server-env KEY=VALUE`; driver code stays uncommitted.
- **Development stack only, never production:** the master server is blocked so other sessions' CD-key use cannot lock you out (`[masterserver.key-authentication] mode = "if-reachable"` from `SWLOR.Game.Server/Docker/settings.tml`, plus `extra_hosts` mapping `nwmaster2.beamdog.net`, `nwn.beamdog.net`, and `api.nwn.beamdog.net` to `127.0.0.1`), and NWSync is blank.
- **Character:** `--character` copies the `.bic` into `servervault/<public key>/` (`NWN_SERVERVAULT=1`) and seeds its Player record with pending initialization, so the server runs normal first-login setup on that copy. The `.bic` needs an object UUID; `--no-seed` skips the seed.
- **Client:** the Linux client in its own container on a private Xvfb display, with all volumes at zero, movies off, a 1280x720 window at UI scale 1.0, HAK and TLK aliases pointing at the mounted haks, chat-window logging, and a copied `cdkey.ini`. `ui` input (xdotool) never reaches the user's desktop. Character select can take several minutes to appear while the client loads the haks; `--play`, or `play <run dir>`, clicks Play at (742,591) every 20 seconds until SWLOR's Connection log shows the character entering (the server log's "Joined as" line is written at connection, before character select), then waits until no area transition has started for `--settle` seconds (default 45). A slow first render can still show the loading screen, so inspect every capture.
- **DM checks:** `--dm-avatar <dmvault .bic>` instead of `--character` launches `-dmc +connect <server> +password <per-run password>`, with `NWN_DMPASSWORD` set and `SWLOR_SUPER_ADMIN_CD_KEY` set to your public key so DM authorization accepts you. Play stays disabled until an avatar is selected, so `play` clicks the first list row before each Play click.
- **Test conditions:** live areas spawn hostiles. Clear creatures from the test areas and wait until the character is out of combat before acting; log combat state with each step. The server's automated restart at 10:00 UTC boots everyone; do not span it.
- **Lifetime and teardown:** the server and client stop themselves after `--max-minutes` (default 120), and a failed `up` tears itself down unless `--keep-on-failure`. `down` boots the character through the server console first (a killed client holds its CD key for 60 seconds), stops the client, runs `docker compose -p <project> down --volumes --remove-orphans` on its own project only, deletes the copied key, profile, and server home, and writes `evidence/cleanup.json`. It exits non-zero if any container, volume, network, key, or character copy remains.
- **Evidence:** `<run dir>/evidence` (default run directory `%TEMP%\swlor-live\<project>`) keeps the captures, `ui-actions.log`, client and server logs, and `cleanup.json`. If the stack cannot start or a capture is incomplete, report the affected items as unverified with the error.

## Client traps

- Offline `+TestNewModule` runs need no CD key. Never copy the user's credentials into an offline profile.
- `-dmc` ignores `+TestNewModule` and plays the intro movies; do not add it to offline runs.
- Do not resize the client window; a DPI-unaware resize at 150% scaling produced black captures.
- Do not capture during area loads or transitions or in a tight loop; both have frozen the client.
- Offline captures do not establish server or NUI behavior.

## Public access

A good-looking placement can still be unreachable. For each moved NPC, start on a known public route outside the surrounding enclosure and walk normally to interaction distance (about 1.5 m), adding approaches when walls, doors, or scenery make access ambiguous. Record the start coordinates and final `GetDistanceBetween`. Never teleport beside the NPC or through walls for the tested approach; any collision bypass used to place the camera must end before walking. State separately whether doors, faction travel, and live conversation dispatch were tested. (This caught an NPC reachable only from inside an enclosed strip.)

## Evidence and cleanup

- Save a manifest mapping each subject to source hashes, authored and observed transforms, screenshot filenames and hashes, camera settings, approach results, client PID, isolation metadata, and completion status. Keep earlier captures and logs when recapturing.
- Inspect every final image: identity, orientation, ground contact, clearance from furniture and walls, and the surrounding route. Evidence is valid only for unchanged source and assets; recapture after any later visual change.
- Give the user a labelled image for every changed placement, links to the original captures, and explicit limitations. Show the images or open the report; do not merely say screenshots were taken.
- In the pull request, describe method, coverage, approach results, limitations, and artifact location, marked as local (reviewers cannot open local paths).
- Repack the production module after final corrections, extract its GIT resources, and verify final placements against the inspected plan. Report packing and live deployment separately.
- If the client cannot run or a capture is incomplete, report the affected items as unverified with the concrete blocker. Never substitute an illustration, toolset view, or another subject's screenshot.
