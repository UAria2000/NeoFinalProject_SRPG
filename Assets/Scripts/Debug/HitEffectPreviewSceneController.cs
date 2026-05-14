using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public sealed class HitEffectPreviewSceneController : MonoBehaviour
{
    [Serializable]
    public sealed class PreviewEntry
    {
        public string label;
        public HitEffectType type;
        public GameObject prefab;
        public RectTransform anchor;
    }

    [SerializeField] private PreviewEntry[] entries;
    [SerializeField] private HitEffectRegistry registry;
    [Header("Battle Target Preview")]
    [SerializeField] private RectTransform combatEffectRoot;
    [SerializeField] private RectTransform guardAnchor;
    [SerializeField] private BattleUnitView guardViewPrefab;
    [SerializeField] private UnitDefinition guardUnitDefinition;
    [SerializeField] private UnitViewDefinition guardViewDefinition;
    [SerializeField] private BattleUnitView guardView;
    [SerializeField] private Button resetButton;
    [SerializeField, Min(0.01f)] private float hitReactionDuration = 1f;

    [SerializeField] private float loopInterval = 1.45f;
    [SerializeField, Min(0.01f)] private float galleryPreviewScale = 0.5f;
    [SerializeField, Range(0.1f, 1f)] private float galleryCellFitRatio = 0.55f;
    [SerializeField] private bool staggerPlayback;
    [SerializeField] private Text statusText;

    private readonly List<GameObject> activeInstances = new List<GameObject>();
    private Coroutine loopRoutine;
    private Coroutine focusedRoutine;
    private bool focusedPreviewActive;

    private void OnEnable()
    {
        EnsureEventSystem();
        EnsureGuardView();
        BindEntryButtons();
        BindResetButton();

        if (loopRoutine != null)
            StopCoroutine(loopRoutine);

        ClearActiveInstances();
        loopRoutine = StartCoroutine(LoopEffects());
    }

    private void OnDisable()
    {
        if (loopRoutine != null)
        {
            StopCoroutine(loopRoutine);
            loopRoutine = null;
        }

        if (focusedRoutine != null)
        {
            StopCoroutine(focusedRoutine);
            focusedRoutine = null;
        }

        ClearActiveInstances();
    }

    private IEnumerator LoopEffects()
    {
        WaitForSeconds intervalWait = new WaitForSeconds(loopInterval);
        WaitForSeconds staggerWait = new WaitForSeconds(0.12f);

        while (isActiveAndEnabled)
        {
            int spawnedCount = 0;

            if (!focusedPreviewActive && entries != null)
            {
                ClearActiveInstances();
                ClearLoopInstances();

                for (int i = 0; i < entries.Length; i++)
                {
                    GameObject instance = Spawn(entries[i], registry, out float duration, ResolveLoopParent(entries[i]), GetEntryWorldPosition(entries[i]), true);
                    if (instance != null)
                    {
                        instance.transform.localScale *= galleryPreviewScale;
                        FitGalleryInstanceToCell(instance, entries[i].anchor);
                        MoveCellLabelsToFront(entries[i].anchor);
                        activeInstances.Add(instance);
                        spawnedCount++;
                    }

                    if (staggerPlayback)
                        yield return staggerWait;
                }
            }

            CleanupDestroyedInstances();

            if (statusText != null)
                statusText.text = focusedPreviewActive
                    ? "Focused hit effect preview"
                    : "Looping hit effects: " + spawnedCount;

            yield return intervalWait;
        }
    }

    private void BindEntryButtons()
    {
        if (entries == null)
            return;

        for (int i = 0; i < entries.Length; i++)
        {
            PreviewEntry entry = entries[i];
            if (entry == null || entry.anchor == null)
                continue;

            EnsureCellMask(entry.anchor);

            Button button = entry.anchor.GetComponent<Button>();
            if (button == null)
                button = entry.anchor.gameObject.AddComponent<Button>();

            Graphic graphic = entry.anchor.GetComponent<Graphic>();
            if (graphic == null)
            {
                Image image = entry.anchor.gameObject.AddComponent<Image>();
                image.color = new Color(1f, 1f, 1f, 0.01f);
                graphic = image;
            }

            graphic.raycastTarget = true;
            button.targetGraphic = graphic;
            button.onClick.RemoveAllListeners();

            PreviewEntry captured = entry;
            button.onClick.AddListener(() => PlayFocusedPreview(captured));
        }
    }

    private void BindResetButton()
    {
        if (resetButton == null)
            return;

        resetButton.onClick.RemoveAllListeners();
        resetButton.onClick.AddListener(ResetFocusedPreview);
    }

    private void PlayFocusedPreview(PreviewEntry entry)
    {
        if (!isActiveAndEnabled || entry == null)
            return;

        focusedPreviewActive = true;
        ClearActiveInstances();

        if (focusedRoutine != null)
            StopCoroutine(focusedRoutine);

        focusedRoutine = StartCoroutine(FocusedPreviewRoutine(entry));
    }

    private IEnumerator FocusedPreviewRoutine(PreviewEntry entry)
    {
        EnsureGuardView();

        Transform parent = combatEffectRoot != null ? combatEffectRoot : transform;
        if (combatEffectRoot != null)
            combatEffectRoot.SetAsLastSibling();

        Vector3 worldPosition = GetGuardHitEffectPosition(entry, out float duration);
        GameObject instance = Spawn(entry, registry, out duration, parent, worldPosition, false);
        if (instance != null)
        {
            activeInstances.Add(instance);
            Destroy(instance, duration);
        }

        if (guardView != null)
            yield return StartCoroutine(guardView.PlayHitReaction(hitReactionDuration));

        focusedRoutine = null;
    }

    private void ResetFocusedPreview()
    {
        focusedPreviewActive = false;
        if (focusedRoutine != null)
        {
            StopCoroutine(focusedRoutine);
            focusedRoutine = null;
        }

        ClearActiveInstances();
        ClearLoopInstances();
        RecreateGuardView();
        EnsureGuardView();

        if (loopRoutine != null)
            StopCoroutine(loopRoutine);

        loopRoutine = StartCoroutine(LoopEffects());
    }

    private Vector3 GetGuardHitEffectPosition(PreviewEntry entry, out float duration)
    {
        duration = 2f;
        HitEffectAnchorType anchorType = HitEffectAnchorType.Default;
        Vector2 offset = Vector2.zero;

        if (registry != null && entry != null && entry.type != HitEffectType.None && registry.TryGetEntry(entry.type, out HitEffectRegistry.Entry registryEntry))
        {
            duration = registryEntry.duration;
            anchorType = registryEntry.anchorType;
            offset = registryEntry.offset;
        }

        if (guardView != null)
            return guardView.GetHitEffectAnchorPosition(anchorType, offset);

        RectTransform fallback = guardAnchor != null ? guardAnchor : null;
        if (fallback == null)
            return transform.position;

        Vector3[] corners = new Vector3[4];
        fallback.GetWorldCorners(corners);
        Vector3 bottomCenter = (corners[0] + corners[3]) * 0.5f;
        Vector3 topCenter = (corners[1] + corners[2]) * 0.5f;
        Vector3 anchor = Vector3.Lerp(bottomCenter, topCenter, 0.62f);
        anchor += fallback.TransformVector(offset);
        return anchor;
    }

    private Transform ResolveLoopParent(PreviewEntry entry)
    {
        if (entry == null || entry.anchor == null)
            return transform;

        return entry.anchor;
    }

    private Vector3 GetEntryWorldPosition(PreviewEntry entry)
    {
        return entry != null && entry.anchor != null ? entry.anchor.position : transform.position;
    }

    private void EnsureGuardView()
    {
        if (guardView == null && guardViewPrefab != null && guardAnchor != null)
        {
            guardView = Instantiate(guardViewPrefab, guardAnchor.parent != null ? guardAnchor.parent : guardAnchor);
            guardView.name = "GuardReference_BattleUnitView";
            RectTransform rect = guardView.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchorMin = guardAnchor.anchorMin;
                rect.anchorMax = guardAnchor.anchorMax;
                rect.pivot = guardAnchor.pivot;
                rect.anchoredPosition = guardAnchor.anchoredPosition;
                rect.sizeDelta = guardAnchor.sizeDelta;
                rect.localScale = guardAnchor.localScale;
                rect.localRotation = guardAnchor.localRotation;
                rect.SetAsLastSibling();
            }
        }

        if (guardView == null || guardUnitDefinition == null || guardViewDefinition == null)
            return;

        PartyMemberData data = new PartyMemberData
        {
            unitDefinition = guardUnitDefinition,
            unitViewDefinition = guardViewDefinition,
            startSlotIndex = 0,
            instanceId = "hit_effect_preview_guard",
            persistentCurrentHP = -1
        };

        BattleUnit unit = new BattleUnit(data, TeamType.Enemy);
        guardView.Initialize(unit, "경비병");
    }

    private void RecreateGuardView()
    {
        if (guardView == null)
            return;

        guardView.gameObject.SetActive(false);
        Destroy(guardView.gameObject);
        guardView = null;
    }

    private static void EnsureEventSystem()
    {
        if (EventSystem.current != null)
            return;

        GameObject go = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        DontDestroyOnLoad(go);
    }

    private static GameObject Spawn(PreviewEntry entry, HitEffectRegistry registry, out float duration, Transform parent, Vector3 worldPosition, bool applyRegistryOffset)
    {
        duration = 2f;

        if (entry == null)
            return null;

        GameObject prefab = entry.prefab;
        Vector2 offset = Vector2.zero;

        if (registry != null && entry.type != HitEffectType.None && registry.TryGetEntry(entry.type, out HitEffectRegistry.Entry registryEntry))
        {
            prefab = registryEntry.prefab;
            duration = registryEntry.duration;
            offset = registryEntry.offset;
        }

        if (prefab == null)
            return null;

        GameObject instance = Instantiate(prefab, parent);
        instance.transform.position = applyRegistryOffset
            ? worldPosition + new Vector3(offset.x, offset.y, 0f)
            : worldPosition;

        if (instance.TryGetComponent(out BattleRichHitEffectUI richEffect))
        {
            duration = richEffect.Duration;
            RectTransform rect = instance.GetComponent<RectTransform>();
            if (rect != null)
                rect.anchoredPosition += richEffect.SpawnOffset;
            else
                instance.transform.position += new Vector3(richEffect.SpawnOffset.x, richEffect.SpawnOffset.y, 0f);
        }

        instance.name = entry.label + "_Preview";
        return instance;
    }

    private static void EnsureCellMask(RectTransform cell)
    {
        if (cell == null)
            return;

        if (cell.GetComponent<RectMask2D>() == null)
            cell.gameObject.AddComponent<RectMask2D>();
    }

    private static void MoveCellLabelsToFront(RectTransform cell)
    {
        if (cell == null)
            return;

        Text[] labels = cell.GetComponentsInChildren<Text>(true);
        for (int i = 0; i < labels.Length; i++)
        {
            if (labels[i] != null)
                labels[i].transform.SetAsLastSibling();
        }
    }

    private void FitGalleryInstanceToCell(GameObject instance, RectTransform cell)
    {
        if (instance == null || cell == null)
            return;

        if (!TryGetRectWorldBounds(cell, out Bounds cellBounds) || !TryGetChildrenRectWorldBounds(instance.transform, out Bounds effectBounds))
            return;

        float maxWidth = Mathf.Max(0.01f, cellBounds.size.x * galleryCellFitRatio);
        float maxHeight = Mathf.Max(0.01f, cellBounds.size.y * galleryCellFitRatio);
        float widthRatio = maxWidth / Mathf.Max(0.01f, effectBounds.size.x);
        float heightRatio = maxHeight / Mathf.Max(0.01f, effectBounds.size.y);
        float fitRatio = Mathf.Min(1f, widthRatio, heightRatio);

        if (fitRatio < 0.999f)
        {
            instance.transform.localScale *= fitRatio;
            if (!TryGetChildrenRectWorldBounds(instance.transform, out effectBounds))
                return;
        }

        Vector3 correction = cellBounds.center - effectBounds.center;
        correction.z = 0f;
        instance.transform.position += correction;
    }

    private static bool TryGetRectWorldBounds(RectTransform rect, out Bounds bounds)
    {
        Vector3[] corners = new Vector3[4];
        rect.GetWorldCorners(corners);
        bounds = new Bounds(corners[0], Vector3.zero);
        for (int i = 1; i < corners.Length; i++)
            bounds.Encapsulate(corners[i]);

        return true;
    }

    private static bool TryGetChildrenRectWorldBounds(Transform root, out Bounds bounds)
    {
        RectTransform[] rects = root.GetComponentsInChildren<RectTransform>(true);
        Vector3[] corners = new Vector3[4];
        bool hasBounds = false;
        bounds = new Bounds(root.position, Vector3.zero);

        for (int i = 0; i < rects.Length; i++)
        {
            RectTransform rect = rects[i];
            if (rect == root)
                continue;

            rect.GetWorldCorners(corners);
            for (int j = 0; j < corners.Length; j++)
            {
                if (!hasBounds)
                {
                    bounds = new Bounds(corners[j], Vector3.zero);
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(corners[j]);
                }
            }
        }

        return hasBounds;
    }

    private void CleanupDestroyedInstances()
    {
        for (int i = activeInstances.Count - 1; i >= 0; i--)
        {
            if (activeInstances[i] == null)
                activeInstances.RemoveAt(i);
        }
    }

    private void ClearLoopInstances()
    {
        if (entries == null)
            return;

        HashSet<Transform> loopParents = new HashSet<Transform>();
        for (int i = 0; i < entries.Length; i++)
        {
            Transform parent = ResolveLoopParent(entries[i]);
            if (parent != null)
                loopParents.Add(parent);
        }

        foreach (Transform parent in loopParents)
        {
            BattleRichHitEffectUI[] effects = parent.GetComponentsInChildren<BattleRichHitEffectUI>(false);
            for (int i = 0; i < effects.Length; i++)
            {
                if (effects[i] == null)
                    continue;

                effects[i].gameObject.SetActive(false);
                Destroy(effects[i].gameObject);
            }
        }
    }

    private void ClearActiveInstances()
    {
        for (int i = activeInstances.Count - 1; i >= 0; i--)
        {
            if (activeInstances[i] != null)
            {
                activeInstances[i].SetActive(false);
                Destroy(activeInstances[i]);
            }
        }

        activeInstances.Clear();
    }
}
