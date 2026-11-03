using UnityEngine;
namespace MemoryWhitebox
{
    // Asset references only. Chapter state and persistence remain owned by PaintingChapter.
    public sealed class PaintingVisualAssets : ScriptableObject
    {
        public GameObject sheet, swing, bridge, counterweight, flower, door, suitcase, pendant;
        public static PaintingVisualAssets Load() => Resources.Load<PaintingVisualAssets>("PaintingVisualAssets");
    }
}
