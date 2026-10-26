using System;
using System.IO;
using UnityEngine;

namespace Inspection.Network
{
    /// <summary>
    /// 客户端配置：从 StreamingAssets/client-config.json 读取后端地址和请求超时，打包后可直接修改该文件。
    /// </summary>
    [Serializable]
    public class ClientConfig
    {
        private const string ConfigFileName = "client-config.json";
        private const string DefaultApiBaseUrl = "http://127.0.0.1:8080";
        private const int DefaultTimeoutSeconds = 8;

        /// <summary>Go 后端地址，例如 http://127.0.0.1:8080</summary>
        public string apiBaseUrl = DefaultApiBaseUrl;

        /// <summary>单次请求超时时间（秒）</summary>
        public int requestTimeoutSeconds = DefaultTimeoutSeconds;

        /// <summary>读取配置文件，文件缺失或格式错误时使用默认值并记录警告</summary>
        public static ClientConfig Load()
        {
            string configPath = Path.Combine(Application.streamingAssetsPath, ConfigFileName);
            if (!File.Exists(configPath))
            {
                Debug.LogWarning($"[Inspection] 未找到配置文件 {configPath}，使用默认后端地址 {DefaultApiBaseUrl}");
                return new ClientConfig();
            }

            try
            {
                ClientConfig config = JsonUtility.FromJson<ClientConfig>(File.ReadAllText(configPath));
                config.Normalize();
                return config;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[Inspection] 配置文件 {configPath} 格式错误，使用默认值：{exception.Message}");
                return new ClientConfig();
            }
        }

        /// <summary>去掉地址末尾的斜杠，补齐缺失的配置项</summary>
        private void Normalize()
        {
            apiBaseUrl = string.IsNullOrWhiteSpace(apiBaseUrl) ? DefaultApiBaseUrl : apiBaseUrl.Trim().TrimEnd('/');
            if (requestTimeoutSeconds <= 0)
            {
                requestTimeoutSeconds = DefaultTimeoutSeconds;
            }
        }
    }
}
