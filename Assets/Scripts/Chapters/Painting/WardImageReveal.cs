using UnityEngine;

namespace MemoryWhitebox
{
    [RequireComponent(typeof(Camera))]
    public sealed class WardImageReveal : MonoBehaviour
    {
        Material material;
        float runProgress;
        static readonly int ProgressId=Shader.PropertyToID("_Progress");
        static readonly int BlendId=Shader.PropertyToID("_SceneBlend");
        static readonly int PageId=Shader.PropertyToID("_PageTex");
        static readonly int SceneId=Shader.PropertyToID("_SceneTex");
        static readonly int SmokeTimeId=Shader.PropertyToID("_SmokeTime");
        static readonly int AspectId=Shader.PropertyToID("_ViewportAspect");
        // Calibrated against the cold-light long-hair v2 image (1672 x 941). Change this
        // profile with the image, never infer face positions from screen aspect.
        internal static readonly Vector4 ProtectedFace=new Vector4(.494f,.703f,.037f,.022f);
        internal const float NoseLine=.688f;
        public bool Presenting=>enabled && material!=null;

        public void Present(float progress,float blend,RenderTexture ink)
        {
            if(material==null)
            {
                var template=Resources.Load<Material>("WardRuntimeReveal");
                if(template==null)throw new System.InvalidOperationException("WardRuntimeReveal material is missing.");
                material=new Material(template){hideFlags=HideFlags.HideAndDontSave};
                ConfigureRevealMaterial(material);
            }
            runProgress=Mathf.Clamp01(progress);
            material.SetFloat(ProgressId,Mathf.Pow(runProgress,2.5f));
            material.SetFloat(BlendId,Mathf.Clamp01(blend));
            material.SetTexture(PageId,ink);
            // This camera always owns live ink composition; do not inherit the
            // preview material's disabled toggle when authoring refreshes it.
            material.SetFloat("_UsePageInk",ink!=null?1:0);
            enabled=true;
        }
        public void End(){enabled=false;}
        public static void ConfigureRevealMaterial(Material target)
        {
            target.SetFloat("_RevealStyle",1);
            target.SetFloat("_MeltEase",1);
            target.SetFloat("_RevealTop",NoseLine);
            target.SetVector("_FaceEllipse",ProtectedFace);
        }
        void OnRenderImage(RenderTexture source,RenderTexture destination)
        {
            if(material==null){Graphics.Blit(source,destination);return;}
            material.SetFloat(SmokeTimeId,runProgress*2);
            material.SetTexture(SceneId,source);
            material.SetFloat(AspectId,(float)source.width/source.height);
            Graphics.Blit(material.mainTexture,destination,material);
        }
        void OnGUI()
        {
            if(!Presenting || runProgress>=1)return;
            float width=Mathf.Min(320,Screen.width*.35f),x=(Screen.width-width)*.5f,y=Screen.height-65;
            GUI.Box(new Rect(x-48,y,38,34),"D");
            var old=GUI.color;GUI.color=new Color(.15f,.18f,.18f,.9f);
            GUI.DrawTexture(new Rect(x,y+5,width,24),Texture2D.whiteTexture);
            GUI.color=new Color(.8f,.75f,.56f,1);
            GUI.DrawTexture(new Rect(x+3,y+8,(width-6)*runProgress,18),Texture2D.whiteTexture);GUI.color=old;
        }
        void OnDestroy()
        {
            if(material==null)return;
            if(Application.isPlaying)Destroy(material);else DestroyImmediate(material);
        }
    }
}
