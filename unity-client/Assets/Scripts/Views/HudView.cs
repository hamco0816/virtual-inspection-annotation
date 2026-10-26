using System;
using UnityEngine;
using UnityEngine.UI;

namespace Inspection.Views
{
    /// <summary>常驻界面引用的元素，由场景生成工具创建并绑定</summary>
    [Serializable]
    public class HudElements
    {
        public Text backendStatusText;
        public GameObject hoverRoot;
        public Text hoverText;
        public GameObject toastRoot;
        public Image toastBackground;
        public Text toastText;
    }

    /// <summary>
    /// 常驻界面：后端连接状态、鼠标指向的对象名称、操作结果提示条（几秒后自动隐藏）。
    /// </summary>
    public class HudView : MonoBehaviour
    {
        private const float ToastDurationSeconds = 3.5f;

        private static readonly Color OnlineColor = new Color32(120, 200, 150, 255);
        private static readonly Color OfflineColor = new Color32(255, 140, 125, 255);
        private static readonly Color ToastInfoColor = new Color32(28, 38, 48, 235);
        private static readonly Color ToastErrorColor = new Color32(179, 38, 30, 240);

        [SerializeField] private HudElements elements;

        private float toastHideTime;
        private string backendUrl = string.Empty;

        /// <summary>绑定界面元素，供场景生成工具调用</summary>
        public void Bind(HudElements hudElements)
        {
            elements = hudElements;
        }

        private void Update()
        {
            if (elements.toastRoot.activeSelf && Time.unscaledTime >= toastHideTime)
            {
                elements.toastRoot.SetActive(false);
            }
        }

        public void SetBackendUrl(string url)
        {
            backendUrl = url;
            elements.backendStatusText.text = $"正在检查后端（{backendUrl}）";
        }

        public void SetBackendOnline(bool online)
        {
            elements.backendStatusText.text = online ? $"后端已连接（{backendUrl}）" : $"后端未连接（{backendUrl}）";
            elements.backendStatusText.color = online ? OnlineColor : OfflineColor;
        }

        /// <summary>显示鼠标指向的对象，传 null 时隐藏</summary>
        public void SetHoverTarget(string targetName)
        {
            bool visible = !string.IsNullOrEmpty(targetName);
            elements.hoverRoot.SetActive(visible);
            if (visible)
            {
                elements.hoverText.text = $"指向：{targetName}";
            }
        }

        /// <summary>显示操作结果提示，几秒后自动隐藏</summary>
        public void ShowToast(string message, bool isError)
        {
            elements.toastText.text = message;
            elements.toastBackground.color = isError ? ToastErrorColor : ToastInfoColor;
            elements.toastRoot.SetActive(true);
            toastHideTime = Time.unscaledTime + ToastDurationSeconds;
        }
    }
}
