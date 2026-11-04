using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace MemoryWhitebox.Editor
{
    // Current authored portable inspection contract. No obsolete fixed-device/dial fixtures.
    public static class InspectionGestureChecks
    {
        public static string Result="Not run";
        static readonly List<string> checks=new List<string>();
        static void Check(bool ok,string name){if(!ok)throw new Exception(name);checks.Add("PASS "+name);}
        static void Pointer(SafeInspection v,EventType type,int button,Vector2 point,Vector2 delta=default(Vector2))=>v.PointerEvent(type,button,point,delta);
        static void Release(SafeInspection v,Vector2 p){Pointer(v,EventType.MouseUp,0,p);Pointer(v,EventType.MouseUp,1,p);}
        public static string Start()
        {
            var v=UnityEngine.Object.FindObjectOfType<SafeInspection>();
            if(!Application.isPlaying || v==null || v.Active)throw new Exception("Start outside inspection in authored Play scene");
            checks.Clear();Result="Running";v.StartCoroutine(Run(v));return Result;
        }
        static IEnumerator Run(SafeInspection v)
        {
            bool suppress=PhotoProgressStore.SuppressWrites;PhotoProgressStore.SuppressWrites=true;
            try
            {
                var subject=v.subjects.FirstOrDefault(s=>s!=null && !s.fixedDevice && s.source!=null);
                Check(subject!=null,"Authored portable inspection subject");
                var source=subject.source.localToWorldMatrix;
                var eye=v.view.transform.position;var rotation=v.view.transform.rotation;float fov=v.view.fieldOfView;
                Check(v.Open(subject),"Open authored display");
                // Allow the real entry handoff to observe released inputs.
                for(int i=0;i<8;i++)yield return null;
                v.Advance(1);var home=subject.displayRoot.rotation;float distance=v.Distance;
                // Blank screen space avoids intentionally operating a structural part.
                var p=new Vector2(Screen.width*.85f,Screen.height*.8f);
                Pointer(v,EventType.MouseDown,1,p);Pointer(v,EventType.MouseDrag,1,p,new Vector2(100,30));v.Advance(.2f);
                Check(v.Active && Quaternion.Angle(home,subject.displayRoot.rotation)<.01f,"RMB alone neither rotates nor exits");Release(v,p);
                foreach(int first in new[]{0,1})
                {
                    var before=subject.displayRoot.rotation;
                    Pointer(v,EventType.MouseDown,first,p);Pointer(v,EventType.MouseDown,1-first,p);
                    Pointer(v,EventType.MouseDrag,0,p,new Vector2(100,30));v.Advance(.2f);
                    Check(Quaternion.Angle(before,subject.displayRoot.rotation)>10,"Both-button rotation order "+first);
                    before=subject.displayRoot.rotation;
                    Pointer(v,EventType.MouseUp,first,p);Pointer(v,EventType.MouseDrag,1-first,p,new Vector2(100,30));v.Advance(.2f);
                    Check(Quaternion.Angle(before,subject.displayRoot.rotation)<.01f,"Releasing either button stops rotation "+first);Release(v,p);
                }
                v.Zoom(2);v.Advance(.3f);Check(Mathf.Abs(v.Distance-distance)>.01f,"Zoom changes viewing distance");
                v.ResetView();v.Advance(1);Check(Mathf.Abs(v.Distance-distance)<.001f && Quaternion.Angle(home,subject.displayRoot.rotation)<.01f,"Reset restores distance and orientation");
                var ui=v.ResetButtonRect.center;ui.y=Screen.height-ui.y;
                Pointer(v,EventType.MouseDown,0,ui);Pointer(v,EventType.MouseDown,1,p);Pointer(v,EventType.MouseDrag,0,p,new Vector2(100,30));v.Advance(.2f);
                Check(Quaternion.Angle(home,subject.displayRoot.rotation)<.01f,"UI-started gesture does not rotate object");Release(v,p);
                Check(source==subject.source.localToWorldMatrix,"Source transform unchanged");
                v.Close();Check(Vector3.Distance(eye,v.view.transform.position)<.001f && Quaternion.Angle(rotation,v.view.transform.rotation)<.01f && Mathf.Abs(fov-v.view.fieldOfView)<.001f,"Closing restores previous operation camera");
                for(int i=0;i<8;i++)yield return null;
                v.ReleaseInputGate(true);Check(v.Open(subject),"Re-entry after release");
                v.Advance(1);Check(Mathf.Abs(v.Distance-distance)<.001f,"Re-entry starts at default viewing distance");
                Result="PASS "+checks.Count+" current inspection interface checks (not native E/keyboard acceptance)";
            }
            finally
            {
                v.Close();PhotoProgressStore.SuppressWrites=suppress;
                if(Result=="Running")Result="FAIL after "+checks.Count+" checks";
                System.IO.Directory.CreateDirectory("QA");
                System.IO.File.WriteAllText("QA/inspection-gestures-"+UnityEngine.SceneManagement.SceneManager.GetActiveScene().name+".txt",Result+"\n"+string.Join("\n",checks));
            }
        }
    }
}
