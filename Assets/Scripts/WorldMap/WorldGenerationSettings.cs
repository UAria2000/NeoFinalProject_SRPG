using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "World Map/Generation Settings", fileName = "WorldGenerationSettings")]
public class WorldGenerationSettings : ScriptableObject
{
    [Header("World")]
    [Range(3, 6)] public int radius = 3;
    public WorldDifficulty difficulty = WorldDifficulty.Normal;
    [Min(1)] public int maxGenerationAttempts = 250;
    [Min(1)] public int enemyPortraitMinCount = 1;
    [Range(1, 6)] public int enemyPortraitMaxCount = 6;


    [Header("Roguelite Chapters")]
    [Tooltip("로그라이트 1런의 장 수입니다. 현재 기획상 3장 고정입니다.")]
    [Min(1)] public int fixedChapterCount = 3;
    [Tooltip("시작 타일을 제외한 장별 점령 가능 타일 수입니다. 현재 기획상 30개입니다.")]
    [Min(1)] public int chapterNonStartTileCount = 30;
    [Tooltip("장 클리어에 필요한 시작 타일 제외 점령 타일 수입니다. 기본 21/30입니다.")]
    [Min(0)] public int requiredOccupiedTilesForChapterClear = 21;
    [Tooltip("각 장에서 중복 없이 뽑을 최대 팩션 수입니다. 현재 기획상 최대 3개입니다.")]
    [Min(1)] public int maxFactionsPerChapter = 3;
    [Tooltip("각 팩션이 최소로 보유할 타일 수입니다.")]
    [Min(0)] public int minTilesPerChapterFaction = 5;
    [Tooltip("장 클리어 시 군단 전체에 지급할 EXP입니다. 인덱스 0=1장, 1=2장, 2=3장입니다.")]
    public List<int> chapterClearExpRewards = new List<int> { 100, 200, 300 };

    [Header("Roguelite Purple Essence Reward")]
    [Min(0)] public int purpleEssencePerOccupiedTile = 1;
    [Min(0)] public int purpleEssencePerCorruptedUnit = 5;
    [Min(0)] public int purpleEssenceDifficultyBonusEasyPercent = 0;
    [Min(0)] public int purpleEssenceDifficultyBonusNormalPercent = 25;
    [Min(0)] public int purpleEssenceDifficultyBonusHardPercent = 50;

    [Header("Mana Spring Event")]
    [Range(0f, 1f)] public float manaSpringRestorePercentOfMax = 0.3f;

    [Header("Factions")]
    public List<FactionType> enemyFactions = new List<FactionType> { FactionType.FactionA, FactionType.FactionB };
    [Tooltip("월드 생성 시 팩션별 타일 수를 완전 균등이 아니라 무작위 가중치로 배분합니다.")]
    public bool randomizeFactionTileRatios = true;
    [Tooltip("가장 큰 팩션과 가장 작은 팩션의 타일 수 비율 상한입니다. 2면 최대 2:1입니다.")]
    [Min(1f)] public float maxFactionTileRatio = 2f;
    [Tooltip("3개 이상 팩션 확장을 대비한 단일 팩션 최대 점유 비율입니다. 기본값은 약 66.6%입니다.")]
    [Range(0.01f, 1f)] public float maxSingleFactionTileShare = 0.6666667f;
    public List<FactionPresentation> factionPresentations = new List<FactionPresentation>();

    [Header("Events")]
    public WorldEventWeightSettings eventWeightSettings;
    public List<WorldEventPresentation> eventPresentations = new List<WorldEventPresentation>();
    [Tooltip("이벤트별 랜덤 장소 설명 풀입니다. 비어 있으면 기본 텍스트 풀을 사용합니다.")]
    public List<WorldEventDescriptionPool> eventDescriptionPools = new List<WorldEventDescriptionPool>();
    [Tooltip("퀘스트 종류별 랜덤 장소 설명 풀입니다. 비어 있으면 기본 텍스트 풀을 사용합니다.")]
    public List<WorldQuestDescriptionPool> questDescriptionPools = new List<WorldQuestDescriptionPool>();
    [SerializeField] private Sprite startTileIcon;
    public Sprite StartTileIcon => startTileIcon;

    [Header("Battle Event Config")]
    public List<FactionBattleConfig> factionBattleConfigs = new List<FactionBattleConfig>();

