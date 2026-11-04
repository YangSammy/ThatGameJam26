using UnityEngine;

namespace MemoryWhitebox
{
    public sealed partial class PaintingWorld3D
    {
        AudioSource pencilVoice;
        AudioClip pencil;
        LineRenderer emptyHanger;

        float introTime, answerTime, closedTime, sheetY, seatHintTime;
        bool openedOnce, introShown;
        int shortPresses;
        MemoryWhitebox.Art.AlbumWinterVisual observedSeat;

        void BeginFirstGap()
        {
            if(!pencilVoice)
            {
                pencilVoice=Root.AddComponent<AudioSource>();
                pencilVoice.playOnAwake=false;
                pencil=Resources.Load<AudioClip>("ChapterFoley/Pencil");
                // A broken pencil outline at the top pivot belongs only to the live page.
                emptyHanger=Line("Unfinished suspension mark",new[]{new Vector3(-.32f,.15f,0),
                    new Vector3(0,.15f,0),new Vector3(0,-.05f,0),new Vector3(.12f,-.12f,0)}, "#81796A",.065f);
                emptyHanger.transform.SetParent(route,false);
                emptyHanger.transform.position=layout.swing.transform.TransformPoint(new Vector3(0,.33f,0));
                emptyHanger.gameObject.layer=RouteLayer;
                emptyHanger.sharedMaterial.shader=Shader.Find("Unlit/Color");
                observedSeat=layout.observedSwing.GetComponentInChildren<MemoryWhitebox.Art.AlbumWinterVisual>(true);
            }
            bool fresh=State.Phase==0 && !State.MiddleCheckpoint && !State.Painted[0];
            introTime=fresh && !introShown?2.8f:0;
            introShown|=fresh;
            sheetY=introTime>0?-.16f:-.77f;
            answerTime=closedTime=seatHintTime=0;shortPresses=0;
        }

        void TurnPageForInput()
        {
            State.SetBook(!State.BookOpen);
            closedTime=0;answerTime=0;introTime=0;
            if(State.BookOpen)openedOnce=true;
            else if(State.Phase==0 && !State.Painted[0])seatHintTime=2;
        }

        void ShapeRecorded()
        {
            if(pencil!=null && (Host==null || !Host.Muted))pencilVoice.PlayOneShot(pencil,.48f);
            answerTime=1.15f;closedTime=0;shortPresses=0;
        }

        void TickFirstGap(float dt)
        {
            introTime=Mathf.Max(0,introTime-dt);
            answerTime=Mathf.Max(0,answerTime-dt);
            closedTime=State.BookOpen?0:closedTime+dt;
            if(seatHintTime>0 && observedSeat!=null)
            {
                seatHintTime=Mathf.Max(0,seatHintTime-dt);
                float u=1-seatHintTime/2;
                observedSeat.SetSwingAngleRadians(.075f*Mathf.Sin(2*Mathf.PI*u)*Mathf.Sin(Mathf.PI*u));
            }
            if(State.Phase!=0)return;
            float edge=Mathf.InverseLerp(.24f,.06f,pointer.y);
            float peek=State.BookOpen?1:Mathf.Max(Mathf.SmoothStep(0,.94f,Mathf.Clamp01(introTime)),edge*.36f);
            if(answerTime>0 && !State.BookOpen)peek=Mathf.Max(peek,.53f*Mathf.Sin(Mathf.PI*answerTime/1.15f));
            sheetY=Mathf.MoveTowards(sheetY,Mathf.Lerp(-.77f,-.14f,peek),dt*2.6f);
        }

        public string Guidance
        {
            get
            {
                if(State.Phase!=0 || State.MiddleCheckpoint)return State.TaskHint;
                if(State.BookOpen)
                {
                    if(State.Riding==0)return "A D  Swing / Space  Jump off";
                    if(!State.Painted[0])return "A D  Walk / Upper edge  Look up";
                    return "Walk to the swing seat";
                }
                if(shortPresses>=2 && !State.Painted[0])return "Hold LMB on the swing";
                if(closedTime>6 && (!openedOnce || State.Painted[0]))return "Lower edge  Raise album";
                if(openedOnce && !State.Painted[0] && closedTime>6)return "Hold LMB  Observe";
                return "";
            }
        }
    }
}
