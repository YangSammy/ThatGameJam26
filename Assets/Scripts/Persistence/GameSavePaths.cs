using System.IO;
using UnityEngine;

namespace MemoryWhitebox
{
    // Both photography systems resolve their saves here. Editor play sessions
    // use a separate slot; standalone players always use their persistent slot.
    public static class GameSavePaths
    {
        // Keep the installed save identity when the display/product title changes.
        public static string PersistentRoot
        {
            get
            {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
                return Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile),"AppData","LocalLow","Memory Prototype","Memory Whitebox");
#else
                return Application.persistentDataPath;
#endif
            }
        }
#if DEVELOPMENT_BUILD && !UNITY_EDITOR
        // A standalone startup regression can exercise a copy of an old save.
        // Only a GUID-named slot below Verification can be selected.
        static readonly string verificationRoot=GetVerificationRoot();
        static string GetVerificationRoot()
        {
            var args=System.Environment.GetCommandLineArgs();
            int index=System.Array.IndexOf(args,"-qa-boot-slot");
            if(index<0)return null;
            if(index+1>=args.Length || !System.Guid.TryParseExact(args[index+1],"N",out var id))
                throw new System.ArgumentException("Expected a GUID after -qa-boot-slot.");
            return Path.Combine(PersistentRoot,"Verification",id.ToString("N"));
        }
#endif
#if UNITY_EDITOR
        public static string ResetPreferenceKey=>"MemoryWhitebox.ResetSavesOnPlay."+Application.dataPath;
        public static bool ResetOnEditorPlay=>UnityEditor.EditorPrefs.GetBool(ResetPreferenceKey,true);
        public static string EditorSessionRoot=>Path.Combine(PersistentRoot,"EditorPlaySession");
#endif
        public static string Root
        {
            get
            {
#if UNITY_EDITOR
                return ResetOnEditorPlay?EditorSessionRoot:Path.Combine(PersistentRoot,"EditorPersistentSession");
#elif DEVELOPMENT_BUILD
                return verificationRoot??PersistentRoot;
#else
                return PersistentRoot;
#endif
            }
        }
    }
}
