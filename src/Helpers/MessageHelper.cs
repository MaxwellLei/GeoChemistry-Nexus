using HandyControl.Controls;
using HandyControl.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeoChemistryNexus.Helpers
{
    public class MessageHelper
    {
        //消息自动关闭通知时间
        public static int waitTime = 5;

        //普通消息通知
        public static void Info(string message)
        {
            GrowlInfo growlInfo = new GrowlInfo();
            growlInfo.WaitTime = waitTime;
            growlInfo.Message = message;
            Growl.Info(growlInfo);
        }

        //成功消息通知
        public static void Success(string message)
        {
            GrowlInfo growlInfo = new GrowlInfo();
            growlInfo.WaitTime = waitTime;
            growlInfo.Message = message;
            Growl.Success(growlInfo);
        }

        //警告消息通知
        public static void Warning(string message)
        {
            GrowlInfo growlInfo = new GrowlInfo();
            growlInfo.WaitTime = waitTime;
            growlInfo.Message = message;
            Growl.Warning(growlInfo);
        }

        //错误消息通知
        public static void Error(string message)
        {
            GrowlInfo growlInfo = new GrowlInfo();
            growlInfo.WaitTime = waitTime;
            growlInfo.Message = message;
            Growl.Error(growlInfo);
        }

        public static Task<bool> ShowAsyncDialog(string message)
        {
            var tcs = new TaskCompletionSource<bool>();

            Growl.Ask(message, isConfirmed =>
            {
                tcs.SetResult(isConfirmed);
                return true;
            });

            return tcs.Task;
        }
    }
}
