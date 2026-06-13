using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [Header("Buttons")]
    public Button startButton;
    public Button helpButton;
    public Button aboutButton;
    public Button exitButton;

    [Header("Info Panel")]
    public GameObject gameInfoPanel;          // Parent object containing the Info Text
    public TMP_Text infoText;                 // Info Text component

    private enum InfoType { None, Help, About }
    private InfoType currentInfoType = InfoType.None;

    private string helpMessage = "游戏操作说明：\n- 使用鼠标点击选择技能\n- 按侦察键暂停/继续\n- 更多内容请查看游戏内提示";
    private string aboutMessage = "游戏版本 1.0\n© 2026 游戏开发者\n保留所有权利";

    void Start()
    {
        // Initialize button listeners
        if (startButton != null)
            startButton.onClick.AddListener(OnStart);
        if (helpButton != null)
            helpButton.onClick.AddListener(OnHelp);
        if (aboutButton != null)
            aboutButton.onClick.AddListener(OnAbout);
        if (exitButton != null)
            exitButton.onClick.AddListener(OnExit);

        // Ensure info panel is initially hidden
        if (gameInfoPanel != null)
            gameInfoPanel.SetActive(false);
    }

    // Start game
    public void OnStart()
    {
        SceneController.LoadDemonTower();
    }

    // Help info
    public void OnHelp()
    {
        // If help info is currently displayed, close the panel
        if (currentInfoType == InfoType.Help)
        {
            gameInfoPanel.SetActive(false);
            currentInfoType = InfoType.None;
            return;
        }

        // Otherwise show help info
        gameInfoPanel.SetActive(true);
        infoText.text = helpMessage;
        currentInfoType = InfoType.Help;
    }

    // About info (copyright)
    public void OnAbout()
    {
        if (currentInfoType == InfoType.About)
        {
            gameInfoPanel.SetActive(false);
            currentInfoType = InfoType.None;
            return;
        }

        gameInfoPanel.SetActive(true);
        infoText.text = aboutMessage;
        currentInfoType = InfoType.About;
    }

    // Exit game
    public void OnExit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}