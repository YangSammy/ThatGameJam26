using UnityEngine;
using UnityEditor;
using System.Linq;
namespace MemoryWhitebox.Art.Editor { public static class WritingDDeliveryBuild {
public static void Run(){try{
Debug.Log("WritingD Import "+Newtonsoft.Json.JsonConvert.SerializeObject(Import()));
Debug.Log("WritingD Validate "+Newtonsoft.Json.JsonConvert.SerializeObject(Validate()));
Debug.Log("WritingD TestDriver "+Newtonsoft.Json.JsonConvert.SerializeObject(TestDriver()));
Debug.Log("WritingD PreviewScene "+Newtonsoft.Json.JsonConvert.SerializeObject(PreviewScene()));
Debug.Log("WritingD Details "+Newtonsoft.Json.JsonConvert.SerializeObject(Details()));
foreach(string f in new[]{"unity_checks.json","driver_checks.json","mechanical_checks.json"}){var j=Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText("output/D_WritingProps_v1/validation/"+f));if((string)j["status"]!="PASS")throw new System.Exception("Validation failed: "+f);}
System.IO.File.WriteAllText("output/D_WritingProps_v1/validation/batch_complete.json",Newtonsoft.Json.JsonConvert.SerializeObject(new {status="PASS",version=Application.unityVersion,pipeline=UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline==null?"Built-in":"SRP",batch=Application.isBatchMode}));
}catch(System.Exception e){Debug.LogException(e);throw;} }
static object Import(){
// Unity MCP method body. Asset creation only; never edits a production scene.
string art="Assets/Art/Props/WritingD";
AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
var specs=Newtonsoft.Json.Linq.JArray.Parse(System.IO.File.ReadAllText("output/D_WritingProps_v1/source/assets.json"));
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
    if(name=="M_D_Optics"||name=="M_D_Water")
    {mat.SetFloat("_Mode",3);mat.SetInt("_SrcBlend",(int)UnityEngine.Rendering.BlendMode.One);mat.SetInt("_DstBlend",(int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);mat.SetInt("_ZWrite",0);mat.EnableKeyword("_ALPHAPREMULTIPLY_ON");mat.renderQueue=3000;}
    mat.SetTexture("_MainTex",AssetDatabase.LoadAssetAtPath<Texture2D>(path));
    mat.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(art+"/Textures/"+name+"_Normal.png"));mat.SetFloat("_BumpScale",.55f);mat.EnableKeyword("_NORMALMAP");
    mat.SetTexture("_MetallicGlossMap",AssetDatabase.LoadAssetAtPath<Texture2D>(art+"/Textures/"+name+"_MetallicSmoothness.png"));mat.SetFloat("_GlossMapScale",1);mat.EnableKeyword("_METALLICGLOSSMAP");
    mat.SetTexture("_OcclusionMap",AssetDatabase.LoadAssetAtPath<Texture2D>(art+"/Textures/"+name+"_AO.png"));mat.SetFloat("_OcclusionStrength",1);
    if(name=="M_D_InkContent") {mat.shader=Shader.Find("ThatGamejam/Art/WritingInk");mat.SetTexture("_PaperTex",AssetDatabase.LoadAssetAtPath<Texture2D>(art+"/Textures/M_D_Paper_BaseColor.png"));mat.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(art+"/Textures/M_D_Paper_Normal.png"));mat.SetTexture("_MainTex",Texture2D.whiteTexture);mat.SetFloat("_InkAmount",0);}
    EditorUtility.SetDirty(mat);AssetDatabase.SaveAssetIfDirty(mat);mats[name]=mat;
}
var report=new System.Collections.Generic.List<object>();
foreach(var spec in specs)
{
    string id=(string)spec["model"];string modelPath=art+"/Models/"+id+".fbx";
    var imp=(ModelImporter)AssetImporter.GetAtPath(modelPath);imp.bakeAxisConversion=false;imp.globalScale=1;imp.useFileScale=true;imp.importAnimation=false;imp.importCameras=false;imp.importLights=false;imp.importBlendShapes=true;imp.importNormals=ModelImporterNormals.Import;imp.importTangents=ModelImporterTangents.CalculateMikk;imp.isReadable=true;imp.meshCompression=ModelImporterMeshCompression.Off;
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
            mesh.RecalculateBounds();string meshPath=art+"/Meshes/"+mesh.name+".asset";var existing=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            if(existing==null)AssetDatabase.CreateAsset(mesh,meshPath);else{EditorUtility.CopySerialized(mesh,existing);UnityEngine.Object.DestroyImmediate(mesh);mesh=existing;}
            // CopySerialized updates CPU mesh data, but can retain obsolete GPU buffers in this Editor session.
            mesh.vertices=mesh.vertices;mesh.normals=mesh.normals;mesh.uv=mesh.uv;mesh.tangents=mesh.tangents;for(int i=0;i<mesh.subMeshCount;i++)mesh.SetTriangles(mesh.GetTriangles(i),i,false);mesh.UploadMeshData(false);EditorUtility.SetDirty(mesh);
            if(mf!=null)mf.sharedMesh=mesh;else{sk.sharedMesh=mesh;sk.rootBone=null;sk.bones=new Transform[0];sk.updateWhenOffscreen=true;sk.localBounds=new Bounds(mesh.bounds.center,mesh.bounds.size+Vector3.one*.4f);}
            ren.sharedMaterials=ren.sharedMaterials.Select(m=>mats[m.name.Replace(" (Instance)","")]).ToArray();
        }
        all=root.GetComponentsInChildren<Transform>(true);
        System.Func<string,Transform> find=n=>all.First(t=>t.name==n);
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
                var times=cv["times"].Select(v=>(float)v).ToArray();var vals=cv["values"].Select(v=>(float)v).ToArray();var curve=new AnimationCurve(times.Select((time,i)=>new Keyframe(time,vals[i])).ToArray());
                for(int i=0;i<curve.length;i++){AnimationUtility.SetKeyLeftTangentMode(curve,i,tn=="GameObject"?AnimationUtility.TangentMode.Constant:AnimationUtility.TangentMode.Linear);AnimationUtility.SetKeyRightTangentMode(curve,i,tn=="GameObject"?AnimationUtility.TangentMode.Constant:AnimationUtility.TangentMode.Linear);}
                AnimationUtility.SetEditorCurve(ac,EditorCurveBinding.FloatCurve(AnimationUtility.CalculateTransformPath(t,root.transform),type,(string)cv["property"]),curve);
            }
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
        var driver=root.AddComponent<MemoryWhitebox.Art.WritingPropVisual>();
        var channelNames=new System.Collections.Generic.Dictionary<string,MemoryWhitebox.Art.WritingPropVisual.Channel>{{"Crumple",MemoryWhitebox.Art.WritingPropVisual.Channel.Crumple},{"ChainPose",MemoryWhitebox.Art.WritingPropVisual.Channel.ChainPose},{"ChainSway",MemoryWhitebox.Art.WritingPropVisual.Channel.Sway},{"WindSway",MemoryWhitebox.Art.WritingPropVisual.Channel.Sway},{"PickupSettle",MemoryWhitebox.Art.WritingPropVisual.Channel.PickupSettle},{"HandleLift",MemoryWhitebox.Art.WritingPropVisual.Channel.Handle},{"WheelRoll",MemoryWhitebox.Art.WritingPropVisual.Channel.Wheel},{"FlapOpen",MemoryWhitebox.Art.WritingPropVisual.Channel.Flap},{"TimerProgress",MemoryWhitebox.Art.WritingPropVisual.Channel.Timer},{"DialAdjust",MemoryWhitebox.Art.WritingPropVisual.Channel.Dial}};
        var channels=new System.Collections.Generic.List<MemoryWhitebox.Art.WritingPropVisual.Binding>();
        foreach(var cc in spec["clips"]){string cn=(string)cc["name"];string suffix=cn.Substring(cn.LastIndexOf('_')+1);if(channelNames.ContainsKey(suffix))channels.Add(new MemoryWhitebox.Art.WritingPropVisual.Binding{channel=channelNames[suffix],clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(art+"/Animations/"+cn+".anim")});}
        driver.Configure(AssetDatabase.LoadAssetAtPath<AnimationClip>(rp),channels.ToArray(),root.GetComponentsInChildren<Renderer>(true).FirstOrDefault(r=>r.name=="InkSurface"));
        string pf=art+"/Prefabs/"+(string)spec["prefab"]+".prefab";PrefabUtility.SaveAsPrefabAsset(root,pf);
        var usedMaterials=root.GetComponentsInChildren<Renderer>(true).SelectMany(r=>r.sharedMaterials).Distinct().ToArray();
        var textures=usedMaterials.SelectMany(m=>m.GetTexturePropertyNames().Select(m.GetTexture)).Where(t=>t!=null).Distinct().Select(t=>new{path=AssetDatabase.GetAssetPath(t),width=t.width,height=t.height}).Where(t=>t.path.StartsWith("Assets/")).ToArray();
        report.Add(new {asset=id,prefab=pf,triangles=triangles,materials=usedMaterials.Length,materialNames=usedMaterials.Select(m=>m.name).ToArray(),textures=textures,renderers=root.GetComponentsInChildren<Renderer>(true).Length,blendShapes=shapes,clips=spec["clips"].Count()+1,size=new[]{bounds.size.x,bounds.size.y,bounds.size.z},min=new[]{bounds.min.x,bounds.min.y,bounds.min.z},max=new[]{bounds.max.x,bounds.max.y,bounds.max.z}});
    }
    finally{UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);}
}
foreach(string guid in AssetDatabase.FindAssets("",new[]{art})){var a=AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(guid));if(a!=null)AssetDatabase.SaveAssetIfDirty(a);}
System.IO.File.WriteAllText("output/D_WritingProps_v1/source/assets.json",specs.ToString());string json=Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented);System.IO.File.WriteAllText("output/D_WritingProps_v1/validation/unity_import.json",json);return new{prefabs=report.Count};

}

