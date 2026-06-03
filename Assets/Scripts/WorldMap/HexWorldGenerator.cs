using System.Collections.Generic;
using UnityEngine;

public class HexWorldGenerator
{
    private readonly WorldGenerationSettings settings;

    public HexWorldGenerator(WorldGenerationSettings settings)
    {
        this.settings = settings;
    }

    public WorldMapData Generate()
    {
        if (settings == null)
        {
            Debug.LogError("[HexWorldGenerator] WorldGenerationSettings is null.");
            return null;
        }

        for (int attempt = 0; attempt < Mathf.Max(1, settings.maxGenerationAttempts); attempt++)
        {
            List<FactionType> enemyFactions = PickChapterFactions();
            if (enemyFactions.Count <= 0)
            {
                Debug.LogError("[HexWorldGenerator] At least one enemy faction is required.");
                return null;
            }

            WorldMapData mapData = TryGenerateOnce(enemyFactions);
            if (mapData != null)
                return mapData;
        }

        Debug.LogError("[HexWorldGenerator] Failed to generate a valid roguelite chapter map.");
        return null;
    }

    private WorldMapData TryGenerateOnce(List<FactionType> enemyFactions)
    {
        WorldMapData mapData = CreateBaseMap();
        if (mapData == null)
            return null;

        WorldTileData startTile = mapData.GetStartTile();
        if (startTile == null)
            return null;

        int[] allocations = CreateFactionAllocations(mapData.tiles.Count - 1, enemyFactions.Count);
        if (!AssignFactionTerritories(mapData, enemyFactions, allocations))
            return null;

        if (!AssignEvents(mapData, startTile, enemyFactions))
            return null;

        mapData.RebuildLookup();
        return mapData;
    }

    private WorldMapData CreateBaseMap()
    {
        int totalTileCount = settings.GetChapterTotalTileCount();
        HashSet<HexCoord> coords = new HashSet<HexCoord>();
        List<HexCoord> coordList = new List<HexCoord>();
        HexCoord start = new HexCoord(0, 0);
        coords.Add(start);
        coordList.Add(start);

        int guard = Mathf.Max(200, totalTileCount * 100);
        while (coordList.Count < totalTileCount && guard-- > 0)
        {
            HexCoord seed = coordList[Random.Range(0, coordList.Count)];
            List<HexCoord> neighbors = seed.GetNeighbors();
            Shuffle(neighbors);

            for (int i = 0; i < neighbors.Count && coordList.Count < totalTileCount; i++)
            {
                HexCoord candidate = neighbors[i];
                if (coords.Contains(candidate))
                    continue;

                coords.Add(candidate);
                coordList.Add(candidate);
                break;
            }
        }

        if (coordList.Count < totalTileCount)
            return null;

        // 보기 좋은 ID 순서를 위해 시작 타일을 0번으로 두고 나머지는 거리/좌표 기준으로 정렬한다.
        coordList.Sort((a, b) =>
        {
            bool aStart = a.q == 0 && a.r == 0;
            bool bStart = b.q == 0 && b.r == 0;
            if (aStart != bStart)
                return aStart ? -1 : 1;

            int da = HexCoord.Distance(start, a);
            int db = HexCoord.Distance(start, b);
            if (da != db)
                return da.CompareTo(db);
            if (a.q != b.q)
                return a.q.CompareTo(b.q);
            return a.r.CompareTo(b.r);
        });

        WorldMapData mapData = new WorldMapData
        {
            radius = Mathf.Max(1, settings.radius),
        };

        for (int i = 0; i < coordList.Count; i++)
        {
            HexCoord coord = coordList[i];
            bool isStart = coord.q == 0 && coord.r == 0;
            WorldTileData tile = new WorldTileData
            {
                tileId = i,
                coord = coord,
                nativeFaction = isStart ? FactionType.Player : FactionType.None,
                currentOwner = isStart ? FactionType.Player : FactionType.None,
                eventType = WorldTileEventType.None,
                revealed = isStart,
                isPlayerStart = isStart,
                isResolved = false,
                isIconDisabled = false,
            };

            if (isStart)
                mapData.startTileId = tile.tileId;

            mapData.tiles.Add(tile);
        }

        mapData.RebuildLookup();
        return mapData;
    }

