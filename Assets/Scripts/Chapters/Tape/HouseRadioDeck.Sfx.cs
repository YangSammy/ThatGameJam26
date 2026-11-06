using UnityEngine;
namespace MemoryWhitebox
{
    public sealed partial class HouseRadioDeck
    {
        [Tooltip("Mechanical audio only; source recordings and save hashes remain unchanged.")]
        public TapeSfxBank sfxBank;
        TapeSfxPlayer sfx;
        float windingSoundUntil;
        void InitializeSfx()
        {
            sfx=gameObject.AddComponent<TapeSfxPlayer>();
            sfx.bank=sfxBank!=null?sfxBank:Resources.Load<TapeSfxBank>("TapeSfxBank");
            if(dualDeck!=null)dualDeck.MechanicalSound=DeckSound;
        }
        void DeckSound(int id,int side){if(sfx!=null)sfx.Shot(id,side==0?sourceSocket.position:slotHit.bounds.center,side==0?-.3f:.3f);}
        void TapeSound(int id,Vector3 point){if(sfx!=null)sfx.Shot(id,point);}
        void TickSfx()
        {
            if(sfx==null)return;
            bool silent=!focused || house==null || house.Muted || house.ShowHelp || (house.safeInspection!=null&&house.safeInspection.BlocksInput);
            sfx.SetMuted(silent);
            if(transport==null)return;
            transport.SetMuted(silent);
            string status=transport.TargetStatus;
            bool seeking=status=="REWINDING"||status=="FAST FORWARD";
            string sourceStatus=transport.SourceStatus;
            bool sourceSeeking=sourceStatus=="REWINDING" || sourceStatus=="FAST FORWARD";
            sfx.Loop(0,sourceSeeking?13:12,!silent&&Chapter.SourceLoaded&&(sourceSeeking||sourceStatus=="PLAYING"),sourceSocket.position,-.3f);
            sfx.Loop(1,12,!silent&&Selected>=0&&(status=="PLAYBACK"||transport.Writing),slotHit.bounds.center,.3f);
            sfx.Loop(2,13,!silent&&Selected>=0&&seeking,slotHit.bounds.center,.3f);
            if(silent || !WorkbenchMode || benchGesture!=BenchGesture.Spindle || Time.unscaledTime>windingSoundUntil)sfx.StopLoop(3);
        }
    }
}
