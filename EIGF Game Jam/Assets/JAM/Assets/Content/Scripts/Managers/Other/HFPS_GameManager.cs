/*
 * HFPS_GameManager.cs - script written by ThunderWire Games
 * ver. 1.32
*/

using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine.PostProcessing;

public enum hideType
{
    Interact, Grab, Examine
}

/// <summary>
/// The main GameManager
/// </summary>
public class HFPS_GameManager : Singleton<HFPS_GameManager> {

    private ConfigHandler configHandler;

    private PostProcessingBehaviour processingBehaviour;
    private PostProcessingProfile processingProfile;

    private ColorGradingModel.Settings colorGrading;

    [Header("Main")]
    public GameObject Player;
    public string m_sceneLoader;

    private SaveGameHandler saveHandler;

    [HideInInspector]
    public InputController inputManager;

    [HideInInspector]
    public Inventory inventoryScript;

    [HideInInspector]
    public ScriptManager scriptManager;

    [HideInInspector]
    public HealthManager healthManager;

    [Header("Cursor")]
    public bool m_ShowCursor = false;

    [Header("Game Panels")]
    public GameObject PauseGamePanel;
    public GameObject MainGamePanel;
    public GameObject PlayerDeadPanel;
    public GameObject TabButtonPanel;

    [Header("Pause UI")]
    public KeyCode ShowPauseMenuKey = KeyCode.Escape;
    public bool reallyPause = false;
    public bool useGreyscale = true;
    public float greyscaleFadeSpeed;

    private bool greyscale;
    private bool greyscaleIn = false;
    private bool greyscaleOut = false;

    [HideInInspector] public bool isPaused = false;

    [Header("Paper UI")]
    public GameObject PaperTextUI;
    public Text PaperReadText;

    [Header("UI Percentagles")]
    public GameObject LightPercentagle;

    private Sprite defaultIcon;
    private Slider lightSlider;
    private Image lightBackground;

    [Header("Valve UI")]
    public Slider ValveSlider;

    [Header("Notification UI")]
    public GameObject NotificationsPanel;
    public GameObject SaveNotification;
    public GameObject NotificationPanel;
    public GameObject NotificationPrefab;
    public Sprite WarningSprite;
    public float saveFadeSpeed;

    private List<GameObject> Notifications = new List<GameObject>();
    private List<GameObject> PickupMessages = new List<GameObject>();

    [Header("Hints UI")]
    public GameObject ExamineNotification;
    public GameObject HintNotification;

    public GameObject HintMessages;
    public GameObject HintTipsPanel;
    public GameObject HintTipPrefab;

    private Text HintText;
    private Text ExamineTxt;

    [Header("Crosshair")]
    public Image Crosshair;

    [Header("UI Amounts")]
    public Text HealthText;
    public GameObject AmmoUI;
    public Text BulletsText;
    public Text MagazinesText;

    [Header("Interact UI")]
    public GameObject InteractUI;
    public GameObject InteractInfoUI;
    public GameObject KeyboardButton1;
    public GameObject KeyboardButton2;

    [Header("Down Examine Buttons")]
    public GameObject DownExamineUI;
    public GameObject ExamineButton1;
    public GameObject ExamineButton2;
    public GameObject ExamineButton3;
    public GameObject ExamineButton4;

    [Header("Down Grab Buttons")]
    public GameObject DownGrabUI;
    public GameObject GrabButton1;
    public GameObject GrabButton2;
    public GameObject GrabButton3;

    public Sprite DefaultSprite;

    [HideInInspector]
    public bool isHeld;
    [HideInInspector]
    public bool canGrab;
    [HideInInspector]
    public bool isGrabbed;
    [HideInInspector]
    public bool isExamining;
    [HideInInspector]
    public bool isLocked;

    private KeyCode UseKey;
    private KeyCode GrabKey;
    private KeyCode ThrowKey;
    private KeyCode RotateKey;
    private KeyCode CursorKey;
    private KeyCode InventoryKey;

    private bool playerLocked;
    private int oldBlurLevel;

    private bool uiInteractive = true;
    private bool isOverlapping;
    private bool isPressed;
    private bool antiSpam;

