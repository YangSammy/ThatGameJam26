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
        public void NotifyDarkroomExitBlocked(){DarkroomNotice("Progress not saved. Stay here; press Esc to retry.");}
        string DrainTarget=>sceneProcessing?"film-drain-basin":"water";
        bool CanPourChemical(string id)=>Photo.Phase==PhotoChapter.FilmPhase.Tank && !Printing && id=="chemical-"+Photo.Chemistry && (!ManualTank || (tankSealed && !needsDrain));
        // Preview, hover feedback and release share the same reachable receiving region.
        bool CanDropDarkroom(Ray ray,string id)
        {
            if(id=="print-frame" && printObjectsState!=null && printObjectsState.frameClosed)return false;
            GameObject target;if(!Nodes.TryGetValue(id,out target) || !target.activeInHierarchy)return false;
            float tolerance=printObjects!=null && printObjects.definition!=null?printObjects.definition.dropTolerance:.14f;
            return DragReceiverProbe.Accepts(ray,target.transform,objectDrag.Item,tolerance,2.5f,col=>
            {
                var hotspot=col.GetComponent<WorldHotspot>();
                if(id=="chamber" && col.GetComponent<PhotographyInteractionHit>()!=null)return true;
                if(id==DrainTarget && Nodes.ContainsKey("water") && DragReceiverProbe.BelongsTo(col.transform,Nodes["water"].transform))return true;
                return hotspot!=null && hotspot.Owner==this && id=="print-frame" &&
                    (hotspot.Id=="print-paper" || hotspot.Id=="print-negative" || hotspot.Id=="print-frame-lid");
            });
        }
        bool CanCommitDarkroom(Ray ray,string id)=>!objectDrag.Blocked && CanDropDarkroom(ray,id);
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
            foreach(var renderer in tool.GetComponentsInChildren<Renderer>())bounds.Encapsulate(renderer.bounds);
            float z=receiver.transform.position.z;
            // Pour beside the rim instead of placing the tilted tank through the fixed tap.
            if(targetId==DrainTarget && collider!=null)z=collider.bounds.min.z-bounds.extents.z-.035f;
            return new Vector3(receiver.transform.position.x,top+bounds.extents.y+.035f,z);
        }
        Vector3 MoveDarkroomDrag(Ray ray)
        {
            var receiver=DragReceiver(ray);
            if(receiver==null)
            {
                if(CarryingExposedFilm)
                {
                    // The same drag can travel between rooms. No area clamp or fixed
                    // world plane: a safe near-view offset follows walking and looking.
                    var carry=View.transform.TransformPoint(new Vector3(.12f,-.16f,.6f));
                    return objectDrag.MoveTo(Vector3.MoveTowards(objectDrag.Item.position,carry,Time.deltaTime*3));
                }
                return objectDrag.Move(ray);
            }
            string target=CurrentDropTarget();
            return objectDrag.MoveOverSurface(ToolDropPreview(target,objectDrag.Item.gameObject),receiver);
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
