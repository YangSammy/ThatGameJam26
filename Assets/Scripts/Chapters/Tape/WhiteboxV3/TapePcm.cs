using System;
using UnityEngine;

namespace MemoryWhitebox.V3
{
    // Audio content and acceptance oracle are deliberately separate from transport input.
    // Nothing here changes a recorder's transport or removes samples from a player's take.
    public static class TapePcm
    {
        public const int SampleRate = 12000;
        public const float Duration = 12f;
        public const int FrameCount = SampleRate * 12;

        public sealed class Programme
        {
            public readonly int Commission;
            public readonly float[] Source;
            public readonly float[] Expected;
            internal readonly bool[] Noise;
            internal readonly int[] Phrase;
            internal readonly int[] PhraseFrames;
            internal Programme(int commission, float[] source, float[] expected, bool[] noise, int[] phrase, int[] counts)
            { Commission = commission; Source = source; Expected = expected; Noise = noise; Phrase = phrase; PhraseFrames = counts; }
        }

        public struct Acceptance
        {
            public bool Passed;
            public float Quality, RetainedFraction, NoiseSeconds, InvalidSeconds;
        }

        public static Programme Create(int commission)
        {
            commission = Mathf.Clamp(commission, 0, 2);
            var source = new float[FrameCount];
            var noise = new bool[FrameCount];
            var phrase = new int[FrameCount];
            var counts = new int[3];
            int cleanCount = 0;
            uint random = (uint)(67231 + commission * 19237);
            float[] notes = commission == 0 ? new[] { 220f, 277.18f, 329.63f, 293.66f }
                : commission == 1 ? new[] { 261.63f, 349.23f, 329.63f, 196f }
                : new[] { 293.66f, 349.23f, 329.63f, 220f };
            for (int i = 0; i < FrameCount; i++)
            {
                float t = i / (float)SampleRate;
                // These intervals belong to the generated source asset, never to an input controller.
                bool damaged = t >= 3.2f && t < 4.2f || t >= 7.4f && t < 8.4f;
                noise[i] = damaged;
                phrase[i] = t < 3.2f ? 0 : t < 7.4f ? 1 : 2;
                random ^= random << 13; random ^= random >> 17; random ^= random << 5;
                float white = (random & 65535) / 32767.5f - 1;
                if (damaged)
                    source[i] = Mathf.Clamp(white * .65f + Mathf.Sin(t * 2 * Mathf.PI * 91) * .18f, -.9f, .9f);
                else
                {
                    float local = t % .6f;
                    float envelope = Mathf.Min(1, local / .025f) * Mathf.Min(1, (.6f - local) / .09f);
                    float hz = notes[Mathf.FloorToInt(t / .6f) % notes.Length];
                    float music = (Mathf.Sin(t * 2 * Mathf.PI * hz) + .25f * Mathf.Sin(t * 2 * Mathf.PI * hz * 2)) * .22f * envelope;
                    if (commission == 2)
                    {
                        // Non-verbal birthday placeholders: wrapping rustle, chair, chain and a breathy laugh.
                        float wrapping = t > .4f && t < 1.2f ? white * .18f : 0;
                        float chain = t > 4.7f && t < 5.3f ? Mathf.Sin(t * 2 * Mathf.PI * 1800) * .12f * Mathf.Exp(-(t - 4.7f) * 7) : 0;
                        float chair = t > 8.8f && t < 9.5f ? Mathf.Sin(t * 2 * Mathf.PI * 73) * .12f : 0;
                        float laugh = t > 10 && t < 11.6f ? Mathf.Sin(t * 2 * Mathf.PI * (390 + 35 * Mathf.Sin(t * 19))) * Mathf.Max(0, Mathf.Sin(t * 18)) * .12f : 0;
                        music = music * .5f + wrapping + chain + chair + laugh;
                    }
                    source[i] = music;
                    counts[phrase[i]]++; cleanCount++;
                }
            }
            var expected = new float[cleanCount];
            for (int i = 0, n = 0; i < FrameCount; i++) if (!noise[i]) expected[n++] = source[i];
            return new Programme(commission, source, expected, noise, phrase, counts);
        }

        public static Acceptance Evaluate(Programme programme, float[] written, int[] sourceMap, int frames)
        {
            frames = Mathf.Clamp(frames, 0, written.Length);
            var seen = new bool[programme.Source.Length];
            var retained = new int[3];
            int noisy = 0, invalid = 0;
            for (int i = 0; i < frames; i++)
            {
                int source = sourceMap[i];
                if (source < 0 || source >= programme.Source.Length || Mathf.Abs(written[i] - programme.Source[source]) > .00001f)
                { invalid++; continue; }
                if (programme.Noise[source]) noisy++;
                else if (!seen[source]) { retained[programme.Phrase[source]]++; seen[source] = true; }
            }
            int total = retained[0] + retained[1] + retained[2];
            float coverage = total / (float)programme.Expected.Length;
            bool everyPhrase = true;
            for (int i = 0; i < retained.Length; i++)
                everyPhrase &= retained[i] >= programme.PhraseFrames[i] * .86f;
            float noiseSeconds = noisy / (float)SampleRate;
            float invalidSeconds = invalid / (float)SampleRate;
            bool length = frames >= programme.Expected.Length * .86f && frames <= programme.Expected.Length + SampleRate * .35f;
            return new Acceptance
            {
                Passed = everyPhrase && coverage >= .92f && noiseSeconds <= .16f && invalid == 0 && length,
                Quality = Mathf.Clamp01(coverage - noiseSeconds * .2f - invalidSeconds * .3f),
                RetainedFraction = coverage, NoiseSeconds = noiseSeconds, InvalidSeconds = invalidSeconds
            };
        }

        public static bool IsNoiseFrameForQA(Programme programme, int frame)
        { return frame >= 0 && frame < programme.Noise.Length && programme.Noise[frame]; }
    }
}
