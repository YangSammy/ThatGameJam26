using UnityEngine;

// A small deterministic stream crosses the open vent; no imported particle package.
public sealed class AutumnLeafDrift : MonoBehaviour
{
    public Transform[] Leaves;
    public Vector3 Source;
    public Vector3 Flow=Vector3.forward;
    Vector3[] start;
    void Start(){start=new Vector3[Leaves.Length];for(int i=0;i<Leaves.Length;i++)start[i]=Leaves[i].position;}
    void Update()
    {
        for(int i=0;i<Leaves.Length;i++)
        {
            float t=Mathf.Repeat(Time.time*.14f+i*.137f,1);
            Leaves[i].position=start[i]+new Vector3(Mathf.Sin(t*8+i)*.14f,-t*.85f,0)+Flow*(t*1.25f);
            Leaves[i].rotation=Quaternion.Euler(t*280,i*43+t*130,t*190);
        }
    }
}
