using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MemoryWhitebox
{
    // The seasonal scene owns the furniture and the clickable approach. HouseScene
    // continues to own the chapter, its internal cameras, actors and checkpoint.
    public sealed class NarrativeBedroom : MonoBehaviour
    {
        public const string AutumnScene="Assets/Scenes/Bedroom/V17/Bedroom_Autumn.unity";
        public const string WinterScene="Assets/Scenes/Bedroom/V17/Bedroom_Winter.unity";
        public Transform entrance;
        public StudyEntry chapterEntry;
        public Material cameraProfile;
        public AutumnWritingDesk writingDesk;
        public WinterAlbumWorkspace winterAlbum;
        public bool IsEntry(StudyEntry entry)=>entry!=null && (entry==chapterEntry ||
            (writingDesk!=null && (entry==writingDesk.paperEntry || entry==writingDesk.penEntry)));
        readonly List<GameObject> chapterLighting=new List<GameObject>();
        readonly List<GameObject> artwork=new List<GameObject>();
        BedroomColorResponse grade;
        BedroomPaperCue discovery;

        public void Enter(WBMain host)
        {
            foreach(var root in host.gameObject.scene.GetRootGameObjects())
                if(root.name=="Lighting")chapterLighting.Add(root);
            foreach(var root in gameObject.scene.GetRootGameObjects())
                if(root!=gameObject && root.activeSelf)artwork.Add(root);
            var obstacles=BedroomWalkBounds.Collect(gameObject.scene);
            host.Room.UseBedroomBounds(new[]{BedroomWalkBounds.Interior},obstacles);
            grade=host.Room.Camera.gameObject.AddComponent<BedroomColorResponse>();
            grade.Profile=cameraProfile;
            PresentStation(host,false);
            LoadDevelopedPhotos();
            if(chapterEntry.chapter==2 && writingDesk!=null)
            {
                var hit=writingDesk.paper;var t=hit.transform;
                var corner=t.TransformPoint(hit.center+new Vector3(hit.size.x*.47f,hit.size.y*.5f,hit.size.z*.47f));
                discovery=BedroomPaperCue.Create(transform,corner+t.up*.003f,t.right,t.forward,.043f);
            }
            host.Room.Camera.transform.SetPositionAndRotation(entrance.position,entrance.rotation);
            host.Room.EnableLook(true);host.SuspendHouseInput();
        }

        readonly List<Texture2D> developedPhotos=new List<Texture2D>();
        void LoadDevelopedPhotos()
        {
            ParkFilmRoll roll;
            try{roll=ParkFilmStore.LoadActive();}catch(System.Exception e){Debug.LogWarning("Bedroom photos unavailable: "+e.Message);return;}
            if(roll==null || roll.state!=ParkFilmState.Developed)return;
            for(int i=0;i<roll.frames.Count;i++)
            {
                byte[] bytes;string reason;
                Texture2D image=null;
                if(ParkFilmStore.TryReadDevelopedFrame(roll.id,i+1,out bytes,out reason))
                {image=new Texture2D(2,2,TextureFormat.RGB24,false);if(!image.LoadImage(bytes)){Destroy(image);image=null;}}
                developedPhotos.Add(image);
            }
            foreach(var root in gameObject.scene.GetRootGameObjects())foreach(var surface in root.GetComponentsInChildren<Renderer>(true))
            {
                const string prefix="Photo_original_print_";
                if(!surface.name.StartsWith(prefix))continue;
                int index;string number=surface.name.Substring(prefix.Length).Split('.')[0];
                if(!int.TryParse(number,out index) || index>=developedPhotos.Count || developedPhotos[index]==null)continue;
                var props=new MaterialPropertyBlock();surface.GetPropertyBlock(props);
                props.SetTexture("_MainTex",developedPhotos[index]);props.SetColor("_Color",Color.white);
                // Fill the existing paper geometry, including its original aspect ratio.
                var filter=surface.GetComponent<MeshFilter>();
                if(filter!=null && filter.sharedMesh!=null)
                {
                    var uv=filter.sharedMesh.uv;var min=Vector2.one*float.MaxValue;var max=Vector2.one*float.MinValue;
                    foreach(var point in uv){min=Vector2.Min(min,point);max=Vector2.Max(max,point);}
                    var size=max-min;if(size.x>.00001f && size.y>.00001f)props.SetVector("_MainTex_ST",new Vector4(1/size.x,1/size.y,-min.x/size.x,-min.y/size.y));
                }
                surface.SetPropertyBlock(props);
            }
        }
        void OnDestroy(){foreach(var image in developedPhotos)if(image!=null)Destroy(image);}
        public void TickDiscovery(float dt,bool muted){if(discovery)discovery.Step(dt,muted);}

        public void PresentStation(WBMain host,bool station)
        {
            if(station && discovery){Destroy(discovery.gameObject);discovery=null;}
            bool writing=station && chapterEntry.chapter==2;
            bool album=station && !writing;
            SceneManager.SetActiveScene(gameObject.scene);
            host.HideLegacyHousePresentation();
            // The authored album uses its own observation/route/paper layers. Retain
            // chapter lighting only; do not resurrect the old room around those views.
            foreach(var obj in chapterLighting)obj.SetActive(album);
            foreach(var obj in artwork)obj.SetActive(!album);
            chapterEntry.gameObject.SetActive(!station);
            host.StudyEntries.gameObject.SetActive(false);
            if(writingDesk!=null)writingDesk.Present(writing);
            if(winterAlbum!=null)winterAlbum.Present(station);
            grade.enabled=!album;
            Physics.SyncTransforms();
        }

        public void Leave(WBMain host)
        {
            SceneManager.SetActiveScene(host.gameObject.scene);
            host.HideLegacyHousePresentation();
            host.StudyEntries.gameObject.SetActive(false);
            grade.enabled=false;Destroy(grade);
            host.Room.StopMotion();host.SuspendHouseInput();
            SceneManager.UnloadSceneAsync(gameObject.scene);
        }
    }
}

