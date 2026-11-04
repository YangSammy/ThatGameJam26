using System.Collections.Generic;
using MemoryWhitebox.Art;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MemoryWhitebox
{
    // Day one and day five load the same art scene. This authored group is dormant on day one.
    public sealed class DepartureBedroom : MonoBehaviour
    {
        public Transform hinge,threshold,spawn,paperAnchor;
        public AlbumWinterVisual finalPage;
        public BoxCollider handle;
        public Renderer[] originalDoor;
        public Renderer[] archivedTapeArt;
        public Transform[] snow;
        public MeshRenderer petals;
        public MeshRenderer[] prints;
        public GameObject tapeKeepsakes,letterBasket;
        public Light doorLight;
        public AudioSource windowSounds;
        public Material cameraProfile;
        Vector3[] initialSnow;
        Vector3 petalsScale,petalsCenter;
        readonly List<GameObject> hidden=new List<GameObject>();
        readonly List<Texture2D> photos=new List<Texture2D>();
        BedroomColorResponse grade;
        WBMain owner;
        public void Enter(WBMain host,EndingChapter ending,PaintingWorld3D album,bool preserveView,Vector3 eye,Quaternion rotation)
        {
            owner=host;gameObject.SetActive(true);
            var image=album.GetFinalPageImage();
            finalPage.SetPageTexture(image);
            finalPage.gameObject.SetActive(image!=null);
            // A developed roll keeps its mechanical camera alive even outside the darkroom.
            // Its progress stays in HouseScene while its presentation is hidden for departure.
            var photo=host.DarkroomWorld.Root;
            if(photo.activeSelf){hidden.Add(photo);photo.SetActive(false);}
            foreach(Transform child in host.houseShell.transform)
            {
                if(!child.gameObject.activeSelf)continue;
                hidden.Add(child.gameObject);child.gameObject.SetActive(false);
            }
            foreach(var root in host.gameObject.scene.GetRootGameObjects())
                if(root.name=="Lighting" && root.activeSelf){hidden.Add(root);root.SetActive(false);}
            foreach(var art in archivedTapeArt)art.enabled=false;
            foreach(var r in originalDoor)
            {
                r.enabled=false;
                var oldHit=r.GetComponent<Collider>();if(oldHit!=null)oldHit.enabled=false;
            }
            host.Room.UseBedroomBounds(new[]{BedroomWalkBounds.Interior},BedroomWalkBounds.Collect(gameObject.scene));
            host.Room.ExitThreshold=threshold;host.Room.ExitHalfWidth=.42f;
            ApplyDoor(ending.DoorOpen?1:0);
            host.Room.ExitOpen=ending.DoorOpen;
            bool keep=preserveView && host.Room.CanWalkAt(eye);
            host.Room.Camera.transform.SetPositionAndRotation(keep?eye:spawn.position,keep?rotation:spawn.rotation);
            host.Room.EnableLook(true);host.SuspendHouseInput();
            grade=host.Room.Camera.gameObject.AddComponent<BedroomColorResponse>();grade.Profile=cameraProfile;
            SceneManager.SetActiveScene(gameObject.scene);
            initialSnow=new Vector3[snow.Length];for(int i=0;i<snow.Length;i++)initialSnow[i]=snow[i].localScale;
            petalsScale=petals.transform.localScale;petalsCenter=petals.transform.InverseTransformPoint(petals.bounds.center);
            tapeKeepsakes.SetActive(host.Done[0]);letterBasket.SetActive(host.Done[2]);
            LoadPrints();
            ApplyThaw(ending.SpringProgress);
            windowSounds.mute=host.Muted;if(!host.StoryFinished)windowSounds.Play();
            Physics.SyncTransforms();
        }

        void LoadPrints()
        {
            ParkFilmRoll roll;
            try{roll=ParkFilmStore.LoadActive();}
            catch(System.Exception ex){Debug.LogWarning("Departure photographs unavailable: "+ex.Message);return;}
            if(roll==null || roll.state!=ParkFilmState.Developed)return;
            for(int i=0;i<prints.Length && i<roll.frames.Count;i++)
            {
                if(!ParkFilmStore.TryReadDevelopedFrame(roll.id,i+1,out var bytes,out var reason)){Debug.LogWarning(reason);continue;}
                var image=new Texture2D(2,2,TextureFormat.RGB24,false);
                if(!image.LoadImage(bytes)){Destroy(image);continue;}
                photos.Add(image);
                var r=prints[i];var props=new MaterialPropertyBlock();props.SetTexture("_MainTex",image);r.SetPropertyBlock(props);
                var scale=r.transform.localScale;scale.y=Mathf.Min(scale.y,scale.x*image.height/image.width);
                scale.x=scale.y*image.width/image.height;r.transform.localScale=scale;r.enabled=true;
            }
        }
        public void ApplyDoor(float opening)
        {
            hinge.localRotation=Quaternion.Euler(0,100*opening,0);
            doorLight.intensity=Mathf.Lerp(.9f,2,opening);
        }
        public void ApplyThaw(float progress)
        {
            float t=Mathf.SmoothStep(0,1,progress);
            for(int i=0;i<snow.Length;i++)
                snow[i].localScale=Vector3.Scale(initialSnow[i],Vector3.Lerp(Vector3.one,new Vector3(.29f,.08f,.37f),t));
            var petal=petals.transform;var center=petal.TransformPoint(petalsCenter);
            petal.localScale=Vector3.Scale(petalsScale,new Vector3(Mathf.Lerp(.25f,1,t),1,Mathf.Lerp(.25f,1,t)));
            petal.position+=center-petal.TransformPoint(petalsCenter);
        }
        void Update()
        {
            if(owner!=null)windowSounds.mute=owner.Muted || owner.ShowHelp || !Application.isFocused || owner.StoryFinished;
        }
        public bool HitHandle(Ray ray)
        {
            return Physics.Raycast(ray,out var hit,2.5f,~0,QueryTriggerInteraction.Collide) && hit.collider==handle;
        }
        public void Leave(WBMain host)
        {
            host.Room.ExitOpen=false;host.Room.ExitThreshold=null;
            SceneManager.SetActiveScene(host.gameObject.scene);
            foreach(var obj in hidden)if(obj)obj.SetActive(true);
            finalPage.SetPageTexture(null);
            if(grade){grade.enabled=false;Destroy(grade);}
            host.Room.CreateShared(host.Room.Camera,host.houseShell);
            if(host.playerSpawn!=null)host.playerSpawn.Apply(host.Room.Camera);
            host.Room.EnableLook(true);host.SuspendHouseInput();
            gameObject.SetActive(false);SceneManager.UnloadSceneAsync(gameObject.scene);
        }
        void OnDestroy(){foreach(var texture in photos)if(texture!=null)Destroy(texture);}
    }
}
