using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuManager : MonoBehaviour {

    [System.Serializable]
    public struct MissionInfo {
        public string missionName;
        public Button missionSelect;
        public CheckpointManager.SaveInfo[] rallyPointInfo;
    }

    public GameObject title;
    public GameObject logo;
    public GameObject mainMenu;
    public GameObject[] subMenus;
    public GameObject missionSelect;
    public MissionInfo[] missionInfo;
    public Text missionText;
    public int selectedMission;
    public GameObject entrySelect;
    public Button[] entries;
    public Text entryText;
    public int selectedEntry;
    public Slider missionLoadingBar;
    public Text missionInfoText;
    public Text loadingInfoTextMission;
    public GameObject extraSelect;
    public MissionInfo[] extrasInfo;
    public Text extraText;
    public int selectedExtra;
    public Slider extraLoadingBar;
    public Text extraInfoText;
    public Text loadingInfoTextExtra;
    public int settingsMenuIndex;
    public GameObject[] settingsPanel;
    public GameObject[] selections;

    int activeMenu = -1;
    int curSettingsPanel;
    bool loading;

    private void Start() {
        title.SetActive(true);
        logo.SetActive(true);
        mainMenu.SetActive(true);
        foreach (GameObject menu in subMenus) {
            menu.SetActive(false);
        }
        missionSelect.SetActive(false);
        entrySelect.SetActive(false);
        extraSelect.SetActive(false);
        loadingInfoTextMission.gameObject.SetActive(false);
        loadingInfoTextExtra.gameObject.SetActive(false);
        missionLoadingBar.gameObject.SetActive(false);
        extraLoadingBar.gameObject.SetActive(false);

        UpdatedMissionInfo();
        UpdatedExtraInfo();
    }

    void Update() {
        
    }

    public void EnterMenu(int menu) {
        if (!loading) {
            if (activeMenu == -1) {
                title.SetActive(false);
                logo.SetActive(false);
                mainMenu.SetActive(false);
            }
            else {
                subMenus[activeMenu].SetActive(false);
            }
            subMenus[menu].SetActive(true);
            activeMenu = menu;

            if (menu == settingsMenuIndex) {
                Settings();
            }
        }
    }

    public void BackToMain() {
        if (!loading) {
            subMenus[activeMenu].SetActive(false);
            title.SetActive(true);
            logo.SetActive(true);
            mainMenu.SetActive(true);
            activeMenu = -1;
        }
    }

    public void MissionSelect() {
        if (!loading) {
            entrySelect.SetActive(false);
            missionSelect.SetActive(true);
            missionInfo[selectedMission].missionSelect.Select();
        }
    }

    public void SelectedMission(int mission) {
        if (!loading) {
            missionSelect.SetActive(false);
            if (mission >= 0) {
                selectedMission = mission;
                missionText.text = "Mission: " + missionInfo[mission].missionName;
                UpdatedMissionInfo();
            }
        }
    }

    public void EntrySelect() {
        if (!loading) {
            missionSelect.SetActive(false);
            entrySelect.SetActive(true);
            entries[selectedEntry].Select();
        }
    }

    public void SelectedEntry(int entry) {
        if (!loading) {
                entrySelect.SetActive(false);
            if (entry >= 0) {
                selectedEntry = entry;
                entryText.text = "Rally Point: " + entries[entry].name;
                UpdatedMissionInfo();
            }
        }
    }

    public void UpdatedMissionInfo() {
        missionInfoText.text = missionInfo[selectedMission].missionName + " on Normal\nat " + (selectedEntry == 0 ? "Mission Start" : "Rally Point " + entries[selectedEntry].name);
    }

    public void StartMission() {
        if (!loading) {
            missionLoadingBar.gameObject.SetActive(true);
            CheckpointManager.instance.saveInfo = missionInfo[selectedMission].rallyPointInfo[selectedEntry];
            StartCoroutine(LoadScene(missionInfo[selectedMission].missionName));
        }
    }

    public void ExtraSelect() {
        if (!loading) {
            extraSelect.SetActive(true);
            extrasInfo[selectedExtra].missionSelect.Select();
        }
    }

    public void SelectedExtra(int extra) {
        if (!loading) {
            extraSelect.SetActive(false);
            if (extra >= 0) {
                selectedExtra = extra;
                extraText.text = "Mission: " + extrasInfo[selectedExtra].missionName;
                UpdatedExtraInfo();
            }
        }
    }

    public void UpdatedExtraInfo() {
        extraInfoText.text = extrasInfo[selectedExtra].missionName + " on Normal\nat " + "Mission Start";
    }

    public void StartExtra() {
        if (!loading) {
            extraLoadingBar.gameObject.SetActive(true);
            CheckpointManager.instance.saveInfo = extrasInfo[selectedExtra].rallyPointInfo[0];
            StartCoroutine(LoadScene(extrasInfo[selectedExtra].missionName));
        }
    }

    IEnumerator LoadScene(string level) {
        Debug.Log("Loading Scene: " + level);

        loading = true;
        AsyncOperation load = SceneManager.LoadSceneAsync(level);
        load.allowSceneActivation = true;

        float loadTime = 0;
        float prevProgress = 0;
        while (!load.isDone) {
            if (prevProgress == load.progress) {
                loadTime += Time.deltaTime;
            }
            else {
                loadTime = 0;
            }
            prevProgress = load.progress;

            if (loadTime > 1.5f && load.progress > .8f) {
                loadingInfoTextMission.gameObject.SetActive(true);
                loadingInfoTextExtra.gameObject.SetActive(true);
            }

            missionLoadingBar.value = load.progress;
            extraLoadingBar.value = load.progress;
            yield return null;
        }
    }

    public void Settings() {
        for (int i = 0; i < settingsPanel.Length; i++) {
            settingsPanel[i].SetActive(false);
            selections[i].SetActive(false);
        }

        settingsPanel[curSettingsPanel].SetActive(true);
        selections[curSettingsPanel].SetActive(true);
    }

    public void SwitchSettingsPanel(int panel) {
        settingsPanel[curSettingsPanel].SetActive(false);
        selections[curSettingsPanel].SetActive(false);
        curSettingsPanel = panel;
        settingsPanel[curSettingsPanel].SetActive(true);
        selections[curSettingsPanel].SetActive(true);
    }

    public void ExitGame() {
        Debug.Log("Quitted... but you're in the editor right now so nothing will happen");
        Application.Quit();
    }
}
