using UnityEngine;
namespace MemoryWhitebox
{
    public sealed partial class ParkPhotoPrototype
    {
        float obstructedFor,clearFor,palmBlockedFor,palmClearFor;
        bool subjectObstructionCue,palmObstructionCue;
        bool SubjectVisible()=>Visible(companion.chest.position) ||
            Visible(companion.chest.position+companion.transform.right*.18f+Vector3.up*.12f) ||
            Visible(companion.chest.position-companion.transform.right*.18f+Vector3.up*.12f);
        static bool DebounceObstruction(bool visible,float dt,ref float blocked,ref float clear,bool cue)
        {
            if(visible){clear+=dt;blocked=0;if(clear>=.15f)cue=false;}
            else{blocked+=dt;clear=0;if(blocked>=.3f)cue=true;}
            return cue;
        }
        void TickPhotoVisibility(bool visible,float dt)
        {
            subjectObstructionCue=DebounceObstruction(visible,dt,ref obstructedFor,ref clearFor,subjectObstructionCue);
            bool handRelevant=CurrentTaskId=="meadow" && CurrentTask!=null && CurrentTask.aligned && HandInReach && InView(PhotoHandPosition);
            palmObstructionCue=DebounceObstruction(!handRelevant || Visible(PhotoHandPosition),dt,ref palmBlockedFor,ref palmClearFor,palmObstructionCue);
        }
        void DrawPhotoObstruction()
        {
            if(!subjectObstructionCue && !palmObstructionCue)return;
            Vector3 point=view.WorldToScreenPoint(palmObstructionCue?PhotoHandPosition:companion.chest.position);
            if(point.z<=0)return;
            var color=new Color(1,.71f,.35f,.9f);float x=point.x,y=Screen.height-point.y;
            for(int i=-2;i<=2;i++)
            {
                FeedbackLine(new Rect(x-26+i*12,y-26,7,2),color);
                FeedbackLine(new Rect(x-26+i*12,y+26,7,2),color);
            }
        }
    }
}
