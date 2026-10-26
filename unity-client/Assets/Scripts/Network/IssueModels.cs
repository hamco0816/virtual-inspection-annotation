using System;
using UnityEngine;

namespace Inspection.Network
{
    /// <summary>问题优先级取值，与 Go 后端、React 管理端一致</summary>
    public static class IssuePriority
    {
        public const string Low = "low";
        public const string Medium = "medium";
        public const string High = "high";

        /// <summary>优先级的中文显示文字</summary>
        public static string Label(string priority) => priority switch
        {
            High => "高",
            Medium => "中",
            Low => "低",
            _ => priority,
        };
    }

    /// <summary>问题处理状态取值，与 Go 后端、React 管理端一致</summary>
    public static class IssueStatus
    {
        public const string Open = "open";
        public const string InProgress = "in_progress";
        public const string Resolved = "resolved";

        /// <summary>处理状态的中文显示文字</summary>
        public static string Label(string status) => status switch
        {
            Open => "待处理",
            InProgress => "处理中",
            Resolved => "已解决",
            _ => status,
        };
    }

    /// <summary>问题标记在场景中的世界坐标</summary>
    [Serializable]
    public class PositionDto
    {
        private const int CoordinateDecimals = 3;

        public float x;
        public float y;
        public float z;

        /// <summary>从 Unity 坐标转换，保留到毫米精度</summary>
        public static PositionDto FromVector3(Vector3 position) => new PositionDto
        {
            x = (float)Math.Round(position.x, CoordinateDecimals),
            y = (float)Math.Round(position.y, CoordinateDecimals),
            z = (float)Math.Round(position.z, CoordinateDecimals),
        };

        public Vector3 ToVector3() => new Vector3(x, y, z);
    }

    /// <summary>后端返回的巡检问题</summary>
    [Serializable]
    public class IssueDto
    {
        public long id;
        public string title;
        public string description;
        public string priority;
        public string status;
        public PositionDto position;
        public string createdAt;
        public string updatedAt;
    }

    /// <summary>上报问题的请求体</summary>
    [Serializable]
    public class CreateIssueRequest
    {
        public string title;
        public string description;
        public string priority;
        public PositionDto position;
    }

    /// <summary>统一响应格式中的 code 和 message，用于解析失败响应</summary>
    [Serializable]
    public class ApiResponseHeader
    {
        public int code;
        public string message;
    }

    /// <summary>单个问题的响应</summary>
    [Serializable]
    public class IssueResponse
    {
        public int code;
        public string message;
        public IssueDto data;
    }

    /// <summary>分页查询结果</summary>
    [Serializable]
    public class IssuePage
    {
        public long total;
        public IssueDto[] list;
        public int pageNum;
        public int pageSize;
    }

    /// <summary>分页查询的响应</summary>
    [Serializable]
    public class IssuePageResponse
    {
        public int code;
        public string message;
        public IssuePage data;
    }
}
