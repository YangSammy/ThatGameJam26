using UnityEngine;
namespace MemoryWhitebox
{
    public static class HouseLayout
    {
        public static readonly Quaternion SpawnRotation=Quaternion.Euler(0,0,0);
        public static readonly Vector3 Spawn=new Vector3(-.8f,1.65f,-3.5f);
        public static readonly Vector3[] Origins={new Vector3(-.8f,0,2.35f),new Vector3(-.8f,0,5.65f),new Vector3(-.8f,-.19f,2.5f),new Vector3(-1.8f,-.19f,1.6f),new Vector3(-.8f,-.07f,2.55f)};
        public static readonly Vector3[] Targets={new Vector3(-1.91f,.83f,2.14f),new Vector3(.35f,1,3.1f),new Vector3(-.8f,.83f,2.1f),new Vector3(.35f,.83f,2.1f),new Vector3(-2.8f,1.3f,.8f)};
        public static readonly Vector3[] Approaches={new Vector3(-2,1.65f,1),new Vector3(.35f,1.65f,1),new Vector3(-.8f,1.65f,1),new Vector3(.35f,1.65f,1),new Vector3(-2.2f,1.65f,.2f)};
        public static readonly string[] Names={"磁带录音机","胶片相机","写作书桌","绘画本","围巾","拉开 / 合上帘子"};
    }
}








