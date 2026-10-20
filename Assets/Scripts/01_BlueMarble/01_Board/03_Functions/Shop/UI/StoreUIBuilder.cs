#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;

namespace BlueMarble
{
    /// <summary>
    /// 상점 UI 화면을 코드로 자동 생성하는 에디터 전용 빌더.
    /// 빈 GameObject에 이 스크립트를 붙이고, 컴포넌트 우클릭 메뉴에서
    /// "1. Build UI" / "2. Clear UI" 를 실행한다.
    ///
    /// 주의: 이 스크립트는 화면(하이어라키)만 생성한다.
    /// ShopService, InventoryManager 등 실제 게임 로직과의 연결은 별도 스크립트에서 처리한다.
    /// </summary>
    public class StoreUIBuilder : MonoBehaviour
    {
        #region 설정값

        private const string RootName = "Store UI";
        private static readonly Vector2 ReferenceResolution = new Vector2(1920f, 1080f);

        [Header("한글 폰트 (선택)")]
        [SerializeField] private TMP_FontAsset font;

        [Header("슬롯 개수")]
        [SerializeField] private int shopSlotCount = 5;

        [Header("에셋 스프라이트 (선택 - 비워두면 색상 블록 유지)")]
        [Tooltip("패널/카드/헤더 배경에 쓸 9-slice 스프라이트. 비워두면 지금처럼 단색 블록으로 나온다.")]
        [SerializeField] private Sprite panelSprite;
        [Tooltip("버튼 배경에 쓸 9-slice 스프라이트. 비워두면 지금처럼 단색 블록으로 나온다.")]
        [SerializeField] private Sprite buttonSprite;

        // 차분하고 모던한 회색/파랑 톤
        private static readonly Color ColorBackgroundDark = new Color(0.11f, 0.12f, 0.14f, 1f);
        private static readonly Color ColorPanelFrame = new Color(0.24f, 0.32f, 0.42f, 1f);
        private static readonly Color ColorCardBody = new Color(0.17f, 0.18f, 0.21f, 1f);
        private static readonly Color ColorGold = new Color(0.95f, 0.78f, 0.35f, 1f);
        private static readonly Color ColorBuyButton = new Color(0.22f, 0.5f, 0.82f, 1f);
        private static readonly Color ColorNeutralButton = new Color(0.32f, 0.42f, 0.52f, 1f);
        private static readonly Color ColorDim = new Color(0f, 0f, 0f, 0.55f);
        private static readonly Color ColorTooltipBody = new Color(0.08f, 0.09f, 0.11f, 0.95f);

        #endregion

        #region 진입점 (ContextMenu)

        [ContextMenu("1. Build UI")]
        private void BuildUI()
        {
            if (GameObject.Find(RootName) != null)
            {
                Debug.LogWarning($"[StoreUIBuilder] 이미 '{RootName}' 오브젝트가 존재합니다. 먼저 Clear UI를 실행하세요.");
                return;
            }

            if (FindFirstObjectByType<EventSystem>() == null)
            {
                Debug.LogWarning("[StoreUIBuilder] 씬에 EventSystem이 없습니다. 버튼 클릭이 동작하지 않을 수 있으니 EventSystem을 추가하세요.");
            }

            GameObject rootGo = CreateCanvas();
            Undo.RegisterCreatedObjectUndo(rootGo, "Build Store UI");

            // ShopPanel 하나로 통합 - 열리면 헤더/아이템목록/인벤토리가 한 번에 다 나온다.
            GameObject shopPanelGo = CreateShopPanel(rootGo.transform, out Button closeButton);

            // 상점 닫기 확인 팝업 (Yes -> 닫기, No -> 팝업만 닫기)
            GameObject confirmPopupGo = CreateConfirmClosePopup(rootGo.transform, shopPanelGo);
            confirmPopupGo.SetActive(false);

            // 상점 닫기 버튼은 바로 닫지 않고, 확인 팝업을 먼저 띄운다.
            UnityEventTools.AddBoolPersistentListener(closeButton.onClick, confirmPopupGo.SetActive, true);

            // 처음에는 상점 화면이 꺼져 있어야 한다 (게임 로직이 상점 타일에서 열어줄 때만 켜짐).
            shopPanelGo.SetActive(false);

            EditorSceneManager.MarkSceneDirty(gameObject.scene);
            Debug.Log($"[StoreUIBuilder] '{RootName}' 생성 완료.");
        }

