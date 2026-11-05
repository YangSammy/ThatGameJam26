using System;
using System.Reflection;
using UnityEngine;
namespace MemoryWhitebox.Editor
{
    public static class ParkWalkSafetyChecks
    {
        public static string Run()
        {
            var detector=new ParkWalkStallDetector();bool stalled=false;
            for(int i=0;i<21;i++)stalled|=detector.Tick(false,false,0,0,.1f);
            if(stalled)throw new Exception("Intentional pause was treated as blocked");
            for(int i=0;i<21;i++)stalled|=detector.Tick(true,false,.02f,0,.1f);
            if(stalled)throw new Exception("Moving player was treated as blocked");
            detector.Reset();for(int i=0;i<21;i++)stalled|=detector.Tick(true,false,0,0,.1f);
            if(!stalled)throw new Exception("Blocked player not detected");
            detector.Reset();stalled=false;for(int i=0;i<21;i++)stalled|=detector.Tick(false,true,0,0,.1f);
            if(!stalled)throw new Exception("Blocked companion not detected");
            var type=typeof(ParkPhotoPrototype);var soft=type.GetMethod("SoftenPhotoBoundary",BindingFlags.Static|BindingFlags.NonPublic);
            var velocity=(Vector3)soft.Invoke(null,new object[]{new Vector3(1.05f,0,0),new Vector3(1,0,1),Vector3.zero,Quaternion.identity,false});
            if(velocity.x>=.5f || Mathf.Abs(velocity.z-1)>.001f)throw new Exception("Soft edge must reduce outward movement and retain tangent");
            return "PASS stall detector: intentional stops / slow progress / blocked player / blocked companion; boundary retains tangent";
        }
    }
}
