using Inspection.Views;
using UnityEngine;
using UnityEngine.UI;

namespace Inspection.EditorTools
{
    /// <summary>场景界面生成结果</summary>
    public struct InspectionUi
    {
        public IssueFormPanel FormPanel;
        public IssueDetailPanel DetailPanel;
        public HudView Hud;
    }

    /// <summary>生成巡检客户端界面：操作说明、后端状态、指向提示、提示条、上报表单、问题详情。</summary>
    public static class InspectionUiBuilder
    {
        private const float ReferenceWidth = 1920f;
        private const float ReferenceHeight = 1080f;
        private const float ScreenMargin = 24f;
        private const float SidePanelWidth = 480f;
        private const float FieldHeight = 44f;
        private const float DescriptionHeight = 110f;
        private const float ButtonHeight = 44f;

        private static readonly Color HelpTextColor = new Color32(214, 221, 226, 255);

        /// <summary>创建画布及全部界面，并把元素绑定到对应组件</summary>
        public static InspectionUi Build()
        {
            Transform canvas = CreateCanvas();
            HudView hud = CreateHud(canvas);
            return new InspectionUi
            {
                Hud = hud,
                FormPanel = CreateFormPanel(canvas),
                DetailPanel = CreateDetailPanel(canvas),
            };
        }

        private static Transform CreateCanvas()
        {
            var canvasObject = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(ChineseFontApplier));
            canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(ReferenceWidth, ReferenceHeight);
            scaler.matchWidthOrHeight = 0.5f;
            return canvasObject.transform;
        }

        /// <summary>常驻界面：左上操作说明、右上后端状态、底部指向提示和提示条</summary>
        private static HudView CreateHud(Transform canvas)
        {
            Image helpPanel = UiFactory.CreatePanel("HelpPanel", canvas, UiFactory.InkTranslucent, false);
            UiFactory.Anchor(helpPanel.rectTransform, new Vector2(0f, 1f), new Vector2(ScreenMargin, -ScreenMargin), 560f);
            UiFactory.AddVerticalLayout(helpPanel.gameObject, 18, 6f, false);
            UiFactory.CreateText(helpPanel.transform, "虚拟巡检现场", 22, Color.white, true);
            UiFactory.CreateText(helpPanel.transform, "W / A / S / D 移动，Q / E 升降，按住 Shift 加速；按住鼠标右键转动视角", 16, HelpTextColor, false);
            UiFactory.CreateText(helpPanel.transform, "鼠标左键点击设备或墙面、地面放置问题标记，点击已有标记查看详情", 16, HelpTextColor, false);
            UiFactory.CreateText(helpPanel.transform, "R 从后端刷新标记；Esc 取消当前填写", 16, HelpTextColor, false);

            Image statusPanel = UiFactory.CreatePanel("BackendStatus", canvas, UiFactory.InkTranslucent, false);
            UiFactory.Anchor(statusPanel.rectTransform, new Vector2(1f, 1f), new Vector2(-ScreenMargin, -ScreenMargin), 0f);
            UiFactory.AddVerticalLayout(statusPanel.gameObject, 14, 0f, true);
            Text statusText = UiFactory.CreateText(statusPanel.transform, "正在检查后端", 16, Color.white, false);
            statusText.horizontalOverflow = HorizontalWrapMode.Overflow;

            Image hoverPanel = CreateBottomLabel(canvas, "HoverTarget", 96f, out Text hoverText);
            Image toastPanel = CreateBottomLabel(canvas, "Toast", ScreenMargin, out Text toastText);
            toastPanel.gameObject.SetActive(false);

            HudView hud = canvas.gameObject.AddComponent<HudView>();
            hud.Bind(new HudElements
            {
                backendStatusText = statusText,
                hoverRoot = hoverPanel.gameObject,
                hoverText = hoverText,
                toastRoot = toastPanel.gameObject,
                toastBackground = toastPanel,
                toastText = toastText,
            });
            return hud;
        }

