using UnityEngine;
namespace MemoryWhitebox
{
    public sealed partial class ParkPhotoPrototype
    {
        PhotographyCameraVisual instrumentCamera;
        PhotographyMeterDisplay instrumentMeter;
        float assetPull,meterHeld;
        Transform AssetProxy(string id,Transform parent,Vector3 position,Vector3 size)
        {
            var go=new GameObject(id);go.transform.SetParent(parent,false);go.transform.localPosition=position;go.layer=FirstPersonHands.HandLayer;
            go.AddComponent<BoxCollider>().size=size;instrumentParts[id]=go.transform;partHomes[id]=position;return go.transform;
        }
        internal Transform InstrumentHoverVisual(Collider hit)
        {
            if(!hit || !hit.enabled || !hit.gameObject.activeInHierarchy || HandPresentationBlocked)return null;
            string id=hit.name;
            if(photoMode==PhotographyMode.Meter)
                return id=="meter" && instrumentMeter?instrumentMeter.HoverControl(view.ViewportPointToRay(virtualPointer)):null;
            if(photoMode==PhotographyMode.Aperture)return id=="aperture"?instrumentCamera.apertureRing:null;
            if(photoMode==PhotographyMode.Shutter)return id=="speed"?instrumentCamera.dial:null;
            if(photoMode!=PhotographyMode.Body)return null;
            if(dragPart!=null)
            {
                bool receiver=(dragPart.StartsWith("film-") && loadingStep==1 && id=="chamber") ||
                    (dragPart=="leader" && loadingStep==2 && id=="spool");
                return receiver && instrumentCamera!=null?instrumentCamera.HoverPart(id):null;
            }
            bool operable=id=="rewind" && loadingStep==0 ||
                id.StartsWith("film-") && loadingStep==1 ||
                id=="leader" && loadingStep==2 ||
                id=="advance" && (loadingStep==4 || loadingStep==6) ||
                id=="back" && loadingStep==5 ||
                id=="eyepiece" && roll!=null && roll.ReadyForPark;
            if(!operable)return null;
            var part=instrumentCamera?instrumentCamera.HoverPart(id):null;
            return part?part:hit.transform;
        }
        void BuildAssetInstruments()
        {
            instruments=new GameObject("Photography hand instruments").transform;instruments.SetParent(view.transform,false);
            bodyRoot=new GameObject("Camera operation model").transform;bodyRoot.SetParent(instruments,false);
            meterRoot=new GameObject("Incident light meter").transform;meterRoot.SetParent(instruments,false);
            instrumentCamera=PhotographyCameraVisual.Create(bodyRoot,new Vector3(0,-.135f,.64f),2.5f,FirstPersonHands.HandLayer);
            ApertureHandContact();
            AssetProxy("body",bodyRoot,new Vector3(0,-.03f,.64f),new Vector3(.35f,.22f,.11f));
            foreach(string id in new[]{"back","rewind","chamber","spool","leader","advance","eyepiece","aperture","speed"})AssetProxy(id,bodyRoot,Vector3.zero,id=="back"?new Vector3(.32f,.20f,.02f):new Vector3(.055f,.055f,.04f));
            instrumentParts["chamber"].GetComponent<BoxCollider>().size=new Vector3(.09f,.14f,.08f);
            instrumentParts["spool"].GetComponent<BoxCollider>().size=new Vector3(.08f,.14f,.08f);
            for(int i=0;i<4;i++)
            {
                int iso=filmIsos[i];var proxy=AssetProxy("film-"+iso,bodyRoot,new Vector3(-.18f+i*.12f,-.23f,.6f),new Vector3(.07f,.12f,.07f));
                var go=Instantiate(PhotographyVisualAssets.Load().Film(iso),proxy);go.transform.localScale=Vector3.one*2.5f;
                PhotographyCameraVisual.PrepareVisual(go,FirstPersonHands.HandLayer);PhotographyCameraVisual.SampleFilm(PhotographyVisualAssets.Load(),go,0,0);
            }
            instrumentMeter=PhotographyMeterDisplay.Create(meterRoot,new Vector3(0,-.4f,.60f),3.4f,FirstPersonHands.HandLayer);instrumentMeter.transform.localRotation=Quaternion.Euler(0,180,0);
            var meter=AssetProxy("meter",meterRoot,new Vector3(0,0,.55f),new Vector3(.14f,.24f,.08f));
            meterReadout=Numeric(meter,"",Vector3.zero,.02f);cameraReadout=Numeric(bodyRoot,"",Vector3.zero,.02f);
            SetPhotographyMode(PhotographyMode.Exploration);
        }
        void SyncAssetInstruments()
        {
            bool meter=photoMode==PhotographyMode.Meter;
            meterHeld=Mathf.MoveTowards(meterHeld,meter?1:0,Time.deltaTime*4);
            // Bring the display, hit target and hand grip closer together as it opens.
            meterRoot.localPosition=Vector3.back*(.24f*Mathf.SmoothStep(0,1,meterHeld));
            meterRoot.gameObject.SetActive(meter || meterHeld>0);
            bodyRoot.gameObject.SetActive(photoMode==PhotographyMode.Body || photoMode==PhotographyMode.Aperture || photoMode==PhotographyMode.Shutter);
            instrumentMeter.Apply(filmIsos[meterIsoIndex],MeterEv100,meter && meterReadingValid,meterHeld,MeterSuggestedAperture);
            // The reference captures and gameplay camera never inherit these presentation rotations.
            SyncInstrumentPresentation();
            if(dragPart=="leader")
            {
                // Cursor and spool must be measured in the same projected plane.
                // A fixed world-depth drag otherwise disagrees with the visible receiver.
                var start=(Vector2)view.WorldToScreenPoint(leaderDragOrigin);
                var end=(Vector2)view.WorldToScreenPoint(instrumentCamera.takeUp.position);
                var pointer=(Vector2)view.WorldToScreenPoint(instrumentParts["leader"].position);
                assetPull=Mathf.Clamp01(Vector2.Dot(pointer-start,end-start)/Mathf.Max(.00001f,(end-start).sqrMagnitude));
            }
            int stage=loadingStep>=7?3:loadingStep>=4?2:dragPart=="leader"?1:0;
            instrumentCamera.Apply(loadingStep>0 && loadingStep<6,loadingStep>=2,FilmIso,stage,assetPull,focusDistance,Aperture,ShutterSeconds,flash,Time.deltaTime,smoothDials:true);
            void Follow(string id,Transform target){instrumentParts[id].SetPositionAndRotation(target.position,target.rotation);}
            Follow("rewind",instrumentCamera.rewindKnob);Follow("spool",instrumentCamera.takeUp);Follow("chamber",instrumentCamera.insert);
            if(dragPart!="leader")Follow("leader",instrumentCamera.LeaderTip);
            Follow("advance",instrumentCamera.advanceLever);Follow("eyepiece",instrumentCamera.eyepiece);
            Follow("aperture",instrumentCamera.apertureRing);Follow("speed",instrumentCamera.dial);
            instrumentParts["back"].SetPositionAndRotation(instrumentCamera.back.TransformPoint(new Vector3(.064f,0,0)),instrumentCamera.back.rotation);
            instrumentParts["body"].GetComponent<Collider>().enabled=false; // Never mask the small mechanical controls.
            // The large closed-back proxy used to occlude the rewind/advance controls.
            // Only the mechanical action for this stable step can receive a hit.
            instrumentParts["back"].GetComponent<Collider>().enabled=loadingStep==5;
            instrumentParts["rewind"].GetComponent<Collider>().enabled=loadingStep==0;
            instrumentParts["advance"].GetComponent<Collider>().enabled=loadingStep==4 || loadingStep==6;
            instrumentParts["eyepiece"].GetComponent<Collider>().enabled=loadingStep>=7;
            instrumentParts["aperture"].GetComponent<Collider>().enabled=photoMode==PhotographyMode.Aperture;
            instrumentParts["speed"].GetComponent<Collider>().enabled=photoMode==PhotographyMode.Shutter;
            instrumentParts["chamber"].gameObject.SetActive(loadingStep==1);instrumentParts["spool"].gameObject.SetActive(loadingStep>=2 && loadingStep<6);
            instrumentParts["leader"].gameObject.SetActive(loadingStep==2);
            foreach(int iso in filmIsos)instrumentParts["film-"+iso].gameObject.SetActive(loadingStep==1);
            // Detail rendering uses the same visual off-screen; its proxies must not
            // become invisible obstacles. Body mode restores each operable target above.
            if(CameraDetailMode)foreach(var pair in instrumentParts)
                if(pair.Value.parent==bodyRoot)pair.Value.GetComponent<Collider>().enabled=false;
            float suggested=ParkCameraResponse.SuggestedShutter(8,filmIsos[meterIsoIndex],MeterEv100);
            meterReadout.text=MeterNumbers;
            cameraReadout.text="50 mm / Focus "+focusDistance.ToString("0.00")+" m / f/"+Aperture+" / 1/"+Mathf.RoundToInt(1/ShutterSeconds);
        }
    }
}
