using UnityEngine;

namespace MemoryWhitebox
{
    public sealed class TapePreparationBindings : MonoBehaviour
    {
        [Tooltip("0/1: protection sockets; 2: dispenser; 3: movable strip.")]
        public GameObject[] pieces=new GameObject[4];
        public Material socketMaterial,adhesiveMaterial;
        public Transform toolRoot;
        public Collider cutter;
        public Transform ribbon;
        [Min(.001f),Tooltip("Strip travel required to tear, in metres before inspection scaling.")]
        public float tearDistance=.025f;
        public void ShowState(TapeInspectionState.Candidate data)
        {
            if(pieces.Length!=4)return;
            if(pieces[0]!=null && pieces[0].TryGetComponent<Renderer>(out var a))a.sharedMaterial=data.HoleA?adhesiveMaterial:socketMaterial;
            if(pieces[1]!=null && pieces[1].TryGetComponent<Renderer>(out var b))b.sharedMaterial=data.HoleB?adhesiveMaterial:socketMaterial;
        }
    }
}
