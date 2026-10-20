using System.Collections.Generic;
using BlueMarble;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Combat
{
    [DisallowMultipleComponent]
    public class CombatUI : MonoBehaviour
    {
        [Header("Combat HUD")]
        [SerializeField] private TMP_Text timeText;
        [SerializeField] private TMP_Text bonusText;
        [SerializeField] private Image leftHpFill;
        [SerializeField] private Image rightHpFill;
        [SerializeField] private Transform equipmentsRoot;

        [Header("Result UI")]
        [SerializeField] private CanvasGroup winnerPanel;
        [SerializeField] private TMP_Text winnerNameText;
        [SerializeField] private Image winnerBackground;

        private CombatSystem combatSystem;
        private Timer combatTimer;
        private readonly List<EquipmentSlotView> equipmentSlots = new();
        private bool isInitialized;

        private sealed class EquipmentSlotView
        {
            public GameObject Root;
            public Image Icon;
            public Image Cooldown;
            public Equipment Equipment;
        }

        public void Initialize(CombatSystem system, Timer timer)
        {
            combatSystem = system;
            combatTimer = timer;

            FindReferences();
            ConfigureHealthFill(leftHpFill);
            ConfigureHealthFill(rightHpFill);
            CacheEquipmentSlots();
            HideWinner();

            isInitialized = true;
        }

        private void Update()
        {
            if (!isInitialized || combatSystem == null) return;

            RefreshTime();
            RefreshBonus();
            RefreshHealth();
            RefreshEquipmentSlots();
        }

        public void ShowWinner(string winnerName)
        {
            if (winnerNameText != null)
                winnerNameText.SetText($"{winnerName} 승리!!");

            if (winnerPanel == null) return;

            winnerPanel.alpha = 1f;
            winnerPanel.interactable = true;
            winnerPanel.blocksRaycasts = true;
        }

        private void FindReferences()
        {
            timeText ??= transform.Find("Time")?.GetComponent<TMP_Text>();
            bonusText ??= transform.Find("Bonus")?.GetComponent<TMP_Text>();
            leftHpFill ??= transform.Find("HP_Left/Bar")?.GetComponent<Image>();
            rightHpFill ??= transform.Find("HP_Right/Bar")?.GetComponent<Image>();
            equipmentsRoot ??= transform.Find("Equipments");

            Transform winnerTransform = transform.Find("Winner");
            if (winnerTransform == null) return;

            winnerPanel ??= winnerTransform.GetComponent<CanvasGroup>();
            winnerNameText ??= winnerTransform.Find("Desc")?.GetComponent<TMP_Text>();
            winnerBackground ??= winnerTransform.GetComponent<Image>();

            if (winnerBackground != null)
            {
                Color backgroundColor = winnerBackground.color;
                backgroundColor.a = 0.65f;
                winnerBackground.color = backgroundColor;
            }

            Transform rewardTransform = winnerTransform.Find("Reward");
            if (rewardTransform != null)
                rewardTransform.gameObject.SetActive(false);
        }

        private void HideWinner()
        {
            if (winnerPanel == null) return;

            winnerPanel.alpha = 0f;
            winnerPanel.interactable = false;
            winnerPanel.blocksRaycasts = false;
        }

        private static void ConfigureHealthFill(Image image)
        {
            if (image == null) return;

            image.type = Image.Type.Filled;
            image.fillMethod = Image.FillMethod.Horizontal;
            image.fillOrigin = (int)Image.OriginHorizontal.Left;
            image.fillClockwise = true;
            image.fillAmount = 1f;
        }

        private void CacheEquipmentSlots()
        {
            equipmentSlots.Clear();
            if (equipmentsRoot == null) return;

            List<Transform> slotTransforms = new();
            for (int i = 0; i < equipmentsRoot.childCount; i++)
                slotTransforms.Add(equipmentsRoot.GetChild(i));

            slotTransforms.Sort((left, right) =>
                string.CompareOrdinal(left.name, right.name));

            foreach (Transform slotTransform in slotTransforms)
            {
                equipmentSlots.Add(new EquipmentSlotView
                {
                    Root = slotTransform.gameObject,
                    Icon = slotTransform.Find("Icon")?.GetComponent<Image>(),
                    Cooldown = slotTransform.Find("Cooldown")?.GetComponent<Image>()
                });

                slotTransform.gameObject.SetActive(false);
            }
        }

        private void RefreshTime()
        {
            if (timeText == null) return;

            float remainingTime = combatTimer != null && combatTimer.IsRunning
                ? combatTimer.RemainingTime
                : combatSystem.DurationSeconds;

            timeText.SetText(Mathf.CeilToInt(remainingTime).ToString());
        }

        private void RefreshBonus()
        {
            if (bonusText != null)
                bonusText.SetText($"데미지 배율 x{combatSystem.DamageMultiplier:0.#}");
        }

        private void RefreshHealth()
        {
            SetHealthFill(leftHpFill, combatSystem.AttackerId);
            SetHealthFill(rightHpFill, combatSystem.DefenderId);
        }

        private void SetHealthFill(Image healthFill, string playerId)
        {
            if (healthFill == null || string.IsNullOrEmpty(playerId)) return;

            if (!combatSystem.TryGetEntity(playerId, out CombatEntity entity) ||
                entity.MaxHp <= 0)
            {
                return;
            }

            healthFill.fillAmount = Mathf.Clamp01((float)entity.CurrentHp / entity.MaxHp);
        }

        private void RefreshEquipmentSlots()
        {
            int displayIndex = 0;

            for (int inventoryIndex = 0;
                 inventoryIndex < InventoryManager.MaxSlotCount &&
                 displayIndex < equipmentSlots.Count;
                 inventoryIndex++)
            {
                Equipment equipment = InventoryManager.Instance.GetAt(inventoryIndex);
                if (equipment == null) continue;

                EquipmentSlotView slot = equipmentSlots[displayIndex++];

                if (!slot.Root.activeSelf)
                    slot.Root.SetActive(true);

                if (slot.Equipment != equipment)
                {
                    slot.Equipment = equipment;
                    RefreshEquipmentIcon(slot, equipment);
                }

                if (slot.Cooldown != null)
                {
                    slot.Cooldown.fillAmount = equipment.Def.Cooldown > 0f
                        ? Mathf.Clamp01(equipment.RemainingCooldown / equipment.Def.Cooldown)
                        : 0f;
                }
            }

            for (int i = displayIndex; i < equipmentSlots.Count; i++)
            {
                EquipmentSlotView slot = equipmentSlots[i];
                slot.Equipment = null;

                if (slot.Root.activeSelf)
                    slot.Root.SetActive(false);
            }
        }

        private static void RefreshEquipmentIcon(
            EquipmentSlotView slot,
            Equipment equipment)
        {
            if (slot.Icon == null) return;

            Sprite icon = string.IsNullOrEmpty(equipment.Def.IconKey)
                ? null
                : Resources.Load<Sprite>(equipment.Def.IconKey);

            slot.Icon.sprite = icon;
            slot.Icon.enabled = icon != null;
        }
    }
}
