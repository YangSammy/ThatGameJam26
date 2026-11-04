using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MemoryWhitebox
{
    internal static class BedroomWalkBounds
    {
        internal static readonly Rect Interior=new Rect(-1.94f,-2.49f,3.88f,4.98f);
        internal static List<Bounds> Collect(Scene scene)
        {
            var obstacles=new List<Bounds>();
            foreach(var root in scene.GetRootGameObjects())
            foreach(var box in root.GetComponentsInChildren<BoxCollider>(false))
            {
                if(!box.name.EndsWith("_Walking_collision",StringComparison.Ordinal))continue;
                var bounds=box.bounds;
                if(bounds.min.y<1.4f && bounds.max.y>.14f)
                {
                    bounds.Expand(new Vector3(.36f,0,.36f));
                    obstacles.Add(bounds);
                }
                box.enabled=false;
            }
            return obstacles;
        }
    }
}
