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
        [Header("Motion relative to authored poses")]
        [Tooltip("The frame lid Transform is the closed pose. Opening adds this local rotation.")]
        public Vector3 frameOpenEuler=new Vector3(-100,0,0);
        [Tooltip("Offset from the tank lid's authored local position, in parent local units.")]
        public Vector3 tankLidOpenOffset=new Vector3(0,.16f,0);
        [Tooltip("Scale multiplier applied only while working inside the dark bag.")]
        public Vector3 bagWorkingScale=new Vector3(1,1.4375f,1);
        public Vector3 bagGripAxis=Vector3.right;
        public float bagGripTurns=2;
        public Vector3 tankAgitationEuler=new Vector3(9,0,8);
        public Vector3 tankDrainEuler=new Vector3(0,0,100);
        public Vector3 bottlePourEuler=new Vector3(60,0,-35);
        [Tooltip("Local offset above the contact frame's paper placement; follows its rotation and scale.")]
        public Vector3 negativeOnFrameOffset=new Vector3(0,.008f,0);
        public Prop Get(string id)=>Array.Find(props,p=>p.id==id);
        public Transform Placement(string id){var p=Get(id);return p!=null?(p.placement!=null?p.placement:p.target):null;}
    }
}
