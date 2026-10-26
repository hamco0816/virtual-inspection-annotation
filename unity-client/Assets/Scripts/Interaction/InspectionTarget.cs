using UnityEngine;

namespace Inspection.Interaction
{
    /// <summary>场景中可被标注的对象（设备、墙面、地面），提供在提示和表单中显示的名称。</summary>
    public class InspectionTarget : MonoBehaviour
    {
        [SerializeField] private string displayName = "场景表面"; // 显示名称，如"1 号机柜""北侧墙面"

        public string DisplayName => displayName;

        /// <summary>设置显示名称，供场景生成工具调用</summary>
        public void SetDisplayName(string newDisplayName)
        {
            displayName = newDisplayName;
        }
    }
}
