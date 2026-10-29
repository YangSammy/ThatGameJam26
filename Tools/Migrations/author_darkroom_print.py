"""Add physical darkroom props without changing existing scene transforms. No Unity run."""
from pathlib import Path
import re
import math
from author_data_bindings import ROOT, meta, asset, directory, ref, external

for f in ['DarkroomPrintDefinition','DarkroomPrintState','DarkroomPrintSceneBinding','PhotoWorld3D.PrintObjects']:
    meta(ROOT/'Assets/Scripts/Chapters/Photography'/f'{f}.cs')
shader=meta(ROOT/'Assets/Shaders/ContactPrintReveal.shader','ShaderImporter')
directory(ROOT/'Assets/Data/Photography')
directory(ROOT/'Assets/Art/Materials/Darkroom')

def material(name,color,custom=False):
    p=ROOT/'Assets/Art/Materials/Darkroom'/(name+'.mat')
    shader_ref='{fileID: 4800000, guid: '+shader+', type: 3}' if custom else '{fileID: 46, guid: 0000000000000000f000000000000000, type: 0}'
    if not p.exists():
        p.write_text('%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n--- !u!21 &2100000\nMaterial:\n  serializedVersion: 8\n  m_ObjectHideFlags: 0\n  m_Name: '+name+'\n  m_Shader: '+shader_ref+'\n  m_ValidKeywords: []\n  m_InvalidKeywords: []\n  m_LightmapFlags: 4\n  m_CustomRenderQueue: -1\n  stringTagMap: {}\n  disabledShaderPasses: []\n  m_SavedProperties:\n    serializedVersion: 3\n    m_TexEnvs:\n    - _MainTex:\n        m_Texture: {fileID: 0}\n        m_Scale: {x: 1, y: 1}\n        m_Offset: {x: 0, y: 0}\n    m_Ints: []\n    m_Floats:\n    - _Glossiness: 0.2\n    - _Reveal: 0\n    m_Colors:\n    - _Color: '+color+'\n    - _PaperColor: {r: 0.93, g: 0.91, b: 0.84, a: 1}\n',encoding='utf-8')
    return meta(p,'NativeFormatImporter')

mats={
    'frame':material('PrintFrame','{r: 0.25, g: 0.24, b: 0.20, a: 1}'),
    'negative':material('PrintNegative','{r: 0.18, g: 0.12, b: 0.08, a: 1}'),
    'paper':material('ContactPrint','{r: 0.93, g: 0.91, b: 0.84, a: 1}',True),
    'developer':material('DeveloperTray','{r: 0.58, g: 0.53, b: 0.36, a: 1}'),
    'stop':material('StopTray','{r: 0.52, g: 0.61, b: 0.46, a: 1}'),
    'fixer':material('FixerTray','{r: 0.38, g: 0.55, b: 0.58, a: 1}'),
    'water':material('PrintWater','{r: 0.55, g: 0.67, b: 0.72, a: 1}'),
}
definition=asset('DarkroomPrint',meta(ROOT/'Assets/Scripts/Chapters/Photography/DarkroomPrintDefinition.cs'),
    '  negativeDrySeconds: 3\n  exposureSeconds: 2\n  developerSeconds: 4\n  stopSeconds: 1\n  fixerSeconds: 3\n  washSeconds: 2\n  printDrySeconds: 3\n  dropTolerance: 0.14\n','Photography')

scene=ROOT/'Assets/Scenes/HouseScene.unity'
text=scene.read_text(encoding='utf-8')
binding_script=meta(ROOT/'Assets/Scripts/Chapters/Photography/DarkroomPrintSceneBinding.cs')
if 'guid: '+binding_script in text:
    print('Print station already authored. No scene changes.')
    raise SystemExit(0)
pieces=re.split(r'(?=^--- !u!)',text,flags=re.M)
blocks={int(re.match(r'--- !u!\d+ &(-?\d+)',b).group(1)):b for b in pieces[1:]}
serial=max(i for i in blocks if 0<i<2**62)+100
new={};objects={};children={};props=[];items=[]
def alloc():
    global serial
    serial+=1
    return serial
def v3(x):return '{x: '+str(x[0])+', y: '+str(x[1])+', z: '+str(x[2])+'}'
def obj(name,pos=(0,0,0),scale=(1,1,1),parent=0,rotation=0,active=1):
    go,tr=alloc(),alloc();objects[go]=[tr];children[tr]=[]
    new[go]=('go',name,active)
    a=math.radians(rotation)/2
    new[tr]=f'--- !u!4 &{tr}\nTransform:\n  m_ObjectHideFlags: 0\n  m_GameObject: {ref(go)}\n  serializedVersion: 2\n  m_LocalRotation: {{x: {math.sin(a)}, y: 0, z: 0, w: {math.cos(a)}}}\n  m_LocalPosition: {v3(pos)}\n  m_LocalScale: {v3(scale)}\n  m_ConstrainProportionsScale: 0\n  CHILDREN\n  m_Father: {ref(parent)}\n  m_LocalEulerAnglesHint: {{x: {rotation}, y: 0, z: 0}}\n'
    if parent in children:children[parent].append(tr)
    return go,tr
