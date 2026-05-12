using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TargetPreviewHoverUI : HoverPopupUIBase
{
    [Header("Root")]
    [SerializeField] private GameObject root;

    [Header("Compact Layout (Recommended)")]
    [SerializeField] private GameObject hitChanceRow;
    [SerializeField] private TMP_Text hitChanceLabelText;
    [SerializeField] private TMP_Text hitChanceValueText;
    [SerializeField] private GameObject statusChanceRow;
    [SerializeField] private TMP_Text statusChanceLabelText;
    [SerializeField] private Image statusChanceIconImage;
    [SerializeField] private TMP_Text statusChanceValueText;

    [Header("Legacy / Optional Texts")]
    [SerializeField] private TMP_Text hitChanceText;
    [SerializeField] private TMP_Text damageRangeText;
    [SerializeField] private TMP_Text successText;

    [Header("Multiple Status Entries")]
    [SerializeField] private Transform statusRoot;
    [SerializeField] private StatusChanceEntryUI statusEntryPrefab;

    [Header("Options")]
    [SerializeField] private bool showDamageRange = false;
    [SerializeField] private string hitChanceLabel = "예상 명중률";
    [SerializeField] private string statusChanceLabel = "상태이상 적중률";

    private readonly List<StatusChanceEntryUI> spawnedEntries = new List<StatusChanceEntryUI>();

    public void Show(TargetPreviewData data, Vector2 pointerScreenPosition)
    {
        if (data == null)
        {
            Hide();
            return;
        }

        ShowRootAt(root, pointerScreenPosition);

        RefreshHitChance(data);
        RefreshDamageAndSuccess(data);
        RefreshStatusChance(data.statusChances);
    }

    public void Hide()
    {
        HideRoot(root);
        ClearStatusEntries();
    }

    private void RefreshHitChance(TargetPreviewData data)
    {
        bool show = data != null && data.showHitChance;

        if (hitChanceRow != null)
            hitChanceRow.SetActive(show);

        if (hitChanceLabelText != null)
            hitChanceLabelText.text = hitChanceLabel;

        if (hitChanceValueText != null)
        {
            hitChanceValueText.gameObject.SetActive(show);
            if (show)
                hitChanceValueText.text = FormatPercent(data.hitChancePercent);
        }

        if (hitChanceText != null)
        {
            hitChanceText.gameObject.SetActive(show);
            if (show)
                hitChanceText.text = string.Format("{0}   {1}", hitChanceLabel, FormatPercent(data.hitChancePercent));
        }
    }

    private void RefreshDamageAndSuccess(TargetPreviewData data)
    {
        if (damageRangeText != null)
        {
            bool show = showDamageRange && data != null && data.showDamageRange;
            damageRangeText.gameObject.SetActive(show);
            if (show)
                damageRangeText.text = $"피해 {data.damageMin}~{data.damageMax}";
        }

        if (successText != null)
        {
            bool show = data != null && data.showSuccessOnly;
            successText.gameObject.SetActive(show);
            if (show)
                successText.text = $"성공률 {data.successPercent}%";
        }
    }

    private void RefreshStatusChance(List<StatusChancePreviewData> statuses)
    {
        ClearStatusEntries();

        bool hasStatus = statuses != null && statuses.Count > 0;

        if (statusChanceRow != null)
            statusChanceRow.SetActive(hasStatus);

        if (statusChanceLabelText != null)
            statusChanceLabelText.text = statusChanceLabel;

        StatusChancePreviewData first = hasStatus ? statuses[0] : null;

        if (statusChanceIconImage != null)
        {
            statusChanceIconImage.gameObject.SetActive(hasStatus && first != null && first.icon != null);
            if (hasStatus && first != null)
                statusChanceIconImage.sprite = first.icon;
        }

        if (statusChanceValueText != null)
        {
            statusChanceValueText.gameObject.SetActive(hasStatus);
            if (hasStatus)
                statusChanceValueText.text = BuildCompactStatusText(statuses);
        }

        if (!hasStatus || statusRoot == null || statusEntryPrefab == null)
            return;

        for (int i = 0; i < statuses.Count; i++)
        {
            StatusChanceEntryUI entry = Instantiate(statusEntryPrefab, statusRoot);
            entry.Bind(statuses[i]);
            spawnedEntries.Add(entry);
        }
    }

    private string BuildCompactStatusText(List<StatusChancePreviewData> statuses)
    {
        if (statuses == null || statuses.Count == 0)
            return string.Empty;

        if (statuses.Count == 1)
            return FormatPercent(statuses[0].successPercent);

        List<string> parts = new List<string>();
        for (int i = 0; i < statuses.Count; i++)
            parts.Add(FormatPercent(statuses[i].successPercent));

        return string.Join(" / ", parts);
    }

    private static string FormatPercent(int value)
    {
        return string.Format("{0}%", Mathf.Clamp(value, 0, 100));
    }

    private void ClearStatusEntries()
    {
        for (int i = 0; i < spawnedEntries.Count; i++)
        {
            if (spawnedEntries[i] != null)
                Destroy(spawnedEntries[i].gameObject);
        }
        spawnedEntries.Clear();
    }
}
