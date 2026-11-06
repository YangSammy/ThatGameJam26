using UnityEngine;
namespace MemoryWhitebox
{
    // Presentation only. Must be requested by the authoritative chapter each frame.
    [DefaultExecutionOrder(1000)]
    public sealed class TaskOutlineCue : MonoBehaviour
    {
        readonly HoverOutline outline=new HoverOutline();
        Camera view; Renderer[] surfaces; int requested=-10;
        public static void Show(Transform owner,Camera camera,Renderer[] renderers)
        {
            if(!owner)return;
            var cue=owner.GetComponent<TaskOutlineCue>();
            if(!cue)cue=owner.gameObject.AddComponent<TaskOutlineCue>();
            cue.view=camera;cue.surfaces=renderers;cue.requested=Time.frameCount;
            cue.outline.Show(camera,renderers,color:new Color(1,.65f,.12f,1));
        }
        void LateUpdate(){if(requested!=Time.frameCount || GamePauseMenu.BlocksGameplay)outline.Clear();}
        void OnDisable(){outline.Clear();}
        void OnDestroy(){outline.Dispose();}
    }
}
