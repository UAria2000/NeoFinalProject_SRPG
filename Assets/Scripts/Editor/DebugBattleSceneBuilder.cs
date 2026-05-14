#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class DebugBattleSceneBuilder
{
    private const string SourceScenePath = "Assets/Scenes/WorldMap.unity";
    private const string DebugScenePath = "Assets/Scenes/DebugBattle.unity";

    [MenuItem("Tools/Debug Battle/Create Original Battle Debug Scene")]
    public static void CreateDebugBattleScene()
    {
        Scene scene = EditorSceneManager.OpenScene(SourceScenePath, OpenSceneMode.Single);
        EditorSceneManager.SaveScene(scene, DebugScenePath);
        scene = EditorSceneManager.OpenScene(DebugScenePath, OpenSceneMode.Single);

        GameObject worldMapRoot = FindRootObject("WorldMap");
        GameObject battleSceneRoot = FindRootObject("BattleScene");
        GameObject worldMapStartup = FindRootObject("WorldMapSceneStartup");
        BattleManager battleManager = UnityEngine.Object.FindFirstObjectByType<BattleManager>(FindObjectsInactive.Include);

        if (worldMapRoot != null)
            worldMapRoot.SetActive(false);
        if (worldMapStartup != null)
            worldMapStartup.SetActive(false);
        if (battleSceneRoot != null)
            battleSceneRoot.SetActive(true);

        if (battleManager != null)
        {
            SerializedObject managerSerialized = new SerializedObject(battleManager);
            SerializedProperty autoStart = managerSerialized.FindProperty("autoStartBattleOnStart");
            if (autoStart != null)
                autoStart.boolValue = false;
            managerSerialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(battleManager);
        }

        GameObject controllerObject = GameObject.Find("DebugBattleSceneController");
        if (controllerObject == null)
            controllerObject = new GameObject("DebugBattleSceneController");

        DebugBattleSceneController controller = controllerObject.GetComponent<DebugBattleSceneController>();
        if (controller == null)
            controller = controllerObject.AddComponent<DebugBattleSceneController>();

        SerializedObject serialized = new SerializedObject(controller);
        AssignObject(serialized, "battleManager", battleManager);
        AssignObject(serialized, "battleSceneRoot", battleSceneRoot);
        AssignObject(serialized, "worldMapRoot", worldMapRoot);
        AssignArray(serialized, "allyUnitDefinitions", LoadAssets<UnitDefinition>("Assets/UnitDefinition/Ally"));
        AssignArray(serialized, "enemyUnitDefinitions", LoadAssets<UnitDefinition>("Assets/UnitDefinition/Enemy"));
        AssignArray(serialized, "unitViewDefinitions", LoadAssets<UnitViewDefinition>("Assets/UnitViewDefinition"));
        AssignObject(serialized, "allySkillPoolTable", AssetDatabase.LoadAssetAtPath<SkillLearnPoolTable>("Assets/SkillDefinition/AllySkillTables/AllySkillLearnPoolTable.asset"));
        serialized.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, DebugScenePath);
        AssetDatabase.Refresh();
        Selection.activeObject = controllerObject;
        Debug.Log($"[DebugBattleSceneBuilder] Created original battle debug scene: {DebugScenePath}");
    }

    private static T[] LoadAssets<T>(string folder) where T : UnityEngine.Object
    {
        return AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { folder })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Select(AssetDatabase.LoadAssetAtPath<T>)
            .Where(asset => asset != null)
            .OrderBy(asset => asset.name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static GameObject FindRootObject(string objectName)
    {
        Transform[] transforms = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform t = transforms[i];
            if (t != null && t.parent == null && t.name == objectName)
                return t.gameObject;
        }

        return null;
    }

    private static void AssignObject<T>(SerializedObject serialized, string propertyName, T value) where T : UnityEngine.Object
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property != null)
            property.objectReferenceValue = value;
    }

    private static void AssignArray<T>(SerializedObject serialized, string propertyName, T[] values) where T : UnityEngine.Object
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property == null)
            return;

        property.arraySize = values != null ? values.Length : 0;
        for (int i = 0; i < property.arraySize; i++)
            property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
    }
}
#endif
