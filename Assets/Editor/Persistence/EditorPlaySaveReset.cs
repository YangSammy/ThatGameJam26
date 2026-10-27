using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace MemoryWhitebox.EditorTools
{
    [InitializeOnLoad]
    public static class EditorPlaySaveReset
    {
        const string Menu="Tools/存档/每次 Play 重置测试存档";
        static EditorPlaySaveReset()
        {
            EditorApplication.playModeStateChanged-=OnPlayModeChanged;
            EditorApplication.playModeStateChanged+=OnPlayModeChanged;
        }
        [MenuItem(Menu)]
        static void Toggle()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)return;
            EditorPrefs.SetBool(GameSavePaths.ResetPreferenceKey,!GameSavePaths.ResetOnEditorPlay);
            UnityEditor.Menu.SetChecked(Menu,GameSavePaths.ResetOnEditorPlay);
            Debug.Log(GameSavePaths.ResetOnEditorPlay?
                "已启用：每次 Play 使用全新的测试存档，正式存档保留。":
                "已关闭：下次 Play 读取正常持久化存档，不再重置。测试存档不会合并到正式存档。");
        }
        [MenuItem(Menu,true)]
        static bool ValidateToggle()
        {
            UnityEditor.Menu.SetChecked(Menu,GameSavePaths.ResetOnEditorPlay);
            return !EditorApplication.isPlayingOrWillChangePlaymode;
        }
        static void OnPlayModeChanged(PlayModeStateChange state)
        {
            // This event runs before scene Awake/Start, including when Domain
            // Reload is disabled. Scene loads and script reloads do not reset saves.
            if(state!=PlayModeStateChange.ExitingEditMode || !GameSavePaths.ResetOnEditorPlay)return;
            try
            {
                string parent=Path.GetFullPath(Application.persistentDataPath).TrimEnd(Path.DirectorySeparatorChar,Path.AltDirectorySeparatorChar);
                string target=Path.GetFullPath(GameSavePaths.EditorSessionRoot).TrimEnd(Path.DirectorySeparatorChar,Path.AltDirectorySeparatorChar);
                if(!string.Equals(Path.GetDirectoryName(target),parent,StringComparison.OrdinalIgnoreCase) ||
                   !string.Equals(Path.GetFileName(target),"EditorPlaySession",StringComparison.Ordinal))
                    throw new IOException("测试存档目录不在预期位置，未执行清理。");
                if(Directory.Exists(target))
                {
                    RejectLinkedPaths(target);
                    Directory.Delete(target,true);
                }
                Directory.CreateDirectory(target);
                Debug.Log("已重置本次 Play 的摄影测试存档（装卷、胶卷、暗室进度和浏览页码）。正常存档未改动。");
            }
            catch(Exception e)
            {
                // Do not start with stale progress after promising a fresh session.
                EditorApplication.isPlaying=false;
                Debug.LogError("测试存档重置失败，已取消进入 Play："+e.Message);
            }
        }
        static void RejectLinkedPaths(string path)
        {
            var attributes=File.GetAttributes(path);
            if((attributes & FileAttributes.ReparsePoint)!=0)throw new IOException("测试目录含链接，未执行清理："+path);
            if((attributes & FileAttributes.Directory)==0)return;
            foreach(string child in Directory.GetFileSystemEntries(path))RejectLinkedPaths(child);
        }
    }
}
