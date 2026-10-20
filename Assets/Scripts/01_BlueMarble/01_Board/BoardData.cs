using System.Collections.Generic;
using UnityEngine;

namespace BlueMarble
{
    public class BoardData : MonoBehaviour
    {
        private List<TileEntry> entries = new();
        
        public int TileCount => entries.Count;
        public IReadOnlyList<TileEntry> Entries => entries;

        public void ImportFrom(IReadOnlyList<Tile> tiles)
        {
            entries.Clear();

            for (int i = 0; i < tiles.Count; i++)
            {
                Tile tile = tiles[i];
                
                entries.Add(new TileEntry
                {
                    Id = i,
                    TileId = tile.Def.Id,
                    SpaceId = tile.SpaceDef.Id
                });
            }
        }
        
        public Board ToBoard(Transform root) => Board.FromRoot(root);
        
        public BoardFile ToFile() => new BoardFile { Entries = new List<TileEntry>(entries) };
        public void ApplyFile(BoardFile file) => entries = new List<TileEntry>(file.Entries);
    }
}