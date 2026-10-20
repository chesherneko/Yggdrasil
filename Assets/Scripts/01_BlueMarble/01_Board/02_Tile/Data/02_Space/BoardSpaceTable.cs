using System;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using Utility.Data;
using BlueMarble.Utility;

namespace BlueMarble
{
    public class BoardSpaceTable : DataTable<BoardSpaceDTO, BoardSpaceDef>
    {
        protected override string TablePath => "Board/spaces";

        public override async UniTask LoadAsync()
        {
            BoardSpaceIconMap iconMap = BoardSpaceIconMap.Instance;
            
            defs.Clear();
            List<BoardSpaceDTO> rows = await GetRows();

            for (int i = 0; i < rows.Count; i++)
            {
                BoardSpaceDTO dto = rows[i];

                int id = dto.Id;
                string name = dto.Name;
                int price = dto.Price;
                TileEffectType effectType = Enum.Parse<TileEffectType>(dto.Effect);
                bool isActiveOnPass = dto.IsActiveOnPass;
                Sprite icon = iconMap.GetIcon(dto.IconIndex);
                Color color = dto.ColorHex.HexToColor();

                defs[dto.Id] = new BoardSpaceDef(id, name, price, effectType, isActiveOnPass, icon, color);
            }
        }
    }
}