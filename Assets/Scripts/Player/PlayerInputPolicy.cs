using UnityEngine;

namespace MemoryWhitebox
{
    public enum PlayerControlState { Exploration, Transition, Operation, Inspection, Gallery, Paused, Unfocused }

    public static class PlayerInputPolicy
    {
        // A key may mean movement in one context and recording in another.
        // Require release only for keys held at handoff, not later presses.
        internal static readonly KeyCode[] Actions={KeyCode.Mouse0,KeyCode.Mouse1,KeyCode.Mouse2,
            KeyCode.Escape,KeyCode.E,KeyCode.H,KeyCode.R,KeyCode.F,KeyCode.J,KeyCode.L,KeyCode.Quote,KeyCode.Return,KeyCode.Space,
            KeyCode.W,KeyCode.A,KeyCode.S,KeyCode.D,KeyCode.LeftArrow,KeyCode.RightArrow,KeyCode.UpArrow,KeyCode.DownArrow};
        static int ignoreLookThrough=-1;
        public static bool ActionsReleased
        {get{foreach(var key in Actions)if(Input.GetKey(key))return false;return true;}}
        public static Vector2 LookDelta=>Time.frameCount<=ignoreLookThrough?Vector2.zero:
            new Vector2(Input.GetAxisRaw("Mouse X"),Input.GetAxisRaw("Mouse Y"));
        public static void SuppressLook(){ignoreLookThrough=Time.frameCount+1;}
        public static void SetCursor(bool free)
        {
            var target=free?CursorLockMode.None:CursorLockMode.Locked;
            if(Cursor.lockState!=target){Cursor.lockState=target;SuppressLook();}
            Cursor.visible=free;
        }
        public static void ApplyCursor(PlayerControlState state,bool operationMouseLook=false)
        {
            SetCursor(state==PlayerControlState.Inspection || state==PlayerControlState.Gallery ||
                state==PlayerControlState.Paused || state==PlayerControlState.Unfocused ||
                (state==PlayerControlState.Operation && !operationMouseLook));
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset(){ignoreLookThrough=-1;}
    }

    public sealed class PlayerInputReleaseGate
    {
        int pending,armedFrame=-10;
        public void Arm()
        {
            pending=0;armedFrame=Time.frameCount;
            for(int i=0;i<PlayerInputPolicy.Actions.Length;i++)if(Input.GetKey(PlayerInputPolicy.Actions[i]))pending|=1<<i;
            PlayerInputPolicy.SuppressLook();
        }
        public bool Ready
        {
            get
            {
                for(int i=0;i<PlayerInputPolicy.Actions.Length;i++)if(!Input.GetKey(PlayerInputPolicy.Actions[i]))pending&=~(1<<i);
                return pending==0 && Time.frameCount>armedFrame+1;
            }
        }
    }
}
