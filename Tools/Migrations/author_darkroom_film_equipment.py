"""Author the existing film-processing whitebox props in HouseScene. No Unity run."""
from pathlib import Path
import re,uuid,math
ROOT=Path(__file__).resolve().parents[2]
p=ROOT/'Assets/Scenes/HouseScene.unity'
s=p.read_text(encoding='utf-8');parts=re.split(r'(?=^--- !u!)',s,flags=re.M)
blocks={int(re.match(r'--- !u!\d+ &(-?\d+)',b)[1]):b for b in parts[1:]}
def guid(path):return re.search(r'guid: (\w+)',Path(str(ROOT/path)+'.meta').read_text())[1]
def meta(path,kind='MonoImporter'):
    p=Path(str(ROOT/path)+'.meta')
    if not p.exists():p.write_text(f'fileFormatVersion: 2\nguid: {uuid.uuid4().hex}\n{kind}:\n  externalObjects: {{}}\n',encoding='utf-8')
    return re.search(r'guid: (\w+)',p.read_text())[1]
meta(Path('Assets/Scripts/Chapters/Photography/PhotoWorld3D.SceneProcessing.cs'))
names={}
for k,b in blocks.items():
    if b.startswith('--- !u!1 &'):
        name=re.search(r'^  m_Name: (.+)$',b,re.M);tr=re.search(r'component: \{fileID: (\d+)\}',b)
        if name and tr:names[name[1]]=int(tr[1])
if 'Film Developing Tank' in names:raise SystemExit('Scene equipment already authored. Preserving edits.')
parent=names['Film Processing'];seq=max(k for k in blocks if 0<k<2**62)+100
new={};objects={};kids={};props=[];items=[]
def alloc():
    global seq
    seq+=1;return seq
def ref(n):return '{fileID: '+str(n)+'}'
def vec(v):return '{x: %s, y: %s, z: %s}'%tuple(round(a,7) for a in v)
def pos(t):return tuple(float(v) for v in re.search(r'm_LocalPosition: \{x: ([^,]+), y: ([^,]+), z: ([^}]+)\}',blocks[t]).groups())
def obj(name,position=(0,0,0),scale=(1,1,1),par=None,rot=(0,0,0,1)):
    par=parent if par is None else par
    go,tr=alloc(),alloc();objects[go]=[tr];kids[tr]=[]
    new[go]=name
    new[tr]=f'--- !u!4 &{tr}\nTransform:\n  m_ObjectHideFlags: 0\n  m_GameObject: {ref(go)}\n  serializedVersion: 2\n  m_LocalRotation: {{x: {rot[0]}, y: {rot[1]}, z: {rot[2]}, w: {rot[3]}}}\n  m_LocalPosition: {vec(position)}\n  m_LocalScale: {vec(scale)}\n  m_Children: []\n  m_Father: {ref(par)}\n  m_LocalEulerAnglesHint: {{x: 0, y: 0, z: 0}}\n'
    if par in kids:kids[par].append(tr)
    else:
        b=blocks[par]
        blocks[par]=b.replace('  m_Children: []','  m_Children:\n  - '+ref(tr)) if '  m_Children: []' in b else b.replace('  m_Children:\n','  m_Children:\n  - '+ref(tr)+'\n')
    return go,tr
def comp(go,kind,body):
    n=alloc();objects[go].append(n);new[n]=f'--- !u!{kind} &{n}\n'+body+'  m_GameObject: '+ref(go)+'\n';return n
def mono(go,script,fields):return comp(go,114,'MonoBehaviour:\n  m_ObjectHideFlags: 0\n  m_Enabled: 1\n  m_Script: {fileID: 11500000, guid: '+script+', type: 3}\n  m_Name: \n  m_EditorClassIdentifier: \n'+fields)
def material(name,color):
    path=Path('Assets/Art/Materials/Darkroom')/(name+'.mat');target=ROOT/path
    target.write_text('%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n--- !u!21 &2100000\nMaterial:\n  serializedVersion: 8\n  m_Name: '+name+'\n  m_Shader: {fileID: 46, guid: 0000000000000000f000000000000000, type: 0}\n  m_ValidKeywords: []\n  m_InvalidKeywords: []\n  m_LightmapFlags: 4\n  m_CustomRenderQueue: -1\n  m_SavedProperties:\n    serializedVersion: 3\n    m_TexEnvs: []\n    m_Ints: []\n    m_Floats:\n    - _Glossiness: 0.16\n    m_Colors:\n    - _Color: {r: '+str(color[0])+', g: '+str(color[1])+', b: '+str(color[2])+', a: 1}\n',encoding='utf-8')
    return meta(path,'NativeFormatImporter')
mats={name:material('Film'+name,color) for name,color in {'Bag':(.133,.169,.161),'Tank':(.204,.247,.220),'Lid':(.118,.165,.145),'Metal':(.73,.78,.75),'Grip':(.65,.67,.61),'Sink':(.42,.49,.47),'Clock':(.85,.84,.78)}.items()}
for key,name in [('Developer','DeveloperTray'),('Stop','StopTray'),('Fixer','FixerTray')]:mats[key]=guid(Path('Assets/Art/Materials/Darkroom')/(name+'.mat'))
def mesh(name,par,position,scale,mat,shape=10202,rot=(0,0,0,1)):
    go,tr=obj(name,position,scale,par,rot)
    comp(go,33,'MeshFilter:\n  m_ObjectHideFlags: 0\n  m_Mesh: {fileID: '+str(shape)+', guid: 0000000000000000e000000000000000, type: 0}\n')
    comp(go,23,'MeshRenderer:\n  m_ObjectHideFlags: 0\n  m_Enabled: 1\n  m_CastShadows: 1\n  m_ReceiveShadows: 1\n  m_Materials:\n  - {fileID: 2100000, guid: '+mats[mat]+', type: 2}\n')
    return go,tr
