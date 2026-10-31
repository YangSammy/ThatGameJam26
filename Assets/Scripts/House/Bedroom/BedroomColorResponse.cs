using UnityEngine;

// Retains HDR window highlights and the warm wood palette in the actual first-person renderer.
// This is deliberately only a display response: no painted-in shadows, depth or geometry.
[ExecuteAlways, RequireComponent(typeof(Camera))]
public sealed class BedroomColorResponse : MonoBehaviour
{
    public Material Profile;
    [ImageEffectTransformsToLDR]
    void OnRenderImage(RenderTexture source,RenderTexture destination)
    {
        if(Profile!=null&&Profile.shader.isSupported)Graphics.Blit(source,destination,Profile);
        else Graphics.Blit(source,destination);
    }
}
