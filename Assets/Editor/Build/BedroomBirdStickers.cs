using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

// Incremental decoration only: never regenerates the room, colliders, or GI.
public static class BedroomBirdStickers
{
    const string OwnedRoot = "HuashuBirdStickers";
    const string DedicatedRoot = "Assets/Bedroom";
    const string RepositoryRoot = "Assets/Art/Environments/House/Bedroom/V17";
    static readonly string[] Seasons = { "Spring", "Summer", "Autumn", "Winter" };

    [Serializable] public sealed class PlacementFile { public Placement[] stickers; }
    [Serializable] public sealed class Placement
    {
        public string name, texture, host;
        public Vector3[] vertices;
        public Vector2[] uv;
        public Vector3 normal;
        public int[] triangles;
    }
    [Serializable] sealed class ApplyReport
    {
        public string unity, project, assetRoot, operation, evidenceScope;
        public bool passed, rebuiltViewer;
        public SceneReport[] scenes;
    }
    [Serializable] sealed class SceneReport
    {
        public string season, scene;
        public int originalRenderers, previousOwnedRenderers, finalRenderers;
        public int collidersBefore, collidersAfter, stickers, lightmaps;
        public bool passed, savedLightingReferencesPreserved;
    }

    static string AssetRoot
    {
        get
        {
            if (AssetDatabase.IsValidFolder(DedicatedRoot)) return DedicatedRoot;
            if (AssetDatabase.IsValidFolder(RepositoryRoot)) return RepositoryRoot;
            throw new InvalidOperationException("No supported saved Bedroom V17 asset root.");
        }
    }
    static string ScenePath(string root, string season)
    {
        return (root == DedicatedRoot ? root + "/Scenes" : "Assets/Scenes/Bedroom/V17")
            + "/Bedroom_" + season + ".unity";
    }
    static string OutputFolder(string root)
    {
        string project = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        return Path.Combine(root == DedicatedRoot ? Path.GetFullPath(Path.Combine(project, "..")) : project,
            "QA", "stickers-20261102");
    }
    static Vector3 ToUnity(Vector3 v) { return new Vector3(-v.x, v.z, -v.y); }

    [MenuItem("Bedroom/Bird stickers/Apply only (preserve baked GI)")]
    public static void ApplyOnly() { Apply(false); }

    [MenuItem("Bedroom/Bird stickers/Apply and build dedicated viewer")]
    public static void ApplyAndBuild()
    {
        // Resolve this before any edit: repository projects must not receive the
        // dedicated builder's global quality/build-settings changes.
        if (AssetRoot != DedicatedRoot)
            throw new InvalidOperationException("ApplyAndBuild is only for the dedicated Bedroom project. Use ApplyOnly here.");
        MethodInfo build = AppDomain.CurrentDomain.GetAssemblies()
            .Select(a => a.GetType("BedroomBuild", false)).Where(t => t != null)
            .Select(t => t.GetMethod("BuildPrepared", BindingFlags.Public | BindingFlags.Static))
            .FirstOrDefault(m => m != null);
        if (build == null) throw new InvalidOperationException("Dedicated BedroomBuild.BuildPrepared is unavailable.");
        Apply(false);
        try { build.Invoke(null, null); }
        catch (TargetInvocationException e) { throw e.InnerException ?? e; }
        string reportPath = Path.Combine(OutputFolder(DedicatedRoot), "unity-apply.json");
        var report = JsonUtility.FromJson<ApplyReport>(File.ReadAllText(reportPath));
        report.rebuiltViewer = true;
        File.WriteAllText(reportPath, JsonUtility.ToJson(report, true));
        Debug.Log("BEDROOM_STICKERS_BUILD_PASS");
    }

    [MenuItem("Bedroom/Bird stickers/Validate saved stickers")]
    public static void Validate() { Apply(true); }

