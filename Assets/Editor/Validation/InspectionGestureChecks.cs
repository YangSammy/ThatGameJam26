using System;
using UnityEngine;
namespace MemoryWhitebox.Editor
{
    public static class InspectionGestureChecks
    {
        public static string Result="Not run";
        static readonly System.Collections.Generic.List<string> checks=new System.Collections.Generic.List<string>();
        static void Check(bool ok,string name){if(!ok)throw new Exception("GESTURE_FAIL: "+name);checks.Add(name);}
        static void Event(SafeInspection v,EventType type,int button,Vector2 p,Vector2 delta=default(Vector2))
        {v.PointerEvent(type,button,p,delta);}
        static void Up(SafeInspection v,Vector2 p){Event(v,EventType.MouseUp,0,p);Event(v,EventType.MouseUp,1,p);}
        static Vector2 Point(SafeInspection v,SafeInspectionSubject s,SafeInspectionPart part)
        {
            for(int attempt=0;attempt<24;attempt++)
            {
                v.Advance(.4f);Physics.SyncTransforms();
                foreach(var col in part.GetComponentsInChildren<Collider>(true))
                {
                    Vector3 p=v.displayCamera.WorldToScreenPoint(col.bounds.center);
                    if(p.z>0 && p.x>180 && p.x<Screen.width-30 && p.y>120 && p.y<Screen.height-70 && !v.PointerOverUI(p) && s.Pick(v.displayCamera.ScreenPointToRay(p))==part)return p;
                }
                v.Rotate(new Vector2(110,75));
            }
            throw new Exception("No visible part target: "+part.label);
        }
        static void Reset(SafeInspection v){Up(v,Vector2.zero);v.ResetView();v.Advance(1);Physics.SyncTransforms();}
        public static string Start()
        {
            var v=UnityEngine.Object.FindObjectOfType<SafeInspection>();
            if(!Application.isPlaying || v==null || v.Active)throw new Exception("Start in authored scene outside inspection");
            checks.Clear();Result="Running";v.StartCoroutine(Run(v));return Result;
        }
        static System.Collections.IEnumerator Run(SafeInspection v)
        {
            bool oldSuppress=PhotoProgressStore.SuppressWrites;PhotoProgressStore.SuppressWrites=true;
            try
            {
                SafeInspectionSubject portable=null,device=null;
                foreach(var s in v.subjects){if(!s.fixedDevice && s.parts.Length>0 && s.parts[0].motion==SafeInspectionPart.Motion.Dial && portable==null)portable=s;if(s.fixedDevice && s.parts.Length>0 && s.parts[0].motion==SafeInspectionPart.Motion.Button)device=s;}
                Check(portable!=null && device!=null,"Authored portable and fixed device fixtures available");
                Check(v.Open(portable),"Portable opens");v.Advance(1);
                var part=portable.parts[0];var original=part.transform.localRotation;var source=portable.source.localToWorldMatrix;
                var home=portable.displayRoot.rotation;var homeDistance=v.Distance;
                var p=Point(v,portable,part);var before=portable.displayRoot.rotation;
                Event(v,EventType.MouseDown,1,p);Event(v,EventType.MouseDrag,1,p,new Vector2(100,0));v.Advance(.2f);
                Check(v.Active && Quaternion.Angle(before,portable.displayRoot.rotation)<.01f && !part.Held,"Right alone neither exits nor operates or rotates");Up(v,p);
                v.UpdateHover(p);Check(v.HoveredPart==part && part.Highlighted,"Visible part has subtle hover feedback");
                v.UpdateHover(Vector2.zero);Check(v.HoveredPart==null && !part.Highlighted,"Hover clears outside object/UI");
                foreach(int first in new[]{0,1})
                {
                    Reset(v);p=Point(v,portable,part);before=portable.displayRoot.rotation;
                    Event(v,EventType.MouseDown,first,p);v.Advance(.04f);Check(!part.Held && part.Value==0,"Chord grace suppresses part press order "+first);
                    Event(v,EventType.MouseDown,1-first,p);Event(v,EventType.MouseDrag,0,p,new Vector2(100,30));v.Advance(.1f);
                    Check(Quaternion.Angle(before,portable.displayRoot.rotation)>10 && part.Value==0 && !part.Held,"Both buttons rotate without touching part order "+first);
                    before=portable.displayRoot.rotation;
                    Event(v,EventType.MouseUp,first,p);Event(v,EventType.MouseDrag,1-first,p,new Vector2(100,0));v.Advance(.1f);
                    Check(Quaternion.Angle(before,portable.displayRoot.rotation)<.01f && part.Value==0,"Releasing either button stops rotation without falling into part drag "+first);
                    Event(v,EventType.MouseDown,first,p);Event(v,EventType.MouseDrag,0,p,new Vector2(100,0));v.Advance(.1f);
                    Check(Quaternion.Angle(before,portable.displayRoot.rotation)<.01f,"Repress cannot restart until both buttons released "+first);Up(v,p);
                }
                Reset(v);p=Point(v,portable,part);before=portable.displayRoot.rotation;
                Check(part.motion==SafeInspectionPart.Motion.Dial,"Portable fixture exposes a spindle dial");
                float distance=v.Distance;
                Event(v,EventType.MouseDown,0,p);v.PointerEvent(EventType.ScrollWheel,0,p,Vector2.zero,2);
                Check(part.Value==0 && Mathf.Abs(v.Distance-distance)<.001f,"Pending dial wheel buffers without changing part or zoom");
                v.Advance(.1f);
                Check(part.Value>0 && Mathf.Abs(v.Distance-distance)<.001f,"Held dial receives buffered wheel without zoom");Up(v,p);
                v.PointerEvent(EventType.ScrollWheel,0,p,Vector2.zero,1);v.Advance(.3f);
                Check(Mathf.Abs(v.Distance-distance)>.01f,"Wheel returns to zoom after release");
                Reset(v);p=Point(v,portable,part);before=portable.displayRoot.rotation;
                Event(v,EventType.MouseDown,0,p);v.PointerEvent(EventType.ScrollWheel,0,p,Vector2.zero,2);
                Event(v,EventType.MouseDown,1,p);Event(v,EventType.MouseDrag,0,p,new Vector2(100,30));v.Advance(.2f);
                Check(part.Value==0 && Quaternion.Angle(before,portable.displayRoot.rotation)>10,"Rotation chord discards pending dial wheel");Up(v,p);
                Reset(v);p=Point(v,portable,part);before=portable.displayRoot.rotation;
                Event(v,EventType.MouseDown,0,p);v.Advance(.09f);
                Check(v.HeldPart==part && part.Held,"Left commits part after grace");
                Event(v,EventType.MouseDown,1,p);Event(v,EventType.MouseDrag,0,p,new Vector2(160,0));v.Advance(.3f);
                Check(part.Value>.99f && Quaternion.Angle(before,portable.displayRoot.rotation)<.01f,"Late right cannot turn committed part drag into rotation");
                Event(v,EventType.MouseUp,1,p);Event(v,EventType.MouseDrag,0,p,new Vector2(-160,0));v.Advance(.3f);
                Check(part.Value<.01f && part.Held,"Part drag keeps ownership when extra right is released");Up(v,p);
                Reset(v);p=Point(v,portable,part);before=portable.displayRoot.rotation;
                Event(v,EventType.MouseDown,0,p);Event(v,EventType.MouseDrag,0,p,new Vector2(160,0));Event(v,EventType.MouseUp,0,p);v.Advance(.3f);
                Check(part.Value>.99f && !part.Held && Quaternion.Angle(before,portable.displayRoot.rotation)<.01f,"Fast left down-drag-up is not lost during grace");
                Reset(v);p=Point(v,portable,part);
                Event(v,EventType.MouseDown,0,p);
                // Opening/settling can move the display during chord grace. Keep the part originally pressed.
                portable.displayRoot.position+=Vector3.right*3;Physics.SyncTransforms();
                Check(portable.Pick(v.displayCamera.ScreenPointToRay(p))!=part,"Target moved away during grace fixture");
                v.Advance(.09f);
                Check(v.HeldPart==part,"Grace commits the original part even if the display moves");
                Up(v,p);
                part.Press();part.Drag(160);part.Advance(.3f);part.Release();
                v.Zoom(2);v.Rotate(new Vector2(90,60));v.Advance(.1f);v.ResetView();
                Check(v.Resetting,"Reset animates instead of teleporting");v.Advance(.3f);
                Check(!v.Resetting && Mathf.Abs(v.Distance-homeDistance)<.001f && Quaternion.Angle(home,portable.displayRoot.rotation)<.01f && part.Value==0 && Quaternion.Angle(original,part.transform.localRotation)<.01f,"Reset restores orientation distance and captured part pose");
                Check(portable.source.localToWorldMatrix==source,"Inspection/reset never changes source object");
                p=Point(v,portable,part);Event(v,EventType.MouseDown,0,p);v.Advance(.1f);v.ResetView();v.Advance(.3f);
                Event(v,EventType.MouseDown,1,p);Event(v,EventType.MouseDrag,0,p,new Vector2(160,0));v.Advance(.3f);
                Check(part.Value==0 && !part.Held,"Reset cancels held drag and waits for release");Up(v,p);
                Reset(v);before=portable.displayRoot.rotation;var gui=v.ResetButtonRect.center;var ui=new Vector2(gui.x,Screen.height-gui.y);
                Check(v.PointerOverUI(ui),"Left-side reset button has UI hit protection");
                Event(v,EventType.MouseDown,0,ui);Event(v,EventType.MouseDown,1,p);Event(v,EventType.MouseDrag,0,p,new Vector2(160,0));v.Advance(.2f);
                Check(Quaternion.Angle(before,portable.displayRoot.rotation)<.01f && part.Value==0,"Gesture starting on reset UI never leaks onto object");Up(v,p);
                v.Close();yield return null;yield return null;yield return null;v.ReleaseInputGate(true);
                Check(v.Open(device),"Fixed recorder opens");v.Advance(1);part=device.parts[0];p=Point(v,device,part);before=device.displayRoot.rotation;
                Event(v,EventType.MouseDown,0,p);Event(v,EventType.MouseDown,1,p);Event(v,EventType.MouseDrag,0,p,new Vector2(160,60));v.Advance(.3f);
                Check(part.Value==0 && Quaternion.Angle(before,device.displayRoot.rotation)<.01f,"Fixed recorder ignores rotation chord without pressing its key");Up(v,p);
                Event(v,EventType.MouseDown,0,p);v.Advance(.3f);Check(part.Value>.99f && part.Held,"Left hold depresses recorder button");
                Event(v,EventType.MouseUp,0,p);v.Advance(.3f);Check(part.Value==0 && !part.Held,"Release springs button back");
                v.Zoom(3);v.Advance(.3f);v.ResetView();v.Advance(.3f);Check(Mathf.Abs(v.Distance-.85f)<.001f,"Fixed recorder reset restores viewing distance");
                v.Close();yield return null;yield return null;yield return null;v.ReleaseInputGate(true);
                Result=checks.Count+" inspection gesture checks passed";
            }
            finally
            {
                v.Close();PhotoProgressStore.SuppressWrites=oldSuppress;if(Result=="Running")Result="FAILED after "+checks.Count+" checks";
                System.IO.File.WriteAllText("QA/inspection-gestures-"+UnityEngine.SceneManagement.SceneManager.GetActiveScene().name+".txt",Result+"\n"+string.Join("\n",checks));
            }
        }
        public static string Baseline()
        {
            var v=UnityEngine.Object.FindObjectOfType<SafeInspection>();
            if(!Application.isPlaying || v==null)throw new Exception("Play authored inspection scene first");
            if(!v.Open(v.subjects[0]))throw new Exception("Cannot open inspection");
            try
            {
                v.Advance(1);
                v.PointerEvent(EventType.MouseDown,1,new Vector2(Screen.width*.5f,Screen.height*.5f),Vector2.zero);
                if(!v.Active)throw new Exception("GESTURE_FAIL: right button must not close inspection");
                return "Right button no longer exits";
            }
            finally{v.Close();}
        }
    }
}
