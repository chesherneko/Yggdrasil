using BlueMarble.Server;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace BlueMarble
{
    public class Profile : MonoBehaviour
    {
        [Title("Components")]
        [SerializeField] private Image portraitRenderer;
        [SerializeField] private TMP_Text nicknameText;
        [SerializeField] private TMP_Text goldText;
        [SerializeField] private TMP_Text totalGoldText;
        [FormerlySerializedAs("equipmentIcons")]
        [SerializeField] private Image[] equipmentSlots;
        [SerializeField] private GameObject bubble;
        [SerializeField] private TMP_Text bubbleText;
        [SerializeField] private GameObject bankrupt;

        public string Id { get; private set; }

        public void Initialize(User user)
        {
            Id = user.Id;

            PlayerAppearance appearance = PlayerAppearanceMap.Instance
                .GetAppearance(user.PlayerNumber);

            portraitRenderer.sprite = appearance.Portrait;
            nicknameText.text = $"{user.Nickname}({user.Id})";

            HideBubble();
            bankrupt.SetActive(false);
        }

        public void SetGold(int amount)
        {
            goldText.SetText($"자산: {amount:N0} 골드");
        }

        public void SetTotalGold(int amount)
        {
            totalGoldText.SetText($"총 자산: {amount:N0} 골드");
        }

        public void SetEquipmentIcons(IReadOnlyList<EquipmentSlotState> equipments)
        {
            for (int i = 0; i < equipmentSlots.Length; i++)
            {
                Image icon = equipmentSlots[i].transform.Find("Icon").GetComponent<Image>();
                icon.sprite = null;
                equipmentSlots[i].gameObject.SetActive(false);
            }

            for (int i = 0; i < equipments.Count; i++)
            {
                EquipmentSlotState equipment = equipments[i];
                if (equipment.SlotIndex < 0 || equipment.SlotIndex >= equipmentSlots.Length) continue;

                EquipmentDef def = DataManager.Instance.GetEquipmentDef(equipment.EquipmentId);
                Sprite iconSprite = string.IsNullOrEmpty(def.IconKey)
                    ? null
                    : Resources.Load<Sprite>(def.IconKey);

                Image slot = equipmentSlots[equipment.SlotIndex];
                Image icon = slot.transform.Find("Icon").GetComponent<Image>();
                icon.sprite = iconSprite;
                slot.gameObject.SetActive(iconSprite != null);
            }
        }

        public void ShowBubble(string message)
        {
            bubbleText.SetText(message);
            bubble.SetActive(true);
        }

        public void HideBubble()
        {
            bubble.SetActive(false);
        }

        public void SetBankrupt()
        {
            bankrupt.SetActive(true);
        }
    }
}
