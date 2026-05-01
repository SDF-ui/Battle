using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneController
{
    public static void LoadDemonTower() => SceneManager.LoadScene("Demon Tower");
    public static void LoadBackpack()   => SceneManager.LoadScene("Backpack");
    public static void LoadBattle()     => SceneManager.LoadScene("Battle");
}