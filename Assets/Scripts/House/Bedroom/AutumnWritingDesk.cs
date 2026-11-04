using UnityEngine;
using System.Collections.Generic;
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
        Transform stationery;
        Material inkMaterial;
        internal const float LeftMargin=-.392f/.95f;
        const float UnitsAcrossPage=535f;

        public void Present(bool writing)
        {
            if(writing && !stationery)CreateStationery();
            if(stationery)stationery.gameObject.SetActive(writing);
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

        void CreateStationery()
        {
            stationery=new GameObject("Letter headings").transform;
            stationery.SetParent(paper.transform,false);
            const float titleLength=165;
            DrawHeading("Title",new Vector2(-titleLength/(2*UnitsAcrossPage),.385f),titleLength);
            DrawHeading("Salutation",new Vector2(LeftMargin,.265f),78);
        }

        void DrawHeading(string name,Vector2 origin,float length)
        {
            var stroke=CreateInkStroke(name,stationery);
            ExtendInk(stroke,new List<Vector3>(),origin,0,length);
        }

        internal LineRenderer CreateInkStroke(string name,Transform parent)
        {
            if(!inkMaterial)
            {
                var template=Resources.Load<Material>("WhiteboxMaterial");
                inkMaterial=template?new Material(template):new Material(Shader.Find("Standard"));
                ColorUtility.TryParseHtmlString("#3e3e35",out var inkColor);
                inkMaterial.color=inkColor;
                if(inkMaterial.HasProperty("_Glossiness"))inkMaterial.SetFloat("_Glossiness",.16f);
            }
            var stroke=new GameObject(name).AddComponent<LineRenderer>();
            stroke.transform.SetParent(parent,false);
            stroke.useWorldSpace=false;
            stroke.positionCount=0;
            stroke.startWidth=stroke.endWidth=.0025f;
            stroke.numCapVertices=4;stroke.numCornerVertices=3;
            stroke.sharedMaterial=inkMaterial;
            return stroke;
        }

        internal Vector3 InkPoint(Vector2 origin,float amount)
        {
            float wave=Mathf.Sin(amount*.26f)*(3+Mathf.Sin(amount*.063f)*2)+Mathf.Sin(amount*.67f)*2;
            return paper.center+new Vector3((origin.x+amount/UnitsAcrossPage)*paper.size.x,
                paper.size.y*.5f+.002f,(origin.y+wave*.0022f)*paper.size.z);
        }

        internal void ExtendInk(LineRenderer stroke,List<Vector3> points,Vector2 origin,float start,float end)
        {
            // Use the same sampling grid for prewritten ink and for every drag frame.
            const float spacing=2.5f;
            for(float a=Mathf.Ceil(start/spacing)*spacing;a<end;a+=spacing)
                points.Add(InkPoint(origin,a));
            stroke.positionCount=points.Count;
            stroke.SetPositions(points.ToArray());
        }

        void OnDestroy()
        {
            if(!inkMaterial)return;
            if(Application.isPlaying)Destroy(inkMaterial);
            else DestroyImmediate(inkMaterial);
        }

        public void ShowDrafts(int draft)
        {
            for(int i=0;i<discardedDrafts.Length;i++)discardedDrafts[i].SetActive(i<draft-1);
        }
    }
}
