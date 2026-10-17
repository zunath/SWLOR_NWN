# In-engine visual verification

The single shared procedure for every agent. Read it before any NWN client visual verification.

Visual changes need inspection in the actual NWN:EE client, **running in the background through the isolated private-desktop launcher**, before handoff. This covers NPC and object placements, models, textures, equipment, VFX, and in-game UI. Inspect every changed placement and the relevant appearances or states of other changes. Show the screenshots to the user and describe the validation in the pull request. Static previews, coordinate audits, builds, and tests supplement this evidence; they do not prove the in-game result.

Sections: Hard rules · Run the launcher · Build a faithful fixture · Capture · Client traps · Public access · Evidence and cleanup

## Hard rules

1. **Use only the repository launcher**, `python tools/VerifyNwnVisuals.py` (backed by `tools/nwn_visual_check/`). Never launch `nwmain.exe` directly, write your own launcher or AppContainer/desktop code, or copy helpers from another repository. If the launcher fails, report the error and mark the affected items unverified. There is no visible fallback.
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

## Client traps

- Offline `+TestNewModule` runs need no CD key. Never copy the user's credentials.
- `-dmc` ignores `+TestNewModule` and plays the intro movies; do not add it.
- Do not resize the client window; a DPI-unaware resize at 150% scaling produced black captures.
- Do not capture during area loads or transitions or in a tight loop; both have frozen the client.
- A logged-in client on a live NWNX server is a separate workflow; attempt it only when asked. It needs the user to click Play at character selection; never inject input to bypass that. Offline captures do not establish live server or NUI behavior; server-side tests are in `EngineTesting.md`.

## Public access

A good-looking placement can still be unreachable. For each moved NPC, start on a known public route outside the surrounding enclosure and walk normally to interaction distance (about 1.5 m), adding approaches when walls, doors, or scenery make access ambiguous. Record the start coordinates and final `GetDistanceBetween`. Never teleport beside the NPC or through walls for the tested approach; any collision bypass used to place the camera must end before walking. State separately whether doors, faction travel, and live conversation dispatch were tested. (This caught an NPC reachable only from inside an enclosed strip.)

## Evidence and cleanup

- Save a manifest mapping each subject to source hashes, authored and observed transforms, screenshot filenames and hashes, camera settings, approach results, client PID, isolation metadata, and completion status. Keep earlier captures and logs when recapturing.
- Inspect every final image: identity, orientation, ground contact, clearance from furniture and walls, and the surrounding route. Evidence is valid only for unchanged source and assets; recapture after any later visual change.
- Give the user a labelled image for every changed placement, links to the original captures, and explicit limitations. Show the images or open the report; do not merely say screenshots were taken.
- In the pull request, describe method, coverage, approach results, limitations, and artifact location, marked as local (reviewers cannot open local paths).
- Repack the production module after final corrections, extract its GIT resources, and verify final placements against the inspected plan. Report packing and live deployment separately.
- If the client cannot run or a capture is incomplete, report the affected items as unverified with the concrete blocker. Never substitute an illustration, toolset view, or another subject's screenshot.