        [ContextMenu("2. Clear UI")]
        private void ClearUI()
        {
            GameObject existing = GameObject.Find(RootName);
            if (existing == null)
            {
                Debug.LogWarning($"[StoreUIBuilder] '{RootName}' 오브젝트를 찾을 수 없습니다.");
                return;
            }

            Undo.DestroyObjectImmediate(existing);
            Debug.Log($"[StoreUIBuilder] '{RootName}' 삭제 완료.");
        }

        #endregion

        #region Canvas 생성

        private GameObject CreateCanvas()
        {
            GameObject go = new GameObject(RootName, typeof(RectTransform));
            Canvas canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.matchWidthOrHeight = 0.5f;

            go.AddComponent<GraphicRaycaster>();

            return go;
        }

        #endregion

        #region 도우미 메서드

        private GameObject NewUI(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private GameObject NewPanel(string name, Transform parent, Vector2 size, Sprite spriteOverride = null)
        {
            GameObject go = NewUI(name, parent);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = size;

            Image image = go.AddComponent<Image>();

            if (spriteOverride != null)
            {
                image.sprite = spriteOverride;
                image.type = Image.Type.Sliced;
                image.color = Color.white; // 스프라이트 원래 색 그대로. 필요하면 호출부에서 다시 tint 가능.
            }
            else
            {
                image.color = ColorCardBody;
            }

            return go;
        }

        private TMP_Text NewText(string name, Transform parent, string text, float size, TextAlignmentOptions align)
        {
            GameObject go = NewUI(name, parent);
            TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.alignment = align;
            tmp.color = Color.white;

            if (font != null)
            {
                tmp.font = font;
            }

            return tmp;
        }

        private Button NewButton(string name, Transform parent, string label, out TMP_Text labelText, Sprite spriteOverride = null)
        {
            GameObject go = NewUI(name, parent);
            Image image = go.AddComponent<Image>();

            if (spriteOverride != null)
            {
                image.sprite = spriteOverride;
                image.type = Image.Type.Sliced;
                image.color = Color.white; // 필요하면 호출부에서 다시 tint 가능
            }
            else
            {
                image.color = ColorNeutralButton;
            }

            Button button = go.AddComponent<Button>();
            button.targetGraphic = image;

            labelText = NewText("Label", go.transform, label, 28f, TextAlignmentOptions.Center);
            RectTransform labelRect = labelText.rectTransform;
            Stretch(labelRect, 0f);

            return button;
        }

        private void Stretch(RectTransform rect, float padding)
        {
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.offsetMin = new Vector2(padding, padding);
            rect.offsetMax = new Vector2(-padding, -padding);
        }

        #endregion

        #region 상점 닫기 확인 팝업

        private GameObject CreateConfirmClosePopup(Transform canvasTransform, GameObject shopPanelGo)
        {
            // 화면 전체를 덮는 딤 처리
            GameObject popupRootGo = NewPanel("ConfirmClosePopup", canvasTransform, Vector2.zero);
            RectTransform popupRootRect = popupRootGo.GetComponent<RectTransform>();
            Stretch(popupRootRect, 0f);
            popupRootGo.GetComponent<Image>().color = ColorDim;

            // 팝업 박스
            GameObject boxGo = NewPanel("PopupBox", popupRootGo.transform, new Vector2(560f, 320f), panelSprite);
            RectTransform boxRect = boxGo.GetComponent<RectTransform>();
            boxRect.anchorMin = new Vector2(0.5f, 0.5f);
            boxRect.anchorMax = new Vector2(0.5f, 0.5f);
            boxRect.pivot = new Vector2(0.5f, 0.5f);
            boxRect.anchoredPosition = Vector2.zero;
            boxGo.GetComponent<Image>().color = ColorCardBody;

            Outline boxOutline = boxGo.AddComponent<Outline>();
            boxOutline.effectColor = ColorPanelFrame;
            boxOutline.effectDistance = new Vector2(3f, -3f);

            // 안내 문구
            TMP_Text messageText = NewText("MessageText", boxGo.transform, "상점을 닫으시겠습니까?", 32f, TextAlignmentOptions.Center);
            RectTransform messageRect = messageText.rectTransform;
            messageRect.anchorMin = new Vector2(0.5f, 1f);
            messageRect.anchorMax = new Vector2(0.5f, 1f);
            messageRect.pivot = new Vector2(0.5f, 1f);
            messageRect.sizeDelta = new Vector2(500f, 60f);
            messageRect.anchoredPosition = new Vector2(0f, -40f);

            // 경고 문구
            TMP_Text warningText = NewText("WarningText", boxGo.transform, "(주의) 재입장이 불가능합니다", 24f, TextAlignmentOptions.Center);
            warningText.color = new Color(0.9f, 0.4f, 0.35f, 1f);
            RectTransform warningRect = warningText.rectTransform;
            warningRect.anchorMin = new Vector2(0.5f, 1f);
            warningRect.anchorMax = new Vector2(0.5f, 1f);
            warningRect.pivot = new Vector2(0.5f, 1f);
            warningRect.sizeDelta = new Vector2(500f, 50f);
            warningRect.anchoredPosition = new Vector2(0f, -110f);

            // Yes 버튼 (왼쪽)
            Button yesButton = NewButton("YesButton", boxGo.transform, "Yes", out _, buttonSprite);
            RectTransform yesRect = yesButton.GetComponent<RectTransform>();
            yesRect.anchorMin = new Vector2(0.5f, 0f);
            yesRect.anchorMax = new Vector2(0.5f, 0f);
            yesRect.pivot = new Vector2(0.5f, 0f);
            yesRect.sizeDelta = new Vector2(200f, 70f);
            yesRect.anchoredPosition = new Vector2(-130f, 40f);
            yesButton.image.color = ColorBuyButton;

            // No 버튼 (오른쪽)
            Button noButton = NewButton("NoButton", boxGo.transform, "No", out _, buttonSprite);
            RectTransform noRect = noButton.GetComponent<RectTransform>();
            noRect.anchorMin = new Vector2(0.5f, 0f);
            noRect.anchorMax = new Vector2(0.5f, 0f);
            noRect.pivot = new Vector2(0.5f, 0f);
            noRect.sizeDelta = new Vector2(200f, 70f);
            noRect.anchoredPosition = new Vector2(130f, 40f);
            noButton.image.color = ColorNeutralButton;

            // Yes -> 상점을 닫고, 팝업도 닫는다.
            UnityEventTools.AddBoolPersistentListener(yesButton.onClick, shopPanelGo.SetActive, false);
            UnityEventTools.AddBoolPersistentListener(yesButton.onClick, popupRootGo.SetActive, false);

            // No -> 팝업만 닫는다 (상점은 계속 열려 있음).
            UnityEventTools.AddBoolPersistentListener(noButton.onClick, popupRootGo.SetActive, false);

            return popupRootGo;
        }

        #endregion

        #region 상점 화면 (전체 통합 - 헤더 + 아이템목록 + 인벤토리)

        private GameObject CreateShopPanel(Transform canvasTransform, out Button closeButtonOut)
        {
            GameObject shopGo = NewUI("ShopPanel", canvasTransform);
            RectTransform shopRect = shopGo.GetComponent<RectTransform>();
            // 이제 화면 전체를 하나의 상점 팝업으로 쓴다 (진입 패널과 안 나눔).
            Stretch(shopRect, 0f);

            GameObject dimGo = NewPanel("DimBackground", shopGo.transform, Vector2.zero);
            Stretch(dimGo.GetComponent<RectTransform>(), 0f);
            dimGo.GetComponent<Image>().color = ColorBackgroundDark;

            // 상단 헤더 — 상점주인 + 간판(SHOP) + 보유 골드 + 상점 닫기 버튼
            GameObject headerGo = NewPanel("ShopHeader", shopGo.transform, Vector2.zero, panelSprite);
            RectTransform headerRect = headerGo.GetComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0f, 1f);
            headerRect.anchorMax = new Vector2(1f, 1f);
            headerRect.pivot = new Vector2(0.5f, 1f);
            headerRect.sizeDelta = new Vector2(0f, 130f);
            headerRect.anchoredPosition = Vector2.zero;
            headerGo.GetComponent<Image>().color = ColorPanelFrame;

            // 상점주인 이미지 자리 (외부 스프라이트 대신 색상 블록 placeholder)
            GameObject shopkeeperGo = NewPanel("ShopkeeperImage", headerGo.transform, new Vector2(100f, 100f));
            RectTransform shopkeeperRect = shopkeeperGo.GetComponent<RectTransform>();
            shopkeeperRect.anchorMin = new Vector2(0f, 0.5f);
            shopkeeperRect.anchorMax = new Vector2(0f, 0.5f);
            shopkeeperRect.pivot = new Vector2(0f, 0.5f);
            shopkeeperRect.anchoredPosition = new Vector2(30f, 0f);
            shopkeeperGo.GetComponent<Image>().color = ColorNeutralButton;

            TMP_Text shopkeeperLabel = NewText("ShopkeeperLabel", shopkeeperGo.transform, "상점주인", 18f, TextAlignmentOptions.Center);
            Stretch(shopkeeperLabel.rectTransform, 0f);

            // 간판(타이틀 사인) - 헤더 중앙
            GameObject bannerGo = NewPanel("TitleBanner", headerGo.transform, new Vector2(260f, 60f), panelSprite);
            RectTransform bannerRect = bannerGo.GetComponent<RectTransform>();
            bannerRect.anchorMin = new Vector2(0.5f, 0.5f);
            bannerRect.anchorMax = new Vector2(0.5f, 0.5f);
            bannerRect.pivot = new Vector2(0.5f, 0.5f);
            bannerRect.anchoredPosition = Vector2.zero;
            bannerGo.GetComponent<Image>().color = ColorCardBody;

            TMP_Text titleText = NewText("TitleText", bannerGo.transform, "SHOP", 32f, TextAlignmentOptions.Center);
            Stretch(titleText.rectTransform, 0f);

            // 보유 골드 표시 - 헤더 오른쪽
            GameObject goldPanelGo = NewPanel("CurrentGoldPanel", headerGo.transform, new Vector2(220f, 70f), panelSprite);
            RectTransform goldPanelRect = goldPanelGo.GetComponent<RectTransform>();
            goldPanelRect.anchorMin = new Vector2(1f, 0.5f);
            goldPanelRect.anchorMax = new Vector2(1f, 0.5f);
            goldPanelRect.pivot = new Vector2(1f, 0.5f);
            goldPanelRect.anchoredPosition = new Vector2(-190f, 0f);
            goldPanelGo.GetComponent<Image>().color = ColorPanelFrame;

            TMP_Text goldText = NewText("CurrentGoldText", goldPanelGo.transform, "Current Gold : 0", 24f, TextAlignmentOptions.Center);
            goldText.color = ColorGold;
            Stretch(goldText.rectTransform, 8f);

            // 상점 닫기 버튼 - 헤더 맨 오른쪽
            Button closeButton = NewButton("CloseShopButton", headerGo.transform, "상점 닫기", out _, buttonSprite);
            RectTransform closeRect = closeButton.GetComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(1f, 0.5f);
            closeRect.anchorMax = new Vector2(1f, 0.5f);
            closeRect.pivot = new Vector2(1f, 0.5f);
            closeRect.sizeDelta = new Vector2(150f, 70f);
            closeRect.anchoredPosition = new Vector2(-30f, 0f);
            closeButton.image.color = new Color(0.4f, 0.42f, 0.45f, 1f);
            closeButtonOut = closeButton;

            // 아이템 목록 영역 (Horizontal Layout Group) - 헤더 바로 아래
            GameObject listGo = NewUI("ItemListArea", shopGo.transform);
            RectTransform listRect = listGo.GetComponent<RectTransform>();
            listRect.anchorMin = new Vector2(0.5f, 1f);
            listRect.anchorMax = new Vector2(0.5f, 1f);
            listRect.pivot = new Vector2(0.5f, 1f);
            listRect.sizeDelta = new Vector2(1600f, 480f);
            listRect.anchoredPosition = new Vector2(0f, -150f); // 헤더(130) + 여백(20)

            HorizontalLayoutGroup layout = listGo.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.spacing = 30f;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            for (int i = 0; i < shopSlotCount; i++)
            {
                CreateShopItemCard(listGo.transform, i);
            }

            // 인벤토리 영역 - 아이템 목록 아래, 같은 ShopPanel 안에 붙어서 항상 같이 열리고 닫힌다.
            CreateInventorySection(shopGo.transform);

            return shopGo;
        }

