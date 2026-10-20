using UnityEngine;
using Cysharp.Threading.Tasks;
using Study.Utilities;
using UnityEngine.InputSystem;

namespace BlueMarble
{
    public class TreasureRewardTest : MonoBehaviour
    {
        [SerializeField] private TreasureRewardPopupController popup;

        [Tooltip("equipments.tsv의 Id 컬럼 값. EquipmentDef는 Inspector에 직접 드래그할 수 없어서 id로 조회한다.")]
        [SerializeField] private int testEquipmentId = 1;

        private void Update()
        {
            if (SimpleInput.GetKeyDown(Key.T))
            {
                EquipmentDef def = DataManager.Instance.GetEquipmentDef(testEquipmentId);

                if (def == null)
                {
                    Debug.LogError($"[TreasureRewardTest] id={testEquipmentId}에 해당하는 EquipmentDef를 찾을 수 없습니다. " +
                                   "equipments.tsv에 있는 Id 값인지 확인해주세요.");
                    return;
                }

                popup.ShowRewardAsync(def).Forget();
                Debug.Log($"[TreasureRewardTest] T키 입력 - {def.Name}({def.Grade}) 테스트 보상 팝업 실행");
            }
        }
    }
}