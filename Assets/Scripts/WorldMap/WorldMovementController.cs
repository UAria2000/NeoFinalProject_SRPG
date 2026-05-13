using System.Collections.Generic;

public class WorldMovementController
{
    private readonly WorldMapData mapData;

    public WorldMovementController(WorldMapData mapData)
    {
        this.mapData = mapData;
    }

    public bool CanMoveTo(WorldTileData currentTile, WorldTileData targetTile)
    {
        if (mapData == null || currentTile == null || targetTile == null)
            return false;

        if (currentTile.tileId == targetTile.tileId)
            return false;

        if (targetTile.IsPlayerOwned)
            return true;

        return IsAdjacentToAnyPlayerOwnedTile(targetTile);
    }

    public bool IsAdjacentReachable(WorldTileData currentTile, WorldTileData targetTile)
    {
        if (mapData == null || currentTile == null || targetTile == null)
            return false;

        if (targetTile.IsPlayerOwned)
            return false;

        return IsAdjacentToAnyPlayerOwnedTile(targetTile);
    }

    public List<WorldTileData> GetAdjacentReachableTiles(WorldTileData currentTile)
    {
        List<WorldTileData> result = new List<WorldTileData>();
        if (mapData == null)
            return result;

        IReadOnlyList<WorldTileData> tiles = mapData.Tiles;
        for (int i = 0; i < tiles.Count; i++)
        {
            WorldTileData tile = tiles[i];
            if (tile != null && !tile.IsPlayerOwned && IsAdjacentToAnyPlayerOwnedTile(tile))
                result.Add(tile);
        }

        return result;
    }

    private bool IsAdjacentToAnyPlayerOwnedTile(WorldTileData targetTile)
    {
        if (mapData == null || targetTile == null)
            return false;

        IReadOnlyList<WorldTileData> tiles = mapData.Tiles;
        for (int i = 0; i < tiles.Count; i++)
        {
            WorldTileData owned = tiles[i];
            if (owned == null || !owned.IsPlayerOwned)
                continue;

            if (mapData.AreNeighbors(owned, targetTile))
                return true;
        }

        return false;
    }
}
