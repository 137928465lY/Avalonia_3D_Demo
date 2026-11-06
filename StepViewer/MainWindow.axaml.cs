using AnyCAD.Foundation;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using System;
using System.IO;
using IOPath = System.IO.Path;
using System.Linq;

namespace StepViewer
{
    public partial class MainWindow : Window
    {
        private SceneNode mShapeNode;
        private TopoShape mCurrentShape;
        private bool mWireFrame;

        // REA 逆向确认的导入/显示调用链：
        //   ShapeIO.Open(path)                     [AnyCAD.Exchange.NET.dll, SWIG P/Invoke → 原生 AnyCAD.dll → OCCT TKDESTEP]
        //   BrepSceneNode.Create(shape, mat, null) [AnyCAD.Foundation.NET.dll → 内核 tessellation → 场景节点]
        //   RenderControl.ShowSceneNode(node)      [AnyCAD.Avalonia.NET.dll → 原生 GL 渲染]
        //   RenderControl.ZoomAll(1.1)             [相机适配包围盒]
        public MainWindow()
        {
            InitializeComponent();

            // 启动后加载示例 STEP，验证导入链路
            this.Opened += OnWindowOpened;
        }

        private async void OnWindowOpened(object sender, EventArgs e)
        {
            var demo = IOPath.Combine(
            @"d:\科能慧智\RAE逆向\anycad.rapid.net.sample-master", "data", "ST038.stp");
            if (File.Exists(demo))
                LoadStep(demo);
        }

        private async void OnOpenStep(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "打开 STEP 模型",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("STEP/IGES/BREP/STL")
                    {
                        Patterns = new[] { "*.stp", "*.step", "*.igs", "*.iges", "*.brep", "*.stl" }
                    }
                }
            });

            if (files.Count == 0)
                return;
            LoadStep(files[0].Path.LocalPath);
        }

        private void LoadStep(string fileName)
        {
            try
            {
                var shape = ShapeIO.Open(fileName);
                if (shape == null)
                {
                    StatusText.Text = "导入失败: " + fileName;
                    return;
                }

                mCurrentShape = shape;
                RenderView.ClearScene();
                mShapeNode = BrepSceneNode.Create(shape, null, null);
                RenderView.ShowSceneNode(mShapeNode);
                RenderView.ZoomAll(1.1f);
                RenderView.RequestDraw(EnumUpdateFlags.Scene);

                var solids = shape.GetChildren(EnumTopoShapeType.Topo_SOLID).Count;
                var faces = shape.GetChildren(EnumTopoShapeType.Topo_FACE).Count;
                StatusText.Text = $"{IOPath.GetFileName(fileName)}  实体:{solids} 面:{faces}";
            }
            catch (Exception ex)
            {
                StatusText.Text = "错误: " + ex.Message;
            }
        }

        private void OnZoomAll(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            RenderView.ZoomAll(1.1f);
            RenderView.RequestDraw(EnumUpdateFlags.Scene);
        }

        private void OnToggleWire(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (mCurrentShape == null)
                return;
            mWireFrame = !mWireFrame;
            RenderView.ClearScene();
            var material = LineMaterial.Create("wire");
            material.SetColor(ColorTable.DimGray);
            mShapeNode = BrepSceneNode.Create(mCurrentShape, mWireFrame ? null : material, mWireFrame ? material : null);
            RenderView.ShowSceneNode(mShapeNode);
            RenderView.ZoomAll(1.1f);
            RenderView.RequestDraw(EnumUpdateFlags.Scene);
        }
    }
}