static object Validate(){
string art="Assets/Art/Props/WritingD";string output="output/D_WritingProps_v1";
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
        render("Default",new Vector3(.85f,1.0f,1.15f));render("Back",new Vector3(-.7f,.6f,-1));render("Bottom",new Vector3(.5f,-1,.6f));
        foreach(var c in s["clips"])
        {
            string name=(string)c["name"];var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(art+"/Animations/"+name+".anim");check(clip!=null,name+" imported");if(clip==null)continue;
            var bs=AnimationUtility.GetCurveBindings(clip);check(bs.All(b=>b.path!=""&&!b.path.Contains("Anchor")&&!b.propertyName.Contains("Scale")),name+" never animates root/anchors/scale");
            foreach(var binding in bs)
            {var t=go.transform.Find(binding.path);check(t!=null,name+" bound path "+binding.path);if(binding.type==typeof(SkinnedMeshRenderer)&&t!=null){var sk=t.GetComponent<SkinnedMeshRenderer>();check(sk!=null&&sk.sharedMesh.GetBlendShapeIndex(binding.propertyName.Substring("blendShape.".Length))>=0,name+" morph "+binding.propertyName);}}
            reset();clip.SampleAnimation(go,.37f);var ps=all.Select(t=>t.localPosition).ToArray();var qs=all.Select(t=>t.localRotation).ToArray();var acts=all.Select(t=>t.gameObject.activeSelf).ToArray();var weights=skins.Select(sk=>Enumerable.Range(0,sk.sharedMesh.blendShapeCount).Select(sk.GetBlendShapeWeight).ToArray()).ToArray();
            clip.SampleAnimation(go,.83f);clip.SampleAnimation(go,.37f);
            check(all.Select((t,i)=>Vector3.Distance(t.localPosition,ps[i])<1e-6f&&Quaternion.Angle(t.localRotation,qs[i])<.005f&&t.gameObject.activeSelf==acts[i]).All(v=>v),name+" reversible intermediate transforms");
            check(skins.Select((sk,i)=>Enumerable.Range(0,sk.sharedMesh.blendShapeCount).All(j=>Mathf.Abs(sk.GetBlendShapeWeight(j)-weights[i][j])<.0001f)).All(v=>v),name+" reversible morphs");
            check(go.transform.localPosition==Vector3.zero&&Quaternion.Angle(go.transform.localRotation,Quaternion.identity)<.001f,name+" root stationary");
            reset();clip.SampleAnimation(go,.5f);render(name+"_050",new Vector3(.85f,1,1.15f));reset();clip.SampleAnimation(go,1);render(name+"_100",new Vector3(.85f,1,1.15f));
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
var type=System.AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("MemoryWhitebox.Art.WritingPropVisual")).FirstOrDefault(t=>t!=null);
if(type==null){System.IO.File.WriteAllText("output/D_WritingProps_v1/validation/driver_red.json","{\"status\":\"FAIL\",\"reason\":\"WritingPropVisual presentation API does not exist\"}");return "FAIL: WritingPropVisual presentation API does not exist";}
var fails=new System.Collections.Generic.List<string>();int checks=0;
System.Action<bool,string> check=(ok,s)=>{checks++;if(!ok)fails.Add(s);};
var scene=UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
try{
foreach(string p in System.IO.Directory.GetFiles("Assets/Art/Props/WritingD/Prefabs","*.prefab")){
var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(p),scene);var c=go.GetComponent(type);check(c!=null,p+" presentation component");if(c==null){UnityEngine.Object.DestroyImmediate(go);continue;}
go.transform.position=new Vector3(1,2,3);go.transform.rotation=Quaternion.Euler(12,40,7);
var all=go.GetComponentsInChildren<Transform>(true);var skins=go.GetComponentsInChildren<SkinnedMeshRenderer>(true);
System.Func<string> snapshot=()=>Newtonsoft.Json.JsonConvert.SerializeObject(new {p=all.Select(t=>new[]{t.localPosition.x,t.localPosition.y,t.localPosition.z}).ToArray(),r=all.Select(t=>new[]{t.localRotation.x,t.localRotation.y,t.localRotation.z,t.localRotation.w}).ToArray(),s=skins.Select(x=>Enumerable.Range(0,x.sharedMesh.blendShapeCount).Select(x.GetBlendShapeWeight).ToArray()).ToArray()});
foreach(string method in new[]{"SetCrumpleProgress","SetChainPoseProgress","SetSwayProgress","SetPickupSettleProgress","SetHandleProgress","SetWheelProgress","SetFlapProgress","SetTimerProgress","SetDialProgress"}){
var m=type.GetMethod(method);check(m!=null,method+" exists");if(m==null)continue;
type.GetMethod("ResetVisual").Invoke(c,null);var rest=snapshot();m.Invoke(c,new object[]{.37f});var mid=snapshot();var saved=type.GetMethod("Capture").Invoke(c,null);
m.Invoke(c,new object[]{.83f});m.Invoke(c,new object[]{.37f});check(snapshot()==mid,p+method+" deterministic reverse");m.Invoke(c,new object[]{1f});type.GetMethod("Restore").Invoke(c,new[]{saved});check(snapshot()==mid,p+method+" capture restore");
type.GetMethod("ResetVisual").Invoke(c,null);check(snapshot()==rest,p+method+" complete cancel");m.Invoke(c,new object[]{float.NaN});check(!snapshot().Contains("NaN"),p+method+" finite guard");
check(go.transform.position==new Vector3(1,2,3)&&Quaternion.Angle(go.transform.rotation,Quaternion.Euler(12,40,7))<.01f,p+method+" stable placed root");
}
type.GetMethod("ResetVisual").Invoke(c,null);var basePose=snapshot();
foreach(string method in new[]{"SetCrumpleProgress","SetChainPoseProgress","SetSwayProgress","SetPickupSettleProgress","SetHandleProgress","SetWheelProgress","SetFlapProgress","SetTimerProgress","SetDialProgress"})type.GetMethod(method).Invoke(c,new object[]{.31f});
var combined=snapshot();var combinedState=type.GetMethod("Capture").Invoke(c,null);
foreach(string method in new[]{"SetCrumpleProgress","SetChainPoseProgress","SetSwayProgress","SetHandleProgress","SetWheelProgress","SetFlapProgress","SetTimerProgress","SetDialProgress"})type.GetMethod(method).Invoke(c,new object[]{.79f});
type.GetMethod("Restore").Invoke(c,new[]{combinedState});check(snapshot()==combined,p+" combined channels restore after divergent changes");
go.SetActive(false);go.SetActive(true);check(snapshot()==combined,p+" disable and reenable retains combined pose");
type.GetMethod("ResetVisual").Invoke(c,null);check(snapshot()==basePose,p+" combined cancel resets every channel");
var flags=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.Instance;
check(new[]{"Update","LateUpdate","FixedUpdate","Start","OnEnable","Awake"}.All(m=>type.GetMethod(m,flags)==null),"Presentation has no autonomous frame/time callbacks");
var needle=all.FirstOrDefault(t=>t.name=="NeedlePivot");if(needle!=null){type.GetMethod("SetTimerProgress").Invoke(c,new object[]{.25f});check(Quaternion.Angle(needle.localRotation,Quaternion.Euler(0,90,0))<.01f,"Timer quarter clockwise, looking down +Y face from +Z working edge");var before=snapshot();for(int i=0;i<30;i++)type.GetMethod("SetTimerProgress").Invoke(c,new object[]{.25f});check(snapshot()==before,"Repeated unchanged elapsed fraction holds needle pose");}
UnityEngine.Object.DestroyImmediate(go);
}
var result=new{status=fails.Count==0?"PASS":"FAIL",checks=checks,failures=fails};System.IO.File.WriteAllText("output/D_WritingProps_v1/validation/driver_checks.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
}finally{UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);}

}

