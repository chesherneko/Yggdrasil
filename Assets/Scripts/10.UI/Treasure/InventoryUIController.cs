using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;

namespace BlueMarble
{
    /// <summary>
    /// 인벤토리 UI(슬롯 5칸)를 InventoryManager와 연결한다.
    /// 교체 대기 상태(IsWaitingReplace)일 때는 슬롯 클릭 시 자동으로 교체 처리한다.
    /// </summary>
    public class InventoryUIController : MonoBehaviour
    {
        [Header("Slots")]
        [SerializeField] private Image[] slotIcons = new Image[InventoryManager.MaxSlotCount];
        [SerializeField] private Button[] slotButtons = new Button[InventoryManager.MaxSlotCount];

        [Header("Close")]
        [SerializeField] private Button closeButton;

        private UniTaskCompletionSource decisionWaiter;

        public async UniTask ShowAsync()
        {
            gameObject.SetActive(true);

            decisionWaiter = new UniTaskCompletionSource();
            RegisterSlotClickListeners();
            closeButton.onClick.AddListener(OnCloseClicked);
            RefreshSlots();

            try
            {
                await decisionWaiter.Task;
            }
            finally
            {
                UnregisterSlotClickListeners();
                closeButton.onClick.RemoveListener(OnCloseClicked);

                decisionWaiter = null;
                gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// InventoryManager의 현재 상태를 슬롯 아이콘에 반영한다.
        /// 빈 슬롯은 아이콘을 끄고, 채워진 슬롯은 IconKey로 스프라이트를 불러온다.
        /// </summary>
        private void RefreshSlots()
        {
            for (int i = 0; i < InventoryManager.MaxSlotCount; i++)
            {
                Equipment instance = InventoryManager.Instance.GetAt(i);

                if (instance == null)
                {
                    slotIcons[i].enabled = false;
                    continue;
                }

                Sprite icon = string.IsNullOrEmpty(instance.Def.IconKey)
                    ? null
                    : Resources.Load<Sprite>(instance.Def.IconKey);

                slotIcons[i].enabled = icon != null;
                slotIcons[i].sprite = icon;
            }
        }

        private void RegisterSlotClickListeners()
        {
            for (int i = 0; i < InventoryManager.MaxSlotCount; i++)
            {
                int slotIndex = i; // 클로저 캡처 방지
                slotButtons[i].onClick.AddListener(() => OnSlotClicked(slotIndex));
            }
        }

        private void UnregisterSlotClickListeners()
        {
            foreach (Button button in slotButtons)
            {
                button.onClick.RemoveAllListeners();
            }
        }

        /// <summary>
        /// 교체 대기 중일 때만 슬롯 클릭이 의미를 가진다. 클릭한 슬롯에 교체 대기 장비를 넣는다.
        /// </summary>
        private void OnSlotClicked(int slotIndex)
        {
            if (InventoryManager.Instance.IsWaitingReplace == false)
            {
                Debug.Log("[InventoryUIController] 교체 대기 중이 아니라 슬롯 클릭이 무시됩니다.");
                return;
            }

            InventoryManager.Instance.ReplaceAt(slotIndex);
            Debug.Log($"[InventoryUIController] {slotIndex}번 슬롯 교체 완료");

            decisionWaiter?.TrySetResult();
        }

        private void OnCloseClicked()
        {
            if (InventoryManager.Instance.IsWaitingReplace)
            {
                InventoryManager.Instance.CancelReplace();
                Debug.Log("[InventoryUIController] 신규 보상 획득을 포기했습니다.");
            }

            decisionWaiter?.TrySetResult();
        }
    }
}
