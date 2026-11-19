using System;

namespace GeoChemistryNexus.ViewModels
{
    /// <summary>
    /// 通知卡片上的操作按钮（撤销 / 查看 / 取消 等）。
    /// </summary>
    public sealed class NotificationAction
    {
        public NotificationAction(string text, Action invoke, bool isPrimary = false, bool closeOnInvoke = true)
        {
            Text = text;
            Invoke = invoke;
            IsPrimary = isPrimary;
            CloseOnInvoke = closeOnInvoke;
        }

        /// <summary>按钮文字。</summary>
        public string Text { get; }

        /// <summary>点击后执行的回调（在 UI 线程上调用）。</summary>
        public Action Invoke { get; }

        /// <summary>是否为主要操作（使用品牌色强调）。</summary>
        public bool IsPrimary { get; }

        /// <summary>点击后是否关闭通知，默认关闭。</summary>
        public bool CloseOnInvoke { get; }
    }
}
