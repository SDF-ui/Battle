using UnityEngine;

public static class CharacterPrefabDB
{
    private const string PlayerPrefabPath = "Characters/Player";
    private const string EnemyBasePath = "Characters/Enemy_";

    public static GameObject GetPlayerPrefab()
    {
        return Resources.Load<GameObject>(PlayerPrefabPath);
    }

    public static GameObject GetEnemyPrefab(string enemyType)
    {
        return Resources.Load<GameObject>(EnemyBasePath + enemyType);
    }
}