using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace BuleMarble
{
#if UNITY_EDITOR
    /// <summary>
    /// 보물상자 보상 팝업(TreasureUI) 화면을 코드로 조립하는 에디터 전용 빌더.
    /// 빈 GameObject에 붙인 뒤, 컴포넌트 우클릭 메뉴에서 "1. Build UI" / "2. Clear UI"로 실행한다.
    /// 화면 구조만 생성하며, 게임 로직 스크립트나 실제 에셋 참조는 연결하지 않는다.
    /// </summary>
    public class TreasureUIBuilder : MonoBehaviour
    {
        private const string RootName = "TreasureUI";
        private static readonly Vector2 ReferenceResolution = new Vector2(1920f, 1080f);

        [SerializeField] private TMP_FontAsset font;

        #region 진입점 (Build / Clear)

        [ContextMenu("1. Build UI")]
        private void BuildUI()
        {
            if (GameObject.Find(RootName) != null)
            {
                Debug.LogWarning($"[TreasureUIBuilder] 이미 '{RootName}' 오브젝트가 씬에 존재합니다. " +
                                  "중복 생성을 막기 위해 빌드를 중단합니다. 기존 것을 지우고 다시 시도하세요.");
                return;
            }

            if (FindFirstObjectByType<EventSystem>() == null)
            {
                Debug.LogWarning("[TreasureUIBuilder] 씬에 EventSystem이 없습니다. " +
                                  "버튼 클릭이 동작하려면 EventSystem을 추가해야 합니다.");
            }

            GameObject root = CreateCanvasRoot();
            Undo.RegisterCreatedObjectUndo(root, "Create TreasureUI");

            Transform canvasTransform = root.transform;

            CreateDimBackground(canvasTransform);
            Transform panel = CreatePopupPanel(canvasTransform);
            CreateChestArea(panel);
            CreateCloseButton(panel);
            CreateConfirmButton(panel);

            EditorSceneManager.MarkSceneDirty(gameObject.scene);
            Debug.Log($"[TreasureUIBuilder] '{RootName}' 생성 완료.");
        }

        [ContextMenu("2. Clear UI")]
        private void ClearUI()
        {
            GameObject root = GameObject.Find(RootName);

            if (root == null)
            {
                Debug.Log($"[TreasureUIBuilder] 지울 '{RootName}' 오브젝트가 씬에 없습니다.");
                return;
            }

            Undo.DestroyObjectImmediate(root);
            Debug.Log($"[TreasureUIBuilder] '{RootName}' 오브젝트를 지웠습니다.");
        }

        #endregion

        #region Canvas 루트 생성

        private GameObject CreateCanvasRoot()
        {
            var root = new GameObject(RootName, typeof(RectTransform));

            Canvas canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.matchWidthOrHeight = 0.5f;

            root.AddComponent<GraphicRaycaster>();

            return root;
        }

        #endregion

        #region 배경 / 팝업 패널

        /// <summary>
        /// 화면 전체를 덮는 반투명 어둡게 처리용 배경.
        /// </summary>
        private void CreateDimBackground(Transform canvasTransform)
        {
            GameObject dim = NewUI("DimBackground", canvasTransform);
            Image image = dim.AddComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0.6f);

            Stretch((RectTransform)dim.transform, 0f);
        }

        /// <summary>
        /// 상자 + 버튼들이 배치되는 오렌지톤 팝업 본체. 화면 중앙에 고정 크기로 배치.
        /// </summary>
        private Transform CreatePopupPanel(Transform canvasTransform)
        {
            GameObject panel = NewPanel("PopupPanel", canvasTransform, new Vector2(900f, 700f));

            Image image = panel.GetComponent<Image>();
            image.color = new Color(1f, 0.7f, 0.2f, 1f); // 오렌지 그라데이션 자리 (임시 단색)

            RectTransform rect = (RectTransform)panel.transform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;

            return panel.transform;
        }

        #endregion

        #region 상자 영역

        /// <summary>
        /// 열림 애니메이션이 재생될 상자 이미지 자리. 패널 중앙보다 살짝 위쪽에 배치.
        /// </summary>
        private void CreateChestArea(Transform panel)
        {
            GameObject chest = NewUI("ChestImage", panel);
            Image image = chest.AddComponent<Image>();
            image.color = new Color(0.6f, 0.15f, 0.15f, 1f); // 상자 자리 표시용 블록

            RectTransform rect = (RectTransform)chest.transform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(500f, 400f);
            rect.anchoredPosition = new Vector2(0f, 60f);
        }

        #endregion

        #region 닫기 / 확인 버튼

        /// <summary>
        /// 패널 우측 상단의 X 닫기 버튼.
        /// </summary>
        private void CreateCloseButton(Transform panel)
        {
            Button button = NewButton("CloseButton", panel, "X", out TMP_Text label);
            label.fontSize = 36f;

            RectTransform rect = (RectTransform)button.transform;
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.sizeDelta = new Vector2(70f, 70f);
            rect.anchoredPosition = new Vector2(-20f, -20f);

            button.GetComponent<Image>().color = new Color(0.95f, 0.95f, 0.95f, 1f);
        }

        /// <summary>
        /// 패널 하단 중앙의 확인 버튼.
        /// </summary>
        private void CreateConfirmButton(Transform panel)
        {
            Button button = NewButton("ConfirmButton", panel, "확인", out TMP_Text label);
            label.fontSize = 32f;

            RectTransform rect = (RectTransform)button.transform;
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.sizeDelta = new Vector2(300f, 80f);
            rect.anchoredPosition = new Vector2(0f, 50f);

            button.GetComponent<Image>().color = new Color(0.85f, 0.6f, 0.1f, 1f);
        }

        #endregion

        #region 공통 도우미

        private GameObject NewUI(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private GameObject NewPanel(string name, Transform parent, Vector2 size)
        {
            GameObject go = NewUI(name, parent);
            go.AddComponent<Image>();

            RectTransform rect = (RectTransform)go.transform;
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
            go.AddComponent<Image>();
            Button button = go.AddComponent<Button>();

            labelText = NewText($"{name}_Label", go.transform, label, 28f, TextAlignmentOptions.Center);
            Stretch((RectTransform)labelText.transform, 0f);

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
    }
#endif
}