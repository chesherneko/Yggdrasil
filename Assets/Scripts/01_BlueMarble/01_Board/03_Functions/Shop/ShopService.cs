using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;


namespace BlueMarble
{
    public class ShopService
    {

        /// <summary>
        /// 상점에서 Common 등급 불러오기
        /// </summary>
        /// <returns></returns>
        public IReadOnlyList<EquipmentDef> GetShopList()
        {
            return DataManager.Instance.Equipments.GetDefs().Where(def => def.Grade == EquipmentGrade.Common).ToList();
        }

        /// <summary>
        /// 상점 목록 중 하나(shopIndex, 0부터 시작)를 구매 시도한다.
        /// 자금은 서버가 관리하는 EconomyManager를 직접 조회/차감한다.
        /// 자금 확인 -> 인벤토리 추가 -> 차감 순서로 진행하며,
        /// 인벤토리가 가득 차 있으면 InventoryManager가 자동으로 교체 대기 상태로 전환한다.
        /// 구매 또는 교체 대기 상태로 전환됐으면 true, 자금 부족이나 잘못된 번호면 false
        /// </summary>
        /// <returns></returns>
        public async UniTask<bool> TryPurchaseAsync(int shopIndex, string playerId, InventoryManager inventory)
        {
            IReadOnlyList<EquipmentDef> list = GetShopList();

            if (shopIndex < 0 || shopIndex >= list.Count)
            {
                Debug.Log("[ShopService] 잘못된 번호입니다.");
                return false;
            }

            EquipmentDef def = list[shopIndex];

            if (!EconomyManager.Instance.TryGetGold(playerId, out int currentGold) || currentGold < def.Price)
            {
                Debug.Log("[ShopService] 자금이 부족합니다.");
                return false;
            }

            bool added = inventory.TryAddEquipment(def);

            if (added)
            {
                await EconomyManager.Instance.LoseGoldTo(playerId, def.Price);
                Debug.Log($"[ShopService] {def.Name} 구매 완료 (인벤토리에 즉시 장착)");
            }
            else
            {
                Debug.Log($"[ShopService] {def.Name} 교체 대기 상태입니다. (교체할 슬롯을 선택하거나 포기하세요)");
            }

            return true;
        }



    }
}
