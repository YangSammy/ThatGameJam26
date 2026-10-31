using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;

// Opt-in runtime experiments. These never edit the scene, material assets or bake.
public static class BedroomMaterialDiagnostics
{
    [Serializable] sealed class Sample
    {
        public string label, image, shader, material, mainTexture, normalTexture,
            metallicTexture, occlusionTexture, receiveGI, lightProbeUsage, lightmapTexture;
        public Color tint;
        public int lightmapIndex, uv0Count, uv2Count, vertexCount;
        public float normalScale, smoothness;
        public Vector4 lightmapScaleOffset;
        public Vector3 boundsCenter, boundsSize, probePosition, evaluatedNormal;
        public Color surfaceProbeIrradiance, anchorProbeIrradiance;
        public bool normalKeyword, usesDiagnosticAnchor;
    }
    [Serializable] sealed class Report
    {
        public string purpose = "Temporary receiver/material isolation; not production or a visual quality result";
        public string unity, renderer, graphicsApi, gpu, capturedAtUtc;
        public bool diagnosticOnly = true, linearColor, restored, allImagesPresent;
        public int width, height;
        public Vector3 eye, cameraEuler;
        public float fieldOfView;
        public Sample[] samples;
    }

    public static IEnumerator Run(Camera view)
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = false;
        string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "QA", "receiver-diagnostic-v5"));
        Directory.CreateDirectory(folder);
        MeshRenderer receiver = null;
        Material walnut = null;
        foreach (var renderer in UnityEngine.Object.FindObjectsOfType<MeshRenderer>())
        {
            if (renderer.name.StartsWith("North_wainscot", StringComparison.Ordinal)) receiver = renderer;
            foreach (var material in renderer.sharedMaterials)
                if (material != null && material.name == "Walnut" || material != null && material.name.StartsWith("Walnut (", StringComparison.Ordinal)) walnut = material;
        }
        if (receiver == null || walnut == null)
        {
            Debug.LogError("BEDROOM_RECEIVER_DIAGNOSTIC_FAIL missing North_wainscot or Walnut");
            Application.Quit(2);
            yield break;
        }
        Material[] originalMaterials = receiver.sharedMaterials;
        int originalIndex = receiver.lightmapIndex;
        Vector4 originalScale = receiver.lightmapScaleOffset;
        LightProbeUsage originalProbeUsage = receiver.lightProbeUsage;
        Transform originalAnchor = receiver.probeAnchor;
        var noNormal = new Material(originalMaterials[0]);
        noNormal.DisableKeyword("_NORMALMAP");
        noNormal.SetFloat("_BumpScale", 0);
        var gray = new Material(Shader.Find("Standard"));
        gray.name = "Diagnostic_18_percent_gray";
        gray.color = new Color(.18f, .18f, .18f, 1).gamma;
        gray.SetFloat("_Metallic", 0);
        gray.SetFloat("_Glossiness", .25f);
        var anchorObject = new GameObject("Temporary_receiver_diagnostic_probe_anchor");
        anchorObject.transform.position = new Vector3(0, .45f, -2.3f);
        var samples = new List<Sample>();
        var report = new Report
        {
            unity = Application.unityVersion, renderer = receiver.name,
            graphicsApi = SystemInfo.graphicsDeviceType.ToString(), gpu = SystemInfo.graphicsDeviceName,
            capturedAtUtc = DateTime.UtcNow.ToString("o"),
            width = Screen.width, height = Screen.height,
            linearColor = QualitySettings.activeColorSpace == ColorSpace.Linear,
            eye = view.transform.position, cameraEuler = view.transform.eulerAngles, fieldOfView = view.fieldOfView
        };
        try
        {
            // Keep camera, exposure, lights, geometry and the original baked maps fixed.
            string[] labels = { "A_baseline", "B_no_normal", "C_gray18_lightmap", "D_original_probes", "E_walnut_lightmap", "F_walnut_probes" };
            for (int i = 0; i < labels.Length; i++)
            {
                bool probes = i == 3 || i == 5;
                receiver.sharedMaterials = i == 1 ? new[] { noNormal } : i == 2 ? new[] { gray }
                    : i >= 4 ? new[] { walnut } : originalMaterials;
                receiver.lightmapIndex = probes ? -1 : originalIndex;
                receiver.lightmapScaleOffset = originalScale;
                receiver.lightProbeUsage = probes ? LightProbeUsage.BlendProbes : originalProbeUsage;
                receiver.probeAnchor = probes ? anchorObject.transform : originalAnchor;
                yield return new WaitForSecondsRealtime(.35f);
                samples.Add(Inspect(receiver, labels[i], probes));
                yield return new WaitForEndOfFrame();
                ScreenCapture.CaptureScreenshot(Path.Combine(folder, labels[i] + ".png"));
                yield return new WaitForSecondsRealtime(.4f);
            }
        }
        finally
        {
            receiver.sharedMaterials = originalMaterials;
            receiver.lightmapIndex = originalIndex;
            receiver.lightmapScaleOffset = originalScale;
            receiver.lightProbeUsage = originalProbeUsage;
            receiver.probeAnchor = originalAnchor;
            UnityEngine.Object.Destroy(noNormal);
            UnityEngine.Object.Destroy(gray);
            UnityEngine.Object.Destroy(anchorObject);
            report.restored = true;
            report.samples = samples.ToArray();
            report.allImagesPresent = samples.Count == 6;
            foreach (var sample in samples)
                report.allImagesPresent &= File.Exists(Path.Combine(folder, sample.image));
            File.WriteAllText(Path.Combine(folder, "receiver-diagnostic.json"), JsonUtility.ToJson(report, true));
        }
        Debug.Log("BEDROOM_RECEIVER_DIAGNOSTIC_PASS samples=" + samples.Count + " restored=" + report.restored + " images=" + report.allImagesPresent);
        Application.Quit(report.allImagesPresent ? 0 : 3);
    }

    static Sample Inspect(MeshRenderer receiver, string label, bool probes)
    {
        Material material = receiver.sharedMaterial;
        var filter = receiver.GetComponent<MeshFilter>();
        Mesh mesh = filter != null ? filter.sharedMesh : null;
        Vector3 normal = Vector3.forward; // Actual room-facing north wall normal after FBX axis conversion.
        Vector3 surface = receiver.bounds.center + normal * .03f;
        Vector3 anchor = receiver.probeAnchor != null ? receiver.probeAnchor.position : receiver.bounds.center;
        var sample = new Sample
        {
            label = label, image = label + ".png", shader = material.shader.name, material = material.name,
            tint = material.color, mainTexture = TextureName(material, "_MainTex"),
            normalTexture = TextureName(material, "_BumpMap"),
            metallicTexture = TextureName(material, "_MetallicGlossMap"),
            occlusionTexture = TextureName(material, "_OcclusionMap"),
            normalKeyword = material.IsKeywordEnabled("_NORMALMAP"),
            normalScale = material.HasProperty("_BumpScale") ? material.GetFloat("_BumpScale") : 0,
            smoothness = material.HasProperty("_Glossiness") ? material.GetFloat("_Glossiness") : 0,
            receiveGI = receiver.lightmapIndex >= 0 && receiver.lightmapIndex < LightmapSettings.lightmaps.Length ? "Inferred mapped receiver from lightmapIndex; receiveGI is not read at runtime" : "Inferred probe receiver from unmapped lightmapIndex; receiveGI is not read at runtime", lightProbeUsage = receiver.lightProbeUsage.ToString(),
            lightmapIndex = receiver.lightmapIndex, lightmapScaleOffset = receiver.lightmapScaleOffset,
            boundsCenter = receiver.bounds.center, boundsSize = receiver.bounds.size,
            uv0Count = mesh != null ? mesh.uv.Length : 0, uv2Count = mesh != null ? mesh.uv2.Length : 0,
            vertexCount = mesh != null ? mesh.vertexCount : 0,
            probePosition = anchor, evaluatedNormal = normal, usesDiagnosticAnchor = probes,
            surfaceProbeIrradiance = EvaluateProbe(surface, normal, receiver),
            anchorProbeIrradiance = EvaluateProbe(anchor, normal, receiver)
        };
        int index = receiver.lightmapIndex;
        if (index >= 0 && index < LightmapSettings.lightmaps.Length)
        {
            var map = LightmapSettings.lightmaps[index].lightmapColor;
            sample.lightmapTexture = map != null ? map.name : "missing";
        }
        return sample;
    }

    static string TextureName(Material material, string slot)
    {
        var texture = material.HasProperty(slot) ? material.GetTexture(slot) : null;
        return texture != null ? texture.name : "";
    }
    static Color EvaluateProbe(Vector3 point, Vector3 normal, Renderer receiver)
    {
        SphericalHarmonicsL2 probe;
        LightProbes.GetInterpolatedProbe(point, receiver, out probe);
        var colors = new Color[1];
        probe.Evaluate(new[] { normal }, colors);
        return colors[0];
    }
}

