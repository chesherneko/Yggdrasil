using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

namespace BlueMarble
{
    public static class BoardSpawner
    {
        private static DataManager Data => DataManager.Instance;
        
        public static BoardData SpawnBoard(this BoardFile file,
            Transform root, Transform startPoint, TileDirection dir, Vector3 offset)
        {
            List<TileEntry> entries = file.Entries;
            
            // 첫 타일을 우선 생성
            TileEntry first = entries[0];
            
            Tile lastCreated = CreateTile(first.TileId, first.SpaceId, root);
            lastCreated.transform.position = startPoint.position;
            
            // 이후, 첫 타일을 기준으로 하여 나머지 타일 생성
            for (int i = 1; i < entries.Count; i++)
            {
                TileEntry entry = entries[i];
                
                Tile created = CreateTile(entry.TileId, entry.SpaceId, root);
                created.transform.position = created.CalculateDistFrom(lastCreated, dir, offset);
                
                lastCreated = created;
                if (created.Def.Type == TileType.Corner) dir++;
            }

            BoardData data = root.TryGetComponent(out BoardData result) ? 
                result : 
                root.AddComponent<BoardData>();
            
            data.ApplyFile(file);
            return data;
        }

        public static Tile CreateTile(int tileDefId, int spaceDefId, Transform root)
        {
            TileDef def = Data.GetTileDef(tileDefId);
            BoardSpaceDef spaceDef = Data.GetBoardSpaceDef(spaceDefId);
            
            return root.CreateTile(def.Type, def.Dir, spaceDef.Id);
        }

        public static Tile CreateTile(this Transform root, TileType type, TileDirection dir, int spaceId)
        {
            if (Data.TryGetTileDef(type, dir, out var def) == false)
            {
                Debug.LogWarning($"TileDef not found: {type}");
                return null;
            }
            
            BoardSpaceDef spaceDef = Data.GetBoardSpaceDef(spaceId);
            
            Tile created = Object.Instantiate(def.Prefab, root);
            created.Initialize(def, spaceDef);
            created.UpdateVisuals();
            
            return created;
        }
    }
}