static object PreviewScene(){
string art="Assets/Art/Props/WritingD";var previous=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if(string.IsNullOrEmpty(previous.path)&&!Application.isBatchMode)throw new System.InvalidOperationException("Save your untitled scene before explicitly running preview authoring.");
var scene=UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,string.IsNullOrEmpty(previous.path)?UnityEditor.SceneManagement.NewSceneMode.Single:UnityEditor.SceneManagement.NewSceneMode.Additive);
try {
var collection=new GameObject("WritingD_AssetPreview");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(collection,scene);
var files=System.IO.Directory.GetFiles(art+"/Prefabs","*.prefab").OrderBy(p=>p).ToArray();
for(int i=0;i<files.Length;i++) {var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(files[i]),scene);go.transform.SetParent(collection.transform,false);go.transform.localPosition=new Vector3((i%4)*.85f,0,(i/4)*.80f);}
var camera=new GameObject("Camera_Preview");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(camera,scene);camera.transform.position=new Vector3(2,4,5);camera.transform.LookAt(new Vector3(1.3f,.1f,1.6f));var cam=camera.AddComponent<Camera>();cam.orthographic=true;cam.orthographicSize=2.5f;cam.nearClipPlane=.01f;cam.farClipPlane=20;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.17f,.163f,.15f);
int k=0;foreach(var p in new[]{new Vector3(-2,4,3),new Vector3(3,2,1),new Vector3(0,3,-2)}){var g=new GameObject("Light_Preview_"+k);UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(g,scene);g.transform.position=p;g.transform.LookAt(new Vector3(1,0,1));var light=g.AddComponent<Light>();light.type=LightType.Directional;light.intensity=new[]{.7f,.25f,.35f}[k++];}
string path=art+"/Scenes/WritingDPreview.unity";bool saved=UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene,path);
var scripts=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MonoBehaviour>(true)).ToArray();bool safe=scripts.All(c=>c!=null&&c is MemoryWhitebox.Art.WritingPropVisual);
System.IO.File.WriteAllText("output/D_WritingProps_v1/validation/preview_scene.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{saved=saved,path=path,props=files.Length,safePresentationOnly=safe,scriptTypes=scripts.Select(s=>s.GetType().FullName).Distinct().ToArray()},Newtonsoft.Json.Formatting.Indented));return new{saved=saved,safe=safe};
}finally{if(previous.IsValid()&&previous.isLoaded)UnityEngine.SceneManagement.SceneManager.SetActiveScene(previous);if(UnityEngine.SceneManagement.SceneManager.sceneCount>1)UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene,true);}

}

