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
        public void BindSummerDarkroom(SummerPhotoRoom room)
        {
            var chapter=FindChapterBinding(ChapterIds.At(1));
            var entries=new List<ItemSceneBinding>(chapter.items);
            foreach(var item in room.darkroomStation.GetComponentsInChildren<ItemSceneBinding>(true))
            {
                if(string.IsNullOrEmpty(item.instanceId) || string.IsNullOrEmpty(item.DefinitionId) || itemBindings.ContainsKey(item.instanceId))
                    throw new InvalidOperationException("Invalid darkroom item binding: "+item.instanceId);
                itemBindings.Add(item.instanceId,item);entries.Add(item);
            }
            chapter.items=entries.ToArray();
            DarkroomWorld.AttachDarkroom(room.darkroomStation);
            room.darkroomArea.RestoreMembers(story.AreaPlacements);
            if(DarkroomWorld.DirectDarkroomAvailable)DarkroomWorld.PresentDirectDarkroom();
        }

        public void UnbindSummerDarkroom(SummerPhotoRoom room)
        {
            DarkroomWorld.DetachDarkroom();
            var chapter=FindChapterBinding(ChapterIds.At(1));
            var entries=new List<ItemSceneBinding>(chapter.items);
            foreach(var item in room.darkroomStation.GetComponentsInChildren<ItemSceneBinding>(true))
            {entries.Remove(item);itemBindings.Remove(item.instanceId);}
            chapter.items=entries.ToArray();
        }

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
                    if(itemBindings.ContainsKey(binding.instanceId))throw new InvalidOperationException("Duplicate item ID: "+binding.instanceId+" on '"+itemBindings[binding.instanceId].name+"' and '"+binding.name+"'. Each scene item needs its own instance ID.");
                    itemBindings.Add(binding.instanceId,binding);
                }
            }
            for(int i=0;i<chapters.Length;i++)
            {var id=ChapterIds.At(i);var binding=FindChapterBinding(id);chapters[i].BindDefinition(id,binding!=null?binding.definition:null);}
        }

        public void AttachSpringTape(SpringTapeRoom room)
        {
            var config=room.deck.GetComponent<ChapterSceneBinding>();
            chapterBindings.Add(config.ChapterId,config);
            var model=(RadioTapeChapter)chapters[0];
            model.BindDefinition(config.ChapterId,config.definition);
            foreach(var item in room.workspace.GetComponentsInChildren<ItemSceneBinding>(true))
                itemBindings.Add(item.instanceId,item);
            var ids=System.Array.ConvertAll(room.deck.tapes,t=>t.root.GetComponent<ItemSceneBinding>().instanceId);
            model.BindItemIds(ids);
            try{model.RestoreCheckpoint(story.Checkpoint(0));}
            catch(System.Exception ex){story.BlockRestore(ex);Debug.LogError(story.Error);return;}
            if(story.IsComplete(0))model.RestoreStoryCompletion();
            radioDeck=room.deck;
            room.workspace.SetActive(true);
            radioDeck.Initialize(this,model);
            worlds[0]=new RadioTapeWorld(radioDeck);
            var subjects=new List<SafeInspectionSubject>(safeInspection.subjects);
            subjects.AddRange(room.inspectionSubjects);safeInspection.subjects=subjects.ToArray();
            room.workArea.RestoreMembers(story.AreaPlacements);
            radioDeck.AdvanceInspection(1);radioDeck.SynchronizeCommission();
            radioDeck.RestoreWorkingTape();radioDeck.RestoreSource();radioDeck.RestoreWorkbench();
            radioDeck.SetStationActive(true);
        }

        public void DetachSpringTape(SpringTapeRoom room)
        {
            radioDeck.SetStationActive(false);
            safeInspection.Close();
            var subjects=new List<SafeInspectionSubject>(safeInspection.subjects);
            foreach(var item in room.inspectionSubjects)subjects.Remove(item);
            safeInspection.subjects=subjects.ToArray();
            foreach(var item in room.workspace.GetComponentsInChildren<ItemSceneBinding>(true))itemBindings.Remove(item.instanceId);
            chapterBindings.Remove(ChapterIds.Tape);
            if(CurrentWorld==worlds[0])CurrentWorld=null;
            worlds[0]=null;radioDeck=null;
            room.workspace.SetActive(false);
        }
    }
}
