using System;
using System.Collections.Generic;
using UnityEngine;
namespace MemoryWhitebox.Editor
{
    public static class DarkroomSfxChecks
    {
        static void Check(bool value,string message){if(!value)throw new Exception("DARKROOM_SFX: "+message);}
        public static string Run()
        {
            PhotoWorld3D world=null;
            try
            {
                var photo=new PhotoChapter();world=new PhotoWorld3D(null,photo);world.Root.hideFlags=HideFlags.HideAndDontSave;
                var cues=new List<int>();world.MechanicalSoundRequested+=cue=>cues.Add(cue);
                photo.Phase=PhotoChapter.FilmPhase.Exposed;
                var rewind=world.QARayTo("rewind");world.Press("rewind",rewind,0);
                Check(cues.Count==0,"holding rewind without movement is silent");
                world.Scroll("rewind",-1,rewind);Check(cues.Count==0,"clamped rewind is silent");
                world.Scroll("rewind",1,rewind);Check(cues.Count==1 && cues[0]==28,"actual rewind movement requests winding cue");
                world.CancelInteraction();int count=cues.Count;world.Scroll("rewind",1,rewind);
                Check(cues.Count==count,"cancelled rewind cannot request motion audio");
                world.Press("rewind",rewind,0);world.Scroll("rewind",8,rewind);
                Check(photo.Phase==PhotoChapter.FilmPhase.Rewound,"rewind completes through real scroll interaction");
                count=cues.Count;world.Press("back",world.QARayTo("back"),0);
                Check(cues.Count==count+1 && cues[cues.Count-1]==23,"real camera back opening requests cue");
                world.Press("back",world.QARayTo("back"),0);Check(cues.Count==count+1,"already open back is silent");
                photo.Phase=PhotoChapter.FilmPhase.DarkBag;photo.BagTurns=0;
                var bag=world.QARayTo("bag");world.Press("bag",bag,0);count=cues.Count;
                world.Scroll("bag",-1,bag);Check(cues.Count==count,"clamped bag spool is silent");
                world.Scroll("bag",1,bag);Check(cues.Count==count+1 && cues[cues.Count-1]==29,"actual bag spool movement requests cue");
                world.CancelInteraction();count=cues.Count;world.Scroll("bag",1,bag);
                Check(cues.Count==count,"cancelled bag gesture is silent");
                photo.Phase=PhotoChapter.FilmPhase.Processing;photo.Chemistry=0;
                // QARayTo targets the low tank front: its ray starts below the y=1.2 agitation
                // plane, so Point returns its fallback for both horizontally shifted rays.
                var tankPosition=world.Nodes["tank"].transform.position;
                var tank=new Ray(new Vector3(tankPosition.x,world.Origin.y+2.2f,tankPosition.z),Vector3.down);
                var movedTank=new Ray(tank.origin+Vector3.right*.2f,tank.direction);
                Check(Vector3.Distance(world.Point(tank,1.2f),world.Point(movedTank,1.2f))>.19f,"fixture rays intersect agitation plane at distinct points");
                Check(!world.HasActiveGesture,"cancelled bag leaves no active gesture before agitation");
                world.Press("tank",tank,0);count=cues.Count;
                Check(world.HasActiveGesture,"processing tank press starts agitation hold");
                world.Move(tank);Check(cues.Count==count,"stationary tank hold is silent");
                world.Move(movedTank);
                Check(cues.Count==count+1 && cues[cues.Count-1]==130,"actual tank movement requests agitation segment");
                world.CancelInteraction();count=cues.Count;world.Move(new Ray(tank.origin+Vector3.right*.4f,tank.direction));
                Check(cues.Count==count,"cancelled agitation is silent");
                Check(world.MechanicalSoundRequestCount==cues.Count,"observable requests match actual controller events");
                return "Darkroom SFX controller checks passed: rewind/bag movement versus clamps/holds, back opening versus repeat, agitation versus stationary/cancelled gestures. Null-host generated fixture emits semantic requests only; no sound playback, shared saves, native print/drop acceptance or lifecycle audibility claims.";
            }
            finally{world?.Dispose();}
        }
    }
}
