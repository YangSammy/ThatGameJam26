using UnityEngine;
namespace MemoryWhitebox
{
    public sealed partial class PaintingWorld3D
    {
        Transform straightRoute, wardDesk, albumBacking;
        Vector3 wardPaperStart;
        Quaternion wardPaperRotation;
        bool wardPoseCaptured;
        bool finalFrameReady;
        WardImageReveal wardImage;
        bool wardInkConfigured;
        Color albumBackground;
        Vector3 albumEye;
        Vector3 albumActorScale;
        float albumSize,albumAspect,albumDepth;

        void BindWard()
        {
            if(layout!=null)
            {
                ward=layout.wardSet;
                if(ward==null || layout.revealStart==null || layout.revealCutoff==null)
                    throw new System.InvalidOperationException("Bedroom_Winter is missing its ward or reveal anchors.");
                ward.gameObject.SetActive(false);
                State.BindWardCutoff(layout.revealCutoff.position.y-layout.revealStart.position.y);
                BuildWardForeground();
            }
            // The line belongs to the live page and is also needed when loading day five on its own.
            straightRoute=Block(route,"final-straight-line",new Vector3(8,-.05f,0),new Vector3(24,.1f,.8f),"#474a48");
            // This is ink inside the page; room lighting must not change a restored drawing.
            straightRoute.GetComponent<Renderer>().sharedMaterial.shader=Shader.Find("Unlit/Color");
            straightRoute.gameObject.SetActive(false);Layer(straightRoute,RouteLayer);
        }

        void BuildWardForeground()
        {
            // Keep the sheet and its render texture alive as the surrounding memory changes.
            albumBacking=Block(paper,"Album backing",Vector3.zero,new Vector3(1,1,.012f),"#514d44");
            var hit=pageCollider.transform;
            albumBacking.SetPositionAndRotation(hit.position+hit.forward*.012f,hit.rotation);
            albumBacking.localScale=new Vector3(hit.lossyScale.x*1.06f,hit.lossyScale.y*1.07f,.012f);
            Layer(albumBacking,PaperLayer);
            foreach(var hitbox in albumBacking.GetComponentsInChildren<Collider>())hitbox.enabled=false;
            wardDesk=Block(Root.transform,"Persistent drawing desk",Vector3.zero,new Vector3(2,1.1f,.045f),"#77614b");
            Layer(wardDesk,PaperLayer);
            foreach(var hitbox in wardDesk.GetComponentsInChildren<Collider>())hitbox.enabled=false;
            wardDesk.gameObject.SetActive(false);
        }

        void RefreshWard()
        {
            if(!wardPoseCaptured)
            {
                wardPaperStart=paper.position;wardPaperRotation=paper.rotation;
                wardPoseCaptured=true;
            }
            float white=State.Phase>=3?1:Mathf.Clamp01(State.WhiteTime/(NarrativeDefinition.Resolve(State)?.whiteTransitionSeconds??1.2f));
            float settle=Mathf.SmoothStep(0,1,white);
            ConfigureWardInk();
            if(wardImage==null)wardImage=View.GetComponent<WardImageReveal>() ?? View.gameObject.AddComponent<WardImageReveal>();
            Defocus.End();
            // Put the outgoing physical page in the same camera source as the landscape,
            // then blend into the retained desk and live ink in the ward image.
            pageCamera.enabled=false;
            View.cullingMask=(1<<ObservationLayer)|(1<<PaperLayer);
            View.transform.rotation=Quaternion.Slerp(wardPaperRotation,fixedRotation,settle);
            var settledRotation=fixedRotation*Quaternion.Euler(65,0,0);
            var settledPosition=fixedEye+fixedRotation*new Vector3(0,-.32f,1.0f);
            wardDesk.SetPositionAndRotation(settledPosition+settledRotation*new Vector3(0,0,.04f),settledRotation);
            wardDesk.gameObject.SetActive(true);
            paper.SetPositionAndRotation(Vector3.Lerp(wardPaperStart,settledPosition,settle),Quaternion.Slerp(wardPaperRotation,settledRotation,settle));
            // The backing retracts behind the retained sheet; there is no object swap at white.
            albumBacking.gameObject.SetActive(white<1);
            albumBacking.localScale=new Vector3(albumBacking.localScale.x,albumBacking.localScale.y,Mathf.Max(.0001f,.012f*(1-settle)));
            TintObservedArt(white);
            View.backgroundColor=Color.Lerp(RoomSky,new Color(.973f,.957f,.925f),white);
            observation.gameObject.SetActive(State.Phase==2 && white<1);
            foreach(Transform child in route)child.gameObject.SetActive(child==actor || child==straightRoute);
            straightRoute.gameObject.SetActive(true);
            actor.localPosition=new Vector3(State.Phase==2?0:State.Actor.x,.02f,0);
            // Keep the old authored ward as a reversible asset, but never render it here.
            ward.gameObject.SetActive(false);
            wardImage.Present(State.Phase>=3?State.WardProgress:0,settle,albumTexture);
            Eye=fixedEye;Look=Eye+View.transform.forward;
        }
        void ConfigureWardInk()
        {
            if(wardInkConfigured)return;
            albumBackground=albumCamera.backgroundColor;albumEye=albumCamera.transform.position;
            albumSize=albumCamera.orthographicSize;albumAspect=albumCamera.aspect;albumDepth=albumCamera.depth;
            // The wide paper crop must not enlarge the existing ink figure to
            // the full height of the sheet. Keep its feet on the drawn line.
            albumActorScale=actor.localScale;actor.localScale=albumActorScale*.25f;
            albumCamera.backgroundColor=Color.clear;
            albumCamera.aspect=8;
            albumCamera.orthographicSize=State.WardWalkDistance/14.4f*Mathf.Abs(route.lossyScale.y);
            albumCamera.transform.position=route.TransformPoint(new Vector3(State.WardWalkDistance*.5f,.4f,-16));
            albumCamera.depth=View.depth-2;
            straightRoute.localScale=new Vector3(24,.025f,.8f);
            wardInkConfigured=true;
        }
        void EndWardImage()
        {
            if(wardImage!=null)wardImage.End();
            if(!wardInkConfigured || albumCamera==null)return;
            albumCamera.backgroundColor=albumBackground;albumCamera.transform.position=albumEye;
            albumCamera.orthographicSize=albumSize;albumCamera.aspect=albumAspect;albumCamera.depth=albumDepth;
            actor.localScale=albumActorScale;
            wardInkConfigured=false;
        }
        public void FreezeFinalFrame()
        {
            if(finalFrameReady)return;
            BuildAlbum();
            // The keepsake uses the original opaque page camera on both live completion
            // and a cold day-five load, rather than the transparent wide ink crop.
            EndWardImage();
            if(ward!=null)ward.gameObject.SetActive(false);
            if(observation!=null)observation.gameObject.SetActive(false);
            foreach(Transform child in route)child.gameObject.SetActive(child==actor || child==straightRoute);
            actor.localPosition=new Vector3(Mathf.Clamp(State.Actor.x,0,State.WardWalkDistance),.02f,0);
            bool wasActive=Root.activeSelf;
            Root.SetActive(true);
            albumCamera.Render();
            albumCamera.enabled=false;
            Root.SetActive(wasActive);
            if(winterSheet!=null)winterSheet.SetPageTexture(null);
            if(paper!=null)paper.gameObject.SetActive(false);
            finalFrameReady=true;
        }

        public RenderTexture GetFinalPageImage()
        {
            if(!State.Completed)return null;
            FreezeFinalFrame();
            return albumTexture;
        }
    }
}
