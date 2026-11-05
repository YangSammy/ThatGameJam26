using System.Collections.Generic;
using UnityEngine;

namespace MemoryWhitebox
{
    public sealed partial class PhotoWorld3D
    {
        string guideSource,guideTarget,guideAction,guideTask;
        readonly Dictionary<Renderer, Color> guideColors=new Dictionary<Renderer, Color>();
        readonly Dictionary<Renderer, int> guideProperties=new Dictionary<Renderer, int>();
        readonly Dictionary<string, Renderer[]> guideRenderers=new Dictionary<string, Renderer[]>();
        readonly MaterialPropertyBlock guideBlock=new MaterialPropertyBlock();
        struct GuideMaterialColor { public Renderer renderer; public int index,property; public Color color; }
        readonly List<GuideMaterialColor> guideMaterialColors=new List<GuideMaterialColor>();
        public bool DarkroomGuidanceActive=>(Active || DirectDarkroom) && ParkMode && !parkFilmUnavailable && !parkGallery;

        void Guide(string source,string target,string action,string task)
        {guideSource=source;guideTarget=target;guideAction=action;guideTask=task;}
        void ResolveDarkroomGuide()
        {
            Guide(null,null,null,null);
            if(!ParkMode || parkFilmUnavailable)return;
            if(ParkPrintsReady){Guide("print-paper",null,"Click to view prints","View your finished prints.");return;}
            if(Printing && PhysicalPrinting)
            {
                if(printProcessing)
                {
                    if(printHeld)Guide("print-exposure",null,"Keep holding LMB","Expose paper — keep holding LMB.");
                    else Guide(null,null,null,"Wait — "+PrintProcessName()+" ("+Mathf.CeilToInt(printRemaining)+"s).");
                    return;
                }
                switch(printStage)
                {
                    case ParkPrintStage.NegativeDrying:
                        if(!printObjectsState.negativeRemoved)Guide("tank-lid",null,"Click to take out negative","Open the tank lid and take out the negative.");
                        else Guide("print-negative","print-negative-clip","Drag to drying clip","Hang the negative on its drying clip.");break;
                    case ParkPrintStage.ContactReady:
                    case ParkPrintStage.Exposing:
                        if(printObjectsState.paper.locationId!="print-frame")Guide("print-paper","print-frame","Drag to contact frame","Place paper in the contact frame.");
                        else if(printObjectsState.negative.locationId!="print-frame")Guide("print-negative","print-frame","Drag onto the paper","Place the dry negative over the paper.");
                        else if(!printObjectsState.frameClosed)Guide("print-frame-lid",null,"Click to close","Click the contact frame to close it.");
                        else Guide("print-exposure",null,"Hold LMB to expose",printRemaining>0?"Continue exposure — hold the switch ("+printRemaining.ToString("0.0")+"s).":"Hold the exposure switch.");
                        break;
                    case ParkPrintStage.PaperDeveloper:
                        if(printObjectsState.frameClosed)Guide("print-frame-lid",null,"Click to open","Click the contact frame to open it.");
                        else Guide("print-paper","print-developer","Drag to developer tray","Move the exposed paper into developer.");
                        break;
                    case ParkPrintStage.PaperStop:Guide("print-paper","print-stop","Drag to stop bath","Move the paper into the stop bath.");break;
                    case ParkPrintStage.PaperFixer:Guide("print-paper","print-fixer","Drag to fixer tray","Move the paper into fixer.");break;
                    case ParkPrintStage.PaperWash:
                        if(printObjectsState.paper.locationId=="print-wash")Guide("print-tap",null,"Click to wash","Turn on the paper wash tap.");
                        else Guide("print-paper","print-wash","Drag to wash tray","Move the paper into the wash tray.");
                        break;
                    case ParkPrintStage.PaperDrying:Guide("print-paper","print-photo-clip","Drag to photo clip","Hang the washed print on its clip.");break;
                }
                return;
            }
            switch(Photo.Phase)
            {
                case PhotoChapter.FilmPhase.Exposed:Guide("rewind",null,"Hold LMB + Scroll up","Rewind film — hold knob + scroll up ("+Mathf.RoundToInt(RewindTurns*100)+"%).");break;
                case PhotoChapter.FilmPhase.Rewound:
                    if(!RewoundBackOpen)Guide("back",null,"Click to open","Open the camera back.");
                    else Guide("exposed-film","bag","Hold LMB to carry; WASD to move","Carry the film to the dark bag. Hold LMB + WASD; release over bag.");
                    break;
                case PhotoChapter.FilmPhase.DarkBag:Guide("bag",null,"Hold LMB + Scroll up","Load reel — hold bag + scroll up ("+Mathf.RoundToInt(Photo.BagTurns*100)+"%).");break;
                case PhotoChapter.FilmPhase.Tank:
                    if(ManualTank && !tankSealed)Guide("tank-lid",null,"Click to seal","Seal the developing tank — click its lid.");
                    else if(ManualTank && needsDrain)Guide("tank",DrainTarget,"Drag to basin; release to drain","Empty the tank into the drain basin.");
                    else if(Photo.Chemistry==3)Guide("water",null,"Click to rinse film","Rinse the negative — click the tank rinse tap.");
                    else Guide("chemical-"+Photo.Chemistry,"tank","Drag onto tank to pour","Pour "+(Photo.Chemistry==0?"developer":Photo.Chemistry==1?"stop bath":"fixer")+" into the tank.");
                    break;
                case PhotoChapter.FilmPhase.Processing:
                    if(!Photo.Agitated && Photo.Chemistry<3)Guide("tank",null,"Hold LMB; move side to side","Gently agitate the tank — drag side to side.");
                    else Guide(null,null,null,"Wait — processing negative ("+Mathf.CeilToInt(Photo.ProcessingTime)+"s).");
                    break;
            }
        }
        string PrintProcessName()=>printStage==ParkPrintStage.NegativeDrying?"drying negative":printStage==ParkPrintStage.PaperDeveloper?"developing paper":printStage==ParkPrintStage.PaperStop?"stopping development":printStage==ParkPrintStage.PaperFixer?"fixing paper":printStage==ParkPrintStage.PaperWash?"washing paper":"drying print";
        string DarkroomTask {get{ResolveDarkroomGuide();string target=CurrentDropTarget();return CarryingExposedFilm?"Hold LMB + WASD to carry. RMB to look. Release over the dark bag.":target!=null?"Move to "+DarkroomName(target)+"; release LMB.":guideTask;}}
        static string DarkroomName(string id)
        {
            if(id=="film-100" || id=="film-200" || id=="film-400" || id=="film-800")return "ISO "+id.Substring(5)+" film";
            switch(id)
            {
                case "rewind":return "Rewind knob";
                case "back":return "Camera back";
                case "chamber":return "Film chamber";
                case "exposed-film":return "Exposed film";
                case "bag":return "Dark bag";
                case "tank":return "Developing tank";
                case "tank-lid":return "Tank lid";
                case "water":return "Tank rinse tap";
                case "film-drain-basin":return "Drain basin";
                case "chemical-0":return "Film developer";
                case "chemical-1":return "Film stop bath";
                case "chemical-2":return "Film fixer";
                case "print-negative":return "Negative strip";
                case "print-paper":return "Contact paper";
                case "print-frame":return "Contact frame";
                case "print-frame-lid":return "Contact frame";
                case "print-exposure":return "Exposure switch";
                case "print-negative-clip":return "Negative drying clip";
                case "print-photo-clip":return "Photo drying clip";
                case "print-developer":return "Paper developer tray";
                case "print-stop":return "Paper stop bath";
                case "print-fixer":return "Paper fixer tray";
                case "print-wash":return "Paper wash tray";
                case "print-tap":return "Paper wash tap";
                default:return null;
            }
        }
        public string DarkroomTooltip(Ray ray,out Renderer[] hover)
        {
            hover=null;
            if(!DarkroomGuidanceActive)return "";
            ResolveDarkroomGuide();string id=Pick(ray),name=DarkroomName(id);
            string drop=CurrentDropTarget();
            if(drop!=null)
            {
                if(CanCommitDarkroom(ray,drop))hover=GuideSurfaces(drop);
                return DarkroomName(dragged??printDrag)+" → "+DarkroomName(drop)+"\n"+DropInstruction(ray,drop)+" / Esc Cancel";
            }
            if(objectDrag.AtEdge)return "Keep on this work area\nRelease LMB to place";
            if(objectDrag.Blocked)return "Path blocked\nMove around the obstacle / Esc Cancel";
            if(name==null)return "";
            if(id=="tank-lid" && tankSealed && (guideSource=="tank" || guideTarget=="tank")){id="tank";name=DarkroomName(id);}
            hover=GuideSurfaces(id);
            bool carrying=printDrag!=null || dragged!=null;
            if(id==guideTarget || (id=="print-frame-lid" && guideTarget=="print-frame"))
                return name+"\n"+(carrying?"Move closer; keep target clear":"Place "+DarkroomName(guideSource)+" here");
            if(id==guideSource || (id=="tank" && guideSource=="tank-lid"))return name+"\n"+guideAction;
            if(id!=null && id.StartsWith("chemical-") && guideSource!=null && guideSource.StartsWith("chemical-"))
                return name+"\nUse "+DarkroomName(guideSource)+" first";
            if(printProcessing || Photo.Phase==PhotoChapter.FilmPhase.Processing)return name+"\nProcessing — please wait";
            return name+"\n"+(ParkPrintsReady?"Processing complete":"Not needed for this step");
        }
        Renderer[] GuideSurfaces(string id)
        {
            if(string.IsNullOrEmpty(id) || !Nodes.ContainsKey(id))return null;
            Renderer[] renderers;
            if(!guideRenderers.TryGetValue(id,out renderers))
            {
                var targets=new List<Renderer>(Nodes[id].GetComponentsInChildren<Renderer>(true));
                var cameraPart=cameraVisual?cameraVisual.HoverPart(id):null;
                if(cameraPart)targets.AddRange(cameraPart.GetComponentsInChildren<Renderer>(true));
                // Logical hit roots survive model replacement, but their old
                // renderers are hidden. Highlight the delivered moving part.
                if(darkroomVisuals!=null)
                {
                    string model=null;string[] parts=null;
                    if(id=="print-tap"){model="print-wash";parts=new[]{"WashTap","WashValve"};}
                    else if(id=="water"){model="film-drain-basin";parts=new[]{"TapBody","ValveHandle"};}
                    else if(id=="tank-lid"){model="tank";parts=new[]{"MainLid"};}
                    else if(id=="print-frame-lid"){model="print-frame";parts=new[]{"PressurePlate","Latch"};}
                    if(model!=null)foreach(var part in parts)
                    {
                        var node=DarkroomAssetVisuals.Node(darkroomVisuals.ModelRoot(model),part);
                        if(node!=null)foreach(var r in node.GetComponentsInChildren<Renderer>(true))if(!targets.Contains(r))targets.Add(r);
                    }
                }
                renderers=targets.ToArray();guideRenderers[id]=renderers;
            }
            return renderers;
        }

