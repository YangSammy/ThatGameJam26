using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MemoryWhitebox.Editor
{
    public static class WardRigAuthoring
    {
        const string ScenePath = "Assets/Scenes/Bedroom/V17/Bedroom_Winter.unity";
        const string CharacterPath = "Assets/Art/Characters/ParkGirlTripoRigged/Prefabs/PF_ParkGirlTripo_RiggedReview.prefab";
        const string SourceMaterialPath = "Assets/Art/Characters/ParkGirlTripoRigged/Materials/M_ParkGirlTripoRigged.mat";
        const string WardMaterialPath = "Assets/Art/Props/AlbumWinterE/Materials/Ward/Ward_SeatedParkGirl.mat";
        const string RigName = "Seated ParkGirl Rig";

        public static string Author()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
                throw new InvalidOperationException("Ward authoring requires Unity Edit Mode.");
            var previous = SceneManager.GetActiveScene();
            var scene = SceneManager.GetSceneByPath(ScenePath);
            bool opened = !scene.isLoaded;
            if (!opened && scene.isDirty)
                throw new InvalidOperationException("Save or discard unrelated Winter scene edits before authoring the ward.");
            if (opened) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                var layout = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<WinterAlbumWorkspace>(true)).Single();
                var ward = layout.wardSet;
                var figure = ward.Find("Bed/Seated_Figure");
                if (figure == null || layout.revealCutoff == null || layout.revealStart == null)
                    throw new InvalidOperationException("Winter ward anchors are incomplete.");
                if (figure.Find(RigName) != null) return Describe(layout, figure.Find(RigName));
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CharacterPath);
                var sourceMaterial = AssetDatabase.LoadAssetAtPath<Material>(SourceMaterialPath);
                var shader = Shader.Find("Memory/WardReveal");
                if (prefab == null || sourceMaterial == null || shader == null)
                    throw new InvalidOperationException("Missing formal ParkGirl prefab, albedo, or WardReveal shader.");

                var rig = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                PrefabUtility.UnpackPrefabInstance(rig, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                rig.name = RigName;
                rig.transform.SetParent(figure, false);
                rig.transform.localRotation = Quaternion.Euler(0, 155, 0);
                foreach (var animator in rig.GetComponentsInChildren<Animator>(true)) animator.enabled = false;
                foreach (var child in rig.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = ward.gameObject.layer;
                foreach (var collider in rig.GetComponentsInChildren<Collider>(true)) collider.enabled = false;

                Pose(rig.transform, figure, layout.revealCutoff);
                var wardMaterial = AssetDatabase.LoadAssetAtPath<Material>(WardMaterialPath);
                if (wardMaterial == null)
                {
                    wardMaterial = new Material(shader) { name = "Ward_SeatedParkGirl" };
                    wardMaterial.SetTexture("_MainTex", sourceMaterial.mainTexture);
                    wardMaterial.SetColor("_Color", new Color(.94f, .96f, .96f));
                    wardMaterial.SetFloat("_WardBacklight", 1);
                    wardMaterial.SetFloat("_Height", 1000);
                    AssetDatabase.CreateAsset(wardMaterial, WardMaterialPath);
                }
                foreach (var renderer in rig.GetComponentsInChildren<Renderer>(true))
                {
                    var slots = renderer.sharedMaterials;
                    for (int i = 0; i < slots.Length; i++) slots[i] = wardMaterial;
                    renderer.sharedMaterials = slots;
                }
                foreach (Transform child in figure)
                    if (child != rig.transform && child != layout.revealCutoff) child.gameObject.SetActive(false);

                string description = Describe(layout, rig.transform);
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Could not save Winter ward scene.");
                return description;
            }
            finally
            {
                if (opened) EditorSceneManager.CloseScene(scene, true);
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
            }
        }

        static Transform Bone(Transform root, string name) => root.GetComponentsInChildren<Transform>(true).Single(t => t.name == name);

        static void Aim(Transform bone, Transform tip, Vector3 direction)
        {
            bone.rotation = Quaternion.FromToRotation(tip.position - bone.position, direction) * bone.rotation;
        }

        static void Pose(Transform rig, Transform figure, Transform cutoff)
        {
            var hips = Bone(rig, "Hips");
            rig.position += figure.TransformPoint(new Vector3(0, .75f, -.54f)) - hips.position;
            var front = rig.forward;
            var right = rig.right;
            var spine = Bone(rig, "Spine");
            spine.rotation = Quaternion.AngleAxis(8, right) * spine.rotation;
            foreach (string side in new[] { "Left", "Right" })
            {
                float sign = side == "Left" ? -1 : 1;
                var thigh = Bone(rig, side + "_UpperLeg");
                var knee = Bone(rig, side + "_LowerLeg");
                var foot = Bone(rig, side + "_Foot");
                Aim(thigh, knee, front + Vector3.down * .19f + right * sign * .12f);
                Aim(knee, foot, Vector3.down + front * .08f);
                Aim(foot, Bone(rig, side + "_Toes"), front + Vector3.down * .09f);
                var arm = Bone(rig, side + "_UpperArm");
                var elbow = Bone(rig, side + "_LowerArm");
                Aim(arm, elbow, Vector3.down + front * .18f + right * sign * .16f);
                Aim(elbow, Bone(rig, side + "_Hand"), front + Vector3.down * .54f - right * sign * .48f);
            }

            // Camera looks toward +Z. Keep the eyes turned away and use the original
            // textured hair silhouette and a subdued rim rather than frontal fill.
            var turn = Quaternion.AngleAxis(-100, Vector3.up);
            var face = turn * front;
            var down = Quaternion.AngleAxis(24, Vector3.Cross(Vector3.up, face));
            var neck = Bone(rig, "Neck");
            neck.rotation = Quaternion.AngleAxis(-47, Vector3.up) * neck.rotation;
            var head = Bone(rig, "Head");
            head.rotation = down * Quaternion.AngleAxis(-53, Vector3.up) * head.rotation;
            var eyes = (Bone(rig, "Left_Eye").position + Bone(rig, "Right_Eye").position) * .5f;
            cutoff.SetParent(head, true);
            cutoff.name = "Reveal_Cutoff_Nose";
            cutoff.position = eyes - (down * Vector3.up) * .047f + (down * face) * .035f;
        }

        static string Describe(WinterAlbumWorkspace layout, Transform rig)
        {
            var cutoff = layout.revealCutoff;
            if (cutoff.parent != Bone(rig, "Head") || cutoff.position.y <= layout.revealStart.position.y)
                throw new InvalidOperationException("Ward cutoff must follow the actual head bone above the reveal origin.");
            var surfaces = layout.wardSet.GetComponentsInChildren<Renderer>(true);
            if (surfaces.Any(r => r.sharedMaterials.Any(m => m == null || m.shader.name != "Memory/WardReveal")))
                throw new InvalidOperationException("Every ward renderer slot must use the world-height reveal shader.");
            return "Ward seated rig authored. Reveal renderers=" + surfaces.Length + ", nose world Y=" + cutoff.position.y.ToString("F3");
        }
    }
}
