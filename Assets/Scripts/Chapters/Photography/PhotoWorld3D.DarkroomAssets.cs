using UnityEngine;
namespace MemoryWhitebox
{
    public sealed partial class PhotoWorld3D
    {
        DarkroomAssetVisuals darkroomVisuals;
        bool HasDarkroomAssets=>darkroomVisuals!=null;
        void SyncDarkroomAssets()
        {
            if(darkroomVisuals==null)return;
            var v=darkroomVisuals;
            v.ResetModels();
            bool bagWorking=Photo.Phase==PhotoChapter.FilmPhase.DarkBag;
            v.Sample("bag","C01_Work",bagWorking?Photo.BagTurns:0);
            v.Sample("bag","C01_FilmInside",bagWorking?Photo.BagTurns:0);
            v.Sample("reel","C02_LoadRatchet",Photo.BagTurns);
            bool negativeOut=printObjectsState!=null && printObjectsState.negativeRemoved;
            var coil=v.ModelRoot("internal-negative");
            if(coil!=null){coil.gameObject.SetActive(!negativeOut && (Photo.Phase==PhotoChapter.FilmPhase.Tank || Photo.Phase==PhotoChapter.FilmPhase.Processing));v.Sample("internal-negative","C06_Coiled");}
            // Unfixed film stays behind the tank's light trap, including while sealing after bag work.
            v.Sample("tank","C03_MainOpen",negativeOut?1:0);
            if(tankLid!=null)v.FitPartHit(tankLid.GetComponent<BoxCollider>(),"tank","MainLid_Mesh");
            bool liquid=Photo.Phase==PhotoChapter.FilmPhase.Processing || needsDrain || (dragged!=null && dragged.StartsWith("chemical-"));
            v.Sample("tank","C03_PortOpen",liquid?1:0);
            bool rinse=Photo.Phase==PhotoChapter.FilmPhase.Processing && Photo.Chemistry==3;
            v.Sample("film-drain-basin",rinse?"C05_WaterOn":"C05_WaterOff");
            v.Sample("film-drain-basin",dragged=="tank" && needsDrain && CanCommitDarkroom(lastRay,DrainTarget)?"C05_DrainOn":"C05_DrainOff");
            string[] bottles={"DeveloperBottle","StopBottle","FixerBottle"};
            for(int i=0;i<3;i++)
            {
                string id="chemical-"+i;
                v.Sample(id,"C04_"+bottles[i]+"_CapUnscrew",dragged==id?1:0);
                // Former whitebox boxes were wider/taller than the real bottles,
                // intercepting clicks on the visible shoulder of the bottle behind.
                v.FitPartHit(Nodes[id].GetComponent<BoxCollider>(),id,"Body_Mesh");
            }
            bool closed=printObjectsState!=null && printObjectsState.frameClosed;
            v.SetContactFrameClosed(closed,framePlateHit);
            v.Sample("print-exposure",printHeld?"C08_Held":"C08_Release");
            bool washing=printStage==ParkPrintStage.PaperWash && printProcessing;
            v.Sample("print-wash",washing?"C10_WashTray_WaterOn":"C10_WashTray_WaterOff");
            v.Sample("print-wash",washing?"C10_WashTray_DrainOn":"C10_WashTray_DrainOff");
            v.Sample("print-negative-clip",(printDrag=="print-negative" || printLanding=="print-negative")?"C06_Open":"C06_Closed");
            v.Sample("print-photo-clip",(printDrag=="print-paper" || printLanding=="print-paper")?"C11_ClipsOpen":"C11_ClipsClosed");
            if(printProcessing)
            {
                string tray=printStage==ParkPrintStage.PaperDeveloper?"Developer":printStage==ParkPrintStage.PaperStop?"Stop":printStage==ParkPrintStage.PaperFixer?"Fixer":printStage==ParkPrintStage.PaperWash?"Wash":null;
                if(tray!=null)v.Sample("print-"+tray.ToLowerInvariant(),"C10_"+tray+"Tray_Ripple",1-printRemaining/PrintDuration(printStage));
            }
            if(printObjectsState==null){v.UpdateDeformedBounds();return;}
            string nl=printObjectsState.negative.locationId,pl=printObjectsState.paper.locationId;
            PoseDarkroomMaterial("print-negative",nl,printDrag=="print-negative");
            PoseDarkroomMaterial("print-paper",pl,printDrag=="print-paper");
            v.Sample("print-negative",nl=="print-negative-clip"?"C06_Hanging":"C06_Flat");
            v.Sample("print-paper",pl=="print-photo-clip"?"C09_Hanging":pl!=null && (pl=="print-developer" || pl=="print-stop" || pl=="print-fixer" || pl=="print-wash")?"C09_Wet":"C09_Flat");
            float wet=printStage>=ParkPrintStage.PaperDeveloper && printStage<ParkPrintStage.Complete?1:0;
            if(printStage==ParkPrintStage.PaperDrying && printProcessing)wet=Mathf.Clamp01(printRemaining/PrintDuration(printStage));
            if(printDrag=="print-paper" && wet>0)v.Sample("print-paper","C09_Drip");
            float reveal=printStage>ParkPrintStage.PaperDeveloper?1:printStage==ParkPrintStage.PaperDeveloper && printProcessing?1-printRemaining/PrintDuration(printStage):0;
            v.Surface("print-paper","M_C_PaperContent",contactTexture,Mathf.Clamp01(reveal),wet);
            v.ShowNegatives(parkFilm!=null?parkFilm.id:null,printStage>=ParkPrintStage.NegativeDrying,printStage==ParkPrintStage.NegativeDrying?1:0);
            v.UpdateDeformedBounds();
        }
        void PoseDarkroomMaterial(string id,string location,bool dragging)
        {
            var visual=darkroomVisuals.ModelRoot(id);
            // Old flat whitebox cards rotated at the hanger; the supplied assets already
            // bend into a hanging pose. Compensate only the visual, never the saved anchor.
            visual.localPosition=Vector3.zero;visual.localRotation=Quaternion.identity;
            if(!dragging && (location=="print-photo-clip" || location=="print-negative-clip"))
            {
                var holder=darkroomVisuals.ModelRoot(location);
                var grip=DarkroomAssetVisuals.Node(holder,location=="print-photo-clip"?"PaperHangAnchor":"PaperGrip");
                visual.rotation=DarkroomAssetVisuals.HangingRotation(holder);
                visual.position=grip!=null?grip.position:printObjects.Placement(location).position;
            }
            else if(!dragging && location=="print-frame")
            {
                var frame=darkroomVisuals.ModelRoot("print-frame");visual.rotation=frame.rotation;
                visual.position=DarkroomAssetVisuals.Node(frame,id=="print-paper"?"PaperAnchor":"NegativeAnchor").position;
            }
            if(printLanding==id)
            {
                var end=visual.position;var rotation=visual.rotation;
                float t=Mathf.Clamp01((Time.time-printLandingTime)/PrintLandingDuration);
                float u=Mathf.SmoothStep(0,1,Mathf.Min(1,t/.65f));
                var above=new Vector3(end.x,Mathf.Max(printLandingVisualStart.y,end.y)+.025f,end.z);
                visual.position=t<.65f?Vector3.Lerp(printLandingVisualStart,above,u):Vector3.Lerp(above,end,Mathf.SmoothStep(0,1,(t-.65f)/.35f));
                visual.rotation=Quaternion.Slerp(printLandingVisualRotation,rotation,u);
            }
            // Original whitebox roots are non-uniformly scaled. Counter-scale the visual
            // after orientation changes so the paper remains metric, not stretched.
            var s=visual.lossyScale;float target=darkroomVisuals.materialDisplayScale;
            visual.localScale=Vector3.Scale(visual.localScale,new Vector3(target/Mathf.Max(.0001f,s.x),target/Mathf.Max(.0001f,s.y),target/Mathf.Max(.0001f,s.z)));
        }
    }
}
