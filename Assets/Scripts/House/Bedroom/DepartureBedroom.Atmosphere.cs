using UnityEngine;

namespace MemoryWhitebox
{
    public sealed partial class DepartureBedroom
    {
        AudioSource winterWind,doorAir;
        float springWindowVolume,springMix;
        Transform[] meltwater;
        Vector3[] meltwaterSizes;
        Material meltwaterMaterial;

        void BeginSpringAtmosphere()
        {
            springWindowVolume=windowSounds.volume;
            windowSounds.volume=0;windowSounds.Play();
            var ambience=Resources.Load<GameAmbienceSettings>("GameAmbience");
            if(ambience!=null)
            {
                if(ambience.seasons.Length>3)
                    winterWind=AtmosphereVoice("Winter wind receding",windowSounds.transform.position,ambience.seasons[3],6);
                doorAir=AtmosphereVoice("Air beyond the door",threshold.position-threshold.forward*1.5f+Vector3.up,ambience.globalLoop,5);
            }
            meltwaterMaterial=new Material(Shader.Find("Standard")){name="Departure meltwater",color=new Color(.23f,.29f,.27f)};
            meltwaterMaterial.SetFloat("_Glossiness",.85f);
            meltwater=new Transform[snow.Length];meltwaterSizes=new Vector3[snow.Length];
            for(int i=0;i<snow.Length;i++)
            {
                var r=snow[i].GetComponentInChildren<Renderer>();if(r==null)continue;
                var bounds=r.bounds;
                var water=GameObject.CreatePrimitive(PrimitiveType.Cylinder);water.name="Meltwater under "+snow[i].name;
                water.transform.SetParent(transform,true);
                water.transform.position=new Vector3(bounds.center.x,bounds.min.y+.001f,bounds.center.z);
                var hit=water.GetComponent<Collider>();hit.enabled=false;Destroy(hit);
                water.GetComponent<Renderer>().sharedMaterial=meltwaterMaterial;
                meltwater[i]=water.transform;
                meltwaterSizes[i]=new Vector3(bounds.size.x,.001f,bounds.size.z);
            }
            UpdateMeltwater(springMix);
        }

        AudioSource AtmosphereVoice(string label,Vector3 position,AudioClip clip,float range)
        {
            var g=new GameObject(label);g.transform.SetParent(transform,true);g.transform.position=position;
            var voice=g.AddComponent<AudioSource>();voice.clip=clip;voice.playOnAwake=false;voice.loop=true;
            voice.spatialBlend=1;voice.rolloffMode=AudioRolloffMode.Linear;voice.minDistance=.7f;voice.maxDistance=range;
            voice.dopplerLevel=0;voice.volume=0;if(clip!=null)voice.Play();return voice;
        }

        void UpdateMeltwater(float progress)
        {
            springMix=progress;if(meltwater==null)return;
            for(int i=0;i<meltwater.Length;i++)if(meltwater[i]!=null)
                meltwater[i].localScale=Vector3.Scale(meltwaterSizes[i],new Vector3(Mathf.Lerp(.1f,1.06f,progress),1,Mathf.Lerp(.1f,1.06f,progress)));
        }

        void UpdateSpringAtmosphere()
        {
            if(owner==null)return;
            bool silent=owner.Muted || owner.ShowHelp || !Application.isFocused;
            float tail=owner.StoryFinished?1-owner.EndingFade:1;
            windowSounds.mute=silent;windowSounds.volume=springWindowVolume*springMix*tail;
            if(winterWind!=null){winterWind.mute=silent;winterWind.volume=.18f*(1-springMix)*tail;}
            if(doorAir!=null){doorAir.mute=silent;doorAir.volume=(owner.DepartureDoorOpen?.08f:.035f)*tail;}
        }
    }
}
