"""One-shot additive authoring migration. Does not launch Unity or change transforms.
Existing controllers remain the source of scene references; generated IDs are persisted.
"""
from pathlib import Path
import re
import uuid

ROOT = Path(__file__).resolve().parents[2]
CHAPTERS = ['spring.tape', 'summer.photography', 'autumn.writing', 'winter.painting', 'spring.departure']

def meta(path, kind='MonoImporter'):
    path = Path(str(path)+'.meta')
    if path.exists():
        return re.search(r'guid: (\w+)', path.read_text()).group(1)
    guid=uuid.uuid4().hex
    path.write_text(f'fileFormatVersion: 2\nguid: {guid}\n{kind}:\n  externalObjects: {{}}\n',encoding='utf-8')
    return guid

def directory(path):
    if not path.exists():
        directory(path.parent)
        path.mkdir()
        Path(str(path)+'.meta').write_text(f'fileFormatVersion: 2\nguid: {uuid.uuid4().hex}\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {{}}\n',encoding='utf-8')

for name in ['Core/GameDataContracts','Core/ItemDefinition','Core/ChapterDefinition','Core/ChapterSceneBinding','Core/WBMain.Data','Interaction/ItemSceneBinding']:
    meta(ROOT/'Assets/Scripts'/f'{name}.cs')
item_script=meta(ROOT/'Assets/Scripts/Interaction/ItemSceneBinding.cs')
chapter_script=meta(ROOT/'Assets/Scripts/Core/ChapterSceneBinding.cs')

def asset(name,script,fields,folder):
    directory(ROOT/'Assets/Data'/folder)
    path=ROOT/'Assets/Data'/folder/(name+'.asset')
    if not path.exists():
        path.write_text('%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n--- !u!114 &11400000\nMonoBehaviour:\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n  m_GameObject: {fileID: 0}\n  m_Enabled: 1\n  m_EditorHideFlags: 0\n  m_Script: {fileID: 11500000, guid: '+script+', type: 3}\n  m_Name: '+name+'\n  m_EditorClassIdentifier: \n'+fields,encoding='utf-8')
    return meta(path,'NativeFormatImporter')

def ref(number): return '{fileID: '+str(number)+'}'
def external(guid):return '{fileID: 11400000, guid: '+guid+', type: 2}'
def field(text,key):
    m=re.search(r'^\s*'+re.escape(key)+r': \{fileID: (-?\d+)',text,re.M)
    return int(m.group(1)) if m else 0

