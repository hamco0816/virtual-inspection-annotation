using System;
using System.Globalization;
using Inspection.Network;
using UnityEngine;
using UnityEngine.UI;

namespace Inspection.Views
{
    /// <summary>问题详情面板引用的界面元素，由场景生成工具创建并绑定</summary>
    [Serializable]
    public class IssueDetailElements
    {
        public Text titleText;
        public Text metaText;
        public Text descriptionText;
        public Text positionText;
        public Text timeText;
        public Button closeButton;
    }

    /// <summary>问题详情面板：点击场景中已保存的标记后显示标题、优先级、状态、描述、坐标和时间。</summary>
    public class IssueDetailPanel : MonoBehaviour
    {
        private const string TimeFormat = "yyyy-MM-dd HH:mm";

        [SerializeField] private IssueDetailElements elements;

        private bool listenersAdded;

        /// <summary>绑定界面元素，供场景生成工具调用</summary>
        public void Bind(IssueDetailElements detailElements)
        {
            elements = detailElements;
        }

        /// <summary>显示指定问题的详情</summary>
        public void Show(IssueDto issue)
        {
            gameObject.SetActive(true);
            if (!listenersAdded)
            {
                listenersAdded = true;
                elements.closeButton.onClick.AddListener(Hide);
            }
            elements.titleText.text = $"#{issue.id}  {issue.title}";
            elements.metaText.text = $"优先级：{IssuePriority.Label(issue.priority)}      状态：{IssueStatus.Label(issue.status)}";
            elements.descriptionText.text = string.IsNullOrEmpty(issue.description) ? "（无描述）" : issue.description;
            Vector3 position = issue.position.ToVector3();
            elements.positionText.text = $"标记坐标：x {position.x:F2}   y {position.y:F2}   z {position.z:F2}";
            elements.timeText.text = $"上报：{FormatTime(issue.createdAt)}      更新：{FormatTime(issue.updatedAt)}";
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        /// <summary>把后端的 RFC3339 时间转换为本地时间显示</summary>
        private static string FormatTime(string isoTime)
        {
            return DateTimeOffset.TryParse(isoTime, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTimeOffset time)
                ? time.ToLocalTime().ToString(TimeFormat, CultureInfo.InvariantCulture)
                : isoTime;
        }
    }
}
