using UnityEngine;
using ThatGamejam.Art;
namespace MemoryWhitebox
{
    public sealed class PhotographyMeterDisplay : MonoBehaviour
    {
        public LightMeterVisual visual;
        public Transform grip;
        Texture2D content;string lastText;
        public static PhotographyMeterDisplay Create(Transform parent,Vector3 position,float scale,int layer)
        {
            var go=new GameObject("Meter Visual Adapter");go.transform.SetParent(parent,false);go.transform.localPosition=position;go.transform.localScale=Vector3.one*scale;
            var d=go.AddComponent<PhotographyMeterDisplay>();d.visual=Instantiate(PhotographyVisualAssets.Load().meter,go.transform).GetComponent<LightMeterVisual>();
            PhotographyCameraVisual.PrepareVisual(go,layer);
            foreach(var node in d.visual.GetComponentsInChildren<Transform>(true))if(node.name=="GripAnchor"){d.grip=node;break;}
            d.visual.ClearReading();return d;
        }
        Renderer[] isoSurfaces;
        internal Transform HoverControl(Ray ray)
        {
            if(isoSurfaces==null)isoSurfaces=visual.IsoControl.GetComponentsInChildren<Renderer>(true);
            foreach(var renderer in isoSurfaces)
                if(renderer.enabled && renderer.gameObject.activeInHierarchy && renderer.bounds.IntersectRay(ray))return visual.IsoControl;
            return null;
        }
        public void Apply(int iso,float ev100,bool available,float held,float aperture=8)
        {
            visual.SetHeldProgress(held);visual.SetIsoIndex(iso==200?1:iso==400?2:iso==800?3:0);
            if(!available || float.IsNaN(ev100) || float.IsInfinity(ev100)){visual.ClearReading();return;}
            float seconds=ParkCameraResponse.SuggestedShutter(aperture,iso,ev100);
            string text="ISO"+iso+"\nF"+aperture.ToString("0.#",System.Globalization.CultureInfo.InvariantCulture)+" 1/"+Mathf.RoundToInt(1/seconds)+"\nEV"+ev100.ToString("0.0",System.Globalization.CultureInfo.InvariantCulture);
            if(text!=lastText){Draw(text);lastText=text;}
            visual.SetReadingNormalized(Mathf.InverseLerp(0,20,ev100),true,content);
        }
        void Draw(string text)
        {
            if(content==null){content=new Texture2D(256,128,TextureFormat.RGB24,false);content.name="Live meter reading";content.filterMode=FilterMode.Bilinear;}
            var pixels=new Color32[256*128];for(int i=0;i<pixels.Length;i++)pixels[i]=new Color32(202,200,177,255);
            string chars="0123456789ISOFEV/. ";string[] glyphs={"111101101101111","010110010010111","111001111100111","111001111001111","101101111001001","111100111001111","111100111101111","111001001001001","111101111101111","111101111001111","111010010010111","111100111001111","111101101101111","111100110100100","111100110100111","101101101101010","001001010100100","000000000000010","000000000000000"};
            int x=12,y=110;foreach(char ch in text){if(ch=='\n'){x=12;y-=35;continue;}int n=chars.IndexOf(ch);if(n>=0)for(int row=0;row<5;row++)for(int col=0;col<3;col++)if(glyphs[n][row*3+col]=='1')for(int dy=0;dy<5;dy++)for(int dx=0;dx<5;dx++){int xx=x+col*5+dx,yy=y-row*5-dy;if(xx<256 && yy>=0)pixels[yy*256+xx]=new Color32(31,40,34,255);}x+=20;}
            content.SetPixels32(pixels);content.Apply();
        }
        void OnDestroy(){if(content!=null)Destroy(content);}
    }
}
