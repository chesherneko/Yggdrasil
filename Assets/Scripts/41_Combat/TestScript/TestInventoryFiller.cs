using UnityEngine;
using BlueMarble;

namespace Player
{
    public class TestInventoryFiller : MonoBehaviour
    {
        private EquipmentTable equipmentTable; 

        private async void Start()
        {
            equipmentTable = new EquipmentTable();

            // TSV 파일에서 데이터를 비동기로 읽어옵니다.
            await equipmentTable.LoadAsync();

            InventoryManager inv = InventoryManager.Instance;

            // ID 1번부터 5번까지 차례대로 인벤토리에 넣습니다.
            for (int i = 6; i <= 10; i++)
            {
                EquipmentDef itemDef = equipmentTable.Get(i); 
                
                if (itemDef != null)
                {
                    bool isSuccess = inv.TryAddEquipment(itemDef);
                    if (isSuccess)
                    {
                        Debug.Log($"[테스트 인벤토리] 슬롯 {i-1}번: {itemDef.Name} 추가 성공");
                    }
                    else
                    {
                        Debug.Log($"[테스트 인벤토리] 슬롯 {i-1}번: 추가 실패 (인벤토리 가득 참)");
                    }
                }
                else
                {
                    Debug.Log($"[테스트 인벤토리] 에러: ID {i}번 장비 데이터를 테이블에서 찾을 수 없습니다.");
                }
            }

            Debug.Log("[테스트] TSV 데이터 기반 장비 세팅이 종료되었습니다.");
        }
    }
}