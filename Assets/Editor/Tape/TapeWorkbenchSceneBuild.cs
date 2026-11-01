using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Object=UnityEngine.Object;
namespace MemoryWhitebox.Editor
{
    public static partial class TapeWorkbenchAssetBuild
    {
        static WorkbenchAssetView Instance(string name,Transform parent)
        {
            var obj=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"/Prefabs/"+name+".prefab"),parent);
            // World dimensions are metres, irrespective of the old parent scale.
            var s=parent.lossyScale;obj.transform.localScale=new Vector3(1/s.x,1/s.y,1/s.z);
            obj.transform.rotation=Quaternion.identity;obj.transform.localPosition=Vector3.zero;
            foreach(var t in obj.GetComponentsInChildren<Transform>(true))t.gameObject.layer=25;
            return obj.GetComponent<WorkbenchAssetView>();
        }
        static BoxCollider Box(Transform t,Vector3 worldSize)
        {
            var col=t.gameObject.AddComponent<BoxCollider>();var s=t.lossyScale;col.size=new Vector3(worldSize.x/Mathf.Abs(s.x),worldSize.y/Mathf.Abs(s.y),worldSize.z/Mathf.Abs(s.z));return col;
        }
        static void Tooltip(Collider hit,string text)
        {var l=hit.gameObject.AddComponent<WorldTooltipLabel>();l.target=hit.transform;l.text=text;}
        static void HideOld(Transform root,List<Renderer> renderers,List<Collider> colliders)
        {
            foreach(var r in root.GetComponentsInChildren<Renderer>(true)){r.enabled=false;renderers.Add(r);}
            foreach(var c in root.GetComponentsInChildren<Collider>(true)){c.enabled=false;colliders.Add(c);}
            foreach(var l in root.GetComponentsInChildren<WorldTooltipLabel>(true))l.enabled=false;
        }
        public static string InstallWorkbench()
        {
            if(Application.isPlaying)throw new Exception("Stop Play first");
            var d=Object.FindObjectOfType<HouseRadioDeck>();if(d.workbenchVisuals!=null)throw new Exception("Already installed");
            var set=d.gameObject.AddComponent<TapeWorkbenchVisuals>();d.workbenchVisuals=set;
            var oldR=new List<Renderer>();var oldC=new List<Collider>();var dormant=new List<Transform>();
            var world=new GameObject("Tape Workbench Assets").transform;world.SetParent(d.transform.parent,true);
            set.storage=Instance("A01_Storage_ROOT",world);set.storage.transform.position=new Vector3(-2.16f,.80f,2.12f);
            set.storage.transform.rotation=Quaternion.Euler(0,180,0);
            set.slots=new[]{"Source01","Source02","Source03","Candidate01","Candidate02","Finished"}.Select(n=>Find(set.storage.transform,"A01_Slot_"+n)).ToArray();
            var lid=Find(set.storage.transform,"A01_Lid");set.lidHit=lid.gameObject.AddComponent<BoxCollider>();
            Tooltip(set.lidHit,"Open storage box");
            // Physical walls are authored mesh colliders. The lid remains the only box action.
            foreach(var f in set.storage.GetComponentsInChildren<MeshFilter>())if(f.transform!=lid && !f.name.Contains("SlotMarker")){var c=f.gameObject.AddComponent<MeshCollider>();c.sharedMesh=f.sharedMesh;}
            set.finishedHit=Box(set.slots[5],new Vector3(.09f,.015f,.025f));set.finishedHit.isTrigger=true;
            set.sources=new WorkbenchAssetView[3];set.intact=new WorkbenchAssetView[3];set.broken=new WorkbenchAssetView[3];
            for(int i=0;i<3;i++)
            {
                var o=d.originals[i];HideOld(o.tape,oldR,oldC);
                var v=set.sources[i]=Instance("A02_Source"+(i+1).ToString("00"),o.tape);
                o.home.SetPositionAndRotation(set.slots[i].position,Quaternion.identity);o.tape.SetPositionAndRotation(o.home.position,o.home.rotation);
                v.transform.rotation=Quaternion.identity;
                o.hit=Box(v.transform,new Vector3(.085f,.013f,.014f));((BoxCollider)o.hit).center=new Vector3(0,.025f,0);o.reels=new[]{v.leftReel,v.rightReel};Tooltip(o.hit,"Original "+(i+1).ToString("00")+" · Drag to A");
                var t=d.tapes[i];HideOld(t.root,oldR,oldC);
                var good=set.intact[i]=Instance("A03_WorkTape",t.root);var bad=set.broken[i]=Instance("A04_BrokenTape",t.root);
                if(i<2){t.root.SetPositionAndRotation(set.slots[3+i].position,Quaternion.identity);good.transform.rotation=Quaternion.identity;bad.transform.rotation=Quaternion.identity;}
                else {good.transform.rotation=Quaternion.Euler(-90,0,0);bad.transform.rotation=good.transform.rotation;}
                t.model=good.transform;t.leftReel=good.leftReel;t.rightReel=good.rightReel;
                // Hit objects stay alive when the damaged renderer replaces the intact one.
                var hits=new GameObject("Cassette interaction targets").transform;hits.SetParent(t.root,false);hits.SetPositionAndRotation(good.transform.position,good.transform.rotation);var hs=t.root.lossyScale;hits.localScale=new Vector3(1/hs.x,1/hs.y,1/hs.z);
                var a=new GameObject("A03_WorkTape_AxleHit_L").transform;a.SetParent(hits,false);a.position=Find(good.transform,a.name).position;
                var b=new GameObject("A03_WorkTape_AxleHit_R").transform;b.SetParent(hits,false);b.position=Find(good.transform,b.name).position;
                var grab=new GameObject("A03_WorkTape_GrabArea").transform;grab.SetParent(hits,false);grab.position=Find(good.transform,grab.name).position;
                foreach(var h in new[]{a,b,grab})h.gameObject.layer=25;
                t.leftSpindleHit=Box(a,new Vector3(.017f,.017f,.007f));t.rightSpindleHit=Box(b,new Vector3(.017f,.017f,.007f));t.hit=Box(grab,new Vector3(.082f,.014f,.008f));
                Tooltip(t.hit,"Drag shell · LMB + RMB to rotate");Tooltip(t.leftSpindleHit,"Left reel · Hold LMB + scroll");Tooltip(t.rightSpindleHit,"Right reel · Hold LMB + scroll");
                var upper=new GameObject("Upper shell grip").transform;upper.SetParent(hits,false);upper.localPosition=new Vector3(0,.025f,0);upper.gameObject.layer=25;set.candidateGrips[i]=Box(upper,new Vector3(.08f,.012f,.014f));Tooltip(set.candidateGrips[i],"Work tape · Drag out to check both reels");
                var prep=t.preparation;
                for(int h=0;h<2;h++)
                {
                    var socket=new GameObject(h==0?"Upper edge hole L":"Upper edge hole R").transform;socket.SetParent(hits,false);socket.position=Find(good.transform,"A03_WorkTape_OverwriteHole_"+(h==0?"L":"R")).position;
                    socket.rotation=good.transform.rotation;socket.gameObject.layer=25;Box(socket,new Vector3(.009f,.004f,.014f)).isTrigger=true;prep.pieces[h]=socket.gameObject;
                }
                t.damageMarker=null;bad.gameObject.SetActive(false);EditorUtility.SetDirty(prep);
            }
            d.originalTape=d.originals[0].tape;d.originalHit=d.originals[0].hit;d.originalHome=d.originals[0].home;d.originalReels=d.originals[0].reels;
            var tool=d.adhesiveTab.transform.parent;HideOld(tool,oldR,oldC);
            set.dispenser=Instance("A05_Dispenser_ROOT",tool);set.dispenser.transform.rotation=Quaternion.identity;
            set.roll=Find(set.dispenser.transform,"A05_Roll_PIVOT");set.head=Find(set.dispenser.transform,"A05_TapeHead_GRAB");set.cutLine=Find(set.dispenser.transform,"A05_CutLine");
            d.adhesiveTab=Box(set.head,new Vector3(.018f,.018f,.012f));d.adhesiveCutter=Box(set.cutLine,new Vector3(.025f,.008f,.008f));
            Tooltip(d.adhesiveTab,"Hold LMB · Pull tape across the cutter");Tooltip(d.adhesiveCutter,"Cutter · Pull across to tear one strip");
            foreach(var f in set.dispenser.GetComponentsInChildren<MeshFilter>())if(f.name.Contains("Base")||f.name.Contains("Cheek")){var c=f.gameObject.AddComponent<MeshCollider>();c.sharedMesh=f.sharedMesh;}
            HideOld(d.adhesiveStrip,oldR,oldC);set.strip=Instance("A06_AdhesiveParts_ROOT",d.adhesiveStrip);
            set.loosePiece=Find(set.strip.transform,"A06_TornSmallPiece").GetComponent<SkinnedMeshRenderer>();
            foreach(var t in set.strip.GetComponentsInChildren<Transform>(true))if(t.name=="A06_StretchableSegment"||t.name.StartsWith("A06_Patch_"))dormant.Add(t);
            var stripHit=d.adhesiveStrip.GetComponent<BoxCollider>();oldC.Remove(stripHit);stripHit.enabled=true;stripHit.size=new Vector3(.009f/d.adhesiveStrip.lossyScale.x,.002f/d.adhesiveStrip.lossyScale.y,.012f/d.adhesiveStrip.lossyScale.z);
            Tooltip(stripHit,"Drag patch onto one upper-edge hole");
            // Keep the existing animated carrier nodes. Only their cassette graphics are replaced.
            HideOld(d.dualDeck.tapeA,oldR,oldC);HideOld(d.dualDeck.tapeB,oldR,oldC);
            set.sourceProxies=new WorkbenchAssetView[3];
            for(int i=0;i<3;i++){set.sourceProxies[i]=Instance("A02_Source"+(i+1).ToString("00"),d.dualDeck.tapeA);set.sourceProxies[i].transform.localRotation=Quaternion.Euler(90,0,0);set.sourceProxies[i].gameObject.SetActive(i==0);}
            set.targetProxy=Instance("A03_WorkTape",d.dualDeck.tapeB);set.targetProxy.transform.localRotation=Quaternion.Euler(90,0,0);
            for(int i=0;i<d.completedCopies.Length;i++)
            {
                var root=d.completedCopies[i].transform;HideOld(root,oldR,oldC);var v=Instance("A03_WorkTape",root);root.SetPositionAndRotation(set.slots[5].position+Vector3.back*.014f*i,set.slots[5].rotation);v.transform.rotation=Quaternion.identity;v.Cover(0,1);v.Cover(1,1);
            }
            set.legacyRenderers=oldR.Distinct().ToArray();set.legacyColliders=oldC.Where(c=>c!=stripHit).Distinct().ToArray();set.dormantRoots=dormant.ToArray();set.Tick(0);
            EditorUtility.SetDirty(d);EditorUtility.SetDirty(set);EditorSceneManager.MarkSceneDirty(d.gameObject.scene);EditorSceneManager.SaveScene(d.gameObject.scene);
            return "Installed on existing tape identities; third candidate retained at its desk location";
        }
        public static string RefreshInspection()
        {
            var d=Object.FindObjectOfType<HouseRadioDeck>();var viewer=d.house.safeInspection;
            foreach(var tape in d.tapes)
            {
                var old=viewer.subjects.First(s=>s.source==tape.root);
                var fresh=SafeInspectionBuild.CreateAuthoredSubject(tape.root,old.transform.parent,old.title);
                fresh.mainFaceFrame=tape.model;fresh.mainFaceNormal=Vector3.back;fresh.mainFaceUp=Vector3.up;
                fresh.RefreshFraming();viewer.subjects=viewer.subjects.Select(s=>s==old?fresh:s).ToArray();
                var entry=tape.root.GetComponent<InspectionEntry>();if(entry!=null){entry.subject=fresh;EditorUtility.SetDirty(entry);}
                Object.DestroyImmediate(old.gameObject);
            }
            EditorUtility.SetDirty(viewer);
            return DualDeckV6Build.UpdateInspection();
        }
    }
}
