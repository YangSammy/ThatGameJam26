using UnityEngine;

namespace MemoryWhitebox
{
    // A restrained sun-aligned fill. Large photographic bloom belongs to the lens.
    [ExecuteAlways, DisallowMultipleComponent, RequireComponent(typeof(Light))]
    public sealed class ParkCharacterRimLight : MonoBehaviour
    {
        public Transform subject, viewer;
        public float height=1.25f, distance=1.6f, lift=.45f, sideOffset=.9f;
        public Light sun;
        [Min(0)] public float sunIntensity=4, shadeIntensity=.4f;
        [Min(.01f)] public float transitionSeconds=.35f, sampleInterval=.12f;
        [Min(1)] public float shadowDistance=100;
        // Authored shadow-only canopies have no colliders. Test their actual triangles.
        public MeshFilter[] shadowMeshes=new MeshFilter[0];
        readonly RaycastHit[] hits=new RaycastHit[32];
        Vector3[][] vertices;
        int[][] triangles;
        float visibility, nextSample;
        bool initialized;
        Light rim;
        void OnValidate(){vertices=null;initialized=false;}
        void OnEnable(){rim=GetComponent<Light>();vertices=null;initialized=false;UpdatePose();}
        void LateUpdate(){UpdatePose();}
        void OnDisable(){if(rim!=null)rim.enabled=false;}
        public void UpdatePose()
        {
            if(rim==null)rim=GetComponent<Light>();
            rim.enabled=subject!=null && viewer!=null && subject.gameObject.activeInHierarchy;
            if(!rim.enabled)return;
            Vector3 center=subject.position+Vector3.up*height;
            Vector3 away=sun!=null?Vector3.ProjectOnPlane(-sun.transform.forward,Vector3.up).normalized:
                Vector3.ProjectOnPlane(center-viewer.position,Vector3.up).normalized;
            if(away.sqrMagnitude<.01f)away=subject.forward;
            transform.position=center+away*distance+Vector3.Cross(Vector3.up,away)*sideOffset+Vector3.up*lift;
            transform.rotation=Quaternion.LookRotation(center-transform.position);
            if(!Application.isPlaying || !initialized || Time.unscaledTime>=nextSample)
            {
                visibility=SampleSunVisibility();
                nextSample=Time.unscaledTime+Mathf.Max(.01f,sampleInterval);
            }
            float target=Mathf.Lerp(shadeIntensity,sunIntensity,visibility);
            rim.intensity=!Application.isPlaying || !initialized?target:
                Mathf.Lerp(rim.intensity,target,1-Mathf.Exp(-Time.unscaledDeltaTime/Mathf.Max(.01f,transitionSeconds)));
            initialized=true;
        }

        public float SampleSunVisibility()
        {
            if(subject==null || sun==null || !sun.isActiveAndEnabled || sun.type!=LightType.Directional || sun.intensity<=.001f)return 0;
            if(sun.shadows==LightShadows.None)return 1;
            int count=shadowMeshes==null?0:shadowMeshes.Length;
            if(vertices==null || vertices.Length!=count)
            {
                vertices=new Vector3[count][];triangles=new int[count][];
                for(int i=0;i<count;i++)
                {
                    var mesh=shadowMeshes[i]!=null?shadowMeshes[i].sharedMesh:null;
                    if(mesh!=null && mesh.isReadable){vertices[i]=mesh.vertices;triangles[i]=mesh.triangles;}
                }
            }
            Vector3 direction=-sun.transform.forward;
            Vector3 shoulder=subject.right*.22f;
            int lit=0;
            for(int i=0;i<5;i++)
            {
                Vector3 origin=subject.position+Vector3.up*(i==0?1.65f:i==4?.75f:1.25f);
                if(i==2)origin+=shoulder;else if(i==3)origin-=shoulder;
                if(!Blocked(new Ray(origin+direction*.03f,direction)))lit++;
            }
            return lit/5f;
        }

        bool Blocked(Ray ray)
        {
            int count=Physics.RaycastNonAlloc(ray,hits,shadowDistance,sun.cullingMask,QueryTriggerInteraction.Ignore);
            for(int i=0;i<count;i++)
            {
                var collider=hits[i].collider;
                if(collider.transform.IsChildOf(subject) || (viewer!=null && collider.transform.IsChildOf(viewer)))continue;
                // Invisible walking boundaries do not cast visual shadows.
                var renderer=collider.GetComponent<Renderer>();
                if(renderer==null)renderer=collider.GetComponentInChildren<Renderer>();
                if(collider is TerrainCollider || (renderer!=null && renderer.enabled && renderer.shadowCastingMode!=UnityEngine.Rendering.ShadowCastingMode.Off))return true;
            }
            if(count==hits.Length)return true;
            for(int i=0;i<vertices.Length;i++)
            {
                var filter=shadowMeshes[i];
                if(filter==null || vertices[i]==null || !filter.gameObject.activeInHierarchy || (sun.cullingMask&(1<<filter.gameObject.layer))==0)continue;
                var renderer=filter.GetComponent<Renderer>();
                if(renderer==null || !renderer.enabled || renderer.shadowCastingMode==UnityEngine.Rendering.ShadowCastingMode.Off || !renderer.bounds.IntersectRay(ray))continue;
                Matrix4x4 inverse=filter.transform.worldToLocalMatrix;
                Vector3 origin=inverse.MultiplyPoint3x4(ray.origin),direction=inverse.MultiplyVector(ray.direction);
                var points=vertices[i];var indices=triangles[i];
                for(int j=0;j<indices.Length;j+=3)
                {
                    Vector3 a=points[indices[j]],edge1=points[indices[j+1]]-a,edge2=points[indices[j+2]]-a;
                    Vector3 cross=Vector3.Cross(direction,edge2);float determinant=Vector3.Dot(edge1,cross);
                    if(Mathf.Abs(determinant)<.000001f)continue;
                    float inverseDet=1/determinant;Vector3 offset=origin-a;
                    float u=Vector3.Dot(offset,cross)*inverseDet;if(u<0 || u>1)continue;
                    Vector3 q=Vector3.Cross(offset,edge1);float v=Vector3.Dot(direction,q)*inverseDet;if(v<0 || u+v>1)continue;
                    float t=Vector3.Dot(edge2,q)*inverseDet;
                    if(t>=0 && t<=shadowDistance)return true;
                }
            }
            return false;
        }
    }
}
