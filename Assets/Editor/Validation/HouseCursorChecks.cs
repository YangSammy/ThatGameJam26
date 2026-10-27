using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace MemoryWhitebox.Editor
{
    public static class HouseCursorChecks
    {
        static readonly List<string> passed=new List<string>();
        static void Check(bool value,string text){if(!value)throw new Exception("HOUSE_CURSOR_FAIL: "+text);passed.Add(text);}
        static void Locked(string text){Check(!Cursor.visible && Cursor.lockState==CursorLockMode.Locked,text);}
        static void Free(string text){Check(Cursor.visible && Cursor.lockState==CursorLockMode.None,text);}
        public static string Run()
        {
            var h=UnityEngine.Object.FindObjectOfType<WBMain>();
            if(!Application.isPlaying || h==null || !h.HouseReady || h.InStation || h.safeInspection.Active)throw new Exception("Start HouseScene in exploration first");
            passed.Clear();bool oldSuppress=PhotoProgressStore.SuppressWrites;PhotoProgressStore.SuppressWrites=true;
            var eye=h.Room.Camera.transform.position;var rotation=h.Room.Camera.transform.rotation;
            try
            {
                h.Room.EnableLook(true);Locked("Exploration locks and hides cursor");
                h.SendMessage("OnApplicationFocus",false);Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
                h.SendMessage("OnApplicationFocus",true);Locked("Focus return restores exploration cursor");
                Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
                h.SendMessage("Update");Locked("External unlock is corrected while exploring");
                h.SendMessage("OnApplicationFocus",false);Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
                h.SendMessage("Update");Free("Unfocused app does not capture desktop cursor");
                h.SendMessage("OnApplicationFocus",true);
                var inspect=h.safeInspection;
                Check(inspect.Open(inspect.subjects[0]),"Inspection opens");h.SendMessage("Update");Free("Inspection retains free cursor");
                inspect.Close();h.SendMessage("Update");Locked("Inspection close restores exploration cursor during release gate");
                // Enter through the existing entry path; modal release still owns user input, not programmatic QA navigation.
                h.Room.Camera.transform.position=HouseLayout.Approaches[0];
                foreach(var entry in h.StudyEntries.GetComponentsInChildren<StudyEntry>())if(entry.chapter==0 && entry.GetComponent<Collider>()!=null){h.Room.Camera.transform.LookAt(entry.GetComponent<Collider>().bounds.center);break;}
                Check(h.TryEnterHouseStation(0),"Tape station controller entry remains available");
                h.SendMessage("OnApplicationFocus",true);Locked("Entry transition hides cursor");
                h.AdvanceHouseTransition(1);h.ReleaseHouseEntryInput();Free("Tape operation shows cursor");
                h.SendMessage("OnApplicationFocus",false);Cursor.lockState=CursorLockMode.Locked;Cursor.visible=false;
                h.SendMessage("OnApplicationFocus",true);Free("Focus return preserves tape operation cursor");
                h.LeaveStation();Locked("Leaving tape hides cursor immediately");
                h.SendMessage("Update");Locked("Exploration remains locked after exit");
                File.WriteAllText("QA/house-cursor-checks.txt",string.Join("\n",passed));
                return passed.Count+" cursor checks passed";
            }
            finally
            {
                if(h.safeInspection.Active)h.safeInspection.Close();if(h.InStation)h.LeaveStation();
                h.SendMessage("OnApplicationFocus",true);h.Room.Camera.transform.SetPositionAndRotation(eye,rotation);h.Room.EnableLook(true);
                PhotoProgressStore.SuppressWrites=oldSuppress;
            }
        }
    }
}