    [HideInInspector]
    public bool ConfigError;

    void Awake()
    {
        healthManager = Camera.main.transform.root.gameObject.GetComponent<HealthManager>();
        scriptManager = Player.transform.GetChild(0).transform.GetChild(0).GetComponent<ScriptManager>();
        processingBehaviour = Camera.main.gameObject.GetComponent<PostProcessingBehaviour>();

        processingProfile = processingBehaviour.profile;
        colorGrading = processingProfile.colorGrading.settings;
        lightSlider = LightPercentagle.GetComponentInChildren<Slider>();
        lightBackground = lightSlider.transform.GetChild(0).GetComponent<Image>();
        defaultIcon = LightPercentagle.transform.GetChild(0).GetComponent<Image>().sprite;

        configHandler = GetComponent<ConfigHandler>();
        saveHandler = GetComponent<SaveGameHandler>();
        inputManager = GetComponent<InputController>();
        inventoryScript = GetComponent<Inventory>();

        uiInteractive = true;
    }

    void Start()
    {
        SetupUI();
        Unpause();

        if (m_ShowCursor) {
            Cursor.visible = (true);
            Cursor.lockState = CursorLockMode.None;
        } else {
            Cursor.visible = (false);
            Cursor.lockState = CursorLockMode.Locked;
        }

        if (useGreyscale)
        {
            processingProfile.colorGrading.enabled = true;
            colorGrading.basic.saturation = 1;
        }
    }

    void SetupUI()
    {
        TabButtonPanel.SetActive(false);
        SaveNotification.SetActive(false);

        HideSprites(hideType.Interact);
        HideSprites(hideType.Grab);
        HideSprites(hideType.Examine);

        HintNotification.SetActive(false);
        ExamineNotification.SetActive(false);

        Color HintColor = HintNotification.GetComponent<Image>().color;
        HintNotification.GetComponent<Image>().color = new Color(HintColor.r, HintColor.g, HintColor.b, 0);
        Color ExmColor = ExamineNotification.GetComponent<Image>().color;
        ExamineNotification.GetComponent<Image>().color = new Color(ExmColor.r, ExmColor.g, ExmColor.b, 0);

        HintText = HintNotification.transform.GetChild(0).GetComponent<Text>();
        ExamineTxt = ExamineNotification.transform.GetChild(0).GetComponent<Text>();
    }

