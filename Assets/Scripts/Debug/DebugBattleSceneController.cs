using System;
using System.Collections.Generic;
using UnityEngine;

public partial class DebugBattleSceneController : MonoBehaviour
{
    [Header("Original Battle Scene")]
    [SerializeField] private BattleManager battleManager;
    [SerializeField] private GameObject battleSceneRoot;
    [SerializeField] private GameObject worldMapRoot;

    [Header("Asset Pools")]
    [SerializeField] private UnitDefinition[] allyUnitDefinitions = Array.Empty<UnitDefinition>();
    [SerializeField] private UnitDefinition[] enemyUnitDefinitions = Array.Empty<UnitDefinition>();
    [SerializeField] private UnitViewDefinition[] unitViewDefinitions = Array.Empty<UnitViewDefinition>();
    [SerializeField] private SkillLearnPoolTable allySkillPoolTable;

    [Header("Defaults")]
    [SerializeField] private int defaultLevel = 1;
    [SerializeField] private bool showSetupPanel = true;
    [SerializeField] private bool enableAllSlotsByDefault = true;
    [SerializeField] private bool captureGameViewOnPlay = true;
    [SerializeField, Range(0f, 20f)] private float debugPromotionBonusPercentPerRank = 1f;
    [SerializeField] private Sprite[] promotionRankSprites = Array.Empty<Sprite>();

    [Header("Debug Effect Overrides")]
    [SerializeField] private bool useDebugEffectOverrides;
    [SerializeField] private GameObject debugCastEffectPrefab;
    [SerializeField] private GameObject debugHitEffectPrefab;
    [SerializeField] private HitEffectType debugHitEffectType = HitEffectType.Slashing;
    [SerializeField] private HitEffectAnchorType debugHitEffectAnchorType = HitEffectAnchorType.Default;
    [SerializeField, Min(0f)] private float debugHitEffectDurationOverride;

    [Header("Runtime Debug Options")]
    [SerializeField] private bool debugAllyInvincible;
    [SerializeField] private bool debugNoSkillCooldown;

    private readonly DebugSlot[] allySlots = new DebugSlot[4];
    private readonly DebugSlot[] enemySlots = new DebugSlot[4];
    private readonly Dictionary<SkillDefinition, SkillCooldownSnapshot> runtimeSkillCooldownSnapshots = new Dictionary<SkillDefinition, SkillCooldownSnapshot>();
    private Vector2 pickerScroll;
    private PickerState picker;
    private string pickerSearch = string.Empty;
    private bool gameViewCaptureQueued;
    private bool debugBattleRunning;

    private void Awake()
    {
        if (battleManager == null)
            battleManager = UnityEngine.Object.FindFirstObjectByType<BattleManager>(FindObjectsInactive.Include);
        if (battleSceneRoot == null)
            battleSceneRoot = FindRootObject("BattleScene");
        if (worldMapRoot == null)
            worldMapRoot = FindRootObject("WorldMap");

        for (int i = 0; i < 4; i++)
        {
            allySlots[i] = new DebugSlot { enabled = enableAllSlotsByDefault || i == 0, level = defaultLevel };
            enemySlots[i] = new DebugSlot { enabled = enableAllSlotsByDefault || i == 0, level = defaultLevel };
        }

        EnsurePromotionRankSpritesLoaded();

        if (worldMapRoot != null)
            worldMapRoot.SetActive(false);
        if (battleSceneRoot != null)
            battleSceneRoot.SetActive(true);

        AutoFillSlots();

        if (battleManager != null)
            battleManager.BattleEnded += HandleDebugBattleEnded;
    }

    private void OnDestroy()
    {
        if (battleManager != null)
            battleManager.BattleEnded -= HandleDebugBattleEnded;
    }

    private void Update()
    {
        if (debugBattleRunning && battleManager != null && !battleManager.IsBattleInProgress && battleManager.BattleResult != BattleResultType.None)
            ReturnToDebugSetup();

        if (debugBattleRunning && debugAllyInvincible)
            RefreshAllyInvincibility();
    }

    private GameObject FindRootObject(string objectName)
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

    private void ResetSetup()
    {
        for (int i = 0; i < 4; i++)
        {
            allySlots[i].enabled = enableAllSlotsByDefault || i == 0;
            allySlots[i].level = defaultLevel;
            allySlots[i].promotionRank = LegionFormula.MinPromotionRank;
            allySlots[i].ResetSkills();

            enemySlots[i].enabled = enableAllSlotsByDefault || i == 0;
            enemySlots[i].level = defaultLevel;
            enemySlots[i].ResetSkills();
        }

        AutoFillSlots();
    }

