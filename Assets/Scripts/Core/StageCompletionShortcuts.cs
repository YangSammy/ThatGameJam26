using UnityEngine;
namespace MemoryWhitebox
{
    // One-shot intent only; chapter/artifact progress still uses the normal saves.
    public static class StageCompletionShortcuts
    {
        public static int PendingHouseStage;
        public static int Pressed()
        {
            if(Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))return 0;
            for(int key=1;key<=5;key++)
                if(Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha0+key)) ||
                   Input.GetKeyDown((KeyCode)((int)KeyCode.Keypad0+key)))return key;
            return 0;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset(){PendingHouseStage=0;}
    }
}
