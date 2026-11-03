using UnityEngine;
using UnityEditor;
using System.Linq;
namespace MemoryWhitebox.Art.Editor { public static class AlbumWinterDeliveryBuild {
public static void Run(){try{
Debug.Log("AlbumWinter Import "+Newtonsoft.Json.JsonConvert.SerializeObject(Import()));
Debug.Log("AlbumWinter Validate "+Newtonsoft.Json.JsonConvert.SerializeObject(Validate()));
Debug.Log("AlbumWinter TestDriver "+Newtonsoft.Json.JsonConvert.SerializeObject(TestDriver()));
Debug.Log("AlbumWinter Mechanics "+Newtonsoft.Json.JsonConvert.SerializeObject(Mechanics()));
Debug.Log("AlbumWinter Showcase "+Newtonsoft.Json.JsonConvert.SerializeObject(Showcase()));
foreach(string f in new[]{"unity_checks.json","driver_checks.json","mechanical_checks.json"}){var j=Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText("output/E_AlbumWinter_v1/validation/"+f));if((string)j["status"]!="PASS")throw new System.Exception("Validation failed: "+f);}
System.IO.File.WriteAllText("output/E_AlbumWinter_v1/validation/batch_complete.json",Newtonsoft.Json.JsonConvert.SerializeObject(new {status="PASS",version=Application.unityVersion,pipeline=UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline==null?"Built-in":"SRP",batch=Application.isBatchMode}));
}catch(System.Exception e){Debug.LogException(e);throw;} }
static object Import(){
// Unity MCP method body. Asset creation only; never edits a production scene.
string art="Assets/Art/Props/AlbumWinterE";
AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
var specs=Newtonsoft.Json.Linq.JArray.Parse(System.IO.File.ReadAllText("output/E_AlbumWinter_v1/source/assets.json"));
foreach(string path in System.IO.Directory.GetFiles(art+"/Textures","*.png"))
{
    var ti=(TextureImporter)AssetImporter.GetAtPath(path);
    if(ti.sRGBTexture==path.EndsWith("_BaseColor.png")&&ti.textureType==(path.EndsWith("_Normal.png")?TextureImporterType.NormalMap:TextureImporterType.Default)&&ti.maxTextureSize==1024&&ti.textureCompression==TextureImporterCompression.Uncompressed)continue;
    ti.sRGBTexture=path.EndsWith("_BaseColor.png");
    ti.textureType=path.EndsWith("_Normal.png")?TextureImporterType.NormalMap:TextureImporterType.Default;
    ti.alphaSource=TextureImporterAlphaSource.FromInput;ti.alphaIsTransparency=false;
    ti.textureCompression=TextureImporterCompression.Uncompressed;ti.mipmapEnabled=true;ti.wrapMode=TextureWrapMode.Repeat;ti.filterMode=FilterMode.Trilinear;ti.anisoLevel=4;ti.maxTextureSize=1024;ti.SaveAndReimport();
}
var mats=new System.Collections.Generic.Dictionary<string,Material>();
foreach(string path in System.IO.Directory.GetFiles(art+"/Textures","*_BaseColor.png"))
{
    string name=System.IO.Path.GetFileName(path).Replace("_BaseColor.png","");string mp=art+"/Materials/"+name+".mat";
    var mat=AssetDatabase.LoadAssetAtPath<Material>(mp);if(mat==null){mat=new Material(Shader.Find("Standard"));AssetDatabase.CreateAsset(mat,mp);}
    mat.name=name;mat.color=Color.white;
    if(name=="M_E_Optics"||name=="M_E_Water")
    {mat.SetFloat("_Mode",3);mat.SetInt("_SrcBlend",(int)UnityEngine.Rendering.BlendMode.One);mat.SetInt("_DstBlend",(int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);mat.SetInt("_ZWrite",0);mat.EnableKeyword("_ALPHAPREMULTIPLY_ON");mat.renderQueue=3000;}
    mat.SetTexture("_MainTex",AssetDatabase.LoadAssetAtPath<Texture2D>(path));
    mat.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(art+"/Textures/"+name+"_Normal.png"));mat.SetFloat("_BumpScale",.55f);mat.EnableKeyword("_NORMALMAP");
    mat.SetTexture("_MetallicGlossMap",AssetDatabase.LoadAssetAtPath<Texture2D>(art+"/Textures/"+name+"_MetallicSmoothness.png"));mat.SetFloat("_GlossMapScale",1);mat.EnableKeyword("_METALLICGLOSSMAP");
    mat.SetTexture("_OcclusionMap",AssetDatabase.LoadAssetAtPath<Texture2D>(art+"/Textures/"+name+"_AO.png"));mat.SetFloat("_OcclusionStrength",1);
    if(name=="M_E_PageContent"){mat.shader=Shader.Find("ThatGamejam/Art/AlbumPage");mat.SetTexture("_PaperTex",AssetDatabase.LoadAssetAtPath<Texture2D>(art+"/Textures/M_E_Paper_BaseColor.png"));mat.SetTexture("_MainTex",Texture2D.whiteTexture);mat.SetFloat("_ContentAmount",0);}
    if(name=="M_E_BridgeWood"){mat.shader=Shader.Find("ThatGamejam/Art/AlbumReveal");mat.SetFloat("_Reveal",1);}
    EditorUtility.SetDirty(mat);AssetDatabase.SaveAssetIfDirty(mat);mats[name]=mat;
}
var report=new System.Collections.Generic.List<object>();
foreach(var spec in specs)
{
    string id=(string)spec["model"];string modelPath=art+"/Models/"+id+".fbx";
    var imp=(ModelImporter)AssetImporter.GetAtPath(modelPath);imp.bakeAxisConversion=false;imp.globalScale=1;imp.useFileScale=true;imp.importAnimation=id.StartsWith("E02");imp.animationType=ModelImporterAnimationType.Generic;imp.importCameras=false;imp.importLights=false;imp.importBlendShapes=true;imp.importNormals=ModelImporterNormals.Import;imp.importTangents=ModelImporterTangents.CalculateMikk;imp.isReadable=true;imp.meshCompression=ModelImporterMeshCompression.Off;
    foreach(var kv in mats)imp.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),kv.Key),kv.Value);imp.SaveAndReimport();
    var scene=UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
    try
    {
        var root=new GameObject((string)spec["prefab"]);UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root,scene);
        var visual=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(modelPath),scene);PrefabUtility.UnpackPrefabInstance(visual,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);visual.name="Visual";visual.transform.SetParent(root.transform,false);
        var all=visual.GetComponentsInChildren<Transform>(true);var matrices=all.ToDictionary(t=>t,t=>t.localToWorldMatrix);var points=all.ToDictionary(t=>t,t=>t.position);
        // All pivots use simple Unity local axes. Preserve static vertices AND every morph delta.
        foreach(var t in all){t.rotation=Quaternion.identity;t.localScale=Vector3.one;t.position=points[t];}
        foreach(var ren in visual.GetComponentsInChildren<Renderer>(true))
        {
            Mesh source=null;var mf=ren.GetComponent<MeshFilter>();var sk=ren as SkinnedMeshRenderer;
            if(mf!=null)source=mf.sharedMesh;else if(sk!=null)source=sk.sharedMesh;if(source==null)continue;
            Matrix4x4 old=matrices[ren.transform];ren.transform.localPosition=Vector3.zero;ren.transform.localRotation=Quaternion.identity;ren.transform.localScale=Vector3.one;
            Matrix4x4 change=ren.transform.worldToLocalMatrix*old;Matrix4x4 normal=change.inverse.transpose;
            var mesh=UnityEngine.Object.Instantiate(source);mesh.name=id+"_"+ren.name;
            mesh.vertices=source.vertices.Select(v=>change.MultiplyPoint3x4(v)).ToArray();mesh.normals=source.normals.Select(n=>normal.MultiplyVector(n).normalized).ToArray();
            var tang=source.tangents;for(int i=0;i<tang.Length;i++){var v=change.MultiplyVector(new Vector3(tang[i].x,tang[i].y,tang[i].z)).normalized;tang[i]=new Vector4(v.x,v.y,v.z,tang[i].w);}mesh.tangents=tang;
            mesh.ClearBlendShapes();
            for(int b=0;b<source.blendShapeCount;b++)
            {
                string key=source.GetBlendShapeName(b);if(key.Contains("."))key=key.Substring(key.LastIndexOf('.')+1);
                for(int f=0;f<source.GetBlendShapeFrameCount(b);f++)
                {
                    var dv=new Vector3[source.vertexCount];var dn=new Vector3[source.vertexCount];var dt=new Vector3[source.vertexCount];source.GetBlendShapeFrameVertices(b,f,dv,dn,dt);
                    for(int i=0;i<dv.Length;i++){dv[i]=change.MultiplyVector(dv[i]);dn[i]=normal.MultiplyVector(dn[i]);dt[i]=change.MultiplyVector(dt[i]);}
                    mesh.AddBlendShapeFrame(key,source.GetBlendShapeFrameWeight(b,f),dv,dn,dt);
                }
            }
            if(sk!=null&&sk.bones.Length>0)mesh.bindposes=sk.bones.Select(b=>b.worldToLocalMatrix*sk.transform.localToWorldMatrix).ToArray();
            mesh.RecalculateBounds();string meshPath=art+"/Meshes/"+mesh.name+".asset";var existing=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            if(existing==null)AssetDatabase.CreateAsset(mesh,meshPath);else{EditorUtility.CopySerialized(mesh,existing);UnityEngine.Object.DestroyImmediate(mesh);mesh=existing;}
            // CopySerialized updates CPU mesh data, but can retain obsolete GPU buffers in this Editor session.
            mesh.vertices=mesh.vertices;mesh.normals=mesh.normals;mesh.uv=mesh.uv;mesh.tangents=mesh.tangents;for(int i=0;i<mesh.subMeshCount;i++)mesh.SetTriangles(mesh.GetTriangles(i),i,false);mesh.UploadMeshData(false);EditorUtility.SetDirty(mesh);
            if(mf!=null)mf.sharedMesh=mesh;else{sk.sharedMesh=mesh;sk.updateWhenOffscreen=true;sk.localBounds=new Bounds(mesh.bounds.center,mesh.bounds.size+Vector3.one*.4f);}
            ren.sharedMaterials=ren.sharedMaterials.Select(m=>mats[m.name.Replace(" (Instance)","")]).ToArray();
        }
        all=root.GetComponentsInChildren<Transform>(true);
        System.Func<string,Transform> find=n=>all.First(t=>t.name==n);
        foreach(var a in spec["anchors"]){var p=a["position"].Select(v=>(float)v).ToArray();find((string)a["name"]).position=root.transform.TransformPoint(new Vector3(p[0],p[1],p[2]));}
        if(spec["anchorRotations"]!=null)foreach(var a in (Newtonsoft.Json.Linq.JObject)spec["anchorRotations"]){var v=a.Value.Select(t=>(float)t).ToArray();find(a.Key).localRotation=Quaternion.Euler(v[0],v[1],v[2]);}
        foreach(var c in spec["clips"])
        {
            // Unity samples an entire Vector3 when one component is animated. Preserve the
            // other coordinates explicitly, especially the off-centre exposure switch.
            var curves=(Newtonsoft.Json.Linq.JArray)c["curves"];
            foreach(var group in curves.Where(v=>(string)v["type"]=="Transform").GroupBy(v=>(string)v["node"]+"|"+((string)v["property"]).Substring(0,((string)v["property"]).LastIndexOf('.'))).ToArray())
            {
                var first=group.First();string node=(string)first["node"];string prop=(string)first["property"];string prefix=prop.Substring(0,prop.LastIndexOf('.'));var t=find(node);Vector3 baseline=prefix=="m_LocalPosition"?t.localPosition:t.localEulerAngles;
                for(int axis=0;axis<3;axis++){string component=prefix+"."+new[]{"x","y","z"}[axis];if(!curves.Any(v=>(string)v["node"]==node&&(string)v["property"]==component))curves.Add(Newtonsoft.Json.Linq.JObject.FromObject(new{node=node,type="Transform",property=component,values=new[]{baseline[axis],baseline[axis]},times=new[]{0f,1f}}));}
            }
            var ac=new AnimationClip();ac.name=(string)c["name"];ac.frameRate=30;ac.wrapMode=WrapMode.ClampForever;
            foreach(var cv in c["curves"])
            {
                Transform t=find((string)cv["node"]);string tn=(string)cv["type"];System.Type type=tn=="Transform"?typeof(Transform):tn=="GameObject"?typeof(GameObject):typeof(SkinnedMeshRenderer);
                float duration=(float?)c["duration"]??1f;var times=cv["times"].Select(v=>(float)v*duration).ToArray();var vals=cv["values"].Select(v=>(float)v).ToArray();var curve=new AnimationCurve(times.Select((time,i)=>new Keyframe(time,vals[i])).ToArray());
                for(int i=0;i<curve.length;i++){AnimationUtility.SetKeyLeftTangentMode(curve,i,tn=="GameObject"?AnimationUtility.TangentMode.Constant:AnimationUtility.TangentMode.Linear);AnimationUtility.SetKeyRightTangentMode(curve,i,tn=="GameObject"?AnimationUtility.TangentMode.Constant:AnimationUtility.TangentMode.Linear);}
                AnimationUtility.SetEditorCurve(ac,EditorCurveBinding.FloatCurve(AnimationUtility.CalculateTransformPath(t,root.transform),type,(string)cv["property"]),curve);
            }
            var settings=AnimationUtility.GetAnimationClipSettings(ac);settings.loopTime=(bool?)c["loop"]??false;AnimationUtility.SetAnimationClipSettings(ac,settings);
            string path=art+"/Animations/"+ac.name+".anim";var old=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);if(old==null)AssetDatabase.CreateAsset(ac,path);else{EditorUtility.CopySerialized(ac,old);UnityEngine.Object.DestroyImmediate(ac);}
        }
        if(spec["defaultInactive"]!=null)foreach(var n in spec["defaultInactive"])find((string)n).gameObject.SetActive(false);
        // A complete baseline clip clears every owned channel for cancel/reset, with no root bindings.
        var rest=new AnimationClip();rest.name="AN_"+id+"_RestAll";rest.frameRate=30;rest.wrapMode=WrapMode.ClampForever;
        foreach(var cv in spec["clips"].SelectMany(c=>c["curves"]).GroupBy(c=>(string)c["node"]+"|"+(string)c["property"]).Select(g=>g.First()))
        {
            var t=find((string)cv["node"]);string prop=(string)cv["property"];string tn=(string)cv["type"];System.Type type=tn=="Transform"?typeof(Transform):tn=="GameObject"?typeof(GameObject):typeof(SkinnedMeshRenderer);float value=0;
            if(tn=="GameObject")value=t.gameObject.activeSelf?1:0;else if(tn=="Transform"){int axis=prop.EndsWith(".x")?0:prop.EndsWith(".y")?1:2;value=prop.StartsWith("m_LocalPosition")?t.localPosition[axis]:t.localEulerAngles[axis];}
            AnimationUtility.SetEditorCurve(rest,EditorCurveBinding.FloatCurve(AnimationUtility.CalculateTransformPath(t,root.transform),type,prop),AnimationCurve.Constant(0,1,value));
        }
        string rp=art+"/Animations/"+rest.name+".anim";var ro=AssetDatabase.LoadAssetAtPath<AnimationClip>(rp);if(ro==null)AssetDatabase.CreateAsset(rest,rp);else{EditorUtility.CopySerialized(rest,ro);UnityEngine.Object.DestroyImmediate(rest);}
        foreach(var h in spec["hits"])
        {
            var go=new GameObject("Hit_"+(string)h["name"]+"_DISABLED");go.transform.SetParent(find((string)h["node"]),false);go.layer=2;var bc=go.AddComponent<BoxCollider>();bc.isTrigger=true;bc.enabled=false;
            var p=h["center"].Select(v=>(float)v).ToArray();var s=h["size"].Select(v=>(float)v).ToArray();bc.center=new Vector3(p[0],p[1],p[2]);bc.size=new Vector3(s[0],s[1],s[2]);
        }
        var vertices=new System.Collections.Generic.List<Vector3>();int triangles=0,shapes=0;
        foreach(var r in root.GetComponentsInChildren<Renderer>(true))
        {var f=r.GetComponent<MeshFilter>();var sk=r as SkinnedMeshRenderer;var m=f!=null?f.sharedMesh:sk!=null?sk.sharedMesh:null;if(m==null)continue;triangles+=m.triangles.Length/3;shapes+=m.blendShapeCount;vertices.AddRange(m.vertices.Select(v=>root.transform.InverseTransformPoint(r.transform.TransformPoint(v))));}
        var bounds=new Bounds(vertices[0],Vector3.zero);foreach(var v in vertices)bounds.Encapsulate(v);
        var phys=new GameObject("PhysicsProxy_DISABLED");phys.transform.SetParent(find("MotionPivot"),false);var box=phys.AddComponent<BoxCollider>();box.center=bounds.center;box.size=bounds.size;box.enabled=false;phys.SetActive(false);
        if(id=="E07_PendantDoor"){
            var key=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Props/WritingD/Prefabs/PF_PendantKey_D02.prefab"),scene);key.name="KeyVisual";key.transform.SetParent(find("KeyInsertPivot"),false);key.transform.localRotation=Quaternion.Euler(0,180,0);key.SetActive(false);
        }
        var driver=root.AddComponent<MemoryWhitebox.Art.AlbumWinterVisual>();
        driver.Configure(id,AssetDatabase.LoadAssetAtPath<AnimationClip>(rp),spec["clips"].Select(c=>AssetDatabase.LoadAssetAtPath<AnimationClip>(art+"/Animations/"+(string)c["name"]+".anim")).ToArray());
        string pf=art+"/Prefabs/"+(string)spec["prefab"]+".prefab";PrefabUtility.SaveAsPrefabAsset(root,pf);
        var usedMaterials=root.GetComponentsInChildren<Renderer>(true).SelectMany(r=>r.sharedMaterials).Distinct().ToArray();
        var textures=usedMaterials.SelectMany(m=>m.GetTexturePropertyNames().Select(m.GetTexture)).Where(t=>t!=null).Distinct().Select(t=>new{path=AssetDatabase.GetAssetPath(t),width=t.width,height=t.height}).Where(t=>t.path.StartsWith("Assets/")).ToArray();
        report.Add(new {asset=id,prefab=pf,triangles=triangles,materials=usedMaterials.Length,materialNames=usedMaterials.Select(m=>m.name).ToArray(),textures=textures,renderers=root.GetComponentsInChildren<Renderer>(true).Length,blendShapes=shapes,clips=spec["clips"].Count()+1,size=new[]{bounds.size.x,bounds.size.y,bounds.size.z},min=new[]{bounds.min.x,bounds.min.y,bounds.min.z},max=new[]{bounds.max.x,bounds.max.y,bounds.max.z}});
    }
    finally{UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);}
}
foreach(string guid in AssetDatabase.FindAssets("",new[]{art})){var a=AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(guid));if(a!=null)AssetDatabase.SaveAssetIfDirty(a);}
System.IO.File.WriteAllText("output/E_AlbumWinter_v1/source/assets.json",specs.ToString());string json=Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented);System.IO.File.WriteAllText("output/E_AlbumWinter_v1/validation/unity_import.json",json);return new{prefabs=report.Count};

}

