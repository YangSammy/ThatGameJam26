using UnityEngine;
namespace MemoryWhitebox
{
    [CreateAssetMenu(menuName="Memory/Data/Item Definition")]
    public sealed class ItemDefinition:ScriptableObject
    {
        [Tooltip("稳定类型 ID；发布后不要随显示名称修改。")]
        public string definitionId;
        public string tooltip;
        public bool inspectable,draggable;
        [Tooltip("类型专用配置，例如磁带委托；不保存本局状态。")]
        public ScriptableObject gameplayConfiguration;
    }
}
