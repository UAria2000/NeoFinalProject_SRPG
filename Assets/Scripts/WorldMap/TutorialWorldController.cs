using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum TutorialTileStage
{
    Start = 0,
    WeakBattle = 1,
    Quest = 2,
    Treasure = 3,
    ElfCaptureBattle = 4,
    Rest = 5,
    HumanCaptureBattle = 6,
}

[Serializable]
public class TutorialEnemyPartyMember
{
    public UnitDefinition unitDefinition;
    public UnitViewDefinition unitViewDefinition;
    [Range(0, 3)] public int slotIndex;
    [Min(1)] public int level = 1;
}

[DisallowMultipleComponent]
public class TutorialWorldController : MonoBehaviour
{
    [Header("Overlay")]
    [SerializeField] private TutorialOverlayUI overlayUI;

    [Header("Enemy Parties")]
    [SerializeField] private List<TutorialEnemyPartyMember> weakBattleEnemies = new List<TutorialEnemyPartyMember>(2);
    [SerializeField] private List<TutorialEnemyPartyMember> elfCaptureEnemies = new List<TutorialEnemyPartyMember>(2);
    [SerializeField] private List<TutorialEnemyPartyMember> humanCaptureEnemies = new List<TutorialEnemyPartyMember>(2);

    [Header("Fixed Quest Reward")]
    [SerializeField, Min(0)] private int questSoulReward = 10;
    [SerializeField, Min(0)] private int questExperienceReward = 10;
    [SerializeField] private ItemDefinition questPotionReward;
    [SerializeField, Min(1)] private int questPotionAmount = 1;
    [SerializeField] private ItemDefinition questEquipmentReward;

    [Header("Fixed Treasure Reward")]
    [SerializeField] private ItemDefinition treasurePotionReward;
    [SerializeField, Min(1)] private int treasurePotionAmount = 3;

    [Header("Legion Panel")]
    [SerializeField] private MainUIOverlayController mainUIOverlayController;
    [SerializeField] private bool openLegionAfterElfCaptureBattle = true;

    [Header("Tutorial Text")]
    [TextArea(2, 5)] [SerializeField] private string startDescription = "시작 지점입니다.";
    [TextArea(2, 5)] [SerializeField] private string weakBattleDescription = "약한 적들이 길을 막고 있습니다.";
    [TextArea(2, 5)] [SerializeField] private string questDescription = "보물 타일을 점령하는 임무입니다.";
    [TextArea(2, 5)] [SerializeField] private string treasureDescription = "튜토리얼 보상입니다.";
    [TextArea(2, 5)] [SerializeField] private string elfBattleDescription = "엘프를 포획할 수 있는 전투입니다.";
    [TextArea(2, 5)] [SerializeField] private string restDescription = "군세가 잠시 휴식합니다.";
    [TextArea(2, 5)] [SerializeField] private string humanBattleDescription = "인간 포획 전투입니다.";

    private WorldRunManager runManager;
    private bool finalSettlementOpened;

    public void Initialize(WorldRunManager manager)
    {
        runManager = manager;
        if (overlayUI == null)
            overlayUI = UnityEngine.Object.FindFirstObjectByType<TutorialOverlayUI>(FindObjectsInactive.Include);
        if (mainUIOverlayController == null)
            mainUIOverlayController = UnityEngine.Object.FindFirstObjectByType<MainUIOverlayController>(FindObjectsInactive.Include);
    }

    public WorldMapData GenerateTutorialMap(WorldGenerationSettings settings)
    {
        WorldMapData map = new WorldMapData
        {
            radius = 1,
        };

        for (int i = 0; i < 7; i++)
        {
            WorldTileEventType eventType = WorldTileEventType.None;
            switch ((TutorialTileStage)i)
            {
                case TutorialTileStage.WeakBattle: eventType = WorldTileEventType.Battle; break;
                case TutorialTileStage.Quest: eventType = WorldTileEventType.Quest; break;
                case TutorialTileStage.Treasure: eventType = WorldTileEventType.Treasure; break;
                case TutorialTileStage.ElfCaptureBattle: eventType = WorldTileEventType.Battle; break;
                case TutorialTileStage.Rest: eventType = WorldTileEventType.Rest; break;
                case TutorialTileStage.HumanCaptureBattle: eventType = WorldTileEventType.Battle; break;
            }

            FactionType native = i >= 4 && i <= 5 ? FactionType.FactionB : FactionType.FactionA;
            WorldTileData tile = new WorldTileData
            {
                tileId = i,
                coord = new HexCoord(0, -i),
                nativeFaction = native,
                currentOwner = i == 0 ? FactionType.Player : native,
                eventType = eventType,
                revealed = i <= 1,
                isPlayerStart = i == 0,
                isResolved = i == 0,
                isIconDisabled = false,
                eventDescriptionText = GetStageDescription((TutorialTileStage)i),
                previewEnemyPortraits = BuildPreviewForStage((TutorialTileStage)i),
            };

            if (i == 0)
                map.startTileId = tile.tileId;

            map.tiles.Add(tile);
        }

        map.RebuildLookup();
        return map;
    }

