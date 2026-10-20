using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

namespace BlueMarble
{
    public class Equipment
    {
        public EquipmentDef Def { get; }
        
        public int InstanceId { get;  }
        public int SlotIndex { get; set; }
        public int CurrentDurability {get; private set; } // 현재 횟수
        
        public float RemainingCooldown { get; private set; } // 쿨다운

        public bool IsBroken => CurrentDurability <= 0;
        private bool IsReady => RemainingCooldown <= 0f;

        public Equipment(EquipmentDef def, int instanceId, int slotIndex)
        {
            Def = def;
            InstanceId = instanceId;
            SlotIndex = slotIndex;
            CurrentDurability = def.MaxDurability;
            RemainingCooldown = 0f;

        }

        /// <summary>
        /// 쿨다운 함수 => 전투에서 호출 
        /// </summary>
        /// <param name="deltaTime"></param>
        public void Cooldown(float deltaTime) 
        {
            if (RemainingCooldown <= 0f) return;
            RemainingCooldown -= deltaTime;

            if (RemainingCooldown < 0f) RemainingCooldown = 0f;
        }

        public void ResetCooldown()
        {
            RemainingCooldown = 0f;
        }

        /// <summary>
        /// 장비 사용 시도. 쿨타임이거나 내구도가 0이면 파괴 후 실패하고 성공하면 쿨타임 리셋
        /// </summary>
        /// <returns></returns>
        public bool TryUse()
        {
            if (IsBroken) return false;
            if (IsReady == false) return false;

            RemainingCooldown = Def.Cooldown;
            return true;
        }
    
        /// <summary>
        /// 내구도 감소 함수 - 야차 종료 후 -1 씩 적용
        /// </summary>
        public void LossDurability()
        {
            CurrentDurability -= 1;
        }
        
        
    }
}
