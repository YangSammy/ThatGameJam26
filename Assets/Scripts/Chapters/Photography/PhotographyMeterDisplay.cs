using UnityEngine;
using ThatGamejam.Art;
namespace MemoryWhitebox
{
    public sealed class PhotographyMeterDisplay : MonoBehaviour
    {
        public LightMeterVisual visual;
        public Transform grip;
        UnityEngine.UI.Text settingsLabel,shutterLabel;
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
            if(settingsLabel==null || shutterLabel==null)
            {
                foreach(var label in visual.GetComponentsInChildren<UnityEngine.UI.Text>(true))
                {if(label.name=="MeterSettings")settingsLabel=label;else if(label.name=="MeterShutter")shutterLabel=label;}
            }
            bool valid=available && !float.IsNaN(ev100) && !float.IsInfinity(ev100);
            if(settingsLabel!=null)settingsLabel.text="ISO "+iso+"  /  f/"+aperture.ToString("0.#",System.Globalization.CultureInfo.InvariantCulture);
            if(shutterLabel!=null)
            {
                float seconds=valid?ParkCameraResponse.SuggestedShutter(aperture,iso,ev100):0;
                shutterLabel.text=valid?(seconds>=1?seconds.ToString("0.#",System.Globalization.CultureInfo.InvariantCulture)+" s":"1/"+Mathf.RoundToInt(1/seconds)+" s"):"--";
            }
            if(valid)visual.SetReadingNormalized(Mathf.InverseLerp(0,20,ev100),true);
            else visual.ClearReading();
        }
    }
}
