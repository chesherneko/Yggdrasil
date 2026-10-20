using System;
using System.Collections.Generic;
using UnityEngine;

namespace BlueMarble
{
    [Serializable]
    public class Board
    {
        private readonly Tile[] tiles;
        
        public int MaxTileIndex => tiles.Length - 1;
        
        public Board(Tile[] tiles)
        {
            this.tiles = tiles;
        }
        
        public static Board FromRoot(Transform root)
        {
            Tile[] tiles = root.GetComponentsInChildren<Tile>();
            return new Board(tiles);
        }

        #region GET

        public Tile GetTile(int index) => tiles[index];

        public Tile GetTile(Vector3 worldPos)
        {
            return TryGetTileIndex(worldPos, out int idx) ? 
                tiles[idx] : 
                throw new Exception($"[Board] World position {worldPos} is out of bounds");
        }
        
        public List<Tile> GetOccupiedTiles(string playerId)
        {
            List<Tile> result = new();
            
            for (int i = 0; i < tiles.Length; i++)
            {
                Tile tile = tiles[i];

                if (tile.State.IsOccupied == false) continue;
                if (tile.State.OwnerId != playerId) continue;

                result.Add(tile);
            }
            return result;
        }
        
        public int GetTileIndex(Tile tile) => Array.IndexOf(tiles, tile);

        #endregion
        
        #region TRY_GET

        public bool TryGetTile(int index, out Tile result)
        {
            result = null;
            
            if (index < 0 || index >= tiles.Length)
            {
                Debug.LogWarning($"[Board] Idx({index}) is out of range");
                return false;
            }
            
            result = tiles[index];
            return true;
        }

        public bool TryGetTile(Vector3 worldPos, out Tile result)
        {
            result = null;
            return TryGetTileIndex(worldPos, out int idx) && 
                   TryGetTile(idx, out result);
        }

        #endregion

        #region INDEX_AND_POSITION

        public bool TryGetTileIndex(Vector3 worldPos, out int result)
        {
            result = 0;
            
            for (int i = 0; i < tiles.Length; i++)
            {
                Bounds bounds = tiles[i].MainRenderer.bounds;
                if (bounds.Contains(worldPos) == false) continue;
                
                result = i;
                return true;
            }
            
            Debug.LogWarning($"[Board] World position {worldPos} is out of bounds");
            return false;
        }

        public bool TryGetTilePosition(int index, out Vector3 result)
        {
            result = Vector3.zero;
            
            if (index < 0 || index >= tiles.Length)
            {
                Debug.LogWarning($"[Board] Idx({index}) is out of range");
                return false;
            }
            
            result = tiles[index].transform.position;
            return true;
        }
        
        #endregion
    }
}