using System;
using UnityEngine;
using MemoryWhitebox.Art;

namespace MemoryWhitebox
{
    public sealed partial class WritingWorld3D
    {
        WritingVisualAssets visualAssets;
        WritingPropVisual crumpleVisual, timerVisual;
        Transform penVisual, penTip, penGrip, crumpleGrip, writingHandContact;
        readonly System.Collections.Generic.List<GameObject> discardedVisuals = new System.Collections.Generic.List<GameObject>();
        readonly WritingPropVisual[] carriedVisuals = new WritingPropVisual[4];
        readonly bool[] presentedCollected = new bool[4];
        readonly float[] pickupProgress = new float[4];

        static Transform VisualNode(GameObject root, string name)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == name) return t;
            throw new InvalidOperationException("Writing visual node missing: " + name);
        }

        // Keep authored animation paths intact. A wrapper carries placement/scale, never an animated node.
        GameObject CreateVisual(GameObject prefab, Transform parent, string onlyPart = null)
        {
            if (prefab == null) throw new InvalidOperationException("Missing WritingVisualAssets prefab.");
            var visual = UnityEngine.Object.Instantiate(prefab, parent, false);
            visual.name = "Visual_" + prefab.name;
            foreach (var col in visual.GetComponentsInChildren<Collider>(true)) col.enabled = false;
            if (onlyPart != null)
                foreach (var part in new[] { "PaperBody", "PenAssembly", "BasketBody" })
                    VisualNode(visual, part).gameObject.SetActive(part == onlyPart);
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
            // Keep the paper hotspot and its original footprint, but remove nonuniform inherited mesh scale.
            sheet.GetComponent<Renderer>().enabled = false;
            sheet.GetComponent<BoxCollider>().size = sheet.transform.localScale;
            sheet.transform.localScale = Vector3.one;
            var paper = CreateVisual(visualAssets.writingSet, sheet.transform, "PaperBody");
            FitVisual(paper, new Vector3(.95f, 1, .72f), new Vector3(0, .006f, 0), new Vector3(0, 90, 0));

            paperBall.gameObject.SetActive(true);
            HideMesh(paperBall);
            var folded = CreateVisual(visualAssets.writingSet, paperBall, "PaperBody");
            FitVisual(folded, new Vector3(.95f, 1, .72f), Vector3.zero, new Vector3(0, 90, 0));
            crumpleVisual = folded.GetComponent<WritingPropVisual>();
            crumpleGrip = VisualNode(folded, "CrumpleGrip_R");
            paperBall.gameObject.SetActive(false);

            HideMesh(penHand);
            var pen = CreateVisual(visualAssets.writingSet, penHand, "PenAssembly");
            penVisual = pen.transform;
            penVisual.localScale = Vector3.one * 1.6f;
            penVisual.localRotation = Quaternion.Euler(0, -22, -48);
            penTip = VisualNode(pen, "PenTipAnchor");
            penGrip = VisualNode(pen, "PenGripAnchor");
            // penPoint is the existing hand target, 13 mm above the ink. Align the actual nib to it.
            penVisual.localPosition -= penHand.InverseTransformPoint(penTip.position) + Vector3.up * .013f;

            foreach (var node in Nodes)
                if (node.Key.StartsWith("bin", StringComparison.Ordinal)) HideMesh(node.Value.transform);
            var basketRoot = Group("Visual_PaperBasket", binAt + new Vector3(0, .54f, 0));
            var basket = CreateVisual(visualAssets.writingSet, basketRoot, "BasketBody");
            FitVisual(basket, new Vector3(.51f, .42f, .49f), Vector3.zero, Vector3.zero);
            writingHandContact = Group("WritingHandContact", Vector3.zero);
            // The existing generous receiver and paper coordinates remain unchanged.
            BuildCollectedVisuals();
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
                    FitVisual(v, sizes[i] * (carried ? .45f : 1), Vector3.zero, Vector3.zero);
                    if (carried)
                    {
                        var driver = v.GetComponent<WritingPropVisual>();
                        carriedVisuals[i] = driver;
                        if (i == 2) driver.SetHandleProgress(1);
                    }
                }
            }
            var clock = timerNeedle.parent;
            HideMesh(clock);
            var timer = CreateVisual(visualAssets.timer, clock);
            FitVisual(timer, new Vector3(.20f, .16f, .20f), Vector3.zero, Vector3.zero);
            timerVisual = timer.GetComponent<WritingPropVisual>();
            UpdateWritingVisuals(0, true);
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
                else pickupProgress[i] = Mathf.MoveTowards(pickupProgress[i], collected ? 1 : 0, dt * 3);
                presentedCollected[i] = collected;
                if (i == 1) carriedVisuals[i].SetPickupSettleProgress(pickupProgress[i]);
                // Pendant chain remains supported by its miniature carry pose; no invented swinging physics.
            }
        }

        void AddDiscardedPaper()
        {
            RestoreDiscardedPaper();
        }

        void RestoreDiscardedPaper()
        {
            // Draft already exists in the checkpoint. Reconstruct cosmetic history without new save fields.
            foreach (var old in discardedVisuals) ReleaseObject(old);
            discardedVisuals.Clear();
            int count = Mathf.Min(12, Mathf.Max(0, State.Draft - 1));
            for (int n = 0; n < count; n++)
            {
            int index = n % visualAssets.paperBalls.Length;
            var root = Group("DiscardedDraft_" + n, binAt + new Vector3((n % 3 - 1) * .09f, .43f + n / 3 * .04f, (n % 2) * .07f));
            discardedVisuals.Add(root.gameObject);
            var ball = CreateVisual(visualAssets.paperBalls[index], root);
            FitVisual(ball, Vector3.one * .15f, Vector3.zero, new Vector3(0, index * 73, 0));
            }
        }
    }
}
