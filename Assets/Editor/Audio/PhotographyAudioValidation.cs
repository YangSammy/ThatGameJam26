using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
namespace MemoryWhitebox.EditorTools
{
    public static class PhotographyAudioValidation
    {
        static void Check(bool condition,string message){if(!condition)throw new InvalidOperationException("PHOTO_AUDIO: "+message);}
        [MenuItem("Tools/Audio/Validate Photography Cues")]
        public static void Validate(){Debug.Log(Run());}
        public static string Run()
        {
            var bank=Resources.Load<TapeSfxBank>("PhotographySfxBank");
            Check(bank!=null,"Missing photography bank");
            int[] ids={21,22,23,123,24,25,26,27,28,29,129,30,130,31,131,231,331,51};
            Check(bank.cues.Length==ids.Length && bank.cues.Select(c=>c.id).Distinct().Count()==ids.Length,"Cue IDs overlap");
            foreach(var id in ids)
            {
                var c=bank.Find(id);Check(c!=null && c.clip!=null,"Missing clip "+id);
                Check(c.start>=0 && c.duration>.02f && c.start+c.duration<=c.clip.length+.002f,"Invalid slice "+id);
                Check(c.volume>0 && c.volume<=1,"Invalid volume "+id);
                Check(c.clip.LoadAudioData(),"Cannot load PCM "+id);
                var pcm=new float[c.clip.samples*c.clip.channels];Check(c.clip.GetData(pcm,0),"Cannot read PCM "+id);
                float peak=0;foreach(var value in pcm){Check(!float.IsNaN(value) && !float.IsInfinity(value),"Invalid sample");peak=Mathf.Max(peak,Mathf.Abs(value));}
                Check(peak>.0001f && peak<.55f,"Silent or clipped slice "+id);
            }
            if(!Application.isPlaying)return "PASS 18 photography cues: unique IDs, imported clips, valid bounds, finite nonzero PCM, conservative peak; playback requires Play";
            var go=new GameObject("Isolated photography audio pool validation");
            try
            {
                var player=go.AddComponent<TapeSfxPlayer>();player.bank=bank;
                Vector3 at=Camera.main!=null?Camera.main.transform.position:Vector3.zero;
                var sources=go.GetComponentsInChildren<AudioSource>();int count=sources.Length;
                player.Shot(26,at);Check(sources.Any(a=>a.isPlaying && a.clip==bank.Find(26).clip),"Authored detent does not play");
                player.StopAll();Check(sources.All(a=>!a.isPlaying),"StopAll leaves playing voices");
                player.Loop(0,28,true,at);Check(sources.Any(a=>a.isPlaying && a.clip==bank.Find(28).clip),"Winding loop does not start");
                player.StopLoop(0,true);Check(sources.All(a=>!a.isPlaying),"Release does not stop loop");
                player.Shot(27,at);player.SetMuted(true);Check(sources.All(a=>!a.isPlaying),"Mute leaves active voice");
                player.Shot(27,at);Check(sources.All(a=>!a.isPlaying),"Muted shutter played");
                player.SetMuted(false);player.Shot(27,at);Check(sources.Any(a=>a.isPlaying),"Unmuted shutter unavailable");
                player.StopAll();player.Shot(9999,at);Check(sources.All(a=>!a.isPlaying),"Missing cue fabricated sound");
                // Match a script reload: scene children survive, managed Voice wrappers do not.
                var fields=BindingFlags.Instance|BindingFlags.NonPublic;
                foreach(string name in new[]{"loops","shots"})
                {var cache=(Array)typeof(TapeSfxPlayer).GetField(name,fields).GetValue(player);Array.Clear(cache,0,cache.Length);}
                player.StopLoop(0,true);
                player.Shot(26,at);
                Check(sources.Any(a=>a.isPlaying && a.clip==bank.Find(26).clip),"Reload failed to rebind existing audio source");
                Check(go.GetComponentsInChildren<AudioSource>().Length==count,"Reload duplicated voice pool");
                player.StopAll();
                Check(go.GetComponentsInChildren<AudioSource>().Length==count,"Voice pool grew per action");
                return "PASS 18 imported photography cues and PCM; real authored one-shot/loop playback, immediate release, mute/unmute, missing cue silence, bounded source pool and reload rebind. No scenes or saves changed.";
            }
            finally{UnityEngine.Object.DestroyImmediate(go);}
        }
    }
}
