using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MemoryWhitebox.Editor
{
    public static class TapeStorageToggleAuthoring
    {
        public static string Apply()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play first.");
            var scene=SceneManager.GetSceneByPath(SpringTapeRoom.ScenePath);
            if(!scene.isLoaded)scene=EditorSceneManager.OpenScene(SpringTapeRoom.ScenePath,OpenSceneMode.Additive);
            if(scene.isDirty)throw new InvalidOperationException("Spring scene has unsaved edits; authoring stopped.");
            var deck=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<HouseRadioDeck>(true)).Single();
            if(deck.storageCloseView!=null)return "Already authored; Inspector positions retained.";
            string backup="output/QA/TapeToggleStorage_20261105/Bedroom_Spring.before.unity";
            Directory.CreateDirectory(Path.GetDirectoryName(backup));
            if(!File.Exists(backup))File.Copy(SpringTapeRoom.ScenePath,backup);
            var w=deck.workbenchVisuals;
            var anchors=new GameObject("StorageDeskAnchors").transform;anchors.SetParent(deck.operationView.transform.parent,false);
            deck.storageDeskLandings=new Transform[6];
            for(int i=0;i<6;i++)
            {
                var tape=i<3?deck.originals[i].tape:deck.tapes[i-3].root;
                var landing=new GameObject(i<3?"SourceDesk_"+(i+1):"WorkTapeDesk_"+(i-2)).transform;
                landing.SetParent(anchors,false);landing.SetPositionAndRotation(tape.position,tape.rotation);deck.storageDeskLandings[i]=landing;
            }
            var thirdSlot=new GameObject("A01_Slot_Candidate03").transform;
            thirdSlot.SetParent(w.slots[4].parent,false);
            thirdSlot.SetPositionAndRotation(Vector3.Lerp(w.slots[4].position,w.slots[5].position,.5f),w.slots[4].rotation);
            for(int i=0;i<6;i++)
            {
                var tape=i<3?deck.originals[i].tape:deck.tapes[i-3].root;
                var slot=i==5?thirdSlot:w.slots[i];
                tape.SetPositionAndRotation(slot.position,Quaternion.Euler(0,270,0));
                var visual=i<3?w.sources[i]:w.intact[i-3];
                var renderers=visual.GetComponentsInChildren<Renderer>(true);
                var bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);
                tape.position+=Vector3.up*(slot.position.y-bounds.min.y+.001f);
                if(i<3)deck.originals[i].home.SetPositionAndRotation(tape.position,tape.rotation);
            }
            var close=new GameObject("StorageCloseView").transform;close.SetParent(anchors,false);
            var center=w.storage.transform.position+Vector3.up*.06f;
            close.position=center+new Vector3(.33f,.52f,.025f);
            close.rotation=Quaternion.LookRotation(center-close.position,Vector3.up);
            deck.storageCloseView=close.gameObject.AddComponent<SceneViewAnchor>();deck.storageCloseView.fieldOfView=40;
            deck.transportPanel.panel.SetActive(false);
            foreach(var button in deck.transportPanel.buttons)if(button!=null)button.gameObject.SetActive(false);
            foreach(var button in deck.transportPanel.sourceButtons)if(button!=null)button.gameObject.SetActive(false);
            foreach(var button in new[]{deck.transportPanel.prepareAction,deck.transportPanel.listenAction,deck.transportPanel.submitAction})if(button!=null)button.SetActive(false);
            EditorUtility.SetDirty(deck);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            return "Spring storage authored: 6 existing tape instances, 6 desk landings, third candidate slot and close-view anchor. Backup: "+backup;
        }
    }
}
