using UnityEngine;
namespace MemoryWhitebox
{
    public sealed partial class WritingWorld3D
    {
        void UpdateBoyInventory()
        {
            // Instant item attachment: no pickup reach, IK, or pickup trajectory.
            var offsets=new[]{Vector3.zero,new Vector3(-.32f,.67f,-.08f),new Vector3(.38f,.19f,-.35f),new Vector3(-.15f,.6f,.14f)};
            for(int i=0;i<4;i++)
            {
                inventoryProps[i].gameObject.SetActive(i==2 && State.HasItem(i));
                inventoryProps[i].position=player.TransformPoint(offsets[i]);
                inventoryProps[i].rotation=player.rotation*(i==2?Quaternion.Euler(-15,0,0):i==3?Quaternion.Euler(90,0,-15):Quaternion.identity);
            }
        }
    }
}
