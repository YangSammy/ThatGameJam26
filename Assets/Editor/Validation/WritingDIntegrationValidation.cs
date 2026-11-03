using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using MemoryWhitebox;
using MemoryWhitebox.Art;

namespace MemoryWhitebox.EditorTools
{
    // Runs through existing world pointer handlers and chapter rules, not native OS input.
    public static class WritingDIntegrationValidation
    {
        static WritingWorld3D World => WBMain.Instance != null ? WBMain.Instance.CurrentWorld as WritingWorld3D : null;
        static readonly string ReportPath = "QA/WritingDIntegration-2026-11-02/runtime-checks.txt";
        static void Check(bool condition, string message)
        {
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(ReportPath));
            System.IO.File.AppendAllText(ReportPath, (condition ? "PASS " : "FAIL ") + message + "\n");
            if (!condition) throw new InvalidOperationException(message);
        }
        static Ray PaperRay(WritingWorld3D w, float x) => new Ray(w.World(new Vector3(x, 3, .25f)), Vector3.down);
        static void WriteStep(WritingWorld3D w, WBInput input, float dt)
        {
            var ray = PaperRay(w, -.46f);
            w.Press("paper", ray, 0);
            w.Move(PaperRay(w, .20f));
            w.Tick(dt, input);
            w.Release("paper", ray);
        }
        static void Walk(WritingWorld3D w, Vector2 goal, bool writing)
        {
            for (int i = 0; i < 2000 && w.State.Phase != "crumple" && w.State.Phase != "arrival" && Vector2.Distance(w.State.Player, goal) > 4; i++)
            {
                var d = goal - w.State.Player;
                var input = new WBInput();
                if (Mathf.Abs(d.x) > 3) input.Keys.Add(d.x > 0 ? KeyCode.D : KeyCode.A);
                if (Mathf.Abs(d.y) > 3) input.Keys.Add(d.y > 0 ? KeyCode.S : KeyCode.W);
                if (writing) { input.Keys.Add(KeyCode.E); WriteStep(w, input, .02f); }
                else w.Tick(.02f, input);
            }
            Check(w.State.Phase == "arrival" || Vector2.Distance(w.State.Player, goal) <= (w.State.Phase == "crumple" ? 46 : 4), "Walk through existing movement to " + goal);
        }
        public static string PreparationAndCancel()
        {
            var w = World; var h = WBMain.Instance;
            Check(Application.isPlaying && w != null && w.Active, "House autumn active in Play Mode");
            h.SetHelpVisible(false);
            Check(w.Root.GetComponentsInChildren<WritingPropVisual>(true).Length >= 13, "D01-D06 instantiated through production chapter");
            Physics.SyncTransforms();
            Check(w.Pick(PaperRay(w, -.3f)) == "paper", "Actual ray hits retained paper hotspot");
            WriteStep(w, new WBInput(), .02f);
            Check(w.State.WritingProgress > 0 && w.InkStrokeCount > 0, "Writing produces persistent ink");
            Walk(w, w.State.ItemPositions[3], true);
            Check(w.State.Phase == "crumple" && w.State.HasItem(3), "Tutorial collection enters stalled draft");
            w.Press("paper", PaperRay(w, -.3f), 0); w.Tick(.125f, new WBInput());
            var folded = w.Root.GetComponentsInChildren<WritingPropVisual>(true).First(v => v.Capture().crumple > 0);
            Check(w.PaperGrabbed && folded.Capture().crumple > 0 && folded.Capture().crumple < 1, "Partial crumple uses D01 continuous morph");
            w.CancelPointerGesture();
            Check(!w.PaperGrabbed && w.State.Draft == 1 && w.Nodes["paper"].activeSelf, "Cancel restores paper without consuming draft");
            w.Press("paper", PaperRay(w, -.3f), 0);
            for (int i=0;i<4;i++)w.Tick(.1f,new WBInput());
            var binRay = new Ray(w.BinCenter + Vector3.up * 2, Vector3.down);
            Physics.SyncTransforms();
            string bin = w.Pick(binRay);
            Check(bin.StartsWith("bin", StringComparison.Ordinal), "Actual ray reaches basket receiver");
            w.Move(binRay); w.Release(bin, binRay); w.Tick(0,new WBInput());
            Check(w.State.Draft == 2 && w.State.Collected == 0 && w.State.Phase == "collect", "Drop starts formal draft and clears tutorial collection");
            Check(w.Root.GetComponentsInChildren<Transform>(true).Any(t=>t.name=="DiscardedDraft_0"), "Discarded D01 paper ball visible");
            Walk(w,w.State.ItemPositions[3],true);
            Check(w.State.HasItem(3) && w.State.Remaining < 60, "Formal collection and clock advance");
            var timer=w.Root.GetComponentsInChildren<WritingPropVisual>(true).First(v=>v.name.Contains("MechanicalTimer"));
            Check(Mathf.Abs(timer.Capture().timer-(1-w.State.Remaining/60))<.001f,"D06 follows remaining time");
            h.SetHelpVisible(true);
            Check(h.SaveStableProgress(), "Existing stable save succeeds");
            SessionState.SetString("WritingD.ExpectedCheckpoint",w.State.CaptureCheckpoint());
            h.LeaveStation();
            return "Preparation, ray drag/drop, cancel and stable save passed; exiting through host.";
        }
        public static string Reenter()
        {
            Check(!WBMain.Instance.InStation, "Host completed exit to exploration");
            WBMain.Instance.Select(2);WBMain.Instance.SetHelpVisible(true);
            return "Re-enter requested without resetting via debug shortcut.";
        }
        public static string RestoreAndComplete()
        {
            var h=WBMain.Instance;var w=World;
            Check(h.InStation && w.Active,"Ordinary re-entry active");
            h.SetHelpVisible(true);
            var expected=new WritingChapter();expected.RestoreCheckpoint(SessionState.GetString("WritingD.ExpectedCheckpoint",""));
            Check(w.State.Draft==expected.Draft && w.State.Collected==expected.Collected && w.State.Row==expected.Row && Mathf.Abs(w.State.WritingProgress-expected.WritingProgress)<.01f,"Exit/re-entry preserves draft, collection and ink");
            Check(w.State.Remaining<=expected.Remaining && expected.Remaining-w.State.Remaining<5,"Clock retained across exit/re-entry");
            Check(w.InkStrokeCount>0 && !w.HasPointerGesture,"Ink restored without held gesture");
            var reloaded=new WritingChapter();reloaded.RestoreCheckpoint(w.State.CaptureCheckpoint());
            Check(reloaded.Collected==w.State.Collected && Mathf.Approximately(reloaded.Remaining,w.State.Remaining),"Existing checkpoint format restores fresh model");
            Walk(w,w.State.ItemPositions[0],true);
            Walk(w,new Vector2(300,390),true);Walk(w,new Vector2(400,390),true);
            Walk(w,w.State.ItemPositions[1],true);Walk(w,w.State.ItemPositions[2],true);
            Check(w.State.Collected==15,"All four existing item identities collected");
            Walk(w,new Vector2(400,390),true);Walk(w,new Vector2(300,390),true);Walk(w,new Vector2(300,565),true);
            Check(w.State.Phase=="arrival","All items delivered at existing entrance");
            var input=new WBInput();input.Keys.Add(KeyCode.E);
            for(int i=0;i<90 && w.State.Phase!="farewell";i++)WriteStep(w,input,.02f);
            Check(w.State.Phase=="farewell","Writing and held E trigger existing farewell");
            for(int i=0;i<110;i++)w.Tick(.02f,new WBInput());
            Check(w.State.Completed,"Autumn completion through unchanged chapter rules");
            Check(h.SaveStableProgress(),"Completed chapter saved through existing flow");
            return "Full rule/visual integration sequence passed. Native input requires separate verification; disk reload can be inserted between Reenter and this stage.";
        }
    }
}