    void Update()
    {
        transform.SetSiblingIndex(0);

        if (inputManager.HasInputs())
        {
            UseKey = inputManager.GetInput("Use");
            GrabKey = inputManager.GetInput("Pickup");
            ThrowKey = inputManager.GetInput("Throw");
            RotateKey = inputManager.GetInput("Fire");
            CursorKey = inputManager.GetInput("Zoom");
            InventoryKey = inputManager.GetInput("Inventory");
        }

        if (configHandler.ContainsSectionKey("Game", "Volume"))
        {
            float volume = float.Parse(configHandler.Deserialize("Game", "Volume"));
            AudioListener.volume = volume;
        }

        if (!uiInteractive) return;

        if (Input.GetKeyDown(ShowPauseMenuKey) && !isPressed && !antiSpam)
        {
            isPressed = true;
            PauseGamePanel.SetActive(!PauseGamePanel.activeSelf);
            MainGamePanel.SetActive(!MainGamePanel.activeSelf);

            StartCoroutine(AntiPauseSpam());

            if (useGreyscale)
            {
                if (!greyscaleIn)
                {
                    GreyscaleScreen(true);
                }
                else if(!greyscaleOut)
                {
                    GreyscaleScreen(false);
                }
            }

            isPaused = !isPaused;
        }
        else if (isPressed)
        {
            isPressed = false;
        }

        if (PauseGamePanel.activeSelf && isPaused && isPressed)
        {
            Crosshair.enabled = false;
            LockPlayerControls(false, false, true, 3, true);
            scriptManager.GetScript<PlayerFunctions>().enabled = false;
            GetComponent<FloatingIconManager>().SetAllIconsVisible(false);
            if (reallyPause)
            {
                Time.timeScale = 0;
            }
        }
        else if (isPressed)
        {
            Crosshair.enabled = true;
            LockPlayerControls(true, true, false, 3, false);
            scriptManager.GetScript<PlayerFunctions>().enabled = true;
            GetComponent<FloatingIconManager>().SetAllIconsVisible(true);
            if (TabButtonPanel.activeSelf)
            {
                TabButtonPanel.SetActive(false);
            }
            if (reallyPause)
            {
                Time.timeScale = 1;
            }
        }

        if (Input.GetKeyDown(InventoryKey) && !isPressed && !isPaused && !isOverlapping)
        {
            isPressed = true;
            TabButtonPanel.SetActive(!TabButtonPanel.activeSelf);
        }
        else if (isPressed)
        {
            isPressed = false;
        }

        NotificationsPanel.SetActive(!TabButtonPanel.activeSelf);

        if (TabButtonPanel.activeSelf && isPressed)
        {
            Crosshair.enabled = false;
            GetComponent<FloatingIconManager>().SetAllIconsVisible(false);
            LockPlayerControls(false, false, true, 3, true);
            HideSprites(hideType.Interact);
            HideSprites(hideType.Grab);
            HideSprites(hideType.Examine);
        }
        else if (isPressed)
        {
            Crosshair.enabled = true;
            LockPlayerControls(true, true, false, 3, false);
            GetComponent<FloatingIconManager>().SetAllIconsVisible(true);
        }

        LockScript<ExamineManager>(!TabButtonPanel.activeSelf);

        if (Notifications.Count > 3)
        {
            Destroy(Notifications[0]);
        }

        Notifications.RemoveAll(GameObject => GameObject == null);

        if (greyscale)
        {
            if (greyscaleIn)
            {
                if (colorGrading.basic.saturation > 0)
                {
                    colorGrading.basic.saturation -= Time.fixedDeltaTime * greyscaleFadeSpeed;
                }
                else if (colorGrading.basic.saturation <= 0)
                {
                    colorGrading.basic.saturation = 0;
                }
            }

            if (greyscaleOut)
            {
                if (colorGrading.basic.saturation <= 1)
                {
                    colorGrading.basic.saturation += Time.fixedDeltaTime * greyscaleFadeSpeed;
                }
                else if (colorGrading.basic.saturation >= 1)
                {
                    colorGrading.basic.saturation = 1;
                    greyscaleOut = false;
                }
            }
        }

        processingProfile.colorGrading.settings = colorGrading;
    }

    private void OnDisable()
    {
        colorGrading.basic.saturation = 1;
        processingProfile.colorGrading.settings = colorGrading;
    }

    IEnumerator AntiPauseSpam()
    {
        antiSpam = true;
        yield return new WaitForSecondsRealtime(0.5f);
        antiSpam = false;
    }

    public void ShowInventory()
    {
        TabButtonPanel.SetActive(true);
        NotificationsPanel.SetActive(false);
        Crosshair.enabled = false;
        LockPlayerControls(false, false, true, 3, true);
        HideSprites(hideType.Interact);
        HideSprites(hideType.Grab);
        HideSprites(hideType.Examine);
    }

    public void GreyscaleScreen(bool Greyscale)
    {
        greyscale = true;

        switch (Greyscale)
        {
            case true:
                greyscaleIn = true;
                greyscaleOut = false;
                break;
            case false:
                greyscaleIn = false;
                greyscaleOut = true;
                break;
        }
    }

    public void Unpause()
    {
        GetComponent<FloatingIconManager>().SetAllIconsVisible(true);

        if (TabButtonPanel.activeSelf)
        {
            TabButtonPanel.SetActive(false);
        }

        if (useGreyscale)
        {
            GreyscaleScreen(false);
        }

        Crosshair.enabled = true;
        LockPlayerControls(true, true, false, 3, false);
        PauseGamePanel.SetActive(false);
        MainGamePanel.SetActive(true);
        isPaused = false;

        if (reallyPause)
        {
            Time.timeScale = 1;
        }
    }


