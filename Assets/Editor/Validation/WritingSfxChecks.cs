using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using MemoryWhitebox.Art;

namespace MemoryWhitebox.Editor
{
    public static class WritingSfxChecks
    {
        public static string Run()
        {
            var timerCue=typeof(WritingWorld3D).GetMethod("TimerCue",BindingFlags.Static|BindingFlags.NonPublic);
            Check(60,59.9f,0);
            Check(59.1f,59,35);
            Check(15.2f,15,135);
            Check(15,14.9f,0);
            Check(1.2f,0,235);
            Check(0,0,0);
            return "PASS writing timer audio: whole seconds, final segment, timeout once";

            void Check(float before,float after,int expected)
            {
                var actual=(int)timerCue.Invoke(null,new object[]{before,after});
                if(actual!=expected)throw new InvalidOperationException($"Writing timer {before} to {after}: expected {expected}, got {actual}");
            }
        }

        public static string RunWorld()
        {
            var cues=new List<(int id,bool motion)>();
            void Record(int id,bool motion)=>cues.Add((id,motion));
            var state=new WritingChapter();
            var world=new WritingWorld3D(null,state);
            var root=new GameObject("Isolated writing SFX desk"){hideFlags=HideFlags.HideAndDontSave};
            root.transform.position=world.Root.transform.position;
            StorySfx.CueRequested+=Record;
            try
            {
                var desk=CreateDesk(root);
                world.AttachDesk(desk);
                Expect();
                var start=PaperRay(-.45f);var end=PaperRay(.42f);
                Physics.SyncTransforms();
                Require(world.Pick(start)=="paper","Fixture ray missed the paper receiver");
                var idle=new WBInput();var collect=new WBInput();collect.Keys.Add(KeyCode.E);
                world.Press("paper",start,0);world.Move(start);world.Tick(.02f,idle);Expect();
                world.Move(end);Expect((32,true));
                state.Player=state.ItemPositions[3];world.Tick(.02f,collect);Expect((134,false));
                world.Release("paper",end);world.Tick(.02f,idle);Expect();

                world.Press("paper",start,0);Expect((33,false));
                world.CancelPointerGesture();Expect();
                world.Press("paper",start,0);Expect((33,false));
                world.Release("paper",start);Expect();
                Discard(start);

                state.Player=state.ItemPositions[0];world.Tick(.02f,collect);Expect();
                Write(start,end,collect,.02f);Expect((32,true),(35,false),(34,false),(19,false));
                for(int i=1;i<4;i++)
                {
                    state.Player=state.ItemPositions[i];
                    Write(start,end,collect,.02f);Expect((32,true),(i==3?134:34,false));
                }
                world.Tick(.02f,collect);Expect();

                state.Remaining=16.1f;world.Tick(.15f,idle);Expect((35,false));
                state.Remaining=15.1f;world.Tick(.15f,idle);Expect((135,false));
                state.Remaining=.1f;world.Tick(.15f,idle);Expect((235,false));
                world.Tick(.15f,idle);Expect();

                Discard(start);
                Write(start,end,idle,.02f);Expect((32,true),(35,false));
                state.Collected=15;state.Player=new Vector2(300,565);
                world.Tick(0,idle);Expect();
                for(int i=0;i<30 && state.Phase!="farewell";i++)Write(start,end,collect,.1f);
                Require(state.Phase=="farewell" && Count(36)==1 && Count(136)==0,"Farewell gesture should sound once before travel");
                cues.Clear();
                for(int i=0;i<10;i++)world.Tick(.1f,idle);
                Expect((136,false));
                return "PASS isolated writing handlers: 019 pendant, 032 ink motion, 033 crumple, 233 bin, 133 fresh sheet, 034 object pickups, 134 envelope, 035/135/235 timer, 036 gesture, 136 departure";

                Ray PaperRay(float x)=>new Ray(desk.paper.transform.TransformPoint(new Vector3(x,1,0)),-desk.paper.transform.up);
                void Discard(Ray paper)
                {
                    world.Press("paper",paper,0);Expect((33,false));
                    var binRay=new Ray(world.BinCenter+Vector3.up,Vector3.down);
                    Physics.SyncTransforms();
                    string bin=world.Pick(binRay);
                    Require(bin.StartsWith("bin",StringComparison.Ordinal),"Fixture ray missed the bin receiver");
                    world.Move(binRay);world.Release(bin,binRay);world.Tick(0,idle);
                    Expect((233,false),(133,false));
                }
                void Write(Ray from,Ray to,WBInput input,float dt)
                {
                    world.Press("paper",from,0);world.Move(to);world.Tick(dt,input);world.Release("paper",to);
                }
            }
            finally
            {
                StorySfx.CueRequested-=Record;
                world.Dispose();
                if(Application.isPlaying)UnityEngine.Object.Destroy(root);else UnityEngine.Object.DestroyImmediate(root);
            }

            int Count(int id)=>cues.FindAll(c=>c.id==id).Count;
            void Expect(params (int id,bool motion)[] expected)
            {
                bool equal=cues.Count==expected.Length;
                for(int i=0;equal && i<expected.Length;i++)equal=cues[i].Equals(expected[i]);
                Require(equal,$"Expected [{string.Join(",",expected)}], requested [{string.Join(",",cues)}]");
                cues.Clear();
            }
        }

        static void Require(bool pass,string message)
        {if(!pass)throw new InvalidOperationException(message);}

        static AutumnWritingDesk CreateDesk(GameObject root)
        {
            var desk=root.AddComponent<AutumnWritingDesk>();
            desk.paper=Anchor("paper",Vector3.zero).gameObject.AddComponent<BoxCollider>();
            desk.paper.gameObject.layer=8;
            desk.paper.size=new Vector3(1,.02f,1);
            desk.penMotion=Anchor("pen",Vector3.zero);
            desk.penTip=Anchor("pen tip",Vector3.zero);
            desk.penGrip=Anchor("pen grip",Vector3.zero);
            desk.crumpleGrip=Anchor("crumple grip",Vector3.zero);
            var ball=Anchor("folded paper",Vector3.zero);
            desk.foldedPaper=Anchor("crumple presentation",Vector3.zero,ball).gameObject.AddComponent<WritingPropVisual>();
            desk.timer=Anchor("timer",Vector3.left).gameObject.AddComponent<WritingPropVisual>();
            desk.basketMouth=Anchor("bin",Vector3.right*2);
            desk.basketMouth.gameObject.layer=8;
            desk.basketHits=new[]{desk.basketMouth.gameObject.AddComponent<BoxCollider>()};
            desk.discardedDrafts=Array.Empty<GameObject>();
            return desk;

            Transform Anchor(string name,Vector3 at,Transform parent=null)
            {
                var result=new GameObject(name).transform;
                result.SetParent(parent!=null?parent:root.transform,false);
                result.localPosition=at;
                return result;
            }
        }
    }
}
