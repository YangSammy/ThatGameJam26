using System.Collections.Generic;
using UnityEngine;
namespace MemoryWhitebox
{
    // Data only: never creates a world-space renderer.
    public sealed class WorldTooltipLabel:MonoBehaviour
    {
        [TextArea] public string text;
        public Transform target;
        public string prefix;
        public WorldTooltipLabel mirror;
        public static readonly List<WorldTooltipLabel> Active=new List<WorldTooltipLabel>();
        void OnEnable(){if(!Active.Contains(this))Active.Add(this);}
        void OnDisable(){Active.Remove(this);}
        void Start(){ResolveTarget();}
        public void ResolveTarget()
        {
            if(target!=null)return;
            var parent=transform.parent;if(parent==null)return;
            if(parent.GetComponent<Collider>()!=null){target=parent;return;}
            float nearest=float.MaxValue;
            foreach(var col in parent.GetComponentsInChildren<Collider>(true))
            {float distance=(col.bounds.center-transform.position).sqrMagnitude;if(distance<nearest){nearest=distance;target=col.transform;}}
        }
        public bool Matches(Collider hit)=>hit!=null && target!=null && (hit.transform==target || hit.transform.IsChildOf(target));
        public string Content=>mirror!=null?mirror.Content:string.IsNullOrEmpty(prefix)?text:prefix+text;
    }
}
