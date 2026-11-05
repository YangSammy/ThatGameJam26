"""Build photo interaction slices from preserved source WAVs. Python3.12 stdlib; no Unity saves touched."""
from pathlib import Path
import argparse, audioop, json, re, uuid, wave
ROOT=Path(__file__).resolve().parents[2]
# Cue, source number, start, duration, playback volume. Semantic sub-cues keep materials distinct.
CUES=[(21,21,0,.50,.32),(22,22,.60,.50,.36),(23,23,.12,.30,.32),
      (123,23,.54,.35,.32),(24,24,.95,.40,.25),(25,25,0,.65,.3),
      (26,26,.42,.18,.22),(27,27,0,.55,.42),(28,28,.85,4.55,.3),
      (29,29,.90,1.40,.3),(129,29,5.35,.70,.32),(30,30,.70,1.0,.32),
      (130,30,4.25,.75,.3),(31,31,12,.80,.32),(131,31,4.45,.50,.32),
      (231,31,10,.4,.28),(331,31,12,.80,.32),(51,51,.02,.45,.22)]
STORY_CUES=[
    (3,3,.41,.23,.20),(103,3,24.58,.84,.15),(19,19,1.64,.72,.27),
    (32,32,7.70,1.14,.22),(33,33,.22,.82,.30),(133,33,4.55,.57,.25),(233,33,3.25,.48,.24),
    (34,34,.34,.41,.25),(35,35,.09,.22,.20),(135,35,16.35,.24,.29),(235,35,42.80,1.90,.33),
    (36,36,1.00,.34,.19),(136,36,7.23,.23,.19),
    (37,37,.70,.52,.20),(137,37,1.00,.45,.23),(38,38,.42,.26,.28),(138,38,.42,.34,.21),
    (39,39,.30,2.80,.31),(40,40,1,.20,.23),(140,40,3.24,.21,.28),(240,40,9.75,.45,.29),
    (41,41,.18,.56,.27),(141,41,1.56,1.95,.26),(42,42,5.09,.32,.31),(142,42,6.01,.29,.24),
    (43,43,1.68,3.25,.25),(44,44,.12,.95,.32),(45,45,.10,.66,.31),(145,45,4.0,2,.27),(245,45,8.33,.85,.31),
    (46,46,.66,1.18,.23),(146,46,1.72,.83,.25),(47,47,.65,.44,.28),
    (48,48,.49,.71,.29),(148,48,1.45,.72,.26),(49,49,1.12,.88,.26),(149,49,2.18,.64,.27),(249,49,1.50,.75,.18),
    (50,50,.67,.94,.30),(150,50,4.51,.48,.27),
]

def guid_meta(path,template):
    meta=Path(str(path)+'.meta')
    if not meta.exists():meta.write_text(re.sub(r'guid: [0-9a-f]+','guid: '+uuid.uuid4().hex,template,count=1),encoding='utf-8')
    return re.search(r'guid: ([0-9a-f]+)',meta.read_text(encoding='utf-8-sig'))[1]
def build_cues(cues, name, prefix):
    folder=ROOT/'Assets/audio'/f'{name}Cues';folder.mkdir(exist_ok=True)
    fm=Path(str(folder)+'.meta')
    if not fm.exists():fm.write_text('fileFormatVersion: 2\nguid: '+uuid.uuid4().hex+'\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n',encoding='utf-8')
    template=(ROOT/'Assets/audio/SFX-006.wav.meta').read_text(encoding='utf-8-sig').replace('preloadAudioData: 0','preloadAudioData: 1')
    rows=[];entries=[]
    for cue,source,start,duration,volume in cues:
        path=next((ROOT/'Assets/audio').glob('SFX-'+str(source).zfill(3)+'*.wav'))
        with wave.open(str(path)) as wav:
            channels=wav.getnchannels();width=wav.getsampwidth();rate=wav.getframerate();wav.setpos(round(start*rate));data=wav.readframes(round(duration*rate))
        peak=audioop.max(data,width);gain=min(12, .50*(2**(width*8-1)-1)/max(1,peak))
        data=audioop.mul(data,width,gain)
        # Short equal-gain edge ramps prevent clicks without changing source WAVs.
        stride=channels*width;frames=len(data)//stride;fade=min(round(rate*.005),frames//2);buf=bytearray(data)
        for i in range(fade):
            for frame in (i,frames-1-i):
                offset=frame*stride;buf[offset:offset+stride]=audioop.mul(data[offset:offset+stride],width,i/max(1,fade))
        output=folder/(prefix+'_'+str(cue)+'.wav')
        with wave.open(str(output),'wb') as wav:
            wav.setnchannels(channels);wav.setsampwidth(width);wav.setframerate(rate);wav.writeframes(buf)
        guid=guid_meta(output,template)
        entries.append(f'  - id: {cue}\n    clip: {{fileID: 8300000, guid: {guid}, type: 3}}\n    start: 0\n    duration: {frames/rate:.6f}\n    volume: {volume}\n')
        rows.append(dict(cue=cue,source=str(path.relative_to(ROOT)).replace('\\','/'),start=start,duration=frames/rate,gain=gain,volume=volume,output=str(output.relative_to(ROOT)).replace('\\','/')))
    bank=ROOT/'Assets/Resources'/f'{name}SfxBank.asset'
    bank.write_text('%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n--- !u!114 &11400000\nMonoBehaviour:\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n  m_GameObject: {fileID: 0}\n  m_Enabled: 1\n  m_EditorHideFlags: 0\n  m_Script: {fileID: 11500000, guid: 4dc94a6c4c5428e4d84caa831c523524, type: 3}\n  m_Name: '+name+'SfxBank\n  m_EditorClassIdentifier: \n  cues:\n'+''.join(entries),encoding='utf-8')
    guid_meta(bank,'fileFormatVersion: 2\nguid: '+uuid.uuid4().hex+'\nNativeFormatImporter:\n  externalObjects: {}\n  mainObjectFileID: 11400000\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n')
    (ROOT/'docs/Audio'/f'{name}CueSegments.json').write_text(json.dumps(rows,ensure_ascii=False,indent=2),encoding='utf-8')
    print('Prepared',len(rows),'cues; source WAVs and existing metadata unchanged.')
if __name__=='__main__':
    parser=argparse.ArgumentParser()
    parser.add_argument('--story',action='store_true')
    if parser.parse_args().story:
        build_cues(STORY_CUES,'Story','StoryCue')
    else:
        build_cues(CUES,'Photography','PhotoCue')
