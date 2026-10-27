using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
namespace MemoryWhitebox.Editor
{
    public static class PhotoRecoveryChecks
    {
        static List<string> checks;
        static void Check(bool value,string name){if(!value)throw new Exception("PHOTO_RECOVERY: "+name);checks.Add(name);}
        public static string Run()
        {
            if(!Application.isPlaying)throw new Exception("Fresh HouseScene Play required");
            var directory=PhotoProgressStore.DirectoryOverride;var suppressed=PhotoProgressStore.SuppressWrites;
            try{return RunIsolated();}
            finally {
                // Stop the test-owned gameplay before restoring the player's save destination.
                PhotoProgressStore.SuppressWrites=true;
                Action<UnityEditor.PlayModeStateChange> restore=null;
                restore=state=>{if(state!=UnityEditor.PlayModeStateChange.EnteredEditMode)return;PhotoProgressStore.DirectoryOverride=directory;PhotoProgressStore.SuppressWrites=suppressed;UnityEditor.EditorApplication.playModeStateChanged-=restore;};
                UnityEditor.EditorApplication.playModeStateChanged+=restore;UnityEditor.EditorApplication.isPlaying=false;
            }
        }
        static string RunIsolated()
        {
            var h=UnityEngine.Object.FindObjectOfType<WBMain>();if(!Application.isPlaying||h==null||!h.HouseReady)throw new Exception("Fresh HouseScene Play required");
            checks=new List<string>();PhotoProgressStore.DirectoryOverride=Path.GetFullPath("QA/photo-save-test-"+DateTime.UtcNow.Ticks);PhotoProgressStore.SuppressWrites=false;
            Action enter=()=>{h.Room.WarpTo(1);Check(h.TryEnterHouseStation(1),"Enter photography");h.AdvanceHouseTransition(1);h.ReleaseHouseEntryInput();};
            enter();var w=(PhotoWorld3D)h.CurrentWorld;var m=w.Photo;Check(m.Phase==PhotoChapter.FilmPhase.Empty,"Clean test state");
            Action cycle=()=>{var phase=m.Phase;h.LeaveStation();h.AdvanceHouseTransition(1);enter();Check(m.Phase==phase,"Resume "+phase);};
            Action<string> press=id=>w.Press(id,w.QARayTo(id),0);
            Action<string,string> drag=(from,to)=>{press(from);w.Release(to,w.QARayTo(to));};
            press("film-200");cycle();Check(w.DraggedId==null&&w.Nodes["film-200"].GetComponent<Collider>().enabled,"Cancelled drag restored collider");
            drag("film-200","chamber");cycle();drag("leader","spool");cycle();Check(m.FilmThreaded,"Threading retained");press("back");cycle();press("advance");cycle();
            press("finder");w.Tick(1.1f,new WBInput());cycle();Check(!w.CameraHeld,"Held camera safely put down");
            press("shutter");Check(m.FailureReason!="","Incorrect exposure explains failure immediately");cycle();
            press("rewind");w.Scroll("rewind",4,w.QARayTo("rewind"));w.Release("",w.QARayTo("rewind"));press("rewind");Check(Mathf.Approximately(w.RewindTurns,.5f),"Rewind retains progress after release");cycle();press("rewind");Check(Mathf.Approximately(w.RewindTurns,.5f),"Rewind retains progress after re-entry");w.Scroll("rewind",4,w.QARayTo("rewind"));Check(m.Phase==PhotoChapter.FilmPhase.Rewound,"Two half rewinds finish");press("back");cycle();
            Physics.SyncTransforms();var bag=w.Nodes["bag"].GetComponent<Collider>();var eye=new Vector3(.9f,1.65f,2.3f);var ray=new Ray(eye,bag.bounds.center-eye);
            Check(w.Pick(ray)=="","Closed curtain blocks bag");Check(w.Pick(new Ray(bag.bounds.center+Vector3.back*4,Vector3.forward))=="","Distant interaction rejected");
            if(!h.Curtain.IsOpen)h.Curtain.Toggle();h.Curtain.Advance(1);Physics.SyncTransforms();
            eye=new Vector3(1.85f,1.65f,2.3f);Check(w.Pick(new Ray(eye,bag.bounds.center-eye))=="bag","Darkroom bag reachable from aisle");
            drag("exposed-film","bag");press("bag");w.Scroll("bag",4,w.QARayTo("bag"));cycle();Check(Mathf.Approximately(m.BagTurns,.4f),"Partial dark bag retained");Check(h.Room.CanWalkAt(w.Eye)&&w.Eye.x>1.5f,"Darkroom safe resume position");
            press("bag");w.Scroll("bag",6,w.QARayTo("bag"));cycle();
            drag("chemical-0","tank");cycle();Check(!m.Agitated&&m.ProcessingTime==3,"Unagitated processing retained");
            var tankRay=new Ray(w.Nodes["tank"].transform.position+Vector3.up,Vector3.down);w.Press("tank",tankRay,0);w.Move(new Ray(tankRay.origin+Vector3.right*.2f,tankRay.direction));w.Release("",tankRay);Check(m.Agitated,"Native agitation accepted");
            w.Tick(.5f,new WBInput());float remaining=m.ProcessingTime;cycle();Check(Mathf.Approximately(m.ProcessingTime,remaining),"Remaining processing time retained");
            w.SaveProgress();string warning;bool blocked;var d=PhotoProgressStore.Load(out warning,out blocked);Check(d!=null&&!blocked&&d.phase==m.Phase&&Mathf.Approximately(d.remaining,remaining),"Disk stage and timer round trip");Check(!string.IsNullOrEmpty(d.image)&&d.captured!=null,"Disk includes photograph and exposure");
            // Separate data validation from gameplay; these writes stay in the isolated test directory.
            d.remaining=1.75f;PhotoProgressStore.Save(d);File.WriteAllText(PhotoProgressStore.PathName,"broken");var recovered=PhotoProgressStore.Load(out warning,out blocked);Check(recovered!=null&&!blocked&&warning!="","Damaged primary recovers backup");
            File.WriteAllText(PhotoProgressStore.PathName,"broken");File.WriteAllText(PhotoProgressStore.PathName+".bak","broken");var missing=PhotoProgressStore.Load(out warning,out blocked);Check(missing==null&&blocked,"Both damaged copies protected from overwrite");
            PhotoProgressStore.SuppressWrites=true;
            var report=string.Join("\n",checks.ToArray());File.WriteAllText("QA/photo-recovery-checks.txt",report);return checks.Count+" checks passed; "+PhotoProgressStore.DirectoryOverride;
        }
    }
}