    /// <summary>
    /// Lock some Player Controls
    /// </summary>
    /// <param name="Controller">Player Controller Enabled State</param>
    /// <param name="Interact">Interact Enabled State</param>
    /// <param name="CursorVisible">Show, Hide Cursor?</param>
    /// <param name="BlurLevel">0 - Null, 1 - MainCam Blur, 2 - ArmsCam Blur, 3 - Both Blur</param>
    /// <param name="BlurEnable">Enable/Disable Blur?</param>
    /// <param name="ResetBlur">Reset Blur?</param>
    /// <param name="ForceLockLevel">0 - Null, 1 = Enable, 2 - Disable</param>
    public void LockPlayerControls(bool Controller, bool Interact, bool CursorVisible, int BlurLevel = 0, bool BlurEnable = false, bool ResetBlur = false, int ForceLockLevel = 0)
    {
        if(ForceLockLevel == 2)
        {
            playerLocked = false;
        }

        if (!playerLocked)
        {
            //Controller Lock
            Player.GetComponent<PlayerController>().controllable = Controller;
            scriptManager.GetScript<PlayerFunctions>().enabled = Controller;          
            scriptManager.ScriptGlobalState = Controller;
            LockScript<MouseLook>(Controller);
            isLocked = !Controller;

            //Interact Lock
            scriptManager.GetScript<InteractManager>().inUse = !Interact;
        }

        //Show Cursor
        ShowCursor(CursorVisible);

        //Blur Levels
        if (BlurLevel > 0)
        {
            if (BlurEnable)
            {
                SetBlur(true, BlurLevel, ResetBlur);
            }
            else
            {
                if (playerLocked)
                {
                    SetBlur(true, oldBlurLevel, true);
                }
                else
                {
                    SetBlur(false, BlurLevel);
                }
            }
        }

        if(ForceLockLevel == 1)
        {
            playerLocked = true;
            oldBlurLevel = BlurLevel;
        }
    }

    private void SetBlur(bool Enable, int BlurLevel, bool Reset = false)
    {
        if (Reset)
        {
            scriptManager.MainCameraBlur.enabled = false;
            scriptManager.ArmsCameraBlur.enabled = false;
        }

        if (BlurLevel == 1) { scriptManager.MainCameraBlur.enabled = Enable; }
        if (BlurLevel == 2) { scriptManager.ArmsCameraBlur.enabled = Enable; }
        if (BlurLevel == 3)
        {
            scriptManager.MainCameraBlur.enabled = Enable;
            scriptManager.ArmsCameraBlur.enabled = Enable;
        }
    }

    public void LockScript<T> (bool state) where T : MonoBehaviour
    {
        if (scriptManager.GetScript<T>())
        {
            scriptManager.GetScript<T>().enabled = state;
        }
        else
        {
            if (scriptManager.gameObject.GetComponent<T>())
            {
                scriptManager.gameObject.GetComponent<T>().enabled = state;
            }
            else
            {
                Debug.LogError("Script " + typeof(T).Name + " cannot be locked");
            }
        }
    }

    public bool IsEnabled<T>() where T : MonoBehaviour
    {
        if (scriptManager.GetScript<T>())
        {
            return scriptManager.GetScript<T>().enabled;
        }
        else
        {
            if (scriptManager.gameObject.GetComponent<T>())
            {
                return scriptManager.gameObject.GetComponent<T>().enabled;
            }
        }

        return false;
    }

    public void UIPreventOverlap(bool State)
    {
        isOverlapping = State;
    }

    public void ShowCursor(bool state)
    {
        switch (state) {
            case true:
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
                break;
            case false:
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
                break;
        }
    }

    public void AddPickupMessage(string itemName)
    {
        GameObject Message = Instantiate(NotificationPrefab, NotificationPanel.transform);
        Notifications.Add(Message);
        Message.GetComponent<Notification>().SetMessage(string.Format("Picked up {0}", itemName));
    }

    public void AddMessage(string message)
    {
        GameObject Message = Instantiate(NotificationPrefab.gameObject, NotificationPanel.transform);
        Notifications.Add(Message);
        Message.GetComponent<Notification>().SetMessage(message);
    }