        #endregion

        #region 상점 아이템 카드

        private void CreateShopItemCard(Transform parent, int index)
        {
            // 상점 패널 아래쪽에 남는 공간이 많아서 카드를 크게 키웠다.
            Vector2 cardSize = new Vector2(200f, 400f);

            GameObject cardGo = NewPanel($"ShopItemCard_{index}", parent, cardSize, panelSprite);
            RectTransform cardRect = cardGo.GetComponent<RectTransform>();
            cardRect.pivot = new Vector2(0.5f, 0f);
            cardGo.GetComponent<Image>().color = ColorCardBody;

            // 테두리 느낌을 위한 액자색 아웃라인 (같은 자리에 얇은 배경으로 대체)
            Outline outline = cardGo.AddComponent<Outline>();
            outline.effectColor = ColorPanelFrame;
            outline.effectDistance = new Vector2(3f, -3f);

            // 아이콘 자리 (색상 블록, 등급 색상 교체 지점)
            GameObject iconGo = NewPanel("IconImage", cardGo.transform, new Vector2(170f, 170f));
            RectTransform iconRect = iconGo.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.5f, 1f);
            iconRect.anchorMax = new Vector2(0.5f, 1f);
            iconRect.pivot = new Vector2(0.5f, 1f);
            iconRect.anchoredPosition = new Vector2(0f, -15f);
            iconGo.GetComponent<Image>().color = new Color(0.6f, 0.6f, 0.65f, 1f); // Common 등급 임시색

