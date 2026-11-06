using UnityEngine;
namespace MemoryWhitebox
{
    // Presentation references only; WritingChapter owns progress and persistence.
    public sealed class WritingVisualAssets : ScriptableObject
    {
        public GameObject writingSet;
        public GameObject memoryBedroom;
        public GameObject[] collectedItems;
        public GameObject[] paperBalls;
        public GameObject timer;
        public AutumnFarewellVisual farewell;
        public GameObject memoryActor, memoryVase, memoryTable, memoryChair, memoryCabinet, memorySofa;
        public Shader memoryFadeShader;
        public static WritingVisualAssets Load() => Resources.Load<WritingVisualAssets>("WritingVisualAssets");
    }
}