    public void AddSingleMessage(string message, string id)
    {
        Debug.Log("Single MSG");

        if (Notifications.Count == 0 || Notifications.Count(x => x.GetComponent<Notification>().id == id) == 0)
        {
            GameObject Message = Instantiate(NotificationPrefab.gameObject, NotificationPanel.transform);
            Notifications.Add(Message);
            Message.GetComponent<Notification>().id = id;
            Message.GetComponent<Notification>().SetMessage(message);
        }

        foreach (var item in Notifications)
        {
            Debug.Log(item.GetComponent<Notification>().id);
        }
    }

    public void WarningMessage(string warning)
    {
        GameObject Message = Instantiate(NotificationPrefab, NotificationPanel.transform);
        Notifications.Add(Message);
        Message.GetComponent<Notification>().SetMessage(warning, 3f, WarningSprite);
    }

    public void ShowExamineText(string text)
    {
        ExamineTxt.text = text;
        ExamineNotification.SetActive(true);
        UIFade uIFade = UIFade.CreateInstance(ExamineNotification, "[UIFader] ExamineNotification");
        uIFade.ResetGraphicsColor();
        uIFade.ImageTextAlpha(0.8f, 1f);
        uIFade.fadeOut = false;
        uIFade.FadeInOut(fadeOutSpeed: 3f, fadeOutAfter: UIFade.FadeOutAfter.Bool);
        isExamining = false;
    }

    public void ShowHint(string hint, float time = 3f, InteractiveItem.MessageTip[] messageTips = null)
    {
        HintText.text = hint;

        if(PickupMessages.Count > 0)
        {
            foreach (var item in PickupMessages)
            {
                Destroy(item);
            }
        }

        if(messageTips != null && messageTips.Length > 0)
        {
            foreach (var item in messageTips)
            {
                GameObject obj = Instantiate(HintTipPrefab, HintTipsPanel.transform);
                PickupMessages.Add(obj);
                HintMessageKey hintMessage = obj.GetComponent<HintMessageKey>();
                hintMessage.MessageText.text = item.KeyMessage;
                string input = inputManager.GetInput(item.InputString).ToString();

                if (IsMouseKey(input))
                {
                    hintMessage.MouseKey.GetComponent<Image>().sprite = GetKeySprite(input);
                    hintMessage.NormalKey.SetActive(false);
                    hintMessage.MouseKey.SetActive(true);
                }
                else
                {
                    hintMessage.NormalKey.transform.GetChild(0).GetComponent<Text>().text = input;
                    hintMessage.NormalKey.SetActive(true);
                    hintMessage.MouseKey.SetActive(false);
                }
            }

            HintMessages.SetActive(true);
        }
        else
        {
            HintMessages.SetActive(false);
        }

        UIFade uIFade = UIFade.CreateInstance(HintNotification, "[UIFader] HintNotification");
        uIFade.ResetGraphicsColor();
        uIFade.ImageTextAlpha(0.8f, 1f);
        uIFade.FadeInOut(fadeOutTime: time, fadeOutAfter: UIFade.FadeOutAfter.Time);
        uIFade.onFadeOutEvent += delegate {
            foreach (var item in PickupMessages)
            {
                Destroy(item);
            }
        };
        isExamining = false;
    }

    public void HideExamine()
    {
        UIFade fade = UIFade.FindUIFader(ExamineNotification);

        if(fade != null)
        {
            fade.fadeOut = true;
        }
    }

    public void NewValveSlider(float start, float time)
    {
        ValveSlider.gameObject.SetActive(true);
        StartCoroutine(MoveValveSlide(start, 10f, time));
    }

    public void DisableValveSlider()
    {
        ValveSlider.gameObject.SetActive(false);
        StopCoroutine(MoveValveSlide(0,0,0));
    }

    public IEnumerator MoveValveSlide(float start, float end, float time)
    {
        var currentValue = start;
        var t = 0f;
        while (t < 1)
        {
            t += Time.deltaTime / (time * 10);
            ValveSlider.value = Mathf.Lerp(currentValue, end, t);
            yield return null;
        }
    }

