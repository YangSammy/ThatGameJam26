using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace MemoryWhitebox.Editor
{
    public static class TapeCommissionAuthoring
    {
        public static string CreateDefaults()
        {
            if(EditorApplication.isPlaying)throw new System.InvalidOperationException("Exit Play first.");
            const string folder="Assets/Data/TapeCommissions";
            if(!AssetDatabase.IsValidFolder("Assets/Data"))AssetDatabase.CreateFolder("Assets","Data");
            if(!AssetDatabase.IsValidFolder(folder))AssetDatabase.CreateFolder("Assets/Data","TapeCommissions");
            var deck=Object.FindObjectOfType<HouseRadioDeck>();Undo.RecordObject(deck,"Assign tape commissions");
            for(int i=0;i<3;i++)
            {
                string path=folder+"/Commission_0"+(i+1)+".asset";
                var definition=AssetDatabase.LoadAssetAtPath<TapeCommissionDefinition>(path);
                if(definition==null)
                {
                    definition=ScriptableObject.CreateInstance<TapeCommissionDefinition>();
                    definition.title=i==2?"Birthday - placeholder audio":"Music "+(i+1);
                    definition.sourceAudio=deck.tapes[i==0?2:i-1].audio;
                    definition.cleanDuration=Mathf.Min(12,definition.sourceAudio.length);
                    float duration=definition.cleanDuration;
                    definition.noise=i==1?new[]{
                        new TapeCommissionDefinition.NoiseInsertion{atCleanSecond=duration*.3f,duration=1.2f,amplitude=.22f},
                        new TapeCommissionDefinition.NoiseInsertion{atCleanSecond=duration*.7f,duration=2.4f,amplitude=.22f}
                    }:new[]{new TapeCommissionDefinition.NoiseInsertion{atCleanSecond=duration*.5f,duration=i==0?2:1.6f,amplitude=.22f}};
                    var rules=ScriptableObject.CreateInstance<TapeAcceptanceRules>();
                    rules.requiredSounds=new[]{new TapeAcceptanceRules.RequiredSound{label="Opening",start=0,end=duration*.2f,minimumCoverage=.9f},new TapeAcceptanceRules.RequiredSound{label="Ending",start=duration*.8f,end=duration,minimumCoverage=.9f}};
                    AssetDatabase.CreateAsset(rules,folder+"/Acceptance_0"+(i+1)+".asset");definition.acceptance=rules;
                    AssetDatabase.CreateAsset(definition,path);
                }
                deck.commissions[i]=definition;
            }
            EditorUtility.SetDirty(deck);AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(deck.gameObject.scene);EditorSceneManager.SaveScene(deck.gameObject.scene);
            return folder;
        }
    }
}