def component(go,kind,body):
    cid=alloc();objects[go].append(cid)
    new[cid]=f'--- !u!{kind} &{cid}\n'+body+f'  m_GameObject: {ref(go)}\n'
    return cid
def mono(go,script,fields):
    return component(go,114,'MonoBehaviour:\n  m_ObjectHideFlags: 0\n  m_Enabled: 1\n  m_Script: {fileID: 11500000, guid: '+script+', type: 3}\n  m_Name: \n  m_EditorClassIdentifier: \n'+fields)
def box(name,pos,size,mat,parent,hit=True,active=1):
    go,tr=obj(name,pos,size,parent,active=active)
    component(go,33,'MeshFilter:\n  m_ObjectHideFlags: 0\n  m_Mesh: {fileID: 10202, guid: 0000000000000000e000000000000000, type: 0}\n')
    renderer=component(go,23,'MeshRenderer:\n  m_ObjectHideFlags: 0\n  m_Enabled: 1\n  m_CastShadows: 1\n  m_ReceiveShadows: 1\n  m_Materials:\n  - {fileID: 2100000, guid: '+mats[mat]+', type: 2}\n')
    if hit:component(go,65,'BoxCollider:\n  m_ObjectHideFlags: 0\n  m_Material: {fileID: 0}\n  m_IsTrigger: 0\n  m_Enabled: 1\n  serializedVersion: 3\n  m_Size: {x: 1, y: 1, z: 1}\n  m_Center: {x: 0, y: 0, z: 0}\n')
    return go,tr,renderer
top=892821582
root,root_tr=obj('DarkroomPrintStation',(0,.5,0),(1/1.49,1/.05,1/2.29),top)
def prop(id,name,pos,size,mat,tip,placement=(0,.04,0),rot=0):
    go,tr,r=box(name,pos,size,mat,root_tr)
    # Anchors are siblings so a material's scale does not change the paper size or orientation.
    _,at=obj(name+' Placement',tuple(pos[i]+placement[i] for i in range(3)),parent=root_tr,rotation=rot)
    props.append((id,tr,at))
    mono(go,meta(ROOT/'Assets/Scripts/Interaction/WorldTooltipLabel.cs'),'  text: '+tip+'\n  target: '+ref(tr)+'\n  prefix: \n  mirror: {fileID: 0}\n')
    kind='photo.negative' if id=='print-negative' else 'photo.contact-sheet' if id=='print-paper' else 'photo.darkroom-tool'
    itemdef=asset(kind,meta(ROOT/'Assets/Scripts/Core/ItemDefinition.cs'),f'  definitionId: {kind}\n  tooltip: \n  inspectable: 0\n  draggable: {int(id in ["print-negative","print-paper"])}\n  gameplayConfiguration: {{fileID: 0}}\n','Items')
    instance='darkroom.negative' if id=='print-negative' else 'darkroom.paper' if id=='print-paper' else 'darkroom.'+id[6:]
    items.append(mono(go,meta(ROOT/'Assets/Scripts/Interaction/ItemSceneBinding.cs'),f'  instanceId: {instance}\n  definition: {external(itemdef)}\n  ownerChapterId: summer.photography\n  model: {ref(tr)}\n  homeAnchor: {ref(at)}\n  hitSurfaces: []\n'))
    return go,tr,r

neg,negtr,negr=prop('print-negative','Negative Strip',(-.2,.37,.53),(.08,.006,.28),'negative','Negative - drag to drying clip')
paper,papertr,paperr=prop('print-paper','Contact Paper',(-.57,.035,-.35),(.34,.006,.19),'paper','Contact paper - drag between stations')
for id,pos,rot in [('print-negative-home',(-.2,.37,.53),0),('print-paper-home',(-.57,.035,-.35),0)]:
    go,tr=obj(id,pos,parent=root_tr,rotation=rot);props.append((id,tr,tr))
prop('print-negative-clip','Negative Drying Clip',(-.58,.68,.55),(.08,.07,.05),'frame','Dry negative - drop here',(0,-.15,0),90)
prop('print-photo-clip','Photo Drying Clip',(.23,.68,.72),(.08,.07,.05),'frame','Dry print - drop here',(0,-.15,0),90)
box('Drying Rail',(-.18,.75,.64),(.95,.025,.025),'frame',root_tr,False)
box('Drying Stand',(-.62,.37,.64),(.025,.76,.025),'frame',root_tr,False)
frame,frametr,_=prop('print-frame','Contact Frame',(-.46,.025,.03),(.43,.035,.34),'frame','Contact frame - paper below negative',(0,.027,0))
lidgo,lidtr=obj('Contact Frame Lid',(-.46,.065,.20),parent=root_tr)
# Open/closed rotation is around the hinge, not the centre of the frame.
for pos,size in [((-.205,0,-.17),(.02,.018,.34)),((.205,0,-.17),(.02,.018,.34)),((0,0,-.33),(.43,.018,.02)),((0,0,0),(.43,.018,.02))]:
    box('Frame Rim',pos,size,'frame',lidtr)
