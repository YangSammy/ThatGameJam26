using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
namespace MemoryWhitebox.EditorTools
{
    // Author once; runtime loads a small static prefab, never another bedroom scene.
    public static class MemoryBedroomAuthoring
    {
        const string Folder="Assets/Art/Environments/House/MemoryBedroom";
        const string Materials="Assets/Art/Environments/House/Bedroom/V17/Materials/";
        static Transform root;
        static readonly List<Mesh> temporaryMeshes=new List<Mesh>();
        static void Box(string name,Vector3 p,Vector3 size,string material,Vector3 rotation=default(Vector3))
        {
            Shape(name,p,size,material,PrimitiveType.Cube,rotation);
        }
        static void Shape(string name,Vector3 p,Vector3 size,string material,PrimitiveType shape,Vector3 rotation=default(Vector3))
        {
            var go=GameObject.CreatePrimitive(shape);go.name=name;go.transform.SetParent(root,false);
            go.transform.localPosition=p;go.transform.localScale=size;go.transform.localEulerAngles=rotation;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.GetComponent<Renderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(Materials+material+".mat");
            if(go.GetComponent<Renderer>().sharedMaterial==null)throw new System.Exception("Missing material "+material);
        }
        static void Cloth(string name,Vector3 center,float width,float height,bool curtain)
        {
            const int columns=32,rows=12;
            var vertices=new Vector3[(columns+1)*(rows+1)];var uv=new Vector2[vertices.Length];var indices=new List<int>();
            for(int y=0;y<=rows;y++)for(int x=0;x<=columns;x++)
            {
                float u=x/(float)columns,v=y/(float)rows;
                float wave=Mathf.Sin(u*Mathf.PI*(curtain?12:6))*(curtain?.045f:.018f);
                vertices[y*(columns+1)+x]=curtain?new Vector3((u-.5f)*width,(v-.5f)*height,wave):new Vector3((u-.5f)*width,wave+Mathf.Sin(v*13+u*8)*.012f,(v-.5f)*height);
                uv[y*(columns+1)+x]=new Vector2(u,v);
                if(x<columns && y<rows){int a=y*(columns+1)+x,b=a+1,c=a+columns+1,d=c+1;indices.AddRange(curtain?new[]{a,c,b,b,c,d}:new[]{a,c,b,b,c,d});}
            }
            var mesh=new Mesh{name=name};mesh.vertices=vertices;mesh.uv=uv;mesh.triangles=indices.ToArray();mesh.RecalculateNormals();mesh.RecalculateBounds();temporaryMeshes.Add(mesh);
            var go=new GameObject(name);go.transform.SetParent(root,false);go.transform.localPosition=center;go.AddComponent<MeshFilter>().sharedMesh=mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(Materials+"Linen.mat");
        }
        static void Details()
        {
            // Raised panels, rails and brass fittings remain inside existing footprints.
            for(int side=-1;side<=1;side+=2)
            {
                float z=.85f+side*.32f;
                Box("Wardrobe recessed panel",V(5.175f,1.08f,z),V(.04f,1.62f,.51f),"FloorWood");
                for(int edge=-1;edge<=1;edge+=2)
                {
                    Box("Wardrobe stile",V(5.14f,1.08f,z+edge*.26f),V(.07f,1.8f,.045f),"WoodDark");
                    Box("Wardrobe rail",V(5.14f,1.08f+edge*.88f,z),V(.07f,.055f,.56f),"WoodDark");
                }
                Shape("Wardrobe pull",V(5.09f,1.04f,.85f+side*.075f),V(.055f,.055f,.055f),"Brass",PrimitiveType.Sphere);
            }
            Box("Wardrobe crown",V(5.6f,2.14f,.85f),V(.82f,.12f,1.47f),"WoodDark");
            Box("Wardrobe plinth",V(5.6f,.1f,.85f),V(.8f,.18f,1.43f),"WoodDark");
            for(int i=0;i<3;i++)
            {
                Box("Desk drawer face",V(5.37f,.2f+i*.2f,3.15f),V(.045f,.17f,.65f),"WoodPaint");
                Box("Drawer pull",V(5.33f,.2f+i*.2f,3.15f),V(.035f,.035f,.17f),"Brass");
                Box("Bedside drawer",V(1.44f,.18f+i*.18f,3.215f),V(.42f,.15f,.035f),"FloorWood");
                Shape("Bedside knob",V(1.44f,.18f+i*.18f,3.185f),V(.04f,.04f,.04f),"Brass",PrimitiveType.Sphere);
            }
            for(int x=0;x<2;x++)for(int z=0;z<2;z++)
            {
                var pos=V(.1f+x*1.1f,.55f,2.04f+z*2.72f);
                Shape("Turned bedpost",pos,V(.095f,.55f,.095f),"WoodDark",PrimitiveType.Cylinder);
                Shape("Bedpost finial",pos+Vector3.up*.61f,V(.15f,.15f,.15f),"WoodDark",PrimitiveType.Sphere);
            }
            for(int i=0;i<6;i++)Box("Headboard spindle",V(.19f+i*.18f,.95f,4.7f),V(.035f,.52f,.04f),"WoodDark");
            Box("Headboard cap",V(.65f,1.25f,4.73f),V(1.2f,.08f,.15f),"WoodDark");
            Cloth("Soft cover folds",V(.65f,.765f,3.15f),1.07f,1.85f,false);
            Box("Folded blanket hem",V(.65f,.8f,3.98f),V(1.07f,.055f,.18f),"LinenCream");
            // Warm orange task lamp, angled arm, modest book stack; letter area stays clear.
            Shape("Lamp base",V(5.48f,.86f,3.48f),V(.28f,.025f,.28f),"Terracotta",PrimitiveType.Cylinder);
            Box("Lamp lower arm",V(5.48f,1.04f,3.48f),V(.035f,.35f,.035f),"Brass",V(0,0,-25));
            Box("Lamp upper arm",V(5.36f,1.26f,3.48f),V(.035f,.32f,.035f),"Brass",V(0,0,55));
            Shape("Lamp shade",V(5.22f,1.34f,3.48f),V(.23f,.14f,.23f),"Terracotta",PrimitiveType.Sphere);
            for(int i=0;i<3;i++)
            {
                Box("Stacked book cover",V(5.36f,.85f+i*.065f,2.49f),V(.36f,.06f,.25f),i%2==0?"WoodPaint":"Terracotta",V(0,i*8,0));
                Box("Book pages",V(5.36f,.86f+i*.065f,2.49f),V(.33f,.035f,.24f),"Paper",V(0,i*8,0));
            }
            // Chair tucked beneath the desk, outside the navigable center.
            Box("Chair seat",V(4.83f,.44f,3.63f),V(.43f,.08f,.43f),"WoodDark");
            for(int x=-1;x<=1;x+=2)for(int z=-1;z<=1;z+=2)Box("Chair leg",V(4.83f+x*.17f,.22f,3.63f+z*.17f),V(.045f,.44f,.045f),"WoodDark");
            Box("Chair back",V(4.83f,.76f,3.83f),V(.43f,.5f,.05f),"WoodDark");
            for(int i=0;i<11;i++)Box("North panel bead",V(.2f+i*.56f,.46f,5.16f),V(.025f,.76f,.025f),"WoodPaint");
            Box("Picture shelf",V(.2f,1.25f,3.2f),V(.24f,.055f,2.1f),"WoodDark");
        }
        static Vector3 V(float x,float y,float z){return new Vector3(x,y,z);}
        public static string Build()
        {
            if(!AssetDatabase.IsValidFolder(Folder))AssetDatabase.CreateFolder("Assets/Art/Environments/House","MemoryBedroom");
            root=new GameObject("Autumn memory bedroom - static lightweight set").transform;
            try
            {
                // Individual boards retain the source floor texture at a modest scale.
                for(int x=0;x<12;x++)for(int z=0;z<4;z++)Box("Floor plank",V(.25f+x*.5f,-.06f,.66f+z*1.32f),V(.49f,.12f,1.31f),"FloorWood");
                Box("North left plaster",V(.98f,1.8f,5.35f),V(1.96f,1.8f,.16f),"Wallpaper");
                Box("North right plaster",V(5.02f,1.8f,5.35f),V(1.96f,1.8f,.16f),"Wallpaper");
                Box("North upper wall",V(3,2.6f,5.35f),V(6,.35f,.16f),"Wallpaper");
                Box("West plaster",V(-.08f,1.8f,2.65f),V(.16f,1.8f,5.3f),"Wallpaper");
                Box("East cutaway plaster",V(6.08f,1.15f,2.65f),V(.16f,.5f,5.3f),"Wallpaper");
                Box("North wainscot",V(3,.47f,5.25f),V(6,.94f,.14f),"WoodDark");
                Box("West wainscot",V(0,.47f,2.65f),V(.14f,.94f,5.3f),"WoodDark");
                Box("East wainscot",V(6,.47f,2.65f),V(.14f,.94f,5.3f),"WoodDark");
                Box("West cornice",V(0,2.7f,2.65f),V(.2f,.12f,5.3f),"WoodDark");
                Box("North cornice",V(3,2.7f,5.25f),V(6,.12f,.2f),"WoodDark");
                for(int side=-1;side<=1;side+=2)
                {
                    Box("Window side frame",V(3+side*.9f,1.68f,5.12f),V(.08f,1.7f,.12f),"WoodPaint");
                    Cloth("Gathered curtain",V(3+side*1.18f,1.68f,5.01f),.58f,1.75f,true);
                    Shape("Curtain rod finial",V(3+side*1.53f,2.59f,4.98f),V(.1f,.1f,.1f),"Brass",PrimitiveType.Sphere);
                }
                Box("Window sill",V(3,.88f,5.02f),V(2.8f,.12f,.35f),"WoodPaint");
                Box("Window header",V(3,2.5f,5.12f),V(2.8f,.12f,.12f),"WoodPaint");
                Box("Bed frame",V(.65f,.37f,3.38f),V(1.05f,.3f,2.65f),"WoodDark");
                Box("Cream mattress",V(.65f,.58f,3.38f),V(1.04f,.2f,2.58f),"LinenCream");
                Box("Bed cover",V(.65f,.7f,3.06f),V(1.05f,.09f,1.85f),"Linen");
                Shape("Soft pillow",V(.65f,.77f,4.3f),V(.88f,.25f,.5f),"LinenCream",PrimitiveType.Sphere);
                Box("Bed headboard",V(.65f,.9f,4.76f),V(1.2f,.85f,.1f),"WoodDark");
                for(int x=0;x<2;x++)for(int z=0;z<2;z++)Box("Bed post",V(.1f+x*1.1f,.48f,2.04f+z*2.72f),V(.09f,.96f,.09f),"WoodDark");
                Box("Bedside cabinet",V(1.44f,.35f,3.48f),V(.48f,.7f,.5f),"WoodDark");
                Shape("Pendant dish",V(1.44f,.717f,3.48f),V(.36f,.018f,.32f),"LinenCream",PrimitiveType.Cylinder);
                Box("Flower stand top",V(3,.73f,4.44f),V(.65f,.08f,.55f),"WoodDark");
                for(int x=-1;x<=1;x+=2)for(int z=-1;z<=1;z+=2)Box("Flower stand leg",V(3+x*.25f,.35f,4.44f+z*.2f),V(.06f,.7f,.06f),"WoodDark");
                Box("East desktop",V(5.35f,.78f,3),V(1.25f,.1f,1.4f),"WoodDark");
                Box("Sage drawer unit",V(5.65f,.36f,3),V(.5f,.72f,1.25f),"WoodPaint");
                for(int z=-1;z<=1;z+=2)Box("Desk leg",V(4.85f,.36f,3+z*.58f),V(.07f,.72f,.07f),"WoodPaint");
                Box("Desk shelf",V(5.85f,1.65f,3.3f),V(.3f,.08f,1.6f),"WoodDark");
                for(int i=0;i<6;i++)Box("Shelf book",V(5.82f,1.84f,2.8f+i*.12f),V(.2f,.3f,.08f),i%2==0?"WoodPaint":"LinenCream");
                Box("Wardrobe",V(5.6f,1.05f,.85f),V(.7f,2.1f,1.35f),"WoodDark");
                Box("Wardrobe door",V(5.22f,1.08f,.85f),V(.06f,1.95f,1.23f),"FloorWood");
                Box("Wardrobe handle",V(5.17f,1.08f,.9f),V(.04f,.15f,.04f),"Brass");
                Box("Entrance left",V(1.15f,.2f,0),V(2.3f,.4f,.14f),"WoodDark");
                Box("Entrance right",V(4.85f,.2f,0),V(2.3f,.4f,.14f),"WoodDark");
                // A few framed pictures preserve the artist-bedroom identity.
                for(int i=0;i<3;i++)
                {
                    Box("Photo frame",V(.1f,1.65f,2.5f+i*.65f),V(.06f,.48f,.48f),"WoodDark");
                    Box("Photo paper",V(.14f,1.65f,2.5f+i*.65f),V(.02f,.4f,.4f),"Paper");
                }
                Details();
                // Bake by shared material: no runtime combining or per-prop update scripts.
                var groups=root.GetComponentsInChildren<MeshRenderer>().GroupBy(r=>r.sharedMaterial).ToArray();
                var old=root.Cast<Transform>().Select(t=>t.gameObject).ToArray();
                foreach(var group in groups)
                {
                    var mesh=new Mesh();mesh.name="Memory_"+group.Key.name;
                    mesh.CombineMeshes(group.Select(r=>new CombineInstance{mesh=r.GetComponent<MeshFilter>().sharedMesh,transform=root.worldToLocalMatrix*r.transform.localToWorldMatrix}).ToArray());
                    string path=Folder+"/"+mesh.name+".asset";var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
                    if(existing!=null){existing.Clear();existing.vertices=mesh.vertices;existing.normals=mesh.normals;existing.uv=mesh.uv;existing.triangles=mesh.triangles;existing.RecalculateBounds();existing.RecalculateTangents();EditorUtility.SetDirty(existing);Object.DestroyImmediate(mesh);mesh=existing;}else AssetDatabase.CreateAsset(mesh,path);
                    var go=new GameObject(mesh.name);go.transform.SetParent(root,false);go.AddComponent<MeshFilter>().sharedMesh=mesh;
                    var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=group.Key;renderer.shadowCastingMode=ShadowCastingMode.On;renderer.receiveShadows=true;
                }
                foreach(var go in old)Object.DestroyImmediate(go);
                var prefab=PrefabUtility.SaveAsPrefabAsset(root.gameObject,Folder+"/AutumnMemoryBedroom.prefab");
                var config=WritingVisualAssets.Load();config.memoryBedroom=prefab;EditorUtility.SetDirty(config);AssetDatabase.SaveAssets();
                return "Built lightweight bedroom: "+groups.Length+" shared-material mesh renderers";
            }
            finally {Object.DestroyImmediate(root.gameObject);foreach(var mesh in temporaryMeshes)Object.DestroyImmediate(mesh);temporaryMeshes.Clear();}
        }
    }
}
