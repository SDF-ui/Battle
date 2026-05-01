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
    public GameObject gameInfoPanel;          // 包含 Info Text 的父物体
    public TMP_Text infoText;                 // Info Text 文本组件

    private enum InfoType { None, Help, About }
    private InfoType currentInfoType = InfoType.None;

    private string helpMessage = "游戏操作说明：\n- 使用鼠标点击选择技能\n- 按侦察键暂停/继续\n- 更多内容请查看游戏内提示";
    private string aboutMessage = "游戏版本 1.0\n© 2026 游戏开发者\n保留所有权利";

    void Start()
    {
        // 初始化按钮监听
        if (startButton != null)
            startButton.onClick.AddListener(OnStart);
        if (helpButton != null)
            helpButton.onClick.AddListener(OnHelp);
        if (aboutButton != null)
            aboutButton.onClick.AddListener(OnAbout);
        if (exitButton != null)
            exitButton.onClick.AddListener(OnExit);

        // 确保信息面板初始隐藏
        if (gameInfoPanel != null)
            gameInfoPanel.SetActive(false);
    }

    // 开始游戏
    void OnStart()
    {
        SceneManager.LoadScene("Demon Tower");
    }

    // 帮助信息
    void OnHelp()
    {
        // 如果当前显示的是帮助信息，则关闭面板
        if (currentInfoType == InfoType.Help)
        {
            gameInfoPanel.SetActive(false);
            currentInfoType = InfoType.None;
        }
        else
        {
            // 否则显示帮助信息
            infoText.text = helpMessage;
            gameInfoPanel.SetActive(true);
            currentInfoType = InfoType.Help;
        }
    }

    // 关于信息（版权）
    void OnAbout()
    {
        if (currentInfoType == InfoType.About)
        {
            gameInfoPanel.SetActive(false);
            currentInfoType = InfoType.None;
        }
        else
        {
            infoText.text = aboutMessage;
            gameInfoPanel.SetActive(true);
            currentInfoType = InfoType.About;
        }
    }

    // 退出游戏
    void OnExit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}