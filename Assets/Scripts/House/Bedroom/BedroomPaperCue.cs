using UnityEngine;

namespace MemoryWhitebox
{
    // An angled corner on the existing paper surface. It has no receiver or collider.
    public sealed class BedroomPaperCue : MonoBehaviour
    {
        Mesh mesh;
        Material paper;
        Light pool;
        AudioSource sound;
        float elapsed;
        int rustles;

        public static BedroomPaperCue Create(Transform owner, Vector3 corner, Vector3 right, Vector3 up, float width)
        {
            var obj=new GameObject("Paper corner in window light");
            var t=obj.transform;t.SetParent(owner,true);t.position=corner;
            t.rotation=Quaternion.LookRotation(Vector3.Cross(right,up),up);
            var cue=obj.AddComponent<BedroomPaperCue>();
            cue.mesh=new Mesh {name="Loose paper corner"};
            cue.mesh.vertices=new[]{Vector3.zero,Vector3.left*width,Vector3.down*width*.65f};
            cue.mesh.triangles=new[]{0,1,2,2,1,0};cue.mesh.RecalculateNormals();cue.mesh.RecalculateBounds();
            obj.AddComponent<MeshFilter>().sharedMesh=cue.mesh;
            // The thin two-sided fold must stay legible on either side of the paper.
            cue.paper=new Material(Shader.Find("Unlit/Color")){color=new Color(.94f,.86f,.69f)};
            obj.AddComponent<MeshRenderer>().sharedMaterial=cue.paper;
            cue.pool=obj.AddComponent<Light>();cue.pool.type=LightType.Point;cue.pool.range=.9f;cue.pool.intensity=.42f;
            cue.pool.color=new Color(1,.89f,.71f);cue.pool.shadows=LightShadows.None;
            cue.sound=obj.AddComponent<AudioSource>();cue.sound.playOnAwake=false;cue.sound.spatialBlend=1;
            cue.sound.minDistance=.7f;cue.sound.maxDistance=8;cue.sound.rolloffMode=AudioRolloffMode.Linear;
            cue.sound.clip=Resources.Load<AudioClip>("ChapterFoley/Paper");
            cue.baseRotation=t.localRotation;return cue;
        }
        public void Step(float dt,bool muted)
        {
            elapsed+=dt;
            float start=rustles==0?1.1f:10;
            if(rustles<2 && elapsed>=start){rustles++;if(!muted && sound.clip) sound.PlayOneShot(sound.clip,.42f);}
            float since=elapsed-(rustles==2?10:1.1f);
            float lift=since>=0 && since<1.4f?Mathf.Sin(since*Mathf.PI/1.4f):0;
            transform.localRotation=baseRotation*Quaternion.Euler(6+22*lift,0,0);
            pool.intensity=.42f+.32f*lift;
        }
        Quaternion baseRotation;

        void OnDestroy(){if(mesh)Destroy(mesh);if(paper)Destroy(paper);}
    }
}
