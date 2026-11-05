using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace MemoryWhitebox.Editor
{
    public static class WinterSfxChecks
    {
        const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;

        public static string Run()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Run winter SFX checks outside Play Mode.");
            var state = new PaintingChapter();
            var world = new PaintingWorld3D(null, state);
            var cueLog = new List<(int id, bool motion)>();
            var observed = new HashSet<int>();
            Action<int, bool> record = (id, motion) => cueLog.Add((id, motion));
            StorySfx.CueRequested += record;
            try
            {
                var type = typeof(PaintingWorld3D);
                var audioFrame = type.GetNestedType("WinterAudioFrame", BindingFlags.NonPublic);
                void Call(string name, params object[] args) => type.GetMethod(name, Hidden).Invoke(world, args);
                void Set(string name, object value) => type.GetField(name, Hidden).SetValue(world, value);
                void Require(int id, bool motion = false)
                {
                    if (!cueLog.Contains((id, motion))) throw new Exception("Winter SFX missing " + id + " motion=" + motion);
                    observed.Add(id);
                }
                void RequireSilent()
                {
                    if (cueLog.Count != 0) throw new Exception("Unexpected winter cue " + cueLog[0].id);
                }
                void Step(float dt, params KeyCode[] keys)
                {
                    var before = Activator.CreateInstance(audioFrame, state);
                    var input = new WBInput();
                    foreach (var key in keys) input.Keys.Add(key);
                    state.Tick(dt, input);
                    Call("SyncWinterActionSounds", before);
                }
                void Restore(int phase, bool high = false, bool key = false, float crate = 2)
                {
                    var shapes = phase == 0 ? "[true,true,false,false,false]" : "[true,true,true,true,true]";
                    state.RestoreCheckpoint("{\"phase\":" + phase + ",\"shapes\":" + shapes +
                        ",\"high\":" + (high ? "true" : "false") + ",\"key\":" + (key ? "true" : "false") +
                        ",\"crate\":" + crate.ToString(System.Globalization.CultureInfo.InvariantCulture) + "}");
                    world.CancelPointerGesture();
                }

                var camera = new GameObject("Winter cue test camera").AddComponent<Camera>();
                camera.transform.SetParent(world.Root.transform, false);
                camera.transform.SetPositionAndRotation(new Vector3(3000, 0, -5), Quaternion.identity);
                var defocus = camera.gameObject.AddComponent<PaintingDefocus>();
                type.GetProperty("Defocus").SetValue(world, defocus);
                defocus.Begin();
                world.Press("shape-swing", default, 0);
                Require(37);
                float blur = defocus.Amount;
                defocus.SetFocusHeld(true, 1);
                Call("SyncWinterFocusSounds", 0, blur);
                Require(137);
                if (!state.Observe(0, true, 10)) throw new Exception("Shape observation did not complete");
                Call("ShapeRecorded", 0);
                Require(39);
                world.Release("", default);
                Call("TurnPageForInput"); Require(38);
                Call("TurnPageForInput"); Require(138);

                cueLog.Clear(); Restore(0); RequireSilent();
                state.SetBook(true);
                Step(.02f, KeyCode.D); Require(40, true);
                Step(.02f, KeyCode.Space); Require(40);
                for (int i = 0; i < 12; i++) Step(.1f);
                Require(140);
                state.Actor = new Vector2(4, -3.1f); state.Grounded = false;
                Step(.02f); Require(240);
                state.Actor = state.SwingSeat;
                Step(.02f); Require(41);
                Step(.02f, KeyCode.D); Require(141, true);
                state.Riding = -1; state.Actor = new Vector2(9.2f, .7f); state.Grounded = true;
                state.BridgeGrowth = .99f;
                Step(.02f, KeyCode.D); Require(42); Require(142, true);

                cueLog.Clear(); Restore(1, crate: 3.63f); RequireSilent();
                state.SetBook(true); state.Actor = new Vector2(3.03f, 0);
                Step(.02f, KeyCode.D); Require(43, true); Require(44);
                state.Actor = state.FlowerSeat(0);
                Step(.02f); Require(45);
                Step(.02f); Require(145, true);
                state.Riding = -1; state.Actor = new Vector2(10.8f, 3.23f);
                state.Grounded = false; state.Velocity = new Vector2(0, -2);
                Step(.02f); Require(245);

                state.SetBook(false); state.Painted[4] = false;
                world.Press("shape-pendant", default, 0);
                Call("SyncWinterFocusSounds", 4, 0f); Require(46);
                if (!state.Observe(4, true, 10)) throw new Exception("Pendant observation did not complete");
                Call("ShapeRecorded", 4); Require(146);
                world.Release("", default);
                state.SetBook(true); state.Actor = new Vector2(14.6f, 3.2f);

                var page = GameObject.CreatePrimitive(PrimitiveType.Quad);
                page.transform.SetParent(world.Root.transform);
                page.transform.position = new Vector3(3000, 0, 2);
                var keyObject = new GameObject("Winter cue test key");
                keyObject.transform.SetParent(world.Root.transform);
                var hole = new GameObject("Winter cue test socket");
                hole.transform.SetParent(world.Root.transform);
                hole.transform.position = new Vector3(3000, 0, 1);
                Set("pageCollider", page.GetComponent<Collider>());
                Set("albumCamera", camera);
                Set("key", keyObject.transform);
                Set("hole", hole.transform);
                Physics.SyncTransforms();
                var ray = new Ray(camera.transform.position, Vector3.forward);
                world.Press("album-key", ray, 0); Require(19);
                cueLog.Clear(); world.Release("", new Ray(camera.transform.position, Vector3.up)); RequireSilent();
                world.Press("album-key", ray, 0);
                world.Release("key-recess", ray); Require(47); Require(48);
                state.Actor = new Vector2(15.6f, 3.2f); state.Grounded = true;
                Step(.02f); Require(148);
                if (cueLog.Contains((49, false))) throw new Exception("Ward cue played before the white transition ended.");
                cueLog.Clear();
                Step(2f); Require(49);
                Step(.02f, KeyCode.D); Require(249, true);
                state.Actor.x = state.WardWalkDistance - .01f;
                Step(.02f, KeyCode.D); Require(149);

                return "PASS " + observed.Count + " winter cue IDs through model, focus, album and pendant transitions. " +
                    "Also checked silent checkpoint restore and missed pendant drop. Edit Mode cue requests only.";
            }
            finally
            {
                StorySfx.CueRequested -= record;
                world.Dispose();
            }
        }
    }
}