    private void StartDebugBattle()
    {
        if (battleManager == null)
            return;

        BattlePartyRuntimeState allyState = BuildRuntimeState("Debug Ally", allySlots, allyUnitDefinitions, TeamType.Ally);
        BattlePartyRuntimeState enemyState = BuildRuntimeState("Debug Enemy", enemySlots, enemyUnitDefinitions, TeamType.Enemy);

        battleManager.StopManagedCoroutines();
        battleManager.PrepareBattle(allyState, enemyState, new List<InventoryStackData>());
        battleManager.StartBattle();
        ApplyRuntimeCooldownMode();
        showSetupPanel = false;
        debugBattleRunning = true;
    }

#if UNITY_EDITOR
    public void EditorStartDebugBattleForCapture()
    {
        StartDebugBattle();
    }

    public void EditorSetAllyPromotionRankForDebugCapture(int slotIndex, int rank)
    {
        EnsureSlotsInitialized();
        int index = Mathf.Clamp(slotIndex, 0, allySlots.Length - 1);
        allySlots[index].promotionRank = LegionFormula.ClampLegionRank(rank);
    }

    public void EditorSetAllyLevelForDebugCapture(int slotIndex, int level)
    {
        EnsureSlotsInitialized();
        int index = Mathf.Clamp(slotIndex, 0, allySlots.Length - 1);
        allySlots[index].level = Mathf.Max(1, level);
    }

    public void EditorStopDebugBattleForCapture()
    {
        StopDebugBattle();
    }
#endif

    private void StopDebugBattle()
    {
        if (battleManager != null)
        {
            battleManager.StopManagedCoroutines();
            battleManager.SetBattleResult(BattleResultType.Flee);
            battleManager.SetBattleStarted(false);
        }

        ReturnToDebugSetup();
    }

    private void HandleDebugBattleEnded(BattleResultType result)
    {
        ReturnToDebugSetup();
    }

    private void ReturnToDebugSetup()
    {
        ResetDebugBattleRuntimeScene();
        debugBattleRunning = false;
        showSetupPanel = true;
        picker.Close();
        pickerSearch = string.Empty;

        if (battleSceneRoot != null)
            battleSceneRoot.SetActive(true);
        if (worldMapRoot != null)
            worldMapRoot.SetActive(false);
    }

    private void ResetDebugBattleRuntimeScene()
    {
        if (battleManager != null)
        {
            battleManager.StopManagedCoroutines();
            battleManager.ViewManager?.ClearAllViews();
            battleManager.ClearTargetMarkers();
            battleManager.ClearInfoSelections();
            battleManager.ClearUISelection();
            battleManager.AssignFormations(new BattleFormation(), new BattleFormation());
            battleManager.SetBattleStarted(false);
            battleManager.SetBattleResult(BattleResultType.None);
            battleManager.SetBattleEndEventSent(false);
            battleManager.SetCurrentActingUnit(null);
            battleManager.SetLastShownAllyUnit(null);
            battleManager.SetWaitingForPlayerAction(false);
            battleManager.SetInputMode(BattleInputMode.None);
            battleManager.SetTurnState(TurnState.Waiting);
            battleManager.SetCurrentRoundTurnOrder(null);
            battleManager.RefreshAllUI();
        }

        ClearDebugRuntimeObjects();
    }

    private void ClearDebugRuntimeObjects()
    {
        BattleUnitView[] unitViews = UnityEngine.Object.FindObjectsByType<BattleUnitView>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < unitViews.Length; i++)
        {
            if (unitViews[i] != null)
                DestroyDebugRuntimeObject(unitViews[i].gameObject);
        }

