using System.Collections.Generic;
using Study.Utilities;
using UnityEngine.InputSystem;

namespace BlueMarble
{
    /// <summary>
    /// 플레이어가 보유한 장비 전체를 관리한다.
    /// 별도의 "전투 덱 편성" 단계 없이, 인벤토리에 있는 장비가 곧 그대로 야차의 전투 덱이 된다.
    ///
    /// 슬롯은 0~4 고정 배열이며, 빈 슬롯은 null이다.
    /// 슬롯 번호는 야차의 숫자키 입력(1~5)과 그대로 대응된다. 
    /// </summary>
    public class InventoryManager
    {
        public static InventoryManager Instance { get; } = new InventoryManager();
        
        public const int MaxSlotCount = 5;
        public readonly Equipment[] slots = new Equipment[MaxSlotCount];

        public InventoryManager()
        {
            
        }
        
        private int nextInstanceId = 1;
        
        /// <summary>
        /// 인벤토리가 가득 찬 상태에서 새 장비를 획득했을 때, 교체 대기 중인 장비 정의.
        /// null이 아니면 "지금 교체 대기 모드"라는 뜻이다. 
        /// </summary>
        public EquipmentDef ReplaceDef { get; private set; }
       
        public bool IsWaitingReplace => ReplaceDef is not null;

        /// <summary>
        /// 인벤토리 5칸을 확인하다가 빈칸(null)을 발견하면 즉시 반환
        /// </summary>
        public bool IsFull
        {
            get
            {
                for (int i = 0; i < MaxSlotCount; i++)
                {
                    if (slots[i] == null) return false;
                }

                return true;
            }
        }
        
        
        /// <summary>
        /// 현재 보유중인 인벤토리를 List로 반환
        /// </summary>
        /// <returns></returns>
        public IReadOnlyList<Equipment> GetAll()
        {
            List<Equipment> result = new();
            
            for (int i = 0; i < MaxSlotCount; i++)
            {
                if (slots[i] != null) result.Add(slots[i]);
            }
 
            return result; 
        }
        
        /// <summary>
        /// 보유중인 인벤토리에서 하나만 가져오는 함수
        /// </summary>
        /// <param name="slotIndex"></param>
        /// <returns></returns>
        public Equipment GetAt(int slotIndex) => slots[slotIndex];
        

        /// <summary>
        /// 빈칸 찾기 함수 
        /// </summary>
        /// <returns></returns>
        private int FindEmptySlot()
        {
            for (int i = 0; i < MaxSlotCount; i++)
            {
                if (slots[i] == null) return i;
            }

            return -1;
        }

        /// <summary>
        /// 장비를 넣는 함수 빈칸 찾기 함수에서 찾은 값에 새로운 장비 데이터를 넣음 (일단은 빈칸이 없다면 false 반환)
        /// </summary>
        /// <param name="def"></param>
        /// <returns></returns>
        public bool TryAddEquipment(EquipmentDef def)
        {
            int emptyslot = FindEmptySlot();
            
            // 장비칸이 다 차있다면
            if (emptyslot == -1)
            {
                // 보관함에 보관하고 교체상태로 전환
                ReplaceDef = def;
                return false;
            }

            Equipment equipment = new Equipment(def, nextInstanceId, emptyslot);
            slots[emptyslot] = equipment;
            nextInstanceId++;

            NotifyEquipmentSet(equipment);

            return true;
        }
        
        /// <summary>
        /// 아이템 교체 함수
        /// </summary>
        /// <param name="slotIndex"></param>
        public void ReplaceAt(int slotIndex)
        {
            // 교체 상태가 아니면 반환
            if (IsWaitingReplace == false) return;

            // 아까 보관해뒀던 (ReplaceDef)을 꺼내서
            // 3번 슬롯 자리에 새로 만들어 넣음
            Equipment equipment = new Equipment(ReplaceDef, nextInstanceId, slotIndex);
            slots[slotIndex] = equipment;
            nextInstanceId++;

            ReplaceDef = null;   // 다 썼으니 보관함 비우기

            NotifyEquipmentSet(equipment);
        }
        
        /// <summary>
        /// 아이템 교체 포기
        /// </summary>
        public void CancelReplace()
        {
            ReplaceDef = null;
        }

        public void Reset()
        {
            for (int i = 0; i < slots.Length; i++)
                slots[i] = null;

            ReplaceDef = null;
            nextInstanceId = 1;
        }
        
            
        /// <summary>
        /// 장비 파괴 함수 (내구도 0이거나 패배 패널티)
        /// </summary>
        /// <param name="slotsIndex"></param>
        public void RemoveAt(int slotsIndex)
        {
            Equipment equipment = slots[slotsIndex];
            if (equipment == null) return;

            NotifyEquipmentRemoved(equipment);

            for (int i = slotsIndex; i < slots.Length - 1; i++)
            {
                slots[i] = slots[i + 1];

                if (slots[i] != null)
                    slots[i].SlotIndex = i;
            }

            slots[slots.Length - 1] = null;
        }

        private static void NotifyEquipmentSet(Equipment equipment)
        {
            if (Server.RoomClient.IsInitialized == false) return;
            Server.RoomClient.Instance.RequestSetEquipment(equipment);
        }

        private static void NotifyEquipmentRemoved(Equipment equipment)
        {
            if (Server.RoomClient.IsInitialized == false) return;
            Server.RoomClient.Instance.RequestRemoveEquipment(equipment);
        }
        
        
        /// <summary>
        /// 보유 장비 중 하나를 무작위로 즉시 파괴한다.
        /// 도전자 패배 시, 내구도 감소보다 먼저 호출되어야 한다. 
        /// 보유 장비가 없으면 아무 일도 하지 않는다.
        /// </summary>
        public void DestroyRandomEquipment()
        {
            IReadOnlyList<Equipment> owned = GetAll();
            if (owned.Count == 0) return;
 
            int randomIndex = UnityEngine.Random.Range(0, owned.Count);
            RemoveAt(owned[randomIndex].SlotIndex);
        }

        /// <summary>
        /// 보유한 모든 장비의 내구도를 1 감소시키고, 0이 된 장비는 즉시 제거한다.
        /// 야차 종료 시 실제 사용/전투 여부와 관계없이 양측 모두에 대해 호출되어야 한다. 
        /// </summary>
        public void LossDurabilityAll()
        {
            // GetAll()은 새 리스트를 매번 만들어 반환하므로,
            // 순회 도중 RemoveAt()으로 slots를 바꿔도 안전하다.
            foreach (Equipment instance in GetAll())
            {
                instance.LossDurability();

                if (instance.IsBroken)
                {
                    RemoveAt(instance.SlotIndex);
                }
            }

        }

        public void ResetCooldownAll()
        {
            foreach (Equipment instance in GetAll())
                instance.ResetCooldown();
        }

    }
}
