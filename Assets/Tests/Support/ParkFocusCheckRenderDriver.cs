#if UNITY_EDITOR
using System.Reflection;
using UnityEngine;
namespace MemoryWhitebox.Editor
{
    // Runtime image effects do not receive Edit-mode camera callbacks. Forward only in
    // this isolated preview scene, so tests exercise the real component's render path.
    [ExecuteAlways]
    public sealed class ParkFocusCheckRenderDriver : MonoBehaviour
    {
        public ParkPhotoFocus focus;
        static readonly MethodInfo Render=typeof(ParkPhotoFocus).GetMethod("OnRenderImage",BindingFlags.Instance|BindingFlags.NonPublic);
        void OnRenderImage(RenderTexture source,RenderTexture destination)
        {
            if(focus!=null && !Application.isPlaying)Render.Invoke(focus,new object[]{source,destination});
            else Graphics.Blit(source,destination);
        }
    }
}
#endif
