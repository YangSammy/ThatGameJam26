using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
namespace MemoryWhitebox.Editor
{
    // Isolated rule tests; no production saves or scene assets are modified.
    public static class NarrativeRegressionChecks
    {
        static readonly List<string> results=new List<string>();
        static int failed;
        static WBInput Keys(params KeyCode[] keys){var i=new WBInput();foreach(var k in keys)i.Keys.Add(k);return i;}
        static void Require(bool ok,string why){if(!ok)throw new Exception(why);}
        static void Case(string name,Action test){try{test();results.Add("PASS "+name);}catch(Exception e){failed++;results.Add("FAIL "+name+": "+e.GetBaseException().Message);}}
        static void Stroke(WritingChapter w){w.PointerDown(new Vector2(700,200),0);w.PointerMove(new Vector2(720,200));}
        static void Frames(PaintingChapter p,int count,params KeyCode[] keys){var input=Keys(keys);for(int i=0;i<count;i++)p.Tick(.02f,input);}
        static WritingChapter Formal()=>new WritingChapter{Phase="collect",Draft=2};
        [MenuItem("Memory Whitebox/Run Narrative P1 Regression Checks")]
        public static void Run()
        {
            if(EditorApplication.isPlaying)throw new Exception("Run outside Play Mode.");
            results.Clear();failed=0;var host=WBMain.Instance;bool qa=WBMain.IsQA;WBMain.Instance=null;WBMain.IsQA=true;
            try
            {
                Case("Autumn simultaneous writing and E",()=>{
                    var w=new WritingChapter();w.Player=w.ItemPositions[0];w.Tick(.02f,Keys(KeyCode.E));Require(!w.Carried,"E alone collected");
                    w.Tick(.3f,Keys());Stroke(w);w.Tick(.02f,Keys());Require(!w.Carried,"Writing alone collected");Stroke(w);w.Tick(.02f,Keys(KeyCode.E));Require(w.Phase=="crumple","Tutorial did not stall");
                    w.PointerDown(new Vector2(800,200),0);w.PointerUp(new Vector2(1185,635));Require(w.Draft==2&&w.Phase=="collect"&&!w.Started&&!w.Carried,"Draft reset incorrect");
                });
                Case("Autumn re-entering paper resumes at the new contact point",()=>{
                    var w=Formal();w.Player=w.ItemPositions[3];var start=new Vector2(690,200);
                    w.PointerDown(start,0);w.PointerMove(new Vector2(600,200));w.PointerMove(start);w.Tick(.02f,Keys(KeyCode.E));
                    Require(w.WritingProgress==0 && !w.Started && !w.Carried,"Off-paper travel produced ink");
                    w.PointerMove(start+Vector2.right*12);w.Tick(.02f,Keys(KeyCode.E));
                    Require(w.WritingProgress==12 && w.Started && w.HasItem(3),"On-paper movement failed to resume");
                });
                Case("Autumn independent timer and retry",()=>{
                    var w=Formal();w.Tick(5,Keys());Require(w.Remaining==60,"Timer started before ink");Stroke(w);w.PointerUp(Vector2.zero);w.Tick(5,Keys());Require(w.Remaining==55,"Timer paused without writing");w.Tick(55,Keys());Require(w.Phase=="crumple"&&!w.Completed,"Timeout completed");
                });
                Case("Autumn four items any order; arrival latch",()=>{
                    var w=Formal();foreach(int i in new[]{2,0,3,1}){w.Player=w.ItemPositions[i];Stroke(w);w.Tick(.02f,Keys(KeyCode.E));w.PointerUp(Vector2.zero);}Require(w.Collected==15,"Missing items");
                    w.Player=new Vector2(310,560);w.Remaining=.02f;w.Tick(.02f,Keys());Require(w.Phase=="arrival","Deadline tie failed");w.Tick(80,Keys());Require(w.Phase=="arrival","Latched arrival expired");
                    for(int i=0;i<100&&w.Phase=="arrival";i++){Stroke(w);w.Tick(.02f,Keys(KeyCode.E));}Require(w.Phase=="farewell","Writing/E farewell failed");w.Tick(2.1f,Keys());Require(w.Completed,"Farewell not completed");
                });
                Case("Autumn restore preserves ink/time but clears hold",()=>{
                    var w=Formal();Stroke(w);w.Tick(.02f,Keys());var copy=new WritingChapter();copy.RestoreCheckpoint(w.CaptureCheckpoint());Require(copy.Started&&copy.Remaining==w.Remaining&&copy.InkRows[0]==20&&!copy.InkFresh,"Restore lost ink or inherited input");
                });
                Case("Book observation requires focus; closing freezes motion",()=>{
                    var p=new PaintingChapter();p.Observe(0,false,5);Require(!p.Painted[0],"No-hold discovery");Require(p.Observe(0,true,1.3f),"Focus did not discover");p.SetBook(true);p.Actor=p.SwingSeat;p.Riding=0;Frames(p,10,KeyCode.D);p.SetBook(false);float a=p.SwingAngle,v=p.SwingSpeed;Frames(p,100,KeyCode.D);Require(p.SwingAngle==a&&p.SwingSpeed==v,"Closed book continued motion");p.SetBook(true);Frames(p,1,KeyCode.D);Require(p.SwingAngle!=a,"Opening did not resume");
                });
                Case("Act one traversable using movement, swing and jump",()=>{
                    var p=new PaintingChapter();p.Observe(0,true,1.3f);p.Observe(1,true,1.3f);p.SetBook(true);
                    for(int i=0;i<500&&p.Riding<0;i++)Frames(p,1,KeyCode.D);Require(p.Riding==0,"Cannot board swing");
                    for(int i=0;i<500&&p.SwingAngle<.4f;i++)Frames(p,1,KeyCode.D);
                    Frames(p,1,KeyCode.D,KeyCode.Space);
                    for(int i=0;i<600&&p.Phase==0;i++)Frames(p,1,KeyCode.D);
                    Require(p.Phase==1&&!p.BookOpen,"Swing/bridge route did not reach act two: "+p.Actor);
                });
                Case("Act two suitcase, flower lift and manual pendant",()=>{
                    var p=new PaintingChapter();p.RestoreCheckpoint("{\"version\":2,\"phase\":1,\"shapes\":[true,true,false,false,false],\"crate\":2}");
                    foreach(int i in new[]{2,3,4})p.Observe(i,true,1.3f);p.SetBook(true);
                    for(int i=0;i<300&&p.CrateX<4;i++)Frames(p,1,KeyCode.D);
                    Require(p.PlatePressed,"Suitcase did not hold the plate");Frames(p,1,KeyCode.D,KeyCode.Space);
                    for(int i=0;i<500&&p.Riding<0;i++)Frames(p,1,KeyCode.D);Require(p.PlatePressed&&p.Riding>0,"Cannot board flower behind suitcase gate: "+p.Actor);
                    for(int i=0;i<1200&&(p.Actor.x<9.4f||p.Actor.y<3);i++)Frames(p,1);
                    Frames(p,1,KeyCode.D,KeyCode.Space);
                    for(int i=0;i<250&&!p.AtDoor;i++)Frames(p,1,KeyCode.D);
                    Require(p.HighCheckpoint&&p.AtDoor&&!p.KeyInserted&&p.Phase==1,"Lift/door route failed: "+p.Actor);
                    Require(p.InsertPendant(),"Manual insertion rejected");Frames(p,100,KeyCode.D);Require(p.Phase>=2,"Unlocked door not passable");
                });
                Case("Checkpoint leaves moving carriers; retains unlocked shapes",()=>{
                    var p=new PaintingChapter();p.Painted[0]=true;p.Painted[1]=true;p.MiddleCheckpoint=true;p.Riding=0;p.Velocity=new Vector2(4,5);
                    var copy=new PaintingChapter();copy.RestoreCheckpoint(p.CaptureCheckpoint());Require(copy.Riding<0&&copy.Actor==new Vector2(6.8f,.7f)&&copy.Velocity==Vector2.zero&&copy.Painted[0],"Unsafe carrier restoration");
                });
                Case("Distance ward reveal freezes, preserves retreat and cuts at the end",()=>{
                    var p=new PaintingChapter();p.RestoreCheckpoint("{\"version\":2,\"phase\":3,\"shapes\":[true,true,true,true,true],\"key\":true,\"crate\":4}");
                    Frames(p,20,KeyCode.D);
                    float height=p.WardRevealHeight, peak=p.WardDistance;
                    Require(Mathf.Abs(height-p.NoseHeight*peak/p.WardWalkDistance)<.001f,"Reveal did not follow distance");
                    Frames(p,20);
                    Require(p.WardRevealHeight==height && p.Actor.x==peak,"Idle changed reveal");
                    Frames(p,10,KeyCode.A);
                    float retreated=p.Actor.x;
                    Require(retreated<peak && p.WardDistance==peak && p.WardRevealHeight==height,"Retreat erased reveal");
                    var copy=new PaintingChapter();copy.RestoreCheckpoint(p.CaptureCheckpoint());
                    Require(Mathf.Abs(copy.Actor.x-retreated)<.001f && Mathf.Abs(copy.WardDistance-peak)<.001f &&
                        Mathf.Abs(copy.WardRevealHeight-height)<.001f,"Reload lost retreated position or reveal");
                    Frames(copy,5,KeyCode.D);
                    Require(copy.Actor.x>retreated && copy.Actor.x<peak && copy.WardRevealHeight==height,"Returning under peak advanced reveal");
                    copy.Tick(60,Keys());Require(!copy.Completed,"Idle completed ward");
                    copy.Tick(copy.WardWalkDistance/copy.WardWalkSpeed,Keys(KeyCode.D));
                    Require(copy.Completed && copy.Phase==4 && copy.WardRevealHeight==copy.NoseHeight,"Distance cutoff did not finish in same tick");
                });
                Case("Legacy ward checkpoints retain recorded distance and completion",()=>{
                    const string partial="{\"version\":2,\"phase\":3,\"shapes\":[true,true,true,true,true],\"key\":true,\"ward\":1.5}";
                    var p=new PaintingChapter();p.RestoreCheckpoint(partial);
                    Require(p.Actor.x==1.5f && p.WardDistance==1.5f && p.Phase==3,"Legacy ward did not restore its recorded distance");
                    Require(Mathf.Abs(p.WardTime/PaintingChapter.WardRevealSeconds-1.5f/p.WardWalkDistance)<.001f,"Legacy distance did not migrate to equivalent reveal time");
                    var completed=new PaintingChapter();
                    completed.RestoreCheckpoint(partial.Replace("\"phase\":3","\"phase\":4"));
                    Require(completed.Actor.x==1.5f && completed.WardRevealHeight==completed.NoseHeight,"Legacy completed ward did not restore its final reveal");
                    completed.Tick(.02f,Keys());
                    Require(completed.Completed,"Legacy final phase did not complete on tick");
                });
                Case("Spring requires open door then crossing, no item gate",()=>{
                    var e=new EndingChapter();e.CrossThreshold();Require(!e.Completed,"Closed doorway completed");e.OpenDoor();Require(!e.Completed,"Click alone completed");var copy=new EndingChapter();copy.RestoreCheckpoint(e.CaptureCheckpoint());Require(copy.DoorOpen&&!copy.Completed,"Open door restore failed");copy.CrossThreshold();Require(copy.Completed,"Crossing failed");
                });
            }
            finally{WBMain.Instance=host;WBMain.IsQA=qa;}
            Directory.CreateDirectory("QA");File.WriteAllText("QA/narrative-p1-regression.txt",$"Passed {results.Count-failed}; Failed {failed}\n"+string.Join("\n",results));
            if(failed>0)throw new Exception(string.Join("\n",results));Debug.Log("NARRATIVE_P1_PASS "+results.Count);
        }
    }
}
