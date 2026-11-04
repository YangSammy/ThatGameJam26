#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace MemoryWhitebox
{
    // Explicit development-player readback. Does not enter chapters or alter saves.
    // -qa-readback [-qa-fps 30|60] [-qa-quit]
    public sealed class PlayerReadbackChecks : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Launch()
        {
            if(Environment.GetCommandLineArgs().Contains("-qa-trace"))
            {
                var trace=new GameObject("Player input readback").AddComponent<PlayerReadbackChecks>();
                trace.traceOnly=true;DontDestroyOnLoad(trace.gameObject);
                return;
            }
            if(Environment.GetCommandLineArgs().Contains("-qa-readback"))
                new GameObject("Player readback validation").AddComponent<PlayerReadbackChecks>();
        }
        IEnumerator Start()
        {
            if(traceOnly)yield break;
            float deadline=Time.realtimeSinceStartup+30;
            while((WBMain.Instance==null || !WBMain.Instance.HouseReady) && Time.realtimeSinceStartup<deadline)yield return null;
            var h=WBMain.Instance;
            if(h==null || !h.HouseReady){Debug.LogError("PLAYER_READBACK_FAIL House not ready");yield break;}
            var w=h.DarkroomWorld;
            Debug.Log("PLAYER_READBACK phase="+w.Photo.Phase+" print="+w.CurrentPrintStage+" rewind="+w.RewindTurns+
                " bag="+w.Photo.BagTurns+" hint="+w.ProgressHint.Replace('\n',' ') +" resolution="+Screen.width+"x"+Screen.height+" fullscreen="+Screen.fullScreenMode);
            var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"-qa-fps"),fps;
            if(index>=0 && index+1<args.Length && int.TryParse(args[index+1],out fps) && (fps==30 || fps==60))
            {
                int oldCap=Application.targetFrameRate,oldSync=QualitySettings.vSyncCount;
                QualitySettings.vSyncCount=0;Application.targetFrameRate=fps;
                yield return new WaitForSecondsRealtime(2);
                float[] samples=new float[180];
                for(int i=0;i<samples.Length;i++){yield return null;samples[i]=Time.unscaledDeltaTime*1000;}
                Array.Sort(samples);
                Debug.Log("PLAYER_FRAME_SAMPLE target="+fps+" meanMs="+samples.Average().ToString("F2")+" p95Ms="+samples[(int)(samples.Length*.95f)].ToString("F2")+
                    " samples="+samples.Length+" resolution="+Screen.width+"x"+Screen.height+" context=HouseSpawnIdle");
                Application.targetFrameRate=oldCap;QualitySettings.vSyncCount=oldSync;
            }
            if(args.Contains("-qa-quit"))Application.Quit();
            Destroy(gameObject);
        }
        bool traceOnly;
        float nextTrace;
        string previousTrace;
        void LateUpdate()
        {
            if(!traceOnly || Time.unscaledTime<nextTrace)return;
            nextTrace=Time.unscaledTime+.5f;
            var park=FindObjectOfType<ParkPhotoPrototype>();
            string state;
            if(park!=null)
                state="park mode="+park.PhotoMode+" pointer="+park.TooltipPoint+" paused="+park.Paused+
                    " frames="+park.ExposedFrames+" cursor="+Cursor.lockState+" focused="+Application.isFocused;
            else
            {
                var h=WBMain.Instance;var w=h!=null?h.DarkroomWorld:null;
                state=w==null?"loading":"house phase="+w.Photo.Phase+" print="+w.CurrentPrintStage+
                    " drag="+w.DraggedId+" hint="+w.ProgressHint.Replace('\n',' ');
            }
            if(state!=previousTrace){Debug.Log("PLAYER_TRACE "+state);previousTrace=state;}
        }
    }
}
#endif
