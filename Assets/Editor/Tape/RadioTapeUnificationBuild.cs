using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;
namespace MemoryWhitebox.Editor
{
    public static class RadioTapeUnificationBuild
    {
        static Collider Spindle(Transform pivot)
        {
            var t=pivot.Find("Inspection spindle hit");
            if(t==null){t=new GameObject("Inspection spindle hit").transform;t.SetParent(pivot,false);}
            t.gameObject.layer=25;var hit=t.GetComponent<SphereCollider>();if(hit==null)hit=t.gameObject.AddComponent<SphereCollider>();
            hit.radius=.009f/Mathf.Max(.001f,Mathf.Abs(pivot.lossyScale.x));return hit;
        }
        public static string InstallHouse()
        {
            if(Application.isPlaying)throw new Exception("Stop Play Mode");
            var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(scene.name!="HouseScene")throw new Exception("Open HouseScene");
            var h=Object.FindObjectOfType<WBMain>();var d=h.radioDeck;
            var tapeGroup=d.tapes[0].root.parent;var tapeGroupPosition=tapeGroup.position;tapeGroupPosition.z=d.transform.position.z-.45f;tapeGroup.position=tapeGroupPosition;
            if(d.stationLight==null){var lamp=new GameObject("Tape operation fill light");lamp.transform.SetParent(d.transform,false);lamp.transform.localPosition=new Vector3(0,1,-.8f);d.stationLight=lamp.AddComponent<Light>();d.stationLight.type=LightType.Point;d.stationLight.range=2.8f;d.stationLight.intensity=.7f;d.stationLight.color=new Color(1,.93f,.82f);d.stationLight.enabled=false;}
            const string path="Assets/Art/Props/Radio/Materials/DetectedTapeDamage.mat";
            var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(mat==null){mat=new Material(Shader.Find("Standard")){color=new Color(.32f,.12f,.05f)};AssetDatabase.CreateAsset(mat,path);}
            foreach(var tape in d.tapes)
            {
                tape.leftSpindleHit=Spindle(tape.leftReel);tape.rightSpindleHit=Spindle(tape.rightReel);
                if(tape.damageMarker==null)
                {
                    var root=new GameObject("Detected broken tape ends").transform;root.SetParent(tape.root,false);
                    for(int side=0;side<2;side++)
                    {
                        var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name="Broken end "+side;go.transform.SetParent(root,false);
                        go.GetComponent<Renderer>().sharedMaterial=mat;Object.DestroyImmediate(go.GetComponent<Collider>());
                        go.transform.localPosition=new Vector3(side==0?-.009f:.009f,-.021f,-.01f);go.transform.localScale=new Vector3(.017f,.002f,.001f);go.transform.localRotation=Quaternion.Euler(0,side==0?16:-12,side==0?14:-22);
                    }
                    tape.damageMarker=root.gameObject;root.gameObject.SetActive(false);
                }
            }
            SafeInspectionBuild.RefreshRadioSubjects(d.transform,d.tapes[0].root.parent);
            EditorUtility.SetDirty(d);EditorUtility.SetDirty(h.safeInspection);EditorSceneManager.MarkSceneDirty(scene);AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);
            return "New radio spindle hits, damage cues, per-tape inspection and old inspection aliases updated";
        }
    }
}
