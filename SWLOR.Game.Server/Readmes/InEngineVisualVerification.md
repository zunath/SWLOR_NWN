# In-engine visual verification

This is the single shared procedure for every AI agent, including Codex/GPT and
Claude. Read it before any NWN client visual verification. The root `AGENTS.md`
requires this workflow; Codex reads that file directly and `CLAUDE.md` imports it.
Keep the procedure here rather than maintaining agent-specific copies.

Visual changes require inspection in the actual Neverwinter Nights: Enhanced
Edition client **running in the background through the isolated private-desktop
workflow** before handoff. Never bring the client to the foreground, take user
focus, or switch the user's desktop during this process. This applies to NPC
and object placements,
models, textures, equipment, VFX, and in-game UI. Inspect every changed placement
and the relevant appearances or states of other visual changes. Include the
screenshots in the user report and describe the validation in the pull request.
Static previews, coordinate audits, builds, and automated tests supplement this
evidence; they do not prove the final in-game appearance.

## Hard rules for every agent

1. **Use only the repository launcher:** `python tools/VerifyNwnVisuals.py`, backed
   by `tools/nwn_visual_check/`. Do not launch `nwmain.exe` directly, write your
   own launcher or AppContainer/desktop implementation, or copy helpers from
   another repository. If the launcher fails, report the concrete error and
   affected items as unverified. There is no visible fallback.
2. **No focus or real input.** Never call `SetForegroundWindow`, `SetCursorPos`,
   or send real mouse/keyboard events. Never switch desktops or show the client
   window. The launcher owns an undisplayed private desktop. Its capture helper
   posts messages only to the owned HWND; these do not enter the user's input
   queue. Script camera and movement checks in the fixture's NWScript probe.
3. **Silence belongs to the isolated profile.** The launcher zeroes all four
   volume channels and disables splash/intro movies, which can play audio even
   at zero volume. Never change the user's normal `settings.tml`, Windows mixer,
   or audio devices. If sound is heard, stop the owned run and fix its profile.
4. **Own only your process.** Record the launcher's client PID and confirm it
   exits on success or failure. Never terminate `nwmain.exe` by name; another
   agent or the user may have a separate client running.
5. **Keep generated files outside the repository.** Put fixture sources, HAK
   copies, evidence, and reports in a task-owned temporary directory such as
   `C:\Temp\review\`. Keep evidence after cleanup and leave `git status` clean.

## Standard background client workflow

Background client execution is mandatory. The required runtime is shipped in
`tools/nwn_visual_check/`, with the command `tools/VerifyNwnVisuals.py`. It includes
the private desktop, zero-capability AppContainer, owned profile, module conversion
and native screenshot helpers previously borrowed from Xenomech. No other checkout,
agent session, account or machine-specific helper path is required. Source commit
and helper hashes are recorded in `tools/nwn_visual_check/provenance.json`.

Prerequisites: Windows 10 or newer, Python 3.12 or newer, an installed NWN:EE client,
and Pillow:

```powershell
python -m pip install -r tools/nwn_visual_check/requirements.txt
python -B tools/VerifyNwnVisuals.py --help
```

The client is discovered in the usual Steam/GOG locations. Set `NWN_CLIENT` or pass
`--client C:\path\to\bin\win32\nwmain.exe` for another installation. For module
building, GFF/ERF/script tools are discovered in `tools/SWLOR.CLI`, `SWLOR_Haks`,
then PATH; `--tools` accepts an explicit tool directory. Missing prerequisites
fail with a named error. Every Windows firewall profile must be enabled; the
launcher checks this without changing firewall settings.

Author a task-owned review module with named log markers at each settled camera
view, then run from any copy of this repository:

```powershell
python tools/VerifyNwnVisuals.py `
  --module C:\Temp\review\visual_review.mod `
  --hak-dir C:\Temp\review\hak --tlk C:\Temp\review\sw_tlk.tlk `
  --output C:\Temp\review-evidence `
  --capture "VISUAL_CLOSE_READY=close" --capture "VISUAL_OVERVIEW_READY=overview" `
  --complete-marker VISUAL_REVIEW_COMPLETE --timeout 120
```

