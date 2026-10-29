using System;
using System.Collections.Generic;
using UnityEngine;
namespace MemoryWhitebox
{
    public sealed partial class WBMain
    {
        readonly Dictionary<string,ItemSceneBinding> itemBindings=new Dictionary<string,ItemSceneBinding>();
        readonly Dictionary<string,ChapterSceneBinding> chapterBindings=new Dictionary<string,ChapterSceneBinding>();
        public ItemSceneBinding FindItem(string id)=>id!=null && itemBindings.TryGetValue(id,out var binding)?binding:null;
        public ChapterSceneBinding FindChapterBinding(string id)=>chapterBindings.TryGetValue(id,out var binding)?binding:null;
        public Chapter FindChapter(string id)=>chapters[ChapterIds.Index(id)];
        void BindChapterData()
        {
            itemBindings.Clear();chapterBindings.Clear();
            foreach(var root in gameObject.scene.GetRootGameObjects())
            {
                foreach(var binding in root.GetComponentsInChildren<ChapterSceneBinding>(true))
                {
                    ChapterIds.Index(binding.ChapterId);
                    if(chapterBindings.ContainsKey(binding.ChapterId))throw new InvalidOperationException("Duplicate chapter ID: "+binding.ChapterId);
                    chapterBindings.Add(binding.ChapterId,binding);
                }
                foreach(var binding in root.GetComponentsInChildren<ItemSceneBinding>(true))
                {
                    if(string.IsNullOrEmpty(binding.instanceId) || string.IsNullOrEmpty(binding.DefinitionId))throw new InvalidOperationException("Missing item identity on "+binding.name);
                    if(itemBindings.ContainsKey(binding.instanceId))throw new InvalidOperationException("Duplicate item ID: "+binding.instanceId);
                    itemBindings.Add(binding.instanceId,binding);
                }
            }
            for(int i=0;i<chapters.Length;i++)
            {var id=ChapterIds.At(i);var binding=FindChapterBinding(id);chapters[i].BindDefinition(id,binding!=null?binding.definition:null);}
            var ids=new string[radioDeck.tapes.Length];
            for(int i=0;i<ids.Length;i++)
            {
                var binding=radioDeck.tapes[i].root.GetComponent<ItemSceneBinding>();
                ids[i]=binding!=null?binding.instanceId:"tape.work."+(i+1);
            }
            radioDeck.Chapter.BindItemIds(ids);
            // Configuration references are now available; initial Awake used legacy bindings.
            radioDeck.SynchronizeCommission();
        }
    }
}
