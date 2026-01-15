using GeoChemistryNexus.Models;
using Jint.Runtime;
using Jint;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Text.RegularExpressions;
using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.Services;
using CommunityToolkit.Mvvm.Messaging;
using GeoChemistryNexus.Messages;

namespace GeoChemistryNexus.Controls
{
    /// <summary>
    /// ScriptDefinitionControl.xaml 的交互逻辑
    /// </summary>
    public partial class ScriptDefinitionControl : UserControl, IRecipient<ScriptValidationExecuteMessage>
    {
        public static readonly DependencyProperty ScriptDefinitionProperty =
            DependencyProperty.Register("ScriptDefinition", typeof(ScriptDefinition), typeof(ScriptDefinitionControl),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnScriptDefinitionChanged));

        public ScriptDefinition ScriptDefinition
        {
            get { return (ScriptDefinition)GetValue(ScriptDefinitionProperty); }
            set { SetValue(ScriptDefinitionProperty, value); }
        }

        public ScriptDefinitionControl()
        {
            InitializeComponent();
            InitializeLanguageComboBox();
            InitializeEventHandlers();

            // 如果没有脚本定义对象，默认创建
            if (ScriptDefinition == null)
            {
                ScriptDefinition = new ScriptDefinition();
            }
            
            // 注册消息接收
            WeakReferenceMessenger.Default.Register<ScriptValidationExecuteMessage>(this);
        }

        private static void OnScriptDefinitionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // 当ScriptDefinition属性变化时，会调用此方法
            var control = d as ScriptDefinitionControl;
            if (control != null)
            {
                // 延迟调用,确保控件加载完毕后调用更新
                control.Dispatcher.InvokeAsync(() =>
                {
                    control.UpdateLineColumn();
                    control.UpdateStatus(LanguageService.Instance["ready"]); // 同时也可以重置状态
                });
            }
        }

        private void InitializeLanguageComboBox()
        {
            var languageItems = new List<object>
            {
                new { DisplayName = "JavaScript", Value = ScriptLanguage.JavaScript }
            };

            LanguageComboBox.ItemsSource = languageItems;
        }

        private void InitializeEventHandlers()
        {
            ScriptBodyTextBox.TextChanged += ScriptBodyTextBox_TextChanged;
            ScriptBodyTextBox.SelectionChanged += ScriptBodyTextBox_SelectionChanged;
        }

        private void ScriptBodyTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateStatus(LanguageService.Instance["modified_successfully"]);
            UpdateLineColumn();
        }

        private void ScriptBodyTextBox_SelectionChanged(object sender, RoutedEventArgs e)
        {
            UpdateLineColumn();
        }

        private void UpdateLineColumn()
        {
            var textBox = ScriptBodyTextBox;
            int caretIndex = textBox.CaretIndex;
            int line = textBox.GetLineIndexFromCharacterIndex(caretIndex) + 1;
            if (line >= 1)
            {
                int column = caretIndex - textBox.GetCharacterIndexFromLineIndex(line - 1) + 1;

                LineColumnTextBlock.Text = LanguageService.Instance["row"] + line + " "
                                            + LanguageService.Instance["column"] + column;
            }
        }

        private void UpdateStatus(string status, Brush? color = null)
        {
            StatusTextBlock.Text = status;
            StatusTextBlock.Foreground = color ?? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#666666"));
        }

        private void ValidateButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var script = ScriptDefinition?.ScriptBody ?? "";
                if (string.IsNullOrWhiteSpace(script))
                {
                    UpdateStatus(LanguageService.Instance["script_content_is_empty"], Brushes.Red);
                    return;
                }

                // 发送验证请求消息，让 MainPlotViewModel 处理数据检查和确认
                WeakReferenceMessenger.Default.Send(new Messages.ScriptValidationRequestMessage());
            }
            catch (Exception ex)
            {
                UpdateStatus(LanguageService.Instance["validation_failed"] + ex.Message, Brushes.Red);
            }
        }

        // 执行实际的脚本验证逻辑（由 MainPlotViewModel 调用）
        public void PerformValidation()
        {
            var script = ScriptDefinition?.ScriptBody ?? "";
            var language = ScriptDefinition?.Language ?? ScriptLanguage.JavaScript;
            ValidateScript(script, language);
        }

        /// <summary>
        /// 接收脚本验证执行消息
        /// </summary>
        public void Receive(ScriptValidationExecuteMessage message)
        {
            PerformValidation();
        }

        // 检查参数格式是否正确
        private (string, bool) CheckParameter(string parameter)
        {
            if (string.IsNullOrEmpty(parameter) || parameter == "")
            {
                // 数据参数为空
                return (LanguageService.Instance["data_parameters_are_empty"], false);
            }

            // 正则表达式，确保只包含字母、数字、下划线和英文逗号
            string pattern = @"^([a-zA-Z0-9_]+(,[a-zA-Z0-9_]+)*)?$";
            if (!Regex.IsMatch(parameter, pattern))
            {
                return (LanguageService.Instance["invalid_data_parameter_format"], false);
            }

            return (LanguageService.Instance["parameter_format_correct"], true);

        }


        // 验证脚本
        private void ValidateScript(string script, ScriptLanguage language)
        {
            switch (language)
            {
                case ScriptLanguage.JavaScript:
                     ValidateJavaScript(script);
                    break;

                default:
                    return;
            }
        }

        // 验证 JS 脚本
        private void ValidateJavaScript(string script)
        {
            try
            {
                (string,bool) res = CheckParameter(RequiredDataSeriesTextBox.Text);
                if (!res.Item2)
                {
                    UpdateStatus(res.Item1, Brushes.Red);
                    return;
                }

                // 创建一个新的Jint引擎实例
                var engine = new Engine();
                var tempLogs = new List<string>();
                Helpers.JintHelper.InjectTraceFunction(engine, tempLogs); // 注入trace函数避免验证失败

                // 按逗号分隔字符串并去除多余的空格
                string[] variables = RequiredDataSeriesTextBox.Text.Split(',').Select(v => v.Trim()).ToArray();
                // 将每个变量添加到 Jint 中
                foreach (string variable in variables)
                {
                    engine.SetValue(variable, 1);
                }


                // 尝试解析脚本。如果脚本有语法错误，抛出异常。
                engine.Execute(script);

                // 如果没有异常抛出，说明语法是有效的。
                UpdateStatus(LanguageService.Instance["syntax_validation_passed"], Brushes.Green);
                
                // 发送验证通过消息，通知 MainPlotViewModel 更新数据表格
                WeakReferenceMessenger.Default.Send(new ScriptValidatedMessage(true));
            }
            catch (Exception ex)
            {
                // 捕获其他可能的异常
                UpdateStatus(LanguageService.Instance["error"] + ex.Message, Brushes.Red);
            }
        }
    }
}