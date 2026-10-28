using UnityEngine;

namespace MemoryWhitebox
{
    public sealed class TapePreparationBindings : MonoBehaviour
    {
        [Tooltip("0/1: protection sockets; 2: dispenser; 3: movable strip.")]
        public GameObject[] pieces=new GameObject[4];
        public Material socketMaterial,adhesiveMaterial;
        [Min(.001f),Tooltip("Strip travel required to tear, in metres before inspection scaling.")]
        public float tearDistance=.025f;
        public void ShowState(TapeInspectionState.Candidate data)
        {
            if(pieces.Length!=4)return;
            if(pieces[0]!=null)pieces[0].GetComponent<Renderer>().sharedMaterial=data.HoleA?adhesiveMaterial:socketMaterial;
            if(pieces[1]!=null)pieces[1].GetComponent<Renderer>().sharedMaterial=data.HoleB?adhesiveMaterial:socketMaterial;
        }
    }
}