        public void ClearDarkroomHighlight()
        {
            foreach(var saved in guideMaterialColors)
            {
                if(saved.renderer==null)continue;
                saved.renderer.GetPropertyBlock(guideBlock,saved.index);
                guideBlock.SetColor(saved.property,saved.color);
                saved.renderer.SetPropertyBlock(guideBlock,saved.index);
            }
            guideMaterialColors.Clear();
            foreach(var pair in guideColors)
            {
                if(pair.Key==null)continue;
                pair.Key.GetPropertyBlock(guideBlock);
                guideBlock.SetColor(guideProperties[pair.Key],pair.Value);
                pair.Key.SetPropertyBlock(guideBlock);
            }
            guideColors.Clear();guideProperties.Clear();
        }
        internal bool TaskMarkerPosition(Camera camera,out Vector2 screen)
        {
            screen=default;
            if(guideColors.Count==0 || !camera)return false;
            Bounds bounds=default;
            bool first=true;
            foreach(var r in guideColors.Keys)
            {
                if(!r || r.forceRenderingOff)continue;
                if(first){bounds=r.bounds;first=false;}else bounds.Encapsulate(r.bounds);
            }
            if(first)return false;
            var center=camera.WorldToScreenPoint(bounds.center);
            if(center.z<=camera.nearClipPlane || !camera.pixelRect.Contains(center))return false;
            string target=CurrentDropTarget() ?? guideSource;
            bool OnTask(Vector2 point)
            {
                string seen=Pick(camera.ScreenPointToRay(point));
                return seen==target || target=="tank" && seen=="tank-lid" || target=="print-frame" && seen=="print-frame-lid";
            }
            if(!OnTask(center))return false;
            // Keep the marker on the item's visible surface: a floating marker above
            // a flat bag can visually land on the neighbouring tank.
            var right=camera.transform.right;
            float halfWidth=Vector3.Dot(new Vector3(Mathf.Abs(right.x),Mathf.Abs(right.y),Mathf.Abs(right.z)),bounds.extents);
            Vector2 inset=camera.WorldToScreenPoint(bounds.center-right*(halfWidth*.5f));
            screen=camera.pixelRect.Contains(inset) && OnTask(inset)?inset:(Vector2)center;
            return true;
        }

