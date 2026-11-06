using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace MemoryWhitebox.EditorTools
{
    public static class BedroomWindowAmbienceAuthoring
    {
        public static string Apply(int season)
        {
            if(Application.isPlaying)throw new InvalidOperationException("Edit mode required.");
            for(int i=0;i<SceneManager.sceneCount;i++)
                if(SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Unsaved scene preserved; save it before authoring.");
            var names=new[]{"Spring","Summer","Autumn","Winter"};
            if(season<0 || season>=names.Length)throw new ArgumentOutOfRangeException(nameof(season));
            string path="Assets/Scenes/Bedroom/V17/Bedroom_"+names[season]+".unity";
            const string backups="output/QA/WindowAmbience_20261106";
            Directory.CreateDirectory(backups);
            string backup=Path.Combine(backups,"Bedroom_"+names[season]+".before.unity");
            if(!File.Exists(backup))File.Copy(path,backup);
            var scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Single);
            var windows=scene.GetRootGameObjects().Where(g=>g.name.StartsWith("Bedroom_"))
                .SelectMany(g=>g.GetComponentsInChildren<Renderer>(true))
                .Where(r=>r.name.StartsWith("Window_glass_")).ToArray();
            if(windows.Length!=2)throw new InvalidOperationException("Expected two authored bedroom window panes, got "+windows.Length);
            var anchors=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<BedroomWindowAmbience>(true)).ToArray();
            if(anchors.Length>1)throw new InvalidOperationException("Duplicate acoustic anchors.");
            var anchor=anchors.FirstOrDefault();
            if(anchor==null)
            {
                var root=scene.GetRootGameObjects().FirstOrDefault(g=>g.name=="Audio") ?? new GameObject("Audio");
                var obj=new GameObject("WindowAmbience");obj.transform.SetParent(root.transform,false);
                var bounds=windows[0].bounds;bounds.Encapsulate(windows[1].bounds);
                obj.transform.position=bounds.center;
                anchor=obj.AddComponent<BedroomWindowAmbience>();
            }
            anchor.season=season;
            EditorUtility.SetDirty(anchor);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            return scene.name+": Audio/WindowAmbience at "+anchor.transform.position+"; panes="+windows.Length+"; range="+anchor.nearDistance+".."+anchor.farDistance;
        }
    }
}
