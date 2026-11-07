using UnityEngine;

namespace MemoryWhitebox
{
    public sealed partial class ParkPhotoPrototype
    {
        Texture2D outlineSource;
        RenderTexture referenceOutline;
        Material outlineMaterial;
        Material subjectMaskMaterial;
        AudioClip readyTone;
        AudioSource feedbackSound;
        bool feedbackWasReady;
        float feedbackCooldown,outlineAlpha;

        void InitializePhotoFeedback()
        {
            var shader=Shader.Find("Hidden/ParkPhoto/ReferenceOutline");
            if(shader!=null)outlineMaterial=new Material(shader);
            var maskShader=Shader.Find("Hidden/ParkPhoto/SubjectMask");
            if(maskShader!=null)subjectMaskMaterial=new Material(maskShader);
            feedbackSound=sound;
            if(feedbackSound==null){feedbackSound=gameObject.AddComponent<AudioSource>();feedbackSound.playOnAwake=false;feedbackSound.spatialBlend=0;}
            const int rate=22050,count=2646;
            var samples=new float[count];
            for(int i=0;i<count;i++)
            {
                float t=(float)i/(count-1);
                samples[i]=Mathf.Sin(i*2*Mathf.PI*660/rate)*Mathf.Sin(Mathf.PI*t)*.12f;
            }
            readyTone=AudioClip.Create("Viewfinder ready",count,1,rate,false);readyTone.SetData(samples,0);
        }
        void TickPhotoFeedback(float dt)
        {
            handJoinTransition=Mathf.Max(0,handJoinTransition-dt);
            feedbackCooldown=Mathf.Max(0,feedbackCooldown-dt);
            bool ready=Aiming && bridgeReady && !HandOffered && handJoinTransition<=0;
            if(ready && !feedbackWasReady && feedbackCooldown<=0 && feedbackSound!=null && readyTone!=null)
            {feedbackSound.PlayOneShot(readyTone,.35f);feedbackCooldown=2;}
            feedbackWasReady=ready;
            float target=Aiming && companion.AtPhotoNode && !guidedDeparturePending && CurrentTask!=null && !CurrentTask.aligned?CompositionCloseness()*.42f:0;
            outlineAlpha=Mathf.MoveTowards(outlineAlpha,target,dt*1.5f);
        }
        float CompositionCloseness()
        {
            var task=CurrentTask;if(task==null || !task.hasReference)return 0;
            float distance=Vector3.Distance(view.transform.position,task.position)/bridgePositionTolerance;
            float angle=Quaternion.Angle(view.transform.rotation,Quaternion.Euler(task.euler))/bridgeAngleTolerance;
            return 1-Mathf.InverseLerp(.35f,3,Mathf.Max(distance,angle));
        }
        void PrepareReferenceOutline()
        {
            if(outlineSource==bridgeReference || bridgeReference==null || outlineMaterial==null)return;
            if(referenceOutline!=null){referenceOutline.Release();Destroy(referenceOutline);}
            referenceOutline=new RenderTexture(bridgeReference.width,bridgeReference.height,0,RenderTextureFormat.ARGB32);
            var task=CurrentTask;
            var active=RenderTexture.active;
            RenderTexture mask=null;
            try
            {
                mask=BuildSubjectMask(task,bridgeReference.width,bridgeReference.height);
                if(mask==null){referenceOutline.Release();Destroy(referenceOutline);referenceOutline=null;return;}
                Graphics.Blit(mask,referenceOutline,outlineMaterial);outlineSource=bridgeReference;
            }
            finally{RenderTexture.active=active;if(mask!=null)RenderTexture.ReleaseTemporary(mask);}
        }
        RenderTexture BuildSubjectMask(BridgePhotoCheckpoint task,int width,int height)
        {
            if(subjectMaskMaterial==null)
            {var shader=Shader.Find("Hidden/ParkPhoto/SubjectMask");if(shader!=null)subjectMaskMaterial=new Material(shader);}
            if(subjectMaskMaterial==null || companion==null)return null;
            var target=RenderTexture.GetTemporary(width,height,24,RenderTextureFormat.ARGB32);
            var commands=new UnityEngine.Rendering.CommandBuffer{name="Photography reference subject silhouette"};
            try
            {
                commands.SetRenderTarget(target);commands.ClearRenderTarget(true,true,Color.clear);
                var matrix=Matrix4x4.Scale(new Vector3(1,1,-1))*Matrix4x4.TRS(task.position,Quaternion.Euler(task.euler),Vector3.one).inverse;
                var projection=GL.GetGPUProjectionMatrix(Matrix4x4.Perspective(task.fov,(float)width/height,.03f,100),false);
                commands.SetViewProjectionMatrices(matrix,projection);
                foreach(var renderer in companion.GetComponentsInChildren<Renderer>())
                {
                    if(!(renderer is MeshRenderer) && !(renderer is SkinnedMeshRenderer))continue;
                    if(!renderer.enabled || !renderer.gameObject.activeInHierarchy)continue;
                    for(int i=0;i<renderer.sharedMaterials.Length;i++)commands.DrawRenderer(renderer,subjectMaskMaterial,i,0);
                }
                Graphics.ExecuteCommandBuffer(commands);return target;
            }
            catch{RenderTexture.ReleaseTemporary(target);throw;}
            finally{commands.Release();}
        }
        static void FeedbackLine(Rect rect,Color color)
        {var old=GUI.color;GUI.color=color;GUI.DrawTexture(rect,Texture2D.whiteTexture);GUI.color=old;}
        static void FeedbackRing(Vector2 centre,float radius,Color color)
        {
            for(int i=0;i<32;i++)
            {
                float angle=i*Mathf.PI/16,next=(i+1)*Mathf.PI/16;
                Vector2 a=centre+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*radius;
                Vector2 b=centre+new Vector2(Mathf.Cos(next),Mathf.Sin(next))*radius;
                var matrix=GUI.matrix;
                GUIUtility.RotateAroundPivot(Mathf.Atan2(b.y-a.y,b.x-a.x)*Mathf.Rad2Deg,a);
                FeedbackLine(new Rect(a.x,a.y,Vector2.Distance(a,b),1.5f),color);GUI.matrix=matrix;
            }
        }
        void DrawPhotoFeedback()
        {
            if(!Aiming || Paused || CurrentTask==null || !CurrentTask.hasReference)return;
            if(!companion.AtPhotoNode || guidedDeparturePending)return;
            PrepareReferenceOutline();
            var task=CurrentTask;
            Color calm=new Color(.88f,.84f,.65f,.65f),good=new Color(.63f,.92f,.8f,.9f);
            if(referenceOutline!=null && outlineAlpha>.005f)
            {
                float width=Screen.height*(float)referenceOutline.width/referenceOutline.height;
                var old=GUI.color;GUI.color=new Color(.9f,.9f,.75f,outlineAlpha);
                GUI.DrawTexture(new Rect((Screen.width-width)*.5f,0,width,Screen.height),referenceOutline);GUI.color=old;
            }
            bool framed=TaskFramed(task);
            float margin=Mathf.Min(Screen.width,Screen.height)*.065f,length=framed?32:20;
            Color frame=framed?good:calm;
            foreach(float x in new[]{margin,Screen.width-margin})foreach(float y in new[]{margin,Screen.height-margin})
            {
                FeedbackLine(new Rect(x>Screen.width*.5f?x-length:x,y,length,2),frame);
                FeedbackLine(new Rect(x,y>Screen.height*.5f?y-length:y,2,length),frame);
            }
            float depth=TaskFocusDepth(task),range=ParkCameraResponse.ClearRange(focusDistance,Aperture);
            bool sharp=ParkCameraResponse.IsSharp(depth,focusDistance,Aperture);
            float gap=sharp?0:Mathf.Clamp(Mathf.Abs(depth-focusDistance)/Mathf.Max(.01f,range)*3,4,24);
            float cx=Screen.width*.5f,cy=Screen.height*.5f;
            // Two halves physically converge when focus is correct; no colour-only signal.
            FeedbackLine(new Rect(cx-15-gap,cy-6,12,2),sharp?good:calm);
            FeedbackLine(new Rect(cx+3+gap,cy+4,12,2),sharp?good:calm);
            FeedbackLine(new Rect(cx-15-gap,cy-6,2,12),sharp?good:calm);
            FeedbackLine(new Rect(cx+13+gap,cy-6,2,12),sharp?good:calm);
            float ey=Screen.height-88,ex=Screen.width*.5f;
            bool exposed=Mathf.Abs(ExposureError)<=bridgeExposureTolerance;
            FeedbackLine(new Rect(ex-64,ey,128,1),calm);
            for(int i=-2;i<=2;i++)FeedbackLine(new Rect(ex+i*30,ey-4,1,i==0?12:7),calm);
            FeedbackLine(new Rect(ex+Mathf.Clamp(-ExposureError,-2,2)*30-2,ey-9,4,18),exposed?good:calm);
            GUI.Label(new Rect(ex-83,ey-12,20,24),"−");GUI.Label(new Rect(ex+69,ey-12,20,24),"+");
            DrawPhotoObstruction();
            if(PhotoHandPointerActive)
            {
                Vector3 palm=view.WorldToScreenPoint(PhotoHandPosition);
                float r=PalmRadiusPixels(view,PhotoHandPosition);Color color=PointerOnHand?Color.white:new Color(1,.65f,.12f,1);
                var centre=new Vector2(palm.x,Screen.height-palm.y);
                FeedbackRing(centre,r,color);
                if(PointerOnHand)FeedbackRing(centre,r*.65f,good);
            }
        }
        void DisposePhotoFeedback()
        {
            if(referenceOutline!=null){referenceOutline.Release();Destroy(referenceOutline);}
            if(outlineMaterial!=null)Destroy(outlineMaterial);
            if(subjectMaskMaterial!=null)Destroy(subjectMaskMaterial);
            if(readyTone!=null)Destroy(readyTone);
        }
    }
}
