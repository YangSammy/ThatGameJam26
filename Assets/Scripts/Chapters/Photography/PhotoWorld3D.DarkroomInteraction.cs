using System;
using UnityEngine;
namespace MemoryWhitebox
{
    public sealed partial class PhotoWorld3D
    {
        string darkroomNotice="";
        float darkroomNoticeUntil;
        bool resumedDarkroom;
        Vector3 dragGrabOffset;
        public void DarkroomNotice(string message){darkroomNotice=message;darkroomNoticeUntil=Time.unscaledTime+3;}
        public void NotifyDarkroomExitBlocked(){DarkroomNotice("Progress not saved. Stay here; press Esc to retry.");}
        string DrainTarget=>sceneProcessing?"film-drain-basin":"water";
        bool CanPourChemical(string id)=>Photo.Phase==PhotoChapter.FilmPhase.Tank && !Printing && id=="chemical-"+Photo.Chemistry && (!ManualTank || (tankSealed && !needsDrain));
        // Preview, hover feedback and release share the same reachable receiving region.
        bool CanDropDarkroom(Ray ray,string id)
        {
            if(id=="print-frame" && printObjectsState!=null && printObjectsState.frameClosed)return false;
            GameObject target;if(!Nodes.TryGetValue(id,out target) || !target.activeInHierarchy)return false;
            Vector3 closest=target.transform.position;
            float best=float.MaxValue;
            float tolerance=printObjects!=null && printObjects.definition!=null?printObjects.definition.dropTolerance:.14f;
            foreach(var col in target.GetComponentsInChildren<Collider>())
            {
                if(!col.enabled || col.isTrigger)continue;
                Bounds receiver=col.bounds;receiver.Expand(tolerance*2);
                float distance;if(!receiver.IntersectRay(ray,out distance) || distance<0 || distance>2.5f)continue;
                if(distance<best){best=distance;closest=col.ClosestPoint(ray.GetPoint(distance));}
            }
            if(best==float.MaxValue)return false;
            Vector3 direction=closest-ray.origin;float length=direction.magnitude;
            if(length>2.5f)return false;
            var hits=Physics.RaycastAll(new Ray(ray.origin,direction.normalized),length+.015f,(1<<8)|(1<<25)|1,QueryTriggerInteraction.Ignore);
            Array.Sort(hits,(a,b)=>a.distance.CompareTo(b.distance));
            foreach(var hit in hits)
            {
                // Walking footprints are not visible walls and must not block tabletop drops.
                if(hit.collider.name.StartsWith("Walk blocker") && hit.collider.GetComponent<Renderer>()==null)continue;
                if(hit.transform==target.transform || hit.transform.IsChildOf(target.transform))return true;
                var hotspot=hit.collider.GetComponent<WorldHotspot>();
                if(hotspot!=null && hotspot.Owner==this && id=="print-frame" && (hotspot.Id=="print-paper" || hotspot.Id=="print-negative" || hotspot.Id=="print-frame-lid"))continue;
                if(hit.distance<length-.015f)return false;
            }
            return true;
        }
        Vector3 ToolDropPreview(string targetId,GameObject tool)
        {
            var receiver=Nodes[targetId];var collider=receiver.GetComponent<Collider>();
            float top=collider!=null?collider.bounds.max.y:receiver.transform.position.y;
            Bounds bounds=new Bounds(tool.transform.position,Vector3.zero);
            foreach(var renderer in tool.GetComponentsInChildren<Renderer>())bounds.Encapsulate(renderer.bounds);
            return new Vector3(receiver.transform.position.x,top+bounds.extents.y+.035f,receiver.transform.position.z);
        }
        string CurrentDropTarget()
        {
            if(dragged=="tank" && needsDrain)return DrainTarget;
            if(dragged!=null && dragged.StartsWith("chemical-"))return CanPourChemical(dragged)?"tank":null;
            if(dragged=="exposed-film")return "bag";
            if(printDrag=="print-negative")return printStage==ParkPrintStage.NegativeDrying?"print-negative-clip":"print-frame";
            if(printDrag=="print-paper")return printStage==ParkPrintStage.ContactReady?"print-frame":printStage==ParkPrintStage.PaperDeveloper?"print-developer":printStage==ParkPrintStage.PaperStop?"print-stop":printStage==ParkPrintStage.PaperFixer?"print-fixer":printStage==ParkPrintStage.PaperWash?"print-wash":"print-photo-clip";
            return null;
        }
    }
}
