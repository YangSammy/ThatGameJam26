using System;
using UnityEngine;
namespace MemoryWhitebox
{
    public sealed partial class PhotoWorld3D
    {
        DarkroomPrintSceneBinding printObjects;
        DarkroomPrintState printObjectsState;
        BoxCollider framePlateHit;
        string printDrag;
        Texture2D contactTexture;
        MaterialPropertyBlock printProperties=new MaterialPropertyBlock();
        Vector3 negativeHomePosition,paperHomePosition,negativeHomeScale,paperHomeScale;
        Quaternion negativeHomeRotation,paperHomeRotation,frameClosedRotation;
        bool printPosesCaptured;
        bool PhysicalPrinting=>ParkMode && darkroomWorkflow>=3;
        bool PrintInput=>Host!=null && (Host.InputContext.Allows(InputContextId.Station) || Host.InputContext.Allows(InputContextId.Darkroom)) && !parkGallery;
        public void AttachDarkroom(DarkroomPrintSceneBinding station)
        {
            if(printObjects==station && sceneProcessing)return;
            if(station==null || sceneProcessing)throw new InvalidOperationException("Invalid summer darkroom binding.");
            printObjects=station;
            BindSceneProcessing();CaptureProcessingPose();BindPrintObjects();BindOperationAreas();
            if(PhysicalPrinting && printStage>=ParkPrintStage.PaperDeveloper)LoadContactTexture();
            SyncProps();
        }

        public void DetachDarkroom()
        {
            StopPhotographySound();
            if(!sceneProcessing)return;
            SetDirectDarkroom(false);
            ClearDarkroomHighlight();guideRenderers.Clear();RestoreSceneProcessing();RestorePrintPoses();
            foreach(var prop in printObjects.props)
            {Nodes.Remove(prop.id);homes.Remove(prop.id);homeRotations.Remove(prop.id);}
            chemistry.Clear();bag=null;bagGrip=tankBody=tankLid=clockHand=null;
            sceneProcessing=printPosesCaptured=false;framePlateHit=null;darkroomVisuals=null;printObjects=null;
            Root.SetActive(false);
        }