    [Header("Settlement & Victory")]
    [Range(0,100)] public int conquestRequiredPercentSmall = 50;
    [Range(0,100)] public int conquestRequiredPercentMedium = 55;
    [Range(0,100)] public int conquestRequiredPercentLarge = 60;
    [Range(0,100)] public int conquestRequiredPercentXLarge = 65;
    public int sizeBonusPercentSmall = 0;
    public int sizeBonusPercentMedium = 10;
    public int sizeBonusPercentLarge = 20;
    public int sizeBonusPercentXLarge = 30;
    public int difficultyBonusPercentEasy = 0;
    public int difficultyBonusPercentNormal = 10;
    public int difficultyBonusPercentHard = 20;
    public int worldVictoryBonusPercent = 20;


    [Header("Mana Crystal")]
    [Tooltip("월드 최대 마나 계산의 기준 정수값입니다. 중형/보통/이전결과 없음이면 이 값이 그대로 최대 마나가 됩니다.")]
    [Min(0)] public int baseMaxMana = 100;
    [Tooltip("radius 3 테스트맵과 radius 4 소형 월드의 마나 배율입니다.")]
    [Min(0)] public int manaSizePercentSmall = 80;
    [Tooltip("radius 5 중형 월드의 마나 배율입니다. 기본 100%입니다.")]
    [Min(0)] public int manaSizePercentMedium = 100;
    [Tooltip("radius 6 대형 월드의 마나 배율입니다.")]
    [Min(0)] public int manaSizePercentLarge = 120;
    [Min(0)] public int manaDifficultyPercentEasy = 120;
    [Min(0)] public int manaDifficultyPercentNormal = 100;
    [Min(0)] public int manaDifficultyPercentHard = 80;
    [Tooltip("이전 월드 결과가 없을 때의 마나 배율입니다. 기본 100% = 보정 없음.")]
    [Min(0)] public int manaPreviousNonePercent = 100;
    [Tooltip("이전 월드 성공 후 다음 월드의 마나 배율입니다. 기본 100% = 보정 없음.")]
    [Min(0)] public int manaPreviousVictoryPercent = 100;
    [Tooltip("이전 월드 실패/중도삭제 후 다음 월드의 마나 배율입니다. 기본 50%.")]
    [Min(0)] public int manaPreviousFailurePercent = 50;

    [Header("Rules")]
    public bool forbidBossNearCenter = true;
    public bool forbidEliteNearCenter = true;

    public Sprite GetFactionTileSprite(FactionType faction)
    {
        FactionPresentation presentation = GetFactionPresentation(faction);
        return presentation != null ? presentation.tileSprite : null;
    }

    public Color GetFactionFallbackColor(FactionType faction)
    {
        FactionPresentation presentation = GetFactionPresentation(faction);
        return presentation != null ? presentation.fallbackColor : Color.white;
    }

    public string GetFactionDisplayName(FactionType faction)
    {
        FactionPresentation presentation = GetFactionPresentation(faction);
        if (presentation != null && !string.IsNullOrWhiteSpace(presentation.displayName))
            return presentation.displayName;
        return faction.ToString();
    }

    public Sprite GetFactionUnknownSprite(FactionType faction)
    {
        FactionPresentation presentation = GetFactionPresentation(faction);
        return presentation != null ? presentation.unknownSprite : null;
    }

    public IReadOnlyList<Sprite> GetFactionEnemyPortraitPool(FactionType faction)
    {
        FactionPresentation presentation = GetFactionPresentation(faction);
        if (presentation == null)
            return Array.Empty<Sprite>();

        List<Sprite> result = new List<Sprite>();
        if (presentation.enemyPortraitPool != null)
        {
            for (int i = 0; i < presentation.enemyPortraitPool.Count; i++)
            {
                if (presentation.enemyPortraitPool[i] != null)
                    result.Add(presentation.enemyPortraitPool[i]);
            }
        }

        if (presentation.units != null)
        {
            for (int i = 0; i < presentation.units.Count; i++)
            {
                FactionUnitPresentationEntry entry = presentation.units[i];
                if (entry == null)
                    continue;

                UnitViewDefinition view = entry.unitViewDefinition;
                if (view == null && entry.unitDefinition != null)
                    view = entry.unitDefinition.defaultViewDefinition;

                Sprite sprite = view != null ? view.GetSlotFaceSprite() : null;
                if (sprite != null && !result.Contains(sprite))
                    result.Add(sprite);
            }
        }

        return result;
    }