    private List<FactionType> PickChapterFactions()
    {
        List<FactionType> pool = GetValidEnemyFactions();
        Shuffle(pool);
        int count = Mathf.Clamp(settings.maxFactionsPerChapter, 1, Mathf.Max(1, pool.Count));
        if (pool.Count < count)
            count = pool.Count;

        List<FactionType> result = new List<FactionType>();
        for (int i = 0; i < pool.Count && result.Count < count; i++)
            result.Add(pool[i]);
        return result;
    }

    private List<FactionType> GetValidEnemyFactions()
    {
        List<FactionType> result = new List<FactionType>();
        if (settings.enemyFactions == null)
            return result;

        for (int i = 0; i < settings.enemyFactions.Count; i++)
        {
            FactionType faction = settings.enemyFactions[i];
            if (faction == FactionType.None || faction == FactionType.Player)
                continue;
            if (!result.Contains(faction))
                result.Add(faction);
        }

        return result;
    }

    private int[] CreateFactionAllocations(int availableTileCount, int factionCount)
    {
        int[] result = new int[factionCount];
        if (availableTileCount <= 0 || factionCount <= 0)
            return result;

        int min = Mathf.Max(0, settings.minTilesPerChapterFaction);
        if (min * factionCount > availableTileCount)
            min = availableTileCount / factionCount;

        int assigned = 0;
        for (int i = 0; i < factionCount; i++)
        {
            result[i] = min;
            assigned += min;
        }

        while (assigned < availableTileCount)
        {
            int index = Random.Range(0, factionCount);
            result[index]++;
            assigned++;
        }

        return result;
    }

    private bool AssignFactionTerritories(WorldMapData mapData, List<FactionType> enemyFactions, int[] allocations)
    {
        if (mapData == null || enemyFactions == null || allocations == null || enemyFactions.Count != allocations.Length)
            return false;

        List<WorldTileData> availableTiles = new List<WorldTileData>();
        for (int i = 0; i < mapData.tiles.Count; i++)
        {
            WorldTileData tile = mapData.tiles[i];
            if (tile != null && !tile.isPlayerStart)
                availableTiles.Add(tile);
        }

        Shuffle(availableTiles);
        int cursor = 0;
        for (int factionIndex = 0; factionIndex < enemyFactions.Count; factionIndex++)
        {
            FactionType faction = enemyFactions[factionIndex];
            int count = Mathf.Max(0, allocations[factionIndex]);
            for (int i = 0; i < count && cursor < availableTiles.Count; i++)
            {
                WorldTileData tile = availableTiles[cursor++];
                tile.nativeFaction = faction;
                tile.currentOwner = faction;
            }
        }

        while (cursor < availableTiles.Count)
        {
            FactionType faction = enemyFactions[Random.Range(0, enemyFactions.Count)];
            WorldTileData tile = availableTiles[cursor++];
            tile.nativeFaction = faction;
            tile.currentOwner = faction;
        }

        return true;
    }

    private bool AssignEvents(WorldMapData mapData, WorldTileData startTile, List<FactionType> enemyFactions)
    {
        List<WorldTileData> pool = new List<WorldTileData>();
        for (int i = 0; i < mapData.tiles.Count; i++)
        {
            WorldTileData tile = mapData.tiles[i];
            if (tile != null && !tile.isPlayerStart)
                pool.Add(tile);
        }

        HashSet<int> blockedNearCenterIds = new HashSet<int>();
        List<WorldTileData> startNeighbors = mapData.GetNeighbors(startTile);
        for (int i = 0; i < startNeighbors.Count; i++)
            blockedNearCenterIds.Add(startNeighbors[i].tileId);

        AssignSingleGraveyard(pool);

        if (!AssignOneBossForChapter(mapData, enemyFactions, blockedNearCenterIds))
            return false;

        return AssignWeightedEvents(mapData, blockedNearCenterIds);
    }

