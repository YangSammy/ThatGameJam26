using System;
using System.Collections.Generic;
using UnityEngine;

namespace MemoryWhitebox.V3
{
    public static class WritingV3Checks
    {
        public static Action<string,WorldChapter3D> CaptureStage;
        static readonly List<string> results=new List<string>();
        static float pen;
        static void Check(bool ok,string name){if(!ok)throw new Exception("Writing V3: "+name);results.Add(name);}
        static void Ink(WritingChapter s)
        {
            if(s.AwaitingReturn){s.PointerMove(new Vector2(100,240));pen=100;}
            else if(pen<100||pen>570){s.PointerUp(Vector2.zero);pen=100;s.PointerDown(new Vector2(pen,240),0);}
            pen+=8;s.PointerMove(new Vector2(pen,240));
        }
        static WBInput Keys(params KeyCode[] keys){var i=new WBInput();foreach(var k in keys)i.Keys.Add(k);return i;}
        static void Walk(WritingChapter s,Vector2 target,bool write)
        {
            for(int n=0;n<600&&Vector2.Distance(s.Player,target)>.09f;n++)
            {
                Vector2 d=target-s.Player;var i=Keys();
                if(Mathf.Abs(d.x)>.055f)i.Keys.Add(d.x>0?KeyCode.D:KeyCode.A);
                if(Mathf.Abs(d.y)>.055f)i.Keys.Add(d.y>0?KeyCode.W:KeyCode.S);
                if(write)Ink(s);
                s.Tick(.025f,i);
                if(s.SuccessLatched&&target==WritingChapter.Foyer)return;
            }
            if(Vector2.Distance(s.Player,target)>.2f)throw new Exception("Writing V3 route blocked at "+s.Player+" toward "+target);
        }
        static void Pick(WritingChapter s,int index,bool write)
        {
            Walk(s,WritingChapter.ItemPositions[index],write);
            s.Tick(.02f,Keys());if(write)Ink(s);s.Tick(.02f,Keys(KeyCode.E));
        }
        static void Crumple(WritingChapter s)
        {s.PointerDown(new Vector2(200,200),0);s.PointerUp(new Vector2(535,650));pen=0;}
        static WritingChapter Formal()
        {
            var s=new WritingChapter();pen=0;
            Pick(s,0,true);Check(s.Phase=="crumple","tutorial requires a written pickup then crumpling");
            Crumple(s);Check(s.Phase=="collecting"&&s.Draft==2&&!s.TimerStarted,"crumpling starts an untimed formal setup");
            return s;
        }
        static void Capture(string label,WritingChapter state)
        {
            if(CaptureStage==null)return;
            var camera=Camera.main;if(camera==null)throw new Exception("Writing capture requires Camera.main");
            Vector3 p=camera.transform.position;Quaternion q=camera.transform.rotation;Rect r=camera.rect;int mask=camera.cullingMask;float f=camera.fieldOfView;
            var world=new WritingWorld3D(null,state);
            try{world.Activate();world.ApplyPose();CaptureStage(label,world);}
            finally{world.Deactivate();world.Dispose();camera.transform.SetPositionAndRotation(p,q);camera.rect=r;camera.cullingMask=mask;camera.fieldOfView=f;}
        }
        public static string[] Run()
        {
            results.Clear();
            var idle=new WritingChapter();idle.PointerDown(new Vector2(100,240),0);idle.Tick(.1f,Keys());
            Check(!idle.InkFresh&&idle.WritingProgress==0,"stationary held mouse produces no ink");
            var timeout=Formal();Ink(timeout);timeout.Tick(.1f,Keys());
            Check(timeout.TimerStarted,"first genuine stroke starts the clock");
            float before=timeout.Remaining;timeout.PointerUp(Vector2.zero);
            for(int n=0;n<20;n++)timeout.Tick(.1f,Keys());
            Check(timeout.Remaining<before-1.8f,"clock continues while pen is stopped");
            for(int n=0;n<610;n++)timeout.Tick(.1f,Keys());
            Check(timeout.Phase=="crumple"&&!timeout.Completed,"timeout requires physical crumple recovery");
            int draft=timeout.Draft;timeout.PointerDown(new Vector2(200,200),0);timeout.SuspendInput();timeout.PointerUp(new Vector2(535,650));
            Check(timeout.Draft==draft&&timeout.Phase=="crumple","help or focus loss cancels a paper drag without recycling it");
            Crumple(timeout);Check(timeout.Remaining==60&&timeout.CollectedMask==0,"new draft resets only its collection and timer");
            var s=Formal();Pick(s,0,false);Check(!s.HasItem(0),"pickup without current writing is rejected");
            Pick(s,0,true);
            Walk(s,new Vector2(-.8f,.55f),true);Pick(s,1,true);
            Walk(s,new Vector2(.7f,.8f),true);Pick(s,2,true);
            Pick(s,3,true);
            Check(s.AllCollected&&!s.Completed,"all four items alone do not complete the chapter");
            Capture("writing-dual-input-four-items",s);
            Walk(s,WritingChapter.Foyer,true);
            Check(s.SuccessLatched&&s.Phase=="farewell","four items and foyer latch success before the deadline");
            Capture("writing-backlit-farewell",s);
            float remaining=s.Remaining;
            for(int n=0;n<50;n++)s.Tick(.1f,Keys());
            Check(Mathf.Abs(s.Remaining-remaining)<.001f,"latched farewell stops the countdown");
            for(int n=0;n<70&&!s.Completed;n++)
            {
                Ink(s);s.Tick(.025f,Keys(KeyCode.E));
                if(n==24)Capture("writing-farewell-walk-visible",s);
            }
            Check(s.Completed,"simultaneous writing and E completes the farewell");
            return results.ToArray();
        }
    }
}
