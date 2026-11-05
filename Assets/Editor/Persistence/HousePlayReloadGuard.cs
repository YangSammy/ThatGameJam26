using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace MemoryWhitebox.EditorTools
{
    // WBMain owns non-serialized chapter/world graphs. Keep that graph alive until
    // Play ends; a new domain then uses normal Start + existing checkpoint restore.
    // This deliberately does not claim to reconstruct a running managed graph.
    [InitializeOnLoad]
    public static class HousePlayReloadGuard
    {
        const string Preference = "ScriptCompilationDuringPlay";
        const string Owned = "MemoryWhitebox.HousePlayReloadGuard.Owned";
        const string Previous = "MemoryWhitebox.HousePlayReloadGuard.Previous";
        const string HadKey = "MemoryWhitebox.HousePlayReloadGuard.HadKey";
        const string Resume = "MemoryWhitebox.HousePlayReloadGuard.ResumeTestSave";
        public static bool Holding => SessionState.GetBool(Owned, false);

        static HousePlayReloadGuard()
        {
            EditorApplication.playModeStateChanged += OnPlayState;
            EditorApplication.update += TryAcquire;
            EditorApplication.quitting += Release;
            CompilationPipeline.compilationStarted += BeforeForcedCompilation;
            if(!EditorApplication.isPlayingOrWillChangePlaymode) Release();
        }

        static void BeforeForcedCompilation(object context)
        {
            // Explicit Refresh/RequestScriptCompilation can bypass the native
            // defer preference. Stop safely before domain teardown in that case.
            if(!Holding || !EditorApplication.isPlaying || WBMain.Instance == null) return;
            var host = WBMain.Instance;
            host.safeInspection?.Close();
            host.radioDeck?.SuspendInput();
            bool saved = host.SaveStableProgress();
            if(saved && GameSavePaths.ResetOnEditorPlay) SessionState.SetBool(Resume, true);
            if(saved) Debug.Log("Forced compilation: saved chapter checkpoint and stopped House Play before reload. Next Play resumes this test save once.");
            else Debug.LogError("Forced compilation: checkpoint save failed: " + host.LastSaveError + ". Existing save files retained; current unsaved progress is not confirmed.");
            EditorApplication.isPlaying = false;
        }

        public static bool ConsumeTestResume()
        {
            bool resume = SessionState.GetBool(Resume, false);
            SessionState.SetBool(Resume, false);
            return resume;
        }

        static void TryAcquire()
        {
            if(Holding || !EditorApplication.isPlaying) return;
            if(Object.FindObjectOfType<WBMain>() == null) return;
            SessionState.SetBool(HadKey, EditorPrefs.HasKey(Preference));
            SessionState.SetInt(Previous, EditorPrefs.GetInt(Preference, 0));
            SessionState.SetBool(Owned, true);
            // Unity 2022.3 PreferencesProvider.ScriptChangesDuringPlayOptions:
            // 1 = RecompileAfterFinishedPlaying. Unlike LockReloadAssemblies,
            // this does not leave managed audio callbacks suspended in compilation.
            EditorPrefs.SetInt(Preference, 1);
        }

        static void OnPlayState(PlayModeStateChange state)
        {
            if(state == PlayModeStateChange.EnteredPlayMode)
            { TryAcquire(); }
            else if(state == PlayModeStateChange.EnteredEditMode)
            { Release(); }
        }

        static void Release()
        {
            if(!Holding) return;
            SessionState.SetBool(Owned, false);
            // Do not overwrite a new explicit user choice made during this Play.
            if(EditorPrefs.GetInt(Preference, 0) != 1) return;
            if(SessionState.GetBool(HadKey, false)) EditorPrefs.SetInt(Preference, SessionState.GetInt(Previous, 0));
            else EditorPrefs.DeleteKey(Preference);
        }
    }
}
