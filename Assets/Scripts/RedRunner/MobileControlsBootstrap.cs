using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityStandardAssets.CrossPlatformInput;

namespace RedRunner
{
    /// <summary>
    /// Adds a minimal touch control overlay without modifying the legacy binary scene.
    /// The overlay is only created in an Android player.
    /// </summary>
    public sealed class MobileControlsBootstrap : MonoBehaviour
    {
        private const string HorizontalAxis = "Horizontal";
        private const string JumpButton = "Jump";

        private bool leftPressed;
        private bool rightPressed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Create()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (FindFirstObjectByType<MobileControlsBootstrap>() != null)
            {
                return;
            }

            var bootstrap = new GameObject(nameof(MobileControlsBootstrap));
            DontDestroyOnLoad(bootstrap);
            bootstrap.AddComponent<MobileControlsBootstrap>();
#endif
        }

        private IEnumerator Start()
        {
            ConfigureLandscapeOrientation();
            CrossPlatformInputManager.SwitchActiveInputMethod(
                CrossPlatformInputManager.ActiveInputMethod.Touch);

            // Let the gameplay scene create its own EventSystem first.
            yield return null;

            EnsureEventSystem();
            CreateOverlay();
        }

        private static void ConfigureLandscapeOrientation()
        {
            Screen.autorotateToPortrait = false;
            Screen.autorotateToPortraitUpsideDown = false;
            Screen.autorotateToLandscapeLeft = true;
            Screen.autorotateToLandscapeRight = true;
            Screen.orientation = ScreenOrientation.AutoRotation;
        }

        private static void EnsureEventSystem()
        {
            if (EventSystem.current != null)
            {
                return;
            }

            var eventSystem = new GameObject(
                "Mobile EventSystem",
                typeof(EventSystem),
                typeof(StandaloneInputModule));
            DontDestroyOnLoad(eventSystem);
        }

        private void CreateOverlay()
        {
            var canvasObject = new GameObject(
                "Mobile Controls",
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);

            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10000;

            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 1f;

            CreateButton(
                canvasObject.transform,
                "Move Left",
                "◀",
                new Vector2(0f, 0f),
                new Vector2(190f, 170f),
                TouchControlButton.Control.Left);

            CreateButton(
                canvasObject.transform,
                "Move Right",
                "▶",
                new Vector2(0f, 0f),
                new Vector2(370f, 170f),
                TouchControlButton.Control.Right);

            CreateButton(
                canvasObject.transform,
                "Jump",
                "JUMP",
                new Vector2(1f, 0f),
                new Vector2(-190f, 170f),
                TouchControlButton.Control.Jump);
        }

        private void CreateButton(
            Transform parent,
            string objectName,
            string label,
            Vector2 anchor,
            Vector2 anchoredPosition,
            TouchControlButton.Control control)
        {
            var buttonObject = new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(Image),
                typeof(TouchControlButton));
            buttonObject.transform.SetParent(parent, false);

            var rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = new Vector2(260f, 260f);

            var image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.08f, 0.08f, 0.08f, 0.58f);

            var handler = buttonObject.GetComponent<TouchControlButton>();
            handler.Initialize(this, control);

            var labelObject = new GameObject(
                "Label",
                typeof(RectTransform),
                typeof(Text));
            labelObject.transform.SetParent(buttonObject.transform, false);

            var labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            var text = labelObject.GetComponent<Text>();
            text.text = label;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.fontSize = 48;
            text.fontStyle = FontStyle.Bold;
            text.raycastTarget = false;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        internal void SetDirection(TouchControlButton.Control control, bool pressed)
        {
            if (control == TouchControlButton.Control.Left)
            {
                leftPressed = pressed;
            }
            else if (control == TouchControlButton.Control.Right)
            {
                rightPressed = pressed;
            }

            var horizontal = (rightPressed ? 1f : 0f) - (leftPressed ? 1f : 0f);
            CrossPlatformInputManager.SetAxis(HorizontalAxis, horizontal);
        }

        internal static void SetJump(bool pressed)
        {
            if (pressed)
            {
                CrossPlatformInputManager.SetButtonDown(JumpButton);
            }
            else
            {
                CrossPlatformInputManager.SetButtonUp(JumpButton);
            }
        }
    }

    public sealed class TouchControlButton : MonoBehaviour,
        IPointerDownHandler,
        IPointerUpHandler,
        IPointerExitHandler
    {
        public enum Control
        {
            Left,
            Right,
            Jump
        }

        private MobileControlsBootstrap owner;
        private Control control;
        private bool pressed;

        internal void Initialize(MobileControlsBootstrap bootstrap, Control buttonControl)
        {
            owner = bootstrap;
            control = buttonControl;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            pressed = true;
            SetState(true);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            Release();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            Release();
        }

        private void OnDisable()
        {
            Release();
        }

        private void Release()
        {
            if (!pressed)
            {
                return;
            }

            pressed = false;
            SetState(false);
        }

        private void SetState(bool isPressed)
        {
            if (owner == null)
            {
                return;
            }

            if (control == Control.Jump)
            {
                MobileControlsBootstrap.SetJump(isPressed);
            }
            else
            {
                owner.SetDirection(control, isPressed);
            }
        }
    }
}
