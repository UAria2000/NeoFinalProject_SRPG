using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Optional runtime font applier.
/// Use this when UI objects are spawned dynamically after the editor batch pass.
/// Place it on a persistent object such as Bootstrap or RuntimeUI.
/// </summary>
public sealed class GlobalRuntimeFontApplier : MonoBehaviour
{
    [Header("Target Fonts")]
    [SerializeField] private TMP_FontAsset targetTmpFont;
    [SerializeField] private Font targetLegacyFont;

    [Header("Apply Timing")]
    [SerializeField] private bool dontDestroyOnLoad = true;
    [SerializeField] private bool applyOnAwake = true;
    [SerializeField] private bool applyOnSceneLoaded = true;

    [Tooltip("0이면 반복 스캔하지 않습니다. 동적 UI가 자주 생성된다면 0.5~1.0 정도를 사용하세요.")]
    [SerializeField] private float periodicRescanInterval = 0f;

    [Header("Options")]
    [SerializeField] private bool includeInactive = true;
    [SerializeField] private bool resetTmpMaterialToFontDefault = true;

    private Coroutine periodicRoutine;

    private void Awake()
    {
        if (dontDestroyOnLoad)
            DontDestroyOnLoad(gameObject);

        if (applyOnAwake)
            ApplyToLoadedScenes();

        if (periodicRescanInterval > 0f)
            periodicRoutine = StartCoroutine(PeriodicRescan());
    }

    private void OnEnable()
    {
        if (applyOnSceneLoaded)
            SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        if (applyOnSceneLoaded)
            SceneManager.sceneLoaded -= OnSceneLoaded;

        if (periodicRoutine != null)
        {
            StopCoroutine(periodicRoutine);
            periodicRoutine = null;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplyToScene(scene);
    }

    private IEnumerator PeriodicRescan()
    {
        WaitForSeconds wait = new WaitForSeconds(periodicRescanInterval);
        while (true)
        {
            yield return wait;
            ApplyToLoadedScenes();
        }
    }

    [ContextMenu("Apply To Loaded Scenes Now")]
    public void ApplyToLoadedScenes()
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
            ApplyToScene(SceneManager.GetSceneAt(i));
    }

    public void ApplyToScene(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded)
            return;

        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
            ApplyToHierarchy(roots[i]);
    }

    public void ApplyToHierarchy(GameObject root)
    {
        if (root == null)
            return;

        if (targetTmpFont != null)
        {
            TMP_Text[] tmpTexts = root.GetComponentsInChildren<TMP_Text>(includeInactive);
            for (int i = 0; i < tmpTexts.Length; i++)
            {
                TMP_Text text = tmpTexts[i];
                if (text == null)
                    continue;

                if (text.font != targetTmpFont)
                    text.font = targetTmpFont;

                if (resetTmpMaterialToFontDefault && text.fontSharedMaterial != targetTmpFont.material)
                    text.fontSharedMaterial = targetTmpFont.material;

                RefreshTmpText(text);
            }
        }

        if (targetLegacyFont != null)
        {
            Text[] legacyTexts = root.GetComponentsInChildren<Text>(includeInactive);
            for (int i = 0; i < legacyTexts.Length; i++)
            {
                Text text = legacyTexts[i];
                if (text == null)
                    continue;

                if (text.font != targetLegacyFont)
                    text.font = targetLegacyFont;
            }
        }
    }

    private static void RefreshTmpText(TMP_Text text)
    {
        if (text == null)
            return;

        text.SetAllDirty();

        if (text.gameObject.activeInHierarchy)
            text.ForceMeshUpdate();
    }

    public void SetTmpFont(TMP_FontAsset fontAsset, bool applyImmediately = true)
    {
        targetTmpFont = fontAsset;
        if (applyImmediately)
            ApplyToLoadedScenes();
    }

    public void SetLegacyFont(Font font, bool applyImmediately = true)
    {
        targetLegacyFont = font;
        if (applyImmediately)
            ApplyToLoadedScenes();
    }
}
