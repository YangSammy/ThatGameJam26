using System;
using UnityEngine;

namespace MemoryWhitebox
{
    // Transient presentation only. Chapter controllers own placement, progress and saving.
    public sealed class ObjectDragSession
    {
        Transform item;
        OperationArea area;
        bool receiverPreview;
        public bool AtEdge {get;private set;}
        public bool HasArea=>area!=null;
        Plane plane;
        Vector3 grabOffset, start, scale, carryOrigin, carryGrip;
        public Vector3 ScreenCarryGrip=>Active?item.TransformPoint(carryGrip):Vector3.zero;
        Quaternion rotation;
        Renderer[] renderers;
        readonly RaycastHit[] obstacles = new RaycastHit[64];
        float lift, maxTravel;
        Transform extractionRoot;
        public bool Active => item != null;
        public bool Blocked { get; private set; }
        public Transform Item => item;

        // Cassette carry is a presentation gesture. Keep the same camera-facing plane
        // throughout; validate the final table placement instead of sweeping tiny props.
        public void BeginScreenCarry(Transform target, Ray pointer, Vector3 normal)
        {
            Begin(target,pointer,normal,0);
            carryOrigin=start;plane=new Plane(normal.normalized,start);
            float distance;
            grabOffset=plane.Raycast(pointer,out distance)?start-pointer.GetPoint(distance):Vector3.zero;
            carryGrip=item.InverseTransformPoint(start-grabOffset);
        }
        // Rebase presentation after camera motion without changing the original cancel pose.
        public void RebaseScreenCarry(Ray pointer,Vector3 normal)
        {
            if(!Active)return;
            carryOrigin=item.position;plane=new Plane(normal.normalized,carryOrigin);
            float distance;grabOffset=plane.Raycast(pointer,out distance)?carryOrigin-pointer.GetPoint(distance):Vector3.zero;
        }
        public void MoveScreenCarry(Ray pointer)
        {
            if(!Active)return;
            float distance;
            if(!plane.Raycast(pointer,out distance))return;
            var desired=pointer.GetPoint(distance)+grabOffset;
            var delta=Vector3.ClampMagnitude(desired-carryOrigin,maxTravel);
            // Do not drag below the supporting surface. Clamping the whole vector
            // retains the original depth and the grabbed point on the cassette.
            if(delta.y<0)
            {
                var upward=Vector3.ProjectOnPlane(Vector3.up,plane.normal);
                if(upward.y>.001f)delta-=upward*(delta.y/upward.y);
            }
            item.position=carryOrigin+delta;Blocked=false;AtEdge=false;receiverPreview=false;
        }

        public void Begin(Transform target, Ray pointer, Vector3 planeNormal, float liftHeight = 0, float travelLimit = 1.8f, bool useArea = true, Transform fromContainer = null)
        {
            End(true);
            item = target;
            // Importers include every animation pose in skinned bounds. A short film
            // leader must not collide as if the whole possible unrolled sheet exists.
            Mesh currentPose=null;
            foreach(var skin in item.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                if(currentPose==null)currentPose=new Mesh();
                skin.BakeMesh(currentPose);currentPose.RecalculateBounds();skin.localBounds=currentPose.bounds;
            }
            if(currentPose!=null)UnityEngine.Object.Destroy(currentPose);
            extractionRoot=fromContainer;
            start = item.position; rotation = item.rotation; scale = item.localScale;
            area=useArea?OperationArea.For(item):null;
            plane = new Plane(area!=null?area.Normal:planeNormal.normalized, start);
            receiverPreview=false;AtEdge=false;
            float distance;
            grabOffset = plane.Raycast(pointer, out distance) ? start - pointer.GetPoint(distance) : Vector3.zero;
            lift = area!=null?area.CarryLift(item,liftHeight):liftHeight;
            maxTravel = Mathf.Max(.01f, travelLimit);
            renderers = item.GetComponentsInChildren<Renderer>();
            Blocked = false;
        }

        public Vector3 Move(Ray pointer, Transform receiver = null)
        {
            if (!Active) return Vector3.zero;
            float distance;
            if (Mathf.Abs(Vector3.Dot(plane.normal, pointer.direction)) < .08f || !plane.Raycast(pointer, out distance))
            { Blocked = true; return item.position; }
            Vector3 target = pointer.GetPoint(distance) + grabOffset;
            AtEdge=false;
            if(area!=null && receiver==null)
            {
                bool edge;Vector3 bounded;
                if(!area.Constrain(item,target,lift,out bounded,out edge)){Blocked=true;return item.position;}
                target=bounded;AtEdge=edge;
            }
            else if(receiver!=null)target=receiver.position+(area!=null?area.Normal:Vector3.up)*lift;
            else target = start + Vector3.ClampMagnitude(target - start, maxTravel) + Vector3.up * lift;
            return MoveTo(target, receiver);
        }

