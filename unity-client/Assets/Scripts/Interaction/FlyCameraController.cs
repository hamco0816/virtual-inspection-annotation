using UnityEngine;

namespace Inspection.Interaction
{
    /// <summary>
    /// 巡检视角控制：WASD 移动，Q/E 升降，按住右键转动视角，Shift 加速；活动范围限制在场地内。
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class FlyCameraController : MonoBehaviour
    {
        private const float MaxPitch = 85f;
        private const float HalfTurn = 180f;
        private const float FullTurn = 360f;

        [SerializeField] private float moveSpeed = 4f;          // 普通移动速度（米/秒）
        [SerializeField] private float fastMultiplier = 2.5f;   // 按住 Shift 时的速度倍数
        [SerializeField] private float lookSensitivity = 2f;    // 视角转动灵敏度
        [SerializeField] private Vector3 boundsMin = new Vector3(-9.5f, 0.4f, -7.5f); // 相机可到达的最小坐标
        [SerializeField] private Vector3 boundsMax = new Vector3(9.5f, 5f, 7.5f);     // 相机可到达的最大坐标

        private float yaw;
        private float pitch;

        private void Start()
        {
            Vector3 eulerAngles = transform.eulerAngles;
            yaw = eulerAngles.y;
            pitch = eulerAngles.x > HalfTurn ? eulerAngles.x - FullTurn : eulerAngles.x;
        }

        private void Update()
        {
            UpdateLook();
            if (!InspectionInput.IsTypingInInputField())
            {
                UpdateMove();
            }
        }

        /// <summary>按住右键时锁定鼠标并根据位移转动视角</summary>
        private void UpdateLook()
        {
            if (!InspectionInput.LookHeld)
            {
                Cursor.lockState = CursorLockMode.None;
                return;
            }
            Cursor.lockState = CursorLockMode.Locked;
            Vector2 lookDelta = InspectionInput.LookDelta;
            yaw += lookDelta.x * lookSensitivity;
            pitch = Mathf.Clamp(pitch - lookDelta.y * lookSensitivity, -MaxPitch, MaxPitch);
            transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
        }

        /// <summary>沿水平朝向前后左右移动，Q/E 垂直升降，并限制在场地范围内</summary>
        private void UpdateMove()
        {
            Vector3 moveAxis = InspectionInput.MoveAxis;
            if (moveAxis == Vector3.zero)
            {
                return;
            }
            float speed = moveSpeed * (InspectionInput.FastHeld ? fastMultiplier : 1f);
            Vector3 forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
            Vector3 right = Vector3.ProjectOnPlane(transform.right, Vector3.up).normalized;
            Vector3 offset = (forward * moveAxis.z + right * moveAxis.x + Vector3.up * moveAxis.y) * (speed * Time.deltaTime);

            Vector3 nextPosition = transform.position + offset;
            transform.position = new Vector3(
                Mathf.Clamp(nextPosition.x, boundsMin.x, boundsMax.x),
                Mathf.Clamp(nextPosition.y, boundsMin.y, boundsMax.y),
                Mathf.Clamp(nextPosition.z, boundsMin.z, boundsMax.z));
        }
    }
}