static object Details(){
string art="Assets/Art/Props/WritingD", output="output/D_WritingProps_v1";
var scene=UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();var rows=new System.Collections.Generic.List<object>();var failures=new System.Collections.Generic.List<string>();
RenderTexture rt=null;Texture2D image=null;
try{
var cg=new GameObject("DetailCamera");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cg,scene);var cam=cg.AddComponent<Camera>();cam.scene=scene;cam.enabled=false;cam.orthographic=true;cam.nearClipPlane=.0001f;cam.farClipPlane=8;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.16f,.153f,.14f);
int n=0;foreach(var pos in new[]{new Vector3(-2,3,2),new Vector3(2,1,1),new Vector3(0,3,-2)}){var o=new GameObject("Light");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(o,scene);o.transform.position=pos;o.transform.LookAt(Vector3.zero);var l=o.AddComponent<Light>();l.type=LightType.Directional;l.intensity=new[]{.9f,.3f,.4f}[n++];}
rt=new RenderTexture(1200,1000,24);rt.antiAliasing=4;rt.Create();cam.targetTexture=rt;image=new Texture2D(1200,1000,TextureFormat.RGB24,false);
System.Action<GameObject,string,Vector3,string[]> render=(go,label,dir,keep)=>{
var ren=go.GetComponentsInChildren<Renderer>(true);var states=ren.Select(r=>r.enabled).ToArray();var tmp=new System.Collections.Generic.List<GameObject>();var meshes=new System.Collections.Generic.List<Mesh>();var pts=new System.Collections.Generic.List<Vector3>();
foreach(var r in ren){bool visible=keep.Length==0||keep.Any(k=>r.transform==go.transform.Find(k)||r.transform.name.Contains(k)||Enumerable.Range(0,1).Any(_=>{var t=r.transform;while(t!=go.transform&&t!=null){if(t.name==k)return true;t=t.parent;}return false;}));r.enabled=visible;if(!visible)continue;
var sk=r as SkinnedMeshRenderer;var f=r.GetComponent<MeshFilter>();Mesh m=f!=null?f.sharedMesh:null;if(sk!=null){m=new Mesh();sk.BakeMesh(m);meshes.Add(m);var b=new GameObject("BakedDetail");b.transform.SetParent(sk.transform,false);b.AddComponent<MeshFilter>().sharedMesh=m;var br=b.AddComponent<MeshRenderer>();br.sharedMaterials=r.sharedMaterials;var block=new MaterialPropertyBlock();r.GetPropertyBlock(block,0);br.SetPropertyBlock(block,0);tmp.Add(b);r.enabled=false;}if(m!=null)pts.AddRange(m.vertices.Select(v=>r.transform.TransformPoint(v)));}
var bounds=new Bounds(pts[0],Vector3.zero);foreach(var p in pts)bounds.Encapsulate(p);cam.transform.position=bounds.center+dir.normalized*2;cam.transform.LookAt(bounds.center);var inv=Quaternion.Inverse(cam.transform.rotation);var pp=pts.Select(p=>inv*(p-bounds.center)).ToArray();cam.orthographicSize=Mathf.Max(pp.Max(p=>Mathf.Abs(p.y)),pp.Max(p=>Mathf.Abs(p.x))/1.2f)*1.13f+.0004f;
cam.Render();var previous=RenderTexture.active;RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,1200,1000),0,0);image.Apply();RenderTexture.active=previous;System.IO.File.WriteAllBytes(output+"/previews/Detail_"+label+".png",image.EncodeToPNG());foreach(var t in tmp)UnityEngine.Object.DestroyImmediate(t);foreach(var m in meshes)UnityEngine.Object.DestroyImmediate(m);for(int i=0;i<ren.Length;i++)ren[i].enabled=states[i];};
foreach(string file in System.IO.Directory.GetFiles(art+"/Prefabs","*.prefab")){
var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(file),scene);var d=go.GetComponent<MemoryWhitebox.Art.WritingPropVisual>();var all=go.GetComponentsInChildren<Transform>(true);System.Func<string,Transform> find=s=>all.FirstOrDefault(t=>t.name==s);
if(go.name.Contains("WritingSet")){
for(int k=0;k<=4;k++){d.SetCrumpleProgress(k*.25f);render(go,"Paper_"+k,new Vector3(.7f,1,.8f),new[]{"PaperBody"});}
d.ResetVisual();render(go,"PenTip",new Vector3(.7f,1,.8f),new[]{"PenAssembly"});render(go,"BasketInterior",new Vector3(.4f,1.4f,.7f),new[]{"BasketBody"});
var t=new Texture2D(64,64,TextureFormat.RGBA32,false);var pixels=new Color[4096];for(int y=0;y<64;y++)for(int x=0;x<64;x++)pixels[y*64+x]=y>10&&y<54&&(y%12<3)&&x>7&&x<45?new Color(.08f,.055f,.03f,1):Color.clear;t.SetPixels(pixels);t.Apply();d.SetInkTexture(t);d.SetCrumpleProgress(.45f);render(go,"InkFollow_RuntimeTest",new Vector3(.5f,1,.7f),new[]{"PaperBody"});
var clone=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(file),scene);var a=new MaterialPropertyBlock();var b=new MaterialPropertyBlock();find("InkSurface").GetComponent<Renderer>().GetPropertyBlock(a,0);clone.GetComponentsInChildren<Renderer>().First(r=>r.name=="InkSurface").GetPropertyBlock(b,0);if(a.GetTexture("_MainTex")!=t||b.GetTexture("_MainTex")==t)failures.Add("Ink per-instance isolation failed");UnityEngine.Object.DestroyImmediate(clone);d.ResetVisual();UnityEngine.Object.DestroyImmediate(t);
}
if(go.name=="PF_Pendant_D02"){
foreach(float u in new[]{0f,.5f,1f}){d.SetChainPoseProgress(u);render(go,"PendantPose_"+u,new Vector3(.55f,.8f,1),new string[0]);var anchor=find("ChainRootAnchor");var root=find("ChainRoot");float gap=Vector3.Distance(anchor.position,root.position);rows.Add(new{check="Pendant chain bail",progress=u,gap=gap});if(gap>.001f)failures.Add("Pendant bail gap "+gap);}
d.SetChainPoseProgress(1);render(go,"Pendant_Close",new Vector3(.25f,.25f,1),new[]{"PendantPose"});
}
if(go.name=="PF_DaisyBouquet_D03")render(go,"Daisy_Close",new Vector3(.3f,1,.4f),new[]{"FlowerStem4"});
if(go.name=="PF_Suitcase_D04"){d.SetHandleProgress(1);render(go,"Handle_Raised",new Vector3(.5f,1,1),new[]{"HandlePivot","CaseShell"});var h=find("CarryGripAnchor");rows.Add(new{check="Raised handle grip",height=h.position.y});if(h.position.y<.46f)failures.Add("Raised handle grip intersects case");}
if(go.name=="PF_Envelope_D05"){d.SetFlapProgress(1);render(go,"Envelope_Open",new Vector3(.3f,.4f,1),new string[0]);}
if(go.name=="PF_MechanicalTimer_D06"){foreach(float u in new[]{0f,.25f,.5f,.75f,1f}){d.SetTimerProgress(u);render(go,"Timer_"+u,new Vector3(0,1,.12f),new string[0]);}}
UnityEngine.Object.DestroyImmediate(go);
}
var result=new{status=failures.Count==0?"PASS":"FAIL",failures=failures,details=rows};System.IO.File.WriteAllText(output+"/validation/mechanical_checks.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
}finally{if(image!=null)UnityEngine.Object.DestroyImmediate(image);if(rt!=null){rt.Release();UnityEngine.Object.DestroyImmediate(rt);}UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);}

}
}}