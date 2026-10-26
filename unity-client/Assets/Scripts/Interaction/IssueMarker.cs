using System.Collections.Generic;
using Inspection.Network;
using UnityEngine;

namespace Inspection.Interaction
{
    /// <summary>
    /// 问题标记：立杆 + 球形标头，沿命中表面的法线方向竖起。待提交时为白色，保存后按优先级着色，已解决变灰。
    /// </summary>
    public class IssueMarker : MonoBehaviour
    {
        private const float PinLength = 0.35f;
        private const float PinThickness = 0.03f;
        private const float HeadDiameter = 0.16f;

        private static readonly Color PendingColor = Color.white;
        private static readonly Color PinColor = new Color32(60, 70, 80, 255);
        private static readonly Color HighColor = new Color32(200, 50, 31, 255);
        private static readonly Color MediumColor = new Color32(220, 141, 0, 255);
        private static readonly Color LowColor = new Color32(63, 110, 140, 255);
        private static readonly Color ResolvedColor = new Color32(150, 160, 168, 255);

        private readonly List<Material> createdMaterials = new List<Material>();
        private Renderer headRenderer;
        private Collider headCollider;

        /// <summary>标记对应的问题，待提交时为 null</summary>
        public IssueDto Issue { get; private set; }

        public bool IsPending => Issue == null;

        /// <summary>在命中点创建标记，立杆方向与表面法线一致</summary>
        public static IssueMarker Create(Vector3 position, Vector3 surfaceNormal, Transform parent)
        {
            var markerObject = new GameObject("IssueMarker (待提交)");
            markerObject.transform.SetParent(parent, false);
            markerObject.transform.SetPositionAndRotation(position, Quaternion.FromToRotation(Vector3.up, surfaceNormal));

            var marker = markerObject.AddComponent<IssueMarker>();
            marker.BuildVisual();
            marker.SetPending();
            return marker;
        }

        /// <summary>标记为待提交：白色标头，暂不响应点击</summary>
        public void SetPending()
        {
            Issue = null;
            headCollider.enabled = false;
            headRenderer.material.color = PendingColor;
        }

        /// <summary>绑定已保存的问题，按优先级和状态更新颜色</summary>
        public void SetIssue(IssueDto issue)
        {
            Issue = issue;
            headCollider.enabled = true;
            headRenderer.material.color = ColorOf(issue);
            gameObject.name = $"IssueMarker #{issue.id}";
        }

        /// <summary>用基础几何体拼出立杆和标头</summary>
        private void BuildVisual()
        {
            GameObject pin = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pin.name = "Pin";
            pin.transform.SetParent(transform, false);
            pin.transform.localScale = new Vector3(PinThickness, PinLength / 2f, PinThickness);
            pin.transform.localPosition = new Vector3(0f, PinLength / 2f, 0f);
            Destroy(pin.GetComponent<Collider>());
            Renderer pinRenderer = pin.GetComponent<Renderer>();
            pinRenderer.material.color = PinColor;
            createdMaterials.Add(pinRenderer.material);

            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head";
            head.transform.SetParent(transform, false);
            head.transform.localScale = Vector3.one * HeadDiameter;
            head.transform.localPosition = new Vector3(0f, PinLength, 0f);
            headRenderer = head.GetComponent<Renderer>();
            headCollider = head.GetComponent<Collider>();
            createdMaterials.Add(headRenderer.material);
        }

        private static Color ColorOf(IssueDto issue)
        {
            if (issue.status == IssueStatus.Resolved)
            {
                return ResolvedColor;
            }
            return issue.priority switch
            {
                IssuePriority.High => HighColor,
                IssuePriority.Medium => MediumColor,
                _ => LowColor,
            };
        }

        private void OnDestroy()
        {
            foreach (Material material in createdMaterials)
            {
                Destroy(material);
            }
        }
    }
}
