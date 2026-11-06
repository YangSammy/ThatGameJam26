using UnityEngine;
namespace MemoryWhitebox
{
    public sealed partial class ParkPhotoPrototype
    {
        Camera dialDetailCamera;
        RenderTexture dialDetailImage;
        int dialDetailFrame=-1;
        Transform dialDetailBody;
        Renderer[] dialDetailRenderers;
        Transform shutterDetailTarget;
        Renderer[] shutterDetailRenderers;
        void RenderCameraDetail()
        {
            if(!CameraDetailMode){if(dialDetailCamera!=null)dialDetailCamera.gameObject.SetActive(false);return;}
            if(view==null || instruments==null || dialDetailFrame==Time.frameCount)return;
            Transform target=instrumentCamera!=null?(photoMode==PhotographyMode.Aperture?instrumentCamera.apertureRing:instrumentCamera.dial):
                instrumentParts[photoMode==PhotographyMode.Aperture?"aperture":"speed"];
            if(target==null)return;
            if(dialDetailCamera==null)
            {
                var existing=instruments.Find("Camera dial detail preview");
                var go=existing!=null?existing.gameObject:new GameObject("Camera dial detail preview");go.transform.SetParent(instruments,false);
                dialDetailCamera=go.GetComponent<Camera>();if(dialDetailCamera==null)dialDetailCamera=go.AddComponent<Camera>();dialDetailCamera.enabled=false;
                dialDetailCamera.cullingMask=1<<FirstPersonHands.HandLayer;
                dialDetailCamera.clearFlags=CameraClearFlags.SolidColor;dialDetailCamera.backgroundColor=Color.clear;
                dialDetailCamera.nearClipPlane=.01f;dialDetailCamera.farClipPlane=2;dialDetailCamera.fieldOfView=35;
                dialDetailCamera.allowHDR=false;dialDetailCamera.allowMSAA=false;dialDetailCamera.useOcclusionCulling=false;
                var light=go.GetComponent<Light>();if(light==null)light=go.AddComponent<Light>();light.type=LightType.Spot;light.range=.8f;light.spotAngle=75;light.intensity=1.4f;
                light.cullingMask=1<<FirstPersonHands.HandLayer;light.shadows=LightShadows.None;
                dialDetailImage=dialDetailCamera.targetTexture;
            }
            dialDetailCamera.gameObject.SetActive(true);
            ResizeCameraDetailImage();
            // Aperture retains the full body; shutter gets a centred dial close-up.
            FrameCameraFromAbove(instrumentCamera!=null?instrumentCamera.body:bodyRoot);
            if(photoMode==PhotographyMode.Shutter)FrameShutterDial(target);
            dialDetailFrame=Time.frameCount;
            dialDetailCamera.Render();
        }
        void FrameShutterDial(Transform dial)
        {
            if(shutterDetailTarget!=dial || shutterDetailRenderers==null)
            {shutterDetailTarget=dial;shutterDetailRenderers=dial.GetComponentsInChildren<Renderer>(true);}
            float radius=0;
            foreach(var renderer in shutterDetailRenderers)
            {
                if(renderer==null || !renderer.enabled || renderer.forceRenderingOff || !renderer.gameObject.activeInHierarchy)continue;
                var bounds=renderer.localBounds;
                for(int i=0;i<8;i++)
                {
                    var corner=bounds.center+Vector3.Scale(bounds.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1));
                    var offset=renderer.transform.TransformPoint(corner)-dial.position;
                    // Measure in dial axes so rotating a detent does not pump the zoom.
                    radius=Mathf.Max(radius,Mathf.Abs(Vector3.Dot(offset,dial.right)),Mathf.Abs(Vector3.Dot(offset,dial.forward)));
                }
            }
            if(radius<=0)return;
            var camera=dialDetailCamera;
            camera.orthographicSize=Mathf.Max(.01f,radius/.68f*Mathf.Max(1,1/camera.aspect));
            var rect=CameraDetailRect();
            // Centre the dial on the screen, accounting for the readout below the preview.
            float offsetY=(Screen.height*.5f-rect.center.y)/rect.height*2*camera.orthographicSize;
            camera.transform.position=dial.position-camera.transform.forward*.4f+camera.transform.up*offsetY;
        }
        void FrameCameraFromAbove(Transform body)
        {
            if(body==null)return;
            if(dialDetailBody!=body || dialDetailRenderers==null)
            {dialDetailBody=body;dialDetailRenderers=body.GetComponentsInChildren<Renderer>(true);}
            Vector3 right=body.right,up=body.up,forward=body.forward;
            Vector3 minimum=Vector3.one*float.PositiveInfinity,maximum=Vector3.one*float.NegativeInfinity;
            bool found=false;
            foreach(var renderer in dialDetailRenderers)
            {
                if(renderer==null || !renderer.enabled || renderer.forceRenderingOff || !renderer.gameObject.activeInHierarchy)continue;
                // Loaded film sits inside the closed body. Its skinned leader has
                // deliberately generous deformation bounds, not visible camera size.
                if(instrumentCamera!=null && instrumentCamera.insert!=null && renderer.transform.IsChildOf(instrumentCamera.insert))continue;
                var bounds=renderer.localBounds;
                for(int i=0;i<8;i++)
                {
                    var corner=bounds.center+Vector3.Scale(bounds.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1));
                    var world=renderer.transform.TransformPoint(corner)-body.position;
                    var point=new Vector3(Vector3.Dot(world,right),Vector3.Dot(world,up),Vector3.Dot(world,forward));
                    minimum=Vector3.Min(minimum,point);maximum=Vector3.Max(maximum,point);found=true;
                }
            }
            if(!found)return;
            var centre=(minimum+maximum)*.5f;var size=maximum-minimum;
            dialDetailCamera.orthographic=true;
            dialDetailCamera.orthographicSize=Mathf.Max(.01f,Mathf.Max(size.z,size.x/dialDetailCamera.aspect)*.515f);
            dialDetailCamera.transform.position=body.position+right*centre.x+forward*centre.z+up*(maximum.y+.4f);
            dialDetailCamera.transform.rotation=Quaternion.LookRotation(-up,forward);
            dialDetailCamera.farClipPlane=Mathf.Max(2,size.y+.5f);
        }
        void DrawCameraDetail()
        {
            if(!CameraDetailMode || dialDetailImage==null)return;
            GUI.DrawTexture(CameraDetailRect(),dialDetailImage,ScaleMode.ScaleToFit,true);
        }
        Rect CameraDetailRect()
        {
            // Reserve only the actual readout height. The previous proportional
            // box still reduced this tall camera/lens to a small image on widescreens.
            float width=Screen.width*.94f;
            float footer=CameraDetailValueSize+8+CameraDetailHintSize*2+10;
            float height=Mathf.Max(1,Mathf.Min(Screen.height*.86f,Screen.height-footer-20));
            float top=Mathf.Max(8,(Screen.height-footer-height)*.5f);
            return new Rect((Screen.width-width)*.5f,top,width,height);
        }
        int CameraDetailValueSize=>Mathf.Clamp(Mathf.RoundToInt(Screen.height*.039f),26,44);
        int CameraDetailHintSize=>Mathf.Clamp(Mathf.RoundToInt(Screen.height*.018f),12,16);
        void ResizeCameraDetailImage()
        {
            var rect=CameraDetailRect();
            float scale=Mathf.Min(1,1920f/Mathf.Max(rect.width,rect.height));
            int width=Mathf.Max(1,Mathf.RoundToInt(rect.width*scale));
            int height=Mathf.Max(1,Mathf.RoundToInt(rect.height*scale));
            if(dialDetailImage==null || dialDetailImage.width!=width || dialDetailImage.height!=height)
            {
                dialDetailCamera.targetTexture=null;
                if(dialDetailImage!=null)
                {
                    dialDetailImage.Release();
                    if(Application.isPlaying)Destroy(dialDetailImage);else DestroyImmediate(dialDetailImage);
                }
                dialDetailImage=new RenderTexture(width,height,24,RenderTextureFormat.ARGB32)
                    {name="Camera dial detail",hideFlags=HideFlags.HideAndDontSave,filterMode=FilterMode.Bilinear};
            }
            if(!dialDetailImage.IsCreated())dialDetailImage.Create();
            dialDetailCamera.targetTexture=dialDetailImage;
            dialDetailCamera.aspect=rect.width/rect.height;
        }
        void DisposeCameraDetail()
        {
            if(dialDetailCamera!=null){dialDetailCamera.targetTexture=null;if(Application.isPlaying)Destroy(dialDetailCamera.gameObject);else DestroyImmediate(dialDetailCamera.gameObject);}
            if(dialDetailImage!=null){dialDetailImage.Release();if(Application.isPlaying)Destroy(dialDetailImage);else DestroyImmediate(dialDetailImage);}
            dialDetailCamera=null;dialDetailImage=null;dialDetailFrame=-1;
            dialDetailBody=null;dialDetailRenderers=null;
            shutterDetailTarget=null;shutterDetailRenderers=null;
        }
    }
}