        private static Image CreateBottomLabel(Transform canvas, string name, float bottomOffset, out Text label)
        {
            Image panel = UiFactory.CreatePanel(name, canvas, UiFactory.InkTranslucent, false);
            UiFactory.Anchor(panel.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, bottomOffset), 0f);
            UiFactory.AddVerticalLayout(panel.gameObject, 12, 0f, true);
            label = UiFactory.CreateText(panel.transform, string.Empty, 18, Color.white, false);
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            return panel;
        }

        /// <summary>右侧上报表单</summary>
        private static IssueFormPanel CreateFormPanel(Transform canvas)
        {
            Image panel = CreateSidePanel(canvas, "IssueFormPanel");
            Transform body = panel.transform;
            UiFactory.CreateText(body, "上报巡检问题", 24, UiFactory.Ink, true);
            Text targetText = UiFactory.CreateText(body, "位置对象：", 16, UiFactory.Muted, false);
            Text positionText = UiFactory.CreateText(body, "标记坐标：", 16, UiFactory.Muted, false);

            UiFactory.CreateText(body, "标题（必填）", 16, UiFactory.Ink, false);
            InputField titleInput = UiFactory.CreateInputField(body, "例如：1 号机柜温度异常", FieldHeight, false);
            UiFactory.CreateText(body, "描述（可选）", 16, UiFactory.Ink, false);
            InputField descriptionInput = UiFactory.CreateInputField(body, "补充现场情况", DescriptionHeight, true);

            UiFactory.CreateText(body, "优先级", 16, UiFactory.Ink, false);
            RectTransform priorityRow = UiFactory.CreateRow("PriorityRow", body, 8f, 40f);
            Button lowButton = UiFactory.CreateButton(priorityRow, "低", UiFactory.QuietButton, UiFactory.Ink, 40f);
            Button mediumButton = UiFactory.CreateButton(priorityRow, "中", UiFactory.QuietButton, UiFactory.Ink, 40f);
            Button highButton = UiFactory.CreateButton(priorityRow, "高", UiFactory.QuietButton, UiFactory.Ink, 40f);

            Text errorText = UiFactory.CreateText(body, string.Empty, 16, UiFactory.Error, false);
            RectTransform actionRow = UiFactory.CreateRow("ActionRow", body, 12f, ButtonHeight);
            Button cancelButton = UiFactory.CreateButton(actionRow, "取消", UiFactory.QuietButton, UiFactory.Ink, ButtonHeight);
            Button submitButton = UiFactory.CreateButton(actionRow, "提交", UiFactory.Ink, Color.white, ButtonHeight);

            IssueFormPanel formPanel = panel.gameObject.AddComponent<IssueFormPanel>();
            formPanel.Bind(new IssueFormElements
            {
                targetText = targetText,
                positionText = positionText,
                titleInput = titleInput,
                descriptionInput = descriptionInput,
                lowButton = lowButton,
                mediumButton = mediumButton,
                highButton = highButton,
                errorText = errorText,
                cancelButton = cancelButton,
                submitButton = submitButton,
            });
            panel.gameObject.SetActive(false);
            return formPanel;
        }

        /// <summary>右侧问题详情</summary>
        private static IssueDetailPanel CreateDetailPanel(Transform canvas)
        {
            Image panel = CreateSidePanel(canvas, "IssueDetailPanel");
            Transform body = panel.transform;
            Text titleText = UiFactory.CreateText(body, string.Empty, 22, UiFactory.Ink, true);
            Text metaText = UiFactory.CreateText(body, string.Empty, 16, UiFactory.Ink, false);
            Text descriptionText = UiFactory.CreateText(body, string.Empty, 16, UiFactory.Muted, false);
            Text positionText = UiFactory.CreateText(body, string.Empty, 16, UiFactory.Muted, false);
            Text timeText = UiFactory.CreateText(body, string.Empty, 16, UiFactory.Muted, false);
            Button closeButton = UiFactory.CreateButton(body, "关闭", UiFactory.QuietButton, UiFactory.Ink, ButtonHeight);

            IssueDetailPanel detailPanel = panel.gameObject.AddComponent<IssueDetailPanel>();
            detailPanel.Bind(new IssueDetailElements
            {
                titleText = titleText,
                metaText = metaText,
                descriptionText = descriptionText,
                positionText = positionText,
                timeText = timeText,
                closeButton = closeButton,
            });
            panel.gameObject.SetActive(false);
            return detailPanel;
        }

        private static Image CreateSidePanel(Transform canvas, string name)
        {
            Image panel = UiFactory.CreatePanel(name, canvas, UiFactory.Sheet, true);
            UiFactory.Anchor(panel.rectTransform, new Vector2(1f, 0.5f), new Vector2(-ScreenMargin, 0f), SidePanelWidth);
            UiFactory.AddVerticalLayout(panel.gameObject, 24, 10f, false);
            return panel;
        }
    }
}
