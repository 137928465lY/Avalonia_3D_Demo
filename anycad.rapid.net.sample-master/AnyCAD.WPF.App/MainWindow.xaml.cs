using AnyCAD.Foundation;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;

namespace AnyCAD.WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        public MainWindow()
        {
            InitializeComponent();
            var vm = new MainViewModel(this.mRenderCtrl);
            this.DataContext = vm;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
           
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            if (this.WindowState == WindowState.Maximized || this.WindowState == WindowState.Normal)
                mRenderCtrl.ForceUpdate();
        }

        private void SampleGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is not ListBox listBox) return;
            if (listBox.SelectedItem is not SampleInfo sample) return;
            try
            {
                Demo.TestCase.CreateTest(sample.SampleType, this.mRenderCtrl);

                // 新 case 加载完成后：缩放到适配全部场景，并强制刷新渲染
                mRenderCtrl.ZoomAll();
                mRenderCtrl.ForceUpdate();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Run sample {sample.Name} failed: {ex.Message}");
            }
        }

        /// <summary>清空搜索框：Clear() 触发绑定更新（SearchText→ApplyFilter 恢复完整列表），并回焦以便继续输入。</summary>
        private void ClearSearch_Click(object sender, RoutedEventArgs e)
        {
            SearchBox.Clear();
            SearchBox.Focus();
        }

        private void RenderCtrl_ViewerReady()
        {
            var vm = this.DataContext as MainViewModel;
            if (vm == null)
                throw new Exception("DataContext is null!");

            CoordinateWidgetSetting setting = new CoordinateWidgetSetting(this.mRenderCtrl.Viewer);
            setting.SetCoordinateWidget(EnumViewCoordinateType.AxisAndSphere);
            vm.ViewReady();

            SyncPerspectiveToggle();
        }

        /// <summary>按当前相机投影类型同步透视切换按钮的选中态。IsChecked=true 表示透视模式。</summary>
        private void SyncPerspectiveToggle()
        {
            try
            {
                var cam = mRenderCtrl.Camera;
                if (cam == null) return;
                PerspectiveToggle.IsChecked = cam.GetProjectionType() == EnumProjectionType.Perspective;
            }
            catch { /* 相机未就绪时忽略 */ }
        }

        /// <summary>透视/正交切换：走 IRenderView.SwitchProjectionType，再同步按钮态。不参与交互模式互斥。</summary>
        private void ProjectionToggle_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                mRenderCtrl.SwitchProjectionType();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SwitchProjectionType failed: {ex.Message}");
                return;
            }
            SyncPerspectiveToggle();
        }


        private void VisitHelpWebsite(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "http://www.anycad.cn",
                UseShellExecute = true,
            });
        }

        /// <summary>
        /// 浮动工具条命令：命令名放在按钮的 Tag 上，经 RenderControl（IRenderView）执行。
        /// 模式类命令（ToggleButton）在工具条内互斥选中：点亮的那个表示当前交互模式。
        /// </summary>
        private void ToolbarCommand_Click(object sender, RoutedEventArgs e)
        {
            var commandName = (sender as FrameworkElement)?.Tag as string;
            if (string.IsNullOrEmpty(commandName))
                return;

            mRenderCtrl.ExecuteCommand(commandName, new AnyCAD.Foundation.ParameterDict());

            if (sender is System.Windows.Controls.Primitives.ToggleButton clicked)
            {
                // 互斥：同一 StackPanel 里其余模式按钮取消选中
                if (clicked.Parent is StackPanel panel)
                {
                    foreach (var child in panel.Children)
                    {
                        if (child is System.Windows.Controls.Primitives.ToggleButton tb && !ReferenceEquals(tb, clicked))
                            tb.IsChecked = false;
                    }
                }
                clicked.IsChecked = true;
            }
        }

    }
}
