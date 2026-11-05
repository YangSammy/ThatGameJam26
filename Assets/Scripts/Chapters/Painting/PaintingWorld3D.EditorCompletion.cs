#if UNITY_EDITOR
using UnityEngine;
namespace MemoryWhitebox
{
    public sealed partial class PaintingWorld3D
    {
        bool editorWardWalk;
        readonly WBInput editorWardInput=new WBInput();
        public void EditorCompleteWard()
        {
            if(State.Completed)return;
            if(State.Phase<2)throw new System.InvalidOperationException("Enter the ward before completing it.");
            CancelPointerGesture();
            editorWardInput.Keys.Clear();editorWardInput.Keys.Add(KeyCode.D);
            editorWardWalk=true;
            // Advance through the ordinary reveal/cutoff path. Esc cancels this walk;
            // another press resumes from the existing stable ward progress.
        }
    }
}
#endif
