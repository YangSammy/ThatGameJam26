using System;
using UnityEngine;
namespace MemoryWhitebox
{
    public sealed partial class PhotoWorld3D
    {
        public bool CarryingExposedFilm=>DirectDarkroom && dragged=="exposed-film";
        string darkroomNotice="";
        float darkroomNoticeUntil;
        bool resumedDarkroom;
        readonly ObjectDragSession objectDrag = new ObjectDragSession();
        public void BindOperationAreas()
        {
            foreach(var pair in Nodes)if(pair.Key.StartsWith("film-") && rolls.Contains(pair.Value))OperationArea.Register("house.study.work-area",pair.Value.transform,"house.photo."+pair.Key);
            OperationArea.Register("house.darkroom.work-area",looseFilm.transform,"house.photo.exposed-film");
        }
        public void DarkroomNotice(string message){darkroomNotice=message;darkroomNoticeUntil=Time.unscaledTime+3;}
        public void NotifyDarkroomExitBlocked(){DarkroomNotice("Progress not saved. Stay here; open settings and retry Save.");}
        Transform returningVisual;
        Vector3 returnStart,returnLocalPosition;
        Quaternion returnRotation,returnLocalRotation;
        float returnStarted;
        void SettleToolReturn()
        {
            if(returningVisual){returningVisual.localPosition=returnLocalPosition;returningVisual.localRotation=returnLocalRotation;}
            returningVisual=null;
        }
        void EndToolDrag(string id)
        {
            SettleToolReturn();
            Transform visual=null;
            if(id=="tank" || (id!=null && id.StartsWith("chemical-")))
                visual=darkroomVisuals!=null?darkroomVisuals.ModelRoot(id):null;
            else if(id=="exposed-film")
            {
                if(extractedVisuals!=null)foreach(var candidate in extractedVisuals)if(candidate && candidate.activeSelf){visual=candidate.transform;break;}
            }
            else if(id!=null && id.StartsWith("film-") && Nodes.ContainsKey(id) && Nodes[id].transform.childCount>0)
                visual=Nodes[id].transform.GetChild(0);
            var position=visual?visual.position:Vector3.zero;var rotation=visual?visual.rotation:Quaternion.identity;
            objectDrag.End(true);
            if(!visual || !visual.gameObject.activeInHierarchy)return;
            returningVisual=visual;returnStart=position;returnRotation=rotation;
            returnLocalPosition=visual.localPosition;returnLocalRotation=visual.localRotation;returnStarted=Time.time;
        }
        void RestoreToolReturnPose()
        {
            if(returningVisual){returningVisual.localPosition=returnLocalPosition;returningVisual.localRotation=returnLocalRotation;}
        }
        void TickToolReturn()
        {
            if(!returningVisual)return;
            if(!returningVisual.gameObject.activeInHierarchy){SettleToolReturn();return;}
            float t=Mathf.Clamp01((Time.time-returnStarted)/.4f);
            var end=returningVisual.position;var rotation=returningVisual.rotation;
            float u=Mathf.SmoothStep(0,1,t);
            returningVisual.position=Vector3.Lerp(returnStart,end,u)+Vector3.up*(Mathf.Sin(t*Mathf.PI)*.06f);
            returningVisual.rotation=Quaternion.Slerp(returnRotation,rotation,u);
            if(t>=1)SettleToolReturn();
        }
        string DrainTarget=>sceneProcessing?"film-drain-basin":"water";
        bool CanPourChemical(string id)=>Photo.Phase==PhotoChapter.FilmPhase.Tank && !Printing && id=="chemical-"+Photo.Chemistry && (!ManualTank || (tankSealed && !needsDrain));
        // Preview, hover feedback and release share the same reachable receiving region.
        bool CanDropDarkroom(Ray ray,string id)=>DarkroomReceiverResult(ray,id)==DragReceiverProbe.Result.Ready;
        DragReceiverProbe.Result DarkroomReceiverResult(Ray ray,string id)
        {
            // Authored worktop edits and fitted model hits must reach physics before querying.
            Physics.SyncTransforms();
            if(id=="print-frame" && printObjectsState!=null && printObjectsState.frameClosed)return DragReceiverProbe.Result.Unavailable;
            GameObject target;if(!Nodes.TryGetValue(id,out target) || !target.activeInHierarchy)return DragReceiverProbe.Result.Unavailable;
            float tolerance=printObjects!=null && printObjects.definition!=null?printObjects.definition.dropTolerance:.14f;
            return DragReceiverProbe.Evaluate(ray,target.transform,objectDrag.Item,tolerance,2.5f,col=>
            {
                var hotspot=col.GetComponent<WorldHotspot>();
                if(id=="chamber" && col.GetComponent<PhotographyInteractionHit>()!=null)return true;
                if(id==DrainTarget && Nodes.ContainsKey("water") && DragReceiverProbe.BelongsTo(col.transform,Nodes["water"].transform))return true;
                return hotspot!=null && hotspot.Owner==this && id=="print-frame" &&
                    (hotspot.Id=="print-paper" || hotspot.Id=="print-negative" || hotspot.Id=="print-frame-lid");
            });
        }
        bool CanCommitDarkroom(Ray ray,string id)=>!objectDrag.Blocked && CanDropDarkroom(ray,id);
        string DropInstruction(Ray ray,string id)
        {
            if(id=="print-frame" && printObjectsState!=null && printObjectsState.frameClosed)return "Open the contact frame first.";
            if(objectDrag.Blocked)return "Path blocked. Move around the obstacle.";
            switch(DarkroomReceiverResult(ray,id))
            {
                case DragReceiverProbe.Result.Ready:return "Release LMB here.";
                case DragReceiverProbe.Result.TooFar:return "Move closer to "+DarkroomName(id)+".";
                case DragReceiverProbe.Result.Occluded:return "Target blocked. Find a clear angle.";
                case DragReceiverProbe.Result.Unavailable:return "Target unavailable. Follow the highlighted step.";
                default:return "Move over "+DarkroomName(id)+".";
            }
        }
        Transform DragReceiver(Ray ray)
        {
            string id=CurrentDropTarget();
            return id!=null && CanDropDarkroom(ray,id)?Nodes[id].transform:null;
        }

