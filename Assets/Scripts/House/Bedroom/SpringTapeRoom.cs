using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MemoryWhitebox
{
    [DefaultExecutionOrder(-1000)]
    public sealed class SpringTapeRoom : MonoBehaviour
    {
        public const string ScenePath="Assets/Scenes/Bedroom/V17/Bedroom_Spring.unity";
        [SerializeField] Transform entrance;
        [SerializeField] Material cameraProfile;
        public GameObject workspace;
        public HouseRadioDeck deck;
        public OperationArea workArea;
        public SafeInspectionSubject[] inspectionSubjects;
        readonly List<GameObject> hidden=new List<GameObject>();
        BedroomColorResponse response;
        Vector3 returnEye;
        Quaternion returnRotation;

        // The hardware is visible for editing. Only day one activates it in Play.
        void Awake(){if(workspace!=null)workspace.SetActive(false);}

        void Hide(GameObject obj)
        {
            if(!obj.activeSelf)return;
            hidden.Add(obj);obj.SetActive(false);
        }

        public void Enter(WBMain host)
        {
            returnEye=host.Room.Camera.transform.position;
            returnRotation=host.Room.Camera.transform.rotation;
            foreach(Transform child in host.houseShell.transform)Hide(child.gameObject);
            foreach(var root in host.gameObject.scene.GetRootGameObjects())if(root.name=="Lighting")Hide(root);
            SceneManager.SetActiveScene(gameObject.scene);
            host.AttachSpringTape(this);
            host.Room.UseBedroomBounds(new[]{BedroomWalkBounds.Interior},BedroomWalkBounds.Collect(gameObject.scene));
            host.Room.Camera.transform.SetPositionAndRotation(entrance.position,entrance.rotation);
            host.Room.EnableLook(true);
            response=host.Room.Camera.gameObject.AddComponent<BedroomColorResponse>();response.Profile=cameraProfile;
            Physics.SyncTransforms();host.SuspendHouseInput();
        }

        public void Leave(WBMain host)
        {
            if(host.radioDeck!=null)host.DetachSpringTape(this);
            SceneManager.SetActiveScene(host.gameObject.scene);
            if(response){response.enabled=false;Destroy(response);}
            foreach(var obj in hidden)if(obj)obj.SetActive(true);
            host.Room.CreateShared(host.Room.Camera,host.houseShell);
            host.Room.Camera.transform.SetPositionAndRotation(returnEye,returnRotation);
            host.Room.EnableLook(true);Physics.SyncTransforms();host.SuspendHouseInput();
            SceneManager.UnloadSceneAsync(gameObject.scene);
        }
    }
}
