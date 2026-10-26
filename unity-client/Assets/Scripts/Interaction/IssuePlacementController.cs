using System;
using Inspection.Network;
using UnityEngine;

namespace Inspection.Interaction
{
    /// <summary>
    /// 射线点击放置问题标记：点击场景表面在命中点生成待提交标记，点击已保存的标记查看详情。
    /// 鼠标在界面上时不响应场景点击；同一时间最多只有一个待提交标记。
    /// </summary>
    public class IssuePlacementController : MonoBehaviour
    {
        private const float MaxRayDistance = 100f;
        private const string PendingExistsMessage = "请先提交或取消当前标记";

        [SerializeField] private Camera viewCamera;
        [SerializeField] private Transform markerParent;
        [SerializeField] private LayerMask placeableLayers = Physics.DefaultRaycastLayers; // 射线可命中的层

        private IssueMarker pendingMarker;

        /// <summary>新放置了待提交标记（标记、命中对象名称）</summary>
        public event Action<IssueMarker, string> PendingMarkerPlaced;

        /// <summary>点击了已保存的标记</summary>
        public event Action<IssueMarker> SavedMarkerClicked;

        /// <summary>点击被忽略，参数为给用户的提示</summary>
        public event Action<string> PlacementBlocked;

        /// <summary>鼠标当前指向的对象名称，未指向任何对象时为 null</summary>
        public string HoverTargetName { get; private set; }

        public bool HasPendingMarker => pendingMarker != null;

        /// <summary>设置射线相机和标记父节点，供场景生成工具调用</summary>
        public void Bind(Camera targetCamera, Transform targetMarkerParent)
        {
            viewCamera = targetCamera;
            markerParent = targetMarkerParent;
        }

        private void Update()
        {
            bool pointerOverUi = InspectionInput.IsPointerOverUi();
            HoverTargetName = !pointerOverUi && TryRaycast(out RaycastHit hoverHit) ? TargetNameOf(hoverHit.collider) : null;

            if (InspectionInput.PlaceClicked && !pointerOverUi)
            {
                HandleSceneClick();
            }
        }

        /// <summary>上报成功后把待提交标记转为已保存</summary>
        public IssueMarker ConfirmPendingMarker(IssueDto savedIssue)
        {
            IssueMarker confirmedMarker = pendingMarker;
            pendingMarker = null;
            confirmedMarker.SetIssue(savedIssue);
            return confirmedMarker;
        }

        /// <summary>取消填写时移除未保存的标记</summary>
        public void DiscardPendingMarker()
        {
            if (pendingMarker == null)
            {
                return;
            }
            Destroy(pendingMarker.gameObject);
            pendingMarker = null;
        }

        /// <summary>处理一次场景点击：命中已保存标记则查看详情，否则在命中点放置待提交标记</summary>
        private void HandleSceneClick()
        {
            if (!TryRaycast(out RaycastHit hit))
            {
                return;
            }
            IssueMarker clickedMarker = hit.collider.GetComponentInParent<IssueMarker>();
            if (clickedMarker != null && !clickedMarker.IsPending)
            {
                SavedMarkerClicked?.Invoke(clickedMarker);
                return;
            }
            if (pendingMarker != null)
            {
                PlacementBlocked?.Invoke(PendingExistsMessage);
                return;
            }
            pendingMarker = IssueMarker.Create(hit.point, hit.normal, markerParent);
            PendingMarkerPlaced?.Invoke(pendingMarker, TargetNameOf(hit.collider));
        }

        private bool TryRaycast(out RaycastHit hit)
        {
            Ray ray = viewCamera.ScreenPointToRay(InspectionInput.PointerPosition);
            return Physics.Raycast(ray, out hit, MaxRayDistance, placeableLayers, QueryTriggerInteraction.Ignore);
        }

        private static string TargetNameOf(Collider hitCollider)
        {
            IssueMarker marker = hitCollider.GetComponentInParent<IssueMarker>();
            if (marker != null && marker.Issue != null)
            {
                return $"问题标记 #{marker.Issue.id} {marker.Issue.title}";
            }
            InspectionTarget target = hitCollider.GetComponentInParent<InspectionTarget>();
            return target != null ? target.DisplayName : hitCollider.gameObject.name;
        }
    }
}
