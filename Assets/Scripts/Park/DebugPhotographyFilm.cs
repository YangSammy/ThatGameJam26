#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using UnityEngine;
namespace MemoryWhitebox
{
    public static class DebugPhotographyFilm
    {
        public static ParkFilmRoll FillCurrentRoll(out bool completedNow)
        {
            completedNow=false;
            var roll=ParkFilmStore.LoadOrCreate();
            if(roll.state!=ParkFilmState.Loaded)return roll;
            int iso=roll.cameraLoading!=null?roll.cameraLoading.iso:100;
            roll=ParkFilmStore.SaveCameraLoading(roll.id,new ParkCameraLoading
            {phase=PhotoChapter.FilmPhase.Ready,threaded=true,mechanicalStep=7,iso=iso});
            while(roll.Remaining>0)
            {
                int index=roll.frames.Count;
                string task=roll.version>=3?roll.NextTask:null;
                byte[] image=CreateDebugNegative(index);
                if(task!=null)
                {
                    var prior=roll.Task(task);
                    var checkpoint=prior!=null?JsonUtility.FromJson<BridgePhotoCheckpoint>(JsonUtility.ToJson(prior)):new BridgePhotoCheckpoint();
                    checkpoint.hasReference=true;checkpoint.aligned=true;checkpoint.captured=false;
                    checkpoint.handJoined=task=="meadow";
                    roll=ParkFilmStore.SaveTask(roll.id,task,checkpoint,image);
                }
                var record=new ParkPhotoRecord
                {
                    id=Guid.NewGuid().ToString("N"),capturedUtc=DateTime.UtcNow.ToString("o"),
                    taskId=task,moment="DEV PLACEHOLDER "+(index+1),feedback="Generated test negative; not a player photograph.",
                    iso=iso,aperture=8,shutterSeconds=1f/125,sceneEv100=13,focusDistance=3,
                    fieldOfView=48,subjectDepth=3,sharp=true,visible=true,framed=true,accepted=true,
                    joinedHands=task=="meadow"
                };
                roll=ParkFilmStore.SaveExposure(roll,record,image);
            }
            completedNow=true;
            return roll;
        }
        static byte[] CreateDebugNegative(int index)
        {
            const int width=480,height=270;
            var texture=new Texture2D(width,height,TextureFormat.RGB24,false);
            try
            {
                // Distinct colour, horizon and frame-count marks make order/reveal easy to inspect.
                var pixels=new Color32[width*height];
                Color sky=Color.HSVToRGB((index*.23f+.1f)%1,.28f,.82f);
                Color ground=Color.HSVToRGB((index*.23f+.1f)%1,.5f,.38f);
                for(int y=0;y<height;y++)for(int x=0;x<width;x++)
                {
                    Color c=y<height/3?ground:sky;
                    if(x<8 || x>=width-8 || y<8 || y>=height-8)c=Color.black;
                    bool head=(x-width/2)*(x-width/2)+(y-165)*(y-165)<22*22;
                    if(head || (x>width/2-24 && x<width/2+24 && y>65 && y<145))c=new Color(.15f,.18f,.2f);
                    if(y>height-45 && y<height-20 && x>=20 && x<20+(index+1)*32 && (x-20)%32<20)c=Color.white;
                    pixels[y*width+x]=c;
                }
                texture.SetPixels32(pixels);texture.Apply();return texture.EncodeToPNG();
            }
            finally{UnityEngine.Object.Destroy(texture);}
        }
    }
}
#endif
