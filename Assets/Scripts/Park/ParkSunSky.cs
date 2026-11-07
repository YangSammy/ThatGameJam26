using UnityEngine;

namespace MemoryWhitebox
{
    [ExecuteAlways, DisallowMultipleComponent]
    public sealed class ParkSunSky : MonoBehaviour
    {
        public Light sun;
        public Material sky;
        [ColorUsage(false,true)] public Color radiance=new Color(24,19,13,1);
        static readonly int Direction=Shader.PropertyToID("_SunDirection"),Radiance=Shader.PropertyToID("_SunRadiance");
        void OnEnable(){Sync();}
        void LateUpdate(){Sync();}
        public void Sync()
        {
            if(sky==null)return;
            if(sun==null)
            {
                if(sky.GetColor(Radiance)!=Color.black)sky.SetColor(Radiance,Color.black);
                return;
            }
            Vector3 d=-sun.transform.forward;
            Vector4 direction=new Vector4(d.x,d.y,d.z,0);
            Color color=sun.isActiveAndEnabled&&sun.intensity>.001f?radiance:Color.black;
            if(sky.GetVector(Direction)!=direction)sky.SetVector(Direction,direction);
            if(sky.GetColor(Radiance)!=color)sky.SetColor(Radiance,color);
        }
    }
}
