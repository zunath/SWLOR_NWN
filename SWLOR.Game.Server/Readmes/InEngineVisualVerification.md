# In-engine visual verification

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

## Standard background client workflow

Background client execution is mandatory for this process. Use the isolated
background NWN client approach established by Xenomech's
"Resume armor work" session and used for the capstone giver relocation work.
The client renders on a private Windows desktop without taking focus or
switching the user's desktop. Capture the owned client's native render.

The reference implementation lives in Xenomech's
`prototypes/asset-pipeline/`:

- `xmasset/offline_client.py`: bounded AppContainer process lifetime, zero
  network capabilities, no loopback exemption, and isolation evidence.
- `xmasset/private_desktop.py` and its supporting helpers: private desktop
  creation and verification, associated with the owned PID and profile.
- `xmasset/localclient.py`: a separate user directory, module installation,
  embedded-server port selection, and client log access.
- `xmasset/testmodule.py`: JSON-to-GFF conversion and review module resources.
- `client/background_frames.py` and its native capture helpers: fresh frames
  from the owned client window, including native screenshot capture on the
  private desktop.

One reference checkout used during this work was
`C:\Users\Zunath\.codex\worktrees\500b\Xenomech`. This is a machine-specific
reference, not a dependency shipped in SWLOR. Locate an available Xenomech
checkout and inspect the helpers and their imports before using them. Copy the
required helpers into a task-owned temporary workspace; do not edit the
reference checkout. If the reference is unavailable, report that dependency
instead of claiming a preview or synthetic render is in-engine evidence.

SWLOR conversion tools are under `tools/SWLOR.CLI/`: `nwn_gff.exe`,
`nwn_erf.exe`, and `nwn_script_comp.exe`. Discover the installed client path;
the capstone check used the Steam installation's `bin/win32/nwmain.exe`.

## Build a faithful review fixture

1. Record the source commit, changed subjects, area resrefs, tags, authored XYZ
   and facing, and SHA-256 hashes of the source files being inspected.
2. For placement checks, convert the actual `Module/are` and `Module/git`
   resources, retaining area geometry and all placed objects. Preserve the
   subject's authored position, facing, appearance, and equipment. An empty
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
5. Compile a temporary review script and pack a temporary module. Script camera
   positions and named log markers so each captured frame maps to a specific
   subject. Keep this instrumentation outside production content.

Read source JSON using its actual encoding. Most module JSON is UTF-8; the
module IFO used in this check required its legacy encoding. Do not silently
rewrite production JSON through a serializer merely to build a fixture.

## Launch and capture

Use a task-owned, key-free `-userdirectory` profile and an unused embedded
server port. Do not reuse the user's normal profile, credentials, or running
client. Launch through the reference `offline_client.run` helper with an
explicit timeout, `on_start` to record the PID, and `until` to detect completion.
The helper must retain its private-desktop and zero-capability isolation.
Track only the process created by this task; never terminate clients by name.

The capstone run used these client settings in the isolated `settings.tml`:

```toml
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

For each subject, set the camera after area loading settles, then wait for a
fresh render before capturing. The capstone script set a close camera at four
seconds, emitted its screenshot marker at seven seconds, then set an overview
at ten seconds and emitted that marker at thirteen seconds. These delays are
examples, not proof of readiness: inspect the images for loading screens,
stale frames, wrong camera angles, and occlusion. Adjust and recapture failures.

Log the subject's actual `GetPosition` and `GetFacing` at capture time. Compare
them with the authored data: the engine can move a creature away from blocked
coordinates. An engine-adjusted spawn is not verification of the requested
coordinate; correct the source and capture again.

Capture at least a clear close view and a contextual overview for each moved
NPC or object. Preserve original native PNGs at their capture resolution.
Contact sheets may resize copies, but must link to the originals. Camera
keyholing can cut away foreground scenery; disclose it and use an overview
that makes the surrounding layout clear. Do not edit away clipping or obstacles.

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
