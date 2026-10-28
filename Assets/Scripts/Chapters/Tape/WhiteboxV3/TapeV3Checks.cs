using System;
using System.Collections.Generic;
using UnityEngine;

namespace MemoryWhitebox.V3
{
    public static class TapeV3Checks
    {
        public static Action<string, WorldChapter3D> CaptureStage;
        static void Check(List<string> result, bool passed, string description)
        { if (!passed) throw new InvalidOperationException("Tape v3: " + description); result.Add(description); }
        static string TakeDetails(TapeChapter model)
        {
            var a = model.AssessTake();
            return string.Format(System.Globalization.CultureInfo.InvariantCulture,
                " [duration={0:F6}, a={1}, b={2}, frames={3}, passed={4}, quality={5:F6}, retained={6:F6}, noiseSeconds={7:F6}, invalidSeconds={8:F6}]",
                model.RecordedDuration, model.A.Frame, model.B.Frame, model.RecordedFrames, a.Passed, a.Quality, a.RetainedFraction, a.NoiseSeconds, a.InvalidSeconds);
        }
        static WBInput Input(bool record = false, bool play = false, bool rew = false, bool ff = false, bool stop = false)
        {
            var i = new WBInput();
            if (record) i.Keys.Add(KeyCode.S); if (play) i.Keys.Add(KeyCode.F);
            if (rew) i.Keys.Add(KeyCode.J); if (ff) i.Keys.Add(KeyCode.L); if (stop) i.Keys.Add(KeyCode.Quote);
            return i;
        }
        public static void PrepareGoodBlank(TapeChapter model)
        {
            model.SelectCandidate(2);
            for (int i = 0; i < 24; i++) model.InspectReel(0, 3);
            for (int i = 0; i < 24; i++) model.InspectReel(1, 3);
        }
        public static void ApplyBothPatches(TapeChapter model)
        {
            for (int side = 0; side < 2; side++)
            { model.BeginTapePull(); model.MoveTapePull(.24f); model.CompleteTapePull(); model.PatchProtectHole(side); }
        }
        public static TapeChapter Prepared()
        {
            var model = new TapeChapter(); PrepareGoodBlank(model); ApplyBothPatches(model);
            model.LoadSource(); model.LoadTarget(); return model;
        }
        public static void RecordCleanWithOracleForQA(TapeChapter model)
        {
            // Test driver only: the game's controls never query these content markers.
            model.SetSourceTransport(1);
            int guard = 0;
            while (model.A.Frame < model.Programme.Source.Length && guard++ < 1800)
            {
                bool noise = TapePcm.IsNoiseFrameForQA(model.Programme, model.A.Frame);
                model.Tick(.01f, noise ? Input(stop: true) : Input(record: true, play: true));
            }
            model.Tick(0, Input());
        }

