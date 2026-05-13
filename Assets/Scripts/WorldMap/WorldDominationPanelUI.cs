using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WorldDominationPanelUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private WorldRunManager runManager;
    [SerializeField] private WorldGenerationSettings generationSettings;

    [Header("Row Prefab Setup")]
    [SerializeField] private Transform rowRoot;
    [SerializeField] private WorldDominationConditionRowUI rowPrefab;
    [SerializeField] private int maxRows = 3;

    [Header("Conquest Button")]
    [SerializeField] private GameObject conquestButtonRoot;
    [SerializeField] private Button conquestButton;

    private readonly List<WorldDominationConditionRowUI> rowInstances = new List<WorldDominationConditionRowUI>();

    private void Awake()
    {
        ResolveReferences();
        BindButton();
        EnsureRows();
    }

    private void OnEnable()
    {
        ResolveReferences();

        if (runManager != null)
        {
            runManager.OnWorldStateChanged += RefreshUI;
            runManager.OnCurrentTileChanged += HandleCurrentTileChanged;
        }

        EnsureRows();
        RefreshUI();
    }

    private void OnDisable()
    {
        if (runManager != null)
        {
            runManager.OnWorldStateChanged -= RefreshUI;
            runManager.OnCurrentTileChanged -= HandleCurrentTileChanged;
        }
    }

    private void ResolveReferences()
    {
        if (runManager == null)
            runManager = Object.FindFirstObjectByType<WorldRunManager>();

        if (runManager != null)
            generationSettings = runManager.Settings;
    }

    private void BindButton()
    {
        if (conquestButton == null)
            return;

        conquestButton.onClick.RemoveListener(HandleConquestButtonClicked);
        conquestButton.onClick.AddListener(HandleConquestButtonClicked);
    }

    private void HandleCurrentTileChanged(WorldTileData _)
    {
        RefreshUI();
    }

    private void EnsureRows()
    {
        if (rowRoot == null || rowPrefab == null)
            return;

        rowInstances.RemoveAll(r => r == null);

        while (rowInstances.Count < Mathf.Max(1, maxRows))
        {
            WorldDominationConditionRowUI instance = Instantiate(rowPrefab, rowRoot);
            instance.name = $"DominationRow_{rowInstances.Count}";
            rowInstances.Add(instance);
        }
    }

    public void RefreshUI()
    {
        ResolveReferences();

        if (runManager == null || generationSettings == null || runManager.MapData == null)
        {
            BindEmpty();
            return;
        }

        EnsureRows();

        CountWorldProgress(
            out int totalBossTiles,
            out int conqueredBossTiles,
            out int nonStartTiles,
            out int conqueredTiles);

        int requiredPercent = generationSettings.GetConquestRequiredPercent();
        int currentPercent = nonStartTiles > 0
            ? Mathf.FloorToInt((conqueredTiles / (float)nonStartTiles) * 100f)
            : 0;

        bool conquestCompleted = currentPercent >= requiredPercent;

        if (runManager.IsTutorialWorld)
        {
            // Tutorial domination condition is intentionally simpler than normal worlds.
            // It has no boss requirement: only conquer every non-start tile.
            string tutorialText = $"전체 지역의 100% 점령 달성 ({conqueredTiles}/{nonStartTiles})";
            BindRow(0, true, conquestCompleted, tutorialText);
            HideRowsFrom(1);
            SetConquestButtonVisible(conquestCompleted);
            return;
        }

        bool bossCompleted = totalBossTiles <= 0 || conqueredBossTiles >= totalBossTiles;

        int rowIndex = 0;
        if (totalBossTiles > 0)
        {
            BindRow(rowIndex, true, bossCompleted, $"모든 보스 타일 점령 ({conqueredBossTiles}/{totalBossTiles})");
            rowIndex++;
        }

        BindRow(rowIndex, true, conquestCompleted, $"전체 지역의 {requiredPercent}% 점령 달성 ({conqueredTiles}/{nonStartTiles})");
        rowIndex++;

        HideRowsFrom(rowIndex);
        SetConquestButtonVisible(bossCompleted && conquestCompleted);
    }

    private void CountWorldProgress(out int totalBossTiles, out int conqueredBossTiles, out int nonStartTiles, out int conqueredTiles)
    {
        totalBossTiles = 0;
        conqueredBossTiles = 0;
        nonStartTiles = 0;
        conqueredTiles = 0;

        IReadOnlyList<WorldTileData> tiles = runManager.MapData.Tiles;
        for (int i = 0; i < tiles.Count; i++)
        {
            WorldTileData tile = tiles[i];
            if (tile == null)
                continue;

            if (!tile.isPlayerStart)
            {
                nonStartTiles++;
                if (tile.currentOwner == FactionType.Player)
                    conqueredTiles++;
            }

            if (tile.eventType == WorldTileEventType.Boss)
            {
                totalBossTiles++;
                if (tile.currentOwner == FactionType.Player)
                    conqueredBossTiles++;
            }
        }
    }

    private void BindEmpty()
    {
        EnsureRows();
        HideRowsFrom(0);
        SetConquestButtonVisible(false);
    }

    private void HideRowsFrom(int startIndex)
    {
        for (int i = Mathf.Max(0, startIndex); i < rowInstances.Count; i++)
            BindRow(i, false, false, string.Empty);
    }

    private void BindRow(int index, bool visible, bool completed, string text)
    {
        if (index < 0 || index >= rowInstances.Count)
            return;

        WorldDominationConditionRowUI row = rowInstances[index];
        if (row == null)
            return;

        row.Bind(visible, completed, text);
    }

    private void SetConquestButtonVisible(bool visible)
    {
        if (conquestButtonRoot != null)
            conquestButtonRoot.SetActive(visible);
        else if (conquestButton != null)
            conquestButton.gameObject.SetActive(visible);
    }

    private void HandleConquestButtonClicked()
    {
        if (runManager == null)
            return;

        if (runManager.IsWorldConquestAvailable())
            runManager.HandleWorldConquestButtonPressed();
    }
}
