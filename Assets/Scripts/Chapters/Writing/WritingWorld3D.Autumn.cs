using UnityEngine;
namespace MemoryWhitebox
{
    public sealed partial class WritingWorld3D
    {
        readonly Transform[] collectionProps=new Transform[4],inventoryProps=new Transform[4];
        Transform timerNeedle,farewellArm;
        void BuildAutumnProps()
        {
            npc.localPosition=new Vector3(3.3f,0,.28f);
            scarf.gameObject.SetActive(false);npcBox.gameObject.SetActive(false);
            string[] colors={"#baab6e","#d6c9a1","#866955","#e7dfc8"};
            Vector3[] sizes={new Vector3(.18f,.05f,.22f),new Vector3(.27f,.08f,.27f),new Vector3(.42f,.32f,.22f),new Vector3(.3f,.015f,.2f)};
            for(int i=0;i<4;i++)
            {
                var at=MemoryPosition(State.ItemPositions[i])+Vector3.up*.22f;
                collectionProps[i]=Child(memory,"collect-"+WritingChapter.ItemIds[i],at,sizes[i],colors[i]).transform;
                inventoryProps[i]=Child(player,"held-"+WritingChapter.ItemIds[i],new Vector3((i-1.5f)*.15f,.55f,-.23f),sizes[i]*.45f,colors[i]).transform;
            }
            Child(memory,"entrance-mat",new Vector3(3.1f,.01f,.35f),new Vector3(1.7f,.02f,.7f),"#d4c3a3");
            var clock=Group("Autumn mechanical timer",new Vector3(.62f,1.05f,.15f));
            Child(clock,"",Vector3.zero,new Vector3(.17f,.025f,.17f),"#d9cdb3",PrimitiveType.Cylinder);
            timerNeedle=Child(clock,"",new Vector3(0,.03f,0),new Vector3(.008f,.012f,.14f),"#4a5049").transform;
            // Hair covers the prototype head at every viewpoint; shoulder/arm motion carries farewell.
            Child(npc,"hair",new Vector3(0,1.15f,0),new Vector3(.34f,.36f,.33f),"#2e302e",PrimitiveType.Sphere);
            farewellArm=Child(npc,"farewell-arm",new Vector3(.28f,.85f,0),new Vector3(.1f,.45f,.1f),"#c2ad91",PrimitiveType.Capsule).transform;
            SetLayer(memory,MemoryLayer);
        }
        void UpdateAutumnActors(float dt)
        {
            var next=MemoryPosition(State.Player);var direction=next-lastMemoryPlayer;
            if(direction.sqrMagnitude>.00001f)player.localRotation=Quaternion.LookRotation(direction);
            player.localPosition=next;lastMemoryPlayer=next;
            float departure=State.Phase=="farewell" || State.Completed?1:0;
            npc.localPosition=Vector3.Lerp(npc.localPosition,new Vector3(3.3f,0,departure>0?-.45f:.28f),Mathf.Clamp01(dt*.7f));
            npc.localRotation=Quaternion.Euler(0,180,0);
            farewellArm.localRotation=Quaternion.Euler(0,0,departure>0?100+Mathf.Sin(time*3)*12:15);
            for(int i=0;i<4;i++){collectionProps[i].gameObject.SetActive(!State.HasItem(i));inventoryProps[i].gameObject.SetActive(State.HasItem(i));}
            scarf.gameObject.SetActive(false);npcBox.gameObject.SetActive(false);doorHand.gameObject.SetActive(false);
            door.localRotation=Quaternion.Euler(0,-100,0);
            var duration=NarrativeDefinition.Resolve(State)?.writingSeconds??60;
            timerNeedle.localRotation=Quaternion.Euler(0,(1-State.Remaining/duration)*360,0);
            float warmth=State.Completed?1:State.InkFresh?.85f:.35f;
            memoryLight.intensity=Mathf.Lerp(memoryLight.intensity,.65f+warmth*1.7f,Mathf.Clamp01(dt*3));
            memoryWindow.sharedMaterial.color=Color.Lerp(new Color(.37f,.46f,.49f),new Color(.9f,.85f,.65f),warmth);
        }
    }
}