        public Vector3 MoveTo(Vector3 target, Transform receiver = null,Collider receivingShell=null)
        {
            if (!Active) return target;
            receiverPreview=receiver!=null;
            Vector3 delta = target - item.position;
            float length = delta.magnitude;
            Blocked = false;
            if (length < .0001f) return item.position;
            Bounds bounds = new Bounds(item.position, Vector3.one * .01f);
            bool found = false;
            foreach (var renderer in renderers)
            {
                if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy || renderer is LineRenderer) continue;
                if (!found) { bounds = renderer.bounds; found = true; }
                else bounds.Encapsulate(renderer.bounds);
            }
            Vector3 extents = Vector3.Max(bounds.extents - Vector3.one * .003f, Vector3.one * .002f);
            // Only the explicitly opened source container permits initial overlap.
            // Once outside it, normal collision applies again, including on return.
            if(extractionRoot!=null)
            {
                bool foundContainer=false;Bounds containerBounds=new Bounds(extractionRoot.position,Vector3.zero);
                foreach(var col in extractionRoot.GetComponentsInChildren<Collider>())
                    if(col.enabled){if(!foundContainer){containerBounds=col.bounds;foundContainer=true;}else containerBounds.Encapsulate(col.bounds);}
                containerBounds.Encapsulate(start);
                containerBounds.Expand(.1f);
                if(!foundContainer || !containerBounds.Intersects(bounds))extractionRoot=null;
            }
            int count = Physics.BoxCastNonAlloc(bounds.center, extents, delta / length, obstacles,
                Quaternion.identity, length + .005f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            float travel = length;
            // A full buffer is ambiguous: stop rather than silently crossing an unreported wall.
            if (count == obstacles.Length) travel = 0;
            for (int i = 0; i < count; i++)
            {
                var hit = obstacles[i]; var col = hit.collider;
                if (col.GetComponent<PhotographyInteractionHit>()!=null || col==receivingShell || DragReceiverProbe.BelongsTo(col.transform, item) ||
                    DragReceiverProbe.BelongsTo(col.transform, receiver) || DragReceiverProbe.BelongsTo(col.transform,extractionRoot) || DragReceiverProbe.IsNavigationOnly(col)) continue;
                // Allow lifting off the original supporting surface, not through a vertical obstacle.
                if (hit.distance <= .001f && delta.y >= 0 && col.bounds.max.y <= bounds.min.y + .012f) continue;
                // Authored props may already overlap at rest. Permit moving outwards,
                // never farther into the obstacle; subsequent sweeps restore normal blocking.
                if(hit.distance<=.001f && col.bounds.Intersects(bounds) &&
                    Vector3.Dot(delta,bounds.center-col.bounds.center)>.00001f)continue;
                travel = Mathf.Min(travel, Mathf.Max(0, hit.distance - .005f));
            }
            Blocked = travel < length - .001f;
            item.position += delta / length * travel;
            return item.position;
        }

        public bool TryPlaceInArea()
        {
            if(!Active || area==null || Blocked || receiverPreview)return false;
            // Temporary pour/drain tilt must not become a permanent table pose.
            item.rotation=rotation;
            if(!area.Place(item))return false;
            End(false);return true;
        }

        // A validated receiver may need a short lift above neighbouring tabletop tools.
        // All three segments still sweep collisions; a wall or overhead obstacle stops it.
        public Vector3 MoveOverSurface(Vector3 target,Transform receiver,bool liftFirst=false,Collider receivingShell=null)
        {
            if(!liftFirst){MoveTo(target,receiver);if(!Blocked || receiver==null)return item.position;}
            float height=Mathf.Max(start.y,target.y)+.45f;
            MoveTo(new Vector3(item.position.x,height,item.position.z),receiver);
            if(Blocked)return item.position;
            MoveTo(new Vector3(target.x,height,target.z),receiver);
            if(Blocked)return item.position;
            return MoveTo(target,receiver,receivingShell);
        }

        public void End(bool restore)
        {
            if (item != null && restore) { item.SetPositionAndRotation(start, rotation); item.localScale = scale; }
            item = null; area=null; extractionRoot=null;renderers = null; Blocked = false;AtEdge=false;receiverPreview=false;
        }
    }

    public static class DragReceiverProbe
    {
        public static bool BelongsTo(Transform candidate, Transform root) => root != null && (candidate == root || candidate.IsChildOf(root));
        public static bool IsNavigationOnly(Collider col) => col.name.StartsWith("Walk blocker", StringComparison.Ordinal) && col.GetComponent<Renderer>() == null;

        // One tolerant visible receiver rule shared by hover, preview and release.
        public static bool Accepts(Ray ray, Transform receiver, Transform dragged, float tolerance,
            float reach = 2.5f, Predicate<Collider> ignore = null, Collider onlyCollider = null)
        {
            if (receiver == null || !receiver.gameObject.activeInHierarchy) return false;
            Vector3 closest = receiver.position;
            float best = float.MaxValue;
            bool directHit = false;
            var colliders = onlyCollider != null ? new[] { onlyCollider } : receiver.GetComponentsInChildren<Collider>();
            foreach (var col in colliders)
            {
                if (!col.enabled || !col.gameObject.activeInHierarchy || (col.isTrigger && onlyCollider == null)) continue;
                // Preserve the actual cursor ray when it hits a receiver. Clamping the
                // expanded bounds' entry point to its edge can invent an occlusion
                // through a neighbouring prop even though the receiver is visible.
                RaycastHit surface;
                if (col.Raycast(ray, out surface, reach))
                {
                    if (!directHit || surface.distance < best)
                    {
                        directHit = true; best = surface.distance; closest = surface.point;
                    }
                    continue;
                }
                if (directHit) continue;
                Bounds region = col.bounds; region.Expand(tolerance * 2);
                float distance;
                if (!region.IntersectRay(ray, out distance) || distance < 0 || distance > reach || distance >= best) continue;
                best = distance; closest = col.ClosestPoint(ray.GetPoint(distance));
            }
            if (best == float.MaxValue) return false;
            Vector3 direction = closest - ray.origin;
            if (direction.magnitude > reach) return false;
            foreach (var hit in Physics.RaycastAll(ray.origin, direction.normalized, direction.magnitude - .005f,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                if (BelongsTo(hit.transform, receiver) || BelongsTo(hit.transform, dragged) ||
                    IsNavigationOnly(hit.collider) || (ignore != null && ignore(hit.collider))) continue;
                return false;
            }
            return true;
        }
    }
}