    public bool TryBuildEnemyPartyForTile(WorldTileData tile, out BattlePartyRuntimeState enemyParty)
    {
        enemyParty = null;
        if (tile == null)
            return false;

        List<TutorialEnemyPartyMember> source = GetEnemyListForTile(tile.tileId);
        if (source == null || source.Count == 0)
            return false;

        enemyParty = BuildRuntimeParty(source, $"Tutorial_{tile.tileId}");
        return enemyParty != null && enemyParty.IsValidMemberCount() && !enemyParty.HasNullDefinitions();
    }

    public bool TryOpenTutorialQuestOffer(WorldTileData sourceTile, WorldQuestController questController)
    {
        if (sourceTile == null || questController == null || runManager == null || runManager.MapData == null)
            return false;
        if (sourceTile.tileId != (int)TutorialTileStage.Quest)
            return false;

        WorldQuestDefinition definition = BuildTutorialQuestDefinition();
        int targetTileId = (int)TutorialTileStage.Treasure;
        bool opened = questController.TryOpenForcedQuestOfferFromTile(sourceTile, runManager.MapData, definition, targetTileId, false);
        if (opened)
            runManager.StartManagedTutorialCoroutine(ShowQuestImagesRoutine());
        return opened;
    }

    public bool TryCreateTutorialTreasure(WorldTileData tile, out WorldTreasureResult result)
    {
        result = null;
        if (tile == null || tile.tileId != (int)TutorialTileStage.Treasure)
            return false;

        result = new WorldTreasureResult();
        result.soulAmount = 0;
        result.Add(treasurePotionReward, treasurePotionAmount);
        return true;
    }

    public IEnumerator PlayWorldEntryIfNeeded()
    {
        yield return ShowSteps(1, 2, 3);
    }

    public IEnumerator PlayBattleIntroIfNeeded(WorldTileData tile)
    {
        if (tile == null)
            yield break;

        if (tile.tileId == (int)TutorialTileStage.WeakBattle)
            yield return ShowSteps(4, 5, 6, 7, 8);
        else if (tile.tileId == (int)TutorialTileStage.ElfCaptureBattle)
            yield return ShowSteps(12, 13, 14);
        else if (tile.tileId == (int)TutorialTileStage.HumanCaptureBattle)
            yield return ShowSteps(16);
    }

    public IEnumerator PlayAfterBattleReturnIfNeeded(WorldTileData tile, BattleResultType result)
    {
        if (tile == null || result != BattleResultType.Victory)
            yield break;

        if (tile.tileId == (int)TutorialTileStage.WeakBattle)
        {
            yield return ShowSteps(9);
        }
        else if (tile.tileId == (int)TutorialTileStage.ElfCaptureBattle)
        {
            yield return ShowSteps(15);
            if (openLegionAfterElfCaptureBattle)
                OpenLegionPanel();
        }
        else if (tile.tileId == (int)TutorialTileStage.HumanCaptureBattle)
        {
            yield return PlayFinalMessageAndOpenSettlement();
        }
    }

    public bool IsElfCaptureBattleTile(int tileId) => tileId == (int)TutorialTileStage.ElfCaptureBattle;

    public int GetCaptureChanceOverridePercent(BattleUnit target)
    {
        if (runManager == null || !runManager.IsTutorialElfCaptureBattleActive)
            return -1;
        if (target == null || target.Team != TeamType.Enemy || target.Definition == null || !target.Definition.canBeCaptured)
            return -1;
        return 100;
    }

    private IEnumerator ShowQuestImagesRoutine()
    {
        yield return null;
        yield return ShowSteps(10, 11);
    }

    private IEnumerator PlayFinalMessageAndOpenSettlement()
    {
        if (finalSettlementOpened)
            yield break;

        finalSettlementOpened = true;
        if (overlayUI != null)
        {
            bool clicked = false;
            yield return overlayUI.ShowFinalMessage(() => clicked = true);
            while (!clicked)
                yield return null;
        }

        if (runManager != null)
        {
            runManager.MarkTutorialStepShown(17);
            runManager.ForceOpenWorldSettlementFromTutorial();
        }
    }

