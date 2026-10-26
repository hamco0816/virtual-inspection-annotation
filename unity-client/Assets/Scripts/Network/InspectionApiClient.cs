using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace Inspection.Network
{
    /// <summary>
    /// Go 后端接口调用：运行状态检查、上报问题、查询全部问题。方法返回协程，由调用方 StartCoroutine 执行。
    /// </summary>
    public class InspectionApiClient
    {
        private const int SuccessCode = 200;
        private const int ListPageSize = 100;
        private const int MaxListPages = 50; // 最多翻 50 页（5000 条），防止异常数据导致无限翻页
        private const string JsonContentType = "application/json";

        private readonly ClientConfig config;

        public InspectionApiClient(ClientConfig config)
        {
            this.config = config;
        }

        public string BaseUrl => config.apiBaseUrl;

        /// <summary>检查后端是否可用（定时调用，失败不写警告日志）</summary>
        public IEnumerator CheckHealth(Action<bool> onFinished)
        {
            using UnityWebRequest request = CreateRequest("/api/health", UnityWebRequest.kHttpVerbGET, null);
            yield return request.SendWebRequest();
            onFinished(request.result == UnityWebRequest.Result.Success);
        }

        /// <summary>上报问题，成功时返回后端保存的完整记录（含 ID 和状态）</summary>
        public IEnumerator CreateIssue(CreateIssueRequest issueRequest, Action<IssueDto> onSuccess, Action<string> onError)
        {
            string requestBody = JsonUtility.ToJson(issueRequest);
            using UnityWebRequest request = CreateRequest("/api/issues", UnityWebRequest.kHttpVerbPOST, requestBody);
            yield return request.SendWebRequest();

            if (!TryReadSuccess(request, out string responseJson, out string errorMessage))
            {
                onError(errorMessage);
                yield break;
            }
            onSuccess(JsonUtility.FromJson<IssueResponse>(responseJson).data);
        }

        /// <summary>逐页拉取全部问题，用于在场景中恢复已保存的标记</summary>
        public IEnumerator ListAllIssues(Action<List<IssueDto>> onSuccess, Action<string> onError)
        {
            var issues = new List<IssueDto>();
            for (int pageNum = 1; pageNum <= MaxListPages; pageNum++)
            {
                string path = $"/api/issues?pageNum={pageNum}&pageSize={ListPageSize}";
                using UnityWebRequest request = CreateRequest(path, UnityWebRequest.kHttpVerbGET, null);
                yield return request.SendWebRequest();

                if (!TryReadSuccess(request, out string responseJson, out string errorMessage))
                {
                    onError(errorMessage);
                    yield break;
                }
                IssuePage page = JsonUtility.FromJson<IssuePageResponse>(responseJson).data;
                issues.AddRange(page.list);
                if (issues.Count >= page.total || page.list.Length < ListPageSize)
                {
                    break;
                }
            }
            onSuccess(issues);
        }

        /// <summary>创建带超时的 JSON 请求</summary>
        private UnityWebRequest CreateRequest(string path, string method, string jsonBody)
        {
            var request = new UnityWebRequest(config.apiBaseUrl + path, method)
            {
                downloadHandler = new DownloadHandlerBuffer(),
                timeout = config.requestTimeoutSeconds,
            };
            if (jsonBody != null)
            {
                request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(jsonBody));
                request.SetRequestHeader("Content-Type", JsonContentType);
            }
            return request;
        }

        /// <summary>判断请求是否成功；失败时生成可直接展示给用户的错误说明</summary>
        private bool TryReadSuccess(UnityWebRequest request, out string responseJson, out string errorMessage)
        {
            responseJson = request.downloadHandler?.text;
            errorMessage = null;

            if (request.result == UnityWebRequest.Result.ConnectionError)
            {
                Debug.LogWarning($"[Inspection] 请求失败 {request.method} {request.url}：{request.error}");
                errorMessage = $"连接不上后端服务（{config.apiBaseUrl}），请确认 Go 服务已启动且网络畅通";
                return false;
            }

            ApiResponseHeader header = ParseHeader(responseJson);
            if (header == null)
            {
                Debug.LogWarning($"[Inspection] 无法识别的响应 {request.method} {request.url}：HTTP {request.responseCode}");
                errorMessage = $"后端返回了无法识别的响应（HTTP {request.responseCode}）";
                return false;
            }
            if (header.code != SuccessCode)
            {
                errorMessage = header.message;
                return false;
            }
            return true;
        }

        /// <summary>解析统一响应格式的 code 和 message，响应不是约定格式时返回 null</summary>
        private static ApiResponseHeader ParseHeader(string responseJson)
        {
            if (string.IsNullOrWhiteSpace(responseJson))
            {
                return null;
            }
            try
            {
                ApiResponseHeader header = JsonUtility.FromJson<ApiResponseHeader>(responseJson);
                return header != null && header.code != 0 && header.message != null ? header : null;
            }
            catch (ArgumentException exception)
            {
                Debug.LogWarning($"[Inspection] 响应不是合法 JSON：{exception.Message}");
                return null;
            }
        }
    }
}
