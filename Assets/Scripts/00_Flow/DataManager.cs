using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Jay;

namespace BlueMarble
{
    public class DataManager : SingletonBase<DataManager>
    {
        public TileTable Tiles { get; } = new();
        public BoardSpaceTable BoardSpaces { get; } = new();

        public EquipmentTable Equipments { get; } = new();
        public DropTable Drops { get; } = new();
        
        public GoldCardTable GoldCards { get; } = new();
        
        public event Action OnDataLoaded;
        
        public bool IsDataLoaded { get; private set; } = false;

        private async UniTaskVoid Start()
        {
            await LoadAsync();
            
            IsDataLoaded = true;
            OnDataLoaded?.Invoke();
        }

        private async UniTask LoadAsync()
        {
            // 추가적인 데이터 확장이 필요하다면 tasks에 계속해서 추가하면 됨
            List<UniTask> tasks = new()
            {
                Tiles.LoadAsync(),
                BoardSpaces.LoadAsync(),
                Equipments.LoadAsync(),
                GoldCards.LoadAsync(),
                
                Drops.LoadAsync(),
            };
            await UniTask.WhenAll(tasks);
        }

        public UniTask EnsureDataLoaded() 
            => UniTask.WaitUntil(this, static self => self.IsDataLoaded);

        #region GET_DEF

        public TileDef GetTileDef(int id) => Tiles.Get(id);
        public BoardSpaceDef GetBoardSpaceDef(int id) => BoardSpaces.Get(id);

        public EquipmentDef GetEquipmentDef(int id) => Equipments.Get(id);
        
        public GoldCardDef GetGoldCardDef(int id) => GoldCards.Get(id);
        
        public DropDef GetDropDef(int id) => Drops.Get(id);

        #endregion
        
        #region  TRY_GET_DEF

        public bool TryGetTileDef(TileType type, TileDirection dir, out TileDef result)
        {
            result = null;
            
            // Corner 혹은 Special 타일의 경우, 방향 보정이 필요
            dir = type switch
            {
                TileType.Corner => TileDirection.All,
                TileType.Special when dir is TileDirection.East or TileDirection.West 
                    => TileDirection.Vertical,
                _ => dir
            };

            foreach (var def in Tiles.GetDefs())
            {
                if (def.Type != type) continue;
                if (def.Dir != dir) continue;

                result = def;
                return true;
            }

            return false;
        }

        #endregion
    }
}