def collider(go,size):return comp(go,65,'BoxCollider:\n  m_ObjectHideFlags: 0\n  m_Material: {fileID: 0}\n  m_IsTrigger: 0\n  m_Enabled: 1\n  serializedVersion: 3\n  m_Size: '+vec(size)+'\n  m_Center: {x: 0, y: 0, z: 0}\n')
itemdef=guid(Path('Assets/Data/Items/photo.darkroom-tool.asset'))
def register(go,tr,id,home=None,size=None,label=None):
    props.append((id,tr,home or tr))
    if size:collider(go,size)
    if label:mono(go,guid(Path('Assets/Scripts/Interaction/WorldTooltipLabel.cs')),f'  text: {label}\n  target: {ref(tr)}\n  prefix: \n  mirror: {{fileID: 0}}\n')
    items.append(mono(go,guid(Path('Assets/Scripts/Interaction/ItemSceneBinding.cs')),f'  instanceId: darkroom.film.{id}\n  definition: {{fileID: 11400000, guid: {itemdef}, type: 2}}\n  ownerChapterId: summer.photography\n  model: {ref(tr)}\n  homeAnchor: {ref(home or tr)}\n  hitSurfaces: []\n'))
def equipment(name,id,anchor,size,label):
    a=names[anchor+' Anchor'];go,tr=obj(name,pos(a));register(go,tr,id,a,size,label);return go,tr
bg,bt=equipment('Dark Bag','bag','film-bag',(.5,.16,.32),'Dark bag')
mesh('Bag Body',bt,(0,0,0),(.5,.16,.32),'Bag')
mesh('Bag Sleeve',bt,(-.22,.02,-.08),(.18,.15,.16),'Tank',10208,(0,0,.7071068,.7071068))
gg,gt=mesh('Bag Grip',bt,(.22,.025,-.05),(.16,.12,.16),'Grip',10207);props.append(('bag-grip',gt,gt))
tg,tt=equipment('Film Developing Tank','tank','film-tank',(.38,.38,.38),'Developing tank')
mesh('Tank Body',tt,(0,0,0),(.38,.19,.38),'Tank',10206)
lg,lt=obj('Tank Lid',(0,.2,0),par=tt);mesh('Lid Surface',lt,(0,0,0),(.4,.025,.4),'Lid',10206)
register(lg,lt,'tank-lid',size=(.4,.05,.4),label='Tank lid')
for i,(name,mat) in enumerate([('Film Developer Bottle','Developer'),('Film Stop Bath Bottle','Stop'),('Film Fixer Bottle','Fixer')]):
    go,tr=equipment(name,'chemical-'+str(i),'film-chemical-'+str(i),(.2,.4,.2),name)
    mesh('Bottle Body',tr,(0,0,0),(.2,.2,.2),mat,10206)
    mesh('Bottle Cap',tr,(0,.24,0),(.12,.04,.12),'Grip',10206)
go,tr=equipment('Film Rinse Tap','water','film-tap',(.28,.065,.065),'Tank rinse / drain')
mesh('Tap Spout',tr,(0,0,0),(.28,.065,.065),'Metal')
mesh('Tap Stem',tr,(.13,-.23,0),(.065,.48,.065),'Metal')
go,tr=equipment('Film Drain Basin','film-drain-basin','film-sink',None,'Film drain basin')
mesh('Basin Surface',tr,(0,0,0),(.4,.055,.28),'Sink')
a=names['film-clock Anchor'];go,tr=obj('Film Processing Timer',pos(a))
mesh('Timer Face',tr,(0,0,0),(.31,.025,.31),'Clock',10206,(.7071068,0,0,.7071068))
hg,ht=mesh('Timer Hand',tr,(0,0,-.023),(.012,.115,.009),'Tank');props.append(('film-clock-hand',ht,ht))
mesh('Timer Stand',tr,(0,-.44,0),(.035,.57,.035),'Tank')
script=guid(Path('Assets/Scripts/Chapters/Photography/DarkroomPrintSceneBinding.cs'))
for k,b in list(blocks.items()):
    if 'guid: '+script in b:
        blocks[k]=b.replace('  paperSurface:',''.join(f'  - id: {id}\n    target: {ref(t)}\n    placement: {ref(a)}\n' for id,t,a in props)+'  paperSurface:')
cs=guid(Path('Assets/Scripts/Core/ChapterSceneBinding.cs'));cd=guid(Path('Assets/Data/Chapters/summer.photography.asset'))
for k,b in list(blocks.items()):
    if 'guid: '+cs in b and 'guid: '+cd in b:blocks[k]=b.rstrip()+'\n'+''.join('  - '+ref(i)+'\n' for i in items)
for go,components in objects.items():
    name=new[go];new[go]=f'--- !u!1 &{go}\nGameObject:\n  m_ObjectHideFlags: 0\n  serializedVersion: 6\n  m_Component:\n'+''.join('  - component: '+ref(i)+'\n' for i in components)+f'  m_Layer: 8\n  m_Name: {name}\n  m_TagString: Untagged\n  m_IsActive: 1\n'
for tr,children in kids.items():
    if children:new[tr]=new[tr].replace('  m_Children: []','  m_Children:\n'+'\n'.join('  - '+ref(i) for i in children))
p.write_text(parts[0]+''.join(blocks.values())+''.join(new.values()),encoding='utf-8')
print('Authored film processing equipment and references in HouseScene. No compile or Play run.')