    public Sprite GetFactionIcon(FactionType faction)
    {
        FactionPresentation presentation = GetFactionPresentation(faction);
        return presentation != null ? presentation.factionIcon : null;
    }


    public Sprite GetRandomBattleBackground(FactionType faction, WorldTileEventType eventType)
    {
        FactionPresentation presentation = GetFactionPresentation(faction);
        if (presentation == null)
            return null;

        if (eventType == WorldTileEventType.Boss && presentation.bossBattleBackground != null)
            return presentation.bossBattleBackground;

        if (presentation.battleBackgroundPool == null || presentation.battleBackgroundPool.Count == 0)
            return null;

        List<Sprite> valid = new List<Sprite>();
        for (int i = 0; i < presentation.battleBackgroundPool.Count; i++)
        {
            if (presentation.battleBackgroundPool[i] != null)
                valid.Add(presentation.battleBackgroundPool[i]);
        }

        if (valid.Count == 0)
            return null;

        return valid[UnityEngine.Random.Range(0, valid.Count)];
    }

    public bool TryGetFactionForUnit(UnitDefinition unitDefinition, UnitViewDefinition unitViewDefinition, out FactionType faction)
    {
        faction = FactionType.None;
        for (int i = 0; i < factionPresentations.Count; i++)
        {
            FactionPresentation presentation = factionPresentations[i];
            if (presentation == null)
                continue;

            if (presentation.ContainsUnit(unitDefinition, unitViewDefinition))
            {
                faction = presentation.faction;
                return true;
            }
        }

        return false;
    }

    public Sprite GetFactionIconForUnit(UnitDefinition unitDefinition, UnitViewDefinition unitViewDefinition)
    {
        FactionType faction;
        return TryGetFactionForUnit(unitDefinition, unitViewDefinition, out faction) ? GetFactionIcon(faction) : null;
    }

    public Sprite GetTileDisplayIcon(WorldTileData tile)
    {
        if (tile == null)
            return null;

        if (tile.isPlayerStart && StartTileIcon != null)
            return StartTileIcon;

        WorldEventPresentation presentation = GetEventPresentation(tile.eventType);
        if (presentation == null)
            return null;

        if (tile.currentOwner == FactionType.Player && presentation.iconDark != null)
            return presentation.iconDark;

        return presentation.icon;
    }

    public Sprite GetQuestionMarkSprite(WorldTileData tile)
    {
        if (tile == null)
            return null;

        FactionType questionFaction = tile.nativeFaction != FactionType.None ? tile.nativeFaction : tile.currentOwner;
        return GetFactionUnknownSprite(questionFaction);
    }

    public Sprite GetEventIcon(WorldTileEventType eventType)
    {
        WorldEventPresentation presentation = GetEventPresentation(eventType);
        return presentation != null ? presentation.icon : null;
    }

    public Sprite GetEventDarkIcon(WorldTileEventType eventType)
    {
        WorldEventPresentation presentation = GetEventPresentation(eventType);
        return presentation != null ? presentation.iconDark : null;
    }

    public string GetEventDisplayName(WorldTileEventType eventType)
    {
        WorldEventPresentation presentation = GetEventPresentation(eventType);
        if (presentation != null && !string.IsNullOrWhiteSpace(presentation.displayName))
            return presentation.displayName;
        return eventType.ToString();
    }

    public string GetEventDescription(WorldTileEventType eventType)
    {
        WorldEventPresentation presentation = GetEventPresentation(eventType);
        if (presentation != null)
            return presentation.description;
        return string.Empty;
    }

    public string GetOrCreateTileDescription(WorldTileData tile)
    {
        if (tile == null)
            return string.Empty;

        if (!string.IsNullOrWhiteSpace(tile.eventDescriptionText))
            return tile.eventDescriptionText;

        tile.eventDescriptionText = GetRandomEventDescription(tile.eventType);
        return tile.eventDescriptionText;
    }

    public string GetRandomEventDescription(WorldTileEventType eventType)
    {
        string picked = PickFromPool(GetConfiguredEventDescriptionPool(eventType));
        if (!string.IsNullOrWhiteSpace(picked))
            return picked;

        picked = PickFromPool(GetDefaultEventDescriptionPool(eventType));
        if (!string.IsNullOrWhiteSpace(picked))
            return picked;

        return GetEventDescription(eventType);
    }

