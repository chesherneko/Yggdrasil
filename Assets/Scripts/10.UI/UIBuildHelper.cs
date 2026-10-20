using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace BlueMarble
{
    /// <summary>
    /// 런타임에 코드로 UI(팝업 등)를 만들 때 공통으로 쓰는 작은 도우미 모음.
    /// InventoryReplacePopup, SlotReplaceConfirmPopup 등에서 재사용해서 중복을 줄인다.
    /// </summary>
    public static class UIBuildHelper
    {
        public static GameObject NewRect(string name, Transform parent, Vector2 size)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.GetComponent<RectTransform>().sizeDelta = size;
            return go;
        }

        public static void CenterRect(RectTransform rect)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
        }

        public static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        /// <summary>스프라이트가 있으면 그걸 쓰고(9-slice), 없으면 지정한 색상 블록으로 대체한다.</summary>
        public static void ApplySprite(Image image, Sprite sprite, Color fallbackColor)
        {
            if (sprite != null)
            {
                image.sprite = sprite;
                image.type = Image.Type.Sliced;
                image.color = Color.white;
            }
            else
            {
                image.color = fallbackColor;
            }
        }

        public static TMP_Text CreateText(string name, Transform parent, string text, float fontSize, Vector2 anchoredPosition, Vector2 size)
        {
            GameObject go = NewRect(name, parent, size);
            RectTransform rect = go.GetComponent<RectTransform>();
            CenterRect(rect);
            rect.anchoredPosition = anchoredPosition;

            TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            return tmp;
        }

        public static Button CreateButton(string name, Transform parent, string label, Sprite sprite, Color fallbackColor, Vector2 anchoredPosition, Vector2 size)
        {
            GameObject go = NewRect(name, parent, size);
            RectTransform rect = go.GetComponent<RectTransform>();
            CenterRect(rect);
            rect.anchoredPosition = anchoredPosition;

            Image img = go.AddComponent<Image>();
            ApplySprite(img, sprite, fallbackColor);

            Button button = go.AddComponent<Button>();
            button.targetGraphic = img;

            CreateText("Label", go.transform, label, 24f, Vector2.zero, size);

            return button;
        }
    }
}