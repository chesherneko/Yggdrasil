# 역할
당신은 Unity uGUI 화면을 **코드만으로** 조립하는 에디터 툴을 작성하는 개발자입니다.

# 목표
첨부한 스크린샷(UI 시안)을 분석해, 그 화면을 만들어 내는 Unity 에디터 스크립트 **한 개**를 작성하세요.

# 프로젝트 설정 (여기를 채운 뒤 요청)
- 클래스명 :  StoreUIBuilder
  - 네임스페이스 :  BlueMarble
  - 루트 오브젝트 이름 :  Store UI
  - 기준 해상도 :  1920 x 1080
  - 주석 언어 : 한국어 

# 반드시 지킬 구조

## 1) 진입점
- `MonoBehaviour` 상속. 빈 GameObject에 붙여 **컴포넌트 우클릭 메뉴**로 실행한다.
  - 구현 전체를 `#if UNITY_EDITOR` ~ `#endif` 로 감싼다. (빌드에 에디터 코드가 들어가지 않도록)
  - 메뉴는 두 개:
      - `[ContextMenu("1. Build UI")]`
      - `[ContextMenu("2. Clear UI")]`
  - Build 시작 시 루트 오브젝트가 **이미 있으면 겹쳐 만들지 말고 경고 후 중단**한다.
  - 생성한 루트마다 `Undo.RegisterCreatedObjectUndo`, 작업 끝에 `EditorSceneManager.MarkSceneDirty(gameObject.scene)`.
  - Clear는 **우리가 만든 루트 이름의 오브젝트만** `Undo.DestroyObjectImmediate` 로 지우고, 지운 것을 로그로 남긴다.

## 2) Canvas
- `RenderMode.ScreenSpaceOverlay`
  - `CanvasScaler` : `ScaleWithScreenSize`, referenceResolution = 기준 해상도, matchWidthOrHeight = 0.5
  - `GraphicRaycaster` 부착
  - 씬에 `EventSystem` 이 없으면 경고 로그를 남긴다.

## 3) 도우미 메서드 (이 이름과 형태를 그대로 사용)
```
GameObject NewUI(string name, Transform parent)
GameObject NewPanel(string name, Transform parent, Vector2 size)
TMP_Text   NewText(string name, Transform parent, string text, float size, TextAlignmentOptions align)
Button     NewButton(string name, Transform parent, string label, out TMP_Text labelText)
void       Stretch(RectTransform rect, float padding)
```
- 텍스트는 **TextMeshProUGUI** 를 쓴다.
  - `[SerializeField] private TMP_FontAsset font;` 필드를 두고 **생성하는 모든 텍스트에 적용**한다. (한글 폰트 교체용)
  - 영역이 여러 개면 `CreateXxx(Transform canvas)` 처럼 영역마다 메서드를 나누고 `#region` 으로 묶는다.

## 4) 배치 규칙
- 모든 요소에 `anchorMin` · `anchorMax` · `pivot` · `anchoredPosition` · `sizeDelta` 를 **명시적으로** 설정한다.
  - 스크린샷에서 그 요소가 붙어 있는 화면 가장자리를 앵커로 잡는다.
      - 상단 가로 띠 → anchor (0,1)~(1,1), pivot (0.5,1)
      - 하단 중앙 패널 → anchor (0.5,0)~(0.5,0), pivot (0.5,0)
      - 우측 하단 버튼 → anchor (1,0)~(1,0), pivot (1,0)
      - 화면 꽉 채움 → `Stretch()`
  - 같은 모양이 반복되면(목록·슬롯·인벤토리 칸) `HorizontalLayoutGroup` / `VerticalLayoutGroup` / `GridLayoutGroup` 을 쓴다.
  - 처음에 숨겨져 있어야 할 패널은 마지막에 `SetActive(false)` 한다.

## 5) 금지 사항
- 외부 이미지·폰트·머티리얼 에셋을 참조하지 않는다. 색은 코드에서 `Color` 로 지정한다.
  - `Resources.Load` 를 쓰지 않는다.
  - **게임 로직 스크립트를 부착하거나 참조를 연결하지 않는다. 화면만 만든다.**
  - 스프라이트가 필요한 자리는 `Image` 색상 블록으로 대신하고, 이름으로 용도를 알 수 있게 한다.

# 스크린샷을 읽는 순서
1. 화면을 영역으로 나눈다 (상단 바 / 중앙 / 하단 패널 / 팝업 …)
   2. 각 영역의 요소를 나열하고 이름을 정한다 (PascalCase, 역할이 드러나게)
   3. 각 요소가 어느 가장자리에 고정되어야 하는지 판단한다
   4. 크기·간격을 기준 해상도 기준 픽셀로 추정한다

# 출력 형식
1. **하이어라키 트리** — 만들 오브젝트 구조를 먼저 보여준다
   2. **C# 코드 전체** — 한 파일, 그대로 붙여넣어 쓸 수 있게
   3. **조정 포인트** — 스크린샷만으로 확신할 수 없었던 부분과, 손으로 다듬으면 좋을 값****