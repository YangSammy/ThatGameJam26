using System;
using UnityEngine;

namespace MemoryWhitebox
{
    // User preferences are separate from chapter progress and authored control tuning.
    public static class GamePreferences
    {
        const string Key="MemoryWhitebox.Preferences.v1";
        [Serializable] public sealed class Values
        {
            public float volume=1,lookMultiplier=1;
            public int quality;
            public bool fullscreen,vsync;
            public Values Copy()=>(Values)MemberwiseClone();
        }
        public static Values Current {get;private set;}
        public static Values Defaults()=>new Values {quality=Mathf.Min(2,QualitySettings.names.Length-1),fullscreen=true,vsync=true};
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Initialize()
        {
            Current=new Values{quality=QualitySettings.GetQualityLevel(),fullscreen=Screen.fullScreen,vsync=QualitySettings.vSyncCount>0};
            if(!PlayerPrefs.HasKey(Key)){AudioListener.volume=1;return;}
            try {Current=JsonUtility.FromJson<Values>(PlayerPrefs.GetString(Key))??Defaults();}
            catch(Exception e){Debug.LogWarning("Preferences reset: "+e.Message);}
            Apply(Current);
        }
        public static float LookMultiplier=>Current!=null?Current.lookMultiplier:1;
        public static void Save(Values value)
        {
            var next=value.Copy();Sanitize(next);
            PlayerPrefs.SetString(Key,JsonUtility.ToJson(next));PlayerPrefs.Save();
            Current=next;Apply(next);
        }
        static void Sanitize(Values value)
        {
            value.volume=float.IsNaN(value.volume)?1:Mathf.Clamp01(value.volume);
            value.lookMultiplier=float.IsNaN(value.lookMultiplier)?1:Mathf.Clamp(value.lookMultiplier,.25f,3);
            value.quality=Mathf.Clamp(value.quality,0,Mathf.Max(0,QualitySettings.names.Length-1));
        }
        static void Apply(Values value)
        {
            Sanitize(value);AudioListener.volume=value.volume;
            if(QualitySettings.GetQualityLevel()!=value.quality)QualitySettings.SetQualityLevel(value.quality,true);
            QualitySettings.vSyncCount=value.vsync?1:0;
            if(Screen.fullScreen!=value.fullscreen)Screen.fullScreen=value.fullscreen;
        }
    }
}
