using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace MemoryWhitebox.Editor
{
    public static class PriorityRegressionChecks
    {
        [Serializable] class Report { public string utc; public int passed, failed; public string[] checks; }
        [MenuItem("Memory Whitebox/Run Priority Regression Checks")]
        public static void Run()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Run these isolated checks outside Play Mode.");
            var checks = new List<string>(); int passed = 0, failed = 0;
            bool oldQA = WBMain.IsQA; WBMain oldHost = WBMain.Instance;
            try
            {
                WBMain.IsQA = true; WBMain.Instance = null;
                RunCase("Slow continuous tank motion", () => Shake(.01f, 20, true), checks, ref passed, ref failed);
                RunCase("Stationary tank does not agitate", () => Shake(0, 20, false), checks, ref passed, ref failed);
                RunCase("Fast tank motion still works", () => Shake(.2f, 1, true), checks, ref passed, ref failed);
                RunCase("Held camera cannot walk into table", Collision, checks, ref passed, ref failed);
                RunCase("Help cancels held interaction", Help, checks, ref passed, ref failed);
            }
            finally { WBMain.IsQA = oldQA; WBMain.Instance = oldHost; }
            Directory.CreateDirectory("QA");
            File.WriteAllText("QA/priority-regression-checks.json", JsonUtility.ToJson(new Report { utc = DateTime.UtcNow.ToString("O"), passed = passed, failed = failed, checks = checks.ToArray() }, true));
            Debug.Log($"PRIORITY_REGRESSION_RESULTS passed={passed} failed={failed}");
        }
        static void RunCase(string name, Action test, List<string> results, ref int passed, ref int failed)
        {
            try { test(); passed++; results.Add("PASS: " + name); }
            catch (Exception ex) { failed++; results.Add("FAIL: " + name + " - " + ex.GetBaseException().Message); }
        }
        static void Require(bool value, string message) { if (!value) throw new Exception(message); }
        static Ray At(WorldChapter3D world, Vector3 local) => new Ray(world.World(local) + Vector3.up * 2, Vector3.down);
        static void Shake(float step, int count, bool expected)
        {
            var model = new PhotoChapter { Phase = PhotoChapter.FilmPhase.Processing, ProcessingTime = 3 };
            var world = new PhotoWorld3D(null, model);
            try
            {
                Vector3 tank = world.Nodes["tank"].transform.position - world.Origin; tank.y = 1.2f;
                world.Press("tank", At(world, tank), 0);
                for (int i = 1; i <= count; i++) world.Move(At(world, tank + Vector3.right * step * i));
                Require(model.Agitated == expected, "Agitated=" + model.Agitated);
                world.Release("", At(world, tank));
                model.Agitated = false;
                world.Press("tank", At(world, tank), 0); world.Move(At(world, tank));
                Require(!model.Agitated, "A fresh hold must not inherit prior movement.");
            }
            finally { world.Dispose(); }
        }
        static void Collision()
        {
            var world = new PhotoWorld3D(null, new PhotoChapter());
            try
            {
                typeof(PhotoWorld3D).GetField("<CameraHeld>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(world, true);
                world.QASetActor(new Vector3(0, 1.5f, -2.23f));
                var input = new WBInput(); input.Keys.Add(KeyCode.S);
                world.Tick(.05f, input);
                Require(world.Eye.z - world.Origin.z >= -2.25f, "Player entered the tabletop footprint.");
                world.QASetActor(new Vector3(-2, 1.5f, -1.8f));
                for (int i = 0; i < 40; i++) world.Tick(.02f, input);
                Require(world.Eye.z - world.Origin.z < -2.8f, "Clear aisle should remain walkable.");
            }
            finally { world.Dispose(); }
        }
        static void Help()
        {
            var go = new GameObject("Priority check host"); go.SetActive(false);
            var host = go.AddComponent<WBMain>(); var world = new PhotoWorld3D(null, new PhotoChapter());
            try
            {
                typeof(WBMain).GetField("<CurrentWorld>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(host, world);
                world.Press("film-200", At(world, Vector3.zero), 0);
                Require(world.DraggedId!=null, "Setup must hold film.");
                var method = typeof(WBMain).GetMethod("SetHelpVisible");
                Require(method != null, "Help transition has no cancellable input boundary.");
                method.Invoke(host, new object[] { true });
                Require(world.DraggedId==null, "Help left the interaction held.");
                method.Invoke(host, new object[] { false });
                Require(world.DraggedId==null, "Closing help revived the interaction.");
            }
            finally { world.Dispose(); UnityEngine.Object.DestroyImmediate(go); }
        }
    }
}
