using Avalonia;
using System;

namespace StepViewer
{
    internal class Program
    {
        // 调用链（REA 逆向结论）：
        //   AnyCAD.Foundation.GlobalInstance.Initialize()
        //     → SWIG P/Invoke → 原生 AnyCAD.dll → 初始化渲染/几何内核(OCCT)
        //   AnyCAD.Foundation.ExchangeInstance.Initialize()
        //     → 注册 STEP/IGES/BREP 等交换模块（ShapeIO 依赖此初始化）
        [STAThread]
        public static void Main(string[] args)
        {
            AnyCAD.Foundation.GlobalInstance.Initialize();
            AnyCAD.Foundation.ExchangeInstance.Initialize();

            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

            AnyCAD.Foundation.GlobalInstance.Destroy();
        }

        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .LogToTrace();
    }
}
