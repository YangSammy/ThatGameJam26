using UnityEngine;
namespace MemoryWhitebox
{
    [CreateAssetMenu(menuName="Memory/Data/Narrative Definition")]
    public sealed class NarrativeDefinition:ScriptableObject
    {
        [Min(5)] public float writingSeconds=60;
        [Min(.1f)] public float farewellHold=1.2f;
        [Min(.1f)] public float shapeFocusSeconds=1.2f;
        [Min(.1f)] public float albumSpeed=2.5f;
        [Min(.1f)] public float jumpSpeed=5;
        [Min(1)] public float gravity=12;
        [Range(20,80)] public float swingMaxDegrees=65;
        [Min(.01f)] public float flowerRadiansPerSecond=.65f;
        [Min(.1f)] public float whiteTransitionSeconds=1.2f;
        [Min(1)] public float wardWalkDistance=8;
        [Min(.1f)] public float wardWalkSpeed=.7f;
        [Min(.1f)] public float springSeconds=5;
        public static NarrativeDefinition Resolve(Chapter chapter)
        {
            if(chapter.Definition!=null)foreach(var config in chapter.Definition.gameplayConfigurations)if(config is NarrativeDefinition value)return value;
            return null;
        }
    }
}