    public string GetRandomQuestDescription(WorldQuestType questType)
    {
        string picked = PickFromPool(GetConfiguredQuestDescriptionPool(questType));
        if (!string.IsNullOrWhiteSpace(picked))
            return picked;

        picked = PickFromPool(GetDefaultQuestDescriptionPool(questType));
        return picked ?? string.Empty;
    }

    private List<string> GetConfiguredEventDescriptionPool(WorldTileEventType eventType)
    {
        if (eventDescriptionPools == null)
            return null;

        for (int i = 0; i < eventDescriptionPools.Count; i++)
        {
            WorldEventDescriptionPool pool = eventDescriptionPools[i];
            if (pool != null && pool.eventType == eventType)
                return pool.descriptions;
        }

        return null;
    }

    private List<string> GetConfiguredQuestDescriptionPool(WorldQuestType questType)
    {
        if (questDescriptionPools == null)
            return null;

        for (int i = 0; i < questDescriptionPools.Count; i++)
        {
            WorldQuestDescriptionPool pool = questDescriptionPools[i];
            if (pool != null && pool.questType == questType)
                return pool.descriptions;
        }

        return null;
    }

    private string PickFromPool(List<string> pool)
    {
        if (pool == null || pool.Count == 0)
            return string.Empty;

        List<string> valid = new List<string>();
        for (int i = 0; i < pool.Count; i++)
        {
            if (!string.IsNullOrWhiteSpace(pool[i]))
                valid.Add(pool[i]);
        }

        if (valid.Count == 0)
            return string.Empty;

        return valid[UnityEngine.Random.Range(0, valid.Count)];
    }

    private List<string> GetDefaultEventDescriptionPool(WorldTileEventType eventType)
    {
        switch (eventType)
        {
            case WorldTileEventType.Battle:
                return new List<string>
                {
                    "저들은 아직 모르고 있다. 무엇이 다가오는지, 이것이 끝의 시작인지. 알게 되는 것은 이미 늦은 다음일 것이다.",
                    "무기를 들고 맞서려는 자들이 있다. 가상한 일이다. 그러나 맞선다고 막을 수 있는 것과, 그렇지 않은 것은 다르다.",
                    "이 땅에도 지키려는 자들이 있었다. 흔적을 보면 안다. 오래 버텼겠지만 생물의 소진은 필연이다.",
                    "소란스럽다. 저들 나름의 방식으로 대비를 했겠지만, 그것이 얼마나 의미 있는 일이었는지는 곧 알게 될 것이다."
                };
            case WorldTileEventType.EliteBattle:
                return new List<string>
                {
                    "도시를 지키는 주둔군들이 뛰어난 지휘관의 휘하에 모여들지만 가엾은 이들의 발버둥도 여기까지일 것입니다.",
                    "선별된 자들이다. 저들이 내세울 수 있는 가장 강한 자들이 모였지만 그만큼 두려움도 깊다는 뜻이겠지. 그렇기에 무너지는 소리 역시 클 것이다.",
                    "저들로서는 최선이었을 것이다. 훈련된 몸짓, 단단한 대형, 꺾이지 않으려는 눈빛. 그러나 최선 역시 그것을 넘어서는 것 앞에서는 아무 의미가 없다."
                };
            case WorldTileEventType.Rest:
                return new List<string>
                {
                    "달큰한 과실 향이 공기를 채우고 있다. 이 땅이 아직 온기를 품고 있다는 증거겠지만, 그것도 오래가지는 않을 것이다. 군세가 잠시 숨을 고르는 동안, 땅은 조용히 소진되어 간다.",
                    "정갈하게 손질된 침구와 허브 향이 남아 있다. 여기서 쉬어간 자들이 내일을 기약했겠지만, 그 내일이 닿지 않을 수도 있다는 것을 알지 못했을 것이다. 군세는 그 자리를 빌려 휴식을 취한다.",
                    "숲 안쪽, 누군가 다녀간 흔적이 남아 있다. 이미 사라진 자의 것인지, 아직 돌아오지 않은 자의 것인지는 알 수 없다. 군세는 그런 것에 개의치 않고 자리를 잡는다.",
                    "코 끝을 찌르는 유황 냄새와 함께 뜨거운 온천수에서 피어오르는 김이 자욱하다. 곧 이곳에서 야영을 준비하고, 휴식을 취하니 지열의 온기가 온몸 곳곳에 스며들어 군대가 마치 새로워지는 듯 활기를 되찾았다."
                };
            case WorldTileEventType.Boss:
                return new List<string>
                {
                    "빛이 닿지 않는 깊은 곳에서 숨결 소리만이 울려 퍼진다. 오랫동안 이곳을 지배해온 신성한 존재겠지만, 지배란 언제나 더 강한 것에게 넘어가기 마련이다. 이제 그 차례가 왔을 뿐이다.",
                    "눈을 찌르는 빛이 입구부터 쏟아진다. 신성함으로 포장된 마지막 방어선이겠지만, 빛이 밝을수록 그것이 꺼지는 순간은 더 선명하게 남는다. 마지막 수호자들이 기다리고 있다."
                };
            case WorldTileEventType.Treasure:
                return new List<string>
                {
                    "오랜 시간 풍화되어 형체를 알아보기 힘들지만, 유적지인 것만은 확실하다. 그 중에서 갖가지 물건을 모아놓는 창고로 쓰였던 흔적이 남은 곳을 발견했다.",
                    "책자 속 알 수 없는 글자들 사이로 물약 표식들이 눈에 띈다. 연금술이 이루어지던 곳으로 보인다. 온기가 남아 있는 물건들이 놓여 있다.",
                    "눈에 띄지 않게 쌓아놓은 덤불을 치우니 문이 드러난다. 도굴꾼들이 쓰던 창고로 보이며, 팔다 남은 물건들이 어지럽게 흩어져 있다."
                };
            default:
                return null;
        }
    }

