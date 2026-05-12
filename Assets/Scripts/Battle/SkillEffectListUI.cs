using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class SkillEffectListUI : MonoBehaviour
{
    [SerializeField] private RectTransform container;
    [SerializeField] private SkillEffectEntryUI entryPrefab;

    private readonly List<SkillEffectEntryUI> pool = new List<SkillEffectEntryUI>();

    private void Awake()
    {
        AutoWireIfNeeded();
        HideAll();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        AutoWireIfNeeded();
    }
#endif

    public void Show(SkillDefinition skill)
    {
        AutoWireIfNeeded();
        List<SkillEffectDisplayEntry> entries = BattleSkillInfoFormatter.GetUnifiedEffectEntries(skill);
        EnsurePool(entries.Count);

        for (int i = 0; i < pool.Count; i++)
        {
            SkillEffectEntryUI item = pool[i];
            if (item == null)
                continue;

            if (i < entries.Count)
                item.Set(entries[i].icon, entries[i].text);
            else
                item.Clear();
        }

        gameObject.SetActive(entries.Count > 0);
    }

    public void HideAll()
    {
        for (int i = 0; i < pool.Count; i++)
        {
            if (pool[i] != null)
                pool[i].Clear();
        }

        gameObject.SetActive(false);
    }

    private void EnsurePool(int count)
    {
        if (entryPrefab == null || container == null)
            return;

        while (pool.Count < count)
        {
            SkillEffectEntryUI entry = Instantiate(entryPrefab, container);
            entry.gameObject.SetActive(false);
            pool.Add(entry);
        }
    }

    [ContextMenu("Auto Wire From Children")]
    public void AutoWireIfNeeded()
    {
        if (container == null)
            container = transform as RectTransform;

        if (entryPrefab == null)
            entryPrefab = GetComponentInChildren<SkillEffectEntryUI>(true);

        if (entryPrefab != null && !pool.Contains(entryPrefab))
            entryPrefab.gameObject.SetActive(false);
    }
}
