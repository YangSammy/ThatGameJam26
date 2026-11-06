using UnityEngine;

namespace MemoryWhitebox
{
    public sealed partial class FirstPersonHands
    {
        Transform clearanceHand;
        Transform[] girlFingerBones;
        Vector3[] fingerStarts,fingerEnds;
        Vector3 girlPalm,palmRight,palmUp,palmForward;
        // Presentation-only envelopes, never physics colliders or writes to the girl.
        void PrepareHandClearance()
        {
            var girl=park!=null && park.companion!=null?park.companion.visualPresentation:null;
            var hand=girl!=null?girl.rightHand:null;
            if(hand!=clearanceHand)
            {
                clearanceHand=hand;girlFingerBones=null;
                if(hand==null)return;
                var bones=new System.Collections.Generic.List<Transform>();
                foreach(var t in hand.GetComponentsInChildren<Transform>())
                    if((t.name.EndsWith("Proximal")||t.name.EndsWith("Intermediate")||t.name.EndsWith("Distal")) && t.childCount>0)bones.Add(t);
                girlFingerBones=bones.ToArray();fingerStarts=new Vector3[bones.Count];fingerEnds=new Vector3[bones.Count];
            }
            if(hand==null)return;
            girlPalm=girl.PalmPosition;palmRight=hand.right;palmUp=hand.up;palmForward=hand.forward;
            for(int i=0;i<girlFingerBones.Length;i++){fingerStarts[i]=girlFingerBones[i].position;fingerEnds[i]=girlFingerBones[i].GetChild(0).position;}
        }
        float GirlHandClearance(Vector3 point)
        {
            if(clearanceHand==null)return 1;
            var delta=point-girlPalm;
            // World-metre palm box; model import scale is not applied twice.
            var q=new Vector3(Mathf.Abs(Vector3.Dot(delta,palmRight))-.012f,
                Mathf.Abs(Vector3.Dot(delta,palmUp))-.035f,
                Mathf.Abs(Vector3.Dot(delta,palmForward))-.032f);
            float distance=Vector3.Max(q,Vector3.zero).magnitude+Mathf.Min(0,Mathf.Max(q.x,Mathf.Max(q.y,q.z)));
            for(int i=0;i<fingerStarts.Length;i++)
            {
                var a=fingerStarts[i];var v=fingerEnds[i]-a;
                var near=a+v*Mathf.Clamp01(Vector3.Dot(point-a,v)/Mathf.Max(.000001f,v.sqrMagnitude));
                distance=Mathf.Min(distance,Vector3.Distance(point,near)-.007f);
            }
            return distance-.006f; // Player finger skin radius + small contact margin.
        }
        float SegmentHandClearance(Vector3 a,Vector3 b)
        {
            float gap=1;
            for(int i=0;i<=6;i++)gap=Mathf.Min(gap,GirlHandClearance(Vector3.Lerp(a,b,i/6f)));
            return gap;
        }
        void AimFingerWithClearance(Transform bone,Transform tip,Vector3 open,Vector3 desired)
        {
            var openRotation=Quaternion.FromToRotation(tip.position-bone.position,open)*bone.rotation;
            bone.rotation=openRotation;
            // Opening is not automatically clear: the thumb can sweep through the
            // offered fingers before closing. Find a clear open direction first.
            if(SegmentHandClearance(bone.position,tip.position)<0)
            {
                for(int step=1;step<=8;step++)
                {
                    var away=(open+park.companion.visualPresentation.PalmNormal*(step*.4f)).normalized;
                    bone.rotation=Quaternion.FromToRotation(tip.position-bone.position,away)*bone.rotation;
                    if(SegmentHandClearance(bone.position,tip.position)>=.001f)break;
                }
                openRotation=bone.rotation;
            }
            var closedRotation=Quaternion.FromToRotation(tip.position-bone.position,desired)*bone.rotation;
            var safe=openRotation;
            // Stop bending at first contact. Only the player's finger yields.
            for(int i=1;i<=12;i++)
            {
                bone.rotation=Quaternion.Slerp(openRotation,closedRotation,i/12f);
                if(SegmentHandClearance(bone.position,tip.position)<0)break;
                safe=bone.rotation;
            }
            bone.rotation=safe;
        }
        float HandholdFingerClearance()
        {
            if(handholdFingers==null || clearanceHand==null)return 1;
            float gap=1;
            for(int f=0;f<4;f++)for(int j=0;j<3;j++)
                if(handholdFingers[f,j]!=null && handholdFingers[f,j+1]!=null)
                    gap=Mathf.Min(gap,SegmentHandClearance(handholdFingers[f,j].position,handholdFingers[f,j+1].position));
            if(handholdThumb!=null)for(int j=0;j<3;j++)
                if(handholdThumb[j]!=null && handholdThumb[j+1]!=null)
                    gap=Mathf.Min(gap,SegmentHandClearance(handholdThumb[j].position,handholdThumb[j+1].position));
            return gap;
        }
        float HandholdForearmClearance()
        {
            var girl=park!=null && park.companion!=null?park.companion.visualPresentation:null;
            if(girl==null || girl.rightLowerArm==null || girl.rightHand==null)return 1;
            var a=skeletalRig.forearms[0].position;var b=skeletalRig.wrists[0].position;
            var ga=girl.rightLowerArm.position;var gv=girl.rightHand.position-ga;
            float gap=1;
            // Tapered display capsules: no Rigidbody, no pushing the companion.
            for(int i=0;i<=16;i++)
            {
                float t=i/16f;var p=Vector3.Lerp(a,b,t);
                float u=Mathf.Clamp01(Vector3.Dot(p-ga,gv)/Mathf.Max(.000001f,gv.sqrMagnitude));
                float radii=Mathf.Lerp(.045f,.021f,t)+Mathf.Lerp(.035f,.018f,u)+.002f;
                gap=Mathf.Min(gap,Vector3.Distance(p,ga+gv*u)-radii);
            }
            return gap;
        }
        void SeparateHandholdSkin()
        {
            if(clearanceHand==null || !HandholdVisible)return;
            var normal=park.companion.visualPresentation.PalmNormal;
            float moved=0;
            float budget=.006f;
            // Rest/open finger bases can already overlap the other hand. Bend limits
            // cannot fix that, so translate only the player's display rig a few mm.
            for(int i=0;i<24;i++)
            {
                float gap=Mathf.Min(HandholdFingerClearance(),HandholdForearmClearance());if(gap>=.001f)break;
                float step=Mathf.Min(budget-moved,Mathf.Max(.002f,.001f-gap));
                if(step<=0)break;
                skeletalRig.model.position+=normal*step;moved+=step;
            }
            handholdWrist=skeletalRig.wrists[0].position;
        }
    }
}
