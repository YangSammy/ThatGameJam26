using System.IO;
using UnityEngine;

namespace MemoryWhitebox
{
    // Both photography systems resolve their saves here. Editor play sessions
    // use a separate slot; standalone players always use their persistent slot.
    public static class GameSavePaths
    {
#if UNITY_EDITOR
        public static string ResetPreferenceKey=>"MemoryWhitebox.ResetSavesOnPlay."+Application.dataPath;
        public static bool ResetOnEditorPlay=>UnityEditor.EditorPrefs.GetBool(ResetPreferenceKey,true);
        public static string EditorSessionRoot=>Path.Combine(Application.persistentDataPath,"EditorPlaySession");
#endif
        public static string Root
        {
            get
            {
#if UNITY_EDITOR
                return ResetOnEditorPlay?EditorSessionRoot:Path.Combine(Application.persistentDataPath,"EditorPersistentSession");
#else
                return Application.persistentDataPath;
#endif
            }
        }
    }
}
