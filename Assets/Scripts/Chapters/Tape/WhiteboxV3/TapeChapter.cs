using System;
using UnityEngine;

namespace MemoryWhitebox.V3
{
    public sealed class TapeChapter : Chapter
    {
        public const float Duration = TapePcm.Duration;
        public const int Samples = 240;
        public sealed class Candidate
        {
            public float Position = Duration * .5f;
            public readonly float[] Breaks;
            public readonly bool[] Seen = new bool[Samples];
            public bool Broken, UsedLeft, UsedRight, ReachedStart, ReachedEnd;
            public int Scanned;
            public Candidate(params float[] breaks) { Breaks = breaks; }
            public bool Verified => !Broken && UsedLeft && UsedRight && ReachedStart && ReachedEnd && Scanned == Samples;
        }
        public sealed class Deck
        {
            public bool Loaded, Play, Armed;
            public int Frame, Seek;
            public float Position { get => Frame / (float)TapePcm.SampleRate; set => Frame = Mathf.Clamp(Mathf.RoundToInt(value * TapePcm.SampleRate), 0, TapePcm.FrameCount); }
        }

        public readonly Deck A = new Deck(), B = new Deck();
        public readonly Candidate[] Candidates = new Candidate[3];
        public readonly bool[] Patched = new bool[2];
        public readonly float[] RecordedPcm = new float[TapePcm.FrameCount];
        public readonly int[] RecordedSourceFrames = new int[TapePcm.FrameCount];
        public TapePcm.Programme Programme { get; private set; }
        public int Selected = -1, DeliveredCount, RecordedFrames, Revision, SubmissionFailures;
        public bool PullingTape, TapePieceReady;
        public float PullLength, Quality;
        public bool SourceVerified => Selected >= 0 && Candidates[Selected].Verified;
        public bool TargetPrepared => SourceVerified && Patched[0] && Patched[1];
        public float InspectionProgress => Selected < 0 ? 0 : Candidates[Selected].Scanned / (float)Samples;
        public float InspectPosition => Selected < 0 ? Duration * .5f : Candidates[Selected].Position;
        public string Phase => Completed ? "complete" : !SourceVerified ? "inspect" : !TargetPrepared ? "prepare" : !A.Loaded || !B.Loaded ? "load" : "record";
        public int CommissionIndex => Mathf.Clamp(DeliveredCount, 0, 2);
        public float RecordedDuration => RecordedFrames / (float)TapePcm.SampleRate;
        public bool[] HeldKeys = new bool[5];
        bool sourcePlaying, previousF, freshF;
        int sourceSeek;
        double sourceRemainder, targetRemainder;

