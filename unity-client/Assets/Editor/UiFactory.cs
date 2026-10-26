using UnityEngine;
using UnityEngine.UI;

namespace Inspection.EditorTools
{
    /// <summary>场景生成工具使用的界面元素工厂：创建统一风格的面板、文字、输入框、按钮和布局行。</summary>
    public static class UiFactory
    {
        public static readonly Color Ink = new Color32(28, 38, 48, 255);
        public static readonly Color InkTranslucent = new Color32(28, 38, 48, 215);
        public static readonly Color Sheet = new Color32(250, 251, 252, 250);
        public static readonly Color Muted = new Color32(86, 99, 110, 255);
        public static readonly Color Field = new Color32(236, 240, 243, 255);
        public static readonly Color QuietButton = new Color32(227, 232, 236, 255);
        public static readonly Color Error = new Color32(179, 38, 30, 255);

        private const string BuiltinFontName = "LegacyRuntime.ttf";
        private static Font builtinFont;

        private static Font BuiltinFont => builtinFont != null ? builtinFont : builtinFont = Resources.GetBuiltinResource<Font>(BuiltinFontName);

        /// <summary>创建空的界面节点</summary>
        public static RectTransform CreateRect(string name, Transform parent)
        {
            var rectObject = new GameObject(name, typeof(RectTransform));
            rectObject.transform.SetParent(parent, false);
            return (RectTransform)rectObject.transform;
        }

        /// <summary>创建纯色面板；不可交互的面板关闭射线检测，避免挡住场景点击</summary>
        public static Image CreatePanel(string name, Transform parent, Color color, bool blocksPointer)
        {
            Image panel = CreateRect(name, parent).gameObject.AddComponent<Image>();
            panel.color = color;
            panel.raycastTarget = blocksPointer;
            return panel;
        }

        /// <summary>把节点固定在屏幕某个角或边的位置</summary>
        public static void Anchor(RectTransform rect, Vector2 anchor, Vector2 anchoredPosition, float width)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = new Vector2(width, 0f);
        }

        /// <summary>给面板加纵向自动布局，高度随内容伸缩；宽度为 0 时同时按内容伸缩宽度</summary>
        public static void AddVerticalLayout(GameObject target, int padding, float spacing, bool fitWidth)
        {
            var layout = target.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(padding, padding, padding, padding);
            layout.spacing = spacing;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var fitter = target.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.horizontalFit = fitWidth ? ContentSizeFitter.FitMode.PreferredSize : ContentSizeFitter.FitMode.Unconstrained;
        }

        /// <summary>创建自动换行的文字</summary>
        public static Text CreateText(Transform parent, string content, int fontSize, Color color, bool bold)
        {
            Text text = CreateRect("Text", parent).gameObject.AddComponent<Text>();
            text.font = BuiltinFont;
            text.text = content;
            text.fontSize = fontSize;
            text.color = color;
            text.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        /// <summary>创建输入框，multiline 为 true 时支持换行输入</summary>
        public static InputField CreateInputField(Transform parent, string placeholder, float height, bool multiline)
        {
            GameObject fieldObject = DefaultControls.CreateInputField(new DefaultControls.Resources());
            fieldObject.transform.SetParent(parent, false);
            fieldObject.GetComponent<Image>().color = Field;

            InputField inputField = fieldObject.GetComponent<InputField>();
            inputField.lineType = multiline ? InputField.LineType.MultiLineNewline : InputField.LineType.SingleLine;
            StyleFieldText(inputField.textComponent, Ink, multiline);
            var placeholderText = (Text)inputField.placeholder;
            placeholderText.text = placeholder;
            StyleFieldText(placeholderText, Muted, multiline);

            SetHeight(fieldObject, height);
            return inputField;
        }

        /// <summary>创建纯色按钮</summary>
        public static Button CreateButton(Transform parent, string label, Color background, Color textColor, float height)
        {
            GameObject buttonObject = DefaultControls.CreateButton(new DefaultControls.Resources());
            buttonObject.name = $"Button {label}";
            buttonObject.transform.SetParent(parent, false);
            buttonObject.GetComponent<Image>().color = background;

            Button button = buttonObject.GetComponent<Button>();
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(0.92f, 0.92f, 0.92f, 1f);
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            colors.disabledColor = new Color(0.75f, 0.75f, 0.75f, 0.6f);
            button.colors = colors;

            Text labelText = buttonObject.GetComponentInChildren<Text>();
            labelText.font = BuiltinFont;
            labelText.text = label;
            labelText.fontSize = 18;
            labelText.color = textColor;

            SetHeight(buttonObject, height);
            return button;
        }

        /// <summary>创建横向等分的按钮行</summary>
        public static RectTransform CreateRow(string name, Transform parent, float spacing, float height)
        {
            RectTransform row = CreateRect(name, parent);
            var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = spacing;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;
            SetHeight(row.gameObject, height);
            return row;
        }

        private static void SetHeight(GameObject target, float height)
        {
            var layoutElement = target.AddComponent<LayoutElement>();
            layoutElement.minHeight = height;
            layoutElement.preferredHeight = height;
        }

        private static void StyleFieldText(Text text, Color color, bool multiline)
        {
            text.font = BuiltinFont;
            text.fontSize = 18;
            text.color = color;
            text.alignment = multiline ? TextAnchor.UpperLeft : TextAnchor.MiddleLeft;
        }
    }
}
