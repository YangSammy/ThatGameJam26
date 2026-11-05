using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace MemoryWhitebox
{
    public static class MainMenuSaveAccess
    {
        public const string ScenePath="Assets/Scenes/MainMenuScene.unity";
        public static bool LaunchedFromMenu {get;set;}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset()=>LaunchedFromMenu=false;
        static bool OwnedFile(string name)=>name.StartsWith("five-day-progress-v1.json",StringComparison.Ordinal) ||
            name.StartsWith("five-day-development-v1.json",StringComparison.Ordinal) ||
            name.StartsWith("study-photo-v1.json",StringComparison.Ordinal) ||
            name.StartsWith("tape-one-pcm-v1.bin",StringComparison.Ordinal) ||
            name.StartsWith("tape-commission-",StringComparison.Ordinal);
        public static bool HasProgress(string root)=>Directory.Exists(root) &&
            (Directory.GetFiles(root).Any(p=>OwnedFile(Path.GetFileName(p))) || Directory.Exists(Path.Combine(root,"ParkFilm")));
        static void RejectLinks(string path)
        {
            var flags=File.GetAttributes(path);
            if((flags&FileAttributes.ReparsePoint)!=0)throw new IOException("Linked save paths cannot be archived.");
            if((flags&FileAttributes.Directory)!=0)foreach(var child in Directory.GetFileSystemEntries(path))RejectLinks(child);
        }
        // Moves only known game-owned data, never the root, preferences or verification slots.
        // A partial failure rolls back every successful move before reporting an error.
        public static string ArchiveForNewGame(string root)
        {
            root=Path.GetFullPath(root);if(!Directory.Exists(root))return null;
            if((File.GetAttributes(root)&FileAttributes.ReparsePoint)!=0)throw new IOException("Linked save root.");
            var entries=Directory.GetFiles(root).Where(p=>OwnedFile(Path.GetFileName(p))).OrderBy(p=>p,StringComparer.Ordinal).ToList();
            string film=Path.Combine(root,"ParkFilm");if(Directory.Exists(film))entries.Add(film);
            if(entries.Count==0)return null;
            foreach(var path in entries)RejectLinks(path);
            string archiveRoot=Path.Combine(root,"ArchivedGames");
            if(Directory.Exists(archiveRoot) && (File.GetAttributes(archiveRoot)&FileAttributes.ReparsePoint)!=0)throw new IOException("Linked archive directory.");
            string archive=Path.Combine(archiveRoot,DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")+"-"+Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(archive);
            var moved=new List<string>();
            try
            {
                foreach(var source in entries){Move(source,Path.Combine(archive,Path.GetFileName(source)));moved.Add(source);}
            }
            catch(Exception failure)
            {
                var errors=new List<Exception>{failure};
                for(int i=moved.Count-1;i>=0;i--)try{Move(Path.Combine(archive,Path.GetFileName(moved[i])),moved[i]);}catch(Exception rollback){errors.Add(rollback);}
                throw new AggregateException("Could not archive progress. Backup: "+archive,errors);
            }
            return archive;
        }
        static void Move(string from,string to){if(Directory.Exists(from))Directory.Move(from,to);else File.Move(from,to);}
    }
}
