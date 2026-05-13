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

        List<FactionType> enemyFactions = GetValidEnemyFactions();
        if (enemyFactions.Count <= 0)
        {
            Debug.LogError("[HexWorldGenerator] At least one enemy faction is required.");
            return null;
        }

        for (int attempt = 0; attempt < Mathf.Max(1, settings.maxGenerationAttempts); attempt++)
        {
            WorldMapData mapData = TryGenerateOnce(enemyFactions);
            if (mapData != null)
                return mapData;
        }

        Debug.LogError("[HexWorldGenerator] Failed to generate a valid world map.");
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
        WorldMapData mapData = new WorldMapData
        {
            radius = settings.radius,
        };

        int nextId = 0;
        int worldRadius = settings.radius - 1;

        for (int q = -worldRadius; q <= worldRadius; q++)
        {
            int rMin = Mathf.Max(-worldRadius, -q - worldRadius);
            int rMax = Mathf.Min(worldRadius, -q + worldRadius);

            for (int r = rMin; r <= rMax; r++)
            {
                WorldTileData tile = new WorldTileData
                {
                    tileId = nextId++,
                    coord = new HexCoord(q, r),
                    nativeFaction = FactionType.None,
                    currentOwner = FactionType.None,
                    eventType = WorldTileEventType.None,
                    revealed = false,
                    isPlayerStart = (q == 0 && r == 0),
                    isResolved = false,
                    isIconDisabled = false,
                };

                if (tile.isPlayerStart)
                {
                    tile.nativeFaction = FactionType.Player;
                    tile.currentOwner = FactionType.Player;
                    tile.revealed = true;
                    mapData.startTileId = tile.tileId;
                }

                mapData.tiles.Add(tile);
            }
        }

        mapData.RebuildLookup();
        return mapData;
    }

    private int[] CreateFactionAllocations(int availableTileCount, int factionCount)
    {
        int[] result = new int[factionCount];
        if (availableTileCount <= 0 || factionCount <= 0)
            return result;

        if (!settings.randomizeFactionTileRatios || factionCount == 1)
            return CreateEvenFactionAllocations(availableTileCount, factionCount);

        float[] weights = CreateBoundedRandomFactionWeights(factionCount);
        float totalWeight = 0f;
        for (int i = 0; i < weights.Length; i++)
            totalWeight += Mathf.Max(0.0001f, weights[i]);

        int assigned = 0;
        float[] fractional = new float[factionCount];
        for (int i = 0; i < factionCount; i++)
        {
            float exact = availableTileCount * (weights[i] / totalWeight);
            int count = Mathf.FloorToInt(exact);

            // 각 팩션은 최소 1개 시드 타일이 필요하다.
            if (availableTileCount >= factionCount)
                count = Mathf.Max(1, count);

            result[i] = count;
            assigned += count;
            fractional[i] = exact - Mathf.Floor(exact);
        }

        while (assigned > availableTileCount)
        {
            int index = FindLargestAllocationIndexAboveMinimum(result, availableTileCount >= factionCount ? 1 : 0);
            if (index < 0)
                break;
            result[index]--;
            assigned--;
        }

        while (assigned < availableTileCount)
        {
            int index = FindLargestFractionIndex(fractional, result);
            result[index]++;
            fractional[index] = 0f;
            assigned++;
        }

        EnforceFactionAllocationBounds(result, availableTileCount);
        return result;
    }

    private int[] CreateEvenFactionAllocations(int availableTileCount, int factionCount)
    {
        int[] result = new int[factionCount];
        int baseCount = availableTileCount / factionCount;
        int remainder = availableTileCount % factionCount;

        for (int i = 0; i < factionCount; i++)
            result[i] = baseCount;

        List<int> indices = new List<int>(factionCount);
        for (int i = 0; i < factionCount; i++)
            indices.Add(i);

        Shuffle(indices);
        for (int i = 0; i < remainder; i++)
            result[indices[i]]++;

        return result;
    }

    private float[] CreateBoundedRandomFactionWeights(int factionCount)
    {
        float[] weights = new float[factionCount];
        float maxRatio = Mathf.Max(1f, settings.maxFactionTileRatio);
        float maxShare = Mathf.Clamp(settings.maxSingleFactionTileShare, 0.01f, 1f);

        for (int attempt = 0; attempt < 100; attempt++)
        {
            float total = 0f;
            for (int i = 0; i < factionCount; i++)
            {
                weights[i] = Random.Range(1f, maxRatio);
                total += weights[i];
            }

            if (total <= 0f)
                continue;

            float largestShare = 0f;
            for (int i = 0; i < factionCount; i++)
                largestShare = Mathf.Max(largestShare, weights[i] / total);

            if (largestShare <= maxShare || factionCount <= 2)
                return weights;
        }

        // 이론상 maxRatio 2:1이면 3개 이상 팩션에서 66%를 넘기 어렵지만,
        // 설정값을 극단적으로 바꿨을 때도 안전하게 균등 분배로 폴백한다.
        for (int i = 0; i < factionCount; i++)
            weights[i] = 1f;
        return weights;
    }

    private int FindLargestFractionIndex(float[] fractional, int[] allocations)
    {
        int bestIndex = 0;
        float bestValue = -1f;
        for (int i = 0; i < fractional.Length; i++)
        {
            float value = fractional[i];
            if (value > bestValue)
            {
                bestValue = value;
                bestIndex = i;
            }
            else if (Mathf.Approximately(value, bestValue) && allocations[i] < allocations[bestIndex])
            {
                bestIndex = i;
            }
        }
        return bestIndex;
    }

    private int FindLargestAllocationIndexAboveMinimum(int[] allocations, int minimum)
    {
        int bestIndex = -1;
        int bestValue = int.MinValue;
        for (int i = 0; i < allocations.Length; i++)
        {
            if (allocations[i] <= minimum)
                continue;

            if (allocations[i] > bestValue)
            {
                bestValue = allocations[i];
                bestIndex = i;
            }
        }
        return bestIndex;
    }

    private void EnforceFactionAllocationBounds(int[] allocations, int totalTileCount)
    {
        if (allocations == null || allocations.Length <= 1 || totalTileCount <= 0)
            return;

        float maxRatio = Mathf.Max(1f, settings.maxFactionTileRatio);
        float maxShare = Mathf.Clamp(settings.maxSingleFactionTileShare, 0.01f, 1f);
        int minimum = totalTileCount >= allocations.Length ? 1 : 0;
        int guard = Mathf.Max(10, totalTileCount * allocations.Length * 4);

        while (guard-- > 0)
        {
            int maxIndex = 0;
            int minIndex = 0;
            for (int i = 1; i < allocations.Length; i++)
            {
                if (allocations[i] > allocations[maxIndex])
                    maxIndex = i;
                if (allocations[i] < allocations[minIndex])
                    minIndex = i;
            }

            int maxValue = allocations[maxIndex];
            int minValue = allocations[minIndex];
            float ratio = minValue > 0 ? maxValue / (float)minValue : float.PositiveInfinity;
            float share = maxValue / (float)totalTileCount;
            bool violatesRatio = ratio > maxRatio + 0.0001f;
            bool violatesShare = allocations.Length >= 3 && share > maxShare + 0.0001f;

            if (!violatesRatio && !violatesShare)
                break;

            if (maxValue <= minimum)
                break;

            allocations[maxIndex]--;
            allocations[minIndex]++;
        }
    }

    private bool AssignFactionTerritories(WorldMapData mapData, List<FactionType> enemyFactions, int[] allocations)
    {
        List<WorldTileData> availableTiles = new List<WorldTileData>();
        for (int i = 0; i < mapData.tiles.Count; i++)
        {
            WorldTileData tile = mapData.tiles[i];
            if (tile != null && !tile.isPlayerStart)
                availableTiles.Add(tile);
        }

        Shuffle(availableTiles);

        Dictionary<FactionType, List<WorldTileData>> territories = new Dictionary<FactionType, List<WorldTileData>>();
        Dictionary<FactionType, HashSet<int>> territoryIds = new Dictionary<FactionType, HashSet<int>>();

        for (int i = 0; i < enemyFactions.Count; i++)
        {
            territories[enemyFactions[i]] = new List<WorldTileData>();
            territoryIds[enemyFactions[i]] = new HashSet<int>();
        }

        List<WorldTileData> unusedSeedCandidates = new List<WorldTileData>(availableTiles);
        for (int i = 0; i < enemyFactions.Count; i++)
        {
            if (allocations[i] <= 0 || unusedSeedCandidates.Count <= 0)
                return false;

            WorldTileData seed = unusedSeedCandidates[0];
            unusedSeedCandidates.RemoveAt(0);
            AssignTileToFaction(seed, enemyFactions[i], territories, territoryIds);
        }

        int assignedCount = enemyFactions.Count;
        int totalNeeded = availableTiles.Count;
        int guard = totalNeeded * 30;

        while (assignedCount < totalNeeded && guard-- > 0)
        {
            bool progress = false;
            List<int> factionOrder = new List<int>(enemyFactions.Count);
            for (int i = 0; i < enemyFactions.Count; i++)
                factionOrder.Add(i);
            Shuffle(factionOrder);

            for (int i = 0; i < factionOrder.Count; i++)
            {
                int factionIndex = factionOrder[i];
                FactionType faction = enemyFactions[factionIndex];
                if (territories[faction].Count >= allocations[factionIndex])
                    continue;

                WorldTileData nextTile = FindExpansionTile(mapData, territories[faction], territoryIds[faction]);
                if (nextTile == null)
                    continue;

                AssignTileToFaction(nextTile, faction, territories, territoryIds);
                assignedCount++;
                progress = true;
            }

            if (!progress)
                return false;
        }

        return assignedCount == totalNeeded;
    }

    private WorldTileData FindExpansionTile(WorldMapData mapData, List<WorldTileData> territory, HashSet<int> territoryIds)
    {
        List<WorldTileData> shuffledTerritory = new List<WorldTileData>(territory);
        Shuffle(shuffledTerritory);

        for (int i = 0; i < shuffledTerritory.Count; i++)
        {
            List<WorldTileData> neighbors = mapData.GetNeighbors(shuffledTerritory[i]);
            Shuffle(neighbors);
            for (int n = 0; n < neighbors.Count; n++)
            {
                WorldTileData candidate = neighbors[n];
                if (candidate.isPlayerStart)
                    continue;
                if (candidate.nativeFaction != FactionType.None)
                    continue;
                if (territoryIds.Contains(candidate.tileId))
                    continue;
                return candidate;
            }
        }

        return null;
    }

    private void AssignTileToFaction(
        WorldTileData tile,
        FactionType faction,
        Dictionary<FactionType, List<WorldTileData>> territories,
        Dictionary<FactionType, HashSet<int>> territoryIds)
    {
        tile.nativeFaction = faction;
        tile.currentOwner = faction;
        territories[faction].Add(tile);
        territoryIds[faction].Add(tile.tileId);
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

        if (!AssignSingleGraveyard(pool, blockedNearCenterIds))
            return false;

        for (int i = 0; i < enemyFactions.Count; i++)
        {
            if (!AssignSingleBoss(mapData, enemyFactions[i], blockedNearCenterIds))
                return false;
        }

        return AssignWeightedEvents(mapData, blockedNearCenterIds);
    }

    private bool AssignSingleGraveyard(List<WorldTileData> pool, HashSet<int> blockedNearCenterIds)
    {
        List<WorldTileData> candidates = new List<WorldTileData>();
        for (int i = 0; i < pool.Count; i++)
        {
            WorldTileData tile = pool[i];
            if (tile.eventType != WorldTileEventType.None)
                continue;
            candidates.Add(tile);
        }

        if (candidates.Count == 0)
            return false;

        WorldTileData selected = candidates[Random.Range(0, candidates.Count)];
        selected.eventType = WorldTileEventType.Graveyard;
        selected.eventDescriptionText = settings.GetOrCreateTileDescription(selected);
        selected.previewEnemyPortraits.Clear();
        return true;
    }

    private bool AssignSingleBoss(WorldMapData mapData, FactionType faction, HashSet<int> blockedNearCenterIds)
    {
        List<WorldTileData> candidates = new List<WorldTileData>();
        List<WorldTileData> factionTiles = mapData.GetTilesByNativeFaction(faction);

        for (int i = 0; i < factionTiles.Count; i++)
        {
            WorldTileData tile = factionTiles[i];
            if (tile.eventType != WorldTileEventType.None)
                continue;
            if (settings.forbidBossNearCenter && blockedNearCenterIds.Contains(tile.tileId))
                continue;
            candidates.Add(tile);
        }

        if (candidates.Count == 0)
            return false;

        WorldTileData selected = candidates[Random.Range(0, candidates.Count)];
        selected.eventType = WorldTileEventType.Boss;
        selected.eventDescriptionText = settings.GetOrCreateTileDescription(selected);
        selected.previewEnemyPortraits = BuildEnemyPreviewList(faction, true);
        return true;
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
        };

        int totalWeight = 0;
        List<int> weightValues = new List<int>(candidates.Count);

        for (int i = 0; i < candidates.Count; i++)
        {
            WorldTileEventType eventType = candidates[i];
            int weight = weights.GetWeight(eventType);

            if (eventType == WorldTileEventType.EliteBattle && settings.forbidEliteNearCenter && blockedNearCenterIds.Contains(tile.tileId))
                weight = 0;

            weightValues.Add(weight);
            totalWeight += weight;
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

    private void Shuffle<T>(IList<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}