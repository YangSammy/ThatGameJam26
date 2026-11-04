using UnityEngine;
using MemoryWhitebox.Art;

namespace MemoryWhitebox
{
    // Only the child poses animate. The four layout roots remain under the artist's control.
    public sealed class AutumnWritingDesk : MonoBehaviour
    {
        public StudyEntry paperEntry,penEntry;
        public BoxCollider paper;
        public Transform penMotion,penTip,penGrip,crumpleGrip;
        public WritingPropVisual foldedPaper,timer;
        public BoxCollider[] basketHits;
        public Transform basketMouth;
        public GameObject[] discardedDrafts;

        public void Present(bool writing)
        {
            paperEntry.GetComponent<Collider>().enabled=!writing;
            penEntry.GetComponent<Collider>().enabled=!writing;
            paper.enabled=writing;
            foreach(var hit in basketHits)hit.enabled=writing;
            if(!writing)
            {
                paper.gameObject.SetActive(true);
                foldedPaper.transform.parent.gameObject.SetActive(false);
                penMotion.localPosition=Vector3.zero;penMotion.localRotation=Quaternion.identity;
            }
        }

        public void ShowDrafts(int draft)
        {
            for(int i=0;i<discardedDrafts.Length;i++)discardedDrafts[i].SetActive(i<draft-1);
        }
    }
}
