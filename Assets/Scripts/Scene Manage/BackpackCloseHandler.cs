using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class BackpackCloseHandler : MonoBehaviour
{
    void Start()
    {
        Button btn = GetComponent<Button>();
        if (btn != null)
            btn.onClick.AddListener(CloseAndReturn);
    }

    public void CloseAndReturn()
    {
        SceneManager.LoadScene("Demon Tower");
    }
}