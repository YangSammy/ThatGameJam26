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
                        else if(!printObjectsState.frameClosed)Guide("print-frame-lid",null,"Click to close","Close the contact frame — click its rim.");
                        else Guide("print-exposure",null,"Hold LMB to expose",printRemaining>0?"Continue exposure — hold the switch ("+printRemaining.ToString("0.0")+"s).":"Hold the exposure switch.");
                        break;
                    case ParkPrintStage.PaperDeveloper:
                        if(printObjectsState.frameClosed)Guide("print-frame-lid",null,"Click to open","Open the contact frame — click its rim.");
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
                    else Guide("exposed-film","bag","Drag to dark bag","Move the film into the dark bag.");
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
        string DarkroomTask {get{ResolveDarkroomGuide();string target=CurrentDropTarget();return target!=null?"Move to "+DarkroomName(target)+"; release LMB.":guideTask;}}
        static string DarkroomName(string id)
        {
            switch(id)
            {
                case "rewind":return "Rewind knob";
                case "back":return "Camera back";
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
                case "print-frame-lid":return "Contact frame rim";
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
        public string DarkroomTooltip(Ray ray)
        {
            if(!DarkroomGuidanceActive)return "";
            ResolveDarkroomGuide();string id=Pick(ray),name=DarkroomName(id);
            string drop=CurrentDropTarget();
            if(drop!=null && CanDropDarkroom(ray,drop))return DarkroomName(drop)+"\nRelease LMB here";
            if(name==null)return "";
            if(id=="tank-lid" && tankSealed && guideSource=="tank"){id="tank";name=DarkroomName(id);}
            bool carrying=printDrag!=null || dragged!=null;
            if(id==guideTarget || (id=="print-frame-lid" && guideTarget=="print-frame"))
                return name+"\n"+(carrying?"Move closer; keep target clear":"Place "+DarkroomName(guideSource)+" here");
            if(id==guideSource || (id=="tank" && guideSource=="tank-lid"))return name+"\n"+guideAction;
            if(printProcessing || Photo.Phase==PhotoChapter.FilmPhase.Processing)return name+"\nProcessing — please wait";
            return name+"\n"+(ParkPrintsReady?"Processing complete":"Not needed for this step");
        }
        public void ClearDarkroomHighlight()
        {
            foreach(var pair in guideColors)
            {
                if(pair.Key==null)continue;
                pair.Key.GetPropertyBlock(guideBlock);
                guideBlock.SetColor(guideProperties[pair.Key],pair.Value);
                pair.Key.SetPropertyBlock(guideBlock);
            }
            guideColors.Clear();guideProperties.Clear();
        }
        public void RefreshDarkroomHighlight()
        {
            ClearDarkroomHighlight();
            if(!DarkroomGuidanceActive || Host==null || !(Host.InputContext.Allows(InputContextId.Station) || Host.InputContext.Allows(InputContextId.Darkroom)))return;
            ResolveDarkroomGuide();
            string id=(printDrag!=null || dragged!=null)?CurrentDropTarget():guideSource;
            if(string.IsNullOrEmpty(id) || !Nodes.ContainsKey(id))return;
            Renderer[] renderers;
            if(!guideRenderers.TryGetValue(id,out renderers)){renderers=Nodes[id].GetComponentsInChildren<Renderer>(true);guideRenderers[id]=renderers;}
            foreach(var renderer in renderers)
            {
                if(renderer==null || !renderer.gameObject.activeInHierarchy || renderer.sharedMaterial==null)continue;
                var material=renderer.sharedMaterial;
                int property=Shader.PropertyToID(material.HasProperty("_GuideTint")?"_GuideTint":material.HasProperty("_BaseColor")?"_BaseColor":"_Color");
                if(!material.HasProperty(property))continue;
                renderer.GetPropertyBlock(guideBlock);
                Color original=guideBlock.HasColor(property)?guideBlock.GetColor(property):material.GetColor(property);
                guideColors[renderer]=original;guideProperties[renderer]=property;
                Color warm=Color.Lerp(original,new Color(1,.91f,.68f,original.a),.22f);
                guideBlock.SetColor(property,warm);renderer.SetPropertyBlock(guideBlock);
            }
        }
    }
}
