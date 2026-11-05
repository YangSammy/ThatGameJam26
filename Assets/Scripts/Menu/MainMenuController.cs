using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MemoryWhitebox
{
    public sealed class MainMenuController:MonoBehaviour
    {
        [Header("Scene-authored pages")]
        public GameObject home,settings,credits,confirmation,loading;
        public Button newGameButton,continueButton,settingsButton,creditsButton,quitButton;
        public Button confirmButton,cancelButton,settingsBack,settingsApply,settingsDefaults,creditsBack;
        public Slider volume,sensitivity;
        public Dropdown quality;
        public Toggle fullscreen,vsync;
        public Text status,confirmText,loadingText,creditsText,volumeValue,sensitivityValue,settingsStatus;
        [Header("Credits — replace with the confirmed team and licensed asset credits")]
        [TextArea(8,24)] public string creditsCopy="Made with Unity\n\nFull contributor and asset credits will be added before release.";
        Action confirmationAction;
        GameObject returnSelection;
        bool busy;
        Font displayFont;
        void Awake()
        {
            Time.timeScale=1;Application.targetFrameRate=60;PlayerInputPolicy.SetCursor(true);
            displayFont=Font.CreateDynamicFontFromOSFont(new[]{"Microsoft YaHei","Arial"},24);
            foreach(var text in GetComponentsInChildren<Text>(true))text.font=displayFont;
            creditsText.text=creditsCopy;
            newGameButton.onClick.AddListener(NewGame);
            continueButton.onClick.AddListener(Continue);
            settingsButton.onClick.AddListener(OpenSettings);
            creditsButton.onClick.AddListener(()=>{Show(credits);Select(creditsBack);});
            quitButton.onClick.AddListener(()=>Confirm("Quit the game?",Quit));
            confirmButton.onClick.AddListener(()=>{var action=confirmationAction;confirmationAction=null;action?.Invoke();});
            cancelButton.onClick.AddListener(CancelConfirmation);
            settingsBack.onClick.AddListener(Home);creditsBack.onClick.AddListener(Home);
            settingsApply.onClick.AddListener(ApplySettings);settingsDefaults.onClick.AddListener(()=>FillSettings(GamePreferences.Defaults()));
            volume.onValueChanged.AddListener(_=>UpdateSettingLabels());sensitivity.onValueChanged.AddListener(_=>UpdateSettingLabels());
            Home();
        }
        void Update()
        {
            if(busy)return;
            if(Input.GetKeyDown(KeyCode.Escape))
            {if(confirmation.activeSelf)CancelConfirmation();else if(!home.activeSelf)Home();else Confirm("Quit the game?",Quit);}
        }
        void OnApplicationFocus(bool focused){if(focused)PlayerInputPolicy.SetCursor(true);}
        void OnDestroy(){if(displayFont!=null)Destroy(displayFont);}
        void Show(GameObject page)
        {
            foreach(var p in new[]{home,settings,credits,confirmation,loading})p.SetActive(p==page);
        }
        static void Select(Selectable item){if(EventSystem.current!=null)EventSystem.current.SetSelectedGameObject(item.gameObject);}
        public void Home()
        {
            if(busy)return;
            Show(home);RefreshContinue();Select(continueButton.interactable?continueButton:newGameButton);
        }
        public void RefreshContinue()
        {
            bool found=FiveDayProgress.TryInspectSave(GameSavePaths.Root,false,out var day,out var finished,out var error);
            continueButton.interactable=found && !finished;
            status.text=found?(finished?"Story complete. Start a new game to revisit it.":"Continue · Day "+((int)day+1)):
                (error!=""?"Save unavailable. Your files have been kept.":"A quiet story, told through the things we keep.");
        }
        public void NewGame()
        {
            if(busy)return;
            try
            {
                if(MainMenuSaveAccess.HasProgress(GameSavePaths.Root))Confirm("Start a new game?\nYour current progress will be archived and a new story will begin.",()=>StartGame(true));
                else StartGame(true);
            }
            catch(Exception e){status.text="Could not access saves. Progress kept.";Debug.LogWarning(e.Message);}
        }
        public void Continue(){if(!busy && continueButton.interactable)StartGame(false);}
        void Confirm(string message,Action action)
        {
            returnSelection=EventSystem.current!=null?EventSystem.current.currentSelectedGameObject:null;
            confirmationAction=action;confirmText.text=message;Show(confirmation);Select(cancelButton);
        }
        void CancelConfirmation()
        {
            confirmationAction=null;Home();
            if(returnSelection!=null && EventSystem.current!=null)EventSystem.current.SetSelectedGameObject(returnSelection);
        }
        void StartGame(bool fresh)
        {
            if(busy)return;
            if(!Application.CanStreamedLevelBeLoaded(ParkFilmRoute.HouseScenePath)){Home();status.text="Game scene is missing from this build.";return;}
            try
            {
                if(fresh)MainMenuSaveAccess.ArchiveForNewGame(GameSavePaths.Root);
                else if(!FiveDayProgress.TryInspectSave(GameSavePaths.Root,false,out _,out _,out _)){Home();return;}
                MainMenuSaveAccess.LaunchedFromMenu=true;FiveDayProgress.DevelopmentSession=false;
                ParkFilmRoute.EnterDarkroomOnLoad=false;ParkFilmRoute.ForgetHouseView();
                busy=true;Show(loading);loadingText.text="Loading your room…";
                StartCoroutine(LoadGame());
            }
            catch(Exception e){busy=false;Home();status.text="Could not start. Existing progress is kept or archived.";Debug.LogWarning(e);}
        }
        IEnumerator LoadGame()
        {
            yield return null; // Present a covered frame before loading.
            AsyncOperation operation=null;
            try{operation=SceneManager.LoadSceneAsync(ParkFilmRoute.HouseScenePath,LoadSceneMode.Single);}
            catch(Exception e){busy=false;Home();status.text="Loading failed. Please try again.";Debug.LogWarning(e);}
            if(operation!=null)yield return operation;
        }
        void OpenSettings()
        {
            FillSettings(GamePreferences.Current??GamePreferences.Defaults());settingsStatus.text="";Show(settings);Select(volume);
        }
        void FillSettings(GamePreferences.Values value)
        {
            volume.value=value.volume;sensitivity.value=value.lookMultiplier;
            quality.ClearOptions();quality.AddOptions(new System.Collections.Generic.List<string>(QualitySettings.names));
            quality.value=value.quality;fullscreen.isOn=value.fullscreen;vsync.isOn=value.vsync;UpdateSettingLabels();
        }
        void UpdateSettingLabels(){volumeValue.text=Mathf.RoundToInt(volume.value*100)+"%";sensitivityValue.text=sensitivity.value.ToString("0.00")+"×";}
        void ApplySettings()
        {
            try{GamePreferences.Save(new GamePreferences.Values{volume=volume.value,lookMultiplier=sensitivity.value,quality=quality.value,fullscreen=fullscreen.isOn,vsync=vsync.isOn});settingsStatus.text="Settings saved.";}
            catch(Exception e){settingsStatus.text="Could not save settings.";Debug.LogWarning(e.Message);}
        }
        void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying=false;
#else
            Application.Quit();
#endif
        }
    }
}