    private List<string> GetDefaultQuestDescriptionPool(WorldQuestType questType)
    {
        switch (questType)
        {
            case WorldQuestType.CaptureSpecificTile:
                return new List<string>
                {
                    "오랜 시간 지켜온 흔적이 역력하다. 저들에게는 의미 있는 자리였겠지만, 의미란 힘이 없으면 지켜지지 않는다. 저 깃발이 내려지는 것은 시간문제일 뿐이다.",
                    "겉은 조용하다. 그러나 저들이 무언가를 감추어두었다는 것은 분명하다. 감춘다는 것은 아직 지키고자 하는 것이 남아 있다는 뜻이다. 그것을 찾아내면 된다.",
                    "각기 다른 온기를 지녔던 땅들이 이어져 있다. 아직은 저들의 손길이 남아 있지만, 하나씩 빼앗을수록 이 세계의 색은 서서히 바래간다."
                };
            case WorldQuestType.KillEnemies:
                return new List<string>
                {
                    "세계를 지키려 하는 이들의 흔적이 아직도 곳곳에 흩어져 있다. 끝까지 버티려는 몸부림이 느껴지지만, 결국 모두 사라질 운명일 뿐이다. 이곳에 설치된 제단에 희생자들의 영혼을 끌어모아 오염시키면 정복의 발판이 되는 힘의 원천이 될 것이다.",
                    "충돌이 반복될수록 저들의 방어는 얇아진다. 한 번의 승리가 쌓일 때마다 이 땅에서 저들이 설 자리는 줄어든다. 계속하면 된다."
                };
            case WorldQuestType.WinEliteBattle:
                return new List<string>
                {
                    "단련된 자들이 모여 있다. 저들 중 가장 강한 것들을 골라낸 모양이다. 그러나 정예란 결국 소모되기 위해 존재하는 법."
                };
            case WorldQuestType.WinBossBattle:
                return new List<string>
                {
                    "저들이 중심으로 삼아온 존재다. 오랜 시간 쌓아온 질서와 믿음이 그 하나에 기대어 있다. 그것이 무너지는 순간, 저들에게 남는 것은 없다. 이미 끝난 것이나 다름없다."
                };
            default:
                return null;
        }
    }

    public FactionBattleConfig GetFactionBattleConfig(FactionType faction)
    {
        for (int i = 0; i < factionBattleConfigs.Count; i++)
        {
            FactionBattleConfig config = factionBattleConfigs[i];
            if (config != null && config.faction == faction)
                return config;
        }

        return null;
    }

    public int GetFixedChapterCount()
    {
        return Mathf.Max(1, fixedChapterCount);
    }

    public int GetChapterNonStartTileCount()
    {
        return Mathf.Max(1, chapterNonStartTileCount);
    }

    public int GetChapterTotalTileCount()
    {
        return 1 + GetChapterNonStartTileCount();
    }