        Vector3 ProcessingAnchor(string id,Vector3 fallback)
        {
            var anchor=printObjects!=null?printObjects.Placement(id):null;
            return anchor!=null?anchor.position-Origin:fallback;
        }
        void BindPrintObjects()
        {
            darkroomVisuals=printObjects.GetComponent<DarkroomAssetVisuals>();
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
            if(HasDarkroomAssets)
            {
                // The old frame has rim hits only. Follow the supplied moving plate
                // while keeping its original hotspot and drop receiver identities.
                var lid=Nodes["print-frame-lid"];
                framePlateHit=lid.GetComponent<BoxCollider>();
                if(framePlateHit==null)framePlateHit=lid.AddComponent<BoxCollider>();
                var hotspot=lid.GetComponent<WorldHotspot>();
                if(hotspot==null)hotspot=lid.AddComponent<WorldHotspot>();
                hotspot.Owner=this;hotspot.Id="print-frame-lid";
                if(lid.GetComponent<PhotographyInteractionHit>()==null)lid.AddComponent<PhotographyInteractionHit>();
            }
            var negative=Nodes["print-negative"].transform;var paper=Nodes["print-paper"].transform;
            negativeHomePosition=negative.localPosition;negativeHomeRotation=negative.localRotation;negativeHomeScale=negative.localScale;
            paperHomePosition=paper.localPosition;paperHomeRotation=paper.localRotation;paperHomeScale=paper.localScale;
            if(printObjects.frameLid!=null)frameClosedRotation=printObjects.frameLid.localRotation;
            printPosesCaptured=true;
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
        string ResolvePrintHit(string id)
        {
            if(PhysicalPrinting && printObjectsState!=null && printDrag==null && !printProcessing)
            {
                bool readyToClose=(printStage==ParkPrintStage.ContactReady || printStage==ParkPrintStage.Exposing)
                    && printObjectsState.paper.locationId=="print-frame" && printObjectsState.negative.locationId=="print-frame";
                bool readyToOpen=printStage==ParkPrintStage.PaperDeveloper && printObjectsState.frameClosed;
                if((readyToClose || readyToOpen) && (id=="print-frame" || id=="print-frame-lid" ||
                    (id=="print-paper" && printObjectsState.paper.locationId=="print-frame") ||
                    (id=="print-negative" && printObjectsState.negative.locationId=="print-frame")))return "print-frame-lid";
            }
            return id;
        }

        bool PressPhysicalPrint(string id,Ray ray)
        {
            if(!PrintInput || printObjects==null)return true;
            if(printProcessing){DarkroomNotice("Processing — please wait.");return true;}
            if(printStage==ParkPrintStage.NegativeDrying && !printObjectsState.negativeRemoved)
            {
                if(id=="tank" || id=="tank-lid")
                {printObjectsState.negativeRemoved=true;PhotographySound(129,"tank-lid");SaveProgress();SyncProps();DarkroomNotice("Negative removed. Hang it to dry.");}
                else DarkroomNotice("Open the tank lid to take out the negative.");
                return true;
            }
            if(id=="print-frame-lid")
            {
                if(printStage>=ParkPrintStage.ContactReady && printStage<=ParkPrintStage.PaperDeveloper)
                {
                    bool before=printObjectsState.frameClosed;
                    if(printObjectsState.frameClosed)printObjectsState.frameClosed=false;
                    else if(printObjectsState.negative.locationId=="print-frame" && printObjectsState.paper.locationId=="print-frame")printObjectsState.frameClosed=true;
                    else DarkroomNotice("Place paper and negative in the frame first.");
                    if(before!=printObjectsState.frameClosed)PhotographySound(231,"print-frame-lid");
                    SaveProgress();SyncPhysicalPrint();
                }
                return true;
            }
            if(id=="print-exposure" && printStage==ParkPrintStage.ContactReady && printObjectsState.frameClosed && printObjectsState.negative.locationId=="print-frame" && printObjectsState.paper.locationId=="print-frame")
            {printStage=ParkPrintStage.Exposing;if(printRemaining<=0)printRemaining=PrintDuration(printStage);printProcessing=printHeld=true;PhotographySound(231,"print-exposure");SyncPhysicalPrint();return true;}
            if(id=="print-tap" && printStage==ParkPrintStage.PaperWash && printObjectsState.paper.locationId=="print-wash")
            {PhotographySound(31,"print-tap");StartPhysicalProcess();return true;}
            bool negative=id=="print-negative" && (printStage==ParkPrintStage.NegativeDrying || printStage==ParkPrintStage.ContactReady);
            bool paper=id=="print-paper" && printStage>=ParkPrintStage.ContactReady && printStage<=ParkPrintStage.PaperDrying;
            if(!negative && !paper){if(!string.IsNullOrEmpty(id))DarkroomNotice("Follow the highlighted item.");return true;}
            string location=negative?printObjectsState.negative.locationId:printObjectsState.paper.locationId;
            if(location=="print-frame" && printObjectsState.frameClosed){DarkroomNotice("Open the contact frame first.");return true;}
            printDrag=id;var item=Nodes[id].transform;
            Transform container=location=="print-frame"?Nodes["print-frame"].transform:
                negative && printStage==ParkPrintStage.NegativeDrying?Nodes["tank"].transform:null;
            objectDrag.Begin(item,ray,Vector3.up,.025f,1.8f,true,container);
            SetColliders(item.gameObject,false);return true;
        }
        bool MovePhysicalPrint(Ray ray)
        {
            if(printDrag==null)return false;
            MoveDarkroomDrag(ray);
            return true;
        }
        void StartPhysicalProcess()
        {printRemaining=PrintDuration(printStage);printProcessing=true;SaveProgress();SyncPhysicalPrint();}
        bool ReleasePhysicalPrint(Ray ray)
        {
            if(printDrag==null)return false;
            if(!PrintInput){CancelPhysicalPrint();return true;}
            string item=printDrag;
            string expected=item=="print-negative"?(printStage==ParkPrintStage.NegativeDrying?"print-negative-clip":"print-frame"):
                printStage==ParkPrintStage.ContactReady?"print-frame":printStage==ParkPrintStage.PaperDeveloper?"print-developer":
                printStage==ParkPrintStage.PaperStop?"print-stop":printStage==ParkPrintStage.PaperFixer?"print-fixer":
                printStage==ParkPrintStage.PaperWash?"print-wash":"print-photo-clip";
            bool valid=CanCommitDarkroom(ray,expected) && !(expected=="print-frame" && printObjectsState.frameClosed);
            string rejection=valid?null:DropInstruction(ray,expected);
            // A wrong tray is not free tabletop space. Its decorative liquid may
            // have no physics collider, so reject by the existing interaction hit.
            string releaseHit=Pick(ray);
            bool overEquipment=releaseHit!=null && releaseHit!=item && Nodes.ContainsKey(releaseHit);
            bool placed=!valid && !overEquipment && objectDrag.TryPlaceInArea();
            objectDrag.End(true);
            SetColliders(Nodes[item],true);printDrag=null;
            if(valid)
            {
                OperationArea.ClearPlacement(Nodes[item].transform);
                var record=item=="print-negative"?printObjectsState.negative:printObjectsState.paper;record.locationId=expected;
                if(printStage==ParkPrintStage.NegativeDrying || (printStage>=ParkPrintStage.PaperDeveloper && printStage!=ParkPrintStage.PaperWash))StartPhysicalProcess();
                if(!PhotographySound(item=="print-negative"?131:331,expected))WBMain.Tone(260,.05f);
            }
            else if(placed)
            {
                var record=item=="print-negative"?printObjectsState.negative:printObjectsState.paper;
                record.locationId="area/"+OperationArea.For(Nodes[item].transform).areaId;
                PhotographySound(item=="print-negative"?131:331,item);DarkroomNotice("Placed on work area.");
            }
            else {WBMain.Tone(110,.05f);DarkroomNotice(rejection);}
            SyncPhysicalPrint();SaveProgress();Host.SaveStableProgress();return true;
        }
        void CancelPhysicalPrint()
        {
            if(printDrag!=null){objectDrag.End(true);SetColliders(Nodes[printDrag],true);printDrag=null;}
            SyncPhysicalPrint();
        }
        void PlacePrintItem(string id,ItemSaveRecord state)
        {
            if(printDrag==id || state==null)return;
            if(state.locationId!=null && state.locationId.StartsWith("area/") && OperationArea.RestorePose(Nodes[id].transform))return;
            // At home, the material's own Inspector pose is authoritative, not an old duplicate anchor.
            if(state.locationId=="print-negative-home" || state.locationId=="print-paper-home" || (state.locationId!=null && state.locationId.StartsWith("area/")))
            {
                var item=Nodes[id].transform;bool negative=id=="print-negative";
                item.localPosition=negative?negativeHomePosition:paperHomePosition;
                item.localRotation=negative?negativeHomeRotation:paperHomeRotation;
                return;
            }
            var anchor=printObjects.Placement(state.locationId);if(anchor==null)return;
            Nodes[id].transform.SetPositionAndRotation(anchor.position,anchor.rotation);
            if(id=="print-negative" && state.locationId=="print-frame")Nodes[id].transform.position=anchor.TransformPoint(printObjects.negativeOnFrameOffset);
        }
        void SyncPhysicalPrint()
        {
            if(printObjects==null)return;
            bool visible=PhysicalPrinting && printStage!=ParkPrintStage.None;
            // Equipment stays authored and visible; only consumed materials are staged.
            Nodes["print-negative"].SetActive(visible && (printObjectsState!=null && (printObjectsState.negativeRemoved || printStage>ParkPrintStage.NegativeDrying)));
            Nodes["print-paper"].SetActive(visible && printStage!=ParkPrintStage.NegativeDrying);
            if(printObjects.exposureLight!=null)printObjects.exposureLight.SetActive(visible && printHeld);
            if(printObjects.waterStream!=null)printObjects.waterStream.SetActive(visible && printStage==ParkPrintStage.PaperWash && printProcessing);
            if(!visible || printObjectsState==null){SyncDarkroomAssets();return;}
            PlacePrintItem("print-negative",printObjectsState.negative);PlacePrintItem("print-paper",printObjectsState.paper);
            SetColliders(Nodes["print-negative"],printDrag!="print-negative" && printStage<=ParkPrintStage.ContactReady && !(printObjectsState.frameClosed && printObjectsState.negative.locationId=="print-frame"));
            SetColliders(Nodes["print-paper"],printDrag!="print-paper" && !(printObjectsState.frameClosed && printObjectsState.paper.locationId=="print-frame"));
            if(!HasDarkroomAssets && printObjects.frameLid!=null)printObjects.frameLid.localRotation=frameClosedRotation*Quaternion.Euler(printObjectsState.frameClosed?Vector3.zero:printObjects.frameOpenEuler);
            float reveal=printStage>ParkPrintStage.PaperDeveloper?1:printStage==ParkPrintStage.PaperDeveloper && printProcessing?1-printRemaining/PrintDuration(printStage):0;
            if(!HasDarkroomAssets && contactTexture!=null && printObjects.paperSurface!=null)
            {
                printProperties.Clear();printProperties.SetTexture("_MainTex",contactTexture);printProperties.SetFloat("_Reveal",Mathf.Clamp01(reveal));printObjects.paperSurface.SetPropertyBlock(printProperties);
            }
            SyncDarkroomAssets();
        }
        void LoadContactTexture()
        {
            if(printObjects==null || contactTexture!=null || !PhysicalPrinting || printStage<ParkPrintStage.PaperDeveloper)return;
            // Private process preview: no gallery or original frame files are exposed before completion.
            try{contactTexture=ParkFilmStore.CreateContactSheetPreview(parkFilm.id,HasDarkroomAssets?4f/3f:16f/9f);}
            catch(Exception e){saveNotice="Print preview unavailable. Negatives kept.";Debug.LogWarning(e.Message);}
            SyncPhysicalPrint();
        }
        void RestorePrintPoses()
        {
            if(!printPosesCaptured || printObjects==null)return;
            var negative=Nodes["print-negative"].transform;var paper=Nodes["print-paper"].transform;
            negative.localPosition=negativeHomePosition;negative.localRotation=negativeHomeRotation;negative.localScale=negativeHomeScale;
            paper.localPosition=paperHomePosition;paper.localRotation=paperHomeRotation;paper.localScale=paperHomeScale;
            if(printObjects.frameLid!=null)printObjects.frameLid.localRotation=frameClosedRotation;
        }
    }
}
