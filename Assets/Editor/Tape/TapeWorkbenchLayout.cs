using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MemoryWhitebox.Editor
{
    public static class TapeWorkbenchLayout
    {
        static Transform Box(Transform parent,string name,Vector3 position,Vector3 scale,Material material)
        {
            var t=parent.Find(name);
            if(t==null){t=GameObject.CreatePrimitive(PrimitiveType.Cube).transform;t.name=name;t.SetParent(parent,false);}
            t.localPosition=position;t.localScale=scale;t.GetComponent<Renderer>().sharedMaterial=material;
            return t;
        }
        public static string Apply()
        {
            if(EditorApplication.isPlaying)throw new System.InvalidOperationException("Exit Play first.");
            var d=Object.FindObjectOfType<HouseRadioDeck>();
            Undo.RegisterFullObjectHierarchyUndo(d.gameObject,"Author tape workbench");
            var source=d.recordingPanel.Find("SourceDeck_A");
            // A is a separate tabletop source deck to the left; the original B loading anchors stay untouched.
            source.SetParent(d.recordingPanel,false);
            source.position=d.transform.TransformPoint(new Vector3(-.53f,.045f,-.34f));
            source.rotation=d.transform.rotation*Quaternion.Euler(0,-15,0);
            source.localScale=Vector3.one;
            var dark=source.Find("CassetteWindow").GetComponent<Renderer>().sharedMaterial;
            var light=d.keys3d[0].GetComponent<Renderer>().sharedMaterial;
            Box(source,"SourceDeckHousing",new Vector3(0,-.015f,.015f),new Vector3(.25f,.05f,.23f),light);
            for(int i=0;i<3;i++){d.keys3d[i].SetParent(source,false);d.keys3d[i].localPosition=new Vector3((i-1)*.055f,.015f,-.078f);d.keys3d[i].localRotation=Quaternion.identity;}
            d.sourceCounter.transform.SetParent(source,false);d.sourceCounter.transform.localPosition=new Vector3(0,.016f,.085f);
            d.sourceCounter.transform.localRotation=Quaternion.Euler(90,0,0);
            d.sourceCounter.characterSize=.0045f;d.targetCounter.characterSize=.0045f;

            var tool=d.transform.Find("AdhesiveTool_Desktop");
            if(tool==null){tool=new GameObject("AdhesiveTool_Desktop").transform;tool.SetParent(d.transform,false);}
            tool.localPosition=new Vector3(-.52f,.015f,-.62f);tool.localRotation=Quaternion.identity;
            Box(tool,"Base",new Vector3(0,0,0),new Vector3(.11f,.015f,.065f),dark);
            // Segmented ring leaves a real hole, making the adhesive roll recognisable in whitebox.
            for(int i=0;i<16;i++)
            {
                float angle=i*Mathf.PI*2/16;
                var segment=Box(tool,"RollSegment_"+i,new Vector3(-.022f+Mathf.Cos(angle)*.023f,.033f+Mathf.Sin(angle)*.023f,0),new Vector3(.012f,.012f,.026f),light);
                segment.localRotation=Quaternion.Euler(0,0,angle*Mathf.Rad2Deg);
            }
            var tab=Box(tool,"TapeTab",new Vector3(.002f,.055f,0),new Vector3(.025f,.002f,.022f),light);
            var cutter=Box(tool,"Cutter",new Vector3(.044f,.028f,0),new Vector3(.012f,.009f,.036f),dark);
            for(int i=0;i<6;i++)
            {
                var tooth=Box(tool,"Tooth_"+i,new Vector3(.044f,.035f,-.015f+i*.006f),new Vector3(.007f,.007f,.004f),light);
                tooth.localRotation=Quaternion.Euler(0,0,45);
            }
            var strip=Box(tool,"LooseStrip",tab.localPosition,new Vector3(.018f,.002f,.022f),light);strip.gameObject.SetActive(false);
            var ribbon=Box(tool,"PulledRibbon",Vector3.zero,Vector3.one*.001f,light);ribbon.gameObject.SetActive(false);
            foreach(var t in tool.GetComponentsInChildren<Transform>(true))t.gameObject.layer=25;
            var subjects=Resources.FindObjectsOfTypeAll<SafeInspectionSubject>().Where(s=>s.gameObject.scene==d.gameObject.scene).ToArray();
            foreach(var tape in d.tapes)
            {
                var b=tape.preparation;var s=subjects.Single(x=>x.source==tape.root);
                Undo.RegisterFullObjectHierarchyUndo(s.gameObject,"Author inspection adhesive tool");
                for(int i=0;i<2;i++)
                {
                    var t=b.pieces[i].transform;t.localPosition=new Vector3(i==0?-.033f:.033f,.0325f,0);
                    t.localRotation=Quaternion.identity;t.localScale=new Vector3(.014f,.003f,.010f);
                }
                // Keep only one real tabletop tool; the portable display is an authored inspection copy.
                b.pieces[2].SetActive(false);b.pieces[3].SetActive(false);
                var old=s.tapePreparation.toolRoot;if(old!=null)Undo.DestroyObjectImmediate(old.gameObject);
                var display=Object.Instantiate(tool.gameObject,s.content,false).transform;display.name="AdhesiveTool_Display";
                s.tapePreparation.toolRoot=display;
                s.tapePreparation.cutter=display.Find("Cutter").GetComponent<Collider>();
                s.tapePreparation.ribbon=display.Find("PulledRibbon");
                s.tapePreparation.pieces[2]=display.Find("TapeTab").gameObject;
                s.tapePreparation.pieces[3]=display.Find("LooseStrip").gameObject;
                s.tapePreparation.tearDistance=.025f;
                foreach(var t in display.GetComponentsInChildren<Transform>(true))t.gameObject.layer=s.displayRoot.gameObject.layer;
                foreach(var pair in s.poses)if(pair.source!=null && pair.source.IsChildOf(b.transform))
                {pair.display.localPosition=pair.source.localPosition;pair.display.localRotation=pair.source.localRotation;pair.display.localScale=pair.source.localScale;pair.display.gameObject.SetActive(pair.source.gameObject.activeSelf);}
                s.surfaces=s.GetComponentsInChildren<Collider>(true);EditorUtility.SetDirty(s);
            }
            // Rebuild only the recording attachment in the radio display, preserving the rest of its inspection setup.
            var radio=subjects.Single(s=>s.source==d.transform);
            Undo.RegisterFullObjectHierarchyUndo(radio.gameObject,"Sync A deck inspection");
            var oldPanel=radio.poses.FirstOrDefault(p=>p.source==d.recordingPanel);
            var poses=radio.poses.Where(p=>p.source==null || !p.source.IsChildOf(d.recordingPanel)).ToList();
            var renders=radio.renderers.Where(p=>p.source==null || !p.source.transform.IsChildOf(d.recordingPanel)).ToList();
            if(oldPanel!=null)Undo.DestroyObjectImmediate(oldPanel.display.gameObject);
            typeof(SafeInspectionBuild).GetMethod("CopyChild",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).Invoke(null,new object[]{d.recordingPanel,radio.content,poses,renders});
            radio.poses=poses.ToArray();radio.renderers=renders.ToArray();radio.surfaces=radio.GetComponentsInChildren<Collider>(true);
            foreach(var t in radio.GetComponentsInChildren<Transform>(true))t.gameObject.layer=radio.displayRoot.gameObject.layer;
            EditorUtility.SetDirty(radio);EditorUtility.SetDirty(d);
            EditorSceneManager.MarkSceneDirty(d.gameObject.scene);EditorSceneManager.SaveScene(d.gameObject.scene);
            return "A deck left of original B; desktop cutter and three authored inspection tools; both holes on upper edge.";
        }
    }
}
