using UnityEngine;

namespace MemoryWhitebox
{
    [RequireComponent(typeof(Camera))]
    public sealed class WardImageReveal : MonoBehaviour
    {
        Material material;
        static readonly int ProgressId=Shader.PropertyToID("_Progress");
        static readonly int BlendId=Shader.PropertyToID("_SceneBlend");
        static readonly int PageId=Shader.PropertyToID("_PageTex");
        static readonly int SceneId=Shader.PropertyToID("_SceneTex");
        static readonly int AspectId=Shader.PropertyToID("_ViewportAspect");
        // Calibrated against the currently bound 1672 x 941 image. Change this
        // profile with the image, never infer face positions from screen aspect.
        internal static readonly Vector4 ProtectedFace=new Vector4(.496f,.694f,.051f,.079f);
        internal const float NoseLine=.667f;
        public bool Presenting=>enabled && material!=null;

        public void Present(float progress,float blend,RenderTexture ink)
        {
            if(material==null)
            {
                var template=Resources.Load<Material>("WardRuntimeReveal");
                if(template==null)throw new System.InvalidOperationException("WardRuntimeReveal material is missing.");
                material=new Material(template){hideFlags=HideFlags.HideAndDontSave};
                ConfigureOrderedMaterial(material);
            }
            material.SetFloat(ProgressId,Mathf.Clamp01(progress));
            material.SetFloat(BlendId,Mathf.Clamp01(blend));
            material.SetTexture(PageId,ink);
            // This camera always owns live ink composition; do not inherit the
            // preview material's disabled toggle when authoring refreshes it.
            material.SetFloat("_UsePageInk",ink!=null?1:0);
            enabled=true;
        }
        public void End(){enabled=false;}
        public static void ConfigureOrderedMaterial(Material target)
        {
            target.SetFloat("_OrderedReveal",1);
            target.SetFloat("_RevealStyle",1);
            target.SetFloat("_RevealTop",NoseLine);
            target.SetVector("_FaceEllipse",ProtectedFace);
        }
        void OnRenderImage(RenderTexture source,RenderTexture destination)
        {
            if(material==null){Graphics.Blit(source,destination);return;}
            material.SetTexture(SceneId,source);
            material.SetFloat(AspectId,(float)source.width/source.height);
            Graphics.Blit(material.mainTexture,destination,material);
        }
        void OnDestroy()
        {
            if(material==null)return;
            if(Application.isPlaying)Destroy(material);else DestroyImmediate(material);
        }
    }
}
