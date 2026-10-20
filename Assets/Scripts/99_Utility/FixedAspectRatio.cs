using UnityEngine;
using UnityEngine.Rendering;

namespace Jay
{
    public sealed class FixedAspectRatio : MonoBehaviour
    {
        private const float TARGET_ASPECT = 16f / 9f;

        private Camera backgroundCamera;
        private Rect viewportRect;
        private int screenWidth;
        private int screenHeight;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            new GameObject(nameof(FixedAspectRatio)).AddComponent<FixedAspectRatio>();
        }

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);

            backgroundCamera = gameObject.AddComponent<Camera>();
            backgroundCamera.clearFlags = CameraClearFlags.SolidColor;
            backgroundCamera.backgroundColor = Color.black;
            backgroundCamera.cullingMask = 0;
            backgroundCamera.depth = float.MinValue;
            backgroundCamera.allowHDR = false;
            backgroundCamera.allowMSAA = false;

            UpdateViewportRect();
            RenderPipelineManager.beginCameraRendering += ApplyViewportRect;
        }

        private void Update()
        {
            if (screenWidth == Screen.width && screenHeight == Screen.height) return;

            UpdateViewportRect();
        }

        private void OnDestroy()
        {
            RenderPipelineManager.beginCameraRendering -= ApplyViewportRect;
        }

        private void UpdateViewportRect()
        {
            screenWidth = Screen.width;
            screenHeight = Screen.height;

            float currentAspect = (float)screenWidth / screenHeight;

            if (currentAspect < TARGET_ASPECT)
            {
                float height = currentAspect / TARGET_ASPECT;
                viewportRect = new Rect(0f, (1f - height) * 0.5f, 1f, height);
            }
            else
            {
                float width = TARGET_ASPECT / currentAspect;
                viewportRect = new Rect((1f - width) * 0.5f, 0f, width, 1f);
            }
        }

        private void ApplyViewportRect(ScriptableRenderContext _, Camera targetCamera)
        {
            if (targetCamera == backgroundCamera || targetCamera.targetTexture != null) return;

            targetCamera.rect = viewportRect;
        }
    }
}
