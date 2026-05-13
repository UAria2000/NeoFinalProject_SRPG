using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class WorldSettlementSummary
{
    public bool wasVictory;
    public int worldNumber;

    public int battleCount;
    public int victoryCount;
    public int defeatCount;
    public int killedEnemyCount;
    public int completedQuestCount;
    public int capturedEnemyCount;

    public int worldEarnedSoulAlreadyGranted;
    public int convertedItemSoul;
    public int capturedPrisonerSoul;
    public int baseSoulTotal;
    public int soulBonusPercent;
    public int totalSettlementSoulAward;
    public int soulAwardToGrant;

    public int battleExp;
    public int victoryExp;
    public int defeatExp;
    public int killedEnemyExp;
    public int capturedEnemyExp;
    public int completedQuestExp;
    public int baseExpTotal;
    public int expBonusPercent;
    public int totalSettlementExpAward;

    public int sizeBonusPercent;
    public int difficultyBonusPercent;
    public int victoryBonusPercent;
    public int levelBonusPercent;

    public string worldSizeLabel;
    public string worldDifficultyLabel;

    public int lordLevelBefore;
    public int lordExpBefore;
    public int lordExpToNextBefore;
    public int lordLevelAfter;
    public int lordExpAfter;
    public int lordExpToNextAfter;

    public readonly List<ItemDefinition> inventoryItems = new List<ItemDefinition>();
    public readonly List<PrisonerRuntimeData> capturedPrisonerRecords = new List<PrisonerRuntimeData>();

    public string ResultLabel => $"{Mathf.Max(1, worldNumber)}번째 세계 {(wasVictory ? "정복" : "정복 실패")}";
}
