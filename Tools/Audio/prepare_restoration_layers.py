"""Reproducible supplemental synthesis, NOT extraction of original instrument stems.
Run: uv run --with numpy --with soundfile python tools/Audio/prepare_restoration_layers.py
Preserves source music and all existing cue IDs. No Unity refresh or scene save.
"""
from pathlib import Path
import hashlib, json, re, uuid
import numpy as np
import soundfile as sf

ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'Assets/audio/RestorationLayers'
SR=48000
NS=uuid.UUID('31c9daa3-efad-4fdb-96f1-f1bc73e0fc31')
rows=[]

def guid(path):
    meta=Path(str(path)+'.meta')
    if meta.exists():return re.search(r'guid: ([0-9a-f]+)',meta.read_text(encoding='utf-8-sig'))[1]
    return uuid.uuid5(NS,str(path.relative_to(ROOT)).replace('\\','/')).hex

def meta(path,folder=False):
    target=Path(str(path)+'.meta')
    if target.exists():return
    g=guid(path)
    if folder:body='folderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n'
    elif path.suffix=='.wav':
        template=(ROOT/'Assets/audio/StoryCues/StoryCue_46.wav.meta').read_text()
        body=template.split('AudioImporter:',1)[1]
        body='AudioImporter:'+body.replace('compressionFormat: 1','compressionFormat: 0').replace('normalize: 1','normalize: 0').replace('sampleRateOverride: 44100','sampleRateOverride: 48000')
    elif path.suffix=='.cs':body='MonoImporter:\n  externalObjects: {}\n  serializedVersion: 2\n  defaultReferences: []\n  executionOrder: 0\n'
    else:body='NativeFormatImporter:\n  externalObjects: {}\n  mainObjectFileID: 11400000\n'
    target.write_text('fileFormatVersion: 2\nguid: '+g+'\n'+body,encoding='utf-8')

