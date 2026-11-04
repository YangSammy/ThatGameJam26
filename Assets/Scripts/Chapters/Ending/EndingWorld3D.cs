using UnityEngine;
namespace MemoryWhitebox
{
    public sealed class EndingWorld3D:WorldChapter3D
    {
        public readonly EndingChapter Ending;
        public Mesh ScarfMesh=>null;
        public override string Help=>Ending.Help;
        NarrativeSceneBinding binding;Transform hinge,snow,flower;Light glow;
        Transform doorParent,handleParent;Vector3 doorPosition,handlePosition;Quaternion doorRotation,handleRotation;
        bool built;float opening,previousSide;bool hadPosition;Quaternion hingeBaseRotation;
        readonly WBInput idle=new WBInput();
        public EndingWorld3D(WBMain host,EndingChapter model):base(host,model,4)
        {Ending=model;Eye=host!=null?host.playerCamera.transform.position:Vector3.zero;Look=Eye+Vector3.forward;}
        void BuildSpring()
        {
            if(built || Host==null)return;
            binding=Host.GetComponent<NarrativeSceneBinding>();
            if(binding==null || binding.entranceDoor==null || binding.threshold==null)return;
            built=true;
            var door=binding.entranceDoor;doorParent=door.parent;doorPosition=door.localPosition;doorRotation=door.localRotation;
            hinge=Group("Spring door hinge",Vector3.zero);hinge.position=door.position-door.right*door.GetComponent<Renderer>().bounds.extents.x;hinge.rotation=door.rotation;
            hingeBaseRotation=hinge.localRotation;
            door.SetParent(hinge,true);
            if(binding.doorHandle!=null){handleParent=binding.doorHandle.parent;handlePosition=binding.doorHandle.localPosition;handleRotation=binding.doorHandle.localRotation;binding.doorHandle.SetParent(hinge,true);}
            var lightObject=Group("Light beyond the doorway",Vector3.zero);lightObject.position=binding.threshold.position-binding.threshold.forward*.35f+Vector3.up*1.3f;
            glow=lightObject.gameObject.AddComponent<Light>();glow.type=LightType.Point;glow.range=4;glow.intensity=1.4f;glow.color=new Color(1,.94f,.77f);
            var at=binding.windowAnchor!=null?binding.windowAnchor.position:new Vector3(-.8f,1.1f,3.9f);
            snow=Group("Spring snow receding",Vector3.zero);snow.position=at+Vector3.forward*.4f;
            Child(snow,"",Vector3.zero,new Vector3(1.4f,.09f,.45f),"#e5e8df");
            flower=Group("Window daisies opening",Vector3.zero);flower.position=at;
            Child(flower,"",new Vector3(0,.12f,0),new Vector3(.025f,.24f,.025f),"#6b805b");
            for(int i=0;i<8;i++){float a=i*Mathf.PI/4;Child(flower,"",new Vector3(Mathf.Cos(a)*.09f,.25f+Mathf.Sin(a)*.09f,0),new Vector3(.1f,.075f,.025f),"#e2d9af",PrimitiveType.Sphere);}
            foreach(var col in snow.GetComponentsInChildren<Collider>())col.enabled=false;
            foreach(var col in flower.GetComponentsInChildren<Collider>())col.enabled=false;
        }
        public bool StepHouse(float dt,Ray ray,bool click)
        {
            var bedroom=Host.DepartureRoom;
            if(bedroom==null){BuildSpring();if(!built)return false;Root.SetActive(true);}
            Ending.Tick(dt,idle);
            bool handled=false;
            if(click && !Ending.DoorOpen && (bedroom!=null?bedroom.HitHandle(ray):HitDoor(ray)))
            {Ending.OpenDoor();Host.SaveStableProgress();handled=true;}
            opening=Mathf.MoveTowards(opening,Ending.DoorOpen?1:0,dt*1.5f);
            if(bedroom!=null)
            {
                bedroom.ApplyDoor(opening);bedroom.ApplyThaw(Ending.SpringProgress);
            }
            else
            {
                hinge.localRotation=hingeBaseRotation*Quaternion.Euler(0,binding.openAngle*opening,0);
                snow.localScale=new Vector3(1,Mathf.Max(.01f,1-Ending.SpringProgress),1);flower.localScale=Vector3.one*Mathf.Lerp(.3f,1,Ending.SpringProgress);
                glow.intensity=Mathf.Lerp(1.4f,2.3f,opening);
            }
            var threshold=bedroom!=null?bedroom.threshold:binding.threshold;
            Host.Room.ExitThreshold=threshold;if(bedroom==null)Host.Room.ExitHalfWidth=binding.doorwayHalfWidth;Host.Room.ExitOpen=opening>.9f;
            Vector3 at=threshold.InverseTransformPoint(Host.Room.Camera.transform.position);
            if(hadPosition && Host.Room.ExitOpen && previousSide>=0 && at.z<0 && Mathf.Abs(at.x)<Host.Room.ExitHalfWidth)Ending.CrossThreshold();
            previousSide=at.z;hadPosition=true;return handled || Ending.Completed;
        }
        public void RestoreDoorPose(){opening=Ending.DoorOpen?1:0;hadPosition=false;}
        bool HitDoor(Ray ray)
        {
            foreach(var target in new[]{binding.entranceDoor,binding.doorHandle})
            {if(target==null)continue;var renderer=target.GetComponent<Renderer>();float distance;if(renderer!=null && renderer.bounds.IntersectRay(ray,out distance) && distance>=0 && distance<2.5f)return true;}
            return false;
        }
        public override void Tick(float dt,WBInput input){}
        public override void Dispose()
        {
            if(built && binding!=null && binding.entranceDoor!=null)
            {
                binding.entranceDoor.SetParent(doorParent,false);binding.entranceDoor.localPosition=doorPosition;binding.entranceDoor.localRotation=doorRotation;
                if(binding.doorHandle!=null){binding.doorHandle.SetParent(handleParent,false);binding.doorHandle.localPosition=handlePosition;binding.doorHandle.localRotation=handleRotation;}
            }
            base.Dispose();
        }
    }
}