    public int GetRequiredOccupiedTilesForChapterClear()
    {
        return Mathf.Clamp(requiredOccupiedTilesForChapterClear, 0, GetChapterNonStartTileCount());
    }

    public int GetChapterClearExpReward(int chapterIndex)
    {
        int index = Mathf.Clamp(chapterIndex - 1, 0, Mathf.Max(0, chapterClearExpRewards != null ? chapterClearExpRewards.Count - 1 : 0));
        if (chapterClearExpRewards == null || chapterClearExpRewards.Count == 0)
            return 0;
        return Mathf.Max(0, chapterClearExpRewards[index]);
    }

    public int GetPurpleEssenceDifficultyBonusPercent()
    {
        switch (difficulty)
        {
            case WorldDifficulty.Easy: return Mathf.Max(0, purpleEssenceDifficultyBonusEasyPercent);
            case WorldDifficulty.Hard: return Mathf.Max(0, purpleEssenceDifficultyBonusHardPercent);
            default: return Mathf.Max(0, purpleEssenceDifficultyBonusNormalPercent);
        }
    }

    public int GetConquestRequiredPercent()
    {
        switch (radius)
        {
            case 3: return conquestRequiredPercentSmall;
            case 4: return conquestRequiredPercentMedium;
            case 5: return conquestRequiredPercentLarge;
            default: return conquestRequiredPercentXLarge;
        }
    }

    public int GetSizeBonusPercent()
    {
        switch (radius)
        {
            case 3: return sizeBonusPercentSmall;
            case 4: return sizeBonusPercentMedium;
            case 5: return sizeBonusPercentLarge;
            default: return sizeBonusPercentXLarge;
        }
    }

    public int GetDifficultyBonusPercent()
    {
        switch (difficulty)
        {
            case WorldDifficulty.Easy: return difficultyBonusPercentEasy;
            case WorldDifficulty.Hard: return difficultyBonusPercentHard;
            default: return difficultyBonusPercentNormal;
        }
    }


    public int GetBattleRewardSizeBonusPercent()
    {
        // radius 3은 초소형 테스트맵으로 취급한다. 실제 보상 기준은 4/5/6이다.
        if (radius <= 4)
            return 0;
        if (radius == 5)
            return 50;
        return 100;
    }

    public int GetBattleRewardCombatBonusPercent(WorldTileEventType eventType)
    {
        if (eventType == WorldTileEventType.EliteBattle)
            return 20;
        if (eventType == WorldTileEventType.Boss)
            return 50;
        return 0;
    }


    public int CalculateMaxMana(WorldSettlementResultState previousResult)
    {
        int baseValue = Mathf.Max(0, baseMaxMana);
        if (baseValue <= 0)
            return 0;

        int sizePercent = GetManaSizePercent();
        int difficultyPercent = GetManaDifficultyPercent();
        // 로그라이트 구조에서는 이전 월드 성공/실패 기록으로 새 런의 최대 마나를 제한하지 않는다.
        int totalPercent = 100 + (sizePercent - 100) + (difficultyPercent - 100);
        totalPercent = Mathf.Max(0, totalPercent);
        return Mathf.Max(0, Mathf.RoundToInt(baseValue * (totalPercent * 0.01f)));
    }

    public int GetManaSizePercent()
    {
        if (radius <= 4)
            return Mathf.Max(0, manaSizePercentSmall);
        if (radius == 5)
            return Mathf.Max(0, manaSizePercentMedium);
        return Mathf.Max(0, manaSizePercentLarge);
    }

    public int GetManaDifficultyPercent()
    {
        switch (difficulty)
        {
            case WorldDifficulty.Easy: return Mathf.Max(0, manaDifficultyPercentEasy);
            case WorldDifficulty.Hard: return Mathf.Max(0, manaDifficultyPercentHard);
            default: return Mathf.Max(0, manaDifficultyPercentNormal);
        }
    }

    public int GetManaPreviousResultPercent(WorldSettlementResultState previousResult)
    {
        switch (previousResult)
        {
            case WorldSettlementResultState.Victory: return Mathf.Max(0, manaPreviousVictoryPercent);
            case WorldSettlementResultState.Failure: return Mathf.Max(0, manaPreviousFailurePercent);
            default: return Mathf.Max(0, manaPreviousNonePercent);
        }
    }

