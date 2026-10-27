using UnityEngine;
namespace MemoryWhitebox
{
    // Used by the editor layout tool; HouseScene stores the resulting furniture and materials.
    public static class HouseRoomGeometry
    {
        public static GameObject Build()
        {
            var root=new GameObject("Lived-in room - shared furniture");
            Material wood=Mat("#765541"),lightWood=Mat("#ab8563"),wall=Mat("#d0c7b5"),cream=Mat("#e4d9c2"),fabric=Mat("#6c7c75"),metal=Mat("#4b5551"),glass=Mat("#b5cfcb"),rug=Mat("#897864");
            Part(root,"Floor",new Vector3(0,-.12f,0),new Vector3(7.6f,.24f,8.8f),lightWood);
            Part(root,"West wall",new Vector3(-3.8f,1.5f,0),new Vector3(.16f,3,8.8f),wall);
            Part(root,"East wall",new Vector3(3.8f,1.5f,0),new Vector3(.16f,3,8.8f),wall);
            Part(root,"North wall",new Vector3(0,1.5f,4.4f),new Vector3(7.6f,3,.16f),wall);
            Part(root,"South wall",new Vector3(0,1.5f,-4.4f),new Vector3(7.6f,3,.16f),wall);
            Part(root,"Ceiling",new Vector3(0,3.1f,0),new Vector3(7.6f,.2f,8.8f),cream);
            for(int i=-3;i<=3;i++)Part(root,"Floor board seam",new Vector3(i,.003f,0),new Vector3(.012f,.006f,8.8f),wood);
            Part(root,"Central woven rug",new Vector3(.15f,.011f,.05f),new Vector3(3.2f,.012f,3.2f),rug);
            Part(root,"Entrance door",new Vector3(-.8f,1.1f,-4.29f),new Vector3(1.25f,2.2f,.07f),wood);
            Part(root,"Door handle",new Vector3(-.3f,1.05f,-4.22f),new Vector3(.18f,.045f,.07f),metal);
            Part(root,"Coat rail",new Vector3(-2.4f,1.75f,-4.24f),new Vector3(1.4f,.12f,.12f),wood);
            for(int i=0;i<4;i++)Part(root,"Coat hook",new Vector3(-2.95f+i*.36f,1.69f,-4.12f),new Vector3(.04f,.12f,.11f),metal);
            // Writing desk remains by the living area; tape now lives by the entrance.
            Part(root,"Shared writing and recording desk",new Vector3(-.35f,.74f,3.37f),new Vector3(2f,.1f,1.3f),wood);
            for(int x=-1;x<=1;x+=2)for(int z=-1;z<=1;z+=2)Part(root,"Desk leg",new Vector3(-.35f+x*.85f,.35f,3.37f+z*.53f),new Vector3(.1f,.7f,.1f),wood);
            Block(root,"main desk",new Vector3(-.35f,.4f,3.37f),new Vector3(2f,.8f,1.3f));
            Part(root,"Desk drawer unit",new Vector3(-1.02f,.4f,3.4f),new Vector3(.55f,.7f,1.1f),lightWood);
            for(int i=0;i<3;i++){Part(root,"Drawer front",new Vector3(-1.02f,.22f+i*.21f,2.83f),new Vector3(.59f,.18f,.04f),wood);Part(root,"Drawer pull",new Vector3(-1.02f,.22f+i*.21f,2.79f),new Vector3(.15f,.025f,.04f),metal);}
            Part(root,"Desk lamp base",new Vector3(.35f,.82f,3.72f),new Vector3(.27f,.04f,.23f),metal);
            Part(root,"Desk lamp stem",new Vector3(.35f,1.06f,3.72f),new Vector3(.035f,.48f,.035f),metal);
            Part(root,"Desk lamp shade",new Vector3(.35f,1.31f,3.65f),new Vector3(.34f,.14f,.28f),cream);
            Part(root,"Wall bookshelf",new Vector3(-.65f,1.75f,4.12f),new Vector3(3.9f,.08f,.4f),wood);
            for(int i=0;i<12;i++)Part(root,"Book",new Vector3(-2.1f+i*.16f,1.95f,4.1f),new Vector3(.11f,.25f+(i%3)*.07f,.24f),i%2==0?fabric:lightWood);
            // Storage has closed cabinet fronts instead of table legs.
            Cabinet(root,"Camera storage cabinet",new Vector3(-2.4f,.45f,-1.1f),new Vector3(2.4f,.9f,1.5f),wood,lightWood,metal);
            Cabinet(root,"Wet cabinet",new Vector3(-2.95f,.45f,2.7f),new Vector3(1.45f,.9f,2.25f),fabric,cream,metal);
            Part(root,"Washable splashback",new Vector3(-3.69f,1.35f,2.7f),new Vector3(.05f,.75f,2.35f),glass);
            Part(root,"Chemical storage shelf",new Vector3(-3.49f,1.8f,2.1f),new Vector3(.42f,.07f,.85f),wood);
            // The window, two chairs and side table are shared by three activities.
            Part(root,"East window",new Vector3(3.69f,1.92f,2.65f),new Vector3(.04f,1.65f,2.3f),glass);
            Part(root,"Window sill",new Vector3(3.49f,1.09f,2.65f),new Vector3(.45f,.08f,2.5f),cream);
            Part(root,"Window vertical frame",new Vector3(3.63f,1.92f,2.65f),new Vector3(.08f,1.7f,.06f),cream);
            Part(root,"Window crossbar",new Vector3(3.63f,1.92f,2.65f),new Vector3(.08f,.06f,2.35f),cream);
            foreach(float z in new[]{1.37f,3.93f})Part(root,"Curtain",new Vector3(3.47f,1.8f,z),new Vector3(.18f,2,.3f),fabric);
            Chair(root,new Vector3(1.25f,0,2.7f),fabric,wood);Chair(root,new Vector3(2.65f,0,2.7f),fabric,wood);
            Part(root,"Shared cup side table",new Vector3(1.12f,.69f,1.8f),new Vector3(.68f,.08f,.62f),wood);
            for(int x=-1;x<=1;x+=2)for(int z=-1;z<=1;z+=2)Part(root,"Side table leg",new Vector3(1.12f+x*.26f,.32f,1.8f+z*.23f),new Vector3(.055f,.64f,.055f),wood);
            Block(root,"side table",new Vector3(1.12f,.4f,1.8f),new Vector3(.68f,.8f,.62f));
            Block(root,"easel",new Vector3(3,1,.8f),new Vector3(1.45f,2,.4f));
            Part(root,"Painting supplies case",new Vector3(3.3f,.16f,1.25f),new Vector3(.55f,.32f,.4f),wood);
            foreach(var p in new[]{new Vector3(-1.2f,2.6f,1),new Vector3(2,2.6f,2.8f),new Vector3(0,2.6f,-2.6f)})
            {var g=new GameObject("Warm ceiling light");g.transform.SetParent(root.transform,false);g.transform.localPosition=p;var l=g.AddComponent<Light>();l.type=LightType.Point;l.range=6;l.intensity=.3f;l.color=new Color(1,.91f,.78f);}
            AddEntranceConsole(root);
            FinishRoom(root,wood,lightWood,cream,fabric,metal);
            return root;
        }
        public static void AddEntranceConsole(GameObject root)
        {
            if(root.transform.Find("Entrance audio cabinet")!=null)return;
            var material=root.transform.Find("Shared writing and recording desk").GetComponent<Renderer>().sharedMaterial;
            Cabinet(root,"Entrance audio cabinet",new Vector3(1.1f,.37f,-2.21f),new Vector3(1.8f,.74f,2.1f),material,material,material);
        }
        static void FinishRoom(GameObject root,Material wood,Material lightWood,Material cream,Material fabric,Material metal)
        {
            var window=root.transform.Find("East window").GetComponent<Renderer>();var windowMaterial=new Material(window.sharedMaterial);windowMaterial.EnableKeyword("_EMISSION");windowMaterial.SetColor("_EmissionColor",new Color(.22f,.27f,.26f));window.sharedMaterial=windowMaterial;
            // Architectural edges anchor the large wall planes without adding story props.
            foreach(float x in new[]{-3.69f,3.69f})Part(root,"Wall skirting",new Vector3(x,.07f,0),new Vector3(.06f,.14f,8.55f),wood);
            foreach(float z in new[]{-4.29f,4.29f})Part(root,"Wall skirting",new Vector3(0,.07f,z),new Vector3(7.4f,.14f,.06f),wood);
            foreach(float x in new[]{-1.48f,-.12f})Part(root,"Door casing",new Vector3(x,1.13f,-4.22f),new Vector3(.09f,2.26f,.08f),cream);
            Part(root,"Door lintel",new Vector3(-.8f,2.25f,-4.22f),new Vector3(1.45f,.1f,.08f),cream);
            Part(root,"Entrance woven mat",new Vector3(-.8f,.018f,-3.9f),new Vector3(1.25f,.02f,.55f),fabric);
            // Drawer fronts and plinths make the worktops read as household storage.
            foreach(var name in new[]{"Camera storage cabinet","Entrance audio cabinet"})
            {
                var body=root.transform.Find(name);var p=body.localPosition;var size=body.localScale;float front=p.z-size.z*.5f-.012f;
                Part(root,"Recessed cabinet plinth",new Vector3(p.x,.07f,p.z),new Vector3(size.x-.12f,.14f,size.z-.12f),metal);
                for(int i=0;i<2;i++)Part(root,"Inset cabinet door",new Vector3(p.x+(i==0?-.25f:.25f)*size.x,p.y,front),new Vector3(size.x*.5f-.045f,size.y-.14f,.025f),lightWood);
            }
            // Cushions and a low stool support the existing activities without introducing people.
            foreach(float x in new[]{1.25f,2.65f})Part(root,"Chair seat cushion",new Vector3(x,.59f,2.7f),new Vector3(.86f,.06f,.76f),fabric);
            Part(root,"Writing stool seat",new Vector3(-.3f,.44f,2.45f),new Vector3(.5f,.09f,.44f),wood);
            for(int x=-1;x<=1;x+=2)for(int z=-1;z<=1;z+=2)Part(root,"Writing stool leg",new Vector3(-.3f+x*.18f,.21f,2.45f+z*.15f),new Vector3(.055f,.42f,.055f),wood);
            Block(root,"writing stool",new Vector3(-.3f,.25f,2.45f),new Vector3(.5f,.5f,.44f));
            Part(root,"Recorder lamp cord",new Vector3(1.1f,2.83f,-2.1f),new Vector3(.018f,.5f,.018f),metal);
            Part(root,"Recorder pendant shade",new Vector3(1.1f,2.55f,-2.1f),new Vector3(.37f,.12f,.33f),cream);
            Spot(root,"Recorder task light",new Vector3(1.1f,2.47f,-2.1f),new Vector3(1.1f,.8f,-2.1f),new Color(1,.82f,.6f),2.0f,68,5);
            Spot(root,"Window daylight",new Vector3(3.5f,2.35f,2.7f),new Vector3(.5f,.7f,2.4f),new Color(.77f,.88f,1),2.2f,100,7);
            Spot(root,"Writing task light",new Vector3(.35f,1.22f,3.65f),new Vector3(-.35f,.8f,3.1f),new Color(1,.87f,.67f),1.15f,82,3);
        }
        static void Spot(GameObject root,string name,Vector3 position,Vector3 target,Color color,float intensity,float angle,float range)
        {
            var g=new GameObject(name);g.transform.SetParent(root.transform,false);g.transform.localPosition=position;g.transform.LookAt(target);
            var light=g.AddComponent<Light>();light.type=LightType.Spot;light.color=color;light.intensity=intensity;light.spotAngle=angle;light.range=range;light.shadows=LightShadows.Soft;light.shadowStrength=.6f;light.shadowBias=.025f;
        }
        static Material Mat(string hex){var m=new Material(Resources.Load<Material>("WhiteboxMaterial"));Color c;ColorUtility.TryParseHtmlString(hex,out c);m.color=c;return m;}
        static void Part(GameObject root,string name,Vector3 p,Vector3 size,Material m){var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(root.transform,false);g.transform.localPosition=p;g.transform.localScale=size;g.GetComponent<Renderer>().sharedMaterial=m;var c=g.GetComponent<Collider>();if(Application.isPlaying)Object.Destroy(c);else Object.DestroyImmediate(c);}
        static void Block(GameObject root,string name,Vector3 p,Vector3 size){var g=new GameObject("Walk blocker - "+name);g.transform.SetParent(root.transform,false);g.transform.localPosition=p;var c=g.AddComponent<BoxCollider>();c.size=size;c.isTrigger=true;}
        static void Cabinet(GameObject root,string name,Vector3 p,Vector3 size,Material body,Material top,Material metal)
        {Part(root,name,p,size,body);Part(root,name+" worktop",p+Vector3.up*(size.y*.5f+.025f),new Vector3(size.x+.04f,.05f,size.z+.04f),top);for(int i=-1;i<=1;i+=2)Part(root,"Cabinet door pull",p+new Vector3(i*.15f,0,-size.z*.5f-.025f),new Vector3(.04f,.17f,.04f),metal);Block(root,name,p,size);}
        static void Chair(GameObject root,Vector3 p,Material fabric,Material wood)
        {Part(root,"Window armchair seat",p+new Vector3(0,.47f,0),new Vector3(1.15f,.18f,.95f),fabric);Part(root,"Window armchair back",p+new Vector3(0,.98f,.43f),new Vector3(1.15f,1,.17f),fabric);foreach(int x in new[]{-1,1}){Part(root,"Chair armrest",p+new Vector3(x*.51f,.7f,0),new Vector3(.14f,.14f,.95f),wood);foreach(int z in new[]{-1,1})Part(root,"Chair leg",p+new Vector3(x*.45f,.2f,z*.35f),new Vector3(.08f,.4f,.08f),wood);}Block(root,"armchair",p+Vector3.up*.5f,new Vector3(1.15f,1,1));}
    }
}







