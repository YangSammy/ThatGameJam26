using UnityEngine;
using UnityEngine.UI;
namespace MemoryWhitebox
{
    // Scene-authored credits. The existing ending clock owns timing and pause semantics.
    public sealed class EndingCreditsUI : MonoBehaviour
    {
        public Canvas canvas;
        public Image whiteBackground;
        public RectTransform content;
        [Min(1),Tooltip("Scroll speed in Canvas reference pixels per second.")]
        public float scrollSpeed=52;
        [Min(0)] public float edgePadding=80;
        public const float StartSeconds=2.5f;
        public float Duration {get;private set;}
        public bool Finished {get;private set;}
        public float FinishedAt {get;private set;}
        float previousElapsed, distance;
        void Awake(){canvas.enabled=false;}
        public void Present(float elapsed,float fade,Font fallbackFont)
        {
            canvas.enabled=true;
            content.gameObject.SetActive(true);
            foreach(var label in content.GetComponentsInChildren<Text>(true))
            {
                if(fallbackFont!=null)label.font=fallbackFont;
                var row=label.GetComponent<LayoutElement>();
                if(row!=null)row.minHeight=label.preferredHeight;
            }
            LayoutRebuilder.ForceRebuildLayoutImmediate(content);
            float height=((RectTransform)canvas.transform).rect.height;
            float travel=height+content.rect.height+edgePadding*2;
            Duration=travel/Mathf.Max(1,scrollSpeed);
            if(elapsed<previousElapsed){distance=0;Finished=false;FinishedAt=0;}
            float dt=Mathf.Max(0,elapsed-Mathf.Max(previousElapsed,StartSeconds));
            previousElapsed=elapsed;
            if(!Finished)distance+=dt*Mathf.Max(1,scrollSpeed);
            if(!Finished && elapsed>=StartSeconds && distance>=travel){Finished=true;FinishedAt=elapsed;}
            whiteBackground.color=new Color(1,.98f,.91f,fade);
            content.gameObject.SetActive(elapsed>=StartSeconds && !Finished);
            canvas.enabled=true; // Keep the white background beneath Brush until the main menu loads.
            content.anchoredPosition=new Vector2(0,-height*.5f-edgePadding+distance);
        }
    }
}
