using System;
using System.Collections.Generic;
using UnityEngine;

// Validates the same standing capsule used by the delivered first-person controller.
// Every graph edge is a real capsule sweep, so the route cannot cut through a box or corner.
public static class BedroomAccessibility
{
    [Serializable] public sealed class Station
    {
        public string furniture;
        public bool reachable;
        public int freeCandidates,reachableCandidates;
        public Vector3 expectedStanding,standing,failurePosition;
        public float routeMetres;
        public string[] blockers;
    }
    [Serializable] public sealed class Report
    {
        public bool passed,entryClear;
        public float gridStep=.16f,capsuleRadius,capsuleHeight,floorHeight;
        public int totalNodes,walkableNodes,reachableNodes;
        public Vector3 entry;
        public Station[] stations;
    }
    static readonly string[] Prefixes={"Bed_","Desk_","Chair_","Wardrobe_","Coatrack_","Easel_"};
    static readonly string[] Labels={"Bed","Desk","Chair","Wardrobe","Coatrack","Easel"};
    static readonly List<Vector3> reachableStanding=new List<Vector3>();
    public static bool TryNearestReachable(Vector3 preferred,out Vector3 standing)
    {
        standing=preferred;float best=.85f*.85f;bool found=false;
        foreach(var point in reachableStanding)
        {
            float dx=point.x-preferred.x,dz=point.z-preferred.z,score=dx*dx+dz*dz;
            if(score<best){best=score;standing=point;found=true;}
        }
        return found;
    }
    // QA uses the already swept standing graph, then walks every edge through
    // CharacterController.Move. This returns waypoints, never teleports a player.
    public static Vector3[] Route(Vector3 from,Vector3 to,float radius,float height)
    {
        int count=reachableStanding.Count;if(count==0)return new Vector3[0];
        int start=-1,end=-1;float nearStart=.5f*.5f,nearEnd=.5f*.5f;
        for(int i=0;i<count;i++)
        {
            var point=reachableStanding[i];var flatFrom=new Vector3(from.x,point.y,from.z);
            float a=(point-flatFrom).sqrMagnitude,b=(point-to).sqrMagnitude;
            if(a<nearStart&&Sweep(flatFrom,point,radius,height)){start=i;nearStart=a;}
            if(b<nearEnd){end=i;nearEnd=b;}
        }
        if(start<0||end<0)return new Vector3[0];
        var parent=new int[count];for(int i=0;i<count;i++)parent[i]=-2;
        parent[start]=-1;var queue=new Queue<int>();queue.Enqueue(start);
        while(queue.Count>0&&parent[end]==-2)
        {
            int n=queue.Dequeue();for(int j=0;j<count;j++)
            {
                if(parent[j]!=-2)continue;float d=(reachableStanding[n]-reachableStanding[j]).sqrMagnitude;
                if(d<.024f||d>.028f||!Sweep(reachableStanding[n],reachableStanding[j],radius,height))continue;
                parent[j]=n;queue.Enqueue(j);
            }
        }
        if(parent[end]==-2)return new Vector3[0];
        var path=new List<Vector3>();for(int i=end;i>=0;i=parent[i])path.Add(reachableStanding[i]);path.Reverse();return path.ToArray();
    }
    public static Report Check(GameObject room,CharacterController controller,Vector3 entry)
    {
        reachableStanding.Clear();
        var result=new Report{entry=entry,capsuleRadius=controller.radius,capsuleHeight=controller.height};
        var renderers=room.GetComponentsInChildren<MeshRenderer>();
        Bounds floor=new Bounds();bool gotFloor=false;
        foreach(var r in renderers)if(r.name.StartsWith("Floorboard_")){if(!gotFloor){floor=r.bounds;gotFloor=true;}else floor.Encapsulate(r.bounds);}
        if(!gotFloor)throw new InvalidOperationException("COMPACT_V15_MISSING_FLOOR_BOUNDS");
        result.floorHeight=floor.max.y;
        float radius=controller.radius,height=controller.height,step=result.gridStep;
        float x0=floor.min.x+radius+.035f,z0=floor.min.z+radius+.035f;
        int nx=Mathf.FloorToInt((floor.size.x-2*(radius+.035f))/step)+1;
        int nz=Mathf.FloorToInt((floor.size.z-2*(radius+.035f))/step)+1;
        Vector3[] points=new Vector3[nx*nz];bool[] free=new bool[points.Length];int[] distance=new int[points.Length];
        result.totalNodes=points.Length;bool wasEnabled=controller.enabled;controller.enabled=false;Physics.SyncTransforms();
        try
        {
            for(int z=0;z<nz;z++)for(int x=0;x<nx;x++)
            {
                int n=x+z*nx;points[n]=new Vector3(x0+x*step,floor.max.y+.035f,z0+z*step);distance[n]=-1;
                free[n]=Clear(points[n],radius,height);if(free[n])result.walkableNodes++;
            }
            Vector3 entryFoot=new Vector3(entry.x,floor.max.y+.035f,entry.z);
            result.entryClear=Clear(entryFoot,radius,height);
            int start=-1;float nearest=.5f*.5f;
            if(result.entryClear)for(int i=0;i<points.Length;i++)if(free[i])
            {
                float d=(points[i]-entryFoot).sqrMagnitude;
                if(d<nearest&&Sweep(entryFoot,points[i],radius,height)){start=i;nearest=d;}
            }
            var queue=new Queue<int>();if(start>=0){queue.Enqueue(start);distance[start]=0;}
            while(queue.Count>0)
            {
                int n=queue.Dequeue();result.reachableNodes++;int x=n%nx,z=n/nx;
                foreach(int d in new[]{-1,1,-nx,nx})
                {
                    if(d==-1&&x==0||d==1&&x==nx-1||d==-nx&&z==0||d==nx&&z==nz-1)continue;
                    int next=n+d;if(!free[next]||distance[next]>=0||!Sweep(points[n],points[next],radius,height))continue;
                    distance[next]=distance[n]+1;queue.Enqueue(next);
                }
            }
            for(int i=0;i<points.Length;i++)if(distance[i]>=0)reachableStanding.Add(points[i]);
            var stations=new List<Station>();
            for(int f=0;f<Prefixes.Length;f++)
            {
                var station=new Station{furniture=Labels[f],blockers=new string[0]};Bounds bounds=new Bounds();bool found=false;
                foreach(var r in renderers)if(r.name.StartsWith(Prefixes[f])){if(!found){bounds=r.bounds;found=true;}else bounds.Encapsulate(r.bounds);}
                if(!found){station.blockers=new[]{"Furniture mesh assembly not found"};stations.Add(station);continue;}
                bool frontZ=f==4;float sign=frontZ?-Mathf.Sign(bounds.center.z):-Mathf.Sign(bounds.center.x);if(sign==0)sign=1;
                Vector3 normal=frontZ?new Vector3(0,0,sign):new Vector3(sign,0,0);
                Vector3 face=bounds.center;face.y=floor.max.y+.035f;
                if(frontZ)face.z=sign>0?bounds.max.z:bounds.min.z;else face.x=sign>0?bounds.max.x:bounds.min.x;
                station.expectedStanding=face+normal*(radius+.22f);float best=float.MaxValue;
                for(int i=0;i<points.Length;i++)
                {
                    if(!free[i])continue;
                    float front=Vector3.Dot(points[i]-face,normal);
                    float side=frontZ?points[i].x:points[i].z;
                    float low=frontZ?bounds.min.x:bounds.min.z,high=frontZ?bounds.max.x:bounds.max.z;
                    if(front<radius+.025f||front>.85f||side<low+.02f||side>high-.02f)continue;
                    station.freeCandidates++;if(distance[i]<0)continue;station.reachableCandidates++;
                    float score=Vector3.Distance(points[i],station.expectedStanding)+distance[i]*step*.10f;
                    if(score<best){best=score;station.standing=points[i];station.routeMetres=distance[i]*step;station.reachable=true;}
                }
                if(!station.reachable)
                {
                    station.failurePosition=station.expectedStanding;
                    if(Clear(station.failurePosition,radius,height))
                    {
                        float closest=float.MaxValue;
                        for(int i=0;i<points.Length;i++)if(distance[i]>=0)
                        {
                            int x=i%nx,z=i/nx;
                            foreach(int d in new[]{-1,1,-nx,nx})
                            {
                                if(d==-1&&x==0||d==1&&x==nx-1||d==-nx&&z==0||d==nx&&z==nz-1)continue;
                                int next=i+d;if(free[next])continue;
                                float score=(points[next]-station.expectedStanding).sqrMagnitude;
                                if(score<closest){closest=score;station.failurePosition=points[next];}
                            }
                        }
                    }
                    var blocking=new HashSet<string>();
                    foreach(var c in Physics.OverlapCapsule(Bottom(station.failurePosition,radius),Top(station.failurePosition,radius,height),radius,~0,QueryTriggerInteraction.Ignore))blocking.Add(c.name);
                    if(blocking.Count==0)blocking.Add("No connected standing capsule route from the entry");
                    station.blockers=new string[blocking.Count];blocking.CopyTo(station.blockers);
                }
                stations.Add(station);
            }
            result.stations=stations.ToArray();result.passed=result.entryClear&&start>=0;
            foreach(var s in result.stations)result.passed&=s.reachable;
            return result;
        }
        finally{controller.enabled=wasEnabled;Physics.SyncTransforms();}
    }
    static Vector3 Bottom(Vector3 foot,float radius){return foot+Vector3.up*radius;}
    static Vector3 Top(Vector3 foot,float radius,float height){return foot+Vector3.up*(height-radius);}
    static bool Clear(Vector3 foot,float radius,float height){return !Physics.CheckCapsule(Bottom(foot,radius),Top(foot,radius,height),radius,~0,QueryTriggerInteraction.Ignore);}
    static bool Sweep(Vector3 from,Vector3 to,float radius,float height)
    {
        Vector3 delta=to-from;float length=delta.magnitude;
        return length<.0001f||!Physics.CapsuleCast(Bottom(from,radius),Top(from,radius,height),radius,delta/length,length,~0,QueryTriggerInteraction.Ignore);
    }
}