        public static string[] Run()
        {
            var result = new List<string>();
            var inspection = new TapeChapter();
            Check(result, Mathf.Approximately(inspection.Candidates[0].Position, TapeChapter.Duration / 2), "Candidates start midway with tape on both reels");
            inspection.SelectCandidate(0);
            for (int i = 0; i < 12; i++) inspection.InspectReel(0, 3);
            Check(result, inspection.Candidates[0].Broken && !inspection.SourceVerified, "Left reel reveals a break and rejects the candidate");
            inspection.SelectCandidate(1);
            for (int i = 0; i < 12; i++) inspection.InspectReel(1, 3);
            Check(result, inspection.Candidates[1].Broken && !inspection.SourceVerified, "Right reel reveals another break and requires replacement");
            inspection.SelectCandidate(2);
            for (int i = 0; i < 24; i++) inspection.InspectReel(1, 3);
            Check(result, !inspection.SourceVerified, "Inspecting only one direction cannot verify the full tape");
            for (int i = 0; i < 24; i++) inspection.InspectReel(0, 3);
            Check(result, inspection.SourceVerified && inspection.Candidates[2].Scanned == TapeChapter.Samples, "Both ends and the whole tape interval are covered");
            Check(result, !inspection.LoadTarget(), "Inspection alone does not bypass record-protect preparation");
            inspection.BeginTapePull(); inspection.MoveTapePull(.05f); inspection.CompleteTapePull();
            Check(result, !inspection.TapePieceReady && !inspection.PatchProtectHole(0), "A short click does not tear off adhesive");
            inspection.BeginTapePull(); inspection.MoveTapePull(.23f); inspection.CompleteTapePull(); inspection.PatchProtectHole(0);
            Check(result, !inspection.LoadTarget(), "Both record-protect holes must be covered");
            inspection.BeginTapePull(); inspection.MoveTapePull(.23f); inspection.CompleteTapePull(); inspection.PatchProtectHole(1);
            Check(result, inspection.LoadTarget() && inspection.LoadSource(), "Prepared destination loads right and commission source loads left");

            var one = Prepared(); one.SetSourceTransport(1); one.Tick(.1f, Input(record: true));
            Check(result, one.A.Frame > 0 && one.B.Frame == 0 && one.RecordedFrames == 0, "S alone never advances or writes destination B");
            var independent = Prepared(); independent.SetSourceTransport(1); independent.Tick(.1f, Input(record: true, play: true));
            int oldA = independent.A.Frame, oldB = independent.B.Frame;
            independent.Tick(.1f, Input(record: true, play: true, stop: true));
            Check(result, independent.A.Frame > oldA && independent.B.Frame == oldB, "Stop freezes only B while source A keeps playing");
            independent.Tick(.1f, Input(record: true, play: true));
            Check(result, independent.B.Frame == oldB, "A held F cannot automatically restart after STOP");
            independent.Tick(.01f, Input()); independent.Tick(.1f, Input(record: true, play: true));
            Check(result, independent.B.Frame > oldB && independent.A.Frame > oldA * 2, "Fresh F plus S resumes at B head without resetting A");
            independent.Tick(.1f, Input(play: true));
            Check(result, independent.B.Play && !independent.B.Armed && independent.RecordedSourceFrames[independent.B.Frame - 1] == -1,
                "Releasing S with F held advances B without writing and leaves an actual gap");
            var seek = Prepared(); seek.B.Position = 2;
            int seekStart = seek.B.Frame;
            seek.Tick(.1f, Input(record: true, play: true, rew: true, ff: true));
            Check(result, seek.B.Frame == seekStart && seek.RecordedFrames == 0, "Simultaneous rewind and fast-forward produce no movement or recording");
            seek.Tick(.1f, Input(rew: true));
            Check(result, seek.B.Frame < seekStart && seek.RecordedFrames == 0, "J rewinds B independently without recording");

            var uncut = Prepared(); uncut.SetSourceTransport(1);
            for (int i = 0; i < 122; i++) uncut.Tick(.1f, Input(record: true, play: true));
            uncut.Tick(0, Input());
            Check(result, !uncut.Submit() && uncut.AssessTake().NoiseSeconds > 1.8f, "Recording the whole source preserves audible noise and fails acceptance");
            var silent = Prepared();
            for (int i = 0; i < 10; i++) silent.Tick(.1f, Input(record: true, play: true));
            Check(result, silent.RecordedFrames > 0 && silent.AssessTake().InvalidSeconds > .9f, "Recording without running A writes real silence and fails");
            var manualStop = Prepared(); manualStop.SetSourceTransport(1); manualStop.Tick(.1f, Input(record: true, play: true));
            int beforeManualStop = manualStop.RecordedFrames;
            manualStop.SetSourceTransport(0); manualStop.Tick(.1f, Input(record: true, play: true));
            Check(result, manualStop.B.Play && manualStop.RecordedFrames > beforeManualStop && manualStop.AssessTake().InvalidSeconds > .09f && !manualStop.AssessTake().Passed,
                "Stopping A midway leaves B recording real silence and never invokes source-end auto-stop" + TakeDetails(manualStop));
            var tail = Prepared(); tail.A.Frame = TapePcm.FrameCount - 13; tail.SetSourceTransport(1);
            tail.Tick(.037f, Input(record: true, play: true));
            Check(result, tail.A.Frame == TapePcm.FrameCount && tail.RecordedFrames == 13 && !tail.B.Play && tail.AssessTake().InvalidSeconds == 0,
                "Natural A tape end inside one input tick records only remaining source samples and interlocks B transport" + TakeDetails(tail));

            var complete = Prepared(); RecordCleanWithOracleForQA(complete);
            Check(result, complete.AssessTake().Passed && complete.RecordedDuration < TapeChapter.Duration - 1.6f,
                "Manually held cuts concatenate actual PCM into a shorter programme" + TakeDetails(complete));
            bool realPcm = true;
            for (int i = 0; i < complete.RecordedFrames; i++)
            {
                int from = complete.RecordedSourceFrames[i];
                if (from < 0 || complete.RecordedPcm[i] != complete.Programme.Source[from]) { realPcm = false; break; }
            }
            Check(result, realPcm, "Every recorded sample matches the source sample actually heard at write time");
            Check(result, complete.Submit() && complete.DeliveredCount == 1 && !complete.Completed, "First ordinary music commission advances to the second");
            PrepareGoodBlank(complete); ApplyBothPatches(complete); complete.LoadSource(); complete.LoadTarget(); RecordCleanWithOracleForQA(complete);
            Check(result, complete.Submit() && complete.DeliveredCount == 2 && complete.CommissionIndex == 2, "Second ordinary commission unlocks the birthday source");
            PrepareGoodBlank(complete); ApplyBothPatches(complete); complete.LoadSource(); complete.LoadTarget(); RecordCleanWithOracleForQA(complete);
            Check(result, complete.Submit() && complete.DeliveredCount == 3 && complete.Completed, "Third narrative commission completes the spring chapter");

            TapeWorld3D world = null;
            try
            {
                var spatial = new TapeChapter(); world = new TapeWorld3D(null, spatial); spatial.SelectCandidate(2); world.Tick(0, Input());
                world.Activate();
                CaptureStage?.Invoke("tape-v3-inspection", world);
                var node = world.Nodes["tape.spindle.2.0"];
                Physics.SyncTransforms();
                var ray = new Ray(node.transform.position + Vector3.up * 2, Vector3.down);
                float before = spatial.InspectPosition;
                world.Scroll(world.Pick(ray), 1, ray);
                Check(result, Mathf.Approximately(before, spatial.InspectPosition), "3D wheel input does nothing before a physical reel is held");
                string hit = world.Pick(ray); world.Press(hit, ray, 0); world.Scroll(hit, 1, ray); world.Release(hit, ray);
                Check(result, hit == "tape.spindle.2.0" && spatial.InspectPosition < before, "Collider raycast on the actual left reel selects its winding direction");
                spatial.SelectCandidate(0);
                for (int i = 0; i < 12; i++) spatial.InspectReel(0, 3);
                world.Tick(0, Input());
                bool redBreak = false;
                foreach (Renderer renderer in world.Root.GetComponentsInChildren<Renderer>())
                    if (renderer.name.StartsWith("Red break") && renderer.sharedMaterial.color.r > renderer.sharedMaterial.color.g * 2)
                        redBreak = true;
                Check(result, redBreak, "Broken cassette renders physical red detached tape ends");
                CaptureStage?.Invoke("tape-v3-red-break", world);
                PrepareGoodBlank(spatial); ApplyBothPatches(spatial); world.Tick(0, Input());
                CaptureStage?.Invoke("tape-v3-patched", world);
                spatial.LoadSource(); spatial.LoadTarget(); world.Tick(0, Input());
                CaptureStage?.Invoke("tape-v3-loaded", world);
                spatial.SetSourceTransport(1); world.Tick(.1f, Input(record: true, play: true));
                int pausedA = spatial.A.Frame, pausedB = spatial.B.Frame;
                world.SuspendInput();
                Check(result, spatial.A.Frame == pausedA && spatial.B.Frame == pausedB && !spatial.B.Play && !spatial.B.Armed,
                    "Focus suspension releases B recording without advancing either PCM head");
                bool stoppedAudio = true;
                foreach (AudioSource speaker in world.Root.GetComponentsInChildren<AudioSource>()) stoppedAudio &= !speaker.isPlaying;
                Check(result, stoppedAudio, "Focus suspension immediately stops the source and destination audio sources");
                world.Tick(.01f, Input());
                Check(result, spatial.A.Frame > pausedA && spatial.B.Frame == pausedB && !spatial.B.Play,
                    "Resume continues latched A at its old head while B remains released");
                spatial.ResetCurrentRecording();
                RecordCleanWithOracleForQA(spatial); world.Tick(0, Input());
                CaptureStage?.Invoke("tape-v3-recorded", world);
                foreach (TextMesh label in world.Root.GetComponentsInChildren<TextMesh>())
                    foreach (char c in label.text) if (!char.IsDigit(c) && c != '.') throw new InvalidOperationException("Tape v3 has nonnumeric device text");
                Check(result, true, "Tape device meshes contain only native numeric counter text");
            }
            finally { if (world != null) world.Dispose(); }
            return result.ToArray();
        }
    }
}
