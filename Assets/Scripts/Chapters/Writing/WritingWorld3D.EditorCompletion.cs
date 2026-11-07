namespace MemoryWhitebox
{
    public sealed partial class WritingWorld3D
    {
        public void EditorBeginFarewell()
        {
            if(State.Completed || State.Phase=="farewell")return;
            CancelPointerGesture();
            string previous=State.Phase;
            State.EditorBeginFarewell();
            PlayWritingStateEdges(State.Collected,previous,false,State.Remaining);
            UpdateActors(0);UpdateWritingVisuals(0,true);
            // The normal wave/turn/walk farewell Tick calls Finish and commits the story.
        }
    }
}
