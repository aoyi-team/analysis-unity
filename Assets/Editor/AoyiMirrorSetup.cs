using UnityEditor;
using UnityEngine;
using Mirror;
using Aoyi.Mirror;

/// <summary>
/// Mirror Prefab 自动生成工具。
/// 使用方法：点击菜单 Tools > 奥义 > 生成 Mirror Prefab
/// 会自动在 Resources/MirrorPrefabs/ 下创建 RoomPlayer 和 GamePlayer prefab。
/// </summary>
public class AoyiMirrorSetup : Editor
{
    [MenuItem("Tools/奥义/生成 Mirror Prefab")]
    public static void GenerateMirrorPrefabs()
    {
        // 确保 Resources 目录存在
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder("Assets/Resources/MirrorPrefabs"))
            AssetDatabase.CreateFolder("Assets/Resources", "MirrorPrefabs");

        // 创建 RoomPlayer prefab
        GameObject roomPlayerObj = new GameObject("AoyiRoomPlayerPrefab");
        roomPlayerObj.AddComponent<NetworkIdentity>();
        roomPlayerObj.AddComponent<AoyiRoomPlayer>();
        PrefabUtility.SaveAsPrefabAsset(roomPlayerObj, "Assets/Resources/MirrorPrefabs/AoyiRoomPlayerPrefab.prefab");
        DestroyImmediate(roomPlayerObj);

        // 创建 GamePlayer prefab（空对象，Plan A 由 PlayerManager 本地生成玩家）
        GameObject gamePlayerObj = new GameObject("AoyiGamePlayerPrefab");
        gamePlayerObj.AddComponent<NetworkIdentity>();
        PrefabUtility.SaveAsPrefabAsset(gamePlayerObj, "Assets/Resources/MirrorPrefabs/AoyiGamePlayerPrefab.prefab");
        DestroyImmediate(gamePlayerObj);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[AoyiMirrorSetup] Mirror prefab 已生成到 Resources/MirrorPrefabs/");
    }
}