    private IEnumerator ShowSteps(params int[] steps)
    {
        if (overlayUI == null || runManager == null)
            yield break;

        yield return overlayUI.ShowSpriteSequence(
            step => !runManager.IsTutorialStepShown(step),
            step => runManager.MarkTutorialStepShown(step),
            steps);
    }

    private void OpenLegionPanel()
    {
        if (mainUIOverlayController == null)
            mainUIOverlayController = UnityEngine.Object.FindFirstObjectByType<MainUIOverlayController>(FindObjectsInactive.Include);
        if (mainUIOverlayController != null)
            mainUIOverlayController.OpenMainPanel(MainUIPanelType.Barracks);
    }

    private WorldQuestDefinition BuildTutorialQuestDefinition()
    {
        WorldQuestDefinition definition = new WorldQuestDefinition
        {
            questId = "tutorial_capture_treasure_tile",
            displayName = "보물 타일 점령",
            description = questDescription,
            questType = WorldQuestType.CaptureSpecificTile,
            targetCount = 1,
            soulReward = Mathf.Max(0, questSoulReward),
            experienceReward = Mathf.Max(0, questExperienceReward),
            enabled = true,
        };

        if (questPotionReward != null && questPotionAmount > 0)
        {
            definition.itemRewards.Add(new WorldQuestRewardItemEntry
            {
                item = questPotionReward,
                amount = Mathf.Max(1, questPotionAmount),
            });
        }
        if (questEquipmentReward != null)
        {
            definition.itemRewards.Add(new WorldQuestRewardItemEntry
            {
                item = questEquipmentReward,
                amount = 1,
            });
        }

        return definition;
    }

    private BattlePartyRuntimeState BuildRuntimeParty(List<TutorialEnemyPartyMember> source, string partyName)
    {
        BattlePartyRuntimeState state = new BattlePartyRuntimeState
        {
            partyName = partyName,
        };

        if (source == null)
            return state;

        for (int i = 0; i < source.Count && state.members.Count < 4; i++)
        {
            TutorialEnemyPartyMember entry = source[i];
            if (entry == null || entry.unitDefinition == null || entry.unitViewDefinition == null)
                continue;

            state.members.Add(new PartyMemberData
            {
                unitDefinition = entry.unitDefinition,
                unitViewDefinition = entry.unitViewDefinition,
                startSlotIndex = Mathf.Clamp(entry.slotIndex, 0, 3),
                currentLevel = Mathf.Max(1, entry.level),
                originalLevel = Mathf.Max(1, entry.level),
                persistentCurrentHP = -1,
                instanceId = Guid.NewGuid().ToString("N"),
            });
        }

        return state;
    }

    private List<TutorialEnemyPartyMember> GetEnemyListForTile(int tileId)
    {
        if (tileId == (int)TutorialTileStage.WeakBattle)
            return weakBattleEnemies;
        if (tileId == (int)TutorialTileStage.ElfCaptureBattle)
            return elfCaptureEnemies;
        if (tileId == (int)TutorialTileStage.HumanCaptureBattle)
            return humanCaptureEnemies;
        return null;
    }

    private List<Sprite> BuildPreviewForStage(TutorialTileStage stage)
    {
        List<Sprite> result = new List<Sprite>();
        List<TutorialEnemyPartyMember> entries = GetEnemyListForTile((int)stage);
        if (entries == null)
            return result;

        for (int i = 0; i < entries.Count; i++)
        {
            UnitViewDefinition view = entries[i] != null ? entries[i].unitViewDefinition : null;
            Sprite sprite = view != null ? view.GetSlotFaceSprite() : null;
            if (sprite != null)
                result.Add(sprite);
        }

        return result;
    }

    private string GetStageDescription(TutorialTileStage stage)
    {
        switch (stage)
        {
            case TutorialTileStage.Start: return startDescription;
            case TutorialTileStage.WeakBattle: return weakBattleDescription;
            case TutorialTileStage.Quest: return questDescription;
            case TutorialTileStage.Treasure: return treasureDescription;
            case TutorialTileStage.ElfCaptureBattle: return elfBattleDescription;
            case TutorialTileStage.Rest: return restDescription;
            case TutorialTileStage.HumanCaptureBattle: return humanBattleDescription;
            default: return string.Empty;
        }
    }
}
