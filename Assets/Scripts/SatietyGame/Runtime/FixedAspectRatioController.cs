using UnityEngine;
using UnityEngine.UI;

namespace SatietyGame
{
    [DefaultExecutionOrder(-1000)]
    public sealed class FixedAspectRatioController : MonoBehaviour
    {
        private const float DefaultAspectRatio = 9f / 16f;

        [SerializeField, Min(0.01f)] private float targetAspectRatio = DefaultAspectRatio;
        [SerializeField] private Color letterboxColor = Color.black;
        [SerializeField] private bool convertOverlayCanvases = true;
        [SerializeField] private bool limitDevicePixelRatio = true;

        private Camera targetCamera;
        private int lastScreenWidth;
        private int lastScreenHeight;
        private Rect lastViewport;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (FindAnyObjectByType<FixedAspectRatioController>() != null)
            {
                return;
            }

            GameObject controllerObject = new GameObject("Fixed Aspect Ratio Controller");
            DontDestroyOnLoad(controllerObject);
            controllerObject.AddComponent<FixedAspectRatioController>();
        }

        private void Awake()
        {
            targetAspectRatio = targetAspectRatio > 0f ? targetAspectRatio : DefaultAspectRatio;
            Application.targetFrameRate = 60;
        }

        private void LateUpdate()
        {
            ApplyLayoutIfNeeded();
        }

        private void ApplyLayoutIfNeeded()
        {
            if (targetCamera == null || !targetCamera.isActiveAndEnabled)
            {
                targetCamera = Camera.main;
                if (targetCamera == null)
                {
                    return;
                }

                ConfigureCamera();
            }

            if (lastScreenWidth == Screen.width && lastScreenHeight == Screen.height)
            {
                return;
            }

            lastScreenWidth = Screen.width;
            lastScreenHeight = Screen.height;
            lastViewport = CalculateViewport(Screen.width, Screen.height);
            targetCamera.rect = lastViewport;
        }

        private void ConfigureCamera()
        {
            targetCamera.clearFlags = CameraClearFlags.SolidColor;
            targetCamera.backgroundColor = letterboxColor;

            if (convertOverlayCanvases)
            {
                Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include);
                for (int i = 0; i < canvases.Length; i++)
                {
                    Canvas canvas = canvases[i];
                    if (canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                    {
                        continue;
                    }

                    canvas.renderMode = RenderMode.ScreenSpaceCamera;
                    canvas.worldCamera = targetCamera;
                    canvas.planeDistance = 100f;
                }
            }

            if (limitDevicePixelRatio)
            {
                QualitySettings.resolutionScalingFixedDPIFactor = 1f;
            }
        }

        private Rect CalculateViewport(int width, int height)
        {
            if (width <= 0 || height <= 0)
            {
                return new Rect(0f, 0f, 1f, 1f);
            }

            float screenAspect = (float)width / height;
            if (screenAspect > targetAspectRatio)
            {
                float viewportWidth = targetAspectRatio / screenAspect;
                return new Rect((1f - viewportWidth) * 0.5f, 0f, viewportWidth, 1f);
            }

            float viewportHeight = screenAspect / targetAspectRatio;
            return new Rect(0f, (1f - viewportHeight) * 0.5f, 1f, viewportHeight);
        }
    }
}
