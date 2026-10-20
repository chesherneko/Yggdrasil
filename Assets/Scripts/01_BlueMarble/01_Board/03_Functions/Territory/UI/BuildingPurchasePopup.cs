using UnityEngine;
using UnityEngine.UI;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace BlueMarble
{
    /// <summary>
    /// 영토 구매 팝업 화면을 코드만으로 조립하는 에디터 툴.
    /// PropertyManager(Epic3)의 OnShowPurchaseUI(TileState, int) / SubmitPurchaseDecision(bool,bool,bool)
    /// 구조에 맞춰 "영토(필수) / 건물(선택) / 랜드마크(선택)" 3항목 구조로 설계했다.
    /// 빈 GameObject에 붙인 뒤 컴포넌트 우클릭 메뉴로 실행한다.
    /// 게임 로직은 전혀 연결하지 않으며, 화면(뼈대)만 생성한다. (연동은 BuildingPurchasePopupController가 담당)
    /// </summary>
    public class BuildingPurchasePopup : MonoBehaviour
    {
#if UNITY_EDITOR

        #region 필드 (인스펙터에서 채울 것)

        [Header("공통")]
        [SerializeField] private TMP_FontAsset font; // 모든 텍스트에 공통 적용할 한글 폰트

        [Header("아이콘 스프라이트 슬롯 (전체 스프라이트로 교체 가능)")]
        [SerializeField] private Sprite closeIconSprite;    // 닫기(X) 버튼 아이콘
        [SerializeField] private Sprite tileIconSprite;      // 영토 카드 아이콘
        [SerializeField] private Sprite buildingIconSprite;  // 건물 카드 아이콘
        [SerializeField] private Sprite landmarkIconSprite;  // 랜드마크 카드 아이콘
        [SerializeField] private Sprite coinIconSprite;      // 구매 버튼 코인 아이콘

        [Header("체크박스 체크 표시 스프라이트 슬롯")]
        [SerializeField] private Sprite buildingCheckIconSprite;  // 건물 카드 체크박스 표시
        [SerializeField] private Sprite landmarkCheckIconSprite;  // 랜드마크 카드 체크박스 표시

        private const string RootName = "BuildingPurchasePopup";

        // 기준 해상도
        private static readonly Vector2 ReferenceResolution = new Vector2(1920f, 1080f);

        #endregion


        #region 진입점 (Build / Clear)

        [ContextMenu("1. Build UI")]
        private void BuildUI()
        {
            // 이미 루트가 있으면 중복 생성 방지
            if (GameObject.Find(RootName) != null)
            {
                Debug.LogWarning($"[{RootName}] 이미 씬에 존재합니다. 중복 생성을 막기 위해 중단합니다. " +
                                  "먼저 '2. Clear UI'로 지운 뒤 다시 시도하세요.");
                return;
            }

            // 씬에 EventSystem이 없으면 경고만 남기고 계속 진행
            if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                Debug.LogWarning("씬에 EventSystem이 없습니다. 버튼 입력을 받으려면 EventSystem을 추가하세요.");
            }

            GameObject root = new GameObject(RootName);
            Undo.RegisterCreatedObjectUndo(root, "Create BuildingPurchasePopup");

            Transform canvasTransform = CreateCanvas(root.transform);

            CreateDimBackground(canvasTransform);
            Transform popupFrame = CreatePopupFrame(canvasTransform);

            CreateTitleBar(popupFrame);
            CreateCardArea(popupFrame);
            CreatePurchaseButton(popupFrame);

            EditorSceneManager.MarkSceneDirty(gameObject.scene);
            Debug.Log($"[{RootName}] 생성 완료. 실제 데이터 연동은 BuildingPurchasePopupController를 붙여서 진행하세요.");
        }

        [ContextMenu("2. Clear UI")]
        private void ClearUI()
        {
            GameObject root = GameObject.Find(RootName);
            if (root == null)
            {
                Debug.Log($"[{RootName}] 씬에 해당 오브젝트가 없습니다. 지울 것이 없습니다.");
                return;
            }

            Undo.DestroyObjectImmediate(root);
            Debug.Log($"[{RootName}] 오브젝트를 지웠습니다.");
        }

        #endregion


        #region 도우미 메서드

        private GameObject NewUI(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private GameObject NewPanel(string name, Transform parent, Vector2 size)
        {
            GameObject go = NewUI(name, parent);
            Image img = go.AddComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0.15f); // 기본 임시 색상(용도별로 바깥에서 재설정)

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = size;
            return go;
        }

        private TMP_Text NewText(string name, Transform parent, string text, float size, TextAlignmentOptions align)
        {
            GameObject go = NewUI(name, parent);
            TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.alignment = align;
            tmp.color = Color.black;
            if (font != null)
            {
                tmp.font = font;
            }
            return tmp;
        }

        private Button NewButton(string name, Transform parent, string label, out TMP_Text labelText)
        {
            GameObject go = NewUI(name, parent);
            Image img = go.AddComponent<Image>();
            img.color = Color.white;

            Button button = go.AddComponent<Button>();
            button.targetGraphic = img;

            labelText = NewText($"{name}_Label", go.transform, label, 28f, TextAlignmentOptions.Center);
            Stretch(labelText.rectTransform, 0f);

            return button;
        }

        private void Stretch(RectTransform rect, float padding)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(padding, padding);
            rect.offsetMax = new Vector2(-padding, -padding);
        }

        #endregion


        #region Canvas 생성

        private Transform CreateCanvas(Transform root)
        {
            GameObject canvasGO = NewUI("Canvas", root);

            Canvas canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.matchWidthOrHeight = 0.5f;

            canvasGO.AddComponent<GraphicRaycaster>();

            RectTransform canvasRect = canvasGO.GetComponent<RectTransform>();
            Stretch(canvasRect, 0f);

            return canvasGO.transform;
        }

        #endregion


        #region 딤드 배경

        private void CreateDimBackground(Transform canvas)
        {
            GameObject dim = NewPanel("DimBackground", canvas, Vector2.zero);
            Image img = dim.GetComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 0.6f);

            RectTransform rect = dim.GetComponent<RectTransform>();
            Stretch(rect, 0f);
        }

        #endregion


        #region 팝업 프레임

        private Transform CreatePopupFrame(Transform canvas)
        {
            Vector2 frameSize = new Vector2(900f, 600f);

            GameObject frame = NewPanel("PopupFrame", canvas, frameSize);
            Image img = frame.GetComponent<Image>();
            img.color = new Color32(0xF3, 0xE3, 0xC0, 0xFF); // 크림색 배경(임시)

            RectTransform rect = frame.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = frameSize;

            return frame.transform;
        }

        #endregion


        #region 타이틀 바

        private void CreateTitleBar(Transform popupFrame)
        {
            GameObject titleBar = NewPanel("TitleBar", popupFrame, new Vector2(0f, 110f));
            Image img = titleBar.GetComponent<Image>();
            img.color = new Color32(0xE8, 0xC0, 0x6A, 0xFF); // 금색 리본 톤

            RectTransform rect = titleBar.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, 0f);
            rect.sizeDelta = new Vector2(-40f, 110f);

            // 타이틀 텍스트 (내용은 비워둠 - 직접 채울 것)
            TMP_Text titleText = NewText("TitleText", titleBar.transform, "", 40f, TextAlignmentOptions.Center);
            Stretch(titleText.rectTransform, 20f);

            // 닫기 버튼 (배경 투명 + 스프라이트 아이콘)
            GameObject closeButtonGO = NewUI("CloseButton", popupFrame);
            Image closeBg = closeButtonGO.AddComponent<Image>();
            closeBg.color = new Color(1f, 1f, 1f, 0f);
            Button closeButton = closeButtonGO.AddComponent<Button>();
            closeButton.targetGraphic = closeBg;

            RectTransform closeRect = closeButtonGO.GetComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(1f, 1f);
            closeRect.anchorMax = new Vector2(1f, 1f);
            closeRect.pivot = new Vector2(1f, 1f);
            closeRect.anchoredPosition = new Vector2(-20f, -20f);
            closeRect.sizeDelta = new Vector2(70f, 70f);

            GameObject closeIconGO = NewUI("CloseIcon", closeButtonGO.transform);
            Image closeIcon = closeIconGO.AddComponent<Image>();
            closeIcon.sprite = closeIconSprite;
            closeIcon.color = closeIconSprite == null ? new Color(0.7f, 0.2f, 0.2f, 1f) : Color.white;
            Stretch(closeIconGO.GetComponent<RectTransform>(), 10f);
        }

        #endregion


        #region 카드 영역 (영토 / 건물 / 랜드마크)

        private void CreateCardArea(Transform popupFrame)
        {
            GameObject cardArea = NewUI("CardArea", popupFrame);
            RectTransform areaRect = cardArea.GetComponent<RectTransform>();
            areaRect.anchorMin = new Vector2(0f, 1f);
            areaRect.anchorMax = new Vector2(1f, 1f);
            areaRect.pivot = new Vector2(0.5f, 1f);
            areaRect.anchoredPosition = new Vector2(0f, -130f);
            areaRect.sizeDelta = new Vector2(-40f, 300f);

            HorizontalLayoutGroup layout = cardArea.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 12f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;
            layout.padding = new RectOffset(0, 0, 0, 0);

            // 영토: 체크박스 없음(항상 필수) / 건물·랜드마크: 체크박스 있음, 아이콘도 전부 스프라이트
            CreateCard(cardArea.transform, "Card_Tile", "영토", tileIconSprite, false, null);
            CreateCard(cardArea.transform, "Card_Building", "건물", buildingIconSprite, true, buildingCheckIconSprite);
            CreateCard(cardArea.transform, "Card_Landmark", "랜드마크", landmarkIconSprite, true, landmarkCheckIconSprite);
        }

        /// <summary>
        /// 카드 하나를 생성한다. 아이콘은 전부 Sprite 슬롯 방식으로 통일했다.
        /// </summary>
        private void CreateCard(Transform parent, string name, string headerLabel,
            Sprite iconSprite, bool hasCheckbox, Sprite checkIconSprite)
        {
            GameObject card = NewPanel(name, parent, new Vector2(270f, 300f));
            Image cardBg = card.GetComponent<Image>();
            cardBg.color = new Color32(0xFD, 0xF6, 0xE3, 0xFF);

            // 카드 자체는 레이아웃 그룹이 크기를 통제하므로 LayoutElement로 선호 크기만 지정
            LayoutElement layoutElement = card.AddComponent<LayoutElement>();
            layoutElement.preferredWidth = 270f;
            layoutElement.preferredHeight = 300f;

            // 헤더 라벨 (상단 고정)
            TMP_Text header = NewText($"{name}_HeaderText", card.transform, headerLabel, 26f, TextAlignmentOptions.Center);
            RectTransform headerRect = header.rectTransform;
            headerRect.anchorMin = new Vector2(0f, 1f);
            headerRect.anchorMax = new Vector2(1f, 1f);
            headerRect.pivot = new Vector2(0.5f, 1f);
            headerRect.anchoredPosition = new Vector2(0f, 0f);
            headerRect.sizeDelta = new Vector2(0f, 40f);

            const float iconTopOffset = 45f;

            // 아이콘 슬롯 (스프라이트 - 인스펙터에서 직접 교체 가능)
            GameObject iconSlotGO = NewUI($"{name}_IconSlot", card.transform);
            RectTransform iconRect = iconSlotGO.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.5f, 1f);
            iconRect.anchorMax = new Vector2(0.5f, 1f);
            iconRect.pivot = new Vector2(0.5f, 1f);
            iconRect.anchoredPosition = new Vector2(0f, -iconTopOffset);
            iconRect.sizeDelta = new Vector2(120f, 120f);

            Image iconImg = iconSlotGO.AddComponent<Image>();
            iconImg.sprite = iconSprite;
            iconImg.color = iconSprite == null ? new Color(0.6f, 0.8f, 0.6f, 1f) : Color.white;

            // 체크박스 (아이콘 바로 아래, 건물·랜드마크 카드에만 존재)
            if (hasCheckbox)
            {
                float checkboxTopOffset = iconTopOffset + 120f + 10f; // 아이콘 하단에서 10px 여백

                GameObject toggleGO = NewUI($"{name}_CheckboxToggle", card.transform);
                Image toggleBg = toggleGO.AddComponent<Image>();
                toggleBg.color = new Color32(0xEE, 0xEE, 0xEE, 0xFF); // 배경은 고정 색상 블록(스프라이트 슬롯 없음)

                Toggle toggle = toggleGO.AddComponent<Toggle>();
                toggle.targetGraphic = toggleBg;
                toggle.isOn = false; // 초기 상태: 미체크

                RectTransform toggleRect = toggleGO.GetComponent<RectTransform>();
                toggleRect.anchorMin = new Vector2(0.5f, 1f);
                toggleRect.anchorMax = new Vector2(0.5f, 1f);
                toggleRect.pivot = new Vector2(0.5f, 1f);
                toggleRect.anchoredPosition = new Vector2(0f, -checkboxTopOffset);
                toggleRect.sizeDelta = new Vector2(40f, 40f);

                // 체크 표시 (스프라이트 슬롯 - 인스펙터에서 직접 교체 가능)
                GameObject checkmarkGO = NewUI($"{name}_CheckboxCheckmark", toggleGO.transform);
                Image checkmarkImg = checkmarkGO.AddComponent<Image>();
                checkmarkImg.sprite = checkIconSprite;
                checkmarkImg.color = checkIconSprite == null ? new Color(0.85f, 0.2f, 0.2f, 1f) : Color.white;
                checkmarkImg.enabled = toggle.isOn; // 초기 상태와 일치(미체크면 숨김)
                Stretch(checkmarkGO.GetComponent<RectTransform>(), 6f);

                toggle.graphic = checkmarkImg;
            }
        }

        #endregion


        #region 구매 버튼

        private void CreatePurchaseButton(Transform popupFrame)
        {
            GameObject buttonGO = NewUI("PurchaseButton", popupFrame);
            Image bg = buttonGO.AddComponent<Image>();
            bg.color = new Color32(0x2C, 0x3E, 0x50, 0xFF); // 네이비 톤 (임시)

            Button button = buttonGO.AddComponent<Button>();
            button.targetGraphic = bg;

            RectTransform rect = buttonGO.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -450f);
            rect.sizeDelta = new Vector2(-40f, 90f);

            // "구매" 라벨 (좌측)
            TMP_Text labelText = NewText("PurchaseButton_LabelText", buttonGO.transform, "구매", 28f, TextAlignmentOptions.Center);
            labelText.color = Color.white;
            RectTransform labelRect = labelText.rectTransform;
            labelRect.anchorMin = new Vector2(0f, 0f);
            labelRect.anchorMax = new Vector2(0f, 1f);
            labelRect.pivot = new Vector2(0f, 0.5f);
            labelRect.anchoredPosition = new Vector2(30f, 0f);
            labelRect.sizeDelta = new Vector2(100f, 0f);

            // 코인 아이콘 (중앙 우측)
            GameObject coinGO = NewUI("CoinIcon", buttonGO.transform);
            Image coinImg = coinGO.AddComponent<Image>();
            coinImg.sprite = coinIconSprite;
            coinImg.color = coinIconSprite == null ? new Color(0.9f, 0.75f, 0.2f, 1f) : Color.white;

            RectTransform coinRect = coinGO.GetComponent<RectTransform>();
            coinRect.anchorMin = new Vector2(1f, 0.5f);
            coinRect.anchorMax = new Vector2(1f, 0.5f);
            coinRect.pivot = new Vector2(1f, 0.5f);
            coinRect.anchoredPosition = new Vector2(-160f, 0f);
            coinRect.sizeDelta = new Vector2(44f, 44f);

            // 가격 텍스트 (우측, 값은 비워둠 - Controller가 tilePrice로 채움)
            TMP_Text priceText = NewText("PurchaseButton_PriceText", buttonGO.transform, "", 28f, TextAlignmentOptions.Right);
            priceText.color = Color.white;
            RectTransform priceRect = priceText.rectTransform;
            priceRect.anchorMin = new Vector2(1f, 0f);
            priceRect.anchorMax = new Vector2(1f, 1f);
            priceRect.pivot = new Vector2(1f, 0.5f);
            priceRect.anchoredPosition = new Vector2(-30f, 0f);
            priceRect.sizeDelta = new Vector2(200f, 0f);
        }

        #endregion

#endif
    }
}