    private void AssignSingleGraveyard(List<WorldTileData> pool)
    {
        List<WorldTileData> candidates = new List<WorldTileData>();
        for (int i = 0; i < pool.Count; i++)
        {
            WorldTileData tile = pool[i];
            if (tile != null && tile.eventType == WorldTileEventType.None)
                candidates.Add(tile);
        }

        if (candidates.Count == 0)
            return;

        WorldTileData selected = candidates[Random.Range(0, candidates.Count)];
        selected.eventType = WorldTileEventType.Graveyard;
        selected.eventDescriptionText = settings.GetOrCreateTileDescription(selected);
        selected.previewEnemyPortraits.Clear();
    }

    private bool AssignOneBossForChapter(WorldMapData mapData, List<FactionType> enemyFactions, HashSet<int> blockedNearCenterIds)
    {
        if (enemyFactions == null || enemyFactions.Count == 0)
            return false;

        List<FactionType> factionOrder = new List<FactionType>(enemyFactions);
        Shuffle(factionOrder);

        for (int f = 0; f < factionOrder.Count; f++)
        {
            FactionType faction = factionOrder[f];
            List<WorldTileData> candidates = new List<WorldTileData>();
            List<WorldTileData> factionTiles = mapData.GetTilesByNativeFaction(faction);
            for (int i = 0; i < factionTiles.Count; i++)
            {
                WorldTileData tile = factionTiles[i];
                if (tile == null || tile.eventType != WorldTileEventType.None)
                    continue;
                if (settings.forbidBossNearCenter && blockedNearCenterIds.Contains(tile.tileId))
                    continue;
                candidates.Add(tile);
            }

            if (candidates.Count == 0)
                continue;

            WorldTileData selected = candidates[Random.Range(0, candidates.Count)];
            selected.eventType = WorldTileEventType.Boss;
            selected.eventDescriptionText = settings.GetOrCreateTileDescription(selected);
            selected.previewEnemyPortraits = BuildEnemyPreviewList(faction, true);
            return true;
        }

        return false;
    }

    private bool AssignWeightedEvents(WorldMapData mapData, HashSet<int> blockedNearCenterIds)
    {
        WorldEventWeightSettings weights = settings.eventWeightSettings;
        if (weights == null)
        {
            Debug.LogError("[HexWorldGenerator] WorldEventWeightSettings is missing.");
            return false;
        }

        List<WorldTileData> unassigned = new List<WorldTileData>();
        for (int i = 0; i < mapData.tiles.Count; i++)
        {
            WorldTileData tile = mapData.tiles[i];
            if (tile != null && tile.eventType == WorldTileEventType.None && !tile.isPlayerStart)
                unassigned.Add(tile);
        }

        for (int i = 0; i < unassigned.Count; i++)
        {
            WorldTileData tile = unassigned[i];
            WorldTileEventType eventType = PickWeightedEventType(weights, tile, blockedNearCenterIds);
            tile.eventType = eventType;
            tile.eventDescriptionText = settings.GetOrCreateTileDescription(tile);
            tile.previewEnemyPortraits = tile.IsCombatEvent
                ? BuildEnemyPreviewList(tile.nativeFaction, eventType == WorldTileEventType.Boss)
                : new List<Sprite>();
        }

        return true;
    }

    private WorldTileEventType PickWeightedEventType(WorldEventWeightSettings weights, WorldTileData tile, HashSet<int> blockedNearCenterIds)
    {
        List<WorldTileEventType> candidates = new List<WorldTileEventType>
        {
            WorldTileEventType.Battle,
            WorldTileEventType.Rest,
            WorldTileEventType.Treasure,
            WorldTileEventType.Merchant,
            WorldTileEventType.Quest,
            WorldTileEventType.EliteBattle,
            WorldTileEventType.ManaSpring,
        };

        int totalWeight = 0;
        List<int> weightValues = new List<int>(candidates.Count);
        for (int i = 0; i < candidates.Count; i++)
        {
            WorldTileEventType eventType = candidates[i];
            int weight = weights.GetWeight(eventType);
            if (eventType == WorldTileEventType.EliteBattle && settings.forbidEliteNearCenter && blockedNearCenterIds.Contains(tile.tileId))
                weight = 0;
            weightValues.Add(Mathf.Max(0, weight));
            totalWeight += Mathf.Max(0, weight);
        }

        if (totalWeight <= 0)
            return WorldTileEventType.Battle;

        int roll = Random.Range(0, totalWeight);
        int running = 0;
        for (int i = 0; i < candidates.Count; i++)
        {
            running += weightValues[i];
            if (roll < running)
                return candidates[i];
        }

        return WorldTileEventType.Battle;
    }

