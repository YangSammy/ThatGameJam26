using System;
using System.IO;
namespace MemoryWhitebox
{
    [Serializable] public sealed class DarkroomPrintState
    {
        public int version=1;
        public bool frameClosed;
        public ItemSaveRecord negative=new ItemSaveRecord{instanceId="darkroom.negative",definitionId="photo.negative",ownerChapterId=ChapterIds.Photography,locationId="print-negative-home"};
        public ItemSaveRecord paper=new ItemSaveRecord{instanceId="darkroom.paper",definitionId="photo.contact-sheet",ownerChapterId=ChapterIds.Photography,locationId="print-paper-home"};
        public void Validate()
        {
            if(version!=1 || negative==null || paper==null || negative.instanceId!="darkroom.negative" || paper.instanceId!="darkroom.paper" ||
               negative.definitionId!="photo.negative" || paper.definitionId!="photo.contact-sheet" || negative.ownerChapterId!=ChapterIds.Photography || paper.ownerChapterId!=ChapterIds.Photography ||
               negative.schemaVersion!=1 || paper.schemaVersion!=1)throw new InvalidDataException("Invalid darkroom item identity.");
            if(Array.IndexOf(new[]{"print-negative-home","print-negative-clip","print-frame"},negative.locationId)<0 ||
               Array.IndexOf(new[]{"print-paper-home","print-frame","print-developer","print-stop","print-fixer","print-wash","print-photo-clip"},paper.locationId)<0)
                throw new InvalidDataException("Invalid darkroom item placement.");
        }
        public static DarkroomPrintState FromLegacy(ParkPrintStage stage,bool processing)
        {
            var state=new DarkroomPrintState();
            if(stage==ParkPrintStage.None)return state;
            state.negative.locationId=stage==ParkPrintStage.NegativeDrying?(processing?"print-negative-clip":"print-negative-home"):"print-frame";
            if(stage==ParkPrintStage.ContactReady || stage==ParkPrintStage.Exposing){state.paper.locationId="print-frame";state.frameClosed=true;}
            else if(stage>=ParkPrintStage.PaperDeveloper)
            {
                string[] stations={"print-frame","print-developer","print-stop","print-fixer","print-wash","print-photo-clip"};
                int index=(int)stage-(int)ParkPrintStage.PaperDeveloper+(processing?1:0);
                state.paper.locationId=stations[Math.Min(index,stations.Length-1)];
            }
            return state;
        }
        public void ValidateProgress(ParkPrintStage stage,bool processing)
        {
            Validate();
            if(frameClosed && (negative.locationId!="print-frame" || paper.locationId!="print-frame"))throw new InvalidDataException("Closed contact frame is incomplete.");
            if(stage>=ParkPrintStage.PaperDeveloper && negative.locationId!="print-frame")throw new InvalidDataException("Negative was not contact printed.");
            if(stage==ParkPrintStage.Complete && paper.locationId!="print-photo-clip")throw new InvalidDataException("Finished print must be hung.");
            if(!processing)return;
            string expected=stage==ParkPrintStage.NegativeDrying?"print-negative-clip":stage==ParkPrintStage.Exposing?"print-frame":
                stage==ParkPrintStage.PaperDeveloper?"print-developer":stage==ParkPrintStage.PaperStop?"print-stop":stage==ParkPrintStage.PaperFixer?"print-fixer":
                stage==ParkPrintStage.PaperWash?"print-wash":stage==ParkPrintStage.PaperDrying?"print-photo-clip":null;
            if(expected==null || (stage==ParkPrintStage.NegativeDrying?negative.locationId:paper.locationId)!=expected)throw new InvalidDataException("Processing material is at the wrong station.");
        }
    }
}
