using System;
using UnityEngine;
namespace MemoryWhitebox
{
    public sealed partial class PhotoWorld3D
    {
        DarkroomPrintSceneBinding printObjects;
        DarkroomPrintState printObjectsState;
        string printDrag;
        Plane printDragPlane;
        Vector3 printDragOffset;
        Texture2D contactTexture;
        MaterialPropertyBlock printProperties=new MaterialPropertyBlock();
        bool PhysicalPrinting=>ParkMode && darkroomWorkflow>=3;
        bool PrintInput=>Host!=null && Host.InputContext.Allows(InputContextId.Station) && !parkGallery;
        void BindPrintObjects()
        {
            if(!SharedRoom)return;
            foreach(var root in Host.gameObject.scene.GetRootGameObjects())
            {printObjects=root.GetComponentInChildren<DarkroomPrintSceneBinding>(true);if(printObjects!=null)break;}
            if(printObjects==null)return;
            if(printObjects.definition==null)throw new InvalidOperationException("Darkroom print configuration is missing.");
            foreach(var prop in printObjects.props)
            {
                if(prop.target==null)throw new InvalidOperationException("Darkroom prop missing: "+prop.id);
                Nodes[prop.id]=prop.target.gameObject;
                foreach(var col in prop.target.GetComponentsInChildren<Collider>(true))
                {
                    var hotspot=col.GetComponent<WorldHotspot>();if(hotspot==null)hotspot=col.gameObject.AddComponent<WorldHotspot>();
                    hotspot.Owner=this;hotspot.Id=prop.id;
                }
            }
            foreach(var id in new[]{"print-negative","print-paper","print-negative-home","print-paper-home","print-negative-clip","print-photo-clip","print-frame","print-frame-lid","print-exposure","print-developer","print-stop","print-fixer","print-wash","print-tap"})
                if(printObjects.Get(id)==null)throw new InvalidOperationException("Darkroom binding missing: "+id);
        }
        float PrintDuration(ParkPrintStage stage)=>printObjects!=null && printObjects.definition!=null?Mathf.Clamp(printObjects.definition.Duration(stage),.1f,3600):3;
        string PhysicalPrintHint
        {
            get
            {
                if(printObjects==null)return "Print equipment unavailable.";
                if(printProcessing)return printHeld?"Hold to expose.":"Processing. Please wait.";
                switch(printStage)
                {
                    case ParkPrintStage.NegativeDrying:return "Drag the negative to its drying clip.";
                    case ParkPrintStage.ContactReady:
                    case ParkPrintStage.Exposing:
                        if(printObjectsState.paper.locationId!="print-frame")return "Place paper in the contact frame.";
                        if(printObjectsState.negative.locationId!="print-frame")return "Lay the dry negative over the paper.";
                        return !printObjectsState.frameClosed?"Close the contact frame.":"Hold the exposure switch.";
                    case ParkPrintStage.PaperDeveloper:return printObjectsState.frameClosed?"Open the frame and take the paper.":"Move paper into the developer tray.";
                    case ParkPrintStage.PaperStop:return "Move paper into the stop bath.";
                    case ParkPrintStage.PaperFixer:return "Move paper into the fixer tray.";
                    case ParkPrintStage.PaperWash:return printObjectsState.paper.locationId=="print-wash"?"Turn on the wash tap.":"Move paper into the wash tray.";
                    case ParkPrintStage.PaperDrying:return "Hang the print on its drying clip.";
                    default:return "Prints ready.";
                }
            }
        }
        bool PressPhysicalPrint(string id,Ray ray)
        {
            if(!PrintInput || printObjects==null)return true;
            if(printProcessing)return true;
            if(id=="print-frame-lid")
            {
                if(printStage>=ParkPrintStage.ContactReady && printStage<=ParkPrintStage.PaperDeveloper)
                {
                    if(printObjectsState.frameClosed)printObjectsState.frameClosed=false;
                    else if(printObjectsState.negative.locationId=="print-frame" && printObjectsState.paper.locationId=="print-frame")printObjectsState.frameClosed=true;
                    SaveProgress();SyncPhysicalPrint();
                }
                return true;
            }
            if(id=="print-exposure" && printStage==ParkPrintStage.ContactReady && printObjectsState.frameClosed && printObjectsState.negative.locationId=="print-frame" && printObjectsState.paper.locationId=="print-frame")
            {printStage=ParkPrintStage.Exposing;if(printRemaining<=0)printRemaining=PrintDuration(printStage);printProcessing=printHeld=true;SyncPhysicalPrint();return true;}
            if(id=="print-tap" && printStage==ParkPrintStage.PaperWash && printObjectsState.paper.locationId=="print-wash")
            {StartPhysicalProcess();return true;}
            bool negative=id=="print-negative" && (printStage==ParkPrintStage.NegativeDrying || printStage==ParkPrintStage.ContactReady);
            bool paper=id=="print-paper" && printStage>=ParkPrintStage.ContactReady && printStage<=ParkPrintStage.PaperDrying;
            if(!negative && !paper)return true;
            string location=negative?printObjectsState.negative.locationId:printObjectsState.paper.locationId;
            if(location=="print-frame" && printObjectsState.frameClosed)return true;
            printDrag=id;var item=Nodes[id].transform;
            printDragPlane=new Plane(View.transform.forward,item.position);
            float distance;printDragOffset=printDragPlane.Raycast(ray,out distance)?item.position-ray.GetPoint(distance):Vector3.zero;
            SetColliders(item.gameObject,false);return true;
        }
        bool MovePhysicalPrint(Ray ray)
        {
            if(printDrag==null)return false;
            float distance;if(printDragPlane.Raycast(ray,out distance))Nodes[printDrag].transform.position=ray.GetPoint(distance)+printDragOffset;
            return true;
        }
        void StartPhysicalProcess()
        {printRemaining=PrintDuration(printStage);printProcessing=true;SaveProgress();SyncPhysicalPrint();}
        bool ReleasePhysicalPrint(Ray ray)
        {
            if(printDrag==null)return false;
            if(!PrintInput){CancelPhysicalPrint();return true;}
            string item=printDrag;string target=Pick(ray);
            if(target=="print-frame-lid")target="print-frame";
            string expected=item=="print-negative"?(printStage==ParkPrintStage.NegativeDrying?"print-negative-clip":"print-frame"):
                printStage==ParkPrintStage.ContactReady?"print-frame":printStage==ParkPrintStage.PaperDeveloper?"print-developer":
                printStage==ParkPrintStage.PaperStop?"print-stop":printStage==ParkPrintStage.PaperFixer?"print-fixer":
                printStage==ParkPrintStage.PaperWash?"print-wash":"print-photo-clip";
            var zone=printObjects.Get(expected);bool near=false;
            if(zone!=null)
            {
                float tolerance=printObjects.definition!=null?printObjects.definition.dropTolerance:.14f;
                var col=zone.target.GetComponent<Collider>();
                if(col!=null){float distance;var plane=new Plane(View.transform.forward,zone.target.position);if(plane.Raycast(ray,out distance))near=Vector3.Distance(col.ClosestPoint(ray.GetPoint(distance)),ray.GetPoint(distance))<=tolerance;}
            }
            bool valid=(target==expected || near) && !(expected=="print-frame" && printObjectsState.frameClosed);
            SetColliders(Nodes[item],true);printDrag=null;
            if(valid)
            {
                var record=item=="print-negative"?printObjectsState.negative:printObjectsState.paper;record.locationId=expected;
                if(printStage==ParkPrintStage.NegativeDrying || (printStage>=ParkPrintStage.PaperDeveloper && printStage!=ParkPrintStage.PaperWash))StartPhysicalProcess();
                WBMain.Tone(260,.05f);
            }
            else {WBMain.Tone(110,.05f);}
            SyncPhysicalPrint();SaveProgress();return true;
        }
        void CancelPhysicalPrint()
        {
            if(printDrag!=null){SetColliders(Nodes[printDrag],true);printDrag=null;}
            SyncPhysicalPrint();
        }
        void PlacePrintItem(string id,ItemSaveRecord state)
        {
            if(printDrag==id || state==null)return;
            var anchor=printObjects.Placement(state.locationId);if(anchor==null)return;
            Nodes[id].transform.SetPositionAndRotation(anchor.position,anchor.rotation);
            if(id=="print-negative" && state.locationId=="print-frame")Nodes[id].transform.position+=anchor.up*.008f;
        }
        void SyncPhysicalPrint()
        {
            if(printObjects==null)return;
            bool visible=PhysicalPrinting && printStage!=ParkPrintStage.None;
            // Equipment stays authored and visible; only consumed materials are staged.
            Nodes["print-negative"].SetActive(visible);
            Nodes["print-paper"].SetActive(visible && printStage!=ParkPrintStage.NegativeDrying);
            if(printObjects.exposureLight!=null)printObjects.exposureLight.SetActive(visible && printHeld);
            if(printObjects.waterStream!=null)printObjects.waterStream.SetActive(visible && printStage==ParkPrintStage.PaperWash && printProcessing);
            if(!visible || printObjectsState==null)return;
            PlacePrintItem("print-negative",printObjectsState.negative);PlacePrintItem("print-paper",printObjectsState.paper);
            SetColliders(Nodes["print-negative"],printDrag!="print-negative" && printStage<=ParkPrintStage.ContactReady && !(printObjectsState.frameClosed && printObjectsState.negative.locationId=="print-frame"));
            SetColliders(Nodes["print-paper"],printDrag!="print-paper" && !(printObjectsState.frameClosed && printObjectsState.paper.locationId=="print-frame"));
            if(printObjects.frameLid!=null)printObjects.frameLid.localRotation=Quaternion.Euler(printObjectsState.frameClosed?0:-100,0,0);
            float reveal=printStage>ParkPrintStage.PaperDeveloper?1:printStage==ParkPrintStage.PaperDeveloper && printProcessing?1-printRemaining/PrintDuration(printStage):0;
            if(contactTexture!=null && printObjects.paperSurface!=null)
            {
                printProperties.Clear();printProperties.SetTexture("_MainTex",contactTexture);printProperties.SetFloat("_Reveal",Mathf.Clamp01(reveal));printObjects.paperSurface.SetPropertyBlock(printProperties);
            }
        }
        void LoadContactTexture()
        {
            if(contactTexture!=null || !PhysicalPrinting || printStage<ParkPrintStage.PaperDeveloper)return;
            // Private process preview: no gallery or original frame files are exposed before completion.
            try{contactTexture=ParkFilmStore.CreateContactSheetPreview(parkFilm.id);}
            catch(Exception e){saveNotice="Print preview unavailable. Negatives kept.";Debug.LogWarning(e.Message);}
            SyncPhysicalPrint();
        }
    }
}
