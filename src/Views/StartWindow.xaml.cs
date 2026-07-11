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
            InitializeComponent();
            UiScaleHelper.Attach(this);
            DisplayRandomImage();
        }

        //随机启动图
        private bool DisplayRandomImage()
        {
            string[] files = StartPicHelper.GetImageFiles();

            //如果没有找到图片文件，返回
            if (files.Length == 0)
            {
                return false;
            }

            // 生成一个随机数
            Random random = new Random();
            int randomIndex = random.Next(files.Length);

            //将随机选择的图片显示为背景（内存加载，避免锁定启动图文件）
            var bitmap = StartPicHelper.LoadBitmapWithoutFileLock(files[randomIndex]);
            if (bitmap == null)
                return false;

            Basemap.Background = new ImageBrush { ImageSource = bitmap };
            return true;
        }

        //窗体启动动画
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // 移除启动渐入动画
            // DoubleAnimation animation = new()
            // {
            //     From = 0,
            //     To = 1,
            //     Duration = TimeSpan.FromSeconds(1)
            // };
            // this.BeginAnimation(UIElement.OpacityProperty, animation);
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
