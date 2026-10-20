using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BlueMarble
{
    public class DropService
    {
       
        // 등급확률을 먼저 뽑고 
        
        // 0 ~ 0.5 => 일반
        // 0.5 ~ 0.8 => 레어
        // 0.8 ~ 1 => 레전더리
        
        // 가중치를 계속 더해서 누적시킨다
        
        public static EquipmentGrade DrawGrade(DropType dropType)
        {
            // 리스트로 반환
            List<DropDef> table = DataManager.Instance.Drops.GetDefs().Where(def => def.Type == dropType).ToList();
            
            // 랜덤으로 굴리고
            float roll = Random.value;
            
            // 처음 값
            float totalvalue = 0f;

            foreach (DropDef def in table)
            {
                // 처음값에 계속 더하고
                totalvalue += def.Probability;
                
                // 당첨
                if (roll <= totalvalue)
                {
                    return def.Grade;
                }
            }

            return table[^1].Grade;
        }
        
        // 거기서도 아이템을 뽑는다

        public static EquipmentDef DrawEquipmentByGrade(EquipmentGrade grade)
        {
            // 리스트 반환
            List<EquipmentDef> preparation = DataManager.Instance.Equipments.GetDefs().Where(def => def.Grade == grade).ToList();

            // 랜덤 배열
            int randomIndex = Random.Range(0, preparation.Count);

            return preparation[randomIndex];
        }
        
        // 드랍 테이블에서 장비를 뽑는다
        
        public static EquipmentDef DrawEquipment(DropType dropType)
        {
            EquipmentGrade grade = DrawGrade(dropType);
            return DrawEquipmentByGrade(grade);
        } 
        
        
    }
}