            // 이름
            TMP_Text nameText = NewText("NameText", cardGo.transform, "Sword", 24f, TextAlignmentOptions.Center);
            RectTransform nameRect = nameText.rectTransform;
            nameRect.anchorMin = new Vector2(0.5f, 1f);
            nameRect.anchorMax = new Vector2(0.5f, 1f);
            nameRect.pivot = new Vector2(0.5f, 1f);
            nameRect.sizeDelta = new Vector2(190f, 40f);
            nameRect.anchoredPosition = new Vector2(0f, -195f);

            // 가격 표시 줄 (코인 아이콘 + 가격 텍스트)
            GameObject priceRowGo = NewUI("PriceRow", cardGo.transform);
            RectTransform priceRowRect = priceRowGo.GetComponent<RectTransform>();
            priceRowRect.anchorMin = new Vector2(0.5f, 1f);
            priceRowRect.anchorMax = new Vector2(0.5f, 1f);
            priceRowRect.pivot = new Vector2(0.5f, 1f);
            priceRowRect.sizeDelta = new Vector2(190f, 40f);
            priceRowRect.anchoredPosition = new Vector2(0f, -245f);

            HorizontalLayoutGroup priceLayout = priceRowGo.AddComponent<HorizontalLayoutGroup>();
            priceLayout.childAlignment = TextAnchor.MiddleCenter;
            priceLayout.spacing = 8f;
            priceLayout.childControlWidth = false;
            priceLayout.childControlHeight = false;

