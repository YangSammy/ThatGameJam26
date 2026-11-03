using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Newtonsoft.Json;

namespace MemoryWhitebox.Art.Editor
{
    /// <summary>Explicit, isolated authoring commands. Never edits production scenes.</summary>
    public static class ParkGirlSampleBuild
    {
        const string Art = "Assets/Art/Characters/ParkGirl";
        const string Out = "output/Characters/ParkGirl";
        public const string ScenePath = Art + "/Scenes/ParkGirlSamplePreview.unity";
        const string ModelPath = Art + "/Models/CH_ParkGirl_Sample.fbx";
        const string PrefabPath = Art + "/Prefabs/PF_ParkGirl.prefab";
        const string ClipPath = Art + "/Animations/AN_ParkGirl_Sample_10s.anim";

        static void Report(string name, object value)
        {
            Directory.CreateDirectory(Out + "/Reports");
            File.WriteAllText(Out + "/Reports/" + name, JsonConvert.SerializeObject(value, Formatting.Indented));
        }

        [MenuItem("Tools/Art/Park Girl/Build Independent Sample")]
        public static void Build()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode first.");
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            foreach(string path in Directory.GetFiles(Art+"/Textures","*.png"))
            {
                var ti=(TextureImporter)AssetImporter.GetAtPath(path);
                bool color=path.EndsWith("_BaseColor.png"),normal=path.EndsWith("_Normal.png");
                ti.sRGBTexture=color;ti.textureType=normal?TextureImporterType.NormalMap:TextureImporterType.Default;
                ti.textureCompression=TextureImporterCompression.Uncompressed;ti.maxTextureSize=512;ti.SaveAndReimport();
            }
            var importer = AssetImporter.GetAtPath(ModelPath) as ModelImporter;
            if (importer == null) throw new FileNotFoundException(ModelPath);
            importer.animationType = ModelImporterAnimationType.Generic;
            importer.importAnimation = true;
            importer.importBlendShapes = true;
            importer.importCameras = false;
            importer.importLights = false;
            importer.isReadable = true;
            importer.optimizeGameObjects = false;
            importer.optimizeBones = false;
            importer.meshCompression = ModelImporterMeshCompression.Off;
            importer.animationCompression = ModelImporterAnimationCompression.Off;
            importer.skinWeights = ModelImporterSkinWeights.Custom;
            importer.maxBonesPerVertex = 16;
            importer.minBoneWeight = 0.000001f;
            importer.importNormals = ModelImporterNormals.Import;
            importer.importTangents = ModelImporterTangents.CalculateMikk;
            importer.SaveAndReimport();

            var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            var sourceClips = AssetDatabase.LoadAllAssetsAtPath(ModelPath).OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview__")).ToArray();
            if (sourceClips.Length == 0) throw new InvalidOperationException("FBX contains no animation.");
            var source = sourceClips.OrderByDescending(c => c.length).First();
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath);
            if (clip == null) { clip = new AnimationClip(); AssetDatabase.CreateAsset(clip, ClipPath); }
            EditorUtility.CopySerialized(source, clip);
            clip.name = "AN_ParkGirl_Sample_10s";
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            settings.loopBlend = false;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            EditorUtility.SetDirty(clip);

            string controllerPath = Art + "/Animations/AC_ParkGirl_Sample.controller";
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if (controller == null) controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            var machine = controller.layers[0].stateMachine;
            var state = machine.states.Select(s => s.state).FirstOrDefault(s => s.name == "Sample_10s") ?? machine.AddState("Sample_10s");
            state.motion = clip; machine.defaultState = state;
            EditorUtility.SetDirty(controller);