static object Validate(){
string art="Assets/Art/Props/AlbumWinterE";string output="output/E_AlbumWinter_v1";
var specs=Newtonsoft.Json.Linq.JArray.Parse(System.IO.File.ReadAllText(output+"/source/assets.json"));
var checks=new System.Collections.Generic.List<string>();var failures=new System.Collections.Generic.List<string>();
System.Action<bool,string> check=(ok,s)=>{if(ok)checks.Add(s);else failures.Add(s);};
var activeScene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();bool dirty=activeScene.isDirty;
var preview=UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();RenderTexture rt=null;Texture2D img=null;
try
{
    var cg=new GameObject("AssetPreviewCamera");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cg,preview);var cam=cg.AddComponent<Camera>();cam.scene=preview;cam.enabled=false;cam.orthographic=true;cam.nearClipPlane=.001f;cam.farClipPlane=8;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.17f,.163f,.15f);cam.allowHDR=false;
    int li=0;foreach(var p in new[]{new Vector3(-2,3,2),new Vector3(2,1,1),new Vector3(0,2,-2)}){var g=new GameObject("PreviewLight");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(g,preview);g.transform.position=p;g.transform.LookAt(Vector3.zero);var l=g.AddComponent<Light>();l.type=LightType.Directional;l.intensity=new[]{.9f,.3f,.4f}[li++];}
    rt=new RenderTexture(800,700,24);rt.antiAliasing=4;rt.Create();cam.targetTexture=rt;img=new Texture2D(800,700,TextureFormat.RGB24,false);
    foreach(var s in specs)
    {
        string id=(string)s["model"];var pf=AssetDatabase.LoadAssetAtPath<GameObject>(art+"/Prefabs/"+(string)s["prefab"]+".prefab");check(pf!=null,id+" prefab exists");if(pf==null)continue;
        var go=(GameObject)PrefabUtility.InstantiatePrefab(pf,preview);var all=go.GetComponentsInChildren<Transform>(true);var skins=go.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        var baseP=all.Select(t=>t.localPosition).ToArray();var baseR=all.Select(t=>t.localRotation).ToArray();var baseS=all.Select(t=>t.localScale).ToArray();var baseA=all.Select(t=>t.gameObject.activeSelf).ToArray();
        System.Action reset=()=>{for(int i=0;i<all.Length;i++){all[i].localPosition=baseP[i];all[i].localRotation=baseR[i];all[i].localScale=baseS[i];all[i].gameObject.SetActive(baseA[i]);}foreach(var sk in skins)for(int b=0;b<sk.sharedMesh.blendShapeCount;b++)sk.SetBlendShapeWeight(b,0);};
        System.Func<Vector3[]> points=()=>{var list=new System.Collections.Generic.List<Vector3>();foreach(var r in go.GetComponentsInChildren<Renderer>(true)){if(!r.gameObject.activeInHierarchy)continue;var f=r.GetComponent<MeshFilter>();var sk=r as SkinnedMeshRenderer;Mesh m=null;bool temp=false;if(f!=null)m=f.sharedMesh;else if(sk!=null){m=new Mesh();sk.BakeMesh(m);temp=true;}if(m!=null){list.AddRange(m.vertices.Select(v=>r.transform.TransformPoint(v)));if(temp)UnityEngine.Object.DestroyImmediate(m);}}return list.ToArray();};
        System.Func<Bounds> bounds=()=>{var pts=points();var b=new Bounds(pts[0],Vector3.zero);foreach(var v in pts)b.Encapsulate(v);return b;};
        var bb=bounds();check(all.All(t=>Vector3.Distance(t.localScale,Vector3.one)<1e-5f),id+" unit scales no mirrors");
        check(go.GetComponentsInChildren<Collider>(true).All(c=>!c.enabled),id+" all suggested colliders disabled");check(go.GetComponent<Animator>()==null,id+" no autonomous animator");
        check(go.GetComponentsInChildren<Renderer>(true).All(r=>r.sharedMaterials.All(m=>m!=null&&m.shader!=null&&m.shader.name!="Hidden/InternalErrorShader")),id+" valid materials");
        foreach(var a in s["anchors"]){var t=all.FirstOrDefault(x=>x.name==(string)a["name"]);check(t!=null,id+" anchor "+(string)a["name"]);}
        foreach(var r in go.GetComponentsInChildren<Renderer>(true))
        {
            var f=r.GetComponent<MeshFilter>();var sk=r as SkinnedMeshRenderer;var m=f!=null?f.sharedMesh:sk!=null?sk.sharedMesh:null;if(m==null)continue;
            check(m.vertexCount>0&&m.uv.Length==m.vertexCount&&m.normals.Length==m.vertexCount,id+" complete mesh channels "+r.name);
            check(m.vertices.All(v=>!float.IsNaN(v.x)&&!float.IsInfinity(v.x)&&!float.IsNaN(v.y)&&!float.IsInfinity(v.y)&&!float.IsNaN(v.z)&&!float.IsInfinity(v.z)),id+" finite vertices "+r.name);
            var vv=m.vertices;var tt=m.triangles;int degenerate=0;for(int k=0;k<tt.Length;k+=3)if(Vector3.Cross(vv[tt[k+1]]-vv[tt[k]],vv[tt[k+2]]-vv[tt[k]]).sqrMagnitude<1e-24f)degenerate++;check(degenerate==0,id+" nondegenerate triangles "+r.name+" count="+degenerate);
        }
        System.Action<string,Vector3> render=(name,direction)=>{var b=bounds();cam.transform.position=b.center+direction.normalized*2;cam.transform.LookAt(b.center);var q=Quaternion.Inverse(cam.transform.rotation);var p=points().Select(v=>q*(v-b.center)).ToArray();float mx=p.Max(v=>Mathf.Abs(v.x)),my=p.Max(v=>Mathf.Abs(v.y));cam.orthographicSize=Mathf.Max(my,mx/(800f/700))*1.18f+.002f;// Bake presentation copies so repeated same-frame captures cannot reuse stale GPU morph data.
        var bakedObjects=new System.Collections.Generic.List<GameObject>();var bakedMeshes=new System.Collections.Generic.List<Mesh>();var enabledStates=skins.Select(sk=>sk.enabled).ToArray();
        foreach(var sk in skins){if(!sk.gameObject.activeInHierarchy||!sk.enabled)continue;var mesh=new Mesh();sk.BakeMesh(mesh);var tmp=new GameObject("BakedPreview");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(tmp,preview);tmp.transform.SetParent(sk.transform,false);tmp.AddComponent<MeshFilter>().sharedMesh=mesh;var rr=tmp.AddComponent<MeshRenderer>();rr.sharedMaterials=sk.sharedMaterials;var mpb=new MaterialPropertyBlock();sk.GetPropertyBlock(mpb,0);rr.SetPropertyBlock(mpb,0);sk.enabled=false;bakedObjects.Add(tmp);bakedMeshes.Add(mesh);}
        cam.Render();foreach(var tmp in bakedObjects)UnityEngine.Object.DestroyImmediate(tmp);foreach(var mesh in bakedMeshes)UnityEngine.Object.DestroyImmediate(mesh);for(int i=0;i<skins.Length;i++)skins[i].enabled=enabledStates[i];var old=RenderTexture.active;RenderTexture.active=rt;img.ReadPixels(new Rect(0,0,800,700),0,0);img.Apply();RenderTexture.active=old;System.IO.File.WriteAllBytes(output+"/previews/"+id+"_"+name+".png",img.EncodeToPNG());};
        render("Default",new Vector3(.6f,.5f,-1.4f));render("Back",new Vector3(-.7f,.6f,1));render("Bottom",new Vector3(.5f,-1,.6f));
        foreach(var c in s["clips"])
        {
            string name=(string)c["name"];var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(art+"/Animations/"+name+".anim");check(clip!=null,name+" imported");if(clip==null)continue;
            var bs=AnimationUtility.GetCurveBindings(clip);check(bs.All(b=>b.path!=""&&!b.path.Contains("Anchor")&&!b.propertyName.Contains("Scale")),name+" never animates root/anchors/scale");
            foreach(var binding in bs)
            {var t=go.transform.Find(binding.path);check(t!=null,name+" bound path "+binding.path);if(binding.type==typeof(SkinnedMeshRenderer)&&t!=null){var sk=t.GetComponent<SkinnedMeshRenderer>();check(sk!=null&&sk.sharedMesh.GetBlendShapeIndex(binding.propertyName.Substring("blendShape.".Length))>=0,name+" morph "+binding.propertyName);}}
            reset();clip.SampleAnimation(go,.37f*clip.length);var ps=all.Select(t=>t.localPosition).ToArray();var qs=all.Select(t=>t.localRotation).ToArray();var acts=all.Select(t=>t.gameObject.activeSelf).ToArray();var weights=skins.Select(sk=>Enumerable.Range(0,sk.sharedMesh.blendShapeCount).Select(sk.GetBlendShapeWeight).ToArray()).ToArray();
            clip.SampleAnimation(go,.83f*clip.length);clip.SampleAnimation(go,.37f*clip.length);
            check(all.Select((t,i)=>Vector3.Distance(t.localPosition,ps[i])<1e-6f&&Quaternion.Angle(t.localRotation,qs[i])<.005f&&t.gameObject.activeSelf==acts[i]).All(v=>v),name+" reversible intermediate transforms");
            check(skins.Select((sk,i)=>Enumerable.Range(0,sk.sharedMesh.blendShapeCount).All(j=>Mathf.Abs(sk.GetBlendShapeWeight(j)-weights[i][j])<.0001f)).All(v=>v),name+" reversible morphs");
            check(go.transform.localPosition==Vector3.zero&&Quaternion.Angle(go.transform.localRotation,Quaternion.identity)<.001f,name+" root stationary");
            reset();clip.SampleAnimation(go,.5f*clip.length);render(name+"_050",new Vector3(.6f,.5f,-1.4f));reset();clip.SampleAnimation(go,clip.length);render(name+"_100",new Vector3(.6f,.5f,-1.4f));
            var rest=AssetDatabase.LoadAssetAtPath<AnimationClip>(art+"/Animations/AN_"+id+"_RestAll.anim");check(rest!=null,id+" complete cancel/reset clip exists");
            if(rest!=null){rest.SampleAnimation(go,1);check(all.Select((t,j)=>Vector3.Distance(t.localPosition,baseP[j])<1e-6f&&Quaternion.Angle(t.localRotation,baseR[j])<.005f&&t.gameObject.activeSelf==baseA[j]).All(v=>v)&&skins.All(sk=>Enumerable.Range(0,sk.sharedMesh.blendShapeCount).All(j=>Mathf.Abs(sk.GetBlendShapeWeight(j))<.0001f)),name+" cancel restores complete default pose");}
        }
        UnityEngine.Object.DestroyImmediate(go);
    }
    foreach(var path in System.IO.Directory.GetFiles(art+"/Textures","*_Normal.png")){var ti=(TextureImporter)AssetImporter.GetAtPath(path);check(ti.textureType==TextureImporterType.NormalMap&&!ti.sRGBTexture,"Normal import "+path);}
    check(activeScene.isDirty==dirty,"Production scene dirty state unchanged");
    string report=Newtonsoft.Json.JsonConvert.SerializeObject(new{status=failures.Count==0?"PASS":"FAIL",checks=checks.Count,failures=failures,details=checks,scope="Actual Unity imported independent prefabs, sampled clips, isolated previews; no gameplay integration or player build"},Newtonsoft.Json.Formatting.Indented);System.IO.File.WriteAllText(output+"/validation/unity_checks.json",report);return new{checks=checks.Count,failures=failures.Count};
}
finally{if(img!=null)UnityEngine.Object.DestroyImmediate(img);if(rt!=null){rt.Release();UnityEngine.Object.DestroyImmediate(rt);}UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(preview);}

}

