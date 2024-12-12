using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Animation;
using System.Windows.Media;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;

namespace GeoChemistryNexus.Models
{
    public class Window3DFlipTransition
    {
        private Window _currentWindow;
        private Window _nextWindow;
        private Window _containerWindow;
        private Viewport3D _viewport;
        private AxisAngleRotation3D _rotation;

        public double TransitionDuration { get; set; } = 0.5;

        public Window3DFlipTransition(Window currentWindow, Window nextWindow)
        {
            _currentWindow = currentWindow;
            _nextWindow = nextWindow;
            SetupContainer();
        }

        private void SetupContainer()
        {
            // 创建容器窗口
            _containerWindow = new Window
            {
                Width = _currentWindow.Width,
                Height = _currentWindow.Height,
                WindowStyle = WindowStyle.None,
                AllowsTransparency = true,
                Background = null,
                Left = _currentWindow.Left,
                Top = _currentWindow.Top,
                ShowInTaskbar = false,
                ResizeMode = ResizeMode.NoResize
            };

            // 基本3D视口设置
            _viewport = new Viewport3D();
            _containerWindow.Content = _viewport;

            // 简单相机设置
            _viewport.Camera = new PerspectiveCamera
            {
                Position = new Point3D(0, 0, 2),
                LookDirection = new Vector3D(0, 0, -1),
                UpDirection = new Vector3D(0, 1, 0)
            };

            // 创建两面
            CreateFrontAndBackSides();
        }

        private void CreateFrontAndBackSides()
        {
            // 前面（当前窗口）
            var frontVisual = CreateSide(_currentWindow);
            _viewport.Children.Add(frontVisual);

            // 背面（下一个窗口）
            var backVisual = CreateSide(_nextWindow);
            backVisual.Transform = new RotateTransform3D(
                new AxisAngleRotation3D(new Vector3D(0, 1, 0), 180));
            _viewport.Children.Add(backVisual);

            // 设置旋转
            _rotation = new AxisAngleRotation3D(new Vector3D(0, 1, 0), 0);
            var rotateTransform = new RotateTransform3D(_rotation);
            var root = new ModelVisual3D { Transform = rotateTransform };
            _viewport.Children.Add(root);
        }

        private ModelVisual3D CreateSide(Window window)
        {
            // 创建简单平面
            var mesh = new MeshGeometry3D();
            mesh.Positions = new Point3DCollection(new[]
            {
            new Point3D(-1, -1, 0),
            new Point3D(1, -1, 0),
            new Point3D(1, 1, 0),
            new Point3D(-1, 1, 0)
        });

            mesh.TriangleIndices = new Int32Collection(new[] { 0, 1, 2, 0, 2, 3 });
            mesh.TextureCoordinates = new PointCollection(new[]
            {
            new Point(0, 1),
            new Point(1, 1),
            new Point(1, 0),
            new Point(0, 0)
        });

            // 捕获窗口内容
            var brush = new VisualBrush(window);
            var material = new DiffuseMaterial(brush);

            return new ModelVisual3D
            {
                Content = new GeometryModel3D(mesh, material)
            };
        }

        public void StartFlipTransition()
        {
            _currentWindow.Hide();
            _containerWindow.Show();

            var animation = new DoubleAnimation
            {
                From = 0,
                To = 180,
                Duration = TimeSpan.FromSeconds(TransitionDuration)
            };

            animation.Completed += (s, e) =>
            {
                _containerWindow.Close();
                _nextWindow.Show();
            };

            _rotation.BeginAnimation(AxisAngleRotation3D.AngleProperty, animation);
        }
    }
}
