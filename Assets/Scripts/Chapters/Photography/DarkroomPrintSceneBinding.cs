using System;
using UnityEngine;
namespace MemoryWhitebox
{
    public sealed class DarkroomPrintSceneBinding:MonoBehaviour
    {
        [Serializable] public sealed class Prop
        {
            public string id;
            public Transform target;
            public Transform placement;
        }
        public DarkroomPrintDefinition definition;
        public Prop[] props=new Prop[0];
        public Renderer paperSurface,negativeSurface;
        public Transform frameLid;
        public GameObject exposureLight,waterStream;
        public Prop Get(string id)=>Array.Find(props,p=>p.id==id);
        public Transform Placement(string id){var p=Get(id);return p!=null?(p.placement!=null?p.placement:p.target):null;}
    }
}
