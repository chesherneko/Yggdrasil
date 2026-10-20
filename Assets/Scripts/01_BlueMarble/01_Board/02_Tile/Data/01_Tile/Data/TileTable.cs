using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Utility.Data;

namespace BlueMarble
{
    public class TileTable : DataTable<TileDTO, TileDef>
    {
        protected override string TablePath => "Board/tiles";
        
        public override async UniTask LoadAsync()
        {
            TilePrefabMap prefabMap = TilePrefabMap.Instance;
            
            defs.Clear();
            List<TileDTO> rows = await GetRows();

            for (int i = 0; i < rows.Count; i++)
            {
                TileDTO dto = rows[i];

                int id = dto.Id;
                TileType type = Enum.Parse<TileType>(dto.TileType);
                TileDirection dir = Enum.Parse<TileDirection>(dto.TileDir);
                Tile prefab = prefabMap.GetPrefab(type, dir);

                defs[dto.Id] = new TileDef(id, type, dir, prefab);
            }
        }
    }
}