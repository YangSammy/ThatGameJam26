using System;
using UnityEngine;

namespace MemoryWhitebox
{
    public static class ChapterIds
    {
        public const string Tape="spring.tape", Photography="summer.photography", Writing="autumn.writing", Painting="winter.painting", Ending="spring.departure";
        static readonly string[] ordered={Tape,Photography,Writing,Painting,Ending};
        public static string At(int index){if(index<0 || index>=ordered.Length)throw new ArgumentOutOfRangeException(nameof(index));return ordered[index];}
        public static int Index(string id){int index=Array.IndexOf(ordered,id);if(index<0)throw new ArgumentException("Unknown chapter ID: "+id);return index;}
    }
    public readonly struct SaveResult
    {
        public readonly bool Success;
        public readonly string Error;
        SaveResult(bool success,string error){Success=success;Error=error??"";}
        public static SaveResult Ok()=>new SaveResult(true,"");
        public static SaveResult Failed(string error)=>new SaveResult(false,error);
    }
    [Serializable] public sealed class ChapterSaveRecord
    {
        public string chapterId;
        public int schemaVersion=1;
        public bool completed;
        // Owned and versioned by the chapter; never contains Unity object references.
        public string payload;
    }
    [Serializable] public sealed class ItemSaveRecord
    {
        public string instanceId,definitionId,ownerChapterId;
        public int schemaVersion=1;
        public string locationId;
        public string payload;
    }
    public sealed class ItemRuntimeState
    {
        public readonly string InstanceId,DefinitionId;
        public string LocationId {get;private set;}
        public ItemRuntimeState(string instanceId,string definitionId,string locationId)
        {InstanceId=instanceId;DefinitionId=definitionId;LocationId=locationId;}
        public void PlaceAt(string locationId){LocationId=locationId;}
    }
    [Serializable] public sealed class ArtifactReference
    {
        public string artifactId,chapterId,relativePath,sourceHash;
        public int contentVersion=1;
    }
}
