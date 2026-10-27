using UnityEngine;
namespace MemoryWhitebox
{
    [RequireComponent(typeof(Camera))]
    public sealed class ParkPhotoFocus : MonoBehaviour
    {
        public Material template;
        [HideInInspector] public bool activeFocus;
        [HideInInspector] public float distance=4;
        Material effect;
        void OnEnable(){GetComponent<Camera>().depthTextureMode|=DepthTextureMode.Depth;if(template!=null)effect=new Material(template);}
        void OnRenderImage(RenderTexture source,RenderTexture destination)
        {
            if(!activeFocus || effect==null){Graphics.Blit(source,destination);return;}
            effect.SetFloat("_FocusDistance",Mathf.Max(.1f,distance));Graphics.Blit(source,destination,effect);
        }
        void OnDisable(){if(effect!=null)Destroy(effect);effect=null;}
    }
}
