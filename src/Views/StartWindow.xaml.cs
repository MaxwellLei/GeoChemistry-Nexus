using GeoChemistryNexus.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace GeoChemistryNexus.Views
{
    /// <summary>
    /// StartWindow.xaml 的交互逻辑
    /// </summary>
    public partial class StartWindow : Window
    {
        private bool isDragging = false;                      // 是否正在拖动窗体
        private Point startPoint;                             // 当前鼠标按下的位置

        public StartWindow()
        {
            LanguageHelper.InitializeLanguage();        // 初始化语言
            InitializeComponent();
            //LanguageHelper.InitializeLanguage();
            // 异步执行启动过程
            Task.Run(() =>
            {
                // 模拟启动过程，更新进度条的进度
                for (int i = 0; i <= 100; i++)
                {
                    // 更新进度条的进度
                    this.Dispatcher.Invoke(() =>
                    {
                        progressBar.Value = i;
                    });

                    // 模拟一段耗时的操作
                    Thread.Sleep(20);
                }

                // 启动过程完成，关闭窗口并打开主界面
                this.Dispatcher.Invoke(() =>
                {
                    Window mainWindow = new MainWindow();
                    mainWindow.Show();
                    this.Close();
                });
            });

            // 为进度条添加动画效果
            DoubleAnimation animation = new()
            {
                From = 0,
                To = 100,
                Duration = new Duration(TimeSpan.FromSeconds(5))
            };
            progressBar.BeginAnimation(ProgressBar.ValueProperty, animation);

            DisplayRandomImage();
        }

        //随机启动图
        private bool DisplayRandomImage()
        {
            //设置图片文件夹的路径
            string folderPath = System.IO.Path.Combine(Environment.CurrentDirectory, "Data", "Image", "StartPic");

            //从文件夹中读取所有图片文件
            string[] files = Directory.GetFiles(folderPath, "*.jpg", SearchOption.TopDirectoryOnly);

            //如果没有找到图片文件，返回
            if (files.Length == 0)
            {
                return false;
            }

            // 生成一个随机数
            Random random = new Random();
            int randomIndex = random.Next(files.Length);

            //将随机选择的图片显示为背景
            ImageBrush imageBrush = new()
            {
                ImageSource = new BitmapImage(new Uri(files[randomIndex], UriKind.Absolute))
            };
            Basemap.Background = imageBrush;
            return true;
        }

        //窗体启动动画
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            DoubleAnimation animation = new()
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromSeconds(1)
            };
            this.BeginAnimation(UIElement.OpacityProperty, animation);
        }
        //鼠标按下
        private void Window_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                //获取当前鼠标按下的位置
                startPoint = e.GetPosition(this);

                //设置拖动标记为 true
                isDragging = true;
            }
        }
        //鼠标移动
        private void Window_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (isDragging)
            {
                //获取窗体当前的位置
                double left = this.Left;
                double top = this.Top;

                //获取鼠标在窗体内的移动量，并计算出窗体应该移动到的位置
                Point currentPoint = e.GetPosition(this);
                double newLeft = left + currentPoint.X - startPoint.X;
                double newTop = top + currentPoint.Y - startPoint.Y;

                //设置窗体的位置
                this.Left = newLeft;
                this.Top = newTop;
            }
        }
        //松开鼠标
        private void Window_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                //重置拖动标记为 false
                isDragging = false;
            }
        }
    }
}
