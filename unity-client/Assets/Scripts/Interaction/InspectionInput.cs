using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Inspection.Interaction
{
    /// <summary>
    /// 键鼠输入读取：同时兼容新版 Input System 和旧版 Input Manager，使用哪一种由项目 Player 设置决定。
    /// </summary>
    public static class InspectionInput
    {
#if ENABLE_INPUT_SYSTEM
        // 新版输入系统的鼠标位移单位是像素，乘以该系数后与旧版 "Mouse X/Y" 的手感一致
        private const float MouseDeltaScale = 0.1f;

        public static Vector2 PointerPosition => Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;

        public static bool PlaceClicked => Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;

        public static bool LookHeld => Mouse.current != null && Mouse.current.rightButton.isPressed;

        public static Vector2 LookDelta => Mouse.current != null ? Mouse.current.delta.ReadValue() * MouseDeltaScale : Vector2.zero;

        public static bool FastHeld => Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed;

        public static bool RefreshPressed => Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame;

        public static bool CancelPressed => Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;

        /// <summary>移动输入：x 为左右（A/D），y 为升降（Q/E），z 为前后（W/S）</summary>
        public static Vector3 MoveAxis
        {
            get
            {
                Keyboard keyboard = Keyboard.current;
                if (keyboard == null)
                {
                    return Vector3.zero;
                }
                return new Vector3(
                    Axis(keyboard.dKey.isPressed, keyboard.aKey.isPressed),
                    Axis(keyboard.eKey.isPressed, keyboard.qKey.isPressed),
                    Axis(keyboard.wKey.isPressed, keyboard.sKey.isPressed));
            }
        }
#else
        public static Vector2 PointerPosition => Input.mousePosition;

        public static bool PlaceClicked => Input.GetMouseButtonDown(0);

        public static bool LookHeld => Input.GetMouseButton(1);

        public static Vector2 LookDelta => new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));

        public static bool FastHeld => Input.GetKey(KeyCode.LeftShift);

        public static bool RefreshPressed => Input.GetKeyDown(KeyCode.R);

        public static bool CancelPressed => Input.GetKeyDown(KeyCode.Escape);

        /// <summary>移动输入：x 为左右（A/D），y 为升降（Q/E），z 为前后（W/S）</summary>
        public static Vector3 MoveAxis => new Vector3(
            Axis(Input.GetKey(KeyCode.D), Input.GetKey(KeyCode.A)),
            Axis(Input.GetKey(KeyCode.E), Input.GetKey(KeyCode.Q)),
            Axis(Input.GetKey(KeyCode.W), Input.GetKey(KeyCode.S)));
#endif

        /// <summary>当前是否正在界面输入框中打字（此时不响应移动和快捷键）</summary>
        public static bool IsTypingInInputField()
        {
            GameObject selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            return selected != null && selected.TryGetComponent(out InputField inputField) && inputField.isFocused;
        }

        /// <summary>鼠标是否停在界面元素上（此时点击不应在场景中放置标记）</summary>
        public static bool IsPointerOverUi()
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }

        private static float Axis(bool positive, bool negative) => (positive ? 1f : 0f) - (negative ? 1f : 0f);
    }
}