        Vector3 ToolDropPreview(string targetId,GameObject tool)
        {
            var receiver=Nodes[targetId];var collider=receiver.GetComponent<Collider>();
            float top=collider!=null?collider.bounds.max.y:receiver.transform.position.y;
            Bounds bounds=new Bounds(tool.transform.position,Vector3.zero);
            foreach(var renderer in tool.GetComponentsInChildren<Renderer>())
                if(renderer.enabled && !renderer.forceRenderingOff && !(renderer is LineRenderer))bounds.Encapsulate(renderer.bounds);
            float z=receiver.transform.position.z;
            // Pour beside the rim instead of placing the tilted tank through the fixed tap.
            if(targetId==DrainTarget && collider!=null)z=collider.bounds.min.z-bounds.extents.z-.035f;
            return new Vector3(receiver.transform.position.x,top+(tool.transform.position.y-bounds.min.y)+.035f,z);
        }
        float extractionElapsed;
        int extractionFrame=-1;
        Vector3 extractionStart;
        Vector3 MoveDarkroomDrag(Ray ray)
        {
            if(CarryingExposedFilm && cameraVisual!=null && extractionElapsed<.3f)
            {
                if(extractionFrame!=Time.frameCount){extractionFrame=Time.frameCount;extractionElapsed+=Time.deltaTime;}
                var clear=cameraVisual.insert.TransformPoint(new Vector3(0,0,-.075f));
                return objectDrag.MoveTo(Vector3.Lerp(extractionStart,clear,Mathf.SmoothStep(0,1,Mathf.Clamp01(extractionElapsed/.3f))));
            }
            var receiver=DragReceiver(ray);
            if(receiver==null)
            {
                if(CarryingExposedFilm)
                {
                    // The same drag can travel between rooms. No area clamp or fixed
                    // world plane: a safe near-view offset follows walking and looking.
                    var carry=View.transform.TransformPoint(new Vector3(.12f,-.16f,.6f));
                    carry=Host.FilmCarryTarget(View.transform.position,carry);
                    return objectDrag.MoveTo(Vector3.MoveTowards(objectDrag.Item.position,carry,Time.deltaTime*3));
                }
                return objectDrag.Move(ray);
            }
            string target=CurrentDropTarget();
            return objectDrag.MoveTo(ToolDropPreview(target,objectDrag.Item.gameObject),receiver);
        }
        string CurrentDropTarget()
        {
            if(dragged!=null && dragged.StartsWith("film-"))return "chamber";
            if(dragged=="tank" && needsDrain)return DrainTarget;
            if(dragged!=null && dragged.StartsWith("chemical-"))return CanPourChemical(dragged)?"tank":null;
            if(dragged=="exposed-film")return "bag";
            if(printDrag=="print-negative")return printStage==ParkPrintStage.NegativeDrying?"print-negative-clip":"print-frame";
            if(printDrag=="print-paper")return printStage==ParkPrintStage.ContactReady?"print-frame":printStage==ParkPrintStage.PaperDeveloper?"print-developer":printStage==ParkPrintStage.PaperStop?"print-stop":printStage==ParkPrintStage.PaperFixer?"print-fixer":printStage==ParkPrintStage.PaperWash?"print-wash":"print-photo-clip";
            return null;
        }
    }
}
