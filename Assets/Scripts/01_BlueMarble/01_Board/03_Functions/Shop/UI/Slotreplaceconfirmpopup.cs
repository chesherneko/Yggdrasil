using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using static BlueMarble.UIBuildHelper;

namespace BlueMarble
{
    /// <summary>
    /// 인벤토리 1~5번 칸 중 "차있는 칸"을 직접 클릭했을 때 뜨는 팝업.
    /// 예전처럼 5칸을 한꺼번에 보여주는 대신, 클릭한 그 칸 하나에 대해서만
    /// "교체하시겠습니까? Yes / No"를 확인한다.
    /// </summary>
    public class SlotReplaceConfirmPopup
    {
        private readonly GameObject _popupRoot;
        private readonly TMP_Text _messageText;
        private readonly Button _yesButton;
        private readonly Button _noButton;

        public SlotReplaceConfirmPopup(Transform canvasRoot, Sprite panelSprite = null, Sprite buttonSprite = null)
        {
            Transform existing = canvasRoot != null ? canvasRoot.Find("SlotReplaceConfirmPopup") : null;

            if (existing != null)
            {
                _popupRoot = existing.gameObject;
                _messageText = existing.Find("MessageText")?.GetComponent<TMP_Text>();
                _yesButton = existing.Find("YesButton")?.GetComponent<Button>();
                _noButton = existing.Find("NoButton")?.GetComponent<Button>();
            }
            else
            {
                (_popupRoot, _messageText, _yesButton, _noButton) = Build(canvasRoot, panelSprite, buttonSprite);
            }

            _popupRoot.SetActive(false);
        }

        public void Show(string currentItemName, string newItemName, Action onYes, Action onNo)
        {
            if (_messageText != null)
            {
                _messageText.text = $"[{currentItemName}] 대신\n[{newItemName}](으)로\n교체하시겠습니까?";
            }

            _yesButton.onClick.RemoveAllListeners();
            _yesButton.onClick.AddListener(() => onYes());

            _noButton.onClick.RemoveAllListeners();
            _noButton.onClick.AddListener(() => onNo());

            _popupRoot.SetActive(true);
        }

        public void Hide()
        {
            _popupRoot.SetActive(false);
        }

        private static (GameObject root, TMP_Text message, Button yes, Button no) Build(
            Transform canvasRoot, Sprite panelSprite, Sprite buttonSprite)
        {
            GameObject rootGo = NewRect("SlotReplaceConfirmPopup", canvasRoot, Vector2.zero);
            Stretch(rootGo.GetComponent<RectTransform>());
            rootGo.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);

            GameObject boxGo = NewRect("PopupBox", rootGo.transform, new Vector2(480f, 280f));
            CenterRect(boxGo.GetComponent<RectTransform>());
            Image boxImg = boxGo.AddComponent<Image>();
            ApplySprite(boxImg, panelSprite, new Color(0.17f, 0.18f, 0.21f, 1f));

            TMP_Text messageText = CreateText("MessageText", boxGo.transform, "교체하시겠습니까?", 26f,
                new Vector2(0f, 50f), new Vector2(420f, 140f));

            Button yesButton = CreateButton("YesButton", boxGo.transform, "Yes", buttonSprite,
                new Color(0.22f, 0.5f, 0.82f, 1f), new Vector2(-100f, -80f), new Vector2(180f, 60f));

            Button noButton = CreateButton("NoButton", boxGo.transform, "No", buttonSprite,
                new Color(0.4f, 0.4f, 0.42f, 1f), new Vector2(100f, -80f), new Vector2(180f, 60f));

            rootGo.SetActive(false);

            return (rootGo, messageText, yesButton, noButton);
        }
    }
}