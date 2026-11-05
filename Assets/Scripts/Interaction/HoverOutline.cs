using UnityEngine;
using UnityEngine.Rendering;

namespace MemoryWhitebox
{
    // Draws a silhouette on its owning camera without changing the item's materials.
    internal sealed class HoverOutline
    {
        CameraEvent stage = CameraEvent.AfterForwardAlpha;
        Camera camera;
        CommandBuffer commands;
        Material material, contrastInk;
        Renderer[] selection;
        bool[] visible, outlineOnly;
        bool previousFill, previousRecessed, previousContrast;
        Transform root;
        Renderer[] rootRenderers;

        public Renderer[] Surfaces(Transform target)
        {
            if (!target) return null;
            if (root != target)
            {
                root = target;
                rootRenderers = target.GetComponentsInChildren<Renderer>(true);
            }
            return rootRenderers;
        }

        public void Show(Camera view, Renderer[] renderers, bool fillOpenings = false, bool recessed = false, bool contrast = false, bool afterEffects = false)
        {
            if (!view || renderers == null || renderers.Length == 0) { Clear(); return; }
            var nextStage=afterEffects?CameraEvent.AfterImageEffects:CameraEvent.AfterForwardAlpha;
            bool changed = selection != renderers;
            bool rebuild = stage!=nextStage || camera != view || changed || previousFill!=fillOpenings ||
                previousRecessed!=recessed || previousContrast!=contrast;
            if(changed)
            {
                outlineOnly=new bool[renderers.Length];
                for(int i=0;i<renderers.Length;i++)outlineOnly[i]=renderers[i] && renderers[i].GetComponent<HoverOutlineProxy>()!=null;
            }
            if (visible == null || visible.Length != renderers.Length) visible = new bool[renderers.Length];
            Bounds bounds = default;
            bool any = false;
            for (int i = 0; i < renderers.Length; i++)
            {
                var r = renderers[i];
                bool draw = r && (r.enabled || outlineOnly[i]) && !r.forceRenderingOff && r.gameObject.activeInHierarchy &&
                    (view.cullingMask & (1 << r.gameObject.layer)) != 0 &&
                    (r is MeshRenderer || r is SkinnedMeshRenderer);
                rebuild |= draw != visible[i];
                visible[i] = draw;
                if (!draw) continue;
                if (fillOpenings)
                {
                    if (any) bounds.Encapsulate(r.bounds); else bounds = r.bounds;
                }
                any = true;
            }
            if (!any) { Clear(); return; }
            if (!material)
            {
                var shader = Resources.Load<Shader>("TapeHoverOutline");
                if (!shader || !shader.isSupported) return;
                material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            }
            if (fillOpenings) material.SetVector("_OutlineCenter", bounds.center);
            if (!rebuild) return;
            Clear();
            material.SetFloat("_FillOpenings", fillOpenings ? 1 : 0);
            material.SetInt("_EdgeDepthTest", (int)(recessed ? CompareFunction.Always : CompareFunction.LessEqual));
            if (commands == null) commands = new CommandBuffer { name = "Interaction hover outline" };
            commands.Clear();
            // A narrow dark separator keeps white legible on pale paper and sunlit wood.
            if (contrast && !contrastInk)
            {
                contrastInk = new Material(material) { hideFlags = HideFlags.HideAndDontSave };
                contrastInk.SetFloat("_EdgeWidth", 2.5f);
                contrastInk.SetColor("_InkColor", new Color(.23f, .23f, .23f, 1));
            }
            // Stamp the complete selection before expanding any edge. Shared seams stay unpainted.
            for (int pass = 0; pass < 3; pass++)
            {
                if (pass == 1 && contrast) DrawPass(renderers,contrastInk,pass);
                DrawPass(renderers,material,pass);
            }
            camera = view;
            selection = renderers;
            previousFill=fillOpenings;previousRecessed=recessed;previousContrast=contrast;
            stage=nextStage;
            camera.AddCommandBuffer(stage, commands);
        }

        void DrawPass(Renderer[] renderers, Material ink, int pass)
        {
                for (int i = 0; i < renderers.Length; i++)
                {
                    if (!visible[i]) continue;
                    var r = renderers[i];
                    var mesh = r is SkinnedMeshRenderer skin ? skin.sharedMesh : r.GetComponent<MeshFilter>()?.sharedMesh;
                    if (!mesh) continue;
                    for (int sub = 0; sub < mesh.subMeshCount; sub++)
                    {
                        if(outlineOnly[i])commands.DrawMesh(mesh,r.localToWorldMatrix,ink,sub,pass);
                        else commands.DrawRenderer(r,ink,sub,pass);
                    }
                }
        }

        public void Clear()
        {
            if (camera && commands != null) camera.RemoveCommandBuffer(stage, commands);
            camera = null;
            selection = null;
        }

        public void Dispose()
        {
            Clear();
            commands?.Release();
            commands = null;
            if (material) Object.Destroy(material);
            if (contrastInk) Object.Destroy(contrastInk);
        }
    }
}
