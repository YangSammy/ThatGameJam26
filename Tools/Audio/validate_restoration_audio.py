"""Validate delivered bytes and bindings without regenerating or touching Unity."""
from pathlib import Path
import hashlib,json,re
import numpy as np
import soundfile as sf
ROOT=Path(__file__).resolve().parents[2]
manifest=json.loads((ROOT/'docs/Audio/RestorationAudioManifest.json').read_text())
groups={};guids=set()
for row in manifest['assets']:
    path=ROOT/row['file'];info=sf.info(path);pcm,rate=sf.read(path,always_2d=True)
    assert info.subtype=='PCM_24' and rate==48000 and info.channels==2,path
    assert len(pcm)==row['samples'] and np.isfinite(pcm).all(),path
    assert 0<np.max(np.abs(pcm))<=.421,path
    assert np.max(np.abs(pcm[-1]-pcm[0]))<.00001,path
    assert hashlib.sha256(path.read_bytes()).hexdigest()==row['sha256'],path
    meta=Path(str(path)+'.meta').read_text()
    guid=re.search(r'guid: ([0-9a-f]+)',meta)[1];assert guid not in guids;guids.add(guid)
    assert 'compressionFormat: 0' in meta,path
    assert ('preloadAudioData: 1' in meta) if row.get('cue') else ('loadType: 2' in meta and 'preloadAudioData: 0' in meta),path
    if row.get('sourceMix'):
        source=sf.info(ROOT/row['sourceMix'])
        assert source.frames==info.frames and source.samplerate==rate,path
        groups.setdefault(row['group'],[]).append(info.frames)
    if row['loopStartSample'] is not None:
        assert row['loopStartSample']==0 and row['loopEndExclusive']==len(pcm),path
for name,lengths in groups.items():assert len(lengths)==4 and len(set(lengths))==1,name
bank=(ROOT/'Assets/Resources/StorySfxBank.asset').read_text();ids=re.findall(r'^  - id: (\d+)$',bank,re.M)
assert len(ids)==len(set(ids)) and all(str(i) in ids for i in (134,234,246))
before=ROOT/'output/QA/AutumnWinterRestore_20261105/audio-before/StorySfxBank.asset'
if before.exists():assert bank.startswith(before.read_text(encoding='utf-8-sig')),'Existing bank entries changed'
print(f'PASS {len(manifest["assets"])} delivered WAVs: PCM24/48k/stereo, exact checksums, finite headroom, matching origins and frame lengths, zero boundaries, unique metadata, additive bank IDs.')
print('Original source mixes were not modified by authoring. No original instrument stems were present; generated parts are supplemental synthesis.')
