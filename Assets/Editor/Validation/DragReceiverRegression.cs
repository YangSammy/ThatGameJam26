using System;
using UnityEngine;
namespace MemoryWhitebox.Editor
{
    public static class DragReceiverRegression
    {
        public static string RunPrintPlacement()
        {
            var state=new DarkroomPrintState();
            state.negative.locationId="area/house.darkroom.work-area";
            state.ValidateProgress(ParkPrintStage.NegativeDrying,false);
            state.negative.locationId="print-frame";
            state.paper.locationId="area/house.darkroom.work-area";
            state.ValidateProgress(ParkPrintStage.PaperDeveloper,false);
            var restored=JsonUtility.FromJson<DarkroomPrintState>(JsonUtility.ToJson(state));
            restored.ValidateProgress(ParkPrintStage.PaperDeveloper,false);
            bool rejected=false;
            try{state.ValidateProgress(ParkPrintStage.PaperDeveloper,true);}catch(System.IO.InvalidDataException){rejected=true;}
            Require(rejected,"Table placement must not count as processing");
            state.frameClosed=true;rejected=false;
            try{state.Validate();state.ValidateProgress(ParkPrintStage.ContactReady,false);}catch(System.IO.InvalidDataException){rejected=true;}
            Require(rejected,"Closed frame still requires both materials");
            state.frameClosed=false;state.paper.locationId="area/";rejected=false;
            try{state.Validate();}catch(System.IO.InvalidDataException){rejected=true;}
            Require(rejected,"Empty work area rejected");
            state.paper.locationId="print-paper-home";state.Validate();
            return "PASS print tabletop placement, JSON restore, processing/frame guards and legacy home";
        }
        public static string Run()
        {
            var receiver = new GameObject("Receiver regression");
            var obstruction = new GameObject("Neighbour regression");
            try
            {
                // Keep synthetic geometry away from scene objects.
                var origin = new Vector3(1000, 1000, 1000);
                receiver.transform.position = origin + new Vector3(0, 0, 2);
                receiver.AddComponent<BoxCollider>().size = new Vector3(1, 1, .1f);
                obstruction.transform.position = origin + new Vector3(.12f, 0, 1);
                obstruction.AddComponent<BoxCollider>().size = new Vector3(.06f, .2f, .1f);
                Physics.SyncTransforms();
                var ray = new Ray(origin, new Vector3(.4f, 0, 2));
                Require(DragReceiverProbe.Accepts(ray, receiver.transform, null, .4f), "Visible receiver must use actual cursor ray");
                obstruction.transform.position = ray.GetPoint(1);
                Physics.SyncTransforms();
                Require(!DragReceiverProbe.Accepts(ray, receiver.transform, null, .4f), "Actual obstruction must still reject drop");
                Require(DragReceiverProbe.Evaluate(ray, receiver.transform, null, .4f)==DragReceiverProbe.Result.Occluded, "Explain occlusion");
                obstruction.SetActive(false);
                Require(!DragReceiverProbe.Accepts(ray, receiver.transform, null, .4f, .5f), "Out of reach rejected");
                Require(DragReceiverProbe.Evaluate(ray, receiver.transform, null, .4f,.5f)==DragReceiverProbe.Result.TooFar, "Explain reach");
                Require(DragReceiverProbe.Evaluate(new Ray(origin,Vector3.left),receiver.transform,null,.4f)==DragReceiverProbe.Result.Outside,"Explain missed target");
                receiver.SetActive(false);
                Require(DragReceiverProbe.Evaluate(ray,receiver.transform,null,.4f)==DragReceiverProbe.Result.Unavailable,"Explain unavailable target");
                return "PASS receiver acceptance and four distinct rejection reasons";
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(receiver);
                UnityEngine.Object.DestroyImmediate(obstruction);
            }
        }
        static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
    }
}

