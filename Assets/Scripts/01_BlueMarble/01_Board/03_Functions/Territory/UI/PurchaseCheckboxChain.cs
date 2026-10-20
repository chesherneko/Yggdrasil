using UnityEngine;
using UnityEngine.UI;

namespace BlueMarble
{
    /// <summary>
    /// 구매 팝업의 "건물" · "랜드마크" 체크박스 사이의 연쇄 선택 규칙을 처리하는 순수 UI 스크립트.
    ///
    /// 규칙 (E3 백로그 기준: 랜드마크는 건물이 있어야 지을 수 있다):
    /// - 랜드마크를 체크하면 건물도 함께 체크된다 (랜드마크는 건물을 전제로 함).
    /// - 건물을 해제하면 랜드마크도 함께 해제된다.
    ///
    /// PropertyManager 등 게임 데이터는 전혀 참조하지 않으며,
    /// 오직 Toggle 2개의 값만 서로 동기화하는 순수 UI 동작이다.
    /// BuildingPurchasePopup이 생성한 Card_Building_CheckboxToggle / Card_Landmark_CheckboxToggle을
    /// 인스펙터에서 직접 드래그해 연결해서 사용한다.
    /// </summary>
    public class PurchaseCheckboxChain : MonoBehaviour
    {
        [Header("체크박스 참조 (BuildingPurchasePopup이 생성한 Toggle을 연결)")]
        [SerializeField] private Toggle buildingToggle;
        [SerializeField] private Toggle landmarkToggle;

        private void OnEnable()
        {
            if (buildingToggle != null) buildingToggle.onValueChanged.AddListener(OnBuildingChanged);
            if (landmarkToggle != null) landmarkToggle.onValueChanged.AddListener(OnLandmarkChanged);
        }

        private void OnDisable()
        {
            if (buildingToggle != null) buildingToggle.onValueChanged.RemoveListener(OnBuildingChanged);
            if (landmarkToggle != null) landmarkToggle.onValueChanged.RemoveListener(OnLandmarkChanged);
        }

        private void OnBuildingChanged(bool isOn)
        {
            if (isOn) return; // 건물 체크 자체는 랜드마크에 영향 없음

            // 건물을 해제하면 랜드마크도 의미가 없으므로 함께 해제
            SetToggleSilently(landmarkToggle, false);
        }

        private void OnLandmarkChanged(bool isOn)
        {
            if (!isOn) return; // 랜드마크 해제 자체는 건물에 영향 없음

            // 랜드마크를 체크하면 건물은 선행 조건이므로 자동 체크
            SetToggleSilently(buildingToggle, true);
        }

        /// <summary>
        /// onValueChanged 이벤트를 발생시키지 않고 Toggle 값만 갱신한다.
        /// (연쇄 처리 중 무한 재귀 호출을 방지하기 위함)
        /// </summary>
        private void SetToggleSilently(Toggle toggle, bool value)
        {
            if (toggle == null) return;
            if (toggle.isOn == value) return;
            toggle.SetIsOnWithoutNotify(value);
        }
    }
}