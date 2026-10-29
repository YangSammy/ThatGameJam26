using UnityEngine;
namespace MemoryWhitebox
{
    [CreateAssetMenu(menuName="Memory/Data/Chapter Definition")]
    public sealed class ChapterDefinition:ScriptableObject
    {
        public string chapterId;
        public int contentVersion=1;
        [Tooltip("关卡专用配置资产；不持有场景物体或玩家进度。")]
        public ScriptableObject[] gameplayConfigurations=new ScriptableObject[0];
    }
}
