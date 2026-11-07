using UnityEngine;

namespace MemoryWhitebox
{
    public sealed partial class PaintingWorld3D
    {
        bool outdoorLayersReady;
        Transform outdoorBridgeHint, outdoorSwingHint, hintRopeL, hintRopeR, hintSeat;
        Material outdoorBridgeMaterial;
        static Vector2 ReferencePoint(float x,float y)
        {return new Vector2(-1.36f+x/1066f*18.72f,7.2f-y/597f*10.4f);}

        // One monotonic view map is shared by the actor, moving seat and landing
        // markers. The authoritative puzzle and save coordinates are untouched.
        internal static Vector2 OutdoorPoint(Vector2 p)
        {
            float x;
            if(p.x<.7f)x=Mathf.LerpUnclamped(0,98,(p.x+1.36f)/2.06f);
            else if(p.x<3)x=Mathf.Lerp(98,200,(p.x-.7f)/2.3f);
            else if(p.x<4.5f)x=Mathf.Lerp(200,299,(p.x-3)/1.5f);
            else if(p.x<6)x=Mathf.Lerp(299,368,(p.x-4.5f)/1.5f);
            else if(p.x<9)x=Mathf.Lerp(368,570,(p.x-6)/3);
            else if(p.x<12)x=Mathf.Lerp(570,892,(p.x-9)/3);
            else x=Mathf.LerpUnclamped(892,1066,(p.x-12)/5.36f);
            float bias=.7f*Mathf.SmoothStep(0,1,Mathf.InverseLerp(3,6,p.x));
            float bridge=Mathf.SmoothStep(0,1,Mathf.InverseLerp(8.8f,9.2f,p.x))*
                (1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(11.8f,12.2f,p.x)));
            var floor=ReferencePoint(x,414+13*bridge);
            return floor+Vector2.up*((p.y-bias)*.87f);
        }
        Transform ArtworkLayer(string name,Transform parent,float z,bool ghost=false)
        {
            var texture=Resources.Load<Texture2D>("WinterPaperArt/Layers/"+name);
            if(texture==null)throw new System.InvalidOperationException("Missing painted layer "+name);
            var material=new Material(Shader.Find("MemoryWhitebox/WinterPaintedLayer"));
            material.mainTexture=texture;material.SetFloat("_Ghost",ghost?1:0);paperOwned.Add(material);
            if(name=="SwingFrame")material.SetFloat("_Erode",4);
            return PaperQuad(name+(ghost?" hint":""),parent,new Vector3(8,2,z),new Vector2(18.72f,10.4f),material);
        }
        void BuildOutdoorLayers()
        {
            ArtworkLayer("Background",outdoorArt,4);
            ArtworkLayer("RiverRocks",outdoorArt,-.3f);
            var platforms=ArtworkLayer("SnowPlatforms",outdoorArt,.2f);
            platforms.localPosition+=Vector3.down*(30f/597f*10.4f);
            ArtworkLayer("Foreground",outdoorArt,-4);
            swingArt=PaperGroup("Swing",outdoorArt);
            ArtworkLayer("SwingFrame",swingArt,-.6f);
            swingLeft=PaperLine(swingArt,"Left suspension",Vector2.zero,Vector2.up,.025f,paperSage,-.8f);
            swingRight=PaperLine(swingArt,"Right suspension",Vector2.zero,Vector2.up,.025f,paperSage,-.8f);
            swingSeatArt=PaintedSeat(swingArt,false);
            bridgeArt=PaperGroup("Bridge",outdoorArt);
            var bridge=ArtworkLayer("Bridge",bridgeArt,-.7f);
            outdoorBridgeMaterial=bridge.GetComponent<Renderer>().sharedMaterial;
            outdoorBridgeMaterial.SetFloat("_UseReveal",1);
            outdoorBridgeHint=PaperGroup("Missing painted bridge",outdoorArt);
            ArtworkLayer("Bridge",outdoorBridgeHint,-.69f,true);
            outdoorSwingHint=PaperGroup("Missing painted swing",outdoorArt);
            ArtworkLayer("SwingFrame",outdoorSwingHint,-.59f,true);
            var hint=PaperMaterial("#D3D1BC");
            hintRopeL=PaperLine(outdoorSwingHint,"Hint rope left",Vector2.zero,Vector2.up,.018f,hint,-.79f);
            hintRopeR=PaperLine(outdoorSwingHint,"Hint rope right",Vector2.zero,Vector2.up,.018f,hint,-.79f);
            hintSeat=PaintedSeat(outdoorSwingHint,true);
            // The small destination flower remains an independent world marker.
            var flower=PaperGroup("Destination daisy",outdoorArt);
            var at=ReferencePoint(975,410);
            PaperLine(flower,"Stem",at,at+Vector2.up*.38f,.025f,paperSage,-1);
            for(int i=0;i<6;i++){float a=i*Mathf.PI/3;PaperOval("Petal",flower,new Vector3(at.x+Mathf.Sin(a)*.1f,at.y+.38f+Mathf.Cos(a)*.1f,-1),new Vector2(.15f,.15f),paperCream);}
            PaperOval("Heart",flower,new Vector3(at.x,at.y+.38f,-1.01f),new Vector2(.13f,.13f),paperGold);
            outdoorLayersReady=true;
        }
        Transform PaintedSeat(Transform parent,bool ghost)
        {
            // Reuse a painted beam strip, rather than substituting a flat rectangle.
            var texture=Resources.Load<Texture2D>("WinterPaperArt/Layers/SwingFrame");
            var m=new Material(Shader.Find("MemoryWhitebox/WinterPaintedLayer"));m.mainTexture=texture;
            m.mainTextureScale=new Vector2(.12f,.021f);m.mainTextureOffset=new Vector2(.215f,.574f);
            m.SetFloat("_Ghost",ghost?1:0);paperOwned.Add(m);
            return PaperQuad("Painted moving seat",parent,Vector3.zero,new Vector2(.72f,.12f),m);
        }
        void RefreshOutdoorLayers()
        {
            var pivot=OutdoorPoint(route.InverseTransformPoint(layout.swing.transform.TransformPoint(new Vector3(0,.33f,0))));
            var seat=OutdoorPoint(State.SwingSeat);
            swingArt.gameObject.SetActive(State.Painted[0]);bridgeArt.gameObject.SetActive(State.Painted[1]);
            outdoorSwingHint.gameObject.SetActive(!State.Painted[0]);outdoorBridgeHint.gameObject.SetActive(!State.Painted[1]);
            outdoorBridgeMaterial.SetFloat("_Reveal",State.BridgeGrowth);
            PaperSegment(swingLeft,pivot,seat+Vector2.left*.36f,.025f,-.8f);
            PaperSegment(swingRight,pivot,seat+Vector2.right*.36f,.025f,-.8f);
            PaperSegment(hintRopeL,pivot,seat+Vector2.left*.36f,.018f,-.79f);
            PaperSegment(hintRopeR,pivot,seat+Vector2.right*.36f,.018f,-.79f);
            swingSeatArt.localPosition=new Vector3(seat.x,seat.y-.06f,-.8f);
            hintSeat.localPosition=swingSeatArt.localPosition;
        }
    }
}
