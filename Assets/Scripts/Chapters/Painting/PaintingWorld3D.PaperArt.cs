using System.Collections.Generic;
using UnityEngine;

namespace MemoryWhitebox
{
    // Presentation only: all positions and visibility come from the existing puzzle.
    public sealed partial class PaintingWorld3D
    {
        Transform paperArt, outdoorArt, indoorArt, swingArt, bridgeArt, wheelArt, caseArt, walkwayArt;
        Transform swingLeft, swingRight, swingSeatArt;
        readonly Transform[] petalArt = new Transform[6], footholdArt = new Transform[6];
        SpriteRenderer travelerArt;
        Sprite[] travelerFrames;

        readonly List<Object> paperOwned = new List<Object>();
        readonly Dictionary<Renderer,bool> paperHidden = new Dictionary<Renderer,bool>();
        readonly Dictionary<Renderer,Material[]> paperOriginalMaterials = new Dictionary<Renderer,Material[]>();
        readonly Dictionary<Renderer,Material[]> paperTintMaterials = new Dictionary<Renderer,Material[]>();
        Material paperWood, paperCream, paperBlue, paperSage, paperGold, paperCavity;
        Vector2 paperLastActor;
        float paperWalkCycle;
        bool paperFacingLeft;
        int paperLastPhase=-1;

