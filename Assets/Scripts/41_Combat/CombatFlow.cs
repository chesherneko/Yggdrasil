using BlueMarble;
using Cysharp.Threading.Tasks;

namespace Combat
{
    public class CombatFlow
    {
        private EconomyManager economyManager => EconomyManager.Instance;
        
        private InventoryManager inventoryManager => InventoryManager.Instance;

        // 통행료를 낸다. 랜드마크가 있다면 영토 가치를 비교하지 않고 대결 신청을 한다. 럾다면 영토의 총 가치와 본인의 돈이 100%이상 인지 검사해서
        // 신청 가능하게 한다.
        public bool CanBattleRequest(string attackerId, string defenderId, Tile tile)
        {
            TerritoryExtensions.GetTollAmount(tile);
            economyManager.TryGetGold(attackerId, out int gold);
            
            if (tile.State.HasLandMark) return true; 
            // if (gold < territoryManager.CalculateTileAssets(tile)) return false;
            
            return true;
        }

        public void SettleBattleResult(string attackerId, string defenderId, string winnerId, string myPlayerId,Tile tile)
        {
            bool isAttackerWin = (winnerId == attackerId);

            if (isAttackerWin)
            {
                // 랜드마크에서 공격자가 이겼을 때 기본 보상 지급(아직 없음)
                // territoryManager.StolenTerritory(defenderId, winnerId, tile);
            }
            else
            {
                // territoryManager.PayToll(attackerId, tile);
                // 지금 이 함수는 그냥 모든 인원에게 일어남
                // 진 사람만 일어나게 하려면?
                if(myPlayerId == attackerId) inventoryManager.DestroyRandomEquipment();
            }

            inventoryManager.LossDurabilityAll();
        }

    }
}