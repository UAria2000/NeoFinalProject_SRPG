using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class WorldRunTransientState
{
    public List<InventoryStackData> inventory = new List<InventoryStackData>();
    public List<PrisonerRuntimeData> prisoners = new List<PrisonerRuntimeData>();
    public ItemDefinition sharedConsumableItem;
    public int worldEarnedSoulAlreadyGranted;
    public int currentMana;
    public int maxMana;
    public long nextPrisonerSequence = 1;
    public List<PartyEquipmentAssignmentData> partyEquipmentAssignments = new List<PartyEquipmentAssignmentData>();

    [Header("Settlement Records")]
    public int settlementBattleCount;
    public int settlementVictoryCount;
    public int settlementDefeatCount;
    public int settlementKilledEnemyCount;
    public int settlementCompletedQuestCount;
    public int settlementCapturedEnemyCount;
    public List<PrisonerRuntimeData> settlementCapturedPrisonerRecords = new List<PrisonerRuntimeData>();
    public static WorldRunTransientState CreateForNewWorld(PartyDefinition playerPartyTemplate)
    {
        WorldRunTransientState state = new WorldRunTransientState();
        if (playerPartyTemplate != null)
            state.inventory = playerPartyTemplate.CreateInventoryRuntime();
        return state;
    }

    public void ResetForNewWorld(PartyDefinition playerPartyTemplate, int initialMaxMana = 0)
    {
        inventory = playerPartyTemplate != null ? playerPartyTemplate.CreateInventoryRuntime() : new List<InventoryStackData>();
        prisoners.Clear();
        sharedConsumableItem = null;
        worldEarnedSoulAlreadyGranted = 0;
        maxMana = System.Math.Max(0, initialMaxMana);
        currentMana = maxMana;
        nextPrisonerSequence = 1;
        partyEquipmentAssignments.Clear();
        ClearSettlementRecords();
    }

    public bool AddItem(ItemDefinition item, int amount = 1)
    {
        if (item == null || amount <= 0)
            return false;

        if (!item.IsInventoryItem())
        {
            Debug.LogWarning($"[WorldRunTransientState] Inventory accepts only equipment/consumables. Ignored: {item.name}");
            return false;
        }

        if (inventory == null)
            inventory = new List<InventoryStackData>();

        int clampedAmount = Math.Max(1, amount);

        if (item.IsStackableInInventory())
        {
            InventoryStackData existing = inventory.Find(x => x != null && x.item == item);
            if (existing != null)
                existing.amount += clampedAmount;
            else
                inventory.Add(new InventoryStackData { item = item, amount = clampedAmount });
        }
        else
        {
            // 장비는 현재 저장 데이터 호환을 위해 amount 집계도 허용하지만,
            // UI에서는 개별 슬롯으로 펼쳐 보여준다. 신규 추가는 같은 정의 장비라도 별도 스택으로 보관한다.
            for (int i = 0; i < clampedAmount; i++)
                inventory.Add(new InventoryStackData { item = item, amount = 1 });
        }

        return true;
    }

    public void AddPrisoner(UnitDefinition unit, int capturedLevel = 1, UnitViewDefinition viewDefinition = null)
    {
        if (unit == null)
            return;

        prisoners.Add(PrisonerRuntimeData.CreateFromCapturedUnit(unit, capturedLevel, nextPrisonerSequence++, viewDefinition));
    }

    public void AddPrisonerFromItem(ItemDefinition prisonerItem, int capturedLevel = 1, UnitDefinition fallbackUnit = null, UnitViewDefinition fallbackView = null)
    {
        if (prisonerItem == null && fallbackUnit == null)
            return;

        prisoners.Add(PrisonerRuntimeData.CreateFromPrisonerItem(prisonerItem, capturedLevel, nextPrisonerSequence++, fallbackUnit, fallbackView));
    }

    public void AddSoulEarnedInWorld(int amount)
    {
        worldEarnedSoulAlreadyGranted += Math.Max(0, amount);
    }

    public void ClearSettlementRecords()
    {
        settlementBattleCount = 0;
        settlementVictoryCount = 0;
        settlementDefeatCount = 0;
        settlementKilledEnemyCount = 0;
        settlementCompletedQuestCount = 0;
        settlementCapturedEnemyCount = 0;
        if (settlementCapturedPrisonerRecords == null)
            settlementCapturedPrisonerRecords = new List<PrisonerRuntimeData>();
        else
            settlementCapturedPrisonerRecords.Clear();
    }

    public void RecordCapturedPrisoner(CapturedPrisonerRewardEntry reward)
    {
        if (reward == null)
            return;

        if (settlementCapturedPrisonerRecords == null)
            settlementCapturedPrisonerRecords = new List<PrisonerRuntimeData>();

        PrisonerRuntimeData record = null;
        long sequence = nextPrisonerSequence++;

        if (reward.prisonerItem != null)
        {
            record = PrisonerRuntimeData.CreateFromPrisonerItem(
                reward.prisonerItem,
                Math.Max(1, reward.capturedLevel),
                sequence,
                reward.fallbackUnit,
                reward.fallbackView);
        }
        else if (reward.fallbackUnit != null)
        {
            record = PrisonerRuntimeData.CreateFromCapturedUnit(
                reward.fallbackUnit,
                Math.Max(1, reward.capturedLevel),
                sequence,
                reward.fallbackView);
        }

        if (record != null)
            settlementCapturedPrisonerRecords.Add(record);
    }
}

