using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MemoryWhitebox
{
    public sealed class SummerPhotoRoom : MonoBehaviour
    {
        public const string ScenePath="Assets/Scenes/Bedroom/V17/Bedroom_Summer.unity";
        public Transform entrance, cameraDock, worktopMount, darkroomView;
        public DarkroomPrintSceneBinding darkroomStation;
        public OperationArea darkroomArea;
        public BoxCollider counterBlocker;
        public MeshRenderer[] frames;
        public Transform curtain;
        public BoxCollider curtainHit, passageBarrier;
        public Material cameraProfile;
        public Material[] darkroomMetalSources, darkroomMetalVariants;
        [Tooltip("Fixed display exposure inside the darkroom; bedroom exposure is retained outside.")]
        [Range(.1f,2f)] public float darkroomExposure=.55f;
        [Header("Darkroom Beautify")]
        [Range(0,5)] public float darkroomBloom=.8f;
        [Range(0,2)] public float darkroomBloomThreshold=1.05f;
        [Range(0,5)] public float darkroomSharpen=2.4f;
        [Range(0,2)] public float darkroomSaturation=1.05f;
        [Range(.5f,1.5f)] public float darkroomContrast=1.08f;
        BeautifyEffect.Beautify darkroomBeautify;
        Material runtimeCameraProfile;
        readonly List<GameObject> hidden=new List<GameObject>();
        readonly List<Action> restore=new List<Action>();
        readonly List<Texture2D> images=new List<Texture2D>();
        BedroomColorResponse grade;
        bool open;
        float opening;
        string displayedRoll;
        float retryImagesAt;
        BedroomPaperCue discovery;

        void Hide(GameObject obj)
        {
            if(!obj.activeSelf)return;
            hidden.Add(obj);obj.SetActive(false);
        }

        void MoveTo(Transform item,Vector3 position,Quaternion rotation)
        {
            var pos=item.localPosition;var rot=item.localRotation;
            restore.Add(()=>{item.localPosition=pos;item.localRotation=rot;});
            item.SetPositionAndRotation(position,rotation);
        }

        public void Enter(WBMain host,bool keepView)
        {
            var shell=host.houseShell.transform;
            var camera=host.StudyEntries.Find("Camera - photo chapter");
            host.StudyEntries.gameObject.SetActive(true);
            foreach(Transform child in shell)
                if(child!=host.StudyEntries)Hide(child.gameObject);
            foreach(Transform child in host.StudyEntries)if(child!=camera)Hide(child.gameObject);
            foreach(var root in host.gameObject.scene.GetRootGameObjects())if(root.name=="Lighting")Hide(root);
            MoveTo(camera,cameraDock.position,cameraDock.rotation);
            host.BindSummerDarkroom(this);
            ApplyDarkroomMetals(host.gameObject.scene);
            // The physical camera still uses the authored camera anchor when the roll returns.
            // Before the park the only photography entrance is one of the three frames.
            if(camera.gameObject.activeSelf)Hide(camera.gameObject);
            Physics.SyncTransforms();
            var obstacles=BedroomWalkBounds.Collect(gameObject.scene);
            var collider=counterBlocker.bounds;collider.Expand(new Vector3(.36f,0,.36f));obstacles.Add(collider);
            host.Room.UseBedroomBounds(new[]{BedroomWalkBounds.Interior,new Rect(-.34f,2.46f,.68f,.55f),
                new Rect(-1.12f,2.96f,2.89f,2.54f)},obstacles,passageBarrier);
            if(!keepView || !host.Room.CanWalkAt(host.Room.Camera.transform.position))
                host.Room.Camera.transform.SetPositionAndRotation(entrance.position,entrance.rotation);
            host.Room.EnableLook(true);
            // Bloom receives HDR light before the existing fixed display response.
            darkroomBeautify=host.Room.Camera.gameObject.AddComponent<BeautifyEffect.Beautify>();
            ConfigureDarkroomBeautify(darkroomBeautify);
            grade=host.Room.Camera.gameObject.AddComponent<BedroomColorResponse>();
            if(cameraProfile!=null)runtimeCameraProfile=new Material(cameraProfile){name="Summer darkroom display response"};
            grade.Profile=runtimeCameraProfile;
            UpdateDarkroomExposure(host.Room.Camera.transform.position);
            SceneManager.SetActiveScene(gameObject.scene);
            host.SuspendHouseInput();
            if(!keepView && frames.Length>0 && !host.DarkroomWorld.DirectDarkroomAvailable && host.DarkroomWorld.DevelopedRollId==null)
            {
                var frame=frames[0].transform;
                var mesh=frame.GetComponent<MeshFilter>().sharedMesh.bounds;
                float side=Mathf.Sign(Vector3.Dot(entrance.position-frame.position,frame.forward));
                var corner=frame.TransformPoint(mesh.center+new Vector3(mesh.extents.x*.82f,mesh.extents.y*.83f,side*mesh.extents.z));
                discovery=BedroomPaperCue.Create(transform,corner+frame.forward*side*.004f,frame.right,frame.up,.055f);
            }
        }

        public void Refresh(WBMain host)
        {
            string id=host.DarkroomWorld.DevelopedRollId;
            if(!host.ShowHelp && !host.InStation && !GamePauseMenu.BlocksGameplay &&
                id==null && !host.DarkroomWorld.DirectDarkroomAvailable && frames.Length>0 && frames[0]!=null)
                TaskOutlineCue.Show(frames[0].transform,host.Room.Camera,new Renderer[]{frames[0]},true);
            if(discovery)
            {
                if(id!=null || host.DarkroomWorld.DirectDarkroomAvailable || host.InStation)DismissDiscovery();
                else if(!host.ShowHelp)discovery.Step(Time.deltaTime,host.Muted);
            }
            if(id==null || displayedRoll==id || Time.unscaledTime<retryImagesAt)return;
            retryImagesAt=Time.unscaledTime+5;
            for(int i=images.Count;i<frames.Length;i++)
            {
                var surface=frames[i];
                if(!ParkFilmStore.TryReadDevelopedFrame(id,i+1,out var bytes,out var message))
                {Debug.LogWarning(message);return;}
                var image=new Texture2D(2,2,TextureFormat.RGB24,false);
                if(!image.LoadImage(bytes)){Destroy(image);return;}
                var props=new MaterialPropertyBlock();props.SetTexture("_MainTex",image);
                props.SetColor("_Color",Color.white);surface.SetPropertyBlock(props);
                // Fit the landscape exposure inside the existing frame's paper mat.
                var size=surface.transform.localScale;
                size.y=Mathf.Min(size.y,size.x*image.height/image.width);
                size.x=size.y*image.width/image.height;surface.transform.localScale=size;
                images.Add(image);
            }
            displayedRoll=id;
        }

        public bool Click(WBMain host,Ray ray)
        {
            var hit=StudyEntry.PickVisible(ray);
            if(hit==null)return false;
            if(hit==curtainHit)
            {
                if(!CanOpenDarkroom(host))return true;
                // Do not close through the player's capsule in the doorway.
                if(!open || Mathf.Abs(host.Room.Camera.transform.position.z-passageBarrier.transform.position.z)>.35f)open=!open;
                return true;
            }
            for(int i=0;i<frames.Length;i++)
            {
                if(hit.transform!=frames[i].transform)continue;
                if(host.DarkroomWorld.DevelopedRollId!=null)host.ViewSummerPrint(frames[i].transform,i+1);
                else if(!host.DarkroomWorld.DirectDarkroomAvailable && host.TryStartParkPhotography())DismissDiscovery();
                else host.DarkroomWorld.DarkroomNotice(FilmHint(host));
                return true;
            }
            return false;
        }

        void ApplyDarkroomMetals(Scene scene)
        {
            if(darkroomMetalSources==null || darkroomMetalVariants==null)return;
            foreach(var root in scene.GetRootGameObjects())
            foreach(var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                var original=renderer.sharedMaterials;
                var replacement=(Material[])original.Clone();bool changed=false;
                for(int i=0;i<replacement.Length;i++)
                for(int j=0;j<darkroomMetalSources.Length && j<darkroomMetalVariants.Length;j++)
                    if(replacement[i]==darkroomMetalSources[j] && darkroomMetalVariants[j]!=null)
                    {replacement[i]=darkroomMetalVariants[j];changed=true;break;}
                if(!changed)continue;
                renderer.sharedMaterials=replacement;
                var captured=renderer;
                restore.Add(()=>{if(captured!=null)captured.sharedMaterials=original;});
            }
        }

        void ConfigureDarkroomBeautify(BeautifyEffect.Beautify effect)
        {
            effect.quality=BeautifyEffect.BEAUTIFY_QUALITY.BestQuality;
            effect.tonemap=BeautifyEffect.BEAUTIFY_TMO.Linear;
            effect.eyeAdaptation=false;
            effect.depthOfField=false;
            effect.dither=0;
            effect.brightness=1;
            effect.bloom=true;
            effect.bloomAntiflicker=true;
            effect.bloomThreshold=darkroomBloomThreshold;
            effect.bloomIntensity=darkroomBloom;
            effect.sharpen=darkroomSharpen;
            effect.sharpenClamp=.08f;
            effect.saturate=darkroomSaturation;
            effect.contrast=darkroomContrast;
        }

        void UpdateDarkroomExposure(Vector3 eye)
        {
            if(runtimeCameraProfile==null || cameraProfile==null || passageBarrier==null)return;
            // The annex begins beyond this authored doorway. Position, not image brightness,
            // controls the transition, so looking at dark objects never pumps exposure.
            float inside=Mathf.SmoothStep(0,1,Mathf.InverseLerp(
                passageBarrier.transform.position.z-.15f,passageBarrier.transform.position.z+.65f,eye.z));
            if(darkroomBeautify!=null)
            {
                darkroomBeautify.enabled=inside>.001f;
                darkroomBeautify.bloomIntensity=darkroomBloom*inside;
                darkroomBeautify.sharpen=darkroomSharpen*inside;
                darkroomBeautify.saturate=Mathf.Lerp(1,darkroomSaturation,inside);
                darkroomBeautify.contrast=Mathf.Lerp(1,darkroomContrast,inside);
            }
            runtimeCameraProfile.SetFloat("_Exposure",Mathf.Lerp(cameraProfile.GetFloat("_Exposure"),darkroomExposure,inside));
        }

        public void TickCurtain(WBMain host,float dt)
        {
            var pos=host.Room.Camera.transform.position;
            UpdateDarkroomExposure(pos);
            if(!CanOpenDarkroom(host)){open=false;opening=0;}
            else if(host.DarkroomWorld.CarryingExposedFilm && Mathf.Abs(pos.x)<.75f && Mathf.Abs(pos.z-passageBarrier.transform.position.z)<1.25f)open=true;
            opening=Mathf.MoveTowards(opening,open?1:0,dt*2);
            float width=Mathf.Lerp(1.05f,.13f,opening);
            curtain.localScale=new Vector3(width,1,1);
            curtain.localPosition=new Vector3(-.525f+width*.5f,1.06f,2.69f);
            passageBarrier.gameObject.SetActive(opening<.92f);
        }

        public Vector3 CarryThroughDoor(Vector3 eye,Vector3 hand)
        {
            var portal=passageBarrier.transform.position;
            if(Mathf.Abs(eye.x-portal.x)>1f)return hand;
            float near=Mathf.InverseLerp(1.25f,.35f,Mathf.Abs(eye.z-portal.z));
            var tucked=new Vector3(portal.x+Mathf.Clamp(eye.x-portal.x,-.28f,.28f),hand.y,eye.z);
            return Vector3.Lerp(hand,tucked,near);
        }

        public string HoverHint(WBMain host,Ray ray)
        {
            var hit=StudyEntry.PickVisible(ray);if(hit==null)return null;
            if(hit==curtainHit)return !CanOpenDarkroom(host)?"Finish the park photographs first":open?"LMB  Close darkroom curtain":"LMB  Open darkroom curtain";
            foreach(var frame in frames)if(hit.transform==frame.transform)
                return host.DarkroomWorld.DevelopedRollId!=null?"LMB  View photograph":
                    host.DarkroomWorld.DirectDarkroomAvailable?FilmHint(host):"LMB  Enter the park memory";
            return null;
        }

        public bool CanOpenDarkroom(WBMain host)=>host!=null && host.DarkroomWorld!=null && host.DarkroomWorld.PhotographyCompleteForDarkroom;

        static string FilmHint(WBMain host)
        {
            switch(host.DarkroomWorld.Photo.Phase)
            {
                case PhotoChapter.FilmPhase.Exposed:return "Rewind the film in the camera on the writing desk.";
                case PhotoChapter.FilmPhase.Rewound:return "Take the film from the desk camera to the dark bag behind the curtain.";
                default:return "Finish developing the roll behind the curtain.";
            }
        }

        void DismissDiscovery(){if(discovery){Destroy(discovery.gameObject);discovery=null;}}

        public void Leave(WBMain host)
        {
            host.UnbindSummerDarkroom(this);
            SceneManager.SetActiveScene(host.gameObject.scene);
            if(darkroomBeautify){darkroomBeautify.enabled=false;Destroy(darkroomBeautify);}
            if(grade){grade.enabled=false;Destroy(grade);}
            foreach(var reset in restore)reset();
            host.StudyEntries.gameObject.SetActive(false);
            host.HideLegacyHousePresentation();
            host.Room.StopMotion();host.SuspendHouseInput();
            SceneManager.UnloadSceneAsync(gameObject.scene);
        }
        void OnDestroy(){if(darkroomBeautify){darkroomBeautify.enabled=false;Destroy(darkroomBeautify);}if(runtimeCameraProfile!=null)Destroy(runtimeCameraProfile);foreach(var image in images)if(image!=null)Destroy(image);}
    }
}
