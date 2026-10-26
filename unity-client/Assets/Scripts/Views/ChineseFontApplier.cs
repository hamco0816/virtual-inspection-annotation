using UnityEngine;
using UnityEngine.UI;

namespace Inspection.Views
{
    /// <summary>运行时为界面文字套用系统中文字体，避免内置字体缺少中文字形显示成方块。</summary>
    public class ChineseFontApplier : MonoBehaviour
    {
        private const int DynamicFontSize = 16;

        // 按顺序选用本机已安装的第一个字体：Windows 微软雅黑、macOS 苹方、Linux 思源黑体
        private static readonly string[] PreferredFontNames =
        {
            "Microsoft YaHei UI", "Microsoft YaHei", "PingFang SC", "Noto Sans CJK SC", "SimHei",
        };

        private void Awake()
        {
            Font chineseFont = Font.CreateDynamicFontFromOSFont(PreferredFontNames, DynamicFontSize);
            foreach (Text text in GetComponentsInChildren<Text>(true))
            {
                text.font = chineseFont;
            }
        }
    }
}
