using UnityEngine;

namespace MemoryWhitebox
{
    // Standalone concept preview. No chapter state, player input or save access.
    [RequireComponent(typeof(Camera))]
    public sealed class WardSoftRevealPreview : MonoBehaviour
    {
        [SerializeField] Material revealMaterial;
        [SerializeField, Min(0.1f)] float duration=12;
        [SerializeField, Min(0)] float openingHold=1;
        [SerializeField, Min(0)] float endingHold=2;
        [SerializeField] bool loop=true;
        Material instance;
        float elapsed;
        static readonly int ProgressId=Shader.PropertyToID("_Progress");
        static readonly int ViewportId=Shader.PropertyToID("_ViewportAspect");

        public float Progress => Mathf.Clamp01((elapsed-openingHold)/Mathf.Max(0.1f,duration));

        void OnEnable()
        {
            elapsed=0;
            if(revealMaterial!=null) instance=new Material(revealMaterial);
        }
        void Update()
        {
            elapsed+=Time.deltaTime;
            float cycle=Mathf.Max(0,openingHold)+Mathf.Max(0.1f,duration)+Mathf.Max(0,endingHold);
            if(loop && elapsed>=cycle)elapsed%=cycle;
        }
        void OnRenderImage(RenderTexture source,RenderTexture destination)
        {
            if(instance==null){Graphics.Blit(source,destination);return;}
            instance.SetFloat(ProgressId,Progress);
            instance.SetFloat(ViewportId,(float)source.width/source.height);
            // Passing the camera source here would overwrite the concept's _MainTex.
            Graphics.Blit(instance.mainTexture,destination,instance);
        }
        void OnDisable()
        {
            if(instance!=null)Destroy(instance);
            instance=null;
        }
    }
}