            GameObject coinGo = NewPanel("CoinIcon", priceRowGo.transform, new Vector2(26f, 26f));
            coinGo.GetComponent<Image>().color = ColorGold;

            TMP_Text priceText = NewText("PriceText", priceRowGo.transform, "0", 24f, TextAlignmentOptions.Left);
            priceText.rectTransform.sizeDelta = new Vector2(110f, 34f);

            // 구매 버튼
            Button buyButton = NewButton("BuyButton", cardGo.transform, "BUY", out _, buttonSprite);
            RectTransform buyRect = buyButton.GetComponent<RectTransform>();
            buyRect.anchorMin = new Vector2(0.5f, 0f);
            buyRect.anchorMax = new Vector2(0.5f, 0f);
            buyRect.pivot = new Vector2(0.5f, 0f);
            buyRect.sizeDelta = new Vector2(170f, 48f);
            buyRect.anchoredPosition = new Vector2(0f, 15f);
            buyButton.image.color = ColorBuyButton;

            // 호버 시 표시되는 툴팁 (효과 / 쿨타임 / 내구도)
            GameObject tooltipGo = CreateTooltip(cardGo.transform, cardSize);

            // 마우스 호버로 툴팁 열고 닫기 (화면 표시 전용, 게임 로직 아님)
            EventTrigger trigger = cardGo.AddComponent<EventTrigger>();

            EventTrigger.Entry enterEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            enterEntry.callback.AddListener(_ => tooltipGo.SetActive(true));
            trigger.triggers.Add(enterEntry);

            EventTrigger.Entry exitEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            exitEntry.callback.AddListener(_ => tooltipGo.SetActive(false));
            trigger.triggers.Add(exitEntry);
        }

