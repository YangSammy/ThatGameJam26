using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace MemoryWhitebox.Editor
{
    public static class PhotographyAssetIntegration
    {
        public static string BindHouseEntry()
        {
            if(Application.isPlaying)throw new Exception("Edit mode required");
            var house=UnityEngine.Object.FindObjectOfType<WBMain>();
            var entry=house.houseShell.GetComponentsInChildren<StudyEntry>(true).Single(e=>e.chapter==1);
            entry.gameObject.SetActive(true);var source=entry.transform;var old=house.safeInspection.subjects.FirstOrDefault(s=>s!=null && s.source==source);
            var existing=source.GetComponentInChildren<PhotographyCameraVisual>(true);
            if(existing!=null)return "Already integrated";
            foreach(var r in source.GetComponentsInChildren<Renderer>(true))r.enabled=false;
            foreach(var c in source.GetComponentsInChildren<Collider>(true))c.enabled=false;
            var visual=PhotographyCameraVisual.Create(source,new Vector3(0,.005f,0),3.1f,source.gameObject.layer);
            visual.transform.localRotation=Quaternion.Euler(0,180,0);visual.Apply(false,false,100,0,0,2,8,1f/125,0,0,true);
            var hit=source.gameObject.AddComponent<BoxCollider>();hit.center=new Vector3(0,.14f,-.04f);hit.size=new Vector3(.45f,.3f,.32f);
            var container=old!=null?old.transform.parent:GameObject.Find("Safe inspection - authored display models").transform;
            var subject=SafeInspectionBuild.CreateAuthoredSubject(source,container,"35 mm camera / 50 mm lens");
            subject.mainFaceFrame=visual.body;subject.mainFaceNormal=Vector3.forward;subject.mainFaceUp=Vector3.up;subject.mainFaceTilt=Vector3.zero;
            foreach(var pair in subject.renderers)
            {
                var mesh=pair.display.GetComponent<MeshFilter>();if(mesh==null || !pair.source.enabled)continue;
                foreach(var c in pair.display.GetComponents<Collider>())UnityEngine.Object.DestroyImmediate(c);
                var c2=pair.display.gameObject.AddComponent<MeshCollider>();c2.sharedMesh=mesh.sharedMesh;
            }
            AddPart(subject,visual.back,"Back cover",SafeInspectionPart.Motion.Hinge,Vector3.up,110,.001f);
            AddPart(subject,visual.shutter,"Shutter (inspection only)",SafeInspectionPart.Motion.Button,Vector3.down,0,.0012f);
            AddPart(subject,visual.rewindKnob,"Rewind knob",SafeInspectionPart.Motion.Dial,Vector3.up,360,0);
            AddPart(subject,visual.focusRing,"Focus ring",SafeInspectionPart.Motion.Dial,Vector3.forward,160,0);
            subject.parts=subject.GetComponentsInChildren<SafeInspectionPart>(true);subject.surfaces=subject.GetComponentsInChildren<Collider>(true);subject.RefreshFraming();
            house.safeInspection.subjects=house.safeInspection.subjects.Where(s=>s!=null && s!=old).Concat(new[]{subject}).ToArray();
            var link=source.GetComponent<InspectionEntry>();if(link!=null)link.subject=subject;
            if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);
            EditorUtility.SetDirty(house.safeInspection);EditorSceneManager.MarkSceneDirty(house.gameObject.scene);EditorSceneManager.SaveScene(house.gameObject.scene);
            return "House camera entry and inspection display rebound";
        }
        static void AddPart(SafeInspectionSubject s,Transform source,string label,SafeInspectionPart.Motion motion,Vector3 axis,float range,float travel)
        {
            var display=s.poses.First(p=>p.source==source).display;var part=display.gameObject.AddComponent<SafeInspectionPart>();
            part.label=label;part.motion=motion;part.axis=axis;part.range=range;part.travel=travel;
        }
    }
}
