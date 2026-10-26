using System.Collections.Generic;
using Inspection.Network;
using UnityEngine;

namespace Inspection.Interaction
{
    /// <summary>
    /// 已保存的问题标记集合：按后端数据在场景中恢复标记，并在刷新时同步颜色和增删。
    /// </summary>
    public class IssueMarkerRegistry : MonoBehaviour
    {
        private readonly Dictionary<long, IssueMarker> markers = new Dictionary<long, IssueMarker>();

        public int Count => markers.Count;

        /// <summary>登记刚上报成功的标记</summary>
        public void Register(IssueMarker marker)
        {
            markers[marker.Issue.id] = marker;
        }

        /// <summary>按后端问题列表同步场景标记：新增缺失的、更新已有的、移除后端已不存在的</summary>
        public void Sync(IReadOnlyCollection<IssueDto> issues)
        {
            var latestIds = new HashSet<long>();
            foreach (IssueDto issue in issues)
            {
                latestIds.Add(issue.id);
                if (!markers.TryGetValue(issue.id, out IssueMarker marker))
                {
                    // 后端只保存坐标不保存表面朝向，恢复的标记统一竖直放置
                    marker = IssueMarker.Create(issue.position.ToVector3(), Vector3.up, transform);
                    markers[issue.id] = marker;
                }
                marker.SetIssue(issue);
            }
            RemoveMarkersNotIn(latestIds);
        }

        /// <summary>移除后端已不存在的标记</summary>
        private void RemoveMarkersNotIn(HashSet<long> latestIds)
        {
            var staleIds = new List<long>();
            foreach (long markerId in markers.Keys)
            {
                if (!latestIds.Contains(markerId))
                {
                    staleIds.Add(markerId);
                }
            }
            foreach (long staleId in staleIds)
            {
                Destroy(markers[staleId].gameObject);
                markers.Remove(staleId);
            }
        }
    }
}