    public void ShowLightPercentagle(float start = 0f, bool fadeIn = true, Sprite icon = null)
    {
        UIFade fade = UIFade.CreateInstance(LightPercentagle, "[UIFader] LightPercentagle");
        lightSlider.value = PercentToValue(start);

        if (icon != null)
        {
            LightPercentagle.transform.GetChild(0).GetComponent<Image>().sprite = icon;
        }
        else
        {
            LightPercentagle.transform.GetChild(0).GetComponent<Image>().sprite = defaultIcon;
        }

        if (fadeIn)
        {
            fade.ResetGraphicsColor();
            fade.SetFadeValues(new UIFade.FadeValue[] { new UIFade.FadeValue(lightBackground.gameObject, 0.7f) });
            fade.FadeInOut(1, 1.5f, 3f, fadeOutAfter: UIFade.FadeOutAfter.Bool);
        }
        else
        {
            fade.fadeOut = true;
        }
    }

    public void UpdateLightPercent(float value)
    {
        float velocity = 0f; 
        lightSlider.value = Mathf.SmoothDamp(lightSlider.value, PercentToValue(value), ref velocity, Time.deltaTime * 5);
    }

    float PercentToValue(float value)
    {
        if(value > 1)
        {
            return value / 100;
        }
        else
        {
            return value;
        }
    }

    public void ShowSaveNotification(float time)
    {
        StartCoroutine(FadeInSave(time));
    }

    IEnumerator FadeInSave(float t)
    {
        SaveNotification.SetActive(true);
        Color color = SaveNotification.GetComponent<Image>().color;

        color.a = 0;
        SaveNotification.GetComponent<Image>().color = color;

        while(SaveNotification.GetComponent<Image>().color.a <= 0.95f)
        {
            color.a += Time.fixedDeltaTime * saveFadeSpeed;
            SaveNotification.GetComponent<Image>().color = color;
            yield return null;
        }

        color.a = 1;
        SaveNotification.GetComponent<Image>().color = color;

        yield return new WaitForSecondsRealtime(t);
        StartCoroutine(FadeOutSave());
    }

    IEnumerator FadeOutSave()
    {
        Color color = SaveNotification.GetComponent<Image>().color;

        while (SaveNotification.GetComponent<Image>().color.a >= 0.1)
        {
            color.a -= Time.fixedDeltaTime * saveFadeSpeed;
            SaveNotification.GetComponent<Image>().color = color;
            yield return null;
        }

        color.a = 0;
        SaveNotification.GetComponent<Image>().color = color;

        SaveNotification.SetActive(false);
    }

    public bool CheckController()
	{
		return Player.GetComponent<PlayerController> ().controllable;
	}

    private void SetKey(Transform KeyObject, KeyCode Key, string customName = "")
    {
        string m_key = Key.ToString();
        KeyObject.gameObject.SetActive(true);

        if (!string.IsNullOrEmpty(customName))
        {
            KeyObject.GetChild(2).GetComponent<Text>().text = customName;
        }

        if (IsMouseKey(m_key))
        {
            KeyObject.GetChild(0).GetComponent<Image>().sprite = GetKeySprite(m_key);
            KeyObject.GetChild(0).gameObject.SetActive(true);
            KeyObject.GetChild(1).gameObject.SetActive(false);
        }
        else
        {
            KeyObject.GetChild(1).GetChild(0).GetComponent<Text>().text = m_key;
            KeyObject.GetChild(0).gameObject.SetActive(false);
            KeyObject.GetChild(1).gameObject.SetActive(true);
        }
    }

    private bool IsMouseKey(string keyString)
    {
        if(keyString == "Mouse0" || keyString == "Mouse1" || keyString == "Mouse2")
        {
            return true;
        }

        return false;
    }

    public void ShowInteractSprite(int Row, string KeyName, KeyCode Key)
    {
        if (isHeld) return;
        InteractUI.SetActive(true);

        switch (Row)
        {
            case 1:
                SetKey(KeyboardButton1.transform, Key, KeyName);
                break;
            case 2:
                SetKey(KeyboardButton2.transform, Key, KeyName);
                break;
        }
    }