        Transform PaperGroup(string name, Transform parent)
        {
            var t = new GameObject(name).transform; t.SetParent(parent,false); t.gameObject.layer=RouteLayer; return t;
        }
        Material PaperMaterial(string hex)
        {
            var m=new Material(Shader.Find("MemoryWhitebox/WinterPaperWash")); Color c; ColorUtility.TryParseHtmlString(hex,out c); m.color=c;
            paperOwned.Add(m);return m;
        }
        Transform PaperQuad(string name,Transform parent,Vector3 p,Vector2 size,Material material)
        {
            var t=PaperGroup(name,parent);
            var mesh=new Mesh(); mesh.name=name;
            mesh.vertices=new[]{new Vector3(-.5f,-.5f),new Vector3(-.5f,.5f),new Vector3(.5f,.5f),new Vector3(.5f,-.5f)};
            mesh.uv=new[]{Vector2.zero,Vector2.up,Vector2.one,Vector2.right};
            mesh.triangles=new[]{0,1,2,0,2,3};mesh.RecalculateBounds();paperOwned.Add(mesh);
            t.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;
            t.gameObject.AddComponent<MeshRenderer>().sharedMaterial=material;
            t.localPosition=p;t.localScale=new Vector3(size.x,size.y,1);return t;
        }
        Transform PaperOval(string name,Transform parent,Vector3 p,Vector2 size,Material material)
        {
            var t=PaperQuad(name,parent,p,size,material);var mesh=t.GetComponent<MeshFilter>().sharedMesh;
            var v=new Vector3[25];var triangles=new int[72];
            for(int i=0;i<24;i++){float a=i*Mathf.PI/12;v[i+1]=new Vector3(Mathf.Sin(a)*.5f,Mathf.Cos(a)*.5f,0);
                triangles[i*3]=0;triangles[i*3+1]=i+1;triangles[i*3+2]=(i+1)%24+1;}
            mesh.Clear();mesh.vertices=v;mesh.triangles=triangles;mesh.RecalculateBounds();return t;
        }
        void PaperSegment(Transform t,Vector2 a,Vector2 b,float width,float depth)
        {
            Vector2 d=b-a;t.localPosition=new Vector3((a.x+b.x)*.5f,(a.y+b.y)*.5f,depth);
            t.localRotation=Quaternion.Euler(0,0,Mathf.Atan2(d.y,d.x)*Mathf.Rad2Deg);
            t.localScale=new Vector3(d.magnitude,width,1);
        }
        Transform PaperLine(Transform parent,string name,Vector2 a,Vector2 b,float width,Material mat,float z=-.8f)
        {var t=PaperQuad(name,parent,Vector3.zero,Vector2.one,mat);PaperSegment(t,a,b,width,z);return t;}
        void PaperHide(Transform target)
        {
            foreach(var r in target.GetComponentsInChildren<Renderer>(true))
            {if(!paperHidden.ContainsKey(r))paperHidden.Add(r,r.enabled);r.enabled=false;}
        }
        void PaperTint(Transform target)
        {
            foreach(var r in target.GetComponentsInChildren<Renderer>(true))
            {
                var originals=r.sharedMaterials;var replacements=new Material[originals.Length];
                for(int i=0;i<originals.Length;i++)
                {
                    string n=originals[i]!=null?originals[i].name.ToLowerInvariant():"";
                    replacements[i]=n.Contains("cavity")||n.Contains("recess")?paperCavity:n.Contains("brass")?paperGold:
                        n.Contains("enamel")?paperSage:n.Contains("wood")?paperWood:paperCream;
                }
                paperOriginalMaterials[r]=originals;paperTintMaterials[r]=replacements;r.sharedMaterials=replacements;
            }
        }
        void PaperPlatform(Transform parent,float left,float right,float top,bool snow)
        {
            PaperQuad("Platform face",parent,new Vector3((left+right)*.5f,(top-3.8f)*.5f,0),new Vector2(right-left,top+3.8f),snow?paperBlue:paperCream);
            PaperQuad("Walkable edge",parent,new Vector3((left+right)*.5f,top-.07f,-.2f),new Vector2(right-left,.14f),snow?paperCream:paperGold);
            if(snow)for(int i=0;i<8;i++)PaperOval("Soft snow edge",parent,new Vector3(Mathf.Lerp(left+.18f,right-.18f,i/7f),top-.12f,-.21f),new Vector2((right-left)/6,.23f),paperCream);
        }
        void PaperBackground(Transform parent,string resource)
        {
            var m=new Material(Shader.Find("Unlit/Texture"));m.mainTexture=Resources.Load<Texture2D>("WinterPaperArt/"+resource);paperOwned.Add(m);
            if(m.mainTexture==null)throw new System.InvalidOperationException("Missing winter paper background: "+resource);
            PaperQuad(resource,parent,new Vector3(8,2,4),new Vector2(18.72f,10.4f),m);
        }
        void BuildPaperArt()
        {
            if(paperArt!=null || layout==null)return;
            paperWood=PaperMaterial("#AB9599");paperCream=PaperMaterial("#F5E9D7");
            paperBlue=PaperMaterial("#B7CCD0");paperSage=PaperMaterial("#B8C1A9");paperGold=PaperMaterial("#D6B386");
            paperCavity=PaperMaterial("#64746E");
            paperArt=PaperGroup("Winter watercolor presentation",route);
            outdoorArt=PaperGroup("Outdoor",paperArt);indoorArt=PaperGroup("Indoor",paperArt);
            BuildOutdoorLayers();
            PaperBackground(indoorArt,"Indoor");
            PaperPlatform(indoorArt,-1.4f,6,0,false);PaperPlatform(indoorArt,10.1f,17.4f,3.2f,false);
            wheelArt=PaperGroup("Flower wheel",indoorArt);
            Vector3 hub=route.InverseTransformPoint(layout.wheel.transform.TransformPoint(new Vector3(0,.2f,0)));
            PaperLine(wheelArt,"Stem",new Vector2(hub.x,-2),hub,.12f,paperSage,.1f);
            for(int i=0;i<6;i++){
                petalArt[i]=PaperOval("Petal "+i,wheelArt,Vector3.zero,new Vector2(.85f,2.15f),i%2==0?paperCream:paperSage);
                footholdArt[i]=PaperQuad("Level foothold "+i,wheelArt,Vector3.zero,new Vector2(.8f,.09f),paperWood);
            }
            PaperOval("Flower center",wheelArt,new Vector3(hub.x,hub.y,-1),new Vector2(.65f,.65f),paperGold);
            caseArt=PaperGroup("Watercolor suitcase",indoorArt);
            PaperQuad("Case",caseArt,new Vector3(0,.36f,-1),new Vector2(.58f,.65f),paperSage);
            for(int i=-1;i<=1;i+=2)PaperQuad("Strap",caseArt,new Vector3(i*.17f,.36f,-1.01f),new Vector2(.035f,.65f),paperGold);
            PaperLine(caseArt,"Handle top",new Vector2(-.1f,.77f),new Vector2(.1f,.77f),.035f,paperWood,-1);
            PaperLine(caseArt,"Handle left",new Vector2(-.1f,.67f),new Vector2(-.1f,.77f),.035f,paperWood,-1);
            PaperLine(caseArt,"Handle right",new Vector2(.1f,.67f),new Vector2(.1f,.77f),.035f,paperWood,-1);
            float plate=route.InverseTransformPoint(layout.counterweight.transform.position).x;
            PaperQuad("Pressure plate",indoorArt,new Vector3(plate,.02f,-.8f),new Vector2(.65f,.08f),paperGold);
            PaperLine(indoorArt,"Plate linkage",new Vector2(plate,0),new Vector2(plate,-.6f),.04f,paperWood);
            PaperLine(indoorArt,"Counterweight linkage",new Vector2(plate,-.6f),new Vector2(plate-.8f,-.6f),.04f,paperWood);
            PaperQuad("Counterweight",indoorArt,new Vector3(plate-.8f,-.85f,-.8f),new Vector2(.3f,.4f),paperWood);
            walkwayArt=PaperQuad("Pressure bridge",indoorArt,new Vector3(7.1f,-.06f,-.7f),new Vector2(2.2f,.12f),paperGold);
            foreach(string id in new[]{"ground-a","ground-mid","ground-end","high-platform"})PaperHide(Nodes[id].transform);
            PaperHide(winterSwing.transform);PaperHide(winterBridge.transform);PaperHide(winterLoad.transform);PaperHide(winterFlower.transform);PaperHide(winterCrate);PaperHide(albumActor.transform);
            PaperTint(winterDoor.transform);
            BuildTravelerSprites();
        }
        void BuildTravelerSprites()
        {
            var texture=Resources.Load<Texture2D>("WinterPaperArt/Traveler");
            if(texture==null || !texture.isReadable)throw new System.InvalidOperationException("Traveler atlas must be readable.");
            travelerFrames=new Sprite[24];int w=texture.width/6,h=texture.height/4;
            for(int row=0;row<4;row++)for(int col=0;col<6;col++){
                int x=col*w,y=(3-row)*h;var pixels=texture.GetPixels(x,y,w,h);
                int left=w,right=0,bottom=h,top=0;
                for(int j=0;j<h;j++)for(int i=0;i<w;i++)if(pixels[j*w+i].a>.5f){left=Mathf.Min(left,i);right=Mathf.Max(right,i);bottom=Mathf.Min(bottom,j);top=Mathf.Max(top,j);}
                if(right<=left||top<=bottom)throw new System.InvalidOperationException("Empty traveler frame");
                var sprite=Sprite.Create(texture,new Rect(x+left,y+bottom,right-left+1,top-bottom+1),new Vector2(.5f,0),220,0,SpriteMeshType.FullRect);
                travelerFrames[row*6+col]=sprite;paperOwned.Add(sprite);
            }
            var t=PaperGroup("Scarf-free traveler",actor);travelerArt=t.gameObject.AddComponent<SpriteRenderer>();
            travelerArt.sprite=travelerFrames[0];paperLastActor=State.Actor;
        }
        void RefreshPaperArt()
        {
            BuildPaperArt();if(paperArt==null)return;
            foreach(var pair in paperHidden)if(pair.Key)pair.Key.enabled=false;
            foreach(var pair in paperTintMaterials)if(pair.Key)pair.Key.sharedMaterials=pair.Value;
            paperArt.gameObject.SetActive(State.Phase<2);
            outdoorArt.gameObject.SetActive(State.Phase==0);indoorArt.gameObject.SetActive(State.Phase==1);
            RefreshOutdoorLayers();
            wheelArt.gameObject.SetActive(State.Painted[3]);caseArt.gameObject.SetActive(State.Painted[2]);
            caseArt.localPosition=new Vector3(State.CrateX,0,0);walkwayArt.gameObject.SetActive(State.PlatePressed);
            var hub=route.InverseTransformPoint(layout.wheel.transform.TransformPoint(new Vector3(0,.2f,0)));
            for(int i=0;i<6;i++){
                Vector2 at=State.FlowerSeat(i);Vector2 d=at-(Vector2)hub;
                petalArt[i].localPosition=new Vector3(hub.x+d.x*.53f,hub.y+d.y*.53f,-.5f);
                petalArt[i].localRotation=Quaternion.Euler(0,0,Mathf.Atan2(d.y,d.x)*Mathf.Rad2Deg-90);
                footholdArt[i].localPosition=new Vector3(at.x,at.y-.045f,-.9f);
            }
            float dx=State.Actor.x-paperLastActor.x;
            bool walking=State.Riding<0 && paperLastPhase==State.Phase && Mathf.Abs(dx)>.0001f && Mathf.Abs(dx)<1;
            if(walking)paperFacingLeft=dx<0;
            if(walking)paperWalkCycle=Mathf.Repeat(paperWalkCycle+Mathf.Abs(dx)*1.4f,1);
            bool air=State.Phase<2 && !State.Grounded && State.Riding<0;
            bool landing=actorTransient>0 && actorTransition=="Land";
            bool seated=State.Riding==0;
            int row=(air||landing||seated?2:0)+(paperFacingLeft?1:0);
            int frame=air?(State.Velocity.y>1?2:State.Velocity.y< -1?4:3):walking?Mathf.FloorToInt(paperWalkCycle*6):0;
            if(air && actorTransient>0 && actorTransition=="Takeoff")frame=1;
            if(landing||seated)frame=5;
            travelerArt.sprite=travelerFrames[row*6+frame];
            // Keep feet anchored to the model, removing baked sprite jump translation.
            if(State.Phase<2)
            {
                Vector2 feet=State.Phase==0?OutdoorPoint(State.Actor):State.Actor;
                travelerArt.transform.position=route.TransformPoint(new Vector3(feet.x,feet.y-(seated?.18f:0),-2));
            }
            else travelerArt.transform.localPosition=new Vector3(0,0,-2);
            travelerArt.transform.localRotation=Quaternion.identity;
            travelerArt.transform.localScale=Vector3.one*(State.Phase==0?1.2f:1f);
            paperLastActor=State.Actor;
            paperLastPhase=State.Phase;
        }
        void RestorePaperArt()
        {
            foreach(var pair in paperHidden)if(pair.Key)pair.Key.enabled=pair.Value;
            foreach(var pair in paperOriginalMaterials)if(pair.Key)pair.Key.sharedMaterials=pair.Value;
        }
        void DisposePaperArt()
        {
            RestorePaperArt();
            if(paperArt)ReleaseObject(paperArt.gameObject);
            if(travelerArt)ReleaseObject(travelerArt.gameObject);
            foreach(var item in paperOwned)if(item)ReleaseObject(item);
            paperOwned.Clear();paperHidden.Clear();paperOriginalMaterials.Clear();paperTintMaterials.Clear();
        }
    }
}
