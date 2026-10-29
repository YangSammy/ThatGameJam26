using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace MemoryWhitebox.Editor
{
    public static class WorldTooltipMigration
    {
        public static string ConvertScene(Scene scene)
        {
            if(EditorApplication.isPlaying)throw new System.InvalidOperationException("Exit Play first.");
            var roots=scene.GetRootGameObjects();int count=0;
            foreach(var text in roots.SelectMany(r=>r.GetComponentsInChildren<TextMesh>(true)).ToArray())
            {
                var label=Undo.AddComponent<WorldTooltipLabel>(text.gameObject);label.text=text.text;
                if(text.name=="WriteProtectGuide")label.target=text.transform.parent;
                else label.ResolveTarget();
                var renderer=text.GetComponent<MeshRenderer>();var filter=text.GetComponent<MeshFilter>();
                Undo.DestroyObjectImmediate(text);if(renderer!=null)Undo.DestroyObjectImmediate(renderer);if(filter!=null)Undo.DestroyObjectImmediate(filter);
                count++;
            }
            foreach(var subject in roots.SelectMany(r=>r.GetComponentsInChildren<SafeInspectionSubject>(true)))
            {
                Undo.RecordObject(subject,"Remove obsolete text renderer references");subject.renderers=subject.renderers.Where(p=>p.source!=null && p.display!=null).ToArray();
                foreach(var pair in subject.poses)
                {
                    var source=pair.source.GetComponent<WorldTooltipLabel>();var display=pair.display.GetComponent<WorldTooltipLabel>();
                    if(source!=null && display!=null){display.mirror=source;EditorUtility.SetDirty(display);}
                }
                EditorUtility.SetDirty(subject);
            }
            foreach(var deck in roots.SelectMany(r=>r.GetComponentsInChildren<HouseRadioDeck>(true)))
            {
                Undo.RecordObject(deck,"Migrate counter references");
                var labels=deck.GetComponentsInChildren<WorldTooltipLabel>(true);
                deck.sourceCounter=labels.FirstOrDefault(t=>t.name=="SourceCounter_A");deck.targetCounter=labels.FirstOrDefault(t=>t.name=="WorkCounter_B");
                if(deck.targetCounter!=null && deck.radioHit!=null)deck.targetCounter.target=deck.radioHit.transform;
                // Finished copies are display-only but still need a hover surface.
                foreach(var copy in deck.completedCopies)
                {
                    if(copy==null)continue;
                    var hit=copy.GetComponent<BoxCollider>();if(hit==null){hit=Undo.AddComponent<BoxCollider>(copy);hit.center=deck.originalHit.GetComponent<BoxCollider>().center;hit.size=deck.originalHit.GetComponent<BoxCollider>().size;}
                    copy.layer=25;foreach(var t in copy.GetComponentsInChildren<WorldTooltipLabel>(true))t.target=copy.transform;
                }
                EditorUtility.SetDirty(deck);
            }
            foreach(var label in roots.SelectMany(r=>r.GetComponentsInChildren<WorldTooltipLabel>(true)))
            {
                if(label.text.StartsWith("PARK NOTICE"))
                {
                    var paper=label.transform.parent.Find("Community notice paper");
                    if(paper!=null){if(paper.GetComponent<Collider>()==null)Undo.AddComponent<BoxCollider>(paper.gameObject);label.target=paper;}
                }
                EditorUtility.SetDirty(label);
            }
            foreach(var subject in roots.SelectMany(r=>r.GetComponentsInChildren<SafeInspectionSubject>(true)))
                foreach(var pair in subject.poses)
                {
                    if(pair.source==null || pair.display==null)continue;
                    var source=pair.source.GetComponent<WorldTooltipLabel>();var display=pair.display.GetComponent<WorldTooltipLabel>();
                    if(source==null || display==null || source.target==null)continue;
                    foreach(var targetPair in subject.poses)if(targetPair.source==source.target){display.target=targetPair.display;EditorUtility.SetDirty(display);break;}
                }
            if(!roots.SelectMany(r=>r.GetComponentsInChildren<WorldTooltipPresenter>(true)).Any())BuildUI(scene);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            return scene.path+": migrated "+count+" world labels";
        }
        static void BuildUI(Scene scene)
        {
            var root=new GameObject("WorldTooltipCanvas",typeof(RectTransform),typeof(Canvas),typeof(WorldTooltipPresenter));Undo.RegisterCreatedObjectUndo(root,"World tooltip UI");SceneManager.MoveGameObjectToScene(root,scene);root.layer=5;
            var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=80;
            var bubble=new GameObject("Tooltip",typeof(RectTransform),typeof(Image));bubble.layer=5;bubble.transform.SetParent(root.transform,false);
            var r=bubble.GetComponent<RectTransform>();r.pivot=new Vector2(0,1);r.sizeDelta=new Vector2(320,70);var image=bubble.GetComponent<Image>();image.color=new Color(.055f,.07f,.06f,.94f);image.raycastTarget=false;
            var text=new GameObject("Text",typeof(RectTransform),typeof(Text));text.layer=5;text.transform.SetParent(bubble.transform,false);var tr=text.GetComponent<RectTransform>();tr.anchorMin=Vector2.zero;tr.anchorMax=Vector2.one;tr.offsetMin=new Vector2(12,10);tr.offsetMax=new Vector2(-12,-10);
            var label=text.GetComponent<Text>();label.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");label.fontSize=15;label.color=new Color(.94f,.94f,.88f);label.raycastTarget=false;label.alignment=TextAnchor.MiddleLeft;
            var presenter=root.GetComponent<WorldTooltipPresenter>();presenter.bubble=r;presenter.label=label;bubble.SetActive(false);
        }
    }
}
