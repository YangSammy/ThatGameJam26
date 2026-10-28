using System;
using System.Collections.Generic;
using UnityEngine;

namespace MemoryWhitebox.V3
{
    // This driver uses the same focus, key and pointer endpoints as a player; no state teleport.
    public static class PaintingV3Checks
    {
        public static Action<string,WorldChapter3D> CaptureStage;
        static readonly List<string> results=new List<string>();
        static void Check(bool ok,string description){if(!ok)throw new Exception("Painting V3: "+description);results.Add(description);}
        static WBInput Keys(params KeyCode[] keys){var i=new WBInput();foreach(var k in keys)i.Keys.Add(k);return i;}
        static void Step(PaintingChapter s,params KeyCode[] keys){s.Tick(.025f,Keys(keys));}
        static void Focus(PaintingChapter s,int index)
        {
            s.SetBoardOpen(false);s.SetGaze(index,true);
            for(int i=0;i<52;i++)Step(s);
            s.SetGaze(-1,false);Check(s.Painted[index],"focus imports object "+index);
        }
        static void Capture(string name,PaintingChapter state)
        {
            if(CaptureStage==null)return;
            Camera camera=Camera.main;if(camera==null)throw new Exception("Painting capture requires Camera.main");
            Vector3 p=camera.transform.position;Quaternion q=camera.transform.rotation;Rect rect=camera.rect;float f=camera.fieldOfView;
            var world=new PaintingWorld3D(null,state);
            try
            {
                world.Activate();world.PrepareQAFrame();
                CaptureStage(name,world);
            }
            finally{world.Deactivate();world.Dispose();camera.transform.SetPositionAndRotation(p,q);camera.rect=rect;camera.fieldOfView=f;}
        }
        static void Park(PaintingChapter s)
        {
            Focus(s,0);Focus(s,1);s.SetBoardOpen(true);
            bool rode=false,launched=false;
            for(int n=0;n<2200&&s.Stage=="park";n++)
            {
                if(s.OnSwing)
                {
                    rode=true;
                    if(s.SwingAngle>.79f&&s.SwingSeat.x>6.9f){Step(s,KeyCode.D,KeyCode.Space);launched=true;}
                    else Step(s,KeyCode.D);
                }
                else if(s.Grounded&&s.Position.x>1.85f&&s.Position.x<3.3f)Step(s,KeyCode.D,KeyCode.Space);
                else Step(s,KeyCode.D);
                if(n==70)Capture("painting-park-swing-and-bridge",s);
            }
            Check(rode&&launched,"park route requires boarding and releasing the swing");
            Check(s.Stage=="fade-out","ordinary bridge crosses the second gap to park exit ("+s.Position+", "+s.Stage+")");
            for(int i=0;i<60&&s.Stage!="room";i++)Step(s);
            Check(s.Act==1&&s.Stage=="room","white transition enters the empty room");
        }
        public static string[] Run()
        {
            results.Clear();var s=new PaintingChapter();
            s.SetGaze(0,true);for(int n=0;n<20;n++)Step(s);
            Check(!s.Painted[0],"a brief glance cannot import a source object");
            s.SetGaze(-1,false);Step(s);Check(s.FocusProgress==0,"looking away resets incomplete focus");
            Park(s);
            Focus(s,2);Focus(s,3);Focus(s,4);
            s.SetBoardOpen(true);
            Check(s.BeginPendantDrag(),"found pendant waits as a manually draggable token");
            Check(!s.EndPendantDrag(true)&&!s.KeyInserted,"a remote pendant drop cannot unlock the distant door");
            Vector2 paused=s.Position;float flower=s.FlowerAngle;
            s.SetBoardOpen(false);for(int n=0;n<80;n++)Step(s,KeyCode.D,KeyCode.Space);
            Check(Vector2.Distance(paused,s.Position)<.001f&&Mathf.Abs(s.FlowerAngle-flower)<.001f,"lowering the book pauses actor and flower physics");
            s.SetBoardOpen(true);
            bool pushed=false,rode=false,jumped=false;
            for(int n=0;n<2200&&s.Position.x<12.55f;n++)
            {
                if(s.OnFlower)
                {
                    rode=true;
                    if(s.FlowerSeat.x>10.25f&&s.FlowerSeat.y>2.55f){Step(s,KeyCode.D,KeyCode.Space);jumped=true;}
                    else Step(s);
                }
                else if(s.Grounded&&s.Position.x>4.35f&&s.Position.x<6.1f&&s.GateOpen)
                {pushed=true;Step(s,KeyCode.D,KeyCode.Space);}
                else if(s.Position.x<9.10f||jumped)Step(s,KeyCode.D);
                else Step(s);
                if(n==100)Capture("painting-room-weight-and-flower",s);
            }
            Check(pushed&&s.GateOpen,"luggage on the physical plate keeps the separate gate open");
            Check(rode&&jumped&&s.Position.y>=2.55f,"upright flower seat carries the actor to the high ledge");
            for(int n=0;n<100&&s.Position.x<14.2f;n++)Step(s,KeyCode.D);
            Check(s.Position.x<14.26f&&!s.KeyInserted,"pendant door blocks the last corridor until a manual insertion");
            Check(s.BeginPendantDrag(),"pendant drag begins at the door");
            paused=s.Position;flower=s.FlowerAngle;for(int n=0;n<30;n++)Step(s,KeyCode.D);
            Check(s.Position==paused&&s.FlowerAngle==flower,"dragging pauses all book physics");
            Check(!s.EndPendantDrag(false)&&!s.KeyInserted,"wrong socket release returns the pendant");
            Check(s.BeginPendantDrag(),"pendant can be retried after a wrong drop");s.SuspendInput();
            Check(!s.PendantDragging&&!s.KeyInserted,"help or focus loss cancels a key drag without unlocking the door");
            Check(s.BeginPendantDrag()&&s.EndPendantDrag(true)&&s.KeyInserted,"matching asymmetric pendant opens the final door");
            Capture("painting-pendant-door-open",s);
            for(int n=0;n<300&&s.Stage=="room";n++)Step(s,KeyCode.D);
            Check(s.Stage=="white","second exit dissolves the room while preserving foreground paper");
            for(int n=0;n<40&&s.Stage!="line";n++)Step(s);
            Check(s.Act==2&&s.Stage=="line"&&s.BoardOpen,"book becomes the persistent sheet and a straight line");
            float start=s.LineProgress;for(int n=0;n<20;n++)Step(s,KeyCode.A);Check(s.LineProgress==start,"hospital line reveals monotonically forward");
            for(int n=0;n<450&&s.LineProgress<.94f;n++)Step(s,KeyCode.D);
            Capture("painting-hospital-below-nose",s);
            for(int n=0;n<100&&s.LineProgress<1;n++)Step(s,KeyCode.D);
            Check(!s.Completed,"line model waits for the actual spatial nose threshold");
            if(CaptureStage!=null)
            {
                Camera camera=Camera.main;Vector3 eye=camera.transform.position;Quaternion rotation=camera.transform.rotation;
                Rect rect=camera.rect;float fov=camera.fieldOfView;
                var world=new PaintingWorld3D(null,s);
                try
                {
                    world.Activate();world.PrepareQAFrame();
                    Check(world.HospitalNoseMarker!=null&&world.NoseProjectionSamples>0,"runtime hospital samples its actual rig nose marker with the real camera");
                    Check(world.HospitalRevealEdge>=world.NoseViewportY-.001f,"spatial reveal edge reaches the projected nose before cutting");
                    Check(!world.HospitalVisible&&world.PersistentPaperVisible,"runtime cut hides the hospital immediately while foreground paper survives");
                    CaptureStage("painting-nose-threshold-cut",world);
                }
                finally{world.Deactivate();world.Dispose();camera.transform.SetPositionAndRotation(eye,rotation);camera.rect=rect;camera.fieldOfView=fov;}
            }
            else s.ObserveHospitalReveal(true);
            Check(s.Completed&&s.MemoryAbruptCut&&s.NoseThresholdReached,"nose threshold performs the abrupt cut without revealing a full face");
            return results.ToArray();
        }
    }
}
