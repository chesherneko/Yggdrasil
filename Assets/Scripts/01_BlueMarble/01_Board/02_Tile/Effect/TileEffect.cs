using BlueMarble.Server;
using UnityEngine;
using Cysharp.Threading.Tasks;
using BlueMarble;

namespace BlueMarble
{
    public interface ITileEffect
    {
        UniTask Apply(string playerId, Tile tile);
    }

    public class TerritoryEffect : ITileEffect // DONE
    {
        public UniTask Apply(string playerId, Tile tile)
        {
            TerritoryManager territory = TerritoryManager.Instance;
            return territory.ResolveAsync(playerId, tile);
        }
    }

    public class SalaryEffect : ITileEffect // DONE
    {
        public UniTask Apply(string playerId, Tile _)
        {
            int amount = GameSettings.Instance.SalaryAmount;
            return EconomyManager.Instance.GainGoldTo(playerId, amount);
        }   
    }

    public class PrisonEffect : ITileEffect // DONE
    {
        public UniTask Apply(string playerId, Tile tile)
        {
            int count = GameSettings.Instance.PrisonActionDisabledTurnCount;
            return RoomClient.Instance.RequestAddIncapacitationCount(count);
        }  
    }

    public class TreasureEffect : ITileEffect // 서버랑 연동해야함
    {
        public async UniTask Apply(string playerId, Tile tile)
        {
            TreasureManager treasure = TreasureManager.Instance;
            await treasure.GrantAsync(playerId);
        }
    }

    public class GoldCardEffect : ITileEffect // 살짝 수정해야함 (장비 획득 관련)
    {
        public async UniTask Apply(string playerId, Tile tile)
        {
            GoldCardManager goldCard = GoldCardManager.Instance;
            await goldCard.DrawAndUseAsync(playerId);
        }
    }

    public class TeleportEffect : ITileEffect // DONE
    {
        public async UniTask Apply(string playerId, Tile _)
        {
            TeleportManager teleport = TeleportManager.Instance;
            await teleport.MoveToSelected(playerId);
        }
    }

    public class ShopEffect : ITileEffect // 살짝 수정해야함
    {
        public async UniTask Apply(string playerId, Tile _)
        {
            ShopManager shop = ShopManager.Instance;
            await shop.OpenAsync(playerId);
        }
    }
}