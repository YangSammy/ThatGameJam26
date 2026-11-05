using System;
using System.Collections.Generic;
using UnityEngine;
using MemoryWhitebox.Art;

namespace MemoryWhitebox
{
    public sealed partial class WritingWorld3D
    {
        WritingVisualAssets visualAssets;
        WritingPropVisual crumpleVisual, timerVisual;
        Transform penTip, penGrip, crumpleGrip, writingHandContact;
        readonly WritingPropVisual[] carriedVisuals = new WritingPropVisual[4];
        readonly bool[] presentedCollected = new bool[4];
        readonly float[] pickupProgress = new float[4];

        // Keep authored animation paths intact. A wrapper carries placement/scale, never an animated node.
        GameObject CreateVisual(GameObject prefab, Transform parent)
        {
            if (prefab == null) throw new InvalidOperationException("Missing WritingVisualAssets prefab.");
            var visual = UnityEngine.Object.Instantiate(prefab, parent, false);
            visual.name = "Visual_" + prefab.name;
            foreach (var col in visual.GetComponentsInChildren<Collider>(true)) col.enabled = false;
            SetLayer(visual.transform, parent.gameObject.layer);
            return visual;
        }

        static Bounds VisualBounds(GameObject visual, Transform space)
        {
            bool found = false;
            var bounds = new Bounds();
            foreach (var r in visual.GetComponentsInChildren<Renderer>())
            {
                var sk = r as SkinnedMeshRenderer;
                var mf = r.GetComponent<MeshFilter>();
                var mesh = sk != null ? sk.sharedMesh : mf != null ? mf.sharedMesh : null;
                if (mesh == null) continue;
                var b = mesh.bounds;
                for (int i = 0; i < 8; i++)
                {
                    var v = b.center + Vector3.Scale(b.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                    v = space.InverseTransformPoint(r.transform.TransformPoint(v));
                    if (!found) { bounds = new Bounds(v, Vector3.zero); found = true; }
                    else bounds.Encapsulate(v);
                }
            }
            if (!found) throw new InvalidOperationException("Writing visual has no active mesh: " + visual.name);
            return bounds;
        }

        static void FitVisual(GameObject visual, Vector3 size, Vector3 center, Vector3 euler)
        {
            var t = visual.transform;
            t.localRotation = Quaternion.Euler(euler);
            var b = VisualBounds(visual, t.parent);
            float scale = Mathf.Min(size.x / Mathf.Max(.0001f, b.size.x), size.y / Mathf.Max(.0001f, b.size.y), size.z / Mathf.Max(.0001f, b.size.z));
            t.localScale *= scale;
            b = VisualBounds(visual, t.parent);
            t.localPosition += center - b.center;
        }

        static void HideMesh(Transform target)
        {
            foreach (var r in target.GetComponentsInChildren<Renderer>(true)) r.enabled = false;
        }

        void BuildWritingVisuals()
        {
            visualAssets = WritingVisualAssets.Load();
            if (visualAssets == null) throw new InvalidOperationException("WritingVisualAssets configuration missing.");
            writingHandContact=Group("WritingHandContact",Vector3.zero);
            BuildCollectedVisuals();
            BuildAutumnPerformance();
            for(int i=0;i<4;i++){presentedCollected[i]=State.HasItem(i);pickupProgress[i]=State.HasItem(i)?1:0;}
            UpdateCarryPresentation(0,true);
        }

        public void AttachDesk(AutumnWritingDesk layout)
        {
            if(desk==layout)return;
            desk=layout;sheet=desk.paper.gameObject;
            crumpleVisual=desk.foldedPaper;paperBall=crumpleVisual.transform.parent;
            penHand=desk.penMotion;penTip=desk.penTip;penGrip=desk.penGrip;
            crumpleGrip=desk.crumpleGrip;timerVisual=desk.timer;
            CacheLetterFraming();
            BindHit(desk.paper,"paper");
            foreach(var hit in desk.basketHits)BindHit(hit,hit.name);
            RestoreInk();desk.ShowDrafts(State.Draft);UpdateWritingVisuals(0,true);
        }
        public void DetachDesk()
        {
            if(desk==null)return;
            CancelPointerGesture();ReleaseTeaTable();
            Nodes.Remove("paper");foreach(var hit in desk.basketHits)Nodes.Remove(hit.name);
            foreach(var stroke in ink)if(stroke!=null)ReleaseObject(stroke.gameObject);
            ink.Clear();strokeRows.Clear();desk=null;sheet=null;paperBall=penHand=null;
            timerVisual=crumpleVisual=null;penTip=penGrip=crumpleGrip=null;
        }
        void BindHit(Collider hit,string id)
        {
            var hotspot=hit.GetComponent<WorldHotspot>();
            if(hotspot==null)hotspot=hit.gameObject.AddComponent<WorldHotspot>();
            hotspot.Owner=this;hotspot.Id=id;Nodes[id]=hit.gameObject;
        }

        void BuildCollectedVisuals()
        {
            var sizes = new[] { new Vector3(.22f, .12f, .27f), new Vector3(.28f, .38f, .28f), new Vector3(.42f, .34f, .24f), new Vector3(.30f, .04f, .20f) };
            for (int i = 0; i < 4; i++)
            {
                foreach (var root in new[] { collectionProps[i], inventoryProps[i] })
                {
                    HideMesh(root);
                    var collider = root.GetComponent<BoxCollider>();
                    if (collider != null) collider.size = root.localScale;
                    root.localScale = Vector3.one;
                    var v = CreateVisual(visualAssets.collectedItems[i], root);
                    bool carried = root == inventoryProps[i];
                    FitVisual(v, sizes[i], Vector3.zero, Vector3.zero);
                    if (!carried && i==2) v.GetComponent<WritingPropVisual>().SetHandleProgress(1);
                    if (carried)
                    {
                        var driver = v.GetComponent<WritingPropVisual>();
                        carriedVisuals[i] = driver;
                        if (i == 2) driver.SetHandleProgress(1);
                    }
                }
            }
            inventoryProps[2].SetParent(memory,true);
        }

        void UpdateWritingVisuals(float dt, bool restoring = false)
        {
            if (timerVisual == null) return;
            var contact = draggingPaper ? crumpleGrip : penGrip;
            writingHandContact.SetPositionAndRotation(contact.position, contact.rotation);
            float duration = NarrativeDefinition.Resolve(State)?.writingSeconds ?? 60;
            timerVisual.SetTimerProgress(1 - Mathf.Clamp01(State.Remaining / Mathf.Max(.01f, duration)));
            for (int i = 0; i < 4; i++)
            {
                bool collected = State.HasItem(i);
                if (restoring) pickupProgress[i] = collected ? 1 : 0;
                else if (collected && !presentedCollected[i]) pickupProgress[i] = 0;
                else pickupProgress[i] = Mathf.MoveTowards(pickupProgress[i], collected ? 1 : 0, dt * 1.5f);
                presentedCollected[i] = collected;
                if (i == 1) carriedVisuals[i].SetPickupSettleProgress(pickupProgress[i]);
            }
            if(restoring)RestoreMemoryPresentation();
            UpdateCarryPresentation(dt,restoring);
        }

    }
}
