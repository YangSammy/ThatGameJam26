using UnityEngine;
namespace MemoryWhitebox
{
    public sealed partial class PaintingWorld3D
    {
        Transform straightRoute;
        bool paperRetained;
        void BuildWard()
        {
            ward=Group("Ward reveal - seated silhouette",Vector3.zero);
            ward.SetPositionAndRotation(fixedEye+Quaternion.Euler(0,fixedRotation.eulerAngles.y,0)*new Vector3(0,-1.65f,3),Quaternion.Euler(0,fixedRotation.eulerAngles.y,0));
            Block(ward,"ward-floor",new Vector3(0,-.04f,0),new Vector3(8,.08f,6),"#d8dcd6");
            Block(ward,"ward-wall",new Vector3(0,1.6f,2.5f),new Vector3(8,3.2f,.1f),"#e5e6de");
            Block(ward,"bed",new Vector3(0,.55f,0),new Vector3(1.5f,.18f,2.1f),"#c1ccc8");
            for(int x=-1;x<=1;x+=2)for(int z=-1;z<=1;z+=2)Block(ward,"bed-leg",new Vector3(x*.6f,.25f,z*.85f),new Vector3(.08f,.5f,.08f),"#9ca9a6");
            Block(ward,"seated-skirt",new Vector3(0,.79f,-.25f),new Vector3(.6f,.3f,.8f),"#aab4ae");
            Block(ward,"seated-body",new Vector3(0,1.15f,.03f),new Vector3(.48f,.75f,.3f),"#aeb9b4",PrimitiveType.Capsule);
            Block(ward,"seated-head-covered",new Vector3(0,1.66f,.03f),new Vector3(.37f,.47f,.36f),"#414845",PrimitiveType.Sphere);
            for(int side=-1;side<=1;side+=2)
            {
                Block(ward,"seated-lower-leg",new Vector3(side*.18f,.34f,-.66f),new Vector3(.14f,.65f,.17f),"#9aa6a1",PrimitiveType.Capsule);
                var arm=Block(ward,"arm-on-lap",new Vector3(side*.23f,.99f,-.23f),new Vector3(.12f,.54f,.12f),"#c5c8b9",PrimitiveType.Capsule);arm.localRotation=Quaternion.Euler(40,0,side*18);
                Block(ward,"long-hair",new Vector3(side*.2f,1.43f,-.06f),new Vector3(.16f,.75f,.22f),"#414845",PrimitiveType.Capsule);
            }
            nose=new GameObject("SeatedNoseCutAnchor").transform;nose.SetParent(ward,false);nose.localPosition=new Vector3(0,1.64f,-.15f);
            var binding=Host!=null?Host.GetComponent<NarrativeSceneBinding>():null;
            if(binding!=null && binding.seatedNose!=null)nose=binding.seatedNose;
            foreach(var renderer in ward.GetComponentsInChildren<Renderer>())
            {var m=new Material(Shader.Find("Memory/WardReveal"));m.color=renderer.sharedMaterial.color;m.SetFloat("_Height",ward.position.y-.1f);renderer.sharedMaterial=m;wardMaterials.Add(m);}
            Layer(ward,ObservationLayer);ward.gameObject.SetActive(false);
            straightRoute=Block(route,"final-straight-line",new Vector3(8,-.05f,0),new Vector3(24,.1f,.8f),"#9faaa1");straightRoute.gameObject.SetActive(false);Layer(straightRoute,RouteLayer);
        }
        void RefreshWard()
        {
            // The paper is the same foreground object throughout; only the surrounding memory changes.
            float white=State.Phase>=3?1:Mathf.Clamp01(State.WhiteTime/(NarrativeDefinition.Resolve(State)?.whiteTransitionSeconds??1.2f));
            if(State.Phase==2)
            {
                foreach(var r in observation.GetComponentsInChildren<Renderer>())r.material.color=Color.Lerp(r.material.color,Color.white,white);
                View.backgroundColor=Color.Lerp(originalBackground,Color.white,white);
            }
            else
            {
                observation.gameObject.SetActive(false);ward.gameObject.SetActive(State.Phase==3);View.backgroundColor=Color.white;
                foreach(Transform child in route)child.gameObject.SetActive(child==actor || child==straightRoute);
                actor.localPosition=new Vector3(Mathf.Min(State.Actor.x,15),.3f,0);straightRoute.gameObject.SetActive(true);
                foreach(var m in wardMaterials)m.SetFloat("_Height",ward.position.y+Mathf.Min(State.WardRevealHeight,State.NoseHeight-.001f));
            }
            paper.position=fixedEye+View.transform.rotation*new Vector3(0,Mathf.Lerp(-.14f,-.43f,white),.85f);
            paper.rotation=View.transform.rotation*Quaternion.Euler(white*75,0,0);
        }
        public void RetainFinalPaper()
        {
            if(paper==null || paperRetained)return;paperRetained=true;
            ward.gameObject.SetActive(false);observation.gameObject.SetActive(false);
            var binding=Host!=null?Host.GetComponent<NarrativeSceneBinding>():null;
            var anchor=binding!=null?binding.finalPaper:null;
            paper.SetParent(Host!=null?Host.houseShell.transform:null,true);
            paper.SetPositionAndRotation(anchor!=null?anchor.position:new Vector3(-.1f,.81f,2.5f),anchor!=null?anchor.rotation:Quaternion.Euler(90,0,0));
            paper.localScale=Vector3.one*.45f;paper.gameObject.SetActive(true);Layer(paper,0);
            // Freeze the last page on its existing render target; no duplicate paper or second capture.
            foreach(Transform child in route)child.gameObject.SetActive(child==actor || child==straightRoute);
            actor.localPosition=new Vector3(Mathf.Min(State.WardDistance,15),.3f,0);
            bool wasActive=Root.activeSelf;Root.SetActive(true);albumCamera.Render();albumCamera.enabled=false;Root.SetActive(wasActive);
        }
        public void RestoreFinalPaper()
        {
            if(!State.Completed || paperRetained)return;
            BuildAlbum();RetainFinalPaper();
        }
    }
}
