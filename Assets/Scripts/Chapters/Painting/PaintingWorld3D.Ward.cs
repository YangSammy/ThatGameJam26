using UnityEngine;
namespace MemoryWhitebox
{
    public sealed partial class PaintingWorld3D
    {
        Transform straightRoute;
        bool finalFrameReady;
        Renderer[] wardSurfaces;
        readonly MaterialPropertyBlock revealBlock=new MaterialPropertyBlock();
        static readonly int RevealHeightId=Shader.PropertyToID("_Height");

        void BindWard()
        {
            if(layout!=null)
            {
                ward=layout.wardSet;
                if(ward==null || layout.revealStart==null || layout.revealCutoff==null)
                    throw new System.InvalidOperationException("Bedroom_Winter is missing its ward or reveal anchors.");
                wardSurfaces=ward.GetComponentsInChildren<Renderer>(true);
                ward.gameObject.SetActive(false);
                State.BindWardCutoff(layout.revealCutoff.position.y-layout.revealStart.position.y);
            }
            // The line belongs to the live page and is also needed when loading day five on its own.
            straightRoute=Block(route,"final-straight-line",new Vector3(8,-.05f,0),new Vector3(24,.1f,.8f),"#474a48");
            // This is ink inside the page; room lighting must not change a restored drawing.
            straightRoute.GetComponent<Renderer>().sharedMaterial.shader=Shader.Find("Unlit/Color");
            straightRoute.gameObject.SetActive(false);Layer(straightRoute,RouteLayer);
        }

        void RefreshWard()
        {
            // The paper is the same foreground object throughout; only the surrounding memory changes.
            float white=State.Phase>=3?1:Mathf.Clamp01(State.WhiteTime/(NarrativeDefinition.Resolve(State)?.whiteTransitionSeconds??1.2f));
            if(State.Phase==2)
            {
                if(layout!=null)TintObservedArt(white);
                View.backgroundColor=Color.Lerp(RoomSky,Color.white,white);
            }
            else
            {
                observation.gameObject.SetActive(false);ward.gameObject.SetActive(State.Phase==3);View.backgroundColor=Color.white;
                foreach(Transform child in route)child.gameObject.SetActive(child==actor || child==straightRoute);
                actor.localPosition=new Vector3(Mathf.Min(State.Actor.x,15),.3f,0);straightRoute.gameObject.SetActive(true);
                float height=layout.revealStart.position.y+Mathf.Min(State.WardRevealHeight,State.NoseHeight-.001f);
                foreach(var surface in wardSurfaces)
                {
                    surface.GetPropertyBlock(revealBlock);
                    revealBlock.SetFloat(RevealHeightId,height);
                    surface.SetPropertyBlock(revealBlock);
                }
            }
            paper.position=fixedEye+View.transform.rotation*new Vector3(0,Mathf.Lerp(-.14f,-.43f,white),.85f);
            paper.rotation=View.transform.rotation*Quaternion.Euler(white*75,0,0);
        }
        public void FreezeFinalFrame()
        {
            if(finalFrameReady)return;
            BuildAlbum();
            if(ward!=null)ward.gameObject.SetActive(false);
            if(observation!=null)observation.gameObject.SetActive(false);
            foreach(Transform child in route)child.gameObject.SetActive(child==actor || child==straightRoute);
            actor.localPosition=new Vector3(Mathf.Min(State.WardDistance,15),.3f,0);
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
