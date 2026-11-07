using System;
using UnityEngine;

namespace MemoryWhitebox
{
    // Presentation only: receives the committed chapter state, never listens for input or saves.
    public sealed class DarkroomAssetVisuals : MonoBehaviour
    {
        [Serializable] public sealed class Model { public string id; public GameObject root; }
        public Model[] models;
        public AnimationClip[] clips;
        public float materialDisplayScale=1.25f;
        MaterialPropertyBlock properties;
        Mesh boundsMesh;
        readonly System.Collections.Generic.Dictionary<SkinnedMeshRenderer,int> boundsStates=new System.Collections.Generic.Dictionary<SkinnedMeshRenderer,int>();
        Texture2D[] negatives;
        string negativeRoll;
        float frameOpenProgress,frameOpenTarget;
        bool framePoseReady;
        BoxCollider movingFrameHit;
        public void SetContactFrameClosed(bool closed,BoxCollider hit)
        {
            frameOpenTarget=closed?0:1;movingFrameHit=hit;
            // First binding restores the saved pose immediately; subsequent
            // interactions animate only presentation, never chapter progress.
            if(!framePoseReady){frameOpenProgress=frameOpenTarget;framePoseReady=true;}
            ApplyContactFramePose();
        }
        void ApplyContactFramePose()
        {
            Sample("print-frame","C07_LatchOpen",Mathf.Clamp01(frameOpenProgress*4));
            Sample("print-frame","C07_PlateOpen",Mathf.SmoothStep(0,1,frameOpenProgress));
            FitPartHit(movingFrameHit,"print-frame","PressurePlate_Mesh");
        }
        void LateUpdate()
        {
            if(!framePoseReady || GamePauseMenu.BlocksGameplay)return;
            frameOpenProgress=Mathf.MoveTowards(frameOpenProgress,frameOpenTarget,Time.deltaTime/.45f);
            ApplyContactFramePose();
        }
        // Hanging meshes extend along local -Y. Gravity stays vertical even when
        // an authored hanger/parent is pitched or rolled while arranging the room.
        public static Quaternion HangingRotation(Transform holder)
        {
            var forward=Vector3.ProjectOnPlane(holder.forward,Vector3.up);
            if(forward.sqrMagnitude<.0001f)
                forward=Vector3.Cross(Vector3.ProjectOnPlane(holder.right,Vector3.up),Vector3.up);
            return Quaternion.LookRotation(forward.normalized,Vector3.up);
        }
        public Transform ModelRoot(string id) { foreach(var m in models) if(m.id==id) return m.root.transform; return null; }
        public static Transform Node(Transform root,string name)
        { if(root==null)return null;foreach(var t in root.GetComponentsInChildren<Transform>(true))if(t.name==name)return t;return null; }
        public void Sample(string id,string clip,float progress=1)
        {
            var root=ModelRoot(id);if(root==null)return;
            foreach(var c in clips)if(c!=null && c.name=="AN_"+clip){c.SamplePropAnimation(root.gameObject,Mathf.Clamp01(progress)*c.length);return;}
        }
        // Committed hanging is a state, not an optional animation result. Explicitly
        // clear progressive poses so a failed/restored clip cannot leave a flat card.
        public void SetMaterialHanging(string id,bool hanging)
        {
            var root=ModelRoot(id);if(root==null)return;
            foreach(var skin in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                var mesh=skin.sharedMesh;if(mesh==null)continue;
                int shape=mesh.GetBlendShapeIndex("Hanging");if(shape<0)continue;
                if(hanging)
                    for(int i=0;i<mesh.blendShapeCount;i++)
                    {
                        string name=mesh.GetBlendShapeName(i);
                        if(name.StartsWith("Hang_") || name.StartsWith("Flatten_") || name.StartsWith("Unroll_") || name=="Coiled" || name=="Wet" || name=="CornerLift")
                            skin.SetBlendShapeWeight(i,0);
                    }
                skin.SetBlendShapeWeight(shape,hanging?100:0);
            }
        }
        public void ResetModels()
        {
            foreach(var m in models)foreach(var c in clips)
                if(c!=null && c.name.EndsWith("_RestAll") && c.name.StartsWith("AN_"+m.root.GetComponent<DarkroomVisualIdentity>().assetName+"_"))c.SamplePropAnimation(m.root,0);
        }
        public void Surface(string id,string materialName,Texture image,float reveal,float wetness)
        {
            if(properties==null)properties=new MaterialPropertyBlock();
            var root=ModelRoot(id);if(root==null)return;
            foreach(var r in root.GetComponentsInChildren<Renderer>(true))for(int i=0;i<r.sharedMaterials.Length;i++)
            {
                var mat=r.sharedMaterials[i];if(mat==null || mat.name!=materialName)continue;
                r.GetPropertyBlock(properties,i);
                properties.SetTexture("_MainTex",image!=null?image:Texture2D.whiteTexture);
                properties.SetFloat("_Reveal",reveal);properties.SetFloat("_Wetness",wetness);
                properties.SetFloat("_Glossiness",Mathf.Lerp(.18f,.7f,wetness));r.SetPropertyBlock(properties,i);
            }
        }
        public void ShowNegatives(string rollId,bool fixedFilm,float wetness)
        {
            if(negativeRoll!=rollId){ReleaseImages();negativeRoll=rollId;}
            if(!fixedFilm || string.IsNullOrEmpty(rollId))return;
            if(negatives==null)
            {
                try{negatives=ParkFilmStore.CreatePhysicalNegativePreviews(rollId);}
                catch(Exception e){Debug.LogWarning("Negative model images unavailable: "+e.Message);return;}
            }
            for(int i=0;i<negatives.Length && i<3;i++)Surface("print-negative","M_C_NegativeFrame"+(i+1),negatives[i],1,wetness);
        }
        void ReleaseImages(){if(negatives!=null)foreach(var t in negatives)if(t!=null)Destroy(t);negatives=null;}
        public void UpdateDeformedBounds()
        {
            if(boundsMesh==null)boundsMesh=new Mesh();
            foreach(var m in models)foreach(var r in m.root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                int hash=17,previous;
                for(int i=0;i<r.sharedMesh.blendShapeCount;i++)unchecked{hash=hash*31+r.GetBlendShapeWeight(i).GetHashCode();}
                if(boundsStates.TryGetValue(r,out previous) && previous==hash)continue;
                r.BakeMesh(boundsMesh);boundsMesh.RecalculateBounds();r.localBounds=boundsMesh.bounds;boundsStates[r]=hash;
            }
            FitMaterialHit("print-paper");FitMaterialHit("print-negative");
        }
        public void FitPartHit(BoxCollider hit,string model,string part)
        {
            var node=Node(ModelRoot(model),part);
            var mesh=node!=null?node.GetComponent<MeshFilter>():null;
            if(hit==null || mesh==null || mesh.sharedMesh==null)return;
            // Use the animated part's local mesh, not the former whitebox lid.
            // Keep the existing hotspot identity and scene anchor untouched.
            var b=mesh.sharedMesh.bounds;var local=new Bounds();
            for(int i=0;i<8;i++)
            {
                var corner=b.center+Vector3.Scale(b.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1));
                var p=hit.transform.InverseTransformPoint(node.TransformPoint(corner));
                if(i==0)local=new Bounds(p,Vector3.zero);else local.Encapsulate(p);
            }
            var scale=hit.transform.lossyScale;
            hit.center=local.center;
            hit.size=local.size+new Vector3(.006f/Mathf.Max(.0001f,Mathf.Abs(scale.x)),.006f/Mathf.Max(.0001f,Mathf.Abs(scale.y)),.006f/Mathf.Max(.0001f,Mathf.Abs(scale.z)));
        }
        void FitMaterialHit(string id)
        {
            var visual=ModelRoot(id);if(visual==null)return;
            var hit=visual.parent.GetComponent<BoxCollider>();if(hit==null)return;
            bool found=false;Bounds local=new Bounds();
            foreach(var r in visual.GetComponentsInChildren<Renderer>())
            {
                if(!r.enabled)continue;var b=r.bounds;
                for(int i=0;i<8;i++)
                {
                    var p=hit.transform.InverseTransformPoint(b.center+Vector3.Scale(b.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1)));
                    if(!found){local=new Bounds(p,Vector3.zero);found=true;}else local.Encapsulate(p);
                }
            }
            if(!found)return;var s=hit.transform.lossyScale;
            hit.center=local.center;hit.size=local.size+new Vector3(.012f/Mathf.Abs(s.x),.012f/Mathf.Abs(s.y),.012f/Mathf.Abs(s.z));
        }
        void OnDestroy(){ReleaseImages();if(boundsMesh!=null)Destroy(boundsMesh);}
    }
}
