using System;
using System.IO;
using UnityEngine;
namespace MemoryWhitebox
{
    public static class TapeRecordingSourceResolver
    {
        public static string Hash(float[] samples)
        {var bytes=new byte[samples.Length*sizeof(float)];Buffer.BlockCopy(samples,0,bytes,0,bytes.Length);using(var h=System.Security.Cryptography.SHA256.Create())return Convert.ToBase64String(h.ComputeHash(bytes));}
        public static TapeCommissionAudio Resolve(TapeCommissionDefinition definition,int rate,int index)
        {
            string file=Path.Combine(GameSavePaths.Root,index==0?"tape-one-pcm-v1.bin":"tape-commission-"+index+"-pcm.bin");
            return ResolveFile(definition,rate,file);
        }
        public static TapeCommissionAudio ResolveFile(TapeCommissionDefinition definition,int rate,string file)
        {
            var current=TapeCommissionCompiler.Build(definition,rate);
            if(!File.Exists(file))return current;
            using(var reader=new BinaryReader(File.OpenRead(file)))
            {
                int version=reader.ReadInt32(),savedRate=reader.ReadInt32(),length=reader.ReadInt32();
                if(version<1 || version>3 || savedRate!=rate)throw new InvalidDataException("Unsupported recording version/sample rate; file preserved.");
                string hash=version>=2?reader.ReadString():null;
                // v1 has no hash: only the immutable legacy definition can interpret it safely.
                if(hash!=null && length==current.Samples.Length && hash==Hash(current.Samples))return current;
                var old=definition.legacyDefinition;
                var visited=new System.Collections.Generic.HashSet<TapeCommissionDefinition>{definition};
                while(old!=null && visited.Add(old))
                {var legacy=TapeCommissionCompiler.Build(old,rate);if(length==legacy.Samples.Length && (hash==null || hash==Hash(legacy.Samples)))return legacy;old=old.legacyDefinition;}
                throw new InvalidDataException("No matching source revision; recording preserved without rewriting.");
            }
        }
    }
}