`--output` must be a new directory for each run, including recaptures. The client
timeout must be greater than zero and at most 600 seconds.

Use `--sources C:\Temp\review\module-src` instead of `--module` to build a flat
directory containing `module.ifo.json`, ARE/GIT/GFF JSON, NSS and binary resources.
JSON resources are converted through `nwn_gff`; NSS is compiled against the installed
client with `--no-ovr`; resources are packed with `nwn_erf`. The Python API also
exposes `nwn_visual_check.module.pack_hak(source, output, tools)` for current source
HAK folders. Do not point `--sources` at the entire production module or an arbitrary
directory: it must contain only the fixture resources intended for that review.

The command copies module/HAK/TLK inputs into a fresh, key-free temporary profile,
sets every sound channel to zero, disables splash/movies and background pausing,
selects an unused non-default port, then launches the client with a bounded lifetime
on an undisplayed desktop. Shared-file hardlinks and reparse points are refused:
granting the AppContainer ACL to a hardlink would also affect the source file.
The child is assigned at creation to a non-inherited kill-on-close Windows job, and
only its standard-I/O handles are inherited. Job termination/closure backs up direct
process termination. It never changes the installed client or the user's normal
profile. Native capture requests address only the task-owned HWND. There is no
visible launch fallback.

Each marker matches a complete log payload (the text passed to
WriteTimestampedLogEntry) and requests a fresh 1280x720 native screenshot. Earlier
messages containing that text as a substring do not trigger capture or completion.
Markers and output labels
must be unique, and views must be held long enough to capture them; multiple pending
uncaptured markers are rejected instead of photographing the latest view under old
labels. A blank frame, early exit, missing marker or timeout fails the run.

The new output directory receives native PNGs, `client.log`, `isolation.json`, and
`manifest.json` with input/capture hashes, PID, completion/failure and profile-cleanup
status. Primary run errors and any evidence/cleanup errors are reported together.
On success or failure, the owned process is stopped and the temporary profile
and copied archives are removed once process exit is confirmed. If Windows refuses
all shutdown methods, the launcher retains retryable control handles and the profile;
`offline_client.retry_retained_processes()` retries those owned handles and removes
the retained profile once exit is confirmed. The CLI makes a final retry during
teardown. Failed AppContainer deletions receive three bounded retry attempts and
are saved under `%LOCALAPPDATA%/SWLOR/NwnVisualChecks/cleanup` for recovery on the
next invocation, including from another repo copy. Only matching tool-owned
container names are recovered. Failed temporary-profile removals also retain the
original ownership token and path there. Recovery requires the same ownership marker
and refuses deletion while a recorded PID remains active. Ownership markers survive
partial removal, and isolation evidence is refreshed after a shutdown retry.
The failure is recorded, and cleanup
never discards control of a live client. Evidence stays outside the repository for inspection.
The manifest proves capture and isolation, not visual correctness: inspect every image,
record observed transforms/approach results, and deliver the labelled report below.

Focused checks, including real Windows network denial, timeout and callback cleanup:

```powershell
$env:SWLOR_NATIVE_ISOLATION_TESTS = '1'
python -B -m unittest discover -s tools/tests -p test_nwn_visual_check.py -v
Remove-Item Env:SWLOR_NATIVE_ISOLATION_TESTS
```

## Build a faithful review fixture

1. Record the source commit, changed subjects, area resrefs, tags, authored XYZ
   and facing, and SHA-256 hashes of the source files being inspected.
2. Make a flat fixture source folder containing `module.ifo.json`, the affected
   area's actual `Module/are` and `Module/git` resources (plus GIC when needed),
   required blueprints, and the probe `.nss` script. Retain area geometry and all
   placed objects. Preserve the subject's authored position, facing, appearance,
   and equipment. An empty
   test room cannot validate world placement.
