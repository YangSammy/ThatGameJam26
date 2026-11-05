using System;
using System.Linq;
using System.Reflection;
using System.IO;
using UnityEngine;
using UnityEditor;

namespace MemoryWhitebox.EditorTools
{
    public static class RestorationAudioChecks
    {
        const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
        static void Check(bool ok,string label){if(!ok)throw new InvalidOperationException("RESTORATION_AUDIO: "+label);}
        static void Set(object obj,string field,object value){obj.GetType().GetField(field,Hidden).SetValue(obj,value);}
        static T Get<T>(object obj,string field)=>(T)obj.GetType().GetField(field,Hidden).GetValue(obj);
        static void Call(object obj,string method,params object[] args){obj.GetType().GetMethod(method,Hidden).Invoke(obj,args);}
        [MenuItem("Tools/Audio/Validate Autumn Winter Restoration")]
        public static void Validate(){Debug.Log(Run());}
        public static string Run()
        {
            var score=Resources.Load<GameScoreSettings>("GameScore");
            var settings=Resources.Load<GameScoreLayers>("GameScoreLayers");
            var bank=Resources.Load<TapeSfxBank>("StorySfxBank");
            Check(score!=null && settings!=null && settings.arrangements.Length==4,"four bound arrangements");
            var music=new[]{score.letterLoop,score.winterPark,score.winterRoom,score.ward};
            foreach(var source in music)
            {
                var arrangement=settings.Find(source);
                Check(arrangement!=null && arrangement.layers.Length==(source==score.ward?5:4),"layer count "+source.name);
                Check(arrangement.layers.Select(l=>l.clip).Distinct().Count()==arrangement.layers.Length,"independent clips "+source.name);
                foreach(var layer in arrangement.layers)
                {
                    Check(layer.clip!=null && layer.clip!=source,"supplement is independent of source mix");
                    Check(layer.clip.frequency==48000 && layer.clip.channels==2 && layer.clip.samples==source.samples,"same origin/rate/length "+layer.role);
                    var importer=(AudioImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(layer.clip));
                    Check(importer.defaultSampleSettings.compressionFormat==AudioCompressionFormat.PCM && importer.defaultSampleSettings.loadType==AudioClipLoadType.Streaming && !importer.defaultSampleSettings.preloadAudioData,"lossless streaming sample sync "+layer.role);
                    // Streamed clips cannot GetData. Validate the original PCM24 RIFF edge directly.
                    using(var file=new BinaryReader(File.OpenRead(AssetDatabase.GetAssetPath(layer.clip))))
                    {
                        file.BaseStream.Position=12;bool found=false;
                        while(file.BaseStream.Position+8<=file.BaseStream.Length)
                        {
                            string tag=new string(file.ReadChars(4));uint size=file.ReadUInt32();long next=file.BaseStream.Position+size+(size%2);
                            if(tag=="data")
                            {
                                Check(size==(uint)(layer.clip.samples*6),"24-bit stereo data size");
                                byte[] first=file.ReadBytes(6);file.BaseStream.Position=next-(size%2)-6;byte[] last=file.ReadBytes(6);
                                Check(first.All(b=>b==0)&&last.All(b=>b==0),"loop PCM edge "+layer.clip.name);found=true;break;
                            }
                            file.BaseStream.Position=next;
                        }
                        Check(found,"RIFF data chunk");
                    }
                }
            }
            Check(bank!=null && bank.cues.Select(c=>c.id).Distinct().Count()==bank.cues.Length,"bank IDs unique");
            foreach(int cue in new[]{134,234,246})
            {
                var item=bank.Find(cue);
                Check(item!=null && item.clip!=null && item.start+item.duration<=item.clip.length+.001f,"new Foley binding "+cue);
            }
            var writing=new WritingChapter{Phase="collect",Started=true,Remaining=16};
            Check(GameScore.AutumnLayerProgress(writing)==0,"before final fifteen seconds");
            writing.Remaining=7.5f;Check(Mathf.Abs(GameScore.AutumnLayerProgress(writing)-.5f)<.001f,"late density midpoint");
            writing.Remaining=0;Check(GameScore.AutumnLayerProgress(writing)==1,"late density maximum");
            writing.Phase="tutorial";Check(GameScore.AutumnLayerProgress(writing)==0,"tutorial does not inject urgency");
            Check(GameScore.LayerWeight(.1f,.4f)==0 && GameScore.LayerWeight(.8f,.4f)==1,"progress gating");
            var root=new GameObject("Isolated restoration score checks"){hideFlags=HideFlags.HideAndDontSave};root.SetActive(false);
            var copiedScore=UnityEngine.Object.Instantiate(score);
            try
            {
                var player=root.AddComponent<GameScore>();
                if(Get<AudioSource[]>(player,"beds")==null)Call(player,"Awake");
                Set(player,"score",copiedScore);
                var beds=Get<AudioSource[]>(player,"beds");var layers=Get<AudioSource[]>(player,"layerVoices");
                var ward=settings.Find(score.ward);
                Set(player,"arrangement",ward);
                for(int i=0;i<ward.layers.Length;i++){layers[i].clip=ward.layers[i].clip;layers[i].volume=.4f;}
                beds[0].clip=score.ward;beds[0].volume=.4f;
                var paper=root.AddComponent<AudioSource>();paper.clip=bank.Find(149).clip;paper.volume=.27f;
                var chapter=new PaintingChapter();Set(chapter,"<Phase>k__BackingField",3);
                player.BeginWardNarrative(chapter);Set(player,"paused",true);Set(player,"restartScheduled",true);
                player.CutWardNarrative(chapter);
                Check(beds.All(b=>b.clip==null)&&layers.All(l=>l.clip==null && l.volume==0),"synchronous cut while paused");
                Check(!Get<bool>(player,"restartScheduled"),"cut cannot revive pending DSP start");
                Check(paper.clip==bank.Find(149).clip && paper.volume==.27f,"paper voice unaffected by cut");
                var autumn=settings.Find(score.letterLoop);Set(player,"arrangement",autumn);
                copiedScore.gain=0;Set(player,"gainScale",1f);
                for(int i=0;i<autumn.layers.Length;i++)layers[i].volume=.4f;
                Call(player,"UpdateLayers",.016f);
                Check(layers.Take(autumn.layers.Length).All(l=>l.volume==0),"music gain zero takes effect immediately");
                Check(paper.clip!=null && paper.volume==.27f,"music mute preserves Foley");
                Set(player,"arrangement",ward);Call(player,"UpdateLayers",1f);
                Check(layers[4].volume>0 && layers.Take(4).All(l=>l.volume==0),"ward environment independent of music mute");
                Set(player,"arrangement",autumn);Set(player,"restartScheduled",true);Call(player,"StopWardVoices");
                Check(Get<bool>(player,"restartScheduled"),"ward cancellation does not cancel another pending score");
            }
            finally{UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(copiedScore);}
            return "PASS four synchronized sample-length arrangements / 17 independently controlled supplemental layers, PCM boundaries, 3 new Foley cues, autumn density, progress gating, immediate paused ward cut, paper independence, music mute and independent ward air. Actual DSP listening remains manual.";
        }
    }
}
