using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace MemoryWhitebox.Editor
{
    public static class TapeThreeCopyAuthoring
    {
        public static string Build()
        {
            if(EditorApplication.isPlaying)throw new System.InvalidOperationException("Exit Play first.");
            var d=Object.FindObjectOfType<HouseRadioDeck>();
            if(d==null || d.originalTape==null)throw new System.InvalidOperationException("Open the authored HouseScene.");
            Undo.RecordObject(d,"Three tape copies");
            if(d.originals==null || d.originals.Length!=3)
            {
                d.originals=new HouseRadioDeck.OriginalCassette[3];
                d.originals[0]=new HouseRadioDeck.OriginalCassette{tape=d.originalTape,home=d.originalHome,hit=d.originalHit,reels=d.originalReels};
                for(int i=1;i<3;i++)
                {
                    var home=new GameObject("OriginalTapeHome_"+(i+1).ToString("00")).transform;Undo.RegisterCreatedObjectUndo(home.gameObject,"Original home");home.SetParent(d.originalHome.parent,false);
                    home.localPosition=d.originalHome.localPosition+new Vector3(-.22f*i,0,0);home.localRotation=d.originalHome.localRotation;
                    var clone=Object.Instantiate(d.originalTape.gameObject,d.originalTape.parent);Undo.RegisterCreatedObjectUndo(clone,"Original cassette");clone.name="OriginalTape_"+(i+1).ToString("00");clone.transform.SetPositionAndRotation(home.position,home.rotation);
                    clone.GetComponentInChildren<WorldTooltipLabel>(true).text="ORIGINAL "+(i+1).ToString("00");
                    var reels=new System.Collections.Generic.List<Transform>();foreach(var t in clone.GetComponentsInChildren<Transform>(true))foreach(var original in d.originalReels)if(t.name==original.name)reels.Add(t);
                    d.originals[i]=new HouseRadioDeck.OriginalCassette{tape=clone.transform,home=home,hit=clone.GetComponent<Collider>(),reels=reels.ToArray()};
                }
            }
            if(d.completedCopies==null || d.completedCopies.Length!=3)
            {
                d.completedCopies=new GameObject[3];
                var rack=new GameObject("CompletedTapeCopies").transform;Undo.RegisterCreatedObjectUndo(rack.gameObject,"Completed copies");rack.SetParent(d.transform,false);rack.localPosition=new Vector3(-.53f,.055f,-.07f);
                for(int i=0;i<3;i++)
                {
                    var copy=Object.Instantiate(d.originals[i].tape.gameObject,rack);Undo.RegisterCreatedObjectUndo(copy,"Completed copy");copy.name="CompletedCopy_"+(i+1).ToString("00");copy.transform.localPosition=new Vector3(-.22f*i,0,0);copy.transform.rotation=d.originalHome.rotation;
                    foreach(var collider in copy.GetComponentsInChildren<Collider>(true))Object.DestroyImmediate(collider);
                    var label=copy.GetComponentInChildren<WorldTooltipLabel>(true);label.text="COPY "+(i+1).ToString("00");label.target=copy.transform;d.completedCopies[i]=copy;copy.SetActive(false);
                }
            }
            EditorUtility.SetDirty(d);EditorSceneManager.MarkSceneDirty(d.gameObject.scene);EditorSceneManager.SaveScene(d.gameObject.scene);
            return "Three original cassettes and three completion displays authored; existing recorder and anchors retained.";
        }
    }
}
