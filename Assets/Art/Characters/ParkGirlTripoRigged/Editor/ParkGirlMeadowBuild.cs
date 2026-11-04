using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

namespace MemoryWhitebox.Art.Editor
{
    public static class ParkGirlMeadowBuild
    {
        const string Art="Assets/Art/Characters/ParkGirlTripoRigged";
        public static void Build()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit Play Mode first.");
            var path=Art+"/Animations/AC_ParkGirl_Performance.controller";
            if(AssetDatabase.LoadAssetAtPath<AnimatorController>(path)==null)
                AssetDatabase.CopyAsset(Art+"/Animations/AC_ParkGirl_Locomotion.controller",path);
            var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if(!controller.parameters.Any(p=>p.name=="OfferHand"))controller.AddParameter("OfferHand",AnimatorControllerParameterType.Bool);
            var sm=controller.layers[0].stateMachine;
            var locomotion=sm.states.Single(s=>s.state.name=="Locomotion").state;
            var reach=sm.states.Select(s=>s.state).FirstOrDefault(s=>s.name=="OfferHandReach")??sm.AddState("OfferHandReach",new Vector3(470,0));
            var hold=sm.states.Select(s=>s.state).FirstOrDefault(s=>s.name=="OfferHandHold")??sm.AddState("OfferHandHold",new Vector3(740,0));
            reach.motion=AssetDatabase.LoadAssetAtPath<AnimationClip>(Art+"/Animations/OfferHandRefinedV1/AN_OfferHand_Right_Refined.anim");
            hold.motion=AssetDatabase.LoadAssetAtPath<AnimationClip>(Art+"/Animations/OfferHandRefinedV1/AN_OfferHand_Hold_Right.anim");
            foreach(var state in new[]{locomotion,reach,hold})foreach(var t in state.transitions)state.RemoveTransition(t);
            var enter=locomotion.AddTransition(reach);enter.hasExitTime=false;enter.hasFixedDuration=true;enter.duration=.35f;
            enter.AddCondition(AnimatorConditionMode.If,0,"OfferHand");enter.AddCondition(AnimatorConditionMode.Less,.08f,"Speed");
            var settled=reach.AddTransition(hold);settled.hasExitTime=true;settled.exitTime=1;settled.hasFixedDuration=true;settled.duration=.18f;settled.AddCondition(AnimatorConditionMode.If,0,"OfferHand");
            foreach(var state in new[]{reach,hold})
            {
                var exit=state.AddTransition(locomotion);exit.hasExitTime=false;exit.hasFixedDuration=true;exit.duration=.35f;exit.AddCondition(AnimatorConditionMode.IfNot,0,"OfferHand");
            }
            EditorUtility.SetDirty(controller);AssetDatabase.SaveAssetIfDirty(controller);
            var prefab=Art+"/Prefabs/PF_ParkGirlTripo_Locomotion.prefab";
            var root=PrefabUtility.LoadPrefabContents(prefab);
            try
            {
                var animator=root.GetComponentInChildren<Animator>();animator.runtimeAnimatorController=controller;animator.applyRootMotion=false;
                var presentation=root.GetComponent<ParkGirlPresentation>();
                var palm=presentation.rightHand.Find("PalmContact");
                if(palm==null){palm=new GameObject("PalmContact").transform;palm.SetParent(presentation.rightHand,false);}
                var fingers=presentation.rightHand.GetComponentsInChildren<Transform>().Where(t=>t.name.EndsWith("Proximal")&&!t.name.Contains("Thumb")).ToArray();
                palm.localPosition=fingers.Aggregate(Vector3.zero,(sum,t)=>sum+presentation.rightHand.InverseTransformPoint(t.position))*(.65f/fingers.Length);
                palm.localRotation=Quaternion.identity;palm.localScale=Vector3.one;
                presentation.palmContact=palm;presentation.offerContactSeconds=2.1f;
                PrefabUtility.SaveAsPrefabAsset(root,prefab);
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
            AssetDatabase.SaveAssets();
        }
    }
}
