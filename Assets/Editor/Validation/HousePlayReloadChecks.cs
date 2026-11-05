using System;
using UnityEditor;
using UnityEngine;

namespace MemoryWhitebox.EditorTools
{
    // Capture before editing a real script and refreshing assets in Play.
    // Do not force RequestScriptCompilation: that bypasses Unity's preference.
    public static class HousePlayReloadChecks
    {
        public static int ProbeRevision => 3;
        static WBMain host;
        static HouseRadioDeck deck;
        static RadioTapeChapter chapter;
        static TapeSampleTransport transport;
        static int sources;
        static double requestedAt;
        static float sourceSeconds;

        public static string Capture()
        {
            if(!Application.isPlaying || !GameSavePaths.ResetOnEditorPlay ||
               !HousePlayReloadGuard.Holding || WBMain.Instance == null || WBMain.Instance.radioDeck == null)
                throw new InvalidOperationException("An isolated spring House Play session is required.");
            host = WBMain.Instance; deck = host.radioDeck; chapter = deck.Chapter; transport = deck.transport;
            sources = deck.GetComponentsInChildren<AudioSource>(true).Length;
            requestedAt = EditorApplication.timeSinceStartup;
            sourceSeconds = transport.SourceSeconds;
            return "Captured references and audio cursor. Edit a test script and Refresh; then Check while A is playing.";
        }

        public static string Check()
        {
            if(host == null || EditorApplication.timeSinceStartup - requestedAt < 2)
                throw new InvalidOperationException("Request a compilation and allow editor updates before checking.");
            if(!HousePlayReloadGuard.Holding || WBMain.Instance != host || host.radioDeck != deck ||
               !ReferenceEquals(chapter, deck.Chapter) || transport != deck.transport || !host.HouseReady)
                throw new InvalidOperationException("Live House references were replaced or lost.");
            if(deck.GetComponentsInChildren<AudioSource>(true).Length != sources)
                throw new InvalidOperationException("Voice pool changed during compilation.");
            if(EditorApplication.isCompiling || transport.SourceSeconds <= sourceSeconds + .25f)
                throw new InvalidOperationException("Audio did not advance or compiler blocked the running session.");
            return "PASS: same host, chapter, deck, transport and voice count; HouseReady preserved; audio advanced from " + sourceSeconds + " to " + transport.SourceSeconds + ". Verify edited script loads after Stop and original preference is restored.";
        }
    }
}
