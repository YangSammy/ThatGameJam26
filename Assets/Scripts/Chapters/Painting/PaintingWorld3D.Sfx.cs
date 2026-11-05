using UnityEngine;

namespace MemoryWhitebox
{
    public sealed partial class PaintingWorld3D
    {
        bool pendantRevealSoundPlayed;

        // Compare one live simulation tick with its previous state. Loading a
        // checkpoint and refreshing visuals do not pass through these hooks.
        readonly struct WinterAudioFrame
        {
            public readonly int Phase, Riding, Falls;
            public readonly bool Grounded, PlatePressed, HighCheckpoint;
            public readonly Vector2 Actor;
            public readonly float SwingAngle, FlowerAngle, CrateX, BridgeGrowth;

            public WinterAudioFrame(PaintingChapter state)
            {
                Phase = state.Phase;
                Riding = state.Riding;
                Falls = state.FallCount;
                Grounded = state.Grounded;
                PlatePressed = state.PlatePressed;
                HighCheckpoint = state.HighCheckpoint;
                Actor = state.Actor;
                SwingAngle = state.SwingAngle;
                FlowerAngle = state.FlowerAngle;
                CrateX = state.CrateX;
                BridgeGrowth = state.BridgeGrowth;
            }
        }

        Vector3 WinterAudioPosition => View!=null ? View.transform.position + View.transform.forward * .65f : Eye;

        void WinterShot(int cue)
        {
            StorySfx.Shot(cue, WinterAudioPosition);
        }

        void WinterMotion(int cue, bool moving)
        {
            if (moving) StorySfx.Motion(cue, WinterAudioPosition);
            else StorySfx.Stop(cue);
        }

        void StopWinterMotion()
        {
            StorySfx.Stop(40);
            StorySfx.Stop(141);
            StorySfx.Stop(142);
            StorySfx.Stop(43);
            StorySfx.Stop(145);
            StorySfx.Stop(249);
        }

        void SyncWinterFocusSounds(int clue, float blurBefore)
        {
            if (State.BookOpen || !focusHeld || Defocus.Amount > .001f) return;
            if (blurBefore > .001f) WinterShot(137);
            if (clue == 4 && !State.Painted[4] && !pendantRevealSoundPlayed)
            {
                pendantRevealSoundPlayed = true;
                WinterShot(46);
            }
        }

        void SyncWinterActionSounds(WinterAudioFrame before)
        {
            if (before.Phase != State.Phase)
            {
                StopWinterMotion();
                if (before.Phase == 1 && State.Phase == 2) WinterShot(148);
                if (before.Phase == 2 && State.Phase == 3)
                {
                    WinterShot(49);
                    GameScore.Instance?.BeginWardNarrative(State);
                }
                if (before.Phase == 3 && State.Phase == 4)
                {
                    // The cut affects only the narrative bed. Paper feedback keeps its own voice.
                    GameScore.Instance?.CutWardNarrative(State);
                    WinterShot(149);
                }
                return;
            }

            bool moved = Mathf.Abs(State.Actor.x - before.Actor.x) > .0005f;
            if (State.Phase == 3)
            {
                WinterMotion(249, moved);
                return;
            }
            if (State.Phase >= 2 || !State.BookOpen || keyDragging)
            {
                StopWinterMotion();
                return;
            }

            bool fell = State.FallCount != before.Falls;
            bool walking = !fell && moved && before.Grounded && State.Grounded && State.Riding < 0;
            bool bridge = walking && State.OnDrawnBridge;
            WinterMotion(40, walking && !bridge);
            WinterMotion(142, bridge);
            WinterMotion(141, State.Riding == 0 && Mathf.Abs(State.SwingAngle - before.SwingAngle) > .0001f);
            WinterMotion(43, State.Phase == 1 && Mathf.Abs(State.CrateX - before.CrateX) > .0005f);
            WinterMotion(145, State.Riding > 0 && Mathf.Abs(State.FlowerAngle - before.FlowerAngle) > .0001f);

            if (fell) WinterShot(240);
            else if (State.Riding < 0)
            {
                if (!State.Grounded && (before.Grounded || before.Riding >= 0) && State.Velocity.y > 0) WinterShot(40);
                else if (!before.Grounded && before.Riding < 0 && State.Grounded) WinterShot(140);
            }
            if (before.Riding != 0 && State.Riding == 0) WinterShot(41);
            if (before.Riding <= 0 && State.Riding > 0) WinterShot(45);
            if (!before.HighCheckpoint && State.HighCheckpoint) WinterShot(245);
            if (before.BridgeGrowth < 1 && State.BridgeGrowth >= 1) WinterShot(42);
            if (State.Phase == 1 && before.PlatePressed != State.PlatePressed) WinterShot(44);
        }
    }
}