        public void RefreshDarkroomHighlight()
        {
            ClearDarkroomHighlight();
            if(!DarkroomGuidanceActive || Host==null || !(Host.InputContext.Allows(InputContextId.Station) || Host.InputContext.Allows(InputContextId.Darkroom)))return;
            ResolveDarkroomGuide();
            string id=(printDrag!=null || dragged!=null)?CurrentDropTarget():guideSource;
            if(string.IsNullOrEmpty(id) || !Nodes.ContainsKey(id))return;
            bool ready=(printDrag!=null || dragged!=null) && CanCommitDarkroom(lastRay,id);
            var renderers=GuideSurfaces(id);
            foreach(var renderer in renderers)
            {
                if(renderer==null || !renderer.enabled || !renderer.gameObject.activeInHierarchy || renderer.sharedMaterial==null)continue;
                var material=renderer.sharedMaterial;
                int property=Shader.PropertyToID(material.HasProperty("_GuideTint")?"_GuideTint":material.HasProperty("_BaseColor")?"_BaseColor":"_Color");
                if(!material.HasProperty(property))continue;
                renderer.GetPropertyBlock(guideBlock);
                Color original=guideBlock.HasColor(property)?guideBlock.GetColor(property):material.GetColor(property);
                guideColors[renderer]=original;guideProperties[renderer]=property;
                Color warm=Color.Lerp(original,ready?new Color(.55f,1,.76f,original.a):new Color(1,.91f,.68f,original.a),ready?.32f:.22f);
                guideBlock.SetColor(property,warm);renderer.SetPropertyBlock(guideBlock);
                // Photo surfaces have indexed blocks for their own image/wetness.
                // Unity gives those precedence over the renderer-wide highlight.
                var materials=renderer.sharedMaterials;
                for(int i=0;i<materials.Length;i++)
                {
                    var slot=materials[i];if(slot==null)continue;
                    renderer.GetPropertyBlock(guideBlock,i);if(guideBlock.isEmpty)continue;
                    int tint=Shader.PropertyToID(slot.HasProperty("_GuideTint")?"_GuideTint":slot.HasProperty("_BaseColor")?"_BaseColor":"_Color");
                    if(!slot.HasProperty(tint))continue;
                    Color baseColor=guideBlock.HasColor(tint)?guideBlock.GetColor(tint):slot.GetColor(tint);
                    guideMaterialColors.Add(new GuideMaterialColor{renderer=renderer,index=i,property=tint,color=baseColor});
                    guideBlock.SetColor(tint,Color.Lerp(baseColor,ready?new Color(.55f,1,.76f,baseColor.a):new Color(1,.91f,.68f,baseColor.a),ready?.32f:.22f));
                    renderer.SetPropertyBlock(guideBlock,i);
                }
            }
        }
    }
}
