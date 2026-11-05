using UnityEngine;
using MemoryWhitebox.Art;
namespace MemoryWhitebox
{
    public sealed partial class PaintingWorld3D
    {
        AlbumWinterVisual winterSheet,winterSwing,winterBridge,winterLoad,winterFlower,winterDoor;
        Transform winterCrate, bridgeHover;
        static Transform VisualInstance(GameObject prefab,Transform parent,Vector3 position,float scale=1)
        {
            var t=Object.Instantiate(prefab,parent,false).transform;t.name="Visual_"+prefab.name;
            t.localPosition=position;t.localRotation=Quaternion.identity;t.localScale=Vector3.one*scale;
            foreach(var c in t.GetComponentsInChildren<Collider>(true))c.enabled=false;
            Layer(t,parent.gameObject.layer);return t;
        }
        static void HideVisual(Transform t)
        {foreach(var r in t.GetComponentsInChildren<Renderer>(true))r.enabled=false;}
        WinterAlbumWorkspace layout;
        void BindWinterVisuals()
        {
            winterSwing=layout.swing;winterBridge=layout.bridge;winterLoad=layout.counterweight;
            winterFlower=layout.wheel;winterDoor=layout.door;
            foreach(var t in winterDoor.GetComponentsInChildren<Transform>(true))
                if(t.name=="KeyVisual"){doorKeyVisual=t;break;}
            var a=PaintingVisualAssets.Load();
            winterCrate=VisualInstance(a.suitcase,route,Vector3.zero,1.4f);
            var keyVisual=VisualInstance(a.pendant,key,Vector3.zero,10);
            keyVisual.localRotation=Quaternion.Euler(0,180,0);
            BindPagePendant();
            hole.position=layout.keySocket.position;
            // E07's real carved cavity was boolean-cut from D02's contour (including
            // the bail); its nested KeyVisual and this draggable key use a.pendant.
            // hole is only a drop anchor, never a substitute rendered silhouette.
            State.BindLayout(layout);
            observedArt=layout.observationCarrier.GetComponentsInChildren<Renderer>(true);
            TintObservedArt(0);
            var bridgeMesh=layout.realtimeObservation?null:Resources.Load<Mesh>("AlbumBridgeHover");
            if(bridgeMesh && !layout.realtimeObservation)
            {
                var hover=new GameObject("Bridge observation outline",typeof(HoverOutlineProxy));
                bridgeHover=hover.transform;bridgeHover.SetParent(layout.parkObservation,false);
                bridgeHover.position=layout.parkView.position;bridgeHover.rotation=layout.parkObservation.rotation;
                hover.layer=ObservationLayer;hover.GetComponent<MeshFilter>().sharedMesh=bridgeMesh;
            }
            BindClue("shape-swing",layout.swingFocus);
            BindClue("shape-bridge",layout.bridgeFocus);
            if(layout.realtimeObservation)
            {
                foreach(var hit in layout.observedBridge.GetComponentsInChildren<MeshCollider>(true))BindClue("shape-bridge",hit);
                Nodes["shape-bridge"]=layout.bridgeFocus.gameObject;
            }
            BindClue("shape-luggage",layout.luggageFocus);
            BindClue("shape-flower",layout.flowerFocus);
            BindClue("shape-pendant",layout.pendantFocus);
        }
        void BindClue(string id,Collider hit)
        {
            var target=hit.GetComponent<WorldHotspot>() ?? hit.gameObject.AddComponent<WorldHotspot>();
            target.Owner=this;target.Id=id;Nodes[id]=hit.gameObject;
        }

        static readonly Color RoomSky=new Color(.80f,.84f,.79f);
        const float ActFadeDuration=.8f;
        float actTransition;
        int observationAct;
        public float ActWhite=>actTransition>0?1-Mathf.Abs(2*actTransition/ActFadeDuration-1):0;

        void SetObservationAct(int act)
        {
            observationAct=act;layout.parkObservation.gameObject.SetActive(act==0);
            layout.roomObservation.gameObject.SetActive(act==1);
            var anchor=act==0?layout.parkView:layout.roomView;
            fixedEye=Eye=anchor.position;fixedRotation=anchor.rotation;Look=Eye+anchor.forward;
            View.transform.SetPositionAndRotation(Eye,fixedRotation);
            yaw=pitch=0;GazeTarget="";ResetObservationPointer();
            View.backgroundColor=act==0?new Color(.68f,.77f,.80f):RoomSky;
            Physics.SyncTransforms();
        }

        void AdvanceActTransition(float dt)
        {
            float half=ActFadeDuration*.5f;
            actTransition=Mathf.Max(0,actTransition-dt);
            if(observationAct==0 && actTransition<=half)
            {
                // Present one fully white frame even at a low frame rate.
                actTransition=half;SetObservationAct(1);RefreshAlbum();Defocus.SetFocusHeld(false,1);
            }
            if(actTransition==0){Host?.InputContext.Handoff();Host?.SyncInputContext();}
        }
        Renderer[] observedArt;
        float observedWhite=-1;
        readonly MaterialPropertyBlock clueTint=new MaterialPropertyBlock();
        void TintObservedArt(float white)
        {
            // Real scene geometry has many more renderers than the former panorama.
            // A settled white-out must not rewrite every property block on every frame.
            if(Mathf.Approximately(observedWhite,white))return;
            observedWhite=white;
            foreach(var renderer in observedArt)
            {
                var material=renderer.sharedMaterial;
                if(material==null)continue;
                if(material.HasProperty("_PaperWhite"))
                {
                    renderer.GetPropertyBlock(clueTint);
                    clueTint.SetFloat("_PaperWhite",white);
                    renderer.SetPropertyBlock(clueTint);
                    continue;
                }
                if(!material.HasProperty("_Color"))continue;
                renderer.GetPropertyBlock(clueTint);
                clueTint.SetColor("_Color",Color.Lerp(material.color,Color.white,white));
                renderer.SetPropertyBlock(clueTint);
            }
        }
        void RefreshWinterVisuals()
        {
            if(layout==null)return;
            layout.observedPendant.gameObject.SetActive(!State.Painted[4] && !State.KeyInserted);
            layout.routeOrigin.gameObject.SetActive(State.Phase<2);
            layout.observationCarrier.gameObject.SetActive(State.Phase<3);
            winterSwing.gameObject.SetActive(State.Phase==0 && State.Painted[0]);winterSwing.SetSwingAngleRadians(State.SwingAngle);
            winterBridge.gameObject.SetActive(State.Phase==0 && State.Painted[1]);winterBridge.SetBridgeProgress(State.BridgeGrowth);
            winterLoad.gameObject.SetActive(State.Phase==1);winterLoad.SetLoadProgress(State.PlatePressed?1:0);
            winterFlower.gameObject.SetActive(State.Phase==1 && State.Painted[3]);winterFlower.SetFlowerAngleRadians(State.FlowerAngle);
            winterDoor.gameObject.SetActive(State.Phase==1);
            ApplyDoorPerformance();
            winterCrate.gameObject.SetActive(State.Phase==1 && State.Painted[2]);winterCrate.localPosition=new Vector3(State.CrateX,0,0);
        }
    }
}
