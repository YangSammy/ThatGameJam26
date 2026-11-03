using UnityEngine;
using MemoryWhitebox.Art;
namespace MemoryWhitebox
{
    public sealed partial class PaintingWorld3D
    {
        AlbumWinterVisual winterSheet,winterSwing,winterBridge,winterLoad,winterFlower,winterDoor;
        Transform winterProps,winterCrate;
        static Transform VisualInstance(GameObject prefab,Transform parent,Vector3 position,float scale=1)
        {
            var t=Object.Instantiate(prefab,parent,false).transform;t.name="Visual_"+prefab.name;
            t.localPosition=position;t.localRotation=Quaternion.identity;t.localScale=Vector3.one*scale;
            foreach(var c in t.GetComponentsInChildren<Collider>(true))c.enabled=false;
            Layer(t,parent.gameObject.layer);return t;
        }
        static void HideVisual(Transform t)
        {foreach(var r in t.GetComponentsInChildren<Renderer>(true))r.enabled=false;}
        AlbumWinterVisual Mechanism(GameObject prefab,Vector3 routePosition)
        {return VisualInstance(prefab,winterProps,routePosition/10).GetComponent<AlbumWinterVisual>();}
        void BuildWinterVisuals()
        {
            var a=PaintingVisualAssets.Load();
            if(a==null || !a.sheet || !a.swing || !a.bridge || !a.counterweight || !a.flower || !a.door || !a.suitcase || !a.pendant)
            {Debug.LogError("PaintingVisualAssets missing: retaining legacy visuals.");return;}
            // One metre-to-miniature conversion. Original logic transforms/colliders are untouched.
            winterProps=new GameObject("Visual_AlbumWorldAdapter_SourceScale10").transform;
            winterProps.SetParent(route,false);winterProps.localScale=Vector3.one*10;Layer(winterProps,RouteLayer);
            winterSwing=Mechanism(a.swing,new Vector3(4.5f,0,0));
            winterBridge=Mechanism(a.bridge,new Vector3(10.5f,.7f,0));
            winterLoad=Mechanism(a.counterweight,new Vector3(4,0,0));
            winterFlower=Mechanism(a.flower,new Vector3(8,0,0));
            winterDoor=Mechanism(a.door,new Vector3(14.7f,3.18f,-.44f)); // KeySeat -> unchanged receiver (14.7,4,-.48).
            foreach(var t in new[]{bridge,swingSeat,swingRope,crate,gate,linkage,flower,hole,key})HideVisual(t);
            foreach(var t in petals)HideVisual(t);
            foreach(string id in new[]{"swing-pivot","pressure-plate","gate-walkway","key-door"})HideVisual(Nodes[id].transform);
            winterCrate=VisualInstance(a.suitcase,winterProps,Vector3.zero,.14f);
            // The original draggable key keeps its identity and all hit regions.
            var keyVisual=VisualInstance(a.pendant,key,Vector3.zero,10);
            keyVisual.localRotation=Quaternion.Euler(0,180,0);
            HideVisual(Nodes["album-backing"].transform);HideVisual(Nodes["album-page"].transform);
            var sheet=VisualInstance(a.sheet,paper,new Vector3(0,0,-.012f),3);
            sheet.localRotation=Quaternion.Euler(90,180,0);Layer(sheet,PaperLayer);
            winterSheet=sheet.GetComponent<AlbumWinterVisual>();winterSheet.SetPageTexture(albumTexture);
            // The delivered content submesh is .4 x .225 m: uniform 3x matches the original 1.2 x .675 picking quad.
            // Observation clues retain their original hotspots. Replace only associated renderers.
            var old=observation.GetComponentsInChildren<Renderer>(true);
            foreach(var r in old)
            {
                var hit=r.GetComponent<WorldHotspot>();
                if(hit!=null && (hit.Id=="shape-swing" || hit.Id=="shape-bridge" || hit.Id=="shape-luggage" || hit.Id=="shape-pendant"))r.enabled=false;
                if(hit!=null && string.IsNullOrEmpty(hit.Id) && Mathf.Abs(r.transform.localPosition.x+1.5f)<.7f && r.transform.localPosition.y==1.5f)r.enabled=false;
            }
            VisualInstance(a.swing,observation,new Vector3(-1.5f,.8f,0),6.666667f);
            VisualInstance(a.bridge,observation,new Vector3(1.6f,.69f,.4f),6);
            VisualInstance(a.suitcase,observation,new Vector3(-1.3f,.05f,-.45f),1.6f);
            var observedKey=VisualInstance(a.pendant,observation,new Vector3(-.75f,.38f,-.7f),3.5f);
            observedKey.localRotation=Quaternion.Euler(0,180,0);
        }
        void RefreshWinterVisuals()
        {
            if(winterProps==null)return;
            winterProps.gameObject.SetActive(State.Phase<2);
            winterSwing.gameObject.SetActive(State.Phase==0 && State.Painted[0]);winterSwing.SetSwingAngleRadians(State.SwingAngle);
            winterBridge.gameObject.SetActive(State.Phase==0 && State.Painted[1]);winterBridge.SetBridgeProgress(State.BridgeGrowth);
            winterLoad.gameObject.SetActive(State.Phase==1);winterLoad.SetLoadProgress(State.PlatePressed?1:0);
            winterFlower.gameObject.SetActive(State.Phase==1 && State.Painted[3]);winterFlower.SetFlowerAngleRadians(State.FlowerAngle);
            winterDoor.gameObject.SetActive(State.Phase==1);
            winterDoor.SetDoorState(State.KeyInserted?1:0,State.KeyInserted?1:0,State.KeyInserted?1:0,State.KeyInserted);
            winterCrate.gameObject.SetActive(State.Phase==1 && State.Painted[2]);winterCrate.localPosition=new Vector3(State.CrateX/10,0,0);
        }
    }
}
