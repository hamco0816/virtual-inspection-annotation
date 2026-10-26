using System;
using Inspection.Network;
using UnityEngine;
using UnityEngine.UI;

namespace Inspection.Views
{
    /// <summary>上报表单引用的界面元素，由场景生成工具创建并绑定</summary>
    [Serializable]
    public class IssueFormElements
    {
        public Text targetText;
        public Text positionText;
        public InputField titleInput;
        public InputField descriptionInput;
        public Button lowButton;
        public Button mediumButton;
        public Button highButton;
        public Text errorText;
        public Button cancelButton;
        public Button submitButton;
    }

    /// <summary>
    /// 问题上报表单：填写标题、描述、优先级并显示标记坐标；本地校验标题必填，提交期间禁用按钮防止重复提交。
    /// </summary>
    public class IssueFormPanel : MonoBehaviour
    {
        private const int MaxTitleLength = 100;
        private const int MaxDescriptionLength = 500;
        private const string SubmitLabel = "提交";
        private const string SubmittingLabel = "提交中…";

        private static readonly Color UnselectedColor = new Color32(227, 232, 236, 255);
        private static readonly Color UnselectedTextColor = new Color32(28, 38, 48, 255);
        private static readonly Color HighColor = new Color32(200, 50, 31, 255);
        private static readonly Color MediumColor = new Color32(178, 110, 0, 255);
        private static readonly Color LowColor = new Color32(63, 110, 140, 255);

        [SerializeField] private IssueFormElements elements;

        private string selectedPriority = IssuePriority.Medium;
        private Vector3 markerPosition;
        private bool listenersAdded;

        /// <summary>点击提交且本地校验通过</summary>
        public event Action<CreateIssueRequest> SubmitRequested;

        /// <summary>点击取消</summary>
        public event Action CancelRequested;

        public bool IsOpen => gameObject.activeSelf;

        public bool IsSubmitting { get; private set; }

        /// <summary>绑定界面元素，供场景生成工具调用</summary>
        public void Bind(IssueFormElements formElements)
        {
            elements = formElements;
        }

        /// <summary>为新放置的标记打开表单，清空上次填写的内容</summary>
        public void Open(Vector3 position, string targetName)
        {
            gameObject.SetActive(true);
            AddListenersOnce();
            markerPosition = position;
            elements.targetText.text = $"位置对象：{targetName}";
            elements.positionText.text = $"标记坐标：x {position.x:F2}   y {position.y:F2}   z {position.z:F2}";
            elements.titleInput.text = string.Empty;
            elements.descriptionInput.text = string.Empty;
            ShowError(string.Empty);
            SelectPriority(IssuePriority.Medium);
            SetSubmitting(false);
            elements.titleInput.ActivateInputField();
        }

        public void Close()
        {
            gameObject.SetActive(false);
        }

        /// <summary>切换提交中状态：提交期间禁用全部输入和按钮</summary>
        public void SetSubmitting(bool submitting)
        {
            IsSubmitting = submitting;
            elements.submitButton.interactable = !submitting;
            elements.cancelButton.interactable = !submitting;
            elements.titleInput.interactable = !submitting;
            elements.descriptionInput.interactable = !submitting;
            elements.lowButton.interactable = !submitting;
            elements.mediumButton.interactable = !submitting;
            elements.highButton.interactable = !submitting;
            elements.submitButton.GetComponentInChildren<Text>().text = submitting ? SubmittingLabel : SubmitLabel;
        }

        public void ShowError(string message)
        {
            elements.errorText.text = message;
            elements.errorText.gameObject.SetActive(!string.IsNullOrEmpty(message));
        }

        /// <summary>首次打开时注册按钮事件并设置输入长度上限</summary>
        private void AddListenersOnce()
        {
            if (listenersAdded)
            {
                return;
            }
            listenersAdded = true;
            elements.titleInput.characterLimit = MaxTitleLength;
            elements.descriptionInput.characterLimit = MaxDescriptionLength;
            elements.lowButton.onClick.AddListener(() => SelectPriority(IssuePriority.Low));
            elements.mediumButton.onClick.AddListener(() => SelectPriority(IssuePriority.Medium));
            elements.highButton.onClick.AddListener(() => SelectPriority(IssuePriority.High));
            elements.cancelButton.onClick.AddListener(() => CancelRequested?.Invoke());
            elements.submitButton.onClick.AddListener(HandleSubmitClicked);
        }

        /// <summary>校验标题必填后发出提交请求</summary>
        private void HandleSubmitClicked()
        {
            string title = elements.titleInput.text.Trim();
            if (title.Length == 0)
            {
                ShowError("请填写标题");
                elements.titleInput.ActivateInputField();
                return;
            }
            ShowError(string.Empty);
            SubmitRequested?.Invoke(new CreateIssueRequest
            {
                title = title,
                description = elements.descriptionInput.text.Trim(),
                priority = selectedPriority,
                position = PositionDto.FromVector3(markerPosition),
            });
        }

        /// <summary>选中优先级，并让对应按钮显示为实底颜色</summary>
        private void SelectPriority(string priority)
        {
            selectedPriority = priority;
            PaintPriorityButton(elements.lowButton, priority == IssuePriority.Low, LowColor);
            PaintPriorityButton(elements.mediumButton, priority == IssuePriority.Medium, MediumColor);
            PaintPriorityButton(elements.highButton, priority == IssuePriority.High, HighColor);
        }

        private static void PaintPriorityButton(Button button, bool selected, Color selectedColor)
        {
            button.targetGraphic.color = selected ? selectedColor : UnselectedColor;
            button.GetComponentInChildren<Text>().color = selected ? Color.white : UnselectedTextColor;
        }
    }
}