3. Build or copy the HAKs named by `Module/ifo/module.ifo.json` into the isolated
   profile, with the matching custom TLK. Use current assets, not an unrelated
   installed HAK that may predate the change. Keep large generated archives
   outside the repository.
4. An offline fixture may disable server event scripts and conversation
   dispatch when NWNX is unavailable. Make those changes only to the temporary
   copies and document them. Never alter collision or move scenery to make a
   placement appear valid. A feature needing server behavior requires a
   suitable server check as well; an offline fixture cannot prove that behavior.
5. Set the temporary module's OnClientEnter (or an equivalent hook) to the probe
   script. Without NWNX, C# server logic does not run; an unhooked probe will not
   produce capture markers. Compile the probe and pack a temporary module, or
   let `--sources` do that. Script camera positions and named log markers so each
   captured frame maps to a specific subject. Keep this instrumentation outside
   production content.

Read source JSON using its actual encoding. Most module JSON is UTF-8; the
module IFO used in this check required its legacy encoding. Do not silently
rewrite production JSON through a serializer merely to build a fixture.

## Launch and capture

Use a task-owned, key-free `-userdirectory` profile and an unused embedded
server port. Do not reuse the user's normal profile, credentials, or running
client. The `VerifyNwnVisuals.py` launcher handles this through the repository's
`nwn_visual_check.offline_client.run` helper, with an explicit timeout, `on_start`
to record the PID, and `until` to detect completion. These are runtime internals,
not instructions to write a separate launcher. Keep the existing private-desktop
and zero-capability isolation.
Track only the process created by this task; never terminate clients by name.

Sound must be disabled for the background client instance. The user is
working on the same machine, and a hidden client must never play music,
ambient audio, voices, or effects. NWN:EE has no master sound switch, so set
all four volume channels to zero in the isolated `settings.tml` before launch.
Also disable the intro, splash, and movie playback: those movies play audio
outside the four volume channels, so a client that reaches them is audible even
at zero volume. Mute only the task-owned profile; never change the user's normal
`settings.tml`, Windows volume mixer, or audio devices. If the client is heard
during a run, stop it and fix the profile before relaunching.

The launcher creates the isolated `settings.tml` with the following sound,
movie, window, and focus settings. The optional keyholing setting shown here can
help a fixture's camera; disclose it when used. Do not edit the user's profile:

```toml
[sound.effects]
volume=0.0
[sound.music]
volume=0.0
[sound.streams]
volume=0.0
[sound.voices]
volume=0.0
[graphics.intro.splash]
enabled=false
[graphics.movies]
enabled=false
[graphics.movies.intro]
enabled=false
[graphics.window]
mode="windowed"
[graphics.window.windowed]
width=1280
height=720
[game.auto-pause.on-focus-loss]
enabled=false
[graphics.keyholing]
enabled=true
[graphics.video.vsync]
mode="off"
```

A `-dmc` (DM client) launch ignores `+TestNewModule` and plays the intro movies
on its way to the main menu, so it needs the movie settings above. For DM-UI
checks, a Player DM login works offline instead. In the task-owned profile, set
`[server.login] dm-password` to a one-off value and
`[server.login.player-dm] enabled=true`, then post `##dm_login <password>` to
the owned private-desktop window.

For each subject, set the camera after area loading settles, then wait for a
fresh render before capturing. The capstone script set a close camera at four
seconds, emitted its screenshot marker at seven seconds, then set an overview
at ten seconds and emitted that marker at thirteen seconds. These delays are
examples, not proof of readiness: inspect the images for loading screens,
stale frames, wrong camera angles, and occlusion. Adjust and recapture failures.

Emit each unique marker as the entire `WriteTimestampedLogEntry` payload, for
example `WriteTimestampedLogEntry("VISUAL_CLOSE_READY")`. Hold the settled view
for a few seconds after its marker before setting the next camera. Never emit
two capture markers back-to-back. End the probe with
`WriteTimestampedLogEntry("VISUAL_REVIEW_COMPLETE")` after the final view has been
held. Use the same marker strings in the launcher arguments.