            var preview = EditorSceneManager.NewPreviewScene();
            try
            {
                var root = new GameObject("PF_ParkGirl");
                SceneManager.MoveGameObjectToScene(root, preview);
                var visual = UnityEngine.Object.Instantiate(model, root.transform);
                visual.name = "Visual";
                visual.transform.localPosition = Vector3.zero;
                visual.transform.localRotation = Quaternion.identity;
                visual.transform.localScale = Vector3.one;
                var animator = visual.GetComponent<Animator>();
                if (animator == null) animator = visual.AddComponent<Animator>();
                animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                foreach (var ren in visual.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    ren.updateWhenOffscreen = true;
                    ren.quality = SkinQuality.Auto;
                    ren.localBounds = new Bounds(new Vector3(0, .8f, 0), new Vector3(2.4f, 2.2f, 2.4f));
                    ren.sharedMaterials = ren.sharedMaterials.Select(GetMaterial).ToArray();
                }
                foreach (var ren in visual.GetComponentsInChildren<MeshRenderer>(true))
                    ren.sharedMaterials = ren.sharedMaterials.Select(GetMaterial).ToArray();
                var anchor = new GameObject("PlacementAnchor"); anchor.transform.SetParent(root.transform, false);
                clip.SampleAnimation(visual, 0);
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally { EditorSceneManager.ClosePreviewScene(preview); }
            SaveSampleAssets();
            CreateScene();
            Validate();
            Debug.Log("ParkGirl independent sample built. Production scenes were not saved.");
        }

        static void SaveSampleAssets()
        {
            foreach (string guid in AssetDatabase.FindAssets("", new[] { Art }))
                AssetDatabase.SaveAssetIfDirty(new GUID(guid));
        }

