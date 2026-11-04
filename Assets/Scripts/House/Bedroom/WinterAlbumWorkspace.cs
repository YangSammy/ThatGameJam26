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

        public void Present(bool active)
        {
            if(wardSet!=null)wardSet.gameObject.SetActive(false);
            gameObject.SetActive(active);
        }
    }
}