static object TestDriver(){
string art="Assets/Art/Props/AlbumWinterE",outdir="output/E_AlbumWinter_v1";
var pass=new System.Collections.Generic.List<string>();var fail=new System.Collections.Generic.List<string>();
System.Action<bool,string> check=(b,s)=>{if(b)pass.Add(s);else fail.Add(s);};
var type=System.AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("MemoryWhitebox.Art.AlbumWinterVisual")).FirstOrDefault(t=>t!=null);
check(type!=null,"Thin externally sampled visual API exists");
if(type!=null){
 var scene=UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
 try{foreach(string path in System.IO.Directory.GetFiles(art+"/Prefabs","*.prefab")){
  var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path),scene);var v=go.GetComponent(type);check(v!=null,path+" driver");if(v==null)continue;
  var all=go.GetComponentsInChildren<Transform>(true);System.Func<string,Transform> find=n=>all.First(t=>t.name==n);
  System.Action<string,object[]> call=(n,args)=>type.GetMethod(n).Invoke(v,args);
  System.Func<string,object[],object> get=(n,args)=>type.GetMethod(n).Invoke(v,args);
  if(path.Contains("Swing"))foreach(float a in new[]{-.9f,0,.8f}){call("SetSwingAngleRadians",new object[]{a});var p=find("SwingSeatAnchor").position-find("SwingPivot").position;check(Mathf.Abs(p.x-.3f*Mathf.Sin(a))<.0001f&&Mathf.Abs(p.y+.3f*Mathf.Cos(a))<.0001f,"Swing exact gameplay seat "+a);check(Mathf.Abs(p.magnitude-.3f)<.0001f,"Swing fixed suspension length");}
  if(path.Contains("DaisyWheel"))foreach(float a in new[]{-12.7f,-1.57f,0,6.6f,22f}){call("SetFlowerAngleRadians",new object[]{a});for(int i=0;i<6;i++){var seat=find("RideAnchor_"+i);var centre=find("WheelPivot").position;var p=seat.position-centre;check(Mathf.Abs(p.x-.2f*Mathf.Cos(a+i*Mathf.PI/3))<.0001f&&Mathf.Abs(p.y-.2f*Mathf.Sin(a+i*Mathf.PI/3))<.0001f,"Wheel follows exact circle "+i+" "+a);check(Vector3.Dot(seat.up,Vector3.up)>.99999f,"Wheel platform remains upright");}}
  if(path.Contains("Counterweight"))foreach(float u in new[]{0,.37f,1,.12f}){call("SetLoadProgress",new object[]{u});check(Mathf.Abs(find("GateLift").localPosition.y-(.1f+.22f*u))<.00001f,"Gate synchronised lift "+u);check(Mathf.Abs(find("Counterweight").localPosition.y-(.4f-.22f*u))<.00001f,"Counterweight opposite travel "+u);}
  if(path.Contains("Album_E01")){var anchor=find("WorldPresentationAnchor");var p=anchor.position;call("SetTurnProgress",new object[]{.53f});call("SetOpenProgress",new object[]{.42f});check(anchor.position==p,"World presentation anchor independent of page and book");}
  if(path.Contains("PendantDoor")){call("SetDoorState",new object[]{.5f,.4f,.2f,true});check(find("KeyVisual").gameObject.activeSelf,"Explicit key visibility");check(Mathf.Abs(find("KeyInsertPivot").localPosition.z+.027f)<.00001f,"Key insertion sampled axial midpoint");call("SetDoorState",new object[]{0f,0f,0f,false});check(!find("KeyVisual").gameObject.activeSelf,"Cancel can hide nested key");}
  call("SetBridgeProgress",new object[]{.37f});call("SetLoadProgress",new object[]{.37f});call("SetOpenProgress",new object[]{.63f});call("SetTurnProgress",new object[]{.41f});call("SetActorPose",new object[]{"Walk",.37f,90f});
  var state=get("Capture",new object[0]);var ps=all.Select(t=>t.localPosition).ToArray();var qs=all.Select(t=>t.localRotation).ToArray();
  call("ResetVisual",new object[0]);call("Restore",new[]{state});check(all.Select((t,i)=>Vector3.Distance(t.localPosition,ps[i])<1e-6f&&Quaternion.Angle(t.localRotation,qs[i])<.01f).All(x=>x),path+" full capture/restore");
  check(go.transform.localPosition==Vector3.zero&&go.transform.localRotation==Quaternion.identity&&go.transform.localScale==Vector3.one,path+" stable root");
  check(go.GetComponentsInChildren<Animator>(true).Length==0,path+" no conflicting Animator");check(go.GetComponentsInChildren<Collider>(true).All(c=>!c.enabled),path+" disabled hit and physics helpers");
  if(path.Contains("AlbumActor")){var skins=go.GetComponentsInChildren<SkinnedMeshRenderer>();check(skins.Length>0&&skins.All(s=>s.bones.Length==15&&s.sharedMesh.bindposes.Length==15),"Unified 15-bone imported actor skeleton retained");var m=new Mesh();call("SetActorPose",new object[]{"Idle",0f,0f});skins.First(s=>s.name.StartsWith("Palm")).BakeMesh(m);var a=m.vertices;call("SetActorPose",new object[]{"Walk",0f,0f});skins.First(s=>s.name.StartsWith("Palm")).BakeMesh(m);check(a.Zip(m.vertices,(x,y)=>Vector3.Distance(x,y)).Max()>.0001f,"Real skeletal deformation changes skin");UnityEngine.Object.DestroyImmediate(m);}
  UnityEngine.Object.DestroyImmediate(go);
 }}finally{UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);}
}
System.IO.File.WriteAllText(outdir+"/validation/driver_checks.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{status=fail.Count==0?"PASS":"FAIL",checks=pass.Count,failures=fail,details=pass},Newtonsoft.Json.Formatting.Indented));return new{checks=pass.Count,failures=fail};

}

