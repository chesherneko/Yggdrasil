using System.Linq;
using BlueMarble.Server;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BlueMarble
{
    public enum FightUIMode
    {
        Challenge,
        Response
    }

    public class FightUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameObject panel;
        [SerializeField] private Button primaryButton;
        [SerializeField] private Button secondaryButton;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text leftPlayerNameText;
        [SerializeField] private TMP_Text rightPlayerNameText;
        [SerializeField] private Image attackerPortrait;
        [SerializeField] private Image defenderPortrait;
        [SerializeField] private Transform attackerInventory;
        [SerializeField] private Transform defenderInventory;
        [SerializeField] private TMP_Text rejectionRateText;

        private UniTaskCompletionSource<bool> decisionSource;

        public static FightUI Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
            ResolveReferences();

            primaryButton?.onClick.AddListener(SubmitPrimary);
            secondaryButton?.onClick.AddListener(SubmitSecondary);

            transform.localScale = Vector3.one;
            Hide();
        }

        private void OnDestroy()
        {
            primaryButton?.onClick.RemoveListener(SubmitPrimary);
            secondaryButton?.onClick.RemoveListener(SubmitSecondary);
            decisionSource?.TrySetResult(false);

            if (Instance == this)
                Instance = null;
        }

        public async UniTask<bool> ShowAsync(
            FightUIMode mode,
            string attackerId,
            string defenderId,
            double rejectionChance = 0.5)
        {
            decisionSource?.TrySetResult(false);
            decisionSource = new UniTaskCompletionSource<bool>();

            SetText(mode, attackerId, defenderId);
            if (panel == null)
            {
                Debug.LogError("[FightUI] 표시할 패널이 없습니다.");
                decisionSource = null;
                return false;
            }

            RefreshPlayerView(attackerId, attackerPortrait, attackerInventory);
            RefreshPlayerView(defenderId, defenderPortrait, defenderInventory);
            SetRejectionRate(mode, rejectionChance);
            panel.SetActive(true);

            bool result = await decisionSource.Task;
            Hide();
            decisionSource = null;
            return result;
        }

        public void Hide()
        {
            if (panel != null)
                panel.SetActive(false);
        }

        private void SubmitPrimary()
        {
            decisionSource?.TrySetResult(true);
        }

        private void SubmitSecondary()
        {
            decisionSource?.TrySetResult(false);
        }

        private void ResolveReferences()
        {
            panel ??= transform.Find("FightPannel")?.gameObject;

            Button[] buttons = GetComponentsInChildren<Button>(true);
            primaryButton ??= buttons.FirstOrDefault(button => button.name == "Go");
            secondaryButton ??= buttons.FirstOrDefault(button => button.name == "Back");

            TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(true);
            titleText ??= texts.FirstOrDefault(text => text.name == "Title Txt");
            leftPlayerNameText ??= texts.FirstOrDefault(text => text.name == "1PlayerName");
            rightPlayerNameText ??= texts.FirstOrDefault(text => text.name == "2PlayerName");

            Transform fightImage = panel?.transform.Find("FightImage");
            Transform attacker = fightImage?.Find("1Player");
            Transform defender = fightImage?.Find("2Player");

            attackerPortrait ??= attacker?.Find("1PlayerIcon")?.GetComponent<Image>();
            defenderPortrait ??= defender?.Find("2PlayerIcon")?.GetComponent<Image>();
            attackerInventory ??= attacker?.Find("1PInventory");
            defenderInventory ??= defender?.Find("2PInventory");
            rejectionRateText ??= secondaryButton?.transform.Find("Rate")?.GetComponent<TMP_Text>();

            if (panel == null || primaryButton == null || secondaryButton == null)
                Debug.LogError("[FightUI] FightPannel, Go, Back 참조를 찾을 수 없습니다.");
        }

        private void SetText(FightUIMode mode, string attackerId, string defenderId)
        {
            if (titleText != null)
            {
                titleText.SetText(mode == FightUIMode.Challenge
                    ? "결투를 신청하시겠습니까?"
                    : "결투 신청을 받았습니다.");
            }

            leftPlayerNameText?.SetText(GetDisplayName(attackerId));
            rightPlayerNameText?.SetText(GetDisplayName(defenderId));

            SetButtonText(primaryButton, mode == FightUIMode.Challenge ? "결투" : "승낙");
            SetButtonText(secondaryButton, mode == FightUIMode.Challenge ? "취소" : "거절");
        }

        private static void RefreshPlayerView(
            string playerId,
            Image playerIcon,
            Transform inventoryRoot)
        {
            RoomClient client = RoomClient.Instance;
            User user = client.GetUser(playerId);

            if (playerIcon != null)
            {
                PlayerAppearance appearance = PlayerAppearanceMap.Instance
                    .GetAppearance(user.PlayerNumber);
                playerIcon.sprite = appearance.Portrait;
            }

            bool isLocalPlayer = playerId == client.Me.Id;
            var equipmentStates = client.GetEquipmentStates(playerId);

            for (int slotIndex = 0; slotIndex < InventoryManager.MaxSlotCount; slotIndex++)
            {
                string slotName = slotIndex == 0 ? "Item" : $"Item ({slotIndex})";
                Transform slot = inventoryRoot?.Find(slotName);
                if (slot == null) continue;

                Equipment localEquipment = isLocalPlayer
                    ? InventoryManager.Instance.GetAt(slotIndex)
                    : null;
                EquipmentSlotState equipmentState = isLocalPlayer
                    ? null
                    : equipmentStates.FirstOrDefault(equipment => equipment.SlotIndex == slotIndex);
                EquipmentDef equipmentDef = localEquipment?.Def;

                if (equipmentDef == null && equipmentState != null)
                    equipmentDef = DataManager.Instance.GetEquipmentDef(equipmentState.EquipmentId);

                Image equipmentIcon = slot.GetComponent<Image>();
                Sprite icon = equipmentDef == null || string.IsNullOrEmpty(equipmentDef.IconKey)
                    ? null
                    : Resources.Load<Sprite>(equipmentDef.IconKey);

                if (equipmentIcon != null)
                {
                    equipmentIcon.sprite = icon;
                    equipmentIcon.enabled = icon != null;
                }

                TMP_Text durabilityText = slot.Find("Remain")?.GetComponent<TMP_Text>();
                bool showDurability = localEquipment != null && durabilityText != null;

                if (durabilityText != null)
                {
                    durabilityText.gameObject.SetActive(showDurability);

                    if (showDurability)
                        durabilityText.SetText(
                            $"{localEquipment.CurrentDurability}/{localEquipment.Def.MaxDurability}");
                }

                slot.gameObject.SetActive(equipmentDef != null);
            }
        }

        private void SetRejectionRate(FightUIMode mode, double rejectionChance)
        {
            if (rejectionRateText == null) return;

            bool isDefender = mode == FightUIMode.Response;
            rejectionRateText.gameObject.SetActive(isDefender);

            if (isDefender)
                rejectionRateText.SetText($"성공률 {rejectionChance * 100:0.##}%");
        }

        private static string GetDisplayName(string playerId)
        {
            RoomClient client = RoomClient.Instance;

            if (client.Me != null && client.Me.Id == playerId)
                return $"{client.Me.Nickname}({client.Me.Id})";

            return client.Others.TryGetValue(playerId, out User user)
                ? $"{user.Nickname}({user.Id})"
                : playerId;
        }

        private static void SetButtonText(Button button, string value)
        {
            if (button == null) return;

            TMP_Text text = button.GetComponentInChildren<TMP_Text>(true);
            text?.SetText(value);
        }
    }
}
