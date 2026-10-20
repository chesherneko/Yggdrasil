using Study.Utilities;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

namespace BlueMarble
{
    /// <summary>
    /// PropertyManager/TerritoryManager 없이 구매 팝업 UI만 독립적으로 확인하기 위한 테스트 스크립트.
    /// BuildingPurchasePopupController가 이미 참조 중인 오브젝트들을 그대로 재사용해서 연결하고,
    /// 키보드 입력으로 팝업을 강제로 띄우고, 버튼/체크박스 결과를 콘솔에 로그로 출력한다.
    ///
    /// 사용법: PopupFrame(또는 임의의 오브젝트)에 이 컴포넌트를 추가하고,
    /// 아래 필드에 BuildingPurchasePopupController와 동일한 오브젝트들을 인스펙터에서 연결한 뒤 Play.
    /// P키를 누르면 팝업이 뜨고, 구매/닫기 버튼과 체크박스 동작이 콘솔에 로그로 찍힌다.
    /// </summary>
    public class BuildingPurchasePopupTester : MonoBehaviour
    {
        [Header("테스트 대상 (BuildingPurchasePopupController와 동일하게 연결)")]
        [SerializeField] private GameObject popupRoot;
        [SerializeField] private TMP_Text priceText;
        [SerializeField] private Toggle buildingToggle;
        [SerializeField] private Toggle landmarkToggle;
        [SerializeField] private Button purchaseButton;
        [SerializeField] private Button closeButton;

        [Header("테스트 설정")]
        [SerializeField] private int testTilePrice = 1105000; // 예: 110만5000

        private void OnEnable()
        {
            if (buildingToggle != null) buildingToggle.onValueChanged.AddListener(OnBuildingToggleChanged);
            if (landmarkToggle != null) landmarkToggle.onValueChanged.AddListener(OnLandmarkToggleChanged);
            if (purchaseButton != null) purchaseButton.onClick.AddListener(OnPurchaseClicked);
            if (closeButton != null) closeButton.onClick.AddListener(OnCloseClicked);
        }

        private void OnDisable()
        {
            if (buildingToggle != null) buildingToggle.onValueChanged.RemoveListener(OnBuildingToggleChanged);
            if (landmarkToggle != null) landmarkToggle.onValueChanged.RemoveListener(OnLandmarkToggleChanged);
            if (purchaseButton != null) purchaseButton.onClick.RemoveListener(OnPurchaseClicked);
            if (closeButton != null) closeButton.onClick.RemoveListener(OnCloseClicked);
        }

        private void Update()
        {
            if (SimpleInput.GetKeyDown(Key.Digit1))
            {
                ShowTestPopup();
            }
        }

        private void ShowTestPopup()
        {
            if (buildingToggle != null) buildingToggle.SetIsOnWithoutNotify(false);
            if (landmarkToggle != null) landmarkToggle.SetIsOnWithoutNotify(false);

            if (priceText != null)
            {
                priceText.text = $"{testTilePrice:N0}";
            }

            if (popupRoot != null)
            {
                popupRoot.SetActive(true);
            }

            Debug.Log($"[테스트] 팝업 표시 - 가격: {testTilePrice:N0}");
        }

        private void OnBuildingToggleChanged(bool isOn)
        {
            Debug.Log($"[테스트] 건물 체크박스 -> {isOn}");
        }

        private void OnLandmarkToggleChanged(bool isOn)
        {
            Debug.Log($"[테스트] 랜드마크 체크박스 -> {isOn}");
        }

        private void OnPurchaseClicked()
        {
            bool buyBuilding = buildingToggle != null && buildingToggle.isOn;
            bool buyLandmark = landmarkToggle != null && landmarkToggle.isOn;
            Debug.Log($"[테스트] 구매 버튼 클릭 -> buyTile: true, buyBuilding: {buyBuilding}, buyLandmark: {buyLandmark}");
        }

        private void OnCloseClicked()
        {
            Debug.Log("[테스트] 닫기 버튼 클릭 -> 전부 취소 (false, false, false)");
        }
    }
}