static object Mechanics(){
string art="Assets/Art/Props/AlbumWinterE",output="output/E_AlbumWinter_v1";var passed=new System.Collections.Generic.List<string>();var failed=new System.Collections.Generic.List<string>();System.Action<bool,string> check=(ok,n)=>{if(ok)passed.Add(n);else failed.Add(n);};
var scene=UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();var trash=new System.Collections.Generic.List<UnityEngine.Object>();
try{
 System.Func<string,GameObject> make=n=>(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(art+"/Prefabs/PF_"+n+".prefab"),scene);
 System.Func<GameObject,string,Transform> find=(g,n)=>g.GetComponentsInChildren<Transform>(true).First(t=>t.name==n);
 // User deferred humanoid action/contact adjustment on 2026-11-02. Preserve the rig;
 // do not report its unfinished sitting/hand contact as an accepted animation.
 var gate=make("CounterweightGate_E05");var gv=gate.GetComponent<MemoryWhitebox.Art.AlbumWinterVisual>();var cable=gate.GetComponentsInChildren<SkinnedMeshRenderer>(true).First(r=>r.name=="CableMesh");var baked=new Mesh();trash.Add(baked);
 foreach(float u in new[]{0,.37f,1,.13f}){gv.SetLoadProgress(u);cable.BakeMesh(baked);var vertices=baked.vertices.Select(p=>cable.transform.TransformPoint(p)).ToArray();foreach(string n in new[]{"GateTopAnchor","WeightHook"}){float d=vertices.Min(p=>Vector3.Distance(p,find(gate,n).position));check(d<.0017f,"Cable endpoint "+n+" connected at "+u+", metres="+d);}}
 UnityEngine.Object.DestroyImmediate(gate);
 var book=make("Album_E01");var bv=book.GetComponent<MemoryWhitebox.Art.AlbumWinterVisual>();var other=make("Album_E01");other.SetActive(false);var ov=other.GetComponent<MemoryWhitebox.Art.AlbumWinterVisual>();
 var tex=new Texture2D(128,72,TextureFormat.RGBA32,false);trash.Add(tex);for(int y=0;y<72;y++)for(int x=0;x<128;x++)tex.SetPixel(x,y,y>=36?(x<64?Color.red:Color.green):(x<64?Color.blue:Color.yellow));tex.Apply();bv.SetPageTexture(tex);
 var cameraGO=new GameObject("MechanicalProofCamera");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cameraGO,scene);var cam=cameraGO.AddComponent<Camera>();cam.scene=scene;cam.enabled=false;cam.orthographic=true;cam.orthographicSize=.24f;cam.aspect=1.25f;cam.nearClipPlane=.001f;cam.farClipPlane=4;cam.transform.position=Vector3.up;cam.transform.rotation=Quaternion.LookRotation(Vector3.down,Vector3.back);cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=Color.gray;
 var rt=new RenderTexture(800,640,24);rt.Create();trash.Add(rt);cam.targetTexture=rt;var img=new Texture2D(800,640,TextureFormat.RGB24,false);trash.Add(img);
 foreach(float u in new[]{0,.5f,1}){
  bv.SetTurnProgress(u);var temp=new System.Collections.Generic.List<GameObject>();var skins=book.GetComponentsInChildren<SkinnedMeshRenderer>();var meshes=new System.Collections.Generic.List<Mesh>();
  foreach(var sk in skins){var m=new Mesh();sk.BakeMesh(m);meshes.Add(m);var g=new GameObject("SampledPage");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(g,scene);g.transform.SetParent(sk.transform,false);g.AddComponent<MeshFilter>().sharedMesh=m;var r=g.AddComponent<MeshRenderer>();r.sharedMaterials=sk.sharedMaterials;for(int i=0;i<r.sharedMaterials.Length;i++){var b=new MaterialPropertyBlock();sk.GetPropertyBlock(b,i);r.SetPropertyBlock(b,i);}sk.enabled=false;temp.Add(g);}
  cam.Render();var old=RenderTexture.active;RenderTexture.active=rt;img.ReadPixels(new Rect(0,0,800,640),0,0);img.Apply();RenderTexture.active=old;
  foreach(var g in temp)UnityEngine.Object.DestroyImmediate(g);foreach(var m in meshes)UnityEngine.Object.DestroyImmediate(m);foreach(var sk in skins)sk.enabled=true;
  System.IO.File.WriteAllBytes(output+"/previews/E01_UV_"+Mathf.RoundToInt(u*100).ToString("D3")+".png",img.EncodeToPNG());
  if(u!=.5f){var positions=new[]{new Vector3(.2f,.02f,-.08f),new Vector3(-.2f,.02f,-.08f),new Vector3(.2f,.02f,.08f),new Vector3(-.2f,.02f,.08f)};var expected=new[]{Color.red,Color.green,Color.blue,Color.yellow};for(int k=0;k<4;k++){var pos=cam.WorldToScreenPoint(positions[k]);var color=img.GetPixel(Mathf.RoundToInt(pos.x),Mathf.RoundToInt(pos.y));check(Vector3.Distance(new Vector3(color.r,color.g,color.b),new Vector3(expected[k].r,expected[k].g,expected[k].b))<.08f,"Upright page quadrant "+k+" at turn "+u+": "+color);}}
 }
 check(ov.Capture().pageTexture==null&&ov.Capture().turn==0,"Second book retains independent texture and pose state");foreach(var r in other.GetComponentsInChildren<Renderer>(true).Where(r=>r.sharedMaterials.Any(m=>m.name=="M_E_PageContent"))){var b=new MaterialPropertyBlock();r.GetPropertyBlock(b,0);check(b.GetFloat("_ContentAmount")==0,"Second book material block blank "+r.name);}
 bv.SetTurnProgress(float.NaN);bv.SetOpenProgress(float.PositiveInfinity);check(bv.Capture().turn==0&&bv.Capture().open==0,"Non-finite inputs sanitised");
 foreach(string guid in AssetDatabase.FindAssets("t:Prefab",new[]{art+"/Prefabs"})){var p=AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));check(p.GetComponentsInChildren<Transform>(true).All(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)==0),p.name+" has no Missing Script");}
 foreach(string guid in AssetDatabase.FindAssets("t:Shader",new[]{art})){var shader=AssetDatabase.LoadAssetAtPath<Shader>(AssetDatabase.GUIDToAssetPath(guid));check(!ShaderUtil.ShaderHasError(shader),shader.name+" shader compiles");}
}finally{foreach(var o in trash){if(o is RenderTexture)((RenderTexture)o).Release();UnityEngine.Object.DestroyImmediate(o);}UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);}
System.IO.File.WriteAllText(output+"/validation/mechanical_checks.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{status=failed.Count==0?"PASS":"FAIL",scope="E01,E03-E07 prop mechanics",deferred=new[]{"E02 humanoid animation and hand/seat contacts: user deferred 2026-11-02"},checks=passed.Count,failures=failed,details=passed},Newtonsoft.Json.Formatting.Indented));return new{checks=passed.Count,failures=failed};

}

