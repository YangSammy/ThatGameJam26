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
            writingFoley=Root.AddComponent<AudioSource>();writingFoley.playOnAwake=false;
            pencilStroke=Resources.Load<AudioClip>("ChapterFoley/Pencil");timerStart=Resources.Load<AudioClip>("ChapterFoley/Clock");
            visualAssets = WritingVisualAssets.Load();
            if (visualAssets == null) throw new InvalidOperationException("WritingVisualAssets configuration missing.");
            writingHandContact=Group("WritingHandContact",Vector3.zero);
            BuildCollectedVisuals();
        }

        public void AttachDesk(AutumnWritingDesk layout)
        {
            if(desk==layout)return;
            desk=layout;sheet=desk.paper.gameObject;
            crumpleVisual=desk.foldedPaper;paperBall=crumpleVisual.transform.parent;
            penHand=desk.penMotion;penTip=desk.penTip;penGrip=desk.penGrip;
            crumpleGrip=desk.crumpleGrip;timerVisual=desk.timer;
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
                    FitVisual(v, sizes[i] * (carried ? .45f : 1), Vector3.zero, Vector3.zero);
                    if (!carried) { HighlightPickup(v); AddPickupRing(root,i); }
                    if (carried)
                    {
                        var driver = v.GetComponent<WritingPropVisual>();
                        carriedVisuals[i] = driver;
                        if (i == 2) driver.SetHandleProgress(1);
                    }
                }
            }
        }

        readonly Dictionary<Material,Material> pickupMaterials=new Dictionary<Material,Material>();

        void HighlightPickup(GameObject visual)
        {
            foreach(var r in visual.GetComponentsInChildren<Renderer>())
            {
                var slots=r.sharedMaterials;
                for(int i=0;i<slots.Length;i++)
                {
                    var source=slots[i];
                    if(source==null || source.renderQueue>=3000 || !source.HasProperty("_Color"))continue;
                    if(!pickupMaterials.TryGetValue(source,out var glow))
                    {
                        glow=new Material(source);
                        glow.color=Color.Lerp(source.color,new Color(1,.72f,.24f),.55f);
                        if(glow.HasProperty("_EmissionColor"))
                        {
                            glow.EnableKeyword("_EMISSION");
                            glow.SetColor("_EmissionColor",new Color(.38f,.16f,.025f));
                        }
                        pickupMaterials.Add(source,glow);
                    }
                    slots[i]=glow;
                }
                r.sharedMaterials=slots;
            }
        }

        readonly LineRenderer[] pickupRings=new LineRenderer[4];
        AudioSource writingFoley;
        AudioClip pencilStroke,timerStart;
        float nextPencil;

        void PlayWritingCue(AudioClip clip)
        {
            if(clip!=null && (Host==null || !Host.Muted))writingFoley.PlayOneShot(clip,.42f);
        }

        void AddPickupRing(Transform item,int index)
        {
            var ring=Line("Pickup halo",new Vector3[0],"#FFC354",.038f);
            ring.transform.SetParent(item,false);
            ring.gameObject.layer=MemoryLayer;
            ring.sharedMaterial.shader=Shader.Find("Unlit/Color");
            pickupRings[index]=ring;
            ring.loop=true;
            ring.positionCount=40;
            for(int i=0;i<ring.positionCount;i++)
            {
                float a=2*Mathf.PI*i/ring.positionCount;
                ring.SetPosition(i,new Vector3(.30f*Mathf.Cos(a),.025f-item.localPosition.y,.30f*Mathf.Sin(a)));
            }
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
                bool near=State.InPickupRange(i);
                var ring=pickupRings[i];
                float radius=near?.58f:1;
                ring.transform.localScale=Vector3.Lerp(ring.transform.localScale,new Vector3(radius,1,radius),restoring?1:Mathf.Clamp01(dt*14));
                ring.startWidth=ring.endWidth=near?.055f:.038f;
                ring.sharedMaterial.color=State.InkFresh && (near || State.Phase=="tutorial")?new Color(1,.97f,.64f):new Color(1,.7f,.24f);
                bool collected = State.HasItem(i);
                if (restoring) pickupProgress[i] = collected ? 1 : 0;
                else if (collected && !presentedCollected[i]) pickupProgress[i] = 0;
                else pickupProgress[i] = Mathf.MoveTowards(pickupProgress[i], collected ? 1 : 0, dt * 3);
                presentedCollected[i] = collected;
                if (i == 1) carriedVisuals[i].SetPickupSettleProgress(pickupProgress[i]);
                // Pendant chain remains supported by its miniature carry pose; no invented swinging physics.
            }
        }

    }
}
