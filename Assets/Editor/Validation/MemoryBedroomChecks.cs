using System;
using UnityEngine;
namespace MemoryWhitebox.EditorTools
{
    public static class MemoryBedroomChecks
    {
        static void Require(bool ok,string message){if(!ok)throw new Exception(message);}
        static void Stroke(WritingChapter s){s.PointerDown(new Vector2(700,200),0);s.PointerMove(new Vector2(710,200));}
        public static string InputContract()
        {
            var e=new WBInput();e.Keys.Add(KeyCode.E);
            var s=new WritingChapter{Draft=2,Phase="collect"};s.Player=s.ItemPositions[0];
            s.Tick(.02f,e);Require(!s.Carried,"E without writing must only prepare");
            s.Tick(.08f,new WBInput());Stroke(s);s.Tick(.02f,new WBInput());
            Require(s.HasItem(0),"A short E tap must buffer for resumed writing");
            var expired=new WritingChapter{Draft=2,Phase="collect"};expired.Player=expired.ItemPositions[0];
            expired.Tick(.02f,e);expired.Tick(.3f,new WBInput());Stroke(expired);expired.Tick(.02f,new WBInput());
            Require(!expired.Carried,"Expired E intent must not collect");
            var stopped=new WritingChapter{Draft=2,Phase="collect"};stopped.Player=stopped.ItemPositions[0];
            Stroke(stopped);stopped.Tick(.02f,new WBInput());stopped.Tick(.02f,e);
            Require(!stopped.Carried,"Stationary pen must not count as writing on a later frame");
            Stroke(stopped);stopped.Tick(.02f,e);Require(stopped.HasItem(0),"Held E plus new ink collects");
            var hint=new WritingChapter{Draft=2,Phase="collect",Started=true,Collected=7,Player=new Vector2(300,565)};
            hint.Tick(.02f,new WBInput());Require(!hint.MissingItemHintUsed,"Girl missing-item cue removed");
            return "PASS buffered E, expiry, stationary pen, held E, no girl cue";
        }
        public static string FarewellContract(float dt=1f/60)
        {
            var e=new WBInput();e.Keys.Add(KeyCode.E);
            var s=new WritingChapter{Draft=2,Phase="collect",Started=true,Collected=15,Player=new Vector2(330,560)};
            s.PointerDown(new Vector2(700,200),0);s.PointerMove(new Vector2(710,200));s.Tick(dt,e);
            Require(s.Phase=="arrival","All items at entrance latch arrival");
            float ink=s.WritingProgress;s.PointerMove(new Vector2(711,200));s.Tick(dt,e);
            Require(s.WritingProgress>ink,"Arrival must preserve an already held writing gesture");
            float remaining=s.Remaining,x=711;
            // Mouse events may arrive less frequently than render frames.
            for(int i=0;i<Mathf.CeilToInt(3/dt) && s.Phase=="arrival";i++)
            {if(i%2==0){x+=1;s.PointerMove(new Vector2(x,200));}s.Tick(dt,e);}
            Require(s.Phase=="farewell","Sparse valid writing + held E must finish farewell hold");
            Require(s.Remaining==remaining,"Farewell hold must not restart countdown");
            s.Tick(2.1f,new WBInput());Require(s.Completed,"Farewell animation completes chapter");
            var stopped=new WritingChapter{Draft=2,Phase="arrival",Collected=15};
            Stroke(stopped);stopped.Tick(dt,e);float progress=stopped.FarewellProgress;
            stopped.Tick(.05f,e);Require(stopped.FarewellProgress==progress,"Idle frame cannot advance farewell");
            stopped.Tick(.3f,e);Require(stopped.FarewellProgress==0 && stopped.Phase=="arrival","Long stopped pen resets hold, E alone cannot finish");
            Stroke(stopped);stopped.Tick(dt,e);stopped.Tick(dt,new WBInput());Require(stopped.FarewellProgress==0,"Releasing E resets hold");
            return "PASS arrival preserves drag; sparse ink completes; idle cannot advance; E release resets; animation completes at dt="+dt;
        }
        public static string CarryPresentation()
        {
            var s=new WritingChapter{Draft=2,Phase="collect"};var w=new WritingWorld3D(null,s);
            var flags=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;
            try
            {
                var held=(Transform[])typeof(WritingWorld3D).GetField("inventoryProps",flags).GetValue(w);
                var items=(Transform[])typeof(WritingWorld3D).GetField("collectionProps",flags).GetValue(w);
                var progress=(float[])typeof(WritingWorld3D).GetField("pickupProgress",flags).GetValue(w);
                var actor=(Transform)typeof(WritingWorld3D).GetField("player",flags).GetValue(w);
                var carry=typeof(WritingWorld3D).GetMethod("UpdateCarryPresentation",flags);
                s.Collected=1;progress[0]=0;carry.Invoke(w,new object[]{.02f,false});
                Require(held[0].gameObject.activeSelf && Vector3.Distance(held[0].position,items[0].position)<.001f,"Pendant begins at dish");
                progress[0]=.65f;carry.Invoke(w,new object[]{.02f,false});
                Require(held[0].gameObject.activeSelf,"Pendant visible while moving to collar");
                progress[0]=.85f;carry.Invoke(w,new object[]{.02f,false});
                Require(actor.InverseTransformPoint(held[0].position).z>.2f && actor.InverseTransformPoint(held[0].position).y>.7f,"Pendant reaches collar/chest before hiding");
                progress[0]=1;carry.Invoke(w,new object[]{.02f,false});
                Require(!held[0].gameObject.activeSelf && s.HasItem(0),"Pendant tucked away without losing possession");
                s.Collected=3;progress[1]=1;carry.Invoke(w,new object[]{.02f,false});
                Require(held[1].parent.name=="BouquetSatchelAnchor" && held[1].localPosition.sqrMagnitude<.00001f,"Bouquet mounted to satchel anchor");
                actor.localPosition+=new Vector3(.3f,0,.2f);actor.localRotation=Quaternion.Euler(0,135,0);carry.Invoke(w,new object[]{.02f,false});
                Require(held[1].localPosition.sqrMagnitude<.00001f,"Bouquet follows turns without drifting");
                return "PASS pendant dish -> visible collar/chest -> hidden while retained; bouquet attached to satchel and follows turns";
            }
            finally{w.Dispose();}
        }
        public static string Run(float dt=.02f)
        {
            var s=new WritingChapter();s.Draft=2;s.Phase="collect";s.Player=new Vector2(330,450);
            Require(s.ItemPositions[3].x>420,"Letter must be on the east desk");
            // Reach each object through actual WASD movement, with simultaneous writing/E.
            Vector2[] route={new Vector2(240,450),new Vector2(240,320),new Vector2(330,320),new Vector2(330,280),new Vector2(445,280),new Vector2(445,360),new Vector2(445,505),new Vector2(330,505),new Vector2(330,560)};
            foreach(var goal in route)
            {
                for(int n=0;n<Mathf.CeilToInt(16/dt) && Vector2.Distance(s.Player,goal)>4 && s.Phase!="arrival";n++)
                {
                    var d=goal-s.Player;var input=new WBInput();
                    if(Mathf.Abs(d.x)>2)input.Keys.Add(d.x>0?KeyCode.D:KeyCode.A);
                    if(Mathf.Abs(d.y)>2)input.Keys.Add(d.y>0?KeyCode.S:KeyCode.W);
                    input.Keys.Add(KeyCode.E);
                    s.PointerDown(new Vector2(700,200),0);s.PointerMove(new Vector2(710,200));s.Tick(dt,input);s.PointerUp(Vector2.zero);
                }
                Require(s.Phase=="arrival" || Vector2.Distance(s.Player,goal)<=4,"Blocked route to "+goal);
            }
            Require(s.Collected==15 && s.Phase=="arrival","Four objects reach entrance before timeout: collected="+s.Collected+" phase="+s.Phase+" remaining="+s.Remaining);
            float time=s.Remaining;Require(60-time>=24 && 60-time<=28,"At 1.5x speed the former 38s route must take about 25s; actual="+(60-time));s.Tick(2,new WBInput());Require(s.Remaining==time,"Arrival stops clock");
            var restored=new WritingChapter();restored.RestoreCheckpoint(s.CaptureCheckpoint());Require(restored.Collected==15 && restored.Phase=="arrival","Checkpoint retains arrival");
            var blocked=new WritingChapter();blocked.Player=new Vector2(245,320);
            var left=new WBInput();left.Keys.Add(KeyCode.A);
            for(int i=0;i<50;i++)blocked.Tick(.02f,left);
            Require(blocked.Player.x>228,"Bedside cabinet blocks walking through it");
            var old=new WritingChapter();old.Player=new Vector2(130,320);
            var migrated=new WritingChapter();migrated.RestoreCheckpoint(old.CaptureCheckpoint());
            Require(migrated.Player==new Vector2(330,450),"Old checkpoint inside bed returns to clear center");
            var paused=new WritingChapter();paused.Player=new Vector2(240,320);
            var e=new WBInput();e.Keys.Add(KeyCode.E);paused.Tick(.02f,e);
            Require(paused.Collected==0,"E without writing does not collect");
            var idle=new WritingChapter();idle.Draft=2;idle.Phase="collect";idle.Started=true;idle.Tick(1,new WBInput());Require(idle.Remaining==59,"Pen-up still consumes time");
            return "PASS: layout, continuous walk/collect route, arrival clock stop, checkpoint, pen-up timer. Route remaining="+time;
        }
    }
}
