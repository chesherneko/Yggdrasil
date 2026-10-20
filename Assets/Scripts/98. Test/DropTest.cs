using System.Collections.Generic;
using Study.Utilities;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BlueMarble
{
    /// <summary>
    /// DropService 동작 확인용 테스트 스크립트.
    /// 1번 키: Treasure로 장비 1개 뽑기
    /// 2번 키: GoldenKey로 장비 1개 뽑기
    /// 3번 키: BattleReward로 장비 1개 뽑기
    /// 4번 키: Treasure로 1000번 뽑아서 등급별 비율이 확률표와 비슷한지 확인
    /// </summary>
    public class DropServiceTest : MonoBehaviour
    {
        private const int SampleCount = 1000;

        private void Update()
        {
            if (DataManager.Instance.IsDataLoaded == false) return;

            if (SimpleInput.GetKeyDown(Key.Digit1))
            {
                DrawOnce(DropType.Treasure);
            }

            if (SimpleInput.GetKeyDown(Key.Digit2))
            {
                DrawOnce(DropType.GoldenKey);
            }

            if (SimpleInput.GetKeyDown(Key.Digit3))
            {
                DrawOnce(DropType.BattleReward);
            }

            if (SimpleInput.GetKeyDown(Key.Digit4))
            {
                CheckGradeDistribution(DropType.Treasure);
            }
        }

        /// <summary>
        /// 지정한 출처로 장비 1개를 뽑아 결과를 콘솔에 출력한다.
        /// </summary>
        private void DrawOnce(DropType dropType)
        {
            EquipmentDef def = DropService.DrawEquipment(dropType);
            Debug.Log($"[{dropType}] 뽑기 결과: {def.Name} ({def.Grade})");
        }

        /// <summary>
        /// 등급 뽑기만 SampleCount번 반복해서, 실제 나온 비율이
        /// drop.tsv에 넣어둔 확률과 비슷한지 눈으로 확인하기 위한 테스트.
        /// (장비까지 뽑을 필요는 없으므로 DrawGrade만 반복한다)
        /// </summary>
        private void CheckGradeDistribution(DropType dropType)
        {
            Dictionary<EquipmentGrade, int> counts = new()
            {
                { EquipmentGrade.Common, 0 },
                { EquipmentGrade.Rare, 0 },
                { EquipmentGrade.Legendary, 0 }
            };

            for (int i = 0; i < SampleCount; i++)
            {
                EquipmentGrade grade = DropService.DrawGrade(dropType);
                counts[grade]++;
            }

            Debug.Log($"===== [{dropType}] {SampleCount}회 등급 뽑기 결과 =====");
            foreach (KeyValuePair<EquipmentGrade, int> pair in counts)
            {
                float ratio = (float)pair.Value / SampleCount;
                Debug.Log($"{pair.Key}: {pair.Value}회 ({ratio:P1})");
            }
        }
    }
}