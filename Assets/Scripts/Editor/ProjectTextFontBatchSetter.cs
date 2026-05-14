#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Project-wide font batch setter for TMP_Text and legacy UnityEngine.UI.Text.
/// Put this file under an Editor folder.
/// Menu: Tools > UI > Project Text Font Batch Setter
/// </summary>
public sealed class ProjectTextFontBatchSetter : EditorWindow
{
    private TMP_FontAsset targetTmpFont;
    private Font targetLegacyFont;

    private bool processOpenScene = true;
    private bool processAllPrefabs = true;
    private bool processAllScenesInAssets = false;

    private bool includeInactive = true;
    private bool resetTmpMaterialToFontDefault = true;
    private bool onlyReplaceWhenFontIsMissingOrDefault = false;

    private Vector2 scroll;
    private readonly List<string> logLines = new();

    [MenuItem("Tools/UI/Project Text Font Batch Setter")]
    public static void Open()
    {
        GetWindow<ProjectTextFontBatchSetter>("Font Batch Setter");
    }

    private void OnGUI()
    {
        scroll = EditorGUILayout.BeginScrollView(scroll);

        EditorGUILayout.LabelField("Target Fonts", EditorStyles.boldLabel);
        targetTmpFont = (TMP_FontAsset)EditorGUILayout.ObjectField(
            "TMP Font Asset",
            targetTmpFont,
            typeof(TMP_FontAsset),
            false);

        targetLegacyFont = (Font)EditorGUILayout.ObjectField(
            "Legacy UI Font",
            targetLegacyFont,
            typeof(Font),
            false);

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Scope", EditorStyles.boldLabel);
        processOpenScene = EditorGUILayout.ToggleLeft("Apply to currently open scene", processOpenScene);
        processAllPrefabs = EditorGUILayout.ToggleLeft("Apply to all prefabs under Assets", processAllPrefabs);
        processAllScenesInAssets = EditorGUILayout.ToggleLeft("Apply to all scenes under Assets", processAllScenesInAssets);

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Options", EditorStyles.boldLabel);
        includeInactive = EditorGUILayout.ToggleLeft("Include inactive objects", includeInactive);
        resetTmpMaterialToFontDefault = EditorGUILayout.ToggleLeft("Reset TMP material to selected font default material", resetTmpMaterialToFontDefault);
        onlyReplaceWhenFontIsMissingOrDefault = EditorGUILayout.ToggleLeft("Only replace missing/default-looking fonts", onlyReplaceWhenFontIsMissingOrDefault);

        EditorGUILayout.Space(8);

        using (new EditorGUI.DisabledScope(targetTmpFont == null && targetLegacyFont == null))
        {
            if (GUILayout.Button("Apply Fonts", GUILayout.Height(36)))
            {
                Apply();
            }
        }

        EditorGUILayout.Space(8);

        if (logLines.Count > 0)
        {
            EditorGUILayout.LabelField("Log", EditorStyles.boldLabel);
            foreach (string line in logLines)
            {
                EditorGUILayout.HelpBox(line, MessageType.None);
            }
        }

        EditorGUILayout.EndScrollView();
    }

    private void Apply()
    {
        logLines.Clear();

        if (targetTmpFont == null && targetLegacyFont == null)
        {
            EditorUtility.DisplayDialog("Font Batch Setter", "TMP Font Asset 또는 Legacy UI Font 중 하나 이상을 지정해야 합니다.", "OK");
            return;
        }

        if ((processOpenScene || processAllScenesInAssets) && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            Log("사용자가 현재 씬 저장을 취소해서 작업을 중단했습니다.");
            return;
        }

        int totalTmp = 0;
        int totalLegacy = 0;
        int totalAssets = 0;

        try
        {
            AssetDatabase.StartAssetEditing();

            if (processOpenScene)
            {
                int tmp;
                int legacy;
                ApplyToOpenScene(out tmp, out legacy);
                totalTmp += tmp;
                totalLegacy += legacy;
                totalAssets += 1;
            }

            if (processAllPrefabs)
            {
                int tmp;
                int legacy;
                int assets;
                ApplyToAllPrefabs(out tmp, out legacy, out assets);
                totalTmp += tmp;
                totalLegacy += legacy;
                totalAssets += assets;
            }

            if (processAllScenesInAssets)
            {
                int tmp;
                int legacy;
                int assets;
                ApplyToAllScenes(out tmp, out legacy, out assets);
                totalTmp += tmp;
                totalLegacy += legacy;
                totalAssets += assets;
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        string summary = $"완료: 처리 대상 {totalAssets}개, TMP_Text {totalTmp}개, Legacy Text {totalLegacy}개 변경.";
        Log(summary);
        EditorUtility.DisplayDialog("Font Batch Setter", summary, "OK");
    }

    private void ApplyToOpenScene(out int tmpChanged, out int legacyChanged)
    {
        tmpChanged = 0;
        legacyChanged = 0;

        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid())
        {
            Log("현재 열린 씬이 유효하지 않습니다.");
            return;
        }

        bool changed = false;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            changed |= ApplyToHierarchy(root, true, out int tmp, out int legacy);
            tmpChanged += tmp;
            legacyChanged += legacy;
        }

        if (changed)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        Log($"Open Scene: {scene.name} / TMP {tmpChanged}, Legacy {legacyChanged}");
    }