        private GameObject CreateTooltip(Transform cardParent, Vector2 cardSize)
        {
            GameObject tooltipGo = NewPanel("TooltipPanel", cardParent, new Vector2(cardSize.x, 200f));
            RectTransform tooltipRect = tooltipGo.GetComponent<RectTransform>();
            tooltipRect.anchorMin = new Vector2(0.5f, 1f);
            tooltipRect.anchorMax = new Vector2(0.5f, 1f);
            tooltipRect.pivot = new Vector2(0.5f, 0f);
            tooltipRect.anchoredPosition = new Vector2(0f, 10f); // 카드 위쪽에 붙는다
            tooltipGo.GetComponent<Image>().color = ColorTooltipBody;

            VerticalLayoutGroup layout = tooltipGo.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = 8f;
            layout.padding = new RectOffset(10, 10, 10, 10);
            layout.childControlWidth = true;
            layout.childControlHeight = false;

            NewText("EffectText", tooltipGo.transform, "효과 : 직선 공격 · 피해 0", 22f, TextAlignmentOptions.Center);
            NewText("CooldownText", tooltipGo.transform, "쿨타임 : 0.0초", 22f, TextAlignmentOptions.Center);
            NewText("DurabilityText", tooltipGo.transform, "내구도 : 0", 22f, TextAlignmentOptions.Center);

            tooltipGo.SetActive(false);

            return tooltipGo;
        }

        #endregion

        #region 인벤토리 영역 (상점 화면 안에 같이 포함)

        private void CreateInventorySection(Transform shopPanelTransform)
        {
            GameObject invGo = NewUI("InventoryPanel", shopPanelTransform);
            RectTransform invRect = invGo.GetComponent<RectTransform>();
            // 화면 하단에 고정 - ShopPanel과 항상 같이 열리고 닫힌다 (별도로 안 나눔).
            invRect.anchorMin = new Vector2(0f, 0f);
            invRect.anchorMax = new Vector2(1f, 0f);
            invRect.pivot = new Vector2(0.5f, 0f);
            invRect.offsetMin = new Vector2(40f, 20f);
            invRect.offsetMax = new Vector2(-40f, 20f + 330f);

            Image bgImage = invGo.AddComponent<Image>();
            if (panelSprite != null)
            {
                bgImage.sprite = panelSprite;
                bgImage.type = Image.Type.Sliced;
                bgImage.color = Color.white;
            }
            else
            {
                bgImage.color = ColorBackgroundDark;
            }

            TMP_Text label = NewText("InventoryLabel", invGo.transform, "인벤토리", 26f, TextAlignmentOptions.Left);
            RectTransform labelRect = label.rectTransform;
            labelRect.anchorMin = new Vector2(0f, 1f);
            labelRect.anchorMax = new Vector2(0f, 1f);
            labelRect.pivot = new Vector2(0f, 1f);
            labelRect.sizeDelta = new Vector2(220f, 40f);
            labelRect.anchoredPosition = new Vector2(20f, -10f);

            GameObject gridGo = NewUI("InventorySlotGrid", invGo.transform);
            RectTransform gridRect = gridGo.GetComponent<RectTransform>();
            gridRect.anchorMin = new Vector2(0f, 0f);
            gridRect.anchorMax = new Vector2(1f, 1f);
            gridRect.offsetMin = new Vector2(20f, 20f);
            gridRect.offsetMax = new Vector2(-20f, -55f); // 위쪽 라벨 자리만큼 남겨둔다

            HorizontalLayoutGroup gridLayout = gridGo.AddComponent<HorizontalLayoutGroup>();
            gridLayout.spacing = 15f;
            gridLayout.childControlWidth = true;
            gridLayout.childControlHeight = true;
            gridLayout.childForceExpandWidth = true;
            gridLayout.childForceExpandHeight = true;

            for (int i = 0; i < InventoryManager.MaxSlotCount; i++)
            {
                CreateInventorySlot(gridGo.transform, i);
            }
        }

        private void CreateInventorySlot(Transform parent, int index)
        {
            GameObject slotGo = NewUI($"InventorySlot_{index}", parent);

            Image slotImage = slotGo.AddComponent<Image>();
            if (panelSprite != null)
            {
                slotImage.sprite = panelSprite;
                slotImage.type = Image.Type.Sliced;
                slotImage.color = Color.white;
            }
            else
            {
                slotImage.color = ColorCardBody;
            }

            Button slotButton = slotGo.AddComponent<Button>();
            slotButton.targetGraphic = slotImage;

            // 아이콘 자리 - 평소엔 투명(빈 칸), 실제 장비가 들어오면 코드에서 sprite/color를 채운다.
            GameObject iconGo = NewUI("IconImage", slotGo.transform);
            Image iconImage = iconGo.AddComponent<Image>();
            iconImage.color = new Color(1f, 1f, 1f, 0f);
            Stretch(iconGo.GetComponent<RectTransform>(), 14f);
        }

        #endregion
    }
}
#endif