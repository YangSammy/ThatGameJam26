using UnityEngine;

namespace MemoryWhitebox
{
    // Built-in pipeline camera effect; no canvas or replacement of scene geometry.
    [ExecuteAlways, RequireComponent(typeof(Camera))]
    public sealed class PaintingDefocus : MonoBehaviour
    {
        public float Amount { get; private set; }
        public const float ClearSeconds = .30f, BlurSeconds = .25f;
        Material material;
        public bool HasShader => material != null;
        public bool IsSupported => material!=null&&material.shader.isSupported&&material.passCount==2;

        public void Begin()
        {
            if(material==null)
            {
                var template=Resources.Load<Material>("PaintingDefocus");
                if(template==null)throw new System.InvalidOperationException("PaintingDefocus material is missing from Resources.");
                material=new Material(template){hideFlags=HideFlags.HideAndDontSave};
            }
            Amount=1;enabled=true;
        }
        public void SetFocusHeld(bool held,float dt)
        {Amount=Mathf.MoveTowards(Amount,held?0:1,Mathf.Max(0,dt)/(held?ClearSeconds:BlurSeconds));}
        public void End(){Amount=0;enabled=false;}

        void OnRenderImage(RenderTexture source,RenderTexture destination)
        {
            if(material==null||Amount<=.001f){Graphics.Blit(source,destination);return;}
            var descriptor=source.descriptor;descriptor.width=Mathf.Max(1,source.width/2);descriptor.height=Mathf.Max(1,source.height/2);
            descriptor.depthBufferBits=0;descriptor.msaaSamples=1;descriptor.useMipMap=false;
            RenderTexture a=null,b=null;
            try
            {
                a=RenderTexture.GetTemporary(descriptor);b=RenderTexture.GetTemporary(descriptor);
                a.filterMode=b.filterMode=FilterMode.Bilinear;
                a.wrapMode=b.wrapMode=TextureWrapMode.Clamp;
                Graphics.Blit(source,a);
                // Nine taps in each axis. Radius is in half-resolution pixels.
                material.SetVector("_Direction",new Vector4(1,0,0,0));material.SetFloat("_Radius",1.7f*Amount);
                Graphics.Blit(a,b,material,0);
                material.SetVector("_Direction",new Vector4(0,1,0,0));Graphics.Blit(b,a,material,0);
                material.SetTexture("_BlurTex",a);material.SetFloat("_Amount",Amount);
                Graphics.Blit(source,destination,material,1);
            }
            finally
            {
                if(a!=null)RenderTexture.ReleaseTemporary(a);if(b!=null)RenderTexture.ReleaseTemporary(b);
            }
        }
        void OnDisable(){Amount=0;}
        void OnDestroy()
        {
            if(material==null)return;
            if(Application.isPlaying)Destroy(material);else DestroyImmediate(material);
        }
    }
}
