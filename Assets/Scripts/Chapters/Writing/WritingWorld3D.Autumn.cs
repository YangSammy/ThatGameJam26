using UnityEngine;
namespace MemoryWhitebox
{
    public sealed partial class WritingWorld3D
    {
        readonly Transform[] collectionProps=new Transform[4],inventoryProps=new Transform[4];
        float farewellTravel=-1;

        Vector3 TimerSoundPosition=>desk.timer!=null?desk.timer.transform.position:PaperCenter;
        // The memory view uses a separate miniature set. Keep its foley beside the main listener.
        Vector3 MemorySoundPosition=>PaperCenter-sheet.transform.right*.25f;

        static int TimerCue(float before,float after)
        {
            if(after>=before)return 0;
            if(after<=0)return 235;
            int second=Mathf.CeilToInt(after);
            if(second>=Mathf.CeilToInt(before))return 0;
            return second<=15?135:35;
        }

        void PlayWritingStateEdges(int previousItems,string previousPhase,bool clockRunning,float remaining)
        {
            int acquired=State.Collected & ~previousItems;
            for(int i=0;i<collectionProps.Length;i++)
            {
                if((acquired & (1<<i))==0)continue;
                StorySfx.Shot(i==3?134:34,MemorySoundPosition);
                if(i==0)StorySfx.Shot(19,MemorySoundPosition);
            }
            if(clockRunning && (State.Phase=="collect" || State.Phase=="crumple"))
            {
                int cue=TimerCue(remaining,State.Remaining);
                if(cue!=0)StorySfx.Shot(cue,TimerSoundPosition);
            }
            if(previousPhase==State.Phase)return;
            StorySfx.Stop(32);
            if(State.Phase=="farewell")
            {
                farewellTravel=0;
                StorySfx.Shot(36,MemorySoundPosition);
            }
            else farewellTravel=-1;
        }
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
            SetLayer(memory,MemoryLayer);
        }
        void UpdateAutumnActors(float dt)
        {
            var next=MemoryPosition(State.Player);var direction=next-lastMemoryPlayer;
            if(direction.sqrMagnitude>.00001f)player.localRotation=Quaternion.LookRotation(direction);
            player.localPosition=next;lastMemoryPlayer=next;
            float departure=State.Phase=="farewell" || State.Completed?1:0;
            var before=npc.localPosition;
            npc.localPosition=Vector3.Lerp(new Vector3(3.3f,0,.28f),new Vector3(3.3f,0,-.45f),departure>0?State.DepartureWalk:0);
            if(State.Phase=="farewell" && farewellTravel>=0)
            {
                farewellTravel+=Vector3.Distance(before,npc.localPosition);
                if(farewellTravel>=.15f)
                {
                    farewellTravel=-1;
                    StorySfx.Shot(136,MemorySoundPosition);
                }
            }
            var toPlayer=player.localPosition-npc.localPosition;toPlayer.y=0;
            var facing=toPlayer.sqrMagnitude>.0001f?Quaternion.LookRotation(toPlayer):Quaternion.identity;
            npc.localRotation=departure>0?Quaternion.Slerp(facing,Quaternion.Euler(0,180,0),Mathf.SmoothStep(0,1,State.DepartureTurn)):Quaternion.identity;
            farewellSeconds=departure>0?State.FarewellElapsed:0;
            if(farewellVisual!=null)
            {
                // The miniature set ends at the doorway; do not walk into its unbuilt exterior.
                farewellVisual.gameObject.SetActive(!(departure>0 && State.DepartureWalk>=1));
                farewellVisual.Present(time,farewellSeconds,departure>0);
            }
            UpdateMemoryRestoration(dt);
            for(int i=0;i<4;i++)collectionProps[i].gameObject.SetActive(!State.HasItem(i));
            scarf.gameObject.SetActive(false);npcBox.gameObject.SetActive(false);doorHand.gameObject.SetActive(false);
            door.localRotation=Quaternion.Euler(0,100,0);
            float warmth=State.Completed?1:State.InkFresh?.85f:.35f;
            memoryLight.intensity=Mathf.Lerp(memoryLight.intensity,1.0f+warmth*.25f,Mathf.Clamp01(dt*3));
            memoryWindow.sharedMaterial.color=Color.Lerp(new Color(.37f,.46f,.49f),new Color(.9f,.85f,.65f),warmth);
        }
    }
}
