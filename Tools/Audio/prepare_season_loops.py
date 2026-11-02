# Requires numpy and soundfile in an isolated Python environment.
from pathlib import Path
import os
os.chdir(Path(__file__).resolve().parents[2])
import soundfile as sf,numpy as np,json
x,s=sf.read('Assets/audio/SFX-002.wav',dtype='float32',always_2d=True)
out=Path('Assets/audio/SeasonLoops');out.mkdir(exist_ok=True)
segments=[('Spring',0,55.2),('Summer',61.6,101.3),('Autumn',115.5,165.2),('Winter',176.8,206.9)]
records=[]
for name,start,end in segments:
 a=x[round(start*s):round(end*s)].copy();n=round(s*1.5);t=np.linspace(0,1,n,dtype=np.float32)[:,None]
 # Equal-gain tail/head overlap; loop boundary lies inside continuous original material.
 mix=a[-n:]*(1-t)+a[:n]*t;y=np.concatenate([a[n:-n],mix]);sf.write(str(out/f'SFX-002_{name}_Loop.wav'),y,s,subtype='PCM_24')
 records.append(dict(season=name,start=start,end=end,crossfade=1.5,outputSeconds=len(y)/s))
Path('docs/Audio/SFX002_Segments.json').write_text(json.dumps(records,indent=2))