def migrate(path):
    text=path.read_text(encoding='utf-8')
    if f'guid: {item_script}' in text or f'guid: {chapter_script}' in text:
        print(path.name+': already authored; no changes')
        return
    pieces=re.split(r'(?=^--- !u!)',text,flags=re.M)
    blocks={int(re.match(r'--- !u!\d+ &(-?\d+)',b).group(1)):b for b in pieces[1:]}
    next_id=max([i for i in blocks if 0<i<2**62])+100
    new=[]
    def add(go,script,fields):
        nonlocal next_id
        next_id+=1
        blocks[go]=blocks[go].replace('  m_Layer:',f'  - component: {ref(next_id)}\n  m_Layer:',1)
        new.append(f'--- !u!114 &{next_id}\nMonoBehaviour:\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {{fileID: 0}}\n  m_PrefabInstance: {{fileID: 0}}\n  m_PrefabAsset: {{fileID: 0}}\n  m_GameObject: {ref(go)}\n  m_Enabled: 1\n  m_EditorHideFlags: 0\n  m_Script: {{fileID: 11500000, guid: {script}, type: 3}}\n  m_Name: \n  m_EditorClassIdentifier: \n'+fields)
        return next_id
    def transform(go):
        return next(i for i,b in blocks.items() if b.startswith('--- !u!4 ') and field(b,'m_GameObject')==go)
    items={};chapters={};config={}
    entry_ids=set()
    def item(tr,id,kind,chapter,home=0):
        if not tr:return
        go=field(blocks[tr],'m_GameObject')
        if go not in items:items[go]=(tr,id,kind,chapter,home)
    deck_guid=meta(ROOT/'Assets/Scripts/Chapters/Tape/HouseRadioDeck.cs')
    entry_guid=meta(ROOT/'Assets/Scripts/House/StudyEntry.cs')
    subject_guid=meta(ROOT/'Assets/Scripts/Interaction/Inspection/SafeInspectionSubject.cs')
    park_guid=meta(ROOT/'Assets/Scripts/Park/ParkPhotoPrototype.cs')
    for b in list(blocks.values()):
        if f'guid: {park_guid}' in b:
            go=field(b,'m_GameObject');tr=transform(go);chapters[1]=(go,tr,0)
        if f'guid: {deck_guid}' in b:
            go=field(b,'m_GameObject');tr=transform(go)
            item(tr,'house.radio','radio',CHAPTERS[0]);chapters[0]=(go,tr,field(b,'operationView'))
            config[0]=re.search(r'  commissions:\n(.*?)  recordingPanel:',b,re.S).group(1)
            work=b.split('  tapes:\n',1)[1].split('  house:',1)[0]
            for i,m in enumerate(re.finditer(r'  - root: \{fileID: (\d+)\}',work)):
                item(int(m.group(1)),f'tape.work.{i+1}','tape.work',CHAPTERS[0])
            originals=b.split('  originals:\n',1)[1].split('  completedCopies:',1)[0]
            for i,m in enumerate(re.finditer(r'  - tape: \{fileID: (\d+)\}\n    home: \{fileID: (\d+)\}',originals)):
                item(int(m.group(1)),f'tape.original.{i+1}','tape.original',CHAPTERS[0],int(m.group(2)))
            for i,n in enumerate(re.findall(r'fileID: (\d+)',b.split('  completedCopies:\n',1)[1].split('  auditionDuration:',1)[0])):
                item(transform(int(n)),f'tape.copy.{i+1}','tape.copy',CHAPTERS[0])
            tab=field(b,'adhesiveTab');parent=field(blocks[tab],'m_Father')
            item(parent,'tape.adhesive.tool','tape.adhesive',CHAPTERS[0])
        if f'guid: {entry_guid}' in b:
            index=int(re.search(r'  chapter: (\d+)',b).group(1));go=field(b,'m_GameObject');tr=transform(go)
            if 0<=index<5:
                entry_id='house.entry.'+CHAPTERS[index]
                # Several physical entry objects may route to the same chapter.
                # Chapter identity is shared; item instance identity must not be.
                if entry_id in entry_ids:entry_id+='.'+str(go)
                entry_ids.add(entry_id)
                item(tr,entry_id, 'chapter.entry.'+CHAPTERS[index],CHAPTERS[index])
                if index not in chapters:chapters[index]=(go,tr,0)
    for b in list(blocks.values()):
        if f'guid: {subject_guid}' not in b:continue
        tr=field(b,'source');entry=field(b,'entry');index=-1
        if entry in blocks:
            m=re.search(r'  chapter: (\d+)',blocks[entry]);index=int(m.group(1)) if m else -1
        item(tr,'house.inspect.'+uuid.uuid5(uuid.NAMESPACE_URL,str(path.relative_to(ROOT))+':'+str(tr)).hex,'inspectable',CHAPTERS[index] if 0<=index<5 else '')
    authored={}
    for go,(tr,id,kind,chapter,home) in items.items():
        definition=asset(kind,meta(ROOT/'Assets/Scripts/Core/ItemDefinition.cs'),f'  definitionId: {kind}\n  tooltip: \n  inspectable: 1\n  draggable: {int(kind.startswith("tape.") and kind!="tape.copy")}\n  gameplayConfiguration: {{fileID: 0}}\n','Items')
        hits=[i for i,b in blocks.items() if re.match(r'--- !u!(65|64|135|136) ',b) and field(b,'m_GameObject')==go]
        fields=f'  instanceId: {id}\n  definition: {external(definition)}\n  ownerChapterId: {chapter}\n  model: {ref(tr)}\n  homeAnchor: {ref(home)}\n'
        fields+='  hitSurfaces:\n'+''.join('  - '+ref(i)+'\n' for i in hits) if hits else '  hitSurfaces: []\n'
        authored[go]=add(go,item_script,fields)
    for index,(go,tr,anchor) in chapters.items():
        extra='  gameplayConfigurations:\n'+config[index] if index in config else '  gameplayConfigurations: []\n'
        definition=asset(CHAPTERS[index],meta(ROOT/'Assets/Scripts/Core/ChapterDefinition.cs'),f'  chapterId: {CHAPTERS[index]}\n  contentVersion: 1\n'+extra,'Chapters')
        owned=[authored[g] for g,v in items.items() if v[3]==CHAPTERS[index]]
        fields=f'  definition: {external(definition)}\n  root: {ref(tr)}\n  operationView: {ref(anchor)}\n'
        fields+='  items:\n'+''.join('  - '+ref(i)+'\n' for i in owned) if owned else '  items: []\n'
        add(go,chapter_script,fields)
    path.write_text(pieces[0]+''.join(blocks.values())+''.join(new),encoding='utf-8')
    print(f'{path.name}: authored {len(items)} item bindings and {len(chapters)} chapter bindings; transforms unchanged')

if __name__=='__main__':
    migrate(ROOT/'Assets/Scenes/HouseScene.unity')
    migrate(ROOT/'Assets/Scenes/ParkScene.unity')
