using System;
using System.IO;
using System.Windows.Media.Imaging;

namespace AnyCAD.WPF
{
    /// <summary>
    /// 定位仓库根目录下的 showcase/ 截图目录，并按
    /// "&lt;group&gt;.&lt;name&gt;.png"（全小写）规则查找用例截图。
    /// 与 scripts/ShowcaseRunner/Program.cs 的 NormalizeCaseKey 规则保持一致。
    /// </summary>
    internal static class ShowcaseLocator
    {
        static string? _showcaseDir;
        static bool _resolved;

        /// <summary>showcase 目录的绝对路径；找不到时返回 null。</summary>
        public static string? ShowcaseDir
        {
            get
            {
                if (!_resolved)
                {
                    _resolved = true;
                    _showcaseDir = ResolveShowcaseDir();
                }
                return _showcaseDir;
            }
        }

        /// <summary>
        /// 根据 group 和 name 加载缩略图；无截图或加载失败时返回 null。
        /// 按目标宽度解码，避免把 1MB+ 的原图整张贴进内存。
        /// </summary>
        public static BitmapImage? TryLoadThumbnail(string group, string name, int decodePixelWidth = 360)
        {
            var dir = ShowcaseDir;
            if (dir == null) return null;
            if (string.IsNullOrEmpty(group) || string.IsNullOrEmpty(name)) return null;

            string fileName = group.ToLowerInvariant() + "." + name.ToLowerInvariant() + ".png";
            string full = Path.Combine(dir, fileName);
            if (!File.Exists(full)) return null;

            try
            {
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.CacheOption = BitmapCacheOption.OnLoad;   // 立即解码并释放文件句柄
                bmp.UriSource = new Uri(full, UriKind.Absolute);
                bmp.DecodePixelWidth = decodePixelWidth;       // 缩略图尺寸
                bmp.EndInit();
                bmp.Freeze();                                  // 跨线程安全
                return bmp;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 从 exe 所在目录向上逐级探测，找到包含 showcase/welcome.anycad.png 的目录。
        /// 兼容从 bin/Debug/net8.0-windows、bin/Release/net8.0-windows 等位置启动。
        /// </summary>
        static string? ResolveShowcaseDir()
        {
            string cur = Path.GetFullPath(AppContext.BaseDirectory);
            for (int i = 0; i < 8; i++)
            {
                string candidate = Path.Combine(cur, "showcase");
                if (Directory.Exists(candidate) &&
                    File.Exists(Path.Combine(candidate, "welcome.anycad.png")))
                {
                    return candidate;
                }
                var parent = Directory.GetParent(cur);
                if (parent == null) break;
                cur = parent.FullName;
            }
            return null;
        }
    }
}
