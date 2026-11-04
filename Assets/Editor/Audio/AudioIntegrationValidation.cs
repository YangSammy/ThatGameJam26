using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;
namespace MemoryWhitebox.EditorTools
{
    public static class AudioIntegrationValidation
    {
        [MenuItem("Tools/Audio/Validate Source Revisions and Ambience")]
        public static void Validate(){Debug.Log(Run());}
        static void Require(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
        public static string Run()
        {
            var report=new StringBuilder();var a=Resources.Load<GameAmbienceSettings>("GameAmbience");
            Require(a!=null&&a.globalLoop!=null&&a.seasons.Length==4&&a.seasons.All(c=>c!=null)&&a.seasons.Distinct().Count()==4,"Ambience bindings incomplete");report.AppendLine("PASS global and four distinct seasonal clips.");
            string file=Path.Combine(Path.GetTempPath(),"tape-source-validation-"+Guid.NewGuid().ToString("N")+".bin");
            try
            {
                for(int i=1;i<=3;i++)
                {
                    var d=AssetDatabase.LoadAssetAtPath<TapeCommissionDefinition>("Assets/Data/TapeCommissions/Commission_"+i.ToString("00")+".asset");
                    var current=TapeCommissionCompiler.Build(d,48000);var legacy=TapeCommissionCompiler.Build(d.legacyDefinition,48000);
                    Require(TapeRecordingSourceResolver.Hash(current.Samples)!=TapeRecordingSourceResolver.Hash(legacy.Samples),"New source must contain new PCM");
                    Require(current.Reject(Enumerable.Range(0,current.CleanMap.Length).Where(n=>current.CleanMap[n]>=0).ToArray())=="","Clean recording rejected");
                    var versions=new System.Collections.Generic.List<TapeCommissionAudio>{current};var revision=d.legacyDefinition;while(revision!=null){versions.Add(TapeCommissionCompiler.Build(revision,48000));revision=revision.legacyDefinition;}
                    foreach(var version in versions)
                    {
                        using(var w=new BinaryWriter(File.Create(file))){w.Write(3);w.Write(48000);w.Write(version.Samples.Length);w.Write(TapeRecordingSourceResolver.Hash(version.Samples));w.Write(0);w.Write(0);w.Write(0);}
                        byte[] before=File.ReadAllBytes(file);var resolved=TapeRecordingSourceResolver.ResolveFile(d,48000,file);
                        Require(TapeRecordingSourceResolver.Hash(resolved.Samples)==TapeRecordingSourceResolver.Hash(version.Samples),"Wrong source selected");
                        Require(ReferenceEquals(resolved.Rules,version.Rules),"Wrong acceptance revision selected");Require(before.SequenceEqual(File.ReadAllBytes(file)),"Resolver modified save");
                    }
                    if(i<=2)
                    {
                        var oldest=versions[versions.Count-1];
                        using(var w=new BinaryWriter(File.Create(file))){w.Write(1);w.Write(48000);w.Write(oldest.Samples.Length);}
                        var restored=TapeRecordingSourceResolver.ResolveFile(d,48000,file);
                        Require(TapeRecordingSourceResolver.Hash(restored.Samples)==TapeRecordingSourceResolver.Hash(oldest.Samples),"Hashless recording must use original PCM");
                    }
                    using(var w=new BinaryWriter(File.Create(file))){w.Write(3);w.Write(48000);w.Write(current.Samples.Length);w.Write("unknown hash");}
                    bool rejected=false;try{TapeRecordingSourceResolver.ResolveFile(d,48000,file);}catch(InvalidDataException){rejected=true;}Require(rejected,"Unknown source must not reset recording");
                    report.AppendLine("PASS commission "+i+": changed PCM, clean submission, new/legacy source and acceptance restoration, unknown version rejected.");
                    if(i==3)
                    {
                        Require(d.sourceContainsNoise,"Approved source must be pre-mixed");
                        Require(current.Samples.Length==1252800,"Noise must not be inserted twice");
                        var pcm=new float[d.sourceAudio.samples*d.sourceAudio.channels];d.sourceAudio.GetData(pcm,0);
                        for(int frame=0;frame<current.Samples.Length;frame++)
                        {
                            float expected=0;for(int c=0;c<d.sourceAudio.channels;c++)expected+=pcm[frame*d.sourceAudio.channels+c]/d.sourceAudio.channels;
                            Require(Mathf.Abs(expected-current.Samples[frame])<.0000001f,"Compiled source differs from approved WAV");
                            bool noise=frame>=700800&&frame<777600;Require((current.CleanMap[frame]<0)==noise,"Incorrect noise map");
                        }
                        report.AppendLine("PASS approved WAV samples unchanged after mono downmix, no duplicate noise, correct 14.6-16.2 noise map.");
                    }
                }
            }
            finally{if(File.Exists(file))File.Delete(file);}
            return report.ToString();
        }
    }
}
