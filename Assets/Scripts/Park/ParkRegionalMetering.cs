using UnityEngine;

namespace MemoryWhitebox
{
    public sealed class ParkRegionalMetering : MonoBehaviour
    {
        [Range(0,20)] public float openEv100=13;
        public ParkLightZone[] zones=System.Array.Empty<ParkLightZone>();
        public Light sunlight;
        [Min(.01f)] public float calibratedSunIntensity=1.28f;
        public float Sample(Vector3 position)
        {
            float ev=openEv100;
            int previous=int.MinValue;
            // Blend each priority over the lower-priority result, not over open air.
            // Otherwise entering a thin edge of shade can briefly brighten the meter.
            for(int layer=0;layer<zones.Length;layer++)
            {
                int priority=int.MaxValue;bool found=false;
                foreach(var zone in zones)
                    if(zone!=null && zone.isActiveAndEnabled && (layer==0 || zone.priority>previous) && (!found || zone.priority<priority))
                    {priority=zone.priority;found=true;}
                if(!found)break;
                float weighted=0,total=0,strongest=0;
                foreach(var zone in zones)
                {
                    if(zone==null || zone.priority!=priority)continue;
                    float weight=zone.Weight(position);
                    weighted+=weight*zone.ev100;total+=weight;strongest=Mathf.Max(strongest,weight);
                }
                if(total>0)ev=Mathf.Lerp(ev,weighted/total,strongest);
                previous=priority;
            }
            // Bounded ambient floor; supports authoring changes to the actual sun.
            if(sunlight!=null)
            {
                float intensity=sunlight.isActiveAndEnabled?sunlight.intensity:0;
                ev+=Mathf.Log(Mathf.Clamp(intensity/Mathf.Max(.01f,calibratedSunIntensity),.125f,8),2);
            }
            return Mathf.Clamp(ev,0,20);
        }
    }
}
