using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MemoryWhitebox.Editor
{
    public static class MainMenuChecks
    {
        static void Check(bool value,string message){if(!value)throw new Exception(message);}
        public static string Run()
        {
            string root=Path.GetFullPath("output/QA/MainMenu_20261105/fixtures-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(root);
            string save=Path.Combine(root,"five-day-progress-v1.json");
            Check(!FiveDayProgress.TryInspectSave(root,false,out _,out _,out _),"Empty slot must disable Continue.");
            File.WriteAllText(save,"not json");var corrupt=File.ReadAllText(save);
            Check(!FiveDayProgress.TryInspectSave(root,false,out _,out _,out var error) && error!="","Corrupt save must not continue.");
            var valid=new FiveDaySave{chapters=Enumerable.Range(0,5).Select(i=>new ChapterSaveRecord{chapterId=ChapterIds.At(i)}).ToArray()};
            File.WriteAllText(save+".bak",JsonUtility.ToJson(valid));
            Check(FiveDayProgress.TryInspectSave(root,false,out var day,out var finished,out _) && day==StoryDay.SpringTape && !finished,"Valid backup must be recognized.");
            Check(File.ReadAllText(save)==corrupt,"Menu must not rewrite the corrupt primary when probing.");
            File.WriteAllText(Path.Combine(root,"study-photo-v1.json"),"photo fixture");
            File.WriteAllBytes(Path.Combine(root,"tape-one-pcm-v1.bin"),new byte[]{1,2,3});
            Directory.CreateDirectory(Path.Combine(root,"ParkFilm","roll"));File.WriteAllText(Path.Combine(root,"ParkFilm","roll","photo.png"),"image fixture");
            Directory.CreateDirectory(Path.Combine(root,"Verification"));File.WriteAllText(Path.Combine(root,"unrelated.txt"),"keep");
            Check(MainMenuSaveAccess.HasProgress(root),"Existing files must request confirmation.");
            string archive=MainMenuSaveAccess.ArchiveForNewGame(root);
            Check(!MainMenuSaveAccess.HasProgress(root),"New game must have no old game data.");
            Check(File.ReadAllText(Path.Combine(archive,"ParkFilm","roll","photo.png"))=="image fixture","Archive must preserve film images.");
            Check(File.ReadAllBytes(Path.Combine(archive,"tape-one-pcm-v1.bin")).SequenceEqual(new byte[]{1,2,3}),"Archive must preserve PCM.");
            Check(File.Exists(Path.Combine(root,"unrelated.txt")) && Directory.Exists(Path.Combine(root,"Verification")),"Unrelated files and QA slots must remain.");
            Check(MainMenuSaveAccess.ArchiveForNewGame(root)==null,"Empty archive call must be idempotent.");
            File.WriteAllText(save,"locked");File.WriteAllText(Path.Combine(root,"study-photo-v1.json"),"must restore");
            // The chapter file moves first; lock the next file to exercise actual rollback.
            using(var locked=new FileStream(Path.Combine(root,"study-photo-v1.json"),FileMode.Open,FileAccess.Read,FileShare.None))
            {
                bool failed=false;try{MainMenuSaveAccess.ArchiveForNewGame(root);}catch(AggregateException){failed=true;}
                Check(failed,"Locked save must abort new game.");
            }
            Check(File.Exists(save) && File.Exists(Path.Combine(root,"study-photo-v1.json")),"Failed archive must keep all progress.");
            var current=SceneManager.GetActiveScene();var scene=EditorSceneManager.OpenScene(MainMenuSaveAccess.ScenePath,OpenSceneMode.Additive);
            try
            {
                var menu=scene.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<MainMenuController>(true)).Single();
                foreach(var field in typeof(MainMenuController).GetFields())
                    if(typeof(UnityEngine.Object).IsAssignableFrom(field.FieldType))Check(field.GetValue(menu)!=null,"Missing menu reference: "+field.Name);
                Check(EditorBuildSettings.scenes.First(x=>x.enabled).path==MainMenuSaveAccess.ScenePath,"Menu must be first build scene.");
                Check(GameBuild.Scenes[0]==MainMenuSaveAccess.ScenePath,"Build script must start in menu.");
                Check(scene.GetRootGameObjects().All(x=>x.GetComponentInChildren<WBMain>(true)==null),"Menu must not initialize gameplay.");
            }
            finally{EditorSceneManager.CloseScene(scene,true);if(current.IsValid())SceneManager.SetActiveScene(current);}
            return "PASS: empty/corrupt/backup save inspection; non-mutating probe; archive/PCM/film isolation; repeat archive; locked-file failure; scene references and build entry. Fixtures: "+root;
        }
    }
}
