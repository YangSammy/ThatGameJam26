using System;
using System.Collections.Generic;
using UnityEngine;

namespace MemoryWhitebox
{
    [Serializable] public sealed class AreaPlacementRecord
    {
        public string areaId,itemId;
        public Vector3 position;
        public Quaternion rotation;
    }
    [DisallowMultipleComponent]
    public sealed class OperationArea : MonoBehaviour
    {
        [Serializable] public sealed class Member
        {
            public string itemId;
            public Transform item;
            [NonSerialized] public AreaPlacementRecord placement;
        }
        public string areaId;
        [Tooltip("Rectangle in this object's local X/Z coordinates. Height is local Y.")]
        public Vector2 center, size=Vector2.one;
        public float surfaceHeight=.5f;
        public Member[] members=new Member[0];
        static readonly List<OperationArea> areas=new List<OperationArea>();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetRegistry(){areas.Clear();}
        void OnEnable(){if(!areas.Contains(this))areas.Add(this);}
        void OnDisable(){areas.Remove(this);}
        public static OperationArea For(Transform item)
        {foreach(var area in areas)if(area!=null && area.Find(item)!=null)return area;return null;}
        Member Find(Transform item)=>Array.Find(members,m=>m.item==item);
        public static void Register(string id,Transform item,string itemId)
        {
            foreach(var area in areas)
            {
                if(area.areaId!=id || area.Find(item)!=null)continue;
                var list=new List<Member>(area.members);list.Add(new Member{item=item,itemId=itemId});area.members=list.ToArray();return;
            }
        }
        public float CarryLift(Transform item,float lift)=>Mathf.Max(0,(Footprint(item).min.y-surfaceHeight)*transform.lossyScale.y)+lift;
        public Vector3 Normal=>transform.up;
        public Vector3 Surface=>transform.TransformPoint(new Vector3(center.x,surfaceHeight,center.y));
        Bounds Footprint(Transform item)
        {
            Bounds result=new Bounds(transform.InverseTransformPoint(item.position),Vector3.zero);bool found=false;
            foreach(var renderer in item.GetComponentsInChildren<Renderer>())
            {
                if(!renderer.enabled || !renderer.gameObject.activeInHierarchy || renderer is LineRenderer)continue;
                var b=renderer.localBounds;
                for(int i=0;i<8;i++)
                {
                    Vector3 corner=b.center+Vector3.Scale(b.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1));
                    var point=transform.InverseTransformPoint(renderer.transform.TransformPoint(corner));
                    if(!found){result=new Bounds(point,Vector3.zero);found=true;}else result.Encapsulate(point);
                }
            }
            return result;
        }
        public bool Constrain(Transform item,Vector3 desired,float lift,out Vector3 constrained,out bool edge)
        {
            var bounds=Footprint(item);var origin=transform.InverseTransformPoint(item.position);
            var local=transform.InverseTransformPoint(desired);
            Vector3 low=bounds.min-origin,high=bounds.max-origin;
            float minX=center.x-size.x*.5f-low.x,maxX=center.x+size.x*.5f-high.x;
            float minZ=center.y-size.y*.5f-low.z,maxZ=center.y+size.y*.5f-high.z;
            constrained=item.position;edge=false;if(minX>maxX || minZ>maxZ)return false;
            var before=local;local.x=Mathf.Clamp(local.x,minX,maxX);local.z=Mathf.Clamp(local.z,minZ,maxZ);
            local.y=surfaceHeight-low.y+lift/Mathf.Max(.0001f,transform.lossyScale.y);
            edge=Mathf.Abs(local.x-before.x)>.001f || Mathf.Abs(local.z-before.z)>.001f;
            constrained=transform.TransformPoint(local);return true;
        }
        public bool Place(Transform item,Collider receiver=null)
        {
            Vector3 at;bool edge;if(!Constrain(item,item.position,0,out at,out edge))return false;
            Vector3 old=item.position;item.position=at;
            Bounds b=new Bounds(at,Vector3.zero);bool found=false;
            foreach(var r in item.GetComponentsInChildren<Renderer>())
            {if(!r.enabled || r is LineRenderer)continue;if(!found){b=r.bounds;found=true;}else b.Encapsulate(r.bounds);}
            if(receiver!=null && found)
            {
                float lift=Mathf.Max(0,receiver.bounds.max.y+.002f-b.min.y);
                at+=Vector3.up*lift;item.position=at;b.center+=Vector3.up*lift;
            }
            foreach(var col in Physics.OverlapBox(b.center,Vector3.Max(b.extents-Vector3.one*.004f,Vector3.one*.001f),Quaternion.identity,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore))
            {
                if(col==receiver)continue;
                if(DragReceiverProbe.BelongsTo(col.transform,item) || DragReceiverProbe.IsNavigationOnly(col))continue;
                if(col.bounds.max.y<=b.min.y+.008f)continue;
                item.position=old;return false;
            }
            var member=Find(item);if(member==null){item.position=old;return false;}
            member.placement=new AreaPlacementRecord{areaId=areaId,itemId=member.itemId,position=transform.InverseTransformPoint(at),rotation=Quaternion.Inverse(transform.rotation)*item.rotation};
            return true;
        }
        public static bool TryGetPose(Transform item,out Vector3 position,out Quaternion rotation)
        {
            position=item.position;rotation=item.rotation;
            var area=For(item);var saved=area!=null?area.Find(item).placement:null;
            if(saved==null)return false;
            position=area.transform.TransformPoint(saved.position);rotation=area.transform.rotation*saved.rotation;return true;
        }
        public static bool RestorePose(Transform item)
        {
            var area=For(item);var saved=area!=null?area.Find(item).placement:null;
            if(saved==null)return false;
            item.SetPositionAndRotation(area.transform.TransformPoint(saved.position),area.transform.rotation*saved.rotation);return true;
        }
        public static void ClearPlacement(Transform item){var area=For(item);if(area!=null)area.Find(item).placement=null;}
        public static AreaPlacementRecord[] Capture()
        {
            var result=new List<AreaPlacementRecord>();
            foreach(var area in areas)foreach(var member in area.members)if(member.placement!=null)result.Add(member.placement);
            return result.ToArray();
        }
        public static void Restore(AreaPlacementRecord[] records)
        {
            foreach(var area in areas)foreach(var member in area.members)
            {
                member.placement=null;
                var saved=records!=null?Array.Find(records,r=>r!=null && r.areaId==area.areaId && r.itemId==member.itemId):null;
                if(saved==null || member.item==null)continue;
                if(!Finite(saved.position) || !Finite(new Vector3(saved.rotation.x,saved.rotation.y,saved.rotation.z)) || float.IsNaN(saved.rotation.w) || float.IsInfinity(saved.rotation.w) || Quaternion.Dot(saved.rotation,saved.rotation)<.1f)continue;
                var item=member.item;Vector3 old=item.position;Quaternion rotation=item.rotation;
                item.SetPositionAndRotation(area.transform.TransformPoint(saved.position),area.transform.rotation*saved.rotation);
                Vector3 bounded;bool edge;
                if(area.Constrain(item,item.position,0,out bounded,out edge))
                {
                    // A designated receiver can sit above the table (e.g. a preparation mat).
                    // Preserve its saved support clearance while still clamping XY bounds and excessive heights.
                    float clearance=Mathf.Clamp(Vector3.Dot(item.position-bounded,area.Normal),0,.25f);
                    bounded+=area.Normal*clearance;
                    member.placement=new AreaPlacementRecord{areaId=saved.areaId,itemId=saved.itemId,position=area.transform.InverseTransformPoint(bounded),rotation=saved.rotation};
                }
                item.SetPositionAndRotation(old,rotation);
                RestorePose(item);
            }
        }
        static bool Finite(Vector3 v)=>!float.IsNaN(v.x)&&!float.IsNaN(v.y)&&!float.IsNaN(v.z)&&!float.IsInfinity(v.x)&&!float.IsInfinity(v.y)&&!float.IsInfinity(v.z);
        void OnDrawGizmosSelected()
        {
            Gizmos.color=new Color(.4f,.85f,.8f,1);var old=Gizmos.matrix;Gizmos.matrix=transform.localToWorldMatrix;
            Gizmos.DrawWireCube(new Vector3(center.x,surfaceHeight,center.y),new Vector3(size.x,.01f,size.y));Gizmos.matrix=old;
        }
    }
}
