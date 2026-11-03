using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

// Inactive during ordinary play. Explicit QA poses are not walking evidence.
[RequireComponent(typeof(Camera))]
public sealed class BedroomStickerCapture : MonoBehaviour
{
    [Serializable] public sealed class StickerTarget { public string Name, Host; public Vector3 Normal; }
    public int SeasonIndex;
    public GameObject StickerRoot;
    public StickerTarget[] Targets;
    static readonly string[] Seasons = { "Spring", "Summer", "Autumn", "Winter" };
    [Serializable] sealed class Shot
    {
        public string name, sticker, host, file;
        public Vector3 eye, target, outwardNormal;
        public float fieldOfView, cameraDistance;
        public bool frontFacingMeasured, colliderSightlineMeasured;
        public bool frontFacing, colliderSightlineClear, nativePngSaved;
        public PoseCheck[] poseAttempts;
        public HostEvidence[] matchingHostRenderers;
    }
    [Serializable] sealed class ColliderEvidence
    {
        public string name, hierarchy, type;
        public Vector3 boundsCenter, boundsSize, closestBoundsPoint, hitPoint;
        public float hitDistance;
        public bool ignoredPlayer, blocks;
    }
    [Serializable] sealed class PoseCheck
    {
        public Vector3 eye, focus;
        public float cameraDistance, overlapRadius = .015f, rayEndTolerance = .025f;
        public bool colliderClear, frontFacing;
        public ColliderEvidence[] overlaps, rayHits;
    }
    [Serializable] sealed class HostEvidence
    {
        public string name, hierarchy;
        public Vector3 boundsCenter, boundsSize, closestBoundsPoint;
        public float distanceToBounds;
    }
    [Serializable] sealed class Report
    {
        public string unity, season, evidenceScope;
        public bool passed, captureCompleted, allInspectionSightlinesClear;
        public int stickers, nativeWidth, nativeHeight;
        public Shot[] captures;
    }
    Camera view;
    BedroomWalk walk;
    string output;