        public TapeChapter() { PrepareCommission(); }
        void PrepareCommission()
        {
            Programme = TapePcm.Create(CommissionIndex);
            Candidates[0] = new Candidate(2.2f);
            Candidates[1] = new Candidate(8.6f);
            Candidates[2] = new Candidate();
            Selected = -1; Patched[0] = Patched[1] = false;
            A.Loaded = B.Loaded = false; A.Frame = B.Frame = 0;
            sourcePlaying = false; sourceSeek = 0; previousF = freshF = false;
            PullingTape = TapePieceReady = false; PullLength = 0;
            A.Play = B.Play = A.Armed = B.Armed = false;
            sourceRemainder = targetRemainder = 0;
            ClearTake();
        }
        void ClearTake()
        {
            Array.Clear(RecordedPcm, 0, RecordedPcm.Length);
            for (int i = 0; i < RecordedSourceFrames.Length; i++) RecordedSourceFrames[i] = -1;
            RecordedFrames = 0; Quality = 0; Revision++;
        }
        public bool SelectCandidate(int index)
        {
            if (Completed || B.Loaded || index < 0 || index >= Candidates.Length) return false;
            if (Selected != index) { Patched[0] = Patched[1] = false; TapePieceReady = PullingTape = false; }
            Selected = index; return true;
        }
        public bool InspectReel(int side, float wheel)
        {
            if (Completed || B.Loaded || Selected < 0 || side < 0 || side > 1 || Mathf.Abs(wheel) < .001f) return false;
            Candidate c = Candidates[Selected];
            if (c.Broken) return false;
            float old = c.Position;
            float next = Mathf.Clamp(old + Mathf.Clamp(wheel, -3, 3) * .35f * (side == 0 ? -1 : 1), 0, Duration);
            if (Mathf.Approximately(old, next)) return false;
            if (side == 0) c.UsedLeft = true; else c.UsedRight = true;
            float lo = Mathf.Min(old, next), hi = Mathf.Max(old, next);
            foreach (float brokenAt in c.Breaks)
                if (brokenAt >= lo && brokenAt <= hi) { c.Position = brokenAt; c.Broken = true; return true; }
            c.Position = next;
            c.ReachedStart |= next <= .0001f; c.ReachedEnd |= next >= Duration - .0001f;
            for (int i = Mathf.Max(0, Mathf.FloorToInt(lo / Duration * Samples)); i < Mathf.Min(Samples, Mathf.CeilToInt(hi / Duration * Samples)); i++)
                if (!c.Seen[i]) { c.Seen[i] = true; c.Scanned++; }
            return true;
        }
        public bool BeginTapePull()
        {
            if (!SourceVerified || B.Loaded || TapePieceReady || Completed) return false;
            PullingTape = true; PullLength = 0; return true;
        }
        public void MoveTapePull(float metres) { if (PullingTape) PullLength = Mathf.Max(PullLength, metres); }
        public bool CompleteTapePull()
        {
            if (!PullingTape) return false;
            PullingTape = false; TapePieceReady = PullLength >= .20f;
            return TapePieceReady;
        }
        public bool PatchProtectHole(int side)
        {
            if (!SourceVerified || B.Loaded || !TapePieceReady || side < 0 || side > 1 || Patched[side]) return false;
            Patched[side] = true; TapePieceReady = false; PullLength = 0; return true;
        }
        public bool LoadSource()
        {
            if (Completed || A.Loaded) return false;
            A.Loaded = true; A.Frame = 0; return true;
        }
        public bool LoadTarget()
        {
            if (Completed || B.Loaded || !TargetPrepared) return false;
            B.Loaded = true; B.Frame = 0; return true;
        }
        public void SetSourceTransport(int transport)
        {
            if (!A.Loaded || Completed) return;
            sourceSeek = transport == -1 ? -1 : transport == 2 ? 1 : 0;
            sourcePlaying = transport == 1 && A.Frame < Programme.Source.Length;
        }
        public void ReleaseSourceSeek() { sourceSeek = 0; }
        public void ResetCurrentRecording()
        {
            if (Completed) return;
            B.Frame = A.Frame = 0; A.Play = B.Play = false; B.Armed = false;
            sourcePlaying = false; sourceSeek = 0; previousF = freshF = false;
            sourceRemainder = targetRemainder = 0; ClearTake();
        }
        static bool Key(WBInput input, KeyCode k) => input != null && input.Keys.Contains(k);
        public override void Tick(float dt, WBInput input)
        {
            if (Completed) return;
            dt = Mathf.Clamp(dt, 0, .1f);
            bool rec = Key(input, KeyCode.S), play = Key(input, KeyCode.F);
            bool rew = Key(input, KeyCode.J), ff = Key(input, KeyCode.L), stop = Key(input, KeyCode.Quote);
            HeldKeys[0] = rec; HeldKeys[1] = play; HeldKeys[2] = rew; HeldKeys[3] = ff; HeldKeys[4] = stop;
            if (play && !previousF) freshF = true;
            if (!play || stop) freshF = false;
            previousF = play;
            A.Seek = A.Loaded ? sourceSeek : 0;
            A.Play = A.Loaded && sourcePlaying && A.Seek == 0 && A.Frame < Programme.Source.Length;
            B.Seek = B.Loaded && !stop && rew != ff ? (rew ? -1 : 1) : 0;
            B.Play = B.Loaded && A.Loaded && play && freshF && !stop && !rew && !ff && B.Frame < RecordedPcm.Length;
            B.Armed = rec && !stop && B.Seek == 0;
            int oldA = A.Frame, oldB = B.Frame;
            bool sourceWasPlaying = A.Play;
            sourceRemainder += dt * TapePcm.SampleRate;
            targetRemainder += dt * TapePcm.SampleRate;
            int aFrames = (int)sourceRemainder, bFrames = (int)targetRemainder;
            sourceRemainder -= aFrames; targetRemainder -= bFrames;
            if (A.Seek != 0) A.Frame = Mathf.Clamp(A.Frame + A.Seek * aFrames * 4, 0, Programme.Source.Length);
            else if (A.Play) A.Frame = Mathf.Min(Programme.Source.Length, A.Frame + aFrames);
            if (A.Frame >= Programme.Source.Length) sourcePlaying = false;
            // Natural source-end interlock is a physical transport boundary. A can end
            // midway through this input tick, so B must not invent a full tick of silence
            // past that boundary. A manual stop has no such interlock and records silence.
            bool naturalSourceEnd = sourceWasPlaying && A.Frame >= Programme.Source.Length && B.Play && B.Armed;
            if (naturalSourceEnd) bFrames = Mathf.Min(bFrames, Programme.Source.Length - oldA);
            if (B.Seek != 0) B.Frame = Mathf.Clamp(B.Frame + B.Seek * bFrames * 4, 0, RecordedPcm.Length);
            else if (B.Play) B.Frame = Mathf.Min(RecordedPcm.Length, B.Frame + bFrames);
            if (B.Play && B.Armed && B.Frame > oldB)
            {
                // Explicitly starting a new take at the head establishes a new programme boundary.
                // The recorded samples themselves are always copied from the running source.
                if (oldB == 0) ClearTake();
                for (int i = oldB; i < B.Frame; i++)
                {
                    int from = oldA + i - oldB;
                    bool valid = sourceWasPlaying && from < Programme.Source.Length;
                    RecordedPcm[i] = valid ? Programme.Source[from] : 0;
                    RecordedSourceFrames[i] = valid ? from : -1;
                }
                RecordedFrames = Mathf.Max(RecordedFrames, B.Frame); Revision++;
            }
            if (naturalSourceEnd) { A.Play = B.Play = B.Armed = false; freshF = false; }
        }
        public TapePcm.Acceptance AssessTake() => TapePcm.Evaluate(Programme, RecordedPcm, RecordedSourceFrames, RecordedFrames);
        public bool Submit()
        {
            if (Completed || !A.Loaded || !B.Loaded || B.Play || B.Seek != 0) return false;
            var assessment = AssessTake(); Quality = assessment.Quality;
            if (A.Frame < Programme.Source.Length || !assessment.Passed) { SubmissionFailures++; return false; }
            DeliveredCount++;
            if (DeliveredCount >= 3) { A.Play = B.Play = false; Finish(); }
            else PrepareCommission();
            return true;
        }
        public override void Draw() { }
        public override string Help => "从收纳盒把磁带拖到检视槽；按住实际左/右轮轴滚轮，左右都检查到带头和带尾。红断头的换掉。拖拉右侧胶带卷撕下两片，分别点贴检视磁带上的两个孔。左侧委托带拖入左仓，检好的带拖入右仓。\n左仓四个图形键用鼠标：倒带、播放、停止、快进。右仓从左到右对应 S录、F播、J倒、L快、'停，按住才保持；F+S录制。杂音前停右仓，左仓继续播放，杂音后重新F+S。停键后须松F再按。源带自然走到带尾会联动停录；中途停源带仍录到静音。右仓停止时把磁带拖入右下交付槽。桌面回环图形可重试本委托；共三份。";
        public override string DebugState => "{\"phase\":\"" + Phase + "\",\"commission\":" + CommissionIndex + ",\"delivered\":" + DeliveredCount + ",\"frames\":" + RecordedFrames + ",\"a\":" + A.Frame + ",\"b\":" + B.Frame + ",\"completed\":" + Completed.ToString().ToLowerInvariant() + "}";
    }
}