    private FactionPresentation GetFactionPresentation(FactionType faction)
    {
        for (int i = 0; i < factionPresentations.Count; i++)
        {
            if (factionPresentations[i] != null && factionPresentations[i].faction == faction)
                return factionPresentations[i];
        }
        return null;
    }

    private WorldEventPresentation GetEventPresentation(WorldTileEventType eventType)
    {
        for (int i = 0; i < eventPresentations.Count; i++)
        {
            if (eventPresentations[i] != null && eventPresentations[i].eventType == eventType)
                return eventPresentations[i];
        }
        return null;
    }
}

[Serializable]
public class FactionPresentation
{
    public FactionType faction = FactionType.None;
    public string displayName;
    public Sprite factionIcon;
    public Sprite tileSprite;
    public Sprite unknownSprite;
    public Color fallbackColor = Color.white;
    public List<Sprite> enemyPortraitPool = new List<Sprite>();

    [Header("Battle Backgrounds")]
    [Tooltip("일반/정예 전투 진입 시 이 팩션의 배경 후보입니다. 전투마다 무작위 1장을 사용합니다.")]
    public List<Sprite> battleBackgroundPool = new List<Sprite>();
    [Tooltip("보스 전투에 사용할 이 팩션의 고유 배경입니다. 비어 있으면 일반 배경 풀에서 무작위 선택합니다.")]
    public Sprite bossBattleBackground;

    [Tooltip("이 팩션에 속하는 유닛과 기본 ViewDefinition입니다. 적 정보 패널의 팩션 아이콘과 월드 타일 적 프리뷰 보정에 사용합니다.")]
    public List<FactionUnitPresentationEntry> units = new List<FactionUnitPresentationEntry>();

    public bool ContainsUnit(UnitDefinition unitDefinition, UnitViewDefinition unitViewDefinition)
    {
        if (units == null)
            return false;

        for (int i = 0; i < units.Count; i++)
        {
            FactionUnitPresentationEntry entry = units[i];
            if (entry == null)
                continue;

            if (unitDefinition != null && entry.unitDefinition == unitDefinition)
                return true;
            if (unitViewDefinition != null && entry.unitViewDefinition == unitViewDefinition)
                return true;
        }

        return false;
    }
}

[Serializable]
public class FactionUnitPresentationEntry
{
    public UnitDefinition unitDefinition;
    public UnitViewDefinition unitViewDefinition;
}

[Serializable]
public class WorldEventPresentation
{
    public WorldTileEventType eventType = WorldTileEventType.None;
    public string displayName;
    [TextArea(2, 5)] public string description;
    public Sprite icon;
    public Sprite iconDark;
}

[Serializable]
public class WorldEventDescriptionPool
{
    public WorldTileEventType eventType = WorldTileEventType.None;
    [TextArea(2, 6)] public List<string> descriptions = new List<string>();
}

[Serializable]
public class WorldQuestDescriptionPool
{
    public WorldQuestType questType = WorldQuestType.KillEnemies;
    [TextArea(2, 6)] public List<string> descriptions = new List<string>();
}

[Serializable]
public class FactionBattleConfig
{
    public FactionType faction = FactionType.None;

    [Header("Normal Battle Tables")]
    public EnemyEncounterTable battleTier1Table;
    public EnemyEncounterTable battleTier2Table;
    public EnemyEncounterTable battleTier3Table;

    [Header("Elite Battle Tables")]
    public EnemyEncounterTable eliteTier1Table;
    public EnemyEncounterTable eliteTier2Table;
    public EnemyEncounterTable eliteTier3Table;

    [Header("Boss")]
    public PartyDefinition bossPartyDefinition;
    public EnemyEncounterTable bossEncounterTable;

    public EnemyEncounterTable GetEncounterTable(WorldTileEventType eventType, int tierIndex)
    {
        int tier = Mathf.Clamp(tierIndex, 0, 2);

        if (eventType == WorldTileEventType.Battle)
        {
            switch (tier)
            {
                case 0: return battleTier1Table;
                case 1: return battleTier2Table;
                default: return battleTier3Table;
            }
        }

        if (eventType == WorldTileEventType.EliteBattle)
        {
            switch (tier)
            {
                case 0: return eliteTier1Table;
                case 1: return eliteTier2Table;
                default: return eliteTier3Table;
            }
        }

        if (eventType == WorldTileEventType.Boss)
            return bossEncounterTable;

        return null;
    }
}
