using Avalonia;
using System;

namespace OpenStepViewer
{
    internal class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            // 无头模式：--stats <file>  /  --mesh <file>  用于验证解析与网格化
            if (args.Length >= 2 && args[0] == "--axes")
            {
                Headless.Axes(args[1]);
                return;
            }
            if (args.Length >= 2 && args[0] == "--types")
            {
                Headless.Types(args[1]);
                return;
            }
            if (args.Length >= 3 && args[0] == "--dump")
            {
                Headless.Dump(args[1], int.Parse(args[2]));
                return;
            }
            if (args.Length >= 2 && (args[0] == "--stats" || args[0] == "--mesh"))
            {
                Headless.Run(args[0] == "--mesh", args[1]);
                return;
            }

            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }

        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .LogToTrace();
    }
}