props.append(('print-frame-lid',lidtr,lidtr))
mono(lidgo,meta(ROOT/'Assets/Scripts/Interaction/WorldTooltipLabel.cs'),'  text: Open or close contact frame\n  target: '+ref(lidtr)+'\n  prefix: \n  mirror: {fileID: 0}\n')
prop('print-exposure','Exposure Switch',(-.46,.045,.31),(.12,.06,.08),'developer','Hold LMB to expose',(0,0,0))
light,_,_=box('Exposure Indicator',(-.46,.16,.31),(.04,.04,.04),'paper',root_tr,False,0)
for id,name,x,mat in [('print-developer','Developer Tray',-.48,'developer'),('print-stop','Stop Bath Tray',0,'stop'),('print-fixer','Fixer Tray',.48,'fixer')]:
    go,tr,_=prop(id,name,(x,.03,-.99),(.42,.035,.28),mat,name+' - drop paper',(0,.025,0))
    # Actual shallow walls, visible from a first-person oblique angle.
    for dx,dz,sx,sz in [(-.205,0,.015,.28),(.205,0,.015,.28),(0,-.135,.42,.015),(0,.135,.42,.015)]:
        box(name+' Rim',(x+dx,.065,-.99+dz),(sx,.05,sz),mat,root_tr,False)
prop('print-wash','Print Wash Tray',(.39,.03,.82),(.48,.04,.32),'water','Wash tray - drop paper',(0,.03,0))
prop('print-tap','Print Wash Tap',(.62,.22,.85),(.08,.08,.14),'frame','LMB - wash paper',(0,0,0))
water,_,_=box('Print Wash Stream',(.39,.18,.82),(.025,.25,.025),'water',root_tr,False,0)
fields='  definition: '+external(definition)+'\n  props:\n'+''.join(f'  - id: {id}\n    target: {ref(tr)}\n    placement: {ref(at)}\n' for id,tr,at in props)
fields+=f'  paperSurface: {ref(paperr)}\n  negativeSurface: {ref(negr)}\n  frameLid: {ref(lidtr)}\n  exposureLight: {ref(light)}\n  waterStream: {ref(water)}\n'
mono(root,binding_script,fields)

blocks[top]=blocks[top].replace('  m_Children: []','  m_Children:\n  - '+ref(root_tr)) if '  m_Children: []' in blocks[top] else blocks[top].replace('  m_Children:\n','  m_Children:\n  - '+ref(root_tr)+'\n')
# Include the new authored items/configuration in the existing photographic chapter binding.
chapter_script=meta(ROOT/'Assets/Scripts/Core/ChapterSceneBinding.cs')
photo_def=meta(ROOT/'Assets/Data/Chapters/summer.photography.asset','NativeFormatImporter')
for k,b in list(blocks.items()):
    if 'guid: '+chapter_script in b and 'guid: '+photo_def in b:
        blocks[k]=b.rstrip()+'\n'+''.join('  - '+ref(i)+'\n' for i in items)
config=ROOT/'Assets/Data/Chapters/summer.photography.asset'
content=config.read_text();content=content.replace('  gameplayConfigurations: []','  gameplayConfigurations:\n  - '+external(definition));config.write_text(content,encoding='utf-8')
for k,b in list(new.items()):
    if isinstance(b,tuple):
        _,name,active=b
        new[k]=f'--- !u!1 &{k}\nGameObject:\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {{fileID: 0}}\n  m_PrefabInstance: {{fileID: 0}}\n  m_PrefabAsset: {{fileID: 0}}\n  serializedVersion: 6\n  m_Component:\n'+''.join('  - component: '+ref(i)+'\n' for i in objects[k])+f'  m_Layer: 8\n  m_Name: {name}\n  m_TagString: Untagged\n  m_IsActive: {active}\n'
    elif '  CHILDREN\n' in b:
        new[k]=b.replace('  CHILDREN\n','  m_Children:\n'+''.join('  - '+ref(i)+'\n' for i in children[k]) if children[k] else '  m_Children: []\n')
scene.write_text(pieces[0]+''.join(blocks.values())+''.join(new.values()),encoding='utf-8')
print('Authored darkroom print station, placement anchors, tray meshes and data bindings. No Play/validation run.')
