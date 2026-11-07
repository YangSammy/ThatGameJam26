using MemoryWhitebox.Art;
using UnityEngine;

namespace MemoryWhitebox
{
    // Authored in Bedroom_Winter. The chapter drives the carriers; edit their children
    // to change the layout without changing how the album follows the camera.
    public sealed class WinterAlbumWorkspace : MonoBehaviour
    {
        public Transform routeOrigin, observationCarrier, paperCarrier;
        public AlbumWinterVisual sheet, swing, bridge, counterweight, wheel, door;
        public Transform observedSwing, observedBridge, observedSuitcase, observedPendant;
        public Transform parkObservation, roomObservation, parkView, roomView;
        public BoxCollider swingFocus, bridgeFocus, luggageFocus, flowerFocus, pendantFocus;
        public Collider pageHit;
        public Transform keySocket;
        public Transform wardSet, revealStart, revealCutoff;
        public bool realtimeObservation;
        [Header("Pendant discovery and door framing")]
        public Transform pagePendantAnchor, recessOutline;
        [Min(.5f)] public float doorViewSize=1.65f;
        [Min(.1f)] public float doorViewSeconds=.75f;
        public Vector2 doorViewOffset=new Vector2(-.35f,.12f);
        [Range(.02f,.15f)] public float pendantDropRadius=.075f;

        void Awake()
        {
            var assets=WritingVisualAssets.Load();
            if(flowerFocus==null || assets==null || assets.collectedItems.Length<2 || assets.collectedItems[1]==null)return;
            var parent=flowerFocus.transform.parent;
            var old=parent.GetComponent<Renderer>();if(old!=null)old.enabled=false;
            var bouquet=Instantiate(assets.collectedItems[1],parent,false);
            bouquet.name="Observed daisy bouquet";
            foreach(var node in bouquet.GetComponentsInChildren<Transform>(true))node.gameObject.layer=parent.gameObject.layer;
            foreach(var hit in bouquet.GetComponentsInChildren<Collider>(true))hit.enabled=false;
            var renderers=bouquet.GetComponentsInChildren<Renderer>(true);
            if(renderers.Length==0)return;
            var bounds=renderers[0].bounds;foreach(var renderer in renderers)bounds.Encapsulate(renderer.bounds);
            bouquet.transform.localScale*=.45f/Mathf.Max(.001f,bounds.size.y);
            bounds=renderers[0].bounds;foreach(var renderer in renderers)bounds.Encapsulate(renderer.bounds);
            bouquet.transform.position+=parent.position+Vector3.up*.225f-bounds.center;
            flowerFocus.center=flowerFocus.transform.InverseTransformPoint(bounds.center+parent.position+Vector3.up*.225f-bounds.center);
            var scale=flowerFocus.transform.lossyScale;
            flowerFocus.size=new Vector3(.45f/Mathf.Abs(scale.x),.5f/Mathf.Abs(scale.y),.45f/Mathf.Abs(scale.z));
        }
        public void Present(bool active)
        {
            if(wardSet!=null)wardSet.gameObject.SetActive(false);
            gameObject.SetActive(active);
        }
    }
}
