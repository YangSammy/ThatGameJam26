using UnityEngine;

namespace MemoryWhitebox.V3
{
    /// <summary>The fifth chapter takes place in the hub itself: open, then physically cross the door.</summary>
    public sealed class EndingChapter : Chapter
    {
        public bool DoorOpened { get; private set; }
        public override string Help => "融雪后的春天：打开背后发光的房门，亲自走过门槛。";
        public void Open(){DoorOpened=true;WBMain.Tone(330,.3f);}
        public void CrossThreshold(){if(!DoorOpened)return;Finish();}
        public override void Draw(){}
    }
}
