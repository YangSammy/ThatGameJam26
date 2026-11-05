using UnityEngine;
namespace MemoryWhitebox
{
    // Only commanded travel is sampled; photo/meter/pause time is never a stall.
    public sealed class ParkWalkStallDetector
    {
        float playerTime,girlTime,playerProgress,girlProgress;
        public bool Tick(bool expectPlayer,bool expectGirl,float playerDistance,float girlDistance,float dt)
        {
            bool a=Sample(ref playerTime,ref playerProgress,expectPlayer,playerDistance,dt);
            bool b=Sample(ref girlTime,ref girlProgress,expectGirl,girlDistance,dt);
            return a||b;
        }
        static bool Sample(ref float time,ref float progress,bool expected,float distance,float dt)
        {
            if(!expected){time=progress=0;return false;}
            progress+=Mathf.Max(0,distance);time+=Mathf.Max(0,dt);
            if(progress>=.05f){time=progress=0;return false;}
            return time>=2;
        }
        public void Reset(){playerTime=girlTime=playerProgress=girlProgress=0;}
    }
}
