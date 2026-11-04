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
        readonly List<GameObject> houseObjects=new List<GameObject>();
        readonly List<GameObject> artwork=new List<GameObject>();
        BedroomColorResponse grade;

        public void Enter(WBMain host)
        {
            foreach(Transform child in host.houseShell.transform)
                if(child!=host.StudyEntries && child.gameObject.activeSelf)houseObjects.Add(child.gameObject);
            foreach(var root in host.gameObject.scene.GetRootGameObjects())
                if(root.name=="Lighting" && root.activeSelf)houseObjects.Add(root);
            foreach(var root in gameObject.scene.GetRootGameObjects())
                if(root!=gameObject && root.activeSelf)artwork.Add(root);
            var obstacles=BedroomWalkBounds.Collect(gameObject.scene);
            host.Room.UseBedroomBounds(new[]{BedroomWalkBounds.Interior},obstacles);
            grade=host.Room.Camera.gameObject.AddComponent<BedroomColorResponse>();
            grade.Profile=cameraProfile;
            PresentStation(host,false);
            host.Room.Camera.transform.SetPositionAndRotation(entrance.position,entrance.rotation);
            host.Room.EnableLook(true);host.SuspendHouseInput();
        }

        public void PresentStation(WBMain host,bool station)
        {
            bool writing=station && chapterEntry.chapter==2;
            bool showHouse=station && !writing;
            SceneManager.SetActiveScene(showHouse?host.gameObject.scene:gameObject.scene);
            foreach(var obj in houseObjects)obj.SetActive(showHouse);
            foreach(var obj in artwork)obj.SetActive(!showHouse);
            chapterEntry.gameObject.SetActive(!station);
            host.StudyEntries.gameObject.SetActive(false);
            if(writingDesk!=null)writingDesk.Present(writing);
            if(winterAlbum!=null)winterAlbum.Present(station);
            grade.enabled=!showHouse;
            Physics.SyncTransforms();
        }

        public void Leave(WBMain host)
        {
            SceneManager.SetActiveScene(host.gameObject.scene);
            foreach(var obj in houseObjects)obj.SetActive(true);
            host.StudyEntries.gameObject.SetActive(true);
            grade.enabled=false;Destroy(grade);
            host.Room.CreateShared(host.Room.Camera,host.houseShell);
            if(host.playerSpawn!=null)host.playerSpawn.Apply(host.Room.Camera);
            host.Room.EnableLook(true);host.SuspendHouseInput();
            SceneManager.UnloadSceneAsync(gameObject.scene);
        }
    }
}