    public void ShowInteractInfo(string info)
    {
        InteractInfoUI.SetActive(true);
        InteractInfoUI.GetComponent<Text>().text = info;
    }

    /// <summary>
    /// Show Examine UI Buttons
    /// </summary>
    /// <param name="btn1">PutDown</param>
    /// <param name="btn2">Use</param>
    /// <param name="btn3">Rotate</param>
    /// <param name="btn4">Show Cursor</param>
    public void ShowExamineSprites(bool btn1 = true, bool btn2 = true, bool btn3 = true, bool btn4 = true, string PutDownText = "", string UseText = "Take")
    {
        if (btn1) { SetKey(ExamineButton1.transform, GrabKey, PutDownText); } else { ExamineButton1.gameObject.SetActive(false); }
        if (btn2) { SetKey(ExamineButton2.transform, UseKey, UseText); } else { ExamineButton2.gameObject.SetActive(false); }
        if (btn3) { SetKey(ExamineButton3.transform, RotateKey); } else { ExamineButton3.gameObject.SetActive(false); }
        if (btn4) { SetKey(ExamineButton4.transform, CursorKey); } else { ExamineButton4.gameObject.SetActive(false); }
        DownExamineUI.SetActive(true);
    }

    public void ShowExamineSprites(KeyCode ExamineKey, string ExamineText = "")
    {
        SetKey(ExamineButton1.transform, GrabKey);
        SetKey(ExamineButton2.transform, ExamineKey, ExamineText);
        SetKey(ExamineButton3.transform, RotateKey);
        SetKey(ExamineButton4.transform, CursorKey);
        DownExamineUI.SetActive(true);
    }

    public void ShowGrabSprites()
    {
        SetKey(GrabButton1.transform, GrabKey);
        SetKey(GrabButton2.transform, RotateKey);
        SetKey(GrabButton3.transform, ThrowKey);
        DownGrabUI.SetActive(true);
    }

    public Sprite GetKeySprite(string Key)
    {
        return Resources.Load<Sprite>(Key);
    }

    public void HideSprites(hideType type)
	{
		switch (type) {
            case hideType.Interact:
                KeyboardButton1.SetActive(false);
                KeyboardButton2.SetActive(false);
                InteractInfoUI.SetActive(false);
                InteractUI.SetActive(false);
                break;
            case hideType.Grab:
                DownGrabUI.SetActive(false);
                break;
            case hideType.Examine:
                DownExamineUI.SetActive(false);
                break;
		}
	}

    public void ShowDeadPanel()
    {
        LockPlayerControls(false, false, true);
        scriptManager.GetScript<ItemSwitcher>().DisableItems();

        PauseGamePanel.SetActive(false);
        MainGamePanel.SetActive(false);
        PlayerDeadPanel.SetActive(true);

        uiInteractive = false;
    }

    public void ChangeScene(string SceneName)
    {
        SceneManager.LoadScene(SceneName);
    }

    public void LoadNextScene(string scene)
    {
        if (saveHandler)
        {
            if (saveHandler.dataBetweenScenes)
            {
                saveHandler.SaveNextSceneData();

                if (!isPaused)
                {
                    LockPlayerControls(false, false, false);
                }

                if (saveHandler.fadeControl)
                {
                    saveHandler.fadeControl.FadeIn();
                }

                StartCoroutine(LoadScene(scene, false));
            }
        }
    }

    public void Retry()
    {
        if (saveHandler.fadeControl)
        {
            saveHandler.fadeControl.FadeIn();
        }

        StartCoroutine(LoadScene(SceneManager.GetActiveScene().name, true));
    }

    private IEnumerator LoadScene(string scene, bool LoadSceneData)
    {
        yield return new WaitUntil(() => !saveHandler.fadeControl.isFading);

        PlayerPrefs.SetString("LoadSaveName", GetComponent<SaveGameHandler>().lastSave);
        PlayerPrefs.SetInt("LoadGame", System.Convert.ToInt32(LoadSceneData));
        PlayerPrefs.SetString("LevelToLoad", scene);
        SceneManager.LoadScene(m_sceneLoader);
    }
}