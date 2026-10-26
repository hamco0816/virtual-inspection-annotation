using System.Collections;
using System.Collections.Generic;
using Inspection.Interaction;
using Inspection.Network;
using Inspection.Views;
using UnityEngine;

namespace Inspection
{
    /// <summary>
    /// 巡检客户端入口：读取配置，串联标记放置、上报表单、详情面板和后端接口，启动时恢复已有标记并定时检查后端连接。
    /// </summary>
    public class InspectionClientApp : MonoBehaviour
    {
        private const float HealthCheckIntervalSeconds = 10f;

        [SerializeField] private IssuePlacementController placementController;
        [SerializeField] private IssueMarkerRegistry markerRegistry;
        [SerializeField] private IssueFormPanel formPanel;
        [SerializeField] private IssueDetailPanel detailPanel;
        [SerializeField] private HudView hud;

        private InspectionApiClient apiClient;
        private bool loadingMarkers;

        /// <summary>绑定各功能组件，供场景生成工具调用</summary>
        public void Bind(IssuePlacementController placement, IssueMarkerRegistry registry, IssueFormPanel form, IssueDetailPanel detail, HudView hudView)
        {
            placementController = placement;
            markerRegistry = registry;
            formPanel = form;
            detailPanel = detail;
            hud = hudView;
        }

        private void Awake()
        {
            apiClient = new InspectionApiClient(ClientConfig.Load());
        }

        private void OnEnable()
        {
            placementController.PendingMarkerPlaced += HandlePendingMarkerPlaced;
            placementController.SavedMarkerClicked += HandleSavedMarkerClicked;
            placementController.PlacementBlocked += HandlePlacementBlocked;
            formPanel.SubmitRequested += HandleSubmitRequested;
            formPanel.CancelRequested += CancelPendingIssue;
        }

        private void OnDisable()
        {
            placementController.PendingMarkerPlaced -= HandlePendingMarkerPlaced;
            placementController.SavedMarkerClicked -= HandleSavedMarkerClicked;
            placementController.PlacementBlocked -= HandlePlacementBlocked;
            formPanel.SubmitRequested -= HandleSubmitRequested;
            formPanel.CancelRequested -= CancelPendingIssue;
        }

        private void Start()
        {
            formPanel.Close();
            detailPanel.Hide();
            hud.SetBackendUrl(apiClient.BaseUrl);
            StartCoroutine(HealthCheckLoop());
            ReloadMarkers();
        }

        private void Update()
        {
            hud.SetHoverTarget(placementController.HoverTargetName);
            if (InspectionInput.CancelPressed && formPanel.IsOpen)
            {
                CancelPendingIssue();
            }
            if (InspectionInput.RefreshPressed && !InspectionInput.IsTypingInInputField())
            {
                ReloadMarkers();
            }
        }

        private void HandlePendingMarkerPlaced(IssueMarker marker, string targetName)
        {
            detailPanel.Hide();
            formPanel.Open(marker.transform.position, targetName);
        }

        private void HandleSavedMarkerClicked(IssueMarker marker)
        {
            if (formPanel.IsOpen)
            {
                hud.ShowToast("请先提交或取消当前标记", false);
                return;
            }
            detailPanel.Show(marker.Issue);
        }

        private void HandlePlacementBlocked(string message)
        {
            hud.ShowToast(message, false);
        }

        private void HandleSubmitRequested(CreateIssueRequest request)
        {
            if (formPanel.IsSubmitting)
            {
                return;
            }
            formPanel.SetSubmitting(true);
            StartCoroutine(apiClient.CreateIssue(request, HandleSubmitSucceeded, HandleSubmitFailed));
        }

        /// <summary>上报成功：标记转为已保存并按优先级着色，关闭表单并提示</summary>
        private void HandleSubmitSucceeded(IssueDto savedIssue)
        {
            IssueMarker marker = placementController.ConfirmPendingMarker(savedIssue);
            markerRegistry.Register(marker);
            formPanel.SetSubmitting(false);
            formPanel.Close();
            hud.ShowToast($"上报成功：#{savedIssue.id} {savedIssue.title}", false);
            Debug.Log($"[Inspection] 上报问题成功 id={savedIssue.id} priority={savedIssue.priority}");
        }

        /// <summary>上报失败：保留表单和待提交标记，显示原因以便重试或取消</summary>
        private void HandleSubmitFailed(string message)
        {
            formPanel.SetSubmitting(false);
            formPanel.ShowError($"上报失败：{message}");
        }

        /// <summary>取消填写：移除未保存的标记并关闭表单，提交中不允许取消</summary>
        private void CancelPendingIssue()
        {
            if (formPanel.IsSubmitting)
            {
                return;
            }
            placementController.DiscardPendingMarker();
            formPanel.Close();
        }

        /// <summary>从后端重新加载全部问题并同步场景标记</summary>
        private void ReloadMarkers()
        {
            if (loadingMarkers)
            {
                return;
            }
            loadingMarkers = true;
            StartCoroutine(apiClient.ListAllIssues(HandleMarkersLoaded, HandleMarkersLoadFailed));
        }

        private void HandleMarkersLoaded(List<IssueDto> issues)
        {
            loadingMarkers = false;
            markerRegistry.Sync(issues);
            hud.ShowToast($"已加载 {issues.Count} 个问题标记", false);
        }

        private void HandleMarkersLoadFailed(string message)
        {
            loadingMarkers = false;
            hud.ShowToast($"加载问题标记失败：{message}", true);
        }

        /// <summary>定时检查后端连接并更新状态显示</summary>
        private IEnumerator HealthCheckLoop()
        {
            var interval = new WaitForSeconds(HealthCheckIntervalSeconds);
            while (true)
            {
                yield return apiClient.CheckHealth(hud.SetBackendOnline);
                yield return interval;
            }
        }
    }
}
