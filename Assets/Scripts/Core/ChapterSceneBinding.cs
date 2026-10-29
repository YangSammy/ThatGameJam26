using UnityEngine;
namespace MemoryWhitebox
{
    [DisallowMultipleComponent]
    public sealed class ChapterSceneBinding:MonoBehaviour
    {
        public ChapterDefinition definition;
        public Transform root;
        public SceneViewAnchor operationView;
        public ItemSceneBinding[] items=new ItemSceneBinding[0];
        public string ChapterId=>definition!=null?definition.chapterId:"";
    }
}
