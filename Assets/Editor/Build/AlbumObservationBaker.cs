using UnityEditor;
using UnityEngine;

namespace MemoryWhitebox.Editor
{
    // Keep the old callable entry point for editor integrations. The shipping
    // observation sets now use the real scene geometry, not six captured quads.
    public static class AlbumObservationBaker
    {
        [MenuItem("Tools/Album/Update real-time observation geometry")]
        public static void Bake()
        {
            Debug.Log(AutumnWinterPresentationAuthoring.AuthorWinter());
        }
    }
}