    private List<Sprite> BuildEnemyPreviewList(FactionType faction, bool isBoss)
    {
        Dictionary<Sprite, int> weights = new Dictionary<Sprite, int>();

        FactionBattleConfig config = settings.GetFactionBattleConfig(faction);
        if (config != null)
        {
            if (isBoss)
            {
                AddWeightedSpritesFromPartyDefinition(weights, config.bossPartyDefinition, 100);
                AddWeightedSpritesFromEncounterTable(weights, config.bossEncounterTable);
            }
            else
            {
                AddWeightedSpritesFromEncounterTable(weights, config.battleTier1Table);
                AddWeightedSpritesFromEncounterTable(weights, config.battleTier2Table);
                AddWeightedSpritesFromEncounterTable(weights, config.battleTier3Table);
                AddWeightedSpritesFromEncounterTable(weights, config.eliteTier1Table);
                AddWeightedSpritesFromEncounterTable(weights, config.eliteTier2Table);
                AddWeightedSpritesFromEncounterTable(weights, config.eliteTier3Table);
            }
        }

        if (weights.Count == 0)
        {
            IReadOnlyList<Sprite> fallbackPool = settings.GetFactionEnemyPortraitPool(faction);
            if (fallbackPool != null)
            {
                for (int i = 0; i < fallbackPool.Count; i++)
                    AddWeightedSprite(weights, fallbackPool[i], 1);
            }
        }

        return BuildTopFourPortraits(weights);
    }

    private void AddWeightedSpritesFromEncounterTable(Dictionary<Sprite, int> target, EnemyEncounterTable table)
    {
        if (target == null || table == null || table.entries == null)
            return;

        for (int i = 0; i < table.entries.Count; i++)
        {
            EnemyEncounterEntry entry = table.entries[i];
            if (entry == null || !entry.enabled || entry.unitViewDefinition == null)
                continue;

            AddWeightedSprite(target, entry.unitViewDefinition.GetSlotFaceSprite(), Mathf.Max(1, entry.weight));
        }
    }

    private void AddWeightedSpritesFromPartyDefinition(Dictionary<Sprite, int> target, PartyDefinition party, int weightPerMember)
    {
        if (target == null || party == null || party.members == null)
            return;

        for (int i = 0; i < party.members.Count; i++)
        {
            PartyMemberData member = party.members[i];
            if (member == null || member.unitViewDefinition == null)
                continue;

            AddWeightedSprite(target, member.unitViewDefinition.GetSlotFaceSprite(), Mathf.Max(1, weightPerMember));
        }
    }

    private void AddWeightedSprite(Dictionary<Sprite, int> target, Sprite sprite, int weight)
    {
        if (target == null || sprite == null)
            return;

        int current;
        target.TryGetValue(sprite, out current);
        target[sprite] = current + Mathf.Max(1, weight);
    }

    private List<Sprite> BuildTopFourPortraits(Dictionary<Sprite, int> weights)
    {
        List<Sprite> result = new List<Sprite>();
        if (weights == null || weights.Count == 0)
            return result;

        List<KeyValuePair<Sprite, int>> ordered = new List<KeyValuePair<Sprite, int>>(weights);
        ordered.Sort((a, b) => b.Value.CompareTo(a.Value));

        for (int i = 0; i < ordered.Count && result.Count < 4; i++)
        {
            if (ordered[i].Key != null)
                result.Add(ordered[i].Key);
        }

        return result;
    }

    private void Shuffle<T>(IList<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}