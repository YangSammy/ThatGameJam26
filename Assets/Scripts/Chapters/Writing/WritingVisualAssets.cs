using UnityEngine;
namespace MemoryWhitebox
{
    // Presentation references only; WritingChapter owns progress and persistence.
    public sealed class WritingVisualAssets : ScriptableObject
    {
        public GameObject writingSet;
        public GameObject[] collectedItems;
        public GameObject[] paperBalls;
        public GameObject timer;
        public static WritingVisualAssets Load() => Resources.Load<WritingVisualAssets>("WritingVisualAssets");
    }
}