static object Showcase(){
string art="Assets/Art/Props/AlbumWinterE",output="output/E_AlbumWinter_v1";
var previous=UnityEngine.SceneManagement.SceneManager.GetActiveScene();bool oldDirty=previous.isDirty;
var occupied=UnityEngine.Object.FindObjectsOfType<Renderer>(true).Select(r=>r.gameObject.layer).Distinct().ToArray();int previewLayer=Enumerable.Range(8,24).Last(x=>!occupied.Contains(x));
System.Action<GameObject> isolate=g=>{foreach(var r in g.GetComponentsInChildren<Renderer>(true))r.gameObject.layer=previewLayer;};
var scene=UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,UnityEditor.SceneManagement.NewSceneMode.Additive);
var resources=new System.Collections.Generic.List<UnityEngine.Object>();
try{
 var collection=new GameObject("AlbumWinter_AssetPreview");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(collection,scene);
 var specs=Newtonsoft.Json.Linq.JArray.Parse(System.IO.File.ReadAllText(output+"/source/assets.json"));var objects=new System.Collections.Generic.List<GameObject>();
 foreach(var s in specs){var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(art+"/Prefabs/"+(string)s["prefab"]+".prefab"),scene);go.transform.SetParent(collection.transform,false);go.transform.localPosition=new Vector3((objects.Count%4)*.7f,0,(objects.Count/4)*.7f);objects.Add(go);}
 foreach(var go in objects)isolate(go);
 var cg=new GameObject("Camera_Preview");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cg,scene);var cam=cg.AddComponent<Camera>();cam.scene=scene;cam.cullingMask=1<<previewLayer;cam.enabled=false;cam.orthographic=true;cam.nearClipPlane=.001f;cam.farClipPlane=20;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.13f,.145f,.14f);cam.allowHDR=false;
 int li=0;foreach(var p in new[]{new Vector3(-2,4,-3),new Vector3(3,2,-1),new Vector3(0,3,2)}){var g=new GameObject("Light_Preview_"+li);UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(g,scene);g.transform.position=p;g.transform.LookAt(Vector3.zero);var l=g.AddComponent<Light>();l.type=LightType.Directional;l.cullingMask=1<<previewLayer;l.intensity=new[]{1.1f,.4f,.6f}[li++];}
 var rt=new RenderTexture(640,560,24){antiAliasing=4};rt.Create();resources.Add(rt);var image=new Texture2D(640,560,TextureFormat.RGB24,false);resources.Add(image);cam.targetTexture=rt;
 System.Action<string> shot=name=>{
  // Explicit fresh skin snapshots avoid same-frame GPU skin/morph caching in editor capture.
  var temp=new System.Collections.Generic.List<GameObject>();var meshes=new System.Collections.Generic.List<Mesh>();var skins=objects.Where(g=>g.activeSelf).SelectMany(g=>g.GetComponentsInChildren<SkinnedMeshRenderer>()).Where(s=>s.enabled).ToArray();
  foreach(var sk in skins){var m=new Mesh();sk.BakeMesh(m);var t=new GameObject("CaptureSkin");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(t,scene);t.transform.SetParent(sk.transform,false);t.layer=previewLayer;t.AddComponent<MeshFilter>().sharedMesh=m;var r=t.AddComponent<MeshRenderer>();r.sharedMaterials=sk.sharedMaterials;for(int j=0;j<r.sharedMaterials.Length;j++){var b=new MaterialPropertyBlock();sk.GetPropertyBlock(b,j);r.SetPropertyBlock(b,j);}sk.enabled=false;temp.Add(t);meshes.Add(m);}
  cam.Render();foreach(var t in temp)UnityEngine.Object.DestroyImmediate(t);foreach(var m in meshes)UnityEngine.Object.DestroyImmediate(m);foreach(var s in skins)s.enabled=true;
  var old=RenderTexture.active;RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,640,560),0,0);image.Apply();RenderTexture.active=old;System.IO.File.WriteAllBytes(output+"/previews/"+name+".png",image.EncodeToPNG());
 };
 for(int index=0;index<objects.Count;index++){
  var go=objects[index];foreach(var o in objects)o.SetActive(o==go);var v=go.GetComponent<MemoryWhitebox.Art.AlbumWinterVisual>();string id=(string)specs[index]["model"];
  Vector3 target=go.transform.position+new Vector3(id.StartsWith("E05")?.18f:0,id.StartsWith("E01")?0:id.StartsWith("E02")?.05f:id.StartsWith("E07")?.12f:.20f,0);
  if(id=="E01_Album")target.y+=.055f;
  cam.transform.position=target+(id.StartsWith("E01")?new Vector3(.12f,.75f,.6f):new Vector3(.22f,.16f,-.8f));cam.transform.LookAt(target);cam.orthographicSize=id.StartsWith("E01")?.29f:id.StartsWith("E02")?.065f:id.StartsWith("E07")?.15f:.30f;
  if(id.StartsWith("E04")){target=go.transform.position;cam.transform.position=target+new Vector3(.2f,.3f,-.5f);cam.transform.LookAt(target);cam.orthographicSize=.20f;}
  for(int frame=0;frame<32;frame++){float u=frame/31f;v.ResetVisual();if(id=="E01_Album")v.SetTurnProgress(u);else if(id.StartsWith("E02"))v.SetActorPose("Walk",u,90);else if(id.StartsWith("E03"))v.SetSwingAngleRadians(Mathf.Sin(u*Mathf.PI*2)*1.05f);else if(id.StartsWith("E04"))v.SetBridgeProgress(u<.5f?u*2:2-u*2);else if(id.StartsWith("E05"))v.SetLoadProgress(u<.5f?u*2:2-u*2);else if(id.StartsWith("E06"))v.SetFlowerAngleRadians(u*Mathf.PI*2);else if(id.StartsWith("E07"))v.SetDoorState(Mathf.Clamp01(u*3),Mathf.Clamp01(u*3-1),Mathf.Clamp01(u*3-2),true);shot(id+"_motion_"+frame.ToString("D2"));}
  if(id.StartsWith("E02"))foreach(string action in new[]{"Idle","Walk","Turn","Takeoff","Air","Land","Push","SitSwing","RideFlower","Board","Dismount","KeyReach"}){v.SetActorPose(action,.5f,90);shot(id+"_pose_"+action);}
  v.ResetVisual();
 }
 foreach(var g in objects)g.SetActive(true);
 // A self-contained live RenderTexture example; the camera sees only a duplicated miniature display.
 var world=new GameObject("AlbumWorldAdapter_SourceScale10");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(world,scene);world.transform.position=new Vector3(0,-30,0);world.transform.localScale=Vector3.one*10;
 foreach(int idx in new[]{2,3,4,5,6,7}){var copy=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(art+"/Prefabs/"+(string)specs[idx]["prefab"]+".prefab"),scene);copy.transform.SetParent(world.transform,false);copy.transform.localPosition=new Vector3((idx-2)*.55f,0,0);if(idx==6)copy.GetComponent<MemoryWhitebox.Art.AlbumWinterVisual>().SetFlowerAngleRadians(-Mathf.PI/2);}
 isolate(world);var wc=new GameObject("Camera_AlbumWorld");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(wc,scene);wc.transform.position=new Vector3(13.2f,-27.5f,-16);var camera=wc.AddComponent<Camera>();camera.cullingMask=1<<previewLayer;camera.orthographic=true;camera.orthographicSize=8.8f;camera.aspect=16f/9;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.35f,.36f,.32f);camera.nearClipPlane=.1f;camera.farClipPlane=40;
 string rtpath=art+"/Preview/AlbumWorldPreview.renderTexture";var live=AssetDatabase.LoadAssetAtPath<RenderTexture>(rtpath);if(live==null){live=new RenderTexture(1280,720,24);AssetDatabase.CreateAsset(live,rtpath);}camera.targetTexture=live;camera.Render();
 objects[0].GetComponent<MemoryWhitebox.Art.AlbumWinterVisual>().SetPageTexture(live);objects[1].GetComponent<MemoryWhitebox.Art.AlbumWinterVisual>().SetPageTexture(live);
 foreach(var g in objects)g.SetActive(g==objects[0]);cam.transform.position=objects[0].transform.position+Vector3.up;cam.transform.rotation=Quaternion.LookRotation(Vector3.down,Vector3.back);cam.orthographicSize=.245f;shot("E01_ActualUnity_RenderTexture");
 foreach(var g in objects)g.SetActive(true);cam.targetTexture=null;cam.enabled=true;cam.transform.position=new Vector3(1.05f,2.2f,-2.8f);cam.transform.LookAt(new Vector3(1.05f,.10f,.35f));cam.orthographicSize=1.55f;
 // No state-changing gameplay scripts in this scene.
 string scenePath=art+"/Preview/AlbumWinterPreview.unity";bool safe=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MonoBehaviour>(true)).All(c=>c!=null&&(c is MemoryWhitebox.Art.AlbumWinterVisual||c is MemoryWhitebox.Art.WritingPropVisual));
 if(!safe)throw new System.Exception("Unexpected non-presentation component in preview");bool saved=UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene,scenePath);AssetDatabase.SaveAssetIfDirty(live);
 System.IO.File.WriteAllText(output+"/validation/preview_scene.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{saved,path=scenePath,safePresentationOnly=safe,originalScene=previous.path,originalDirtyUnchanged=previous.isDirty==oldDirty,frames=256,source="Actual Unity Camera.Render via MCP; manually sampled poses, no game flow"},Newtonsoft.Json.Formatting.Indented));return new{saved,safe};
}finally{foreach(var x in resources){if(x is RenderTexture)((RenderTexture)x).Release();UnityEngine.Object.DestroyImmediate(x);}if(previous.IsValid()&&previous.isLoaded)UnityEngine.SceneManagement.SceneManager.SetActiveScene(previous);UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene,true);}

}
}}