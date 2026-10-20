using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BlueMarble
{
    /// <summary>
    /// BuildingPurchasePopup이 생성한 화면을 TerritoryManager_와 연결하는 런타임 컨트롤러.
    ///
    /// - TerritoryManager_.OnPurchaseRequested 구독 → 팝업 표시
    /// - 구매 버튼 클릭 → 현재 토글 상태 제출
    /// - 닫기(X) 버튼 클릭 → 구매 포기 처리
    /// </summary>
    public class BuildingPurchasePopupController : MonoBehaviour
    {
        [Header("팝업 표시/숨김 대상")]
        [SerializeField] private GameObject popupRoot;

        [Header("가격 표시 텍스트")]
        [SerializeField] private TMP_Text priceText;

        [Header("체크박스")]
        [SerializeField] private Toggle buildingToggle;
        [SerializeField] private Toggle landmarkToggle;

        [Header("버튼")]
        [SerializeField] private Button purchaseButton;
        [SerializeField] private Button closeButton;

        private Tile currentTile;
        private int currentGold;

        private void OnEnable()
        {
            TerritoryManager.Instance.OnPurchaseRequested += HandlePurchaseRequested;

            if (purchaseButton != null) purchaseButton.onClick.AddListener(OnPurchaseClicked);
            if (closeButton != null) closeButton.onClick.AddListener(OnCloseClicked);
            if (buildingToggle != null) buildingToggle.onValueChanged.AddListener(HandleToggleChanged);
            if (landmarkToggle != null) landmarkToggle.onValueChanged.AddListener(HandleToggleChanged);

            HidePopup();
        }

        private void OnDisable()
        {
            if (TerritoryManager.IsInitialized)
            {
                TerritoryManager.Instance.OnPurchaseRequested -= HandlePurchaseRequested;

                if (currentTile != null)
                    TerritoryManager.Instance.SubmitPurchase(false, false, false);
            }

            if (purchaseButton != null) purchaseButton.onClick.RemoveListener(OnPurchaseClicked);
            if (closeButton != null) closeButton.onClick.RemoveListener(OnCloseClicked);
            if (buildingToggle != null) buildingToggle.onValueChanged.RemoveListener(HandleToggleChanged);
            if (landmarkToggle != null) landmarkToggle.onValueChanged.RemoveListener(HandleToggleChanged);
        }

        private void HandlePurchaseRequested(Tile tile, int gold)
        {
            currentTile = tile;
            currentGold = gold;

            TileState state = tile.State;
            int tilePrice = tile.SpaceDef.Price;

            bool canBuyBuilding =
                state.HasBuilding == false &&
                gold >= tilePrice * (state.IsOccupied ? 2 : 3);

            bool canBuyLandMark =
                state.IsOccupied &&
                state.HasBuilding &&
                state.HasLandMark == false &&
                gold >= tilePrice * 3;

            SetToggleState(
                buildingToggle,
                state.HasBuilding,
                canBuyBuilding);

            SetToggleState(
                landmarkToggle,
                state.HasLandMark,
                canBuyLandMark);

            RefreshPurchaseState();
            ShowPopup();
        }

        private void HandleToggleChanged(bool _)
        {
            RefreshPurchaseState();
        }

        private void OnPurchaseClicked()
        {
            if (currentTile == null) return;

            bool buyTile = currentTile.State.IsOccupied == false;
            bool buyBuilding =
                buildingToggle != null &&
                buildingToggle.interactable &&
                buildingToggle.isOn;
            bool buyLandMark =
                landmarkToggle != null &&
                landmarkToggle.interactable &&
                landmarkToggle.isOn;

            TerritoryManager.Instance.SubmitPurchase(
                buyTile,
                buyBuilding,
                buyLandMark);

            HidePopup();
        }

        private void OnCloseClicked()
        {
            TerritoryManager.Instance.SubmitPurchase(false, false, false);
            HidePopup();
        }

        private void RefreshPurchaseState()
        {
            if (currentTile == null) return;

            TileState state = currentTile.State;
            int tilePrice = currentTile.SpaceDef.Price;

            bool buyTile = state.IsOccupied == false;
            bool buyBuilding =
                state.HasBuilding == false &&
                buildingToggle != null &&
                buildingToggle.isOn;
            bool buyLandMark =
                state.HasLandMark == false &&
                landmarkToggle != null &&
                landmarkToggle.isOn;

            int totalPrice = 0;

            if (buyTile) totalPrice += tilePrice;
            if (buyBuilding) totalPrice += tilePrice * 2;
            if (buyLandMark) totalPrice += tilePrice * 3;

            if (priceText != null)
                priceText.text = $"{totalPrice:N0}";

            if (purchaseButton != null)
            {
                bool hasPurchase = buyTile || buyBuilding || buyLandMark;
                purchaseButton.interactable = hasPurchase && totalPrice <= currentGold;
            }
        }

        private static void SetToggleState(Toggle toggle, bool isOn, bool interactable)
        {
            if (toggle == null) return;

            toggle.SetIsOnWithoutNotify(isOn);
            toggle.interactable = interactable;
        }

        private void ShowPopup()
        {
            if (popupRoot != null) popupRoot.SetActive(true);
        }

        private void HidePopup()
        {
            if (popupRoot != null) popupRoot.SetActive(false);
            currentTile = null;
        }
    }
}
