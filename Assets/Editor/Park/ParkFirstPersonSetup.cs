using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace MemoryWhitebox.EditorTools
{
    public static class ParkFirstPersonSetup
    {
        [MenuItem("Tools/公园摄影/连接现有书房第一人称双手")]
        public static string Connect()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("请先停止 Play。");
            var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if(scene.path!="Assets/Scenes/ParkScene.unity")throw new InvalidOperationException("仅在 ParkScene 中使用。");
            var prototype=UnityEngine.Object.FindObjectsOfType<ParkPhotoPrototype>(true).FirstOrDefault(p=>p.gameObject.scene==scene);
            if(prototype==null)throw new InvalidOperationException("未找到公园摄影原型。");
            var hands=prototype.GetComponentsInChildren<FirstPersonHands>(true).FirstOrDefault();
            if(hands==null || hands.authoredRoot==null || hands.authoredHands.Length!=2 || hands.authoredHands.Any(h=>h==null))
                throw new InvalidOperationException("请将书房第一人称镜头及其完整双手副本放在公园 Player 下，再连接。");
            var camera=hands.GetComponent<Camera>();
            var template=prototype.focusEffect!=null?prototype.focusEffect.template:AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/ParkPhoto/ManualFocus.mat");
            Undo.RecordObject(prototype,"绑定公园玩家镜头");prototype.view=camera;
            var focus=camera.GetComponent<ParkPhotoFocus>();if(focus==null)focus=Undo.AddComponent<ParkPhotoFocus>(camera.gameObject);
            Undo.RecordObject(focus,"绑定手动调焦");focus.template=template;focus.enabled=true;prototype.focusEffect=focus;
            Undo.RecordObject(camera,"启用玩家镜头");camera.enabled=true;camera.cullingMask|=1<<FirstPersonHands.HandLayer;camera.gameObject.tag="MainCamera";
            foreach(var other in UnityEngine.Object.FindObjectsOfType<Camera>(true))
            {
                if(other==camera || other.gameObject.scene!=scene || !other.CompareTag("MainCamera"))continue;
                Undo.RecordObject(other,"停用重复主相机");other.enabled=false;
                var listener=other.GetComponent<AudioListener>();if(listener!=null){Undo.RecordObject(listener,"停用重复监听器");listener.enabled=false;}
                var otherFocus=other.GetComponent<ParkPhotoFocus>();if(otherFocus!=null){Undo.RecordObject(otherFocus,"停用备用镜头效果");otherFocus.enabled=false;}
            }
            var ownListener=camera.GetComponent<AudioListener>();if(ownListener==null)ownListener=Undo.AddComponent<AudioListener>(camera.gameObject);ownListener.enabled=true;
            Undo.RecordObject(hands,"连接公园手部动作");hands.park=prototype;hands.enabled=true;
            var poses=camera.transform.Find("Park hand poses - 可编辑手位");
            if(poses==null){var go=new GameObject("Park hand poses - 可编辑手位");go.transform.SetParent(camera.transform,false);Undo.RegisterCreatedObjectUndo(go,"创建公园手位定位");poses=go.transform;}
            if(hands.parkRestTargets==null || hands.parkRestTargets.Length!=2)hands.parkRestTargets=new Transform[2];
            if(hands.parkAimTargets==null || hands.parkAimTargets.Length!=2)hands.parkAimTargets=new Transform[2];
            if(hands.parkArms==null || hands.parkArms.Length!=2)hands.parkArms=new Transform[2];
            for(int i=0;i<2;i++)
            {
                if(hands.parkRestTargets[i]==null)hands.parkRestTargets[i]=Pose(poses,i==0?"Left Rest - 左手静置":"Right Rest - 右手静置",new Vector3(i==0?-.23f:.23f,-.27f,.65f),new Vector3(15,0,0));
                if(hands.parkAimTargets[i]==null)hands.parkAimTargets[i]=Pose(poses,i==0?"Left Aim - 左手举机":"Right Aim - 右手举机",new Vector3(i==0?-.2f:.22f,i==0?-.2f:-.18f,.4f),new Vector3(10,i==0?12:-12,0));
                var hand=hands.authoredHands[i];
                if(hand.childCount>1){var sleeve=hand.GetChild(1);Undo.RecordObject(sleeve.gameObject,"隐藏短袖口");sleeve.gameObject.SetActive(false);}
                if(hands.parkArms[i]==null)
                {
                    var arm=GameObject.CreatePrimitive(PrimitiveType.Capsule);arm.name=i==0?"Player left forearm":"Player right forearm";arm.transform.SetParent(hands.authoredRoot,false);
                    UnityEngine.Object.DestroyImmediate(arm.GetComponent<Collider>());arm.GetComponent<Renderer>().sharedMaterial=hands.sleeveMaterial;
                    Undo.RegisterCreatedObjectUndo(arm,"创建第一人称前臂");hands.parkArms[i]=arm.transform;
                }
            }
            foreach(var t in hands.authoredRoot.GetComponentsInChildren<Transform>(true)){Undo.RecordObject(t.gameObject,"设置第一人称显示层");t.gameObject.layer=FirstPersonHands.HandLayer;}
            foreach(var r in hands.authoredRoot.GetComponentsInChildren<Renderer>(true)){Undo.RecordObject(r,"设置手部渲染");r.forceRenderingOff=false;r.shadowCastingMode=ShadowCastingMode.Off;}
            foreach(var c in hands.authoredRoot.GetComponentsInChildren<Collider>(true))Undo.DestroyObjectImmediate(c);
            hands.authoredRoot.gameObject.SetActive(true);
            hands.Advance(1);
            EditorUtility.SetDirty(hands);EditorUtility.SetDirty(prototype);EditorUtility.SetDirty(focus);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);Selection.activeGameObject=camera.gameObject;
            return "已连接现有书房双手、静置/举机手位和公园镜头；停用重复主相机并保存 ParkScene。未运行 Play。";
        }
        static Transform Pose(Transform parent,string name,Vector3 position,Vector3 angles)
        {
            var existing=parent.Find(name);if(existing!=null)return existing;
            var go=new GameObject(name);go.transform.SetParent(parent,false);go.transform.localPosition=position;go.transform.localEulerAngles=angles;
            Undo.RegisterCreatedObjectUndo(go,"创建手位定位物体");return go.transform;
        }
    }
}