def save(name,data,origin,loop=False,**extra):
    data=np.asarray(data,dtype=np.float64)
    if data.ndim==1:data=np.column_stack((data,data))
    peak=np.max(np.abs(data))
    if peak>.42:data*=.42/peak
    edge=min(int(.025*SR),len(data)//4)
    data[:edge]*=np.linspace(0,1,edge)[:,None]
    data[-edge:]*=np.linspace(1,0,edge)[:,None]
    path=OUT/(name+'.wav');sf.write(path,data,SR,subtype='PCM_24');meta(path)
    if not name.startswith('Cue_'):
        metadata=Path(str(path)+'.meta')
        settings=metadata.read_text().replace('loadType: 0','loadType: 2').replace('preloadAudioData: 1','preloadAudioData: 0')
        metadata.write_text(settings,encoding='utf-8')
    pcm,rate=sf.read(path,always_2d=True)
    rows.append(dict(file=str(path.relative_to(ROOT)).replace('\\','/'),origin=origin,sampleRate=rate,bits=24,channels=2,
        samples=len(pcm),seconds=len(pcm)/rate,loopStartSample=0 if loop else None,loopEndExclusive=len(pcm) if loop else None,
        peak=float(np.max(np.abs(pcm))),rms=float(np.sqrt(np.mean(pcm**2))),boundaryStep=float(np.max(np.abs(pcm[-1]-pcm[0]))),
        sha256=hashlib.sha256(path.read_bytes()).hexdigest(),**extra))
    return path

def pitch_at(audio,start):
    # Only select a support pitch. Source PCM is NEVER copied into musical layers.
    segment=audio[int(start*SR):int(start*SR)+32768].mean(axis=1)
    if len(segment)<4096:return 220.
    spec=np.abs(np.fft.rfft(segment*np.hanning(len(segment))))
    freq=np.fft.rfftfreq(len(segment),1/SR)
    spec[(freq<130)|(freq>650)]=0
    hz=freq[np.argmax(spec)]
    midi=round(69+12*np.log2(max(hz,130)/440))
    return 440*2**((midi-69)/12)

def add_note(target,start,duration,hz,kind):
    offset=int(start*SR);n=min(int(duration*SR),len(target)-offset)
    if n<=0:return
    t=np.arange(n)/SR
    envelope=np.minimum(1,t/.04)*np.minimum(1,(n/SR-t)/.18)
    if kind=='motif':wave=(np.sin(2*np.pi*hz*t)+.15*np.sin(2*np.pi*2*hz*t))*np.exp(-t*2.3)
    elif kind=='harmony':wave=(np.sin(2*np.pi*hz*.5*t)+.18*np.sin(2*np.pi*hz*t))*.38
    else:wave=np.sin(2*np.pi*hz*.25*t)*(.7+.12*np.sin(2*np.pi*.18*t))*.32
    target[offset:offset+n]+=wave*envelope

def musical_group(name,source):
    audio,rate=sf.read(ROOT/source,always_2d=True);assert rate==SR
    count=len(audio);duration=count/SR;clips=[]
    for role in ('motif','harmony','sustain','detail'):
        pcm=np.zeros(count)
        if role=='detail':
            rng=np.random.default_rng(20261105)
            for start in np.arange(.35,duration-.6,.72 if name=='Autumn' else 2.4):
                n=min(int(.12*SR),count-int(start*SR));t=np.arange(n)/SR
                noise=rng.normal(0,1,n);noise=np.convolve(noise,np.ones(12)/12,'same')
                pcm[int(start*SR):int(start*SR)+n]+=.17*noise*np.exp(-t*45)*np.minimum(t/.005,1)
        else:
            for start in np.arange(.2,duration-1,4):
                hz=pitch_at(audio,start)
                add_note(pcm,start,1.8 if role=='motif' else 3.7,hz,role)
        clips.append(save(name+'_'+role,pcm,'new procedural supplementary '+role+'; source pitch selection only, no source samples',True,
            group=name,role=role,sourceMix=source,commonOriginSample=0,tailPolicy='ward hard cut' if name=='Ward' else 'runtime 0.55 second crossfade; separate optional release asset'))
        end=int(np.flatnonzero(np.abs(pcm)>.0001)[-1])+1
        tail=pcm[max(0,end-int(.8*SR)):end].copy()
        tail*=np.linspace(1,0,len(tail))**2
        save(name+'_'+role+'_Release',tail,'new supplementary release export',False,group=name,role=role,deliveryOnly=True)
    if name=='Ward':
        rng=np.random.default_rng(1105);noise=rng.normal(0,1,count)
        # Quiet non-alarm room air; independent environment voice and gain.
        noise=np.convolve(noise,np.ones(110)/110,'same')*.32
        clips.append(save(name+'_air',noise,'new procedural room air, not original hospital recording',True,group=name,role='environment',commonOriginSample=0,tailPolicy='hard cut, no release'))
    return clips

def clipref(path):return '{fileID: 8300000, guid: '+guid(path)+', type: 3}'

def main():
    OUT.mkdir(parents=True,exist_ok=True);meta(OUT,True)
    groups=[('Autumn','Assets/Music/MUS_07_Letter/MUS_07_Letter_Circle.wav'),
        ('WinterGarden','Assets/Music/MUS_08_Winter_Room/MUS_08_Winter_Garden_Circle.wav'),
        ('WinterRoom','Assets/Music/MUS_08_Winter_Room/MUS_08_Winter_Room_Circle.wav'),
        ('Ward','Assets/Music/MUS_08_Winter_Room/MUS_08_Room_Ending.wav')]
    for name in ('GameScoreLayers.cs','GameScore.Layering.cs'):meta(ROOT/'Assets/Scripts/Core'/name)
    script=ROOT/'Assets/Scripts/Core/GameScoreLayers.cs'
    config='%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n--- !u!114 &11400000\nMonoBehaviour:\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n  m_GameObject: {fileID: 0}\n  m_Enabled: 1\n  m_EditorHideFlags: 0\n  m_Script: {fileID: 11500000, guid: '+guid(script)+', type: 3}\n  m_Name: GameScoreLayers\n  m_EditorClassIdentifier: \n  arrangements:\n'
    for name,source in groups:
        clips=musical_group(name,source)
        config+='  - sourceMix: '+clipref(ROOT/source)+'\n    layers:\n'
        for i,path in enumerate(clips):
            role=('motif','harmony','sustain','detail','environment')[i]
            onset=([0,.08,.3,.52,.0] if name=='Ward' else [.08,.25,0,.45] if name=='Autumn' else [0,0,0,.1])[i]
            gain=([.12,.10,.10,.18,.65] if name=='Ward' else [.07,.06,.08,.3])[i]
            config+=f'    - role: {role}\n      clip: {clipref(path)}\n      gain: {gain}\n      enterAt: {onset}\n      environment: {1 if role=="environment" else 0}\n'
    config+='  environmentGain: 0.12\n'
    asset=ROOT/'Assets/Resources/GameScoreLayers.asset';asset.write_text(config,encoding='utf-8');meta(asset)
    # Dedicated envelope source and a quieter, short chain hint; no remapping existing cues.
    cues=[]
    for cue,source,start,duration,volume in [(134,'Assets/Resources/ChapterFoley/Paper.wav',0,.38,.27),(246,'Assets/audio/SFX-019.wav',.38,.34,.2)]:
        pcm,rate=sf.read(ROOT/source,always_2d=True)
        segment=pcm[int(start*rate):int((start+duration)*rate)]
        if rate!=SR:
            old=np.arange(len(segment));new=np.arange(round(len(segment)*SR/rate))*rate/SR
            segment=np.column_stack([np.interp(new,old,segment[:,i]) for i in range(segment.shape[1])])
        if segment.shape[1]==1:segment=np.repeat(segment,2,axis=1)
        segment*=.38/max(.001,float(np.max(np.abs(segment))))
        path=save('Cue_'+str(cue),segment,'slice from '+source,False,cue=cue,sourceStart=start)
        cues.append((cue,path,len(segment)/SR,volume))
    n=SR*2;t=np.arange(n)/SR;rng=np.random.default_rng(234)
    noise=np.convolve(rng.normal(0,1,n),np.ones(30)/30,'same')
    rolling=.20*noise*(.6+.4*np.sin(2*np.pi*9*t)**2)+.025*np.sin(2*np.pi*74*t)
    path=save('Cue_234',rolling,'new procedural suitcase wheel rumble and tread; not winter pushing-box slice',True,cue=234)
    cues.append((234,path,2,.28))
    bank=ROOT/'Assets/Resources/StorySfxBank.asset';text=bank.read_text(encoding='utf-8-sig')
    for cue,path,duration,volume in cues:
        if re.search(r'^  - id: '+str(cue)+r'$',text,re.M):continue
        text+=f'  - id: {cue}\n    clip: {clipref(path)}\n    start: 0\n    duration: {duration:.6f}\n    volume: {volume}\n'
    bank.write_text(text,encoding='utf-8')
    for row in rows:
        assert row['peak']>.00001 and row['peak']<=.421 and row['boundaryStep']<.00001,row['file']
    report=ROOT/'docs/Audio/RestorationAudioManifest.json'
    report.write_text(json.dumps(dict(schema=1,provenance='New procedural supplementary parts; no original instrument stems delivered',assets=rows),indent=2),encoding='utf-8')
    print('PASS',len(rows),'WAVs: 48kHz PCM24 stereo, finite/unclipped, zero boundary; source files preserved. Config and 3 additive cue bindings written.')

if __name__=='__main__':main()
