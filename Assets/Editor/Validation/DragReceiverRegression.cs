using System;
using UnityEngine;
namespace MemoryWhitebox.Editor
{
    public static class DragReceiverRegression
    {
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
                obstruction.SetActive(false);
                Require(!DragReceiverProbe.Accepts(ray, receiver.transform, null, .4f, .5f), "Out of reach rejected");
                return "PASS receiver visibility, real obstruction, reach";
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