    void Start()
    {
        if (Array.IndexOf(Environment.GetCommandLineArgs(), "--bedroom-stickers") < 0) return;
        StartCoroutine(Guarded(Capture()));
    }
    IEnumerator Guarded(IEnumerator operation)
    {
        while (true)
        {
            bool next = false; object current = null; Exception failure = null;
            try { next = operation.MoveNext(); if (next) current = operation.Current; }
            catch (Exception e) { failure = e; }
            if (failure != null)
            {
                Debug.LogException(failure);
                try
                {
                    if (!string.IsNullOrEmpty(output))
                    { Directory.CreateDirectory(output); File.WriteAllText(Path.Combine(output, "failure.txt"), failure.ToString()); }
                }
                catch (Exception e) { Debug.LogException(e); }
                Application.Quit(2);
                yield break;
            }
            if (!next) yield break;
            yield return current;
        }
    }
    IEnumerator Capture()
    {
        if (SeasonIndex < 0 || SeasonIndex > 3) throw new InvalidOperationException("Invalid sticker capture season.");
        view = GetComponent<Camera>();
        walk = FindObjectOfType<BedroomWalk>();
        if (walk == null) throw new InvalidOperationException("Missing original BedroomWalk.");
        walk.enabled = false;
        Cursor.lockState = CursorLockMode.None; Cursor.visible = false;
        string qa = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "QA", "stickers-20261102"));
        output = Path.Combine(qa, "native", Seasons[SeasonIndex]);
        Directory.CreateDirectory(output);
        if (StickerRoot == null || Targets == null || Targets.Length != 12
            || StickerRoot.GetComponentsInChildren<Renderer>(true).Length != 12)
            throw new InvalidOperationException("Expected twelve authored sticker renderers.");
        yield return new WaitForSecondsRealtime(1f);
        Physics.SyncTransforms();
        var shots = new List<Shot>();
        Vector3 entry = view.transform.position;
        shots.Add(new Shot { name = "entry", eye = entry, target = entry + view.transform.forward, fieldOfView = view.fieldOfView });
        shots.Add(new Shot { name = "reverse", eye = new Vector3(-1.25f, 1.62f, -1.48f),
            target = new Vector3(.25f, 1.20f, 1.55f), fieldOfView = 66 });
        // Farthest-point selection gives three different parts of the room.
        var candidates = Targets.Select(t => new { target = t, renderer = StickerRoot.transform.Find(t.Name) })
            .Select(t => new { t.target, renderer = t.renderer == null ? null : t.renderer.GetComponent<Renderer>() }).ToList();
        if (candidates.Any(c => c.renderer == null)) throw new InvalidOperationException("A sticker target renderer is missing.");
        var chosenCenters = new List<Vector3>();
        for (int group = 0; group < 3; group++)
        {
            int index = 0;
            if (chosenCenters.Count > 0)
            {
                float best = -1;
                for (int i = 0; i < candidates.Count; i++)
                {
                    Vector3 center = candidates[i].renderer.bounds.center;
                    float distance = chosenCenters.Min(p => (p - center).sqrMagnitude);
                    if (distance > best) { best = distance; index = i; }
                }
            }
            var selected = candidates[index]; candidates.RemoveAt(index);
            Vector3 focus = selected.renderer.bounds.center, normal = selected.target.Normal.normalized;
            if (normal.sqrMagnitude < .99f) throw new InvalidOperationException("Sticker has no outward normal.");
            chosenCenters.Add(focus);
            Vector3 eye = focus + normal * .45f;
            bool clear = false;
            var attempts = new List<PoseCheck>();
            var proposedEyes = new List<Vector3>();
            foreach (float distance in new[] { .45f, .60f, .32f, .25f, .65f })
                proposedEyes.Add(focus + normal * distance);
            // A close-up eye may overlap a coarse furniture collider even when
            // the sticker is correctly attached. Try bounded oblique front-side
            // eyes; do not relax physics tolerances or alter any collider.
            Vector3 tangent = Vector3.Cross(Vector3.up, normal);
            if (tangent.sqrMagnitude < .001f) tangent = Vector3.right;
            tangent.Normalize();
            foreach (float lateral in new[] { .25f, -.25f, .40f, -.40f })
                proposedEyes.Add(focus + normal * .40f + tangent * lateral + Vector3.up * .08f);
            foreach (float vertical in new[] { .20f, -.20f })
                proposedEyes.Add(focus + normal * .40f + Vector3.up * vertical);
            foreach (Vector3 proposed in proposedEyes)
            {
                PoseCheck attempt = InspectPose(proposed, focus);
                attempt.frontFacing = Vector3.Dot(proposed - focus, normal) > 0;
                attempts.Add(attempt);
                if (attempt.colliderClear && attempt.frontFacing) { eye = proposed; clear = true; break; }
            }
            var shot = new Shot { name = "detail_" + (group + 1).ToString("00"), sticker = selected.target.Name,
                host = selected.target.Host, eye = eye, target = focus, outwardNormal = normal, fieldOfView = 48,
                cameraDistance = Vector3.Distance(eye, focus), frontFacing = Vector3.Dot(eye - focus, normal) > 0,
                colliderSightlineClear = clear, frontFacingMeasured = true, colliderSightlineMeasured = true,
                poseAttempts = attempts.ToArray(), matchingHostRenderers = HostMatches(selected.target.Host, focus) };
            shots.Add(shot);
            if (!clear)
                Debug.LogWarning("BEDROOM_STICKERS_POSE_OCCLUDED " + JsonUtility.ToJson(shot));
        }
        // Keep diagnostics even if a subsequent native screenshot operation fails.
        File.WriteAllText(Path.Combine(output, "pose-selection.json"), JsonUtility.ToJson(new Report
        {
            unity = Application.unityVersion, season = Seasons[SeasonIndex], captures = shots.ToArray(), stickers = 12,
            evidenceScope = "Pose diagnostics before rendering. Collider clearance is measured without changing colliders. A blocked normal-facing QA camera is still photographed for visual inspection; proxy bounds may differ from visible geometry. Host renderer bounds are diagnostics, not triangle-level attachment proof."
        }, true));
        view.nearClipPlane = .01f;
        foreach (Shot shot in shots)
        {
            Vector3 forward = (shot.target - shot.eye).normalized;
            Vector3 up = Mathf.Abs(Vector3.Dot(forward, Vector3.up)) > .98f ? Vector3.forward : Vector3.up;
            view.transform.SetPositionAndRotation(shot.eye, Quaternion.LookRotation(forward, up));
            view.fieldOfView = shot.fieldOfView;
            yield return new WaitForSecondsRealtime(.20f);
            yield return new WaitForEndOfFrame();
            shot.file = Path.Combine(output, shot.name + ".png");
            if (File.Exists(shot.file)) File.Delete(shot.file);
            ScreenCapture.CaptureScreenshot(shot.file);
            float deadline = Time.realtimeSinceStartup + 8f;
            while ((!File.Exists(shot.file) || new FileInfo(shot.file).Length == 0) && Time.realtimeSinceStartup < deadline)
                yield return null;
            // Screenshot writing finishes asynchronously on the following frames.
            yield return new WaitForSecondsRealtime(.20f);
            shot.nativePngSaved = File.Exists(shot.file) && new FileInfo(shot.file).Length > 100;
            if (!shot.nativePngSaved) throw new IOException("Native screenshot was not saved: " + shot.file);
            Debug.Log("BEDROOM_STICKERS_NATIVE " + Seasons[SeasonIndex] + " " + shot.name);
        }
        bool allClear = shots.Where(s => s.colliderSightlineMeasured).All(s => s.colliderSightlineClear && s.frontFacing);
        var report = new Report { unity = Application.unityVersion, season = Seasons[SeasonIndex], passed = allClear,
            captureCompleted = true, allInspectionSightlinesClear = allClear,
            stickers = 12, nativeWidth = Screen.width, nativeHeight = Screen.height, captures = shots.ToArray(),
            evidenceScope = "Actual standalone player screenshots. Camera transforms are explicit QA inspection poses, not CharacterController walking or reachability evidence. Front-side/collider-sightline checks apply only to details, not entry/reverse. Blocked collider checks remain false, with every attempted pose and blocking collider recorded; no collider was disabled and no room object was moved. Capture completion is separate from clearance/visual acceptance. Three detail views sample dispersed sticker hosts; they do not individually photograph all twelve stickers. PNG decode and visual review are separate external checks." };
        File.WriteAllText(Path.Combine(output, "captures.json"), JsonUtility.ToJson(report, true));
        Debug.Log((allClear ? "BEDROOM_STICKERS_NATIVE_PASS " : "BEDROOM_STICKERS_NATIVE_CAPTURE_WITH_COLLIDER_BLOCKERS ")
            + Seasons[SeasonIndex] + " captures=" + shots.Count);
        if (SeasonIndex < 3) SceneManager.LoadScene("Bedroom_" + Seasons[SeasonIndex + 1]);
        else Application.Quit(0);
    }
    static string Hierarchy(Transform t)
    {
        string path = t.name;
        while (t.parent != null) { t = t.parent; path = t.name + "/" + path; }
        return path;
    }
    ColliderEvidence ColliderRecord(Collider c, Vector3 eye, float distance, Vector3 point, bool blocks)
    {
        return new ColliderEvidence { name = c.name, hierarchy = Hierarchy(c.transform), type = c.GetType().Name,
            boundsCenter = c.bounds.center, boundsSize = c.bounds.size, closestBoundsPoint = c.bounds.ClosestPoint(eye),
            hitDistance = distance, hitPoint = point, ignoredPlayer = c.transform.IsChildOf(walk.transform), blocks = blocks };
    }
    PoseCheck InspectPose(Vector3 eye, Vector3 target)
    {
        var overlaps = new List<ColliderEvidence>();
        var rays = new List<ColliderEvidence>();
        foreach (Collider c in Physics.OverlapSphere(eye, .015f, ~0, QueryTriggerInteraction.Ignore))
            overlaps.Add(ColliderRecord(c, eye, -1, Vector3.zero, !c.transform.IsChildOf(walk.transform)));
        Vector3 delta = target - eye;
        foreach (RaycastHit hit in Physics.RaycastAll(eye, delta.normalized, delta.magnitude, ~0, QueryTriggerInteraction.Ignore).OrderBy(h => h.distance))
            rays.Add(ColliderRecord(hit.collider, eye, hit.distance, hit.point,
                !hit.collider.transform.IsChildOf(walk.transform) && hit.distance < delta.magnitude - .025f));
        return new PoseCheck { eye = eye, focus = target, cameraDistance = delta.magnitude,
            overlaps = overlaps.ToArray(), rayHits = rays.ToArray(),
            colliderClear = !overlaps.Any(c => c.blocks) && !rays.Any(c => c.blocks) };
    }
    static HostEvidence[] HostMatches(string host, Vector3 focus)
    {
        return FindObjectsOfType<Renderer>().Where(r => r.name == host || r.name.StartsWith(host + ".", StringComparison.Ordinal))
            .Select(r => new HostEvidence { name = r.name, hierarchy = Hierarchy(r.transform), boundsCenter = r.bounds.center,
                boundsSize = r.bounds.size, closestBoundsPoint = r.bounds.ClosestPoint(focus),
                distanceToBounds = Vector3.Distance(focus, r.bounds.ClosestPoint(focus)) })
            .OrderBy(r => r.distanceToBounds).Take(8).ToArray();
    }
}