        BattleFloatingTextUI[] floatingTexts = UnityEngine.Object.FindObjectsByType<BattleFloatingTextUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < floatingTexts.Length; i++)
        {
            if (floatingTexts[i] != null)
                DestroyDebugRuntimeObject(floatingTexts[i].gameObject);
        }

        BattleHitEffectUI[] debugHitEffects = UnityEngine.Object.FindObjectsByType<BattleHitEffectUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < debugHitEffects.Length; i++)
        {
            if (debugHitEffects[i] != null)
                DestroyDebugRuntimeObject(debugHitEffects[i].gameObject);
        }

        BattleRichHitEffectUI[] richHitEffects = UnityEngine.Object.FindObjectsByType<BattleRichHitEffectUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < richHitEffects.Length; i++)
        {
            if (richHitEffects[i] != null)
                DestroyDebugRuntimeObject(richHitEffects[i].gameObject);
        }
    }

    private static void DestroyDebugRuntimeObject(GameObject target)
    {
        if (target == null)
            return;

#if UNITY_EDITOR
        DestroyImmediate(target);
#else
        Destroy(target);
#endif
    }

    private BattlePartyRuntimeState BuildRuntimeState(string partyName, DebugSlot[] slots, UnitDefinition[] unitPool, TeamType team)
    {
        BattlePartyRuntimeState state = new BattlePartyRuntimeState { partyName = partyName };
        for (int i = 0; i < slots.Length; i++)
        {
            DebugSlot slot = slots[i];
            if (!slot.enabled)
                continue;

            UnitDefinition unit = GetSelected(unitPool, slot.unitIndex);
            UnitViewDefinition view = GetMatchingView(unit);
            if (unit == null || view == null)
                continue;

            UnitDefinition runtimeUnit = CreateRuntimeUnitDefinition(unit);
            int level = Mathf.Max(1, slot.level);

            PartyMemberData member = new PartyMemberData
            {
                unitDefinition = runtimeUnit,
                unitViewDefinition = view,
                startSlotIndex = GetFormationSlotIndex(team, i),
                instanceId = Guid.NewGuid().ToString("N"),
                currentLevel = level,
                originalLevel = level,
                promotionRank = team == TeamType.Ally ? LegionFormula.ClampLegionRank(slot.promotionRank) : LegionFormula.MinPromotionRank,
                promotionBonusPercentPerRank = team == TeamType.Ally ? Mathf.Max(0f, debugPromotionBonusPercentPerRank) : 1f,
                persistentCurrentHP = -1,
            };
            ApplyDebugLevelGrowth(member, runtimeUnit, level);

            if (team == TeamType.Ally)
                AddAllySelectedSkills(member, unit, runtimeUnit, slot);
            else
                AddEnemyFixedSkills(member, unit, runtimeUnit);

            state.members.Add(member);
        }

        return state;
    }

    private static void ApplyDebugLevelGrowth(PartyMemberData member, UnitDefinition definition, int level)
    {
        if (member == null || definition == null)
            return;

        int levelUps = Mathf.Max(0, level - 1);
        member.levelGrowthMaxHp = GetDeterministicGrowthTotal(definition.hpGrowthPerLevel, levelUps);
        member.levelGrowthDmg = GetDeterministicGrowthTotal(definition.dmgGrowthPerLevel, levelUps);
    }

    private static int GetDeterministicGrowthTotal(Vector2Int range, int levelUps)
    {
        if (levelUps <= 0)
            return 0;

        int min = Mathf.Min(range.x, range.y);
        int max = Mathf.Max(range.x, range.y);
        int average = Mathf.RoundToInt((min + max) * 0.5f);
        return Mathf.Max(0, average) * levelUps;
    }

    private static int GetFormationSlotIndex(TeamType team, int visualSlotIndex)
    {
        int clampedIndex = Mathf.Clamp(visualSlotIndex, 0, 3);
        return team == TeamType.Ally ? 3 - clampedIndex : clampedIndex;
    }

    private void EnsurePromotionRankSpritesLoaded()
    {
        if (promotionRankSprites != null && promotionRankSprites.Length >= LegionFormula.MaxPromotionRank)
            return;

        promotionRankSprites = new Sprite[LegionFormula.MaxPromotionRank];
#if UNITY_EDITOR
        for (int i = 0; i < promotionRankSprites.Length; i++)
            promotionRankSprites[i] = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Image/UI/Unit_Rank{i + 1}.png");
#endif
    }

    private UnitDefinition CreateRuntimeUnitDefinition(UnitDefinition source)
    {
        if (source == null)
            return source;

        UnitDefinition clone = Instantiate(source);
        clone.name = source.name + "_DebugRuntime";
        clone.basicAttack = CreateRuntimeSkillDefinition(source.basicAttack);
        clone.fixedStartingSkills = CloneSkillList(source.fixedStartingSkills);
        clone.extraLearnableSkills = CloneSkillList(source.extraLearnableSkills);
        return clone;
    }

    private List<SkillDefinition> CloneSkillList(List<SkillDefinition> source)
    {
        List<SkillDefinition> result = new List<SkillDefinition>();
        if (source == null)
            return result;

        for (int i = 0; i < source.Count; i++)
        {
            SkillDefinition clone = CreateRuntimeSkillDefinition(source[i]);
            if (clone != null && !result.Contains(clone))
                result.Add(clone);
        }

        return result;
    }

    private SkillDefinition CreateRuntimeSkillDefinition(SkillDefinition source)
    {
        if (source == null)
            return source;

        SkillDefinition clone = Instantiate(source);
        clone.name = source.name + "_DebugRuntime";
        RememberRuntimeSkillCooldown(clone);
        ApplyDebugEffectOverrides(clone);
        ApplyNoCooldownToRuntimeSkill(clone);
        return clone;
    }

    private void ApplyDebugEffectOverrides(SkillDefinition skill)
    {
        if (skill == null || !useDebugEffectOverrides)
            return;

        if (debugCastEffectPrefab != null)
            skill.castEffectPrefab = debugCastEffectPrefab;

        if (debugHitEffectPrefab != null)
        {
            skill.hitEffectPrefab = debugHitEffectPrefab;
            skill.hitEffectAnchorType = debugHitEffectAnchorType;
            skill.hitEffectDurationOverride = debugHitEffectDurationOverride;
            return;
        }

        if (skill.hitEffectPrefab != null || skill.hitEffectType != HitEffectType.None)
            return;

        skill.hitEffectPrefab = null;
        skill.hitEffectType = debugHitEffectType;
        skill.hitEffectAnchorType = debugHitEffectAnchorType;
        skill.hitEffectDurationOverride = debugHitEffectDurationOverride;
    }

    private void RememberRuntimeSkillCooldown(SkillDefinition skill)
    {
        if (skill == null || runtimeSkillCooldownSnapshots.ContainsKey(skill))
            return;

        runtimeSkillCooldownSnapshots.Add(skill, new SkillCooldownSnapshot
        {
            cooldownTurns = skill.cooldownTurns,
            initialCooldownTurns = skill.initialCooldownTurns,
        });
    }

    private void ApplyNoCooldownToRuntimeSkill(SkillDefinition skill)
    {
        if (skill == null || !debugNoSkillCooldown)
            return;

        RememberRuntimeSkillCooldown(skill);
        skill.cooldownTurns = 0;
        skill.initialCooldownTurns = 0;
    }

    private void ApplyRuntimeCooldownMode()
    {
        if (battleManager == null)
            return;

        ApplyRuntimeCooldownModeToFormation(battleManager.AllyFormation);
        ApplyRuntimeCooldownModeToFormation(battleManager.EnemyFormation);
    }

    private void ApplyRuntimeCooldownModeToFormation(BattleFormation formation)
    {
        if (formation == null)
            return;

        List<BattleUnit> units = formation.GetAllUnits();
        for (int i = 0; i < units.Count; i++)
            ApplyRuntimeCooldownModeToUnit(units[i]);
    }

    private void ApplyRuntimeCooldownModeToUnit(BattleUnit unit)
    {
        if (unit == null)
            return;

        if (debugNoSkillCooldown)
            ClearRuntimeSkillCooldowns(unit);

        ApplyRuntimeCooldownModeToSkill(unit.BasicAttack);
        if (unit.MemberData == null || unit.MemberData.learnedSkills == null)
            return;

        for (int i = 0; i < unit.MemberData.learnedSkills.Count; i++)
            ApplyRuntimeCooldownModeToSkill(unit.MemberData.learnedSkills[i]);
    }

    private void ApplyRuntimeCooldownModeToSkill(SkillDefinition skill)
    {
        if (skill == null)
            return;

        RememberRuntimeSkillCooldown(skill);
        if (debugNoSkillCooldown)
        {
            skill.cooldownTurns = 0;
            skill.initialCooldownTurns = 0;
            return;
        }

        SkillCooldownSnapshot snapshot;
        if (runtimeSkillCooldownSnapshots.TryGetValue(skill, out snapshot))
        {
            skill.cooldownTurns = snapshot.cooldownTurns;
            skill.initialCooldownTurns = snapshot.initialCooldownTurns;
        }
    }

    private static void ClearRuntimeSkillCooldowns(BattleUnit unit)
    {
        if (unit == null)
            return;

        System.Reflection.FieldInfo field = typeof(BattleUnit).GetField("skillCooldowns", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        Dictionary<string, int> cooldowns = field != null ? field.GetValue(unit) as Dictionary<string, int> : null;
        if (cooldowns != null)
            cooldowns.Clear();
    }

    private void RefreshAllyInvincibility()
    {
        if (battleManager == null || battleManager.AllyFormation == null)
            return;

        bool healedAny = false;
        List<BattleUnit> allies = battleManager.AllyFormation.GetAllUnits();
        for (int i = 0; i < allies.Count; i++)
        {
            BattleUnit ally = allies[i];
            if (ally == null)
                continue;

            if (ally.IsDead)
                ally.ReviveWithHpPercent(100f);
            else if (ally.CurrentHP < ally.MaxHP)
                ally.Heal(ally.MaxHP);

            ally.AddShield(999999);
            healedAny = true;
        }

        if (healedAny)
            battleManager.RefreshAllUI();
    }

    private void AddAllySelectedSkills(PartyMemberData member, UnitDefinition originalUnit, UnitDefinition runtimeUnit, DebugSlot slot)
    {
        if (IsMainPlayerUnit(originalUnit))
        {
            AddSkillsAsLearned(member, originalUnit.fixedStartingSkills, runtimeUnit);
            return;
        }

        IReadOnlyList<SkillDefinition> pool = GetAllySkillChoices(originalUnit);
        AddUniqueSkill(member.learnedSkills, CreateRuntimeSkillDefinition(GetSelected(pool, slot.skill0Index)), runtimeUnit);
        AddUniqueSkill(member.learnedSkills, CreateRuntimeSkillDefinition(GetSelected(pool, slot.skill1Index)), runtimeUnit);
        AddUniqueSkill(member.learnedSkills, CreateRuntimeSkillDefinition(GetSelected(pool, slot.skill2Index)), runtimeUnit);
    }

    private void AddSkillsAsLearned(PartyMemberData member, IReadOnlyList<SkillDefinition> skills, UnitDefinition runtimeUnit)
    {
        if (member == null || skills == null)
            return;

        for (int i = 0; i < skills.Count; i++)
            AddUniqueSkill(member.learnedSkills, CreateRuntimeSkillDefinition(skills[i]), runtimeUnit);
    }

    private void AddEnemyFixedSkills(PartyMemberData member, UnitDefinition originalUnit, UnitDefinition runtimeUnit)
    {
        if (originalUnit == null || originalUnit.fixedStartingSkills == null)
            return;

        for (int i = 0; i < originalUnit.fixedStartingSkills.Count; i++)
            AddUniqueSkill(member.learnedSkills, CreateRuntimeSkillDefinition(originalUnit.fixedStartingSkills[i]), runtimeUnit);
    }

    private void AddUniqueSkill(List<SkillDefinition> target, SkillDefinition skill, UnitDefinition unit)
    {
        if (target == null || skill == null || unit == null || IsSameSkill(skill, unit.basicAttack) || HasSkill(target, skill))
            return;

        target.Add(skill);
    }

    private bool HasSkill(List<SkillDefinition> target, SkillDefinition skill)
    {
        for (int i = 0; i < target.Count; i++)
        {
            if (IsSameSkill(target[i], skill))
                return true;
        }

        return false;
    }

    private bool IsSameSkill(SkillDefinition a, SkillDefinition b)
    {
        if (a == null || b == null)
            return false;
        if (a == b)
            return true;
        if (!string.IsNullOrWhiteSpace(a.skillId) && !string.IsNullOrWhiteSpace(b.skillId))
            return a.skillId == b.skillId;
        return a.name == b.name;
    }

    private IReadOnlyList<SkillDefinition> GetAllySkillChoices(UnitDefinition unit)
    {
        List<SkillDefinition> result = new List<SkillDefinition>();
        if (IsMainPlayerUnit(unit))
        {
            AddSkills(result, unit.fixedStartingSkills);
            return result;
        }

        if (allySkillPoolTable != null)
        {
            IReadOnlyList<SkillDefinition> classSkills = allySkillPoolTable.GetClassSkills(unit != null ? unit.rangeType : CharacterRangeType.Melee);
            AddSkills(result, classSkills);
            AddSkills(result, allySkillPoolTable.commonSkills);
        }

        if (unit != null)
        {
            AddSkills(result, unit.fixedStartingSkills);
            AddSkills(result, unit.extraLearnableSkills);
        }

        return result;
    }

    private bool IsMainPlayerUnit(UnitDefinition unit)
    {
        if (unit == null)
            return false;

        if (unit.isMainPlayerCharacter)
            return true;

        string key = Normalize(unit.unitId + unit.name);
        return key.Contains("mainplayer") || key.Contains("player");
    }

    private void AddSkills(List<SkillDefinition> target, IReadOnlyList<SkillDefinition> source)
    {
        if (target == null || source == null)
            return;

        for (int i = 0; i < source.Count; i++)
        {
            SkillDefinition skill = source[i];
            if (skill != null && !target.Contains(skill))
                target.Add(skill);
        }
    }

    private void AutoFillSlots()
    {
        EnsureSlotsInitialized();

        string[] defaultAllyUnitIds =
        {
            "dark_priest",
            "main_player",
            "shadow_dancer",
            "dark_knight",
        };

        string[] defaultEnemyUnitIds =
        {
            "human_farmer",
            "human_guard",
            "human_crossbowman",
            "human_priest",
        };

        for (int i = 0; i < allySlots.Length; i++)
        {
            allySlots[i].unitIndex = FindUnitIndexById(allyUnitDefinitions, defaultAllyUnitIds, i);
            allySlots[i].viewIndex = FindMatchingViewIndex(GetSelected(allyUnitDefinitions, allySlots[i].unitIndex));
        }

        for (int i = 0; i < enemySlots.Length; i++)
        {
            enemySlots[i].unitIndex = FindUnitIndexById(enemyUnitDefinitions, defaultEnemyUnitIds, i);
            enemySlots[i].viewIndex = FindMatchingViewIndex(GetSelected(enemyUnitDefinitions, enemySlots[i].unitIndex));
        }
    }

    private void EnsureSlotsInitialized()
    {
        for (int i = 0; i < 4; i++)
        {
            if (allySlots[i] == null)
                allySlots[i] = CreateDefaultSlot(i);
            if (enemySlots[i] == null)
                enemySlots[i] = CreateDefaultSlot(i);
        }
    }

    private DebugSlot CreateDefaultSlot(int slotIndex)
    {
        return new DebugSlot
        {
            enabled = enableAllSlotsByDefault || slotIndex == 0,
            level = defaultLevel,
        };
    }

    private int FindUnitIndexById(UnitDefinition[] pool, string[] preferredUnitIds, int slotIndex)
    {
        if (pool == null || pool.Length == 0)
            return 0;

        if (preferredUnitIds != null && slotIndex >= 0 && slotIndex < preferredUnitIds.Length)
        {
            string target = Normalize(preferredUnitIds[slotIndex]);
            for (int i = 0; i < pool.Length; i++)
            {
                UnitDefinition unit = pool[i];
                if (unit != null && Normalize(unit.unitId) == target)
                    return i;
            }
        }

        return Mathf.Min(Mathf.Max(0, slotIndex), pool.Length - 1);
    }

    private UnitViewDefinition GetMatchingView(UnitDefinition unit)
    {
        return GetSelected(unitViewDefinitions, FindMatchingViewIndex(unit));
    }

    private int FindMatchingViewIndex(UnitDefinition unit)
    {
        if (unit == null || unitViewDefinitions == null || unitViewDefinitions.Length == 0)
            return 0;

        string unitId = Normalize(unit.unitId);
        string assetName = Normalize(unit.name.Replace("_UnitDefinition", string.Empty));

        for (int i = 0; i < unitViewDefinitions.Length; i++)
        {
            UnitViewDefinition view = unitViewDefinitions[i];
            if (view == null)
                continue;

            string viewName = Normalize(view.name.Replace("_UnitViewDefinition", string.Empty));
            if (!string.IsNullOrEmpty(unitId) && viewName == unitId)
                return i;
            if (!string.IsNullOrEmpty(assetName) && viewName == assetName)
                return i;
        }

        for (int i = 0; i < unitViewDefinitions.Length; i++)
        {
            UnitViewDefinition view = unitViewDefinitions[i];
            if (view == null)
                continue;

            string viewName = Normalize(view.name);
            if (!string.IsNullOrEmpty(unitId) && viewName.Contains(unitId))
                return i;
            if (!string.IsNullOrEmpty(assetName) && viewName.Contains(assetName))
                return i;
        }

        return 0;
    }

    private int FindMainPlayerUnitIndex()
    {
        if (allyUnitDefinitions == null || allyUnitDefinitions.Length == 0)
            return 0;

        for (int i = 0; i < allyUnitDefinitions.Length; i++)
        {
            UnitDefinition unit = allyUnitDefinitions[i];
            if (unit != null && unit.isMainPlayerCharacter)
                return i;
        }

        for (int i = 0; i < allyUnitDefinitions.Length; i++)
        {
            UnitDefinition unit = allyUnitDefinitions[i];
            string key = unit != null ? Normalize(unit.unitId + unit.name) : string.Empty;
            if (key.Contains("mainplayer") || key.Contains("player"))
                return i;
        }

        return 0;
    }

    private string BuildEnemySkillSummary(UnitDefinition unit)
    {
        List<string> names = new List<string>();
        if (unit.basicAttack != null)
            names.Add(GetDisplayName(unit.basicAttack));
        if (unit.fixedStartingSkills != null)
        {
            for (int i = 0; i < unit.fixedStartingSkills.Count; i++)
            {
                if (unit.fixedStartingSkills[i] != null)
                    names.Add(GetDisplayName(unit.fixedStartingSkills[i]));
            }
        }

        return names.Count > 0 ? string.Join(" / ", names) : "No skill";
    }

    private string GetShortSkillSummary(UnitDefinition unit)
    {
        string summary = BuildEnemySkillSummary(unit);
        const int maxLength = 42;
        return summary.Length <= maxLength ? summary : summary.Substring(0, maxLength) + "...";
    }

    private void SetAllSlotsEnabled(DebugSlot[] slots, bool value)
    {
        if (slots == null)
            return;

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] != null)
                slots[i].enabled = value;
        }
    }

    private static bool MatchesSearch(string value, string search)
    {
        if (string.IsNullOrWhiteSpace(search))
            return true;
        if (string.IsNullOrWhiteSpace(value))
            return false;

        return value.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0 ||
               Normalize(value).Contains(Normalize(search));
    }

    private bool HasAnyEnabled(DebugSlot[] slots)
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] != null && slots[i].enabled)
                return true;
        }

        return false;
    }

    private void SetSkillIndex(DebugSlot slot, PickerKind kind, int index)
    {
        switch (kind)
        {
            case PickerKind.Skill0:
                slot.skill0Index = index;
                break;
            case PickerKind.Skill1:
                slot.skill1Index = index;
                break;
            case PickerKind.Skill2:
                slot.skill2Index = index;
                break;
        }
    }

    private static int ClampIndex<T>(IReadOnlyList<T> values, int index)
    {
        if (values == null || values.Count == 0)
            return 0;
        return Mathf.Clamp(index, 0, values.Count - 1);
    }

    private static T GetSelected<T>(IReadOnlyList<T> values, int index) where T : UnityEngine.Object
    {
        if (values == null || values.Count == 0)
            return null;
        return values[ClampIndex(values, index)];
    }

    private static string GetDisplayName(UnityEngine.Object obj)
    {
        if (obj == null)
            return "None";
        if (obj is UnitDefinition unit && !string.IsNullOrWhiteSpace(unit.unitName))
            return unit.unitName;
        if (obj is SkillDefinition skill && !string.IsNullOrWhiteSpace(skill.skillName))
            return skill.skillName;
        return obj.name;
    }

    private static string Normalize(string value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Replace("_", string.Empty).Replace(" ", string.Empty).ToLowerInvariant();
    }

    private enum PickerKind
    {
        Unit = 0,
        Skill0 = 10,
        Skill1 = 11,
        Skill2 = 12,
    }

    [Serializable]
    private class DebugSlot
    {
        public bool enabled;
        public int unitIndex;
        public int viewIndex;
        public int level = 1;
        public int promotionRank = 1;
        public int skill0Index;
        public int skill1Index = 1;
        public int skill2Index = 2;

        public void ResetSkills()
        {
            skill0Index = 0;
            skill1Index = 1;
            skill2Index = 2;
        }
    }

    private struct PickerState
    {
        public bool IsOpen;
        public bool IsAlly;
        public int SlotIndex;
        public PickerKind Kind;

        public void Close()
        {
            IsOpen = false;
        }
    }

    private struct SkillCooldownSnapshot
    {
        public int cooldownTurns;
        public int initialCooldownTurns;
    }
}
