"""One-shot HouseScene darkroom layout authoring. No Unity launch or validation."""
from pathlib import Path
import re
ROOT=Path(__file__).resolve().parents[2]
p=ROOT/'Assets/Scenes/HouseScene.unity'
text=p.read_text(encoding='utf-8')
parts=re.split(r'(?=^--- !u!)',text,flags=re.M)
blocks={int(re.match(r'--- !u!\d+ &(-?\d+)',b)[1]):b for b in parts[1:]}
transforms={}
names={}
for k,b in blocks.items():
    if b.startswith('--- !u!1 &'):
        m=re.search(r'^  m_Name: (.+)$',b,re.M)
        t=re.search(r'component: \{fileID: (\d+)\}',b)
        if m and t:names.setdefault(m[1],[]).append(int(t[1]))
root=names['DarkroomPrintStation'][0]
if 'Film Processing' in names:raise SystemExit('Layout already authored; leaving manual edits intact.')
seq=max(k for k in blocks if 0<k<2**62)+100
new={}
def alloc():
    global seq
    seq+=1
    return seq
def vec(v):return '{x: %s, y: %s, z: %s}'%tuple(round(x,6) for x in v)
def pos(t):
    return tuple(float(x) for x in re.search(r'm_LocalPosition: \{x: ([^,]+), y: ([^,]+), z: ([^}]+)\}',blocks[t]).groups())
def setpos(t,v):blocks[t]=re.sub(r'm_LocalPosition: \{[^}]+\}','m_LocalPosition: '+vec(v),blocks[t])
def setscale(t,v):blocks[t]=re.sub(r'm_LocalScale: \{[^}]+\}','m_LocalScale: '+vec(v),blocks[t])
def attach(parent,t):
    b=blocks.get(parent,new.get(parent))
    b=b.replace('  m_Children: []','  m_Children:\n  - {fileID: '+str(t)+'}') if '  m_Children: []' in b else b.replace('  m_Children:\n','  m_Children:\n  - {fileID: '+str(t)+'}\n')
    if parent in blocks:blocks[parent]=b
    else:new[parent]=b

def obj(name,parent,position=(0,0,0)):
    go,t=alloc(),alloc()
    new[go]=f'--- !u!1 &{go}\nGameObject:\n  m_ObjectHideFlags: 0\n  serializedVersion: 6\n  m_Component:\n  - component: {{fileID: {t}}}\n  m_Layer: 8\n  m_Name: {name}\n  m_TagString: Untagged\n  m_IsActive: 1\n'
    new[t]=f'--- !u!4 &{t}\nTransform:\n  m_ObjectHideFlags: 0\n  m_GameObject: {{fileID: {go}}}\n  serializedVersion: 2\n  m_LocalRotation: {{x: 0, y: 0, z: 0, w: 1}}\n  m_LocalPosition: {vec(position)}\n  m_LocalScale: {{x: 1, y: 1, z: 1}}\n  m_Children: []\n  m_Father: {{fileID: {parent}}}\n  m_LocalEulerAnglesHint: {{x: 0, y: 0, z: 0}}\n'
    attach(parent,t)
    return t
film=obj('Film Processing',root)
contact=obj('Contact Printing',root)
paper=obj('Paper Processing and Drying',root)
def reparent(t,parent):
    blocks[root]=blocks[root].replace('  - {fileID: '+str(t)+'}\n','')
    blocks[t]=re.sub(r'm_Father: \{fileID: \d+\}',f'm_Father: {{fileID: {parent}}}',blocks[t])
    attach(parent,t)
def move(name,v,group):
    t=names[name][0];old=pos(t);delta=tuple(v[i]-old[i] for i in range(3));setpos(t,v);reparent(t,group)
    for at in names.get(name+' Placement',[]):
        a=pos(at);setpos(at,tuple(a[i]+delta[i] for i in range(3)));reparent(at,group)
    for rim in names.get(name+' Rim',[]):
        a=pos(rim);setpos(rim,tuple(a[i]+delta[i] for i in range(3)));reparent(rim,group)
move('Negative Strip',(-.35,.23,-.43),film)
move('print-negative-home',(-.35,.23,-.43),film)
move('Contact Paper',(-.32,.035,-.20),contact)
move('print-paper-home',(-.32,.035,-.20),contact)
move('Contact Frame',(-.32,.025,.12),contact)
move('Contact Frame Lid',(-.32,.065,.29),contact)
move('Exposure Switch',(.25,.045,.15),contact)
move('Exposure Indicator',(.25,.16,.15),contact)
move('Negative Drying Clip',(.55,.68,-.14),film)
move('Photo Drying Clip',(.55,.68,1.02),paper)
for name in ['Negative Drying Clip Placement','Photo Drying Clip Placement']:
    t=names[name][0]
    blocks[t]=re.sub(r'm_LocalRotation: \{[^}]+\}','m_LocalRotation: {x: 0.5, y: 0.5, z: -0.5, w: 0.5}',blocks[t])
    blocks[t]=re.sub(r'm_LocalEulerAnglesHint: \{[^}]+\}','m_LocalEulerAnglesHint: {x: 90, y: 90, z: 0}',blocks[t])
move('Drying Rail',(.65,.75,.44),paper);setscale(names['Drying Rail'][0],(.025,.025,1.4))
move('Drying Stand',(.65,.37,-.23),paper)
move('Developer Tray',(-.35,.03,.52),paper)
move('Stop Bath Tray',(-.35,.03,.87),paper)
move('Fixer Tray',(.23,.03,.52),paper)
move('Print Wash Tray',(.23,.03,.87),paper);setscale(names['Print Wash Tray'][0],(.42,.04,.28))
move('Print Wash Tap',(.59,.22,.87),paper)
move('Print Wash Stream',(.23,.18,.87),paper)
anchors={
'film-bag':(-.35,.09,-.85),'film-tank':(-.35,.20,-.43),
'film-chemical-0':(.48,.21,-.95),'film-chemical-1':(.48,.21,-.70),'film-chemical-2':(.48,.21,-.45),
'film-sink':(.10,.04,-.90),'film-tap':(.15,.52,-.90),'film-clock':(.60,.74,-.65)}
refs=[]
for id,v in anchors.items():
    t=obj(id+' Anchor',film,v);refs.append(f'  - id: {id}\n    target: {{fileID: {t}}}\n    placement: {{fileID: {t}}}\n')
guid=re.search(r'guid: (\w+)',(ROOT/'Assets/Scripts/Chapters/Photography/DarkroomPrintSceneBinding.cs.meta').read_text())[1]
for k,b in list(blocks.items()):
    if 'guid: '+guid in b:blocks[k]=b.replace('  paperSurface:', ''.join(refs)+'  paperSurface:')
p.write_text(parts[0]+''.join(blocks.values())+''.join(new.values()),encoding='utf-8')
print('Authored three layout groups; retained existing transforms/references and added film placement anchors. No Unity validation.')
