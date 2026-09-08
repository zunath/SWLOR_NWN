SWLOR sound compression
=======================

The sound HAK keeps its existing WAV resource names while storing selected effects as MP3 audio with the eight-byte `BMU V1.0` header. This is the same encoding already used by 106 existing WAV resources, including `bf_treefall.wav`. `SoundPreviewService` supports this wrapper. Changing the filenames to `.mp3` would change the resource type and break existing sound lookups.

The first conversion changes 843 uncompressed effects at a 96 kbps target, preserving channels and supported sample rates. The 42 low-rate sources use 64 kbps, and five sources with nonstandard rates use 22,050 Hz. It leaves all already-compressed audio, music, WAV cue/sampler/ACID metadata, known game-configured loops, and six malformed WAV containers unchanged. The exclusions come from placed and blueprint sounds and the 2DA fields used for looping playback; they do not depend on filename guesses.

The WAV collection falls from 129,209,674 bytes (123.22 MiB) to 58,401,578 bytes (55.70 MiB), saving **70,808,096 bytes (67.53 MiB; 54.80%)**. The original assessment's larger savings included game-configured loops that are deliberately retained in PCM. These numbers describe resource/HAK payloads, not Git history, client memory, or actual NWSync download traffic. Git retains the original masters; NWSync compression, deduplication, and the client's existing cache affect transfer sizes.

`SWLOR_Haks/tools/SoundCompressionManifest.json` records the source commit and per-file hashes, sizes, encoding settings, and skip reasons. Preserve this manifest: it links the deployed resources back to their lossless originals. To recover an original, restore that WAV from the manifest's source commit in the HAK repository. The historical loop audit accepts restoration only when the file matches its recorded original hash. Commit restorations and record a new complete inventory manifest using the conversion command with current exclusions; keep the old manifests intact. Do not decode a converted MP3 back to WAV and use that as a new master. If a rollback cannot restore every affected file, the converter retains an incomplete recovery manifest and reports its path and original source commit; recover those files before retrying.

Before a later conversion, regenerate the game loop exclusions from the parent repository:

```powershell
python tools/AuditSoundCompression.py --write-exclusions artifacts/audio-audit/game-loop-exclusions.json
```

The HAK converter defaults to a dry run and accepts an explicit FFmpeg executable. Run this after generating the exclusions, first omitting both `--apply` and `--manifest` to measure the candidate outputs. Use `SoundCompressionManifest-<batch>.json` for each later batch; the first conversion's manifest already exists and must be retained.

```powershell
$soundManifestPath = "SWLOR_Haks/tools/SoundCompressionManifest-$(Get-Date -Format 'yyyyMMdd-HHmmss-fff').json"
python SWLOR_Haks/tools/CompressSoundResources.py --ffmpeg "C:/Program Files/kdenlive/bin/ffmpeg.exe" --exclude-manifest artifacts/audio-audit/game-loop-exclusions.json --manifest $soundManifestPath --apply
```

Its `--apply` switch and exclusion manifest are required to replace audio; it stages and validates encoded payloads first, rechecks the entire WAV inventory before replacement and completion, preserves resource names, and refuses to overwrite an existing provenance manifest. The command above uses the encoder installed on the authoring machine; pass the path to your FFmpeg build with `libmp3lame` support. Run the current-corpus loop check against the resulting manifest:

```powershell
Get-ChildItem SWLOR_Haks/tools/SoundCompressionManifest*.json | ForEach-Object {
    python tools/AuditSoundCompression.py --check $_.FullName
    if ($LASTEXITCODE -ne 0) { throw "A compressed sound is configured to loop." }
}
python -m unittest discover -s tools -p TestSoundCompressionAudit.py
python SWLOR_Haks/tools/TestSoundCompression.py --ffmpeg "C:/Program Files/kdenlive/bin/ffmpeg.exe"
```

Audit every historical conversion manifest for loop references, because a later batch records previously compressed resources as skipped. Validate the complete WAV file list, deployed hashes, and MP3 decoding against the new batch's manifest using the same path:

```powershell
python SWLOR_Haks/tools/CompressSoundResources.py --ffmpeg "C:/Program Files/kdenlive/bin/ffmpeg.exe" --verify-manifest $soundManifestPath
```

To verify the currently committed batch without running a new conversion, set `$soundManifestPath` to `SWLOR_Haks/tools/SoundCompressionManifest.json` first.

Generic media-file sniffing may not recognize very short BMU-wrapped clips; the verifier removes the wrapper and selects the MP3 demuxer explicitly, so this is not confused with a failed audio decode.

Rebuild and distribute **sw_sound.hak** after merging the companion HAK PR and updating the parent pointer. The module already references the same HAK and resrefs, so this audio change requires no module resource edits. Publish the replacement sound resources through the normal HAK/NWSync deployment workflow. Never pack the generator manifests or original-master backups into the sound HAK.

The conversion and archive checks validate bytes, resource identity, decoding, and known loop exclusions. They do not replace listening in the NWN client. Before live rollout, check positional effects at different distances, creature and conversation voices, short impacts, and transitions between ambient sounds. MP3 is lossy and adds encoder delay/end padding; any newly discovered loop or timing-sensitive effect should be restored from the recorded original and excluded from future conversion.
