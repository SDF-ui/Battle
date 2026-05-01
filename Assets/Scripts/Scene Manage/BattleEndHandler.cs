using UnityEngine;
using UnityEngine.SceneManagement;

public class BattleEndHandler : MonoBehaviour
{
    // 战斗胜利或结束时调用
    public void OnBattleEnd()
    {
        SceneManager.LoadScene("Demon Tower");
    }

    // 通过UI按钮（如“逃跑”、“返回”）调用
    public void ReturnToDemonTower()
    {
        SceneManager.LoadScene("Demon Tower");
    }
}