# Autumn / winter audio restoration

This delivery preserves the supplied stereo music and adds newly synthesized, independently controlled supplemental parts. These are **not original instrument stems**, source separation, or a substitute claim that the composer supplied editable sessions. Tonal support pitches are selected from the source spectrum; all musical part samples are newly synthesized. Mix acceptance still requires listening.

## Inventory and provenance

- `Assets/Music`: 19 delivered stereo mastered WAVs, already documented in MusicIntegration_v3.md. No multitrack sessions, MIDI, or original instrument stems were found in the project inventory. Autumn circle is 46.829 seconds; winter garden and room circles are 53.333 seconds; ward source mix is 30 seconds. Originals are untouched.
- Reusable Foley: `Assets/audio/SFX-001…051.wav`, existing `StoryCues`, and `Resources/ChapterFoley/{Paper,Pencil,Clock}.wav`. Existing IDs and slices are preserved.
- New cue **134**: short envelope paper pickup from `ChapterFoley/Paper.wav`, its complete 0.32-second clip. New **234**: deterministic synthesized suitcase wheel rumble/tread, 2 seconds; specifically distinct from winter box pushing. New **246**: quiet chain hint from `SFX-019.wav`, 0.38–0.72 seconds; distinct from discovery 46/146. Motion 234 is refreshed only during actual suitcase movement and is explicitly stopped on cancellation.
- 36 new stereo, 48 kHz, signed 24-bit PCM WAVs under `Assets/audio/RestorationLayers`: four independent parts for each of autumn, winter garden, winter room and ward; sixteen separate optional release exports; one independent ward room-air layer; three Foley clips. Individual provenance, hashes, sample counts, loop boundaries, peaks, RMS and release policy are in `RestorationAudioManifest.json`.

## Runtime and controls

`GameScore` loads `Resources/GameScoreLayers.asset` without new scene wiring. Existing source mixes remain the main arrangement. Four quietly mixed supplemental roles—motif, harmony, sustain and detail—have separate sources, gains and entry thresholds. All start from sample zero at the **same DSP timestamp** as the corresponding source mix, including silent parts, so adding a layer does not restart it. Lossless PCM streaming avoids decoding all seventeen supplemental tracks into memory at startup; short Foley is preloaded. Source delivery occupies about 224 MB; only the current arrangement streams at runtime.

Autumn density enters during the last fifteen seconds of a started formal draft; the tutorial does not accumulate urgency. Winter garden/room switch with the existing chapter phases and slightly reduce detail when the book is lowered. Ward layer entrances follow distance-driven `WardProgress`; supplemental layers and air sustain with loops during slow walking. The existing 30-second source mix remains a single play. Restored ward checkpoints retain the established no-replayed-onset behavior.

`GameScoreSettings.gain = 0` immediately mutes music without muting `StorySfx` or ward air. `GameScoreLayers.environmentGain` controls ward air separately. Existing global mute/help/focus pause continues to apply to score sources. Pausing before the scheduled start rearms all current parts at a common start on resume. `CutWardNarrative` synchronously stops both the original ward mix and all supplemental/environment sources, including pending scheduled starts. It never touches paper Foley sources.

All new loop files have identical zero-valued endpoint samples and explicit `[0, samples)` boundaries. Source-master loop seams remain those of the delivered masters; they have not been represented as repaired or artist-approved. Supplemental transitions stop the previous parts when the arrangement changes; the main source music keeps its established crossfade. Optional release WAVs are separate delivery assets for editorial use, **not automatically played by the game**. Ward deliberately plays no release after the cut.

## Validation and reproduction

Generator: `uv run --with numpy --with soundfile python tools/Audio/prepare_restoration_layers.py` (deterministic; does not save scenes or refresh Unity). Validator: `uv run --with numpy --with soundfile python tools/Audio/validate_restoration_audio.py`.

Standalone validation passed for all 36 WAVs: format, channel/rate, finite unclipped samples, exact checksums, source-length agreement, zero endpoint boundaries, unique GUID metadata and additive bank entries. Editor callable `MemoryWhitebox.EditorTools.RestorationAudioChecks.Run()` checks imported clips, bindings, layer policy, paused immediate cut, music mute and paper/environment independence; the root integration report records its actual execution result.

Remaining manual acceptance: two loops on headphones/speakers/mono, urgency density and source pitch compatibility, transition clicks, paper and machinery intelligibility with music at normal gain and zero, actual DSP phase coherence after focus pause, and ward cut comfort. Composer original stems/session files, authored loop points for the original mixes and approved musical release tails remain unavailable. These synthesized supplementary parts and optional release exports are clearly distinguished from those missing originals.