    static Placement[] LoadPlacements(string decor)
    {
        var file = JsonUtility.FromJson<PlacementFile>(File.ReadAllText(decor + "/Placement.json"));
        if (file == null || file.stickers == null || file.stickers.Length != 12)
            throw new InvalidOperationException("Expected exactly twelve bird-sticker placements.");
        var names = new HashSet<string>(StringComparer.Ordinal);
        var textures = new HashSet<string>(StringComparer.Ordinal);
        foreach (var p in file.stickers)
        {
            if (p == null || string.IsNullOrEmpty(p.name) || !p.name.StartsWith("HUASHU_", StringComparison.Ordinal)
                || p.name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || !names.Add(p.name))
                throw new InvalidOperationException("Invalid or duplicate owned sticker name.");
            if (string.IsNullOrEmpty(p.host) || string.IsNullOrEmpty(p.texture)
                || Path.GetFileName(p.texture) != p.texture || !p.texture.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(p.name + ": invalid host/texture.");
            if (p.vertices == null || p.vertices.Length != 4 || p.uv == null || p.uv.Length != 4
                || p.triangles == null || p.triangles.Length != 6 || p.normal.sqrMagnitude < .99f
                || !Finite(p.normal.x) || !Finite(p.normal.y) || !Finite(p.normal.z))
                throw new InvalidOperationException(p.name + ": expected four vertices/UVs, two triangles, and a normal.");
            foreach (Vector3 v in p.vertices)
                if (!Finite(v.x) || !Finite(v.y) || !Finite(v.z)) throw new InvalidOperationException(p.name + ": non-finite vertex.");
            foreach (Vector2 uv in p.uv)
                if (!Finite(uv.x) || !Finite(uv.y) || uv.x < 0 || uv.x > 1 || uv.y < 0 || uv.y > 1)
                    throw new InvalidOperationException(p.name + ": invalid UV.");
            for (int t = 0; t < p.triangles.Length; t += 3)
            {
                int a = p.triangles[t], b = p.triangles[t + 1], c = p.triangles[t + 2];
                if (a < 0 || a > 3 || b < 0 || b > 3 || c < 0 || c > 3)
                    throw new InvalidOperationException(p.name + ": triangle index out of range.");
                // Swapping b/c compensates for the determinant -1 coordinate conversion.
                Vector3 cross = Vector3.Cross(ToUnity(p.vertices[c] - p.vertices[a]), ToUnity(p.vertices[b] - p.vertices[a]));
                if (cross.sqrMagnitude < 1e-12f || Vector3.Dot(cross.normalized, ToUnity(p.normal).normalized) < .98f)
                    throw new InvalidOperationException(p.name + ": winding and supplied outward normal disagree.");
            }
            textures.Add(p.texture);
        }
        if (textures.Count != 6) throw new InvalidOperationException("Expected six bird textures distributed across twelve stickers.");
        return file.stickers;
    }
    static bool Finite(float f) { return !float.IsNaN(f) && !float.IsInfinity(f); }
    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = path.Substring(0, path.LastIndexOf('/'));
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, path.Substring(path.LastIndexOf('/') + 1));
    }
    static void ConfigureTextures(string decor, Placement[] placements)
    {
        foreach (string name in placements.Select(p => p.texture).Distinct())
        {
            string path = decor + "/" + name;
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) throw new InvalidOperationException("Missing imported bird texture: " + path);
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = true;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.mipmapEnabled = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }
    }
    static Mesh SaveMesh(string path, Placement p, Vector3 center)
    {
        var mesh = new Mesh { name = p.name };
        mesh.vertices = p.vertices.Select(v => ToUnity(v) - center).ToArray();
        mesh.uv = p.uv;
        mesh.normals = Enumerable.Repeat(ToUnity(p.normal).normalized, 4).ToArray();
        int[] triangles = (int[])p.triangles.Clone();
        for (int i = 0; i < triangles.Length; i += 3)
        { int swap = triangles[i + 1]; triangles[i + 1] = triangles[i + 2]; triangles[i + 2] = swap; }
        mesh.triangles = triangles;
        mesh.RecalculateBounds();
        var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (existing == null) { AssetDatabase.CreateAsset(mesh, path); return mesh; }
        EditorUtility.CopySerialized(mesh, existing);
        UnityEngine.Object.DestroyImmediate(mesh);
        EditorUtility.SetDirty(existing);
        return existing;
    }
    static Material SaveMaterial(string path, Texture2D texture, string name)
    {
        Shader shader = Shader.Find("Standard");
        if (shader == null || !shader.isSupported) throw new InvalidOperationException("Built-in Standard shader unavailable.");
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null) { material = new Material(shader); AssetDatabase.CreateAsset(material, path); }
        material.name = name;
        material.shader = shader;
        material.color = Color.white;
        material.mainTexture = texture;
        material.SetFloat("_Mode", 1);
        material.SetFloat("_Cutoff", .45f);
        material.SetFloat("_Metallic", 0);
        material.SetFloat("_Glossiness", .12f); // Paper roughness .88.
        material.SetInt("_SrcBlend", (int)BlendMode.One);
        material.SetInt("_DstBlend", (int)BlendMode.Zero);
        material.SetInt("_ZWrite", 1);
        material.SetOverrideTag("RenderType", "TransparentCutout");
        material.EnableKeyword("_ALPHATEST_ON");
        material.DisableKeyword("_ALPHABLEND_ON");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        material.renderQueue = (int)RenderQueue.AlphaTest;
        material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
        material.enableInstancing = true;
        EditorUtility.SetDirty(material);
        return material;
    }
    static Renderer[] Renderers(Scene scene)
    { return scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Renderer>(true)).ToArray(); }
    static int ColliderCount(Scene scene)
    { return scene.GetRootGameObjects().Sum(g => g.GetComponentsInChildren<Collider>(true).Length); }
    static Texture[] LightmapReferences()
    { return LightmapSettings.lightmaps.SelectMany(l => new Texture[] { l.lightmapColor, l.lightmapDir, l.shadowMask }).ToArray(); }

    static void CheckRoot(GameObject root, Placement[] placements)
    {
        if (root == null || root.transform.childCount != placements.Length
            || root.GetComponentsInChildren<Renderer>(true).Length != placements.Length
            || root.GetComponentsInChildren<Collider>(true).Length != 0)
            throw new InvalidOperationException("Sticker root must contain twelve independent renderers and no colliders.");
        var meshes = new HashSet<Mesh>();
        var materials = new HashSet<Material>();
        foreach (var p in placements)
        {
            Transform child = root.transform.Find(p.name);
            if (child == null) throw new InvalidOperationException("Missing sticker: " + p.name);
            var filter = child.GetComponent<MeshFilter>();
            var renderer = child.GetComponent<MeshRenderer>();
            Mesh mesh = filter == null ? null : filter.sharedMesh;
            Material material = renderer == null ? null : renderer.sharedMaterial;
            if (mesh == null || mesh.vertexCount != 4 || mesh.uv.Length != 4 || mesh.triangles.Length != 6
                || material == null || material.mainTexture == null || material.shader == null || material.shader.name != "Standard"
                || material.mainTexture.name != Path.GetFileNameWithoutExtension(p.texture)
                || material.GetFloat("_Cutoff") != .45f || material.GetFloat("_Glossiness") != .12f
                || !material.IsKeywordEnabled("_ALPHATEST_ON")
                || renderer.shadowCastingMode != ShadowCastingMode.Off || renderer.lightProbeUsage != LightProbeUsage.BlendProbes
                || renderer.receiveGI != ReceiveGI.LightProbes || child.gameObject.isStatic || renderer.lightmapIndex >= 0
                || !meshes.Add(mesh) || !materials.Add(material))
                throw new InvalidOperationException(p.name + ": missing/incorrect independent mesh, texture, UV, or non-GI settings.");
        }
    }
    static void Apply(bool validateOnly)
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        string root = AssetRoot, decor = root + "/Decor/HuashuBirdStickers";
        AssetDatabase.Refresh();
        Placement[] placements = LoadPlacements(decor);
        if (!validateOnly) { EnsureFolder(decor + "/Meshes"); EnsureFolder(decor + "/Materials"); ConfigureTextures(decor, placements); }
        SceneSetup[] previous = EditorSceneManager.GetSceneManagerSetup();
        var reports = new List<SceneReport>();
        try
        {
            for (int season = 0; season < Seasons.Length; season++)
            {
                string path = ScenePath(root, Seasons[season]);
                Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                var oldRoots = scene.GetRootGameObjects().Where(g => g.name == OwnedRoot).ToArray();
                if (oldRoots.Length > 1) throw new InvalidOperationException(path + ": duplicate owned sticker roots.");
                GameObject owned = oldRoots.FirstOrDefault();
                int previousOwned = owned == null ? 0 : owned.GetComponentsInChildren<Renderer>(true).Length;
                int originalRenderers = Renderers(scene).Length - previousOwned;
                int collidersBefore = ColliderCount(scene);
                Texture[] lightmapsBefore = LightmapReferences();
                var lightingDataBefore = Lightmapping.lightingDataAsset;
                if (lightmapsBefore.Length == 0) throw new InvalidOperationException(path + ": existing baked lighting missing.");
                if (!validateOnly)
                {
                    if (owned != null)
                    {
                        if (owned.GetComponentsInChildren<Collider>(true).Length != 0)
                            throw new InvalidOperationException("Refusing to remove an owned root containing colliders.");
                        UnityEngine.Object.DestroyImmediate(owned);
                    }
                    owned = new GameObject(OwnedRoot);
                    SceneManager.MoveGameObjectToScene(owned, scene);
                    foreach (var p in placements)
                    {
                        Vector3 center = p.vertices.Select(ToUnity).Aggregate(Vector3.zero, (a, b) => a + b) / 4f;
                        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(decor + "/" + p.texture);
                        if (texture == null) throw new InvalidOperationException("Missing texture " + p.texture);
                        var go = new GameObject(p.name);
                        go.transform.SetParent(owned.transform, false);
                        go.transform.position = center;
                        GameObjectUtility.SetStaticEditorFlags(go, 0);
                        go.AddComponent<MeshFilter>().sharedMesh = SaveMesh(decor + "/Meshes/" + p.name + ".asset", p, center);
                        var renderer = go.AddComponent<MeshRenderer>();
                        renderer.sharedMaterial = SaveMaterial(decor + "/Materials/" + p.name + ".mat", texture, p.name + "_Paper");
                        renderer.shadowCastingMode = ShadowCastingMode.Off;
                        renderer.receiveShadows = true;
                        renderer.lightProbeUsage = LightProbeUsage.BlendProbes;
                        renderer.reflectionProbeUsage = ReflectionProbeUsage.BlendProbes;
                        renderer.receiveGI = ReceiveGI.LightProbes;
                        renderer.lightmapIndex = -1;
                    }
                    var cameras = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Camera>(true)).ToArray();
                    Camera view = cameras.FirstOrDefault(c => c.CompareTag("MainCamera"));
                    if (view == null) throw new InvalidOperationException(path + ": main camera missing.");
                    var capture = view.GetComponent<BedroomStickerCapture>();
                    if (capture == null) capture = view.gameObject.AddComponent<BedroomStickerCapture>();
                    capture.SeasonIndex = season;
                    capture.StickerRoot = owned;
                    capture.Targets = placements.Select(p => new BedroomStickerCapture.StickerTarget
                        { Name = p.name, Host = p.host, Normal = ToUnity(p.normal).normalized }).ToArray();
                    EditorUtility.SetDirty(capture);
                }
                CheckRoot(owned, placements);
                int after = Renderers(scene).Length, collidersAfter = ColliderCount(scene);
                bool sameLighting = lightmapsBefore.SequenceEqual(LightmapReferences()) && lightingDataBefore == Lightmapping.lightingDataAsset;
                if (after != originalRenderers + 12 || collidersAfter != collidersBefore || !sameLighting)
                    throw new InvalidOperationException(path + ": room renderer/collider/lighting invariants failed.");
                if (!validateOnly)
                {
                    AssetDatabase.SaveAssets();
                    EditorSceneManager.MarkSceneDirty(scene);
                    if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Could not save " + path);
                    // Check the deserialized scene, not only the in-memory edit.
                    scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                    owned = scene.GetRootGameObjects().Single(g => g.name == OwnedRoot);
                    CheckRoot(owned, placements);
                    after = Renderers(scene).Length;
                    collidersAfter = ColliderCount(scene);
                    sameLighting = lightmapsBefore.SequenceEqual(LightmapReferences()) && lightingDataBefore == Lightmapping.lightingDataAsset;
                    if (after != originalRenderers + 12 || collidersAfter != collidersBefore || !sameLighting)
                        throw new InvalidOperationException(path + ": saved scene renderer/collider/GI references differ.");
                }
                reports.Add(new SceneReport { season = Seasons[season], scene = path, originalRenderers = originalRenderers,
                    previousOwnedRenderers = previousOwned, finalRenderers = after, collidersBefore = collidersBefore,
                    collidersAfter = collidersAfter, stickers = 12, lightmaps = LightmapSettings.lightmaps.Length,
                    savedLightingReferencesPreserved = sameLighting, passed = true });
                Debug.Log("BEDROOM_STICKERS_SCENE_PASS " + Seasons[season] + " original=" + originalRenderers + " stickers=12 colliders=" + collidersAfter);
            }
        }
        finally
        {
            if (previous.Length == 0 || previous.All(s => string.IsNullOrEmpty(s.path)))
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            else EditorSceneManager.RestoreSceneManagerSetup(previous);
        }
        string output = OutputFolder(root);
        Directory.CreateDirectory(output);
        var report = new ApplyReport { unity = Application.unityVersion, project = Application.dataPath,
            assetRoot = root, operation = validateOnly ? "Validate saved independent stickers" : "Append/replace owned decoration only",
            passed = reports.Count == 4 && reports.All(r => r.passed), scenes = reports.ToArray(),
            evidenceScope = "No geometry rebuild, no lightmapping, no collider changes; saved GI references preserved. Runtime captures are a separate operation." };
        File.WriteAllText(Path.Combine(output, validateOnly ? "unity-validate.json" : "unity-apply.json"), JsonUtility.ToJson(report, true));
        Debug.Log("BEDROOM_STICKERS_" + (validateOnly ? "VALIDATE" : "APPLY") + "_PASS");
    }
}