    private void ApplyToAllPrefabs(out int tmpChanged, out int legacyChanged, out int prefabCount)
    {
        tmpChanged = 0;
        legacyChanged = 0;
        prefabCount = 0;

        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" });
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            EditorUtility.DisplayProgressBar("Applying fonts to prefabs", path, guids.Length == 0 ? 1f : i / (float)guids.Length);

            GameObject prefabRoot = null;
            try
            {
                prefabRoot = PrefabUtility.LoadPrefabContents(path);
                bool changed = ApplyToHierarchy(prefabRoot, false, out int tmp, out int legacy);
                if (changed)
                {
                    PrefabUtility.SaveAsPrefabAsset(prefabRoot, path);
                    prefabCount += 1;
                    tmpChanged += tmp;
                    legacyChanged += legacy;
                }
            }
            catch (System.Exception ex)
            {
                Log($"Prefab 처리 실패: {path}\n{ex.Message}");
            }
            finally
            {
                if (prefabRoot != null)
                    PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        EditorUtility.ClearProgressBar();
        Log($"Prefabs: changed assets {prefabCount}, TMP {tmpChanged}, Legacy {legacyChanged}");
    }

    private void ApplyToAllScenes(out int tmpChanged, out int legacyChanged, out int sceneCount)
    {
        tmpChanged = 0;
        legacyChanged = 0;
        sceneCount = 0;

        string originalScenePath = SceneManager.GetActiveScene().path;
        string[] guids = AssetDatabase.FindAssets("t:Scene", new[] { "Assets" });

        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            EditorUtility.DisplayProgressBar("Applying fonts to scenes", path, guids.Length == 0 ? 1f : i / (float)guids.Length);

            try
            {
                Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                bool changed = false;
                int sceneTmp = 0;
                int sceneLegacy = 0;

                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    changed |= ApplyToHierarchy(root, false, out int tmp, out int legacy);
                    sceneTmp += tmp;
                    sceneLegacy += legacy;
                }

                if (changed)
                {
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                    sceneCount += 1;
                    tmpChanged += sceneTmp;
                    legacyChanged += sceneLegacy;
                }
            }
            catch (System.Exception ex)
            {
                Log($"Scene 처리 실패: {path}\n{ex.Message}");
            }
        }

        if (!string.IsNullOrEmpty(originalScenePath) && File.Exists(originalScenePath))
            EditorSceneManager.OpenScene(originalScenePath, OpenSceneMode.Single);

        EditorUtility.ClearProgressBar();
        Log($"Scenes: changed assets {sceneCount}, TMP {tmpChanged}, Legacy {legacyChanged}");
    }

    private bool ApplyToHierarchy(GameObject root, bool useUndo, out int tmpChanged, out int legacyChanged)
    {
        tmpChanged = 0;
        legacyChanged = 0;
        bool changed = false;

        if (targetTmpFont != null)
        {
            TMP_Text[] tmpTexts = root.GetComponentsInChildren<TMP_Text>(includeInactive);
            foreach (TMP_Text text in tmpTexts)
            {
                if (text == null)
                    continue;

                if (onlyReplaceWhenFontIsMissingOrDefault && !ShouldReplaceTmp(text))
                    continue;

                if (text.font == targetTmpFont && (!resetTmpMaterialToFontDefault || text.fontSharedMaterial == targetTmpFont.material))
                    continue;

                if (useUndo)
                    Undo.RecordObject(text, "Apply TMP Font");

                text.font = targetTmpFont;

                if (resetTmpMaterialToFontDefault)
                    text.fontSharedMaterial = targetTmpFont.material;

                RefreshTmpText(text);
                EditorUtility.SetDirty(text);
                tmpChanged += 1;
                changed = true;
            }
        }

        if (targetLegacyFont != null)
        {
            Text[] legacyTexts = root.GetComponentsInChildren<Text>(includeInactive);
            foreach (Text text in legacyTexts)
            {
                if (text == null)
                    continue;

                if (onlyReplaceWhenFontIsMissingOrDefault && !ShouldReplaceLegacy(text))
                    continue;

                if (text.font == targetLegacyFont)
                    continue;

                if (useUndo)
                    Undo.RecordObject(text, "Apply Legacy Font");

                text.font = targetLegacyFont;
                EditorUtility.SetDirty(text);
                legacyChanged += 1;
                changed = true;
            }
        }

        return changed;
    }

    private static bool ShouldReplaceTmp(TMP_Text text)
    {
        if (text.font == null)
            return true;

        string fontName = text.font.name.ToLowerInvariant();
        return fontName.Contains("liberation") ||
               fontName.Contains("arial") ||
               fontName.Contains("default") ||
               fontName.Contains("fallback");
    }

    private static bool ShouldReplaceLegacy(Text text)
    {
        if (text.font == null)
            return true;

        string fontName = text.font.name.ToLowerInvariant();
        return fontName.Contains("arial") ||
               fontName.Contains("default") ||
               fontName.Contains("legacyruntime");
    }

    private static void RefreshTmpText(TMP_Text text)
    {
        if (text == null)
            return;

        text.SetAllDirty();

        if (text.gameObject.activeInHierarchy)
            text.ForceMeshUpdate();
    }

    private void Log(string message)
    {
        logLines.Add(message);
        Debug.Log($"[ProjectTextFontBatchSetter] {message}");
    }
}
#endif