Log the subject's actual `GetPosition` and `GetFacing` at capture time. Compare
them with the authored data: the engine can move a creature away from blocked
coordinates. An engine-adjusted spawn is not verification of the requested
coordinate; correct the source and capture again.

Capture at least a clear close view and a contextual overview for each moved
NPC or object. Preserve original native PNGs at their capture resolution.
Contact sheets may resize copies, but must link to the originals. Camera
keyholing can cut away foreground scenery; disclose it and use an overview
that makes the surrounding layout clear. Do not edit away clipping or obstacles.

## Known client traps and live-server limits

- Offline `+TestNewModule` runs need no CD key. Use the launcher's key-free
  profile and never copy the user's credentials.
- Do not resize the client window. A DPI-unaware resize at 150% scaling caused
  black captures. Keep the launcher's native 1280x720 capture size.
- Do not capture during area loads or transitions or in a tight loop; both have
  frozen the client. Capture only at settled probe markers.
- Do not manually delete AppContainer profiles or ACLs to fix cleanup. Use the
  runtime's ownership-checked recovery described above and report unresolved
  cleanup errors.
- A logged-in client with C# running on a live NWNX server is a separate workflow.
  Do not attempt that client workflow unless asked. It needs the user to click
  Play at character selection; do not inject real input or change desktop/focus
  rules to bypass that step. Server-side integration tests are documented in
  [EngineTesting.md](EngineTesting.md). Offline captures do not establish live
  server or NUI behavior.

## Verify public access separately

A good-looking placement can still be inaccessible. For each moved NPC, start
on a known public route outside the surrounding enclosure and walk normally to
interaction distance. Choose additional public approaches when walls, doors,
or scenery make access ambiguous. Record start coordinates and the final
`GetDistanceBetween` result; the capstone check targeted 1.5 m.

Do not infer public access by teleporting beside the NPC and walking the last
three metres. Do not cross walls by teleport during the tested approach. Any
cutscene ghost or collision bypass used to position the camera must expire or
be removed before walking. Inspect the chosen start and route as well as the
final distance. State separately whether doors, faction travel, and live
conversation dispatch were tested.

This distinction caught a real capstone error: Talan Rees was reachable from
within an enclosed strip but inaccessible from the public spaceport path.
His corrected public-side position was verified from (89,70) and (94,77),
reaching approximately 1.500 m and 1.496 m with normal collision enabled.

## Evidence, delivery, and cleanup

- Save a manifest mapping each subject to its source hashes, authored and
  observed transforms, screenshot filenames and SHA-256 hashes, camera settings,
  approach starts/results, client PID, isolation metadata, and completion status.
  Preserve logs and earlier captures separately when a correction is recaptured.
- Inspect every final image yourself. Confirm identity, orientation, ground
  contact, clearance from furniture/walls, and the surrounding route. Recapture
  after any subsequent visual change; earlier evidence is valid only for
  unchanged source and assets.
- Provide a report with a labelled image for every changed placement, links to
  original captures, and explicit limitations. Show the images or open the
  report for the user; do not merely say screenshots were taken. Keep generated
  reports and captures in the task's artifact directory outside the repository.
- Include the method, coverage, approach results, limitations, and artifact
  location in the PR description. Local artifact paths are not accessible to
  remote reviewers; identify them as local and do not imply they are uploaded.
- Repack the production module after final source corrections. Extract its
  binary GIT resources and verify final placements against the inspected plan.
  Report packing and live deployment separately.
- Stop the owned client even on failure. Confirm its PID has exited, then remove
  only the verified task-owned temporary profile and generated HAKs. Keep the
  evidence and report. Leave the repository clean at handoff.

If the client cannot run or a capture is incomplete, report the affected items
as unverified and give the concrete blocker. Do not substitute an illustration,
toolset view, or another subject's screenshot for engine evidence.