        static Material GetMaterial(Material imported)
        {
            string name = imported != null ? imported.name.Replace(" (Instance)", "") : "M_PG_Jacket";
            string path = Art + "/Materials/" + name + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) { mat = new Material(Shader.Find("Standard")); AssetDatabase.CreateAsset(mat, path); }
            mat.shader = Shader.Find("Standard");
            Color linear = imported != null && imported.HasProperty("_Color") ? imported.color : Color.gray;
            var colors = new Dictionary<string, Color> {
                {"M_PG_Skin",new Color(.44f,.29f,.20f)}, {"M_PG_Hair",new Color(.16f,.068f,.027f)},
                {"M_PG_Jacket",new Color(.20f,.225f,.115f)}, {"M_PG_Shirt",new Color(.27f,.071f,.061f)},
                {"M_PG_Denim",new Color(.11f,.17f,.23f)}, {"M_PG_Boots",new Color(.125f,.068f,.038f)},
                {"M_PG_FaceGuard",new Color(.095f,.041f,.018f)}, {"M_PG_Stitch",new Color(.34f,.265f,.16f)}
            };
            if (colors.ContainsKey(name)) linear = colors[name];
            mat.color = linear.gamma;
            mat.SetFloat("_Metallic", 0);
            mat.SetFloat("_Glossiness", name.Contains("Hair") ? .32f : .16f);
            string texture = Art + "/Textures/" + name + "_BaseColor.png";
            if (File.Exists(texture)) { mat.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(texture); mat.color = Color.white; }
            mat.SetTexture("_MetallicGlossMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"/Textures/"+name+"_MetallicSmoothness.png"));
            mat.EnableKeyword("_METALLICGLOSSMAP");mat.SetFloat("_GlossMapScale",1);
            mat.SetTexture("_OcclusionMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"/Textures/"+name+"_AO.png"));
            mat.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"/Textures/"+name+"_Normal.png"));mat.EnableKeyword("_NORMALMAP");
            mat.SetFloat("_BumpScale",name.Contains("Hair")?.35f:.12f);
            // Old cloth and sculpted hair must not pick up a plastic-looking default sky reflection.
            bool matte=name.Contains("Hair")||name.Contains("Jacket")||name.Contains("Shirt")||name.Contains("FaceGuard");
            mat.SetFloat("_GlossyReflections",matte?0:1);
            if(matte)mat.EnableKeyword("_GLOSSYREFLECTIONS_OFF");else mat.DisableKeyword("_GLOSSYREFLECTIONS_OFF");
            EditorUtility.SetDirty(mat);
            return mat;
        }

        static void CreateScene()
        {
            var original = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene);
                RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
                RenderSettings.ambientSkyColor = new Color(.48f,.51f,.56f);
                RenderSettings.ambientEquatorColor = new Color(.25f,.27f,.29f);
                RenderSettings.ambientGroundColor = new Color(.13f,.12f,.10f);
                RenderSettings.fog = false;
                var root = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath), scene);
                root.name = "ParkGirl_Sample";
                var light = new GameObject("Light_Key").AddComponent<Light>(); light.type = LightType.Directional;
                light.transform.rotation = Quaternion.Euler(42,-35,0); light.intensity = 1.15f; light.color = new Color(1,.93f,.83f); light.shadows = LightShadows.Soft;
                var fill = new GameObject("Light_Fill").AddComponent<Light>(); fill.type = LightType.Directional; fill.transform.rotation = Quaternion.Euler(25,140,0); fill.intensity = .45f; fill.color = new Color(.73f,.83f,1);
                var camera = new GameObject("Camera_Sample").AddComponent<Camera>(); camera.tag = "MainCamera";
                camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.14f,.16f,.18f);
                camera.nearClipPlane = .03f; camera.farClipPlane = 30; camera.fieldOfView = 32;
                camera.transform.position = new Vector3(2.3f,1.55f,3.6f); camera.transform.LookAt(new Vector3(0,.83f,0));
                var floor = GameObject.CreatePrimitive(PrimitiveType.Cylinder); floor.name = "PreviewPlinth"; floor.transform.position = new Vector3(0,-.035f,0); floor.transform.localScale = new Vector3(1.1f,.03f,1.1f);
                UnityEngine.Object.DestroyImmediate(floor.GetComponent<Collider>());
                string floorPath = Art + "/Materials/M_PreviewPlinth.mat";
                var floorMat = AssetDatabase.LoadAssetAtPath<Material>(floorPath);
                if (floorMat == null) { floorMat = new Material(Shader.Find("Standard")); floorMat.color = new Color(.20f,.22f,.23f); AssetDatabase.CreateAsset(floorMat, floorPath); }
                floor.GetComponent<Renderer>().sharedMaterial = floorMat;
                SaveSampleAssets();
                EditorSceneManager.SaveScene(scene, ScenePath);
            }
            finally { SceneManager.SetActiveScene(original); EditorSceneManager.CloseScene(scene, true); }
        }

        [MenuItem("Tools/Art/Park Girl/Validate Sample Assets")]
        public static void Validate()
        {
            var scene = EditorSceneManager.NewPreviewScene();
            try
            {
                var root = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath), scene);
                var visual = root.transform.Find("Visual").gameObject;
                var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath);
                var skins = root.GetComponentsInChildren<SkinnedMeshRenderer>();
                var all = root.GetComponentsInChildren<Transform>();
                var hand = all.First(t => t.name == "HandContactAnchor");
                var head = all.First(t => t.name == "NoseRevealLimit");
                var rows = new List<object>();
                var initialRoot = root.transform.localToWorldMatrix;
                int missing = skins.Count(s => s.sharedMesh == null || s.sharedMaterials.Any(m => m == null) || s.bones.Any(b => b == null));
                var bounds = new Bounds(); bool first = true;
                foreach(float t in new[]{0f,1f,2f,3f,4f,5f,6f,7f,8f,9f,10f})
                {
                    clip.SampleAnimation(visual,t);
                    var shapeValues = new List<object>();
                    foreach(var skin in skins)
                    {
                        var mesh=skin.sharedMesh;
                        for(int i=0;i<mesh.blendShapeCount;i++)
                            if(mesh.GetBlendShapeName(i).Contains("Elbow1.R")) shapeValues.Add(new {mesh=skin.name,weight=skin.GetBlendShapeWeight(i)});
                        var baked=new Mesh();skin.BakeMesh(baked,true);
                        if(t==0) foreach(var v in baked.vertices){var world=skin.transform.TransformPoint(v);if(first){bounds=new Bounds(world,Vector3.zero);first=false;}else bounds.Encapsulate(world);}
                        UnityEngine.Object.DestroyImmediate(baked);
                    }
                    rows.Add(new {time=t,hand=V(hand.position),nose=V(head.position),rootUnchanged=root.transform.localToWorldMatrix==initialRoot,shapes=shapeValues});
                }
                var data = new {
                    unity=Application.unityVersion, pipeline=UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline==null?"Built-in":"SRP",
                    prefab=PrefabPath, clipSeconds=clip.length, missingReferences=missing, bounds= new {center=V(bounds.center),size=V(bounds.size)},
                    renderers=skins.Length, bones=all.Length, triangles=skins.Sum(s=>s.sharedMesh.triangles.Length/3),
                    blendShapes=skins.Sum(s=>s.sharedMesh.blendShapeCount), materials=skins.SelectMany(s=>s.sharedMaterials).Distinct().Count(),
                    blendShapeCurves=AnimationUtility.GetCurveBindings(clip).Count(b=>b.propertyName.StartsWith("blendShape.")), poses=rows,
                    animationRootMotion=visual.GetComponent<Animator>().applyRootMotion
                };
                Report("unity-asset-validation.json",data);
                if(missing>0 || clip.length<9.9f || bounds.size.y<1.45f || bounds.size.y>1.8f)throw new InvalidOperationException("Sample asset validation failed; inspect report.");
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }

        static float[] V(Vector3 p) { return new[]{p.x,p.y,p.z}; }

        [MenuItem("Tools/Art/Park Girl/Render Verification Frames")]
        public static void RenderFrames()
        {
            var scene=EditorSceneManager.NewPreviewScene();
            RenderTexture rt=null;Texture2D image=null;
            try
            {
                var character=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath),scene);
                var visual=character.transform.Find("Visual").gameObject;
                var cg=new GameObject("VerificationCamera");SceneManager.MoveGameObjectToScene(cg,scene);var camera=cg.AddComponent<Camera>();
                camera.scene=scene;camera.enabled=false;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.18f,.20f,.22f);
                camera.orthographic=true;camera.orthographicSize=.93f;camera.nearClipPlane=.03f;camera.farClipPlane=15;
                foreach(var pair in new[]{new Vector2(-35,1.15f),new Vector2(140,.75f)})
                {
                    var lg=new GameObject("VerificationLight");SceneManager.MoveGameObjectToScene(lg,scene);var l=lg.AddComponent<Light>();
                    l.type=LightType.Directional;l.intensity=pair.y;l.transform.rotation=Quaternion.Euler(40,pair.x,0);
                }
                var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath);
                rt=new RenderTexture(720,900,24);rt.antiAliasing=4;rt.Create();camera.targetTexture=rt;
                image=new Texture2D(720,900,TextureFormat.RGB24,false);
                var views=new[]{new Vector3(0,1.35f,3.6f),new Vector3(2.2f,1.45f,3.4f),new Vector3(3.5f,1.35f,0),new Vector3(2.2f,1.45f,-3.4f),new Vector3(0,1.35f,-3.6f),new Vector3(-2.2f,1.45f,3.4f)};
                Directory.CreateDirectory(Out+"/Preview/Unity");
                for(int frame=0;frame<=10;frame++)
                {
                    clip.SampleAnimation(visual,frame);
                    var temporaryObjects=new List<GameObject>();var temporaryMeshes=new List<Mesh>();var skins=visual.GetComponentsInChildren<SkinnedMeshRenderer>();
                    foreach(var skin in skins)
                    {
                        var mesh=new Mesh();skin.BakeMesh(mesh,true);temporaryMeshes.Add(mesh);
                        var g=new GameObject("EvaluatedMesh");SceneManager.MoveGameObjectToScene(g,scene);g.transform.SetParent(skin.transform,false);
                        g.AddComponent<MeshFilter>().sharedMesh=mesh;g.AddComponent<MeshRenderer>().sharedMaterials=skin.sharedMaterials;
                        skin.enabled=false;temporaryObjects.Add(g);
                    }
                    for(int view=0;view<views.Length;view++)
                    {
                        camera.transform.position=views[view];camera.transform.LookAt(new Vector3(0,.82f,0));camera.Render();
                        var old=RenderTexture.active;RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,720,900),0,0);image.Apply();RenderTexture.active=old;
                        File.WriteAllBytes(Out+"/Preview/Unity/frame_"+frame.ToString("00")+"_view_"+view+".png",image.EncodeToPNG());
                    }
                    foreach(var g in temporaryObjects)UnityEngine.Object.DestroyImmediate(g);
                    foreach(var m in temporaryMeshes)UnityEngine.Object.DestroyImmediate(m);
                    foreach(var skin in skins)skin.enabled=true;
                }
            }
            finally
            {
                if(rt!=null){rt.Release();UnityEngine.Object.DestroyImmediate(rt);}if(image!=null)UnityEngine.Object.DestroyImmediate(image);
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }
    }
}
