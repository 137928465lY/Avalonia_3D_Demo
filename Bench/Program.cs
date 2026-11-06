using System;
using System.Diagnostics;
using System.Linq;
using AnyCAD.Foundation;

namespace Bench
{
    static class Program
    {
        static void Main(string[] args)
        {
            if (args.Length > 0 && args[0] == "--api")
            {
                DumpApi(args.Length > 1 ? args[1] : null);
                return;
            }
            if (args.Length > 0 && args[0] == "--anycad")
            {
                RunAnyCad(args.Length > 1 ? args[1] : @"d:\科能慧智\RAE逆向\StepViewer\Assets\ABB机器人_IRB 1200-7-0.7.step",
                          args.Length > 2 ? double.Parse(args[2]) : 0.5);
                return;
            }
            if (args.Length > 0 && args[0] == "--filters")
            {
                GlobalInstance.Initialize();
                ExchangeInstance.Initialize();
                var r = ShapeIO.ReaderFilters();
                var w = ShapeIO.WriterFilters();
                Console.WriteLine("Reader: " + r);
                Console.WriteLine("Writer: " + w);
                return;
            }
            if (args.Length > 0 && args[0] == "--export")
            {
                GlobalInstance.Initialize();
                ExchangeInstance.Initialize();
                string src = args[1];
                var shape = ShapeIO.Open(src);
                if (shape == null) { Console.WriteLine("Open 失败"); return; }
                foreach (var ext in new[] { "stl", "obj", "brep", "glb" })
                {
                    string outp = System.IO.Path.Combine(@"C:\osvbench", "export_test." + ext);
                    var sw = Stopwatch.StartNew();
                    bool ok = false;
                    try { ok = ShapeIO.Save(shape, outp); }
                    catch (Exception ex) { Console.WriteLine($"  {ext}: 异常 {ex.Message}"); continue; }
                    long size = System.IO.File.Exists(outp) ? new System.IO.FileInfo(outp).Length : 0;
                    Console.WriteLine($"  {ext}: ok={ok} size={size:N0}B {sw.ElapsedMilliseconds}ms");
                }
                return;
            }
            if (args.Length > 0 && args[0] == "--occt")
            {
                OcctNative.Run(args.Length > 1 ? args[1] : @"C:\osvbench\1.stp",
                               args.Length > 2 ? double.Parse(args[2]) : 0.5);
                return;
            }
            Console.WriteLine("用法: Bench --anycad <file> [deflection] | --occt <file> [deflection] | --api [filter]");
        }

        static void RunAnyCad(string file, double deflection)
        {
            Console.WriteLine($"== AnyCAD (OCCT 后端)  {System.IO.Path.GetFileName(file)}  deflection={deflection}");
            Console.WriteLine($"  文件存在={System.IO.File.Exists(file)}");
            try
            {
                bool ok = GlobalInstance.Initialize();
                bool ok2 = ExchangeInstance.Initialize();
                Console.WriteLine($"  初始化 Global={ok} Exchange={ok2}");
            }
            catch (Exception ex) { Console.WriteLine("  初始化异常: " + ex.Message); }
            try
            {
                var rf = ShapeIO.ReaderFilters();
                Console.WriteLine($"  ReaderFilters 数量={rf?.Count}");
            }
            catch (Exception ex) { Console.WriteLine("  ReaderFilters 异常: " + ex.Message); }

            var sw = Stopwatch.StartNew();
            TopoShape shape = null;
            try { shape = ShapeIO.Open(file); }
            catch (Exception ex) { Console.WriteLine("Open 失败: " + ex.Message); return; }
            sw.Stop();
            long openMs = sw.ElapsedMilliseconds;
            if (shape == null || shape.IsNullShape()) { Console.WriteLine("Open 返回空"); return; }

            int solids = shape.GetChildren(EnumTopoShapeType.Topo_SOLID).Count;
            int faces = shape.GetChildren(EnumTopoShapeType.Topo_FACE).Count;
            int edges = shape.GetChildren(EnumTopoShapeType.Topo_EDGE).Count;
            Console.WriteLine($"  实体={solids} 面={faces} 边={edges}");

            sw.Restart();
            BrepSceneNode node = null;
            try { node = BrepSceneNode.Create(shape, deflection); }
            catch (Exception ex) { Console.WriteLine("Tessellate 失败: " + ex.GetType().Name + ": " + ex.Message); }
            sw.Stop();
            long meshMs = sw.ElapsedMilliseconds;
            Console.WriteLine($"  解析: {openMs} ms   网格化: {meshMs} ms   合计: {openMs + meshMs} ms");

            if (node != null)
            {
                try
                {
                    uint fc = node.GetFaceCount();
                    uint tc = GetTriCount(node);
                    Console.WriteLine($"  渲染面数={fc} 三角形={tc}");
                }
                catch (Exception ex) { Console.WriteLine("  统计失败: " + ex.Message); }
            }

            // 第二次调用验证增量缓存
            sw.Restart();
            var node2 = BrepSceneNode.Create(shape, deflection);
            sw.Stop();
            Console.WriteLine($"  二次网格化(缓存): {sw.ElapsedMilliseconds} ms");
            GC.KeepAlive(node2);
        }

        static uint GetTriCount(BrepSceneNode node)
        {
            // 尝试常见入口：GRepShape 上的三角化统计
            var m = node.GetType().GetMethods().FirstOrDefault(x => x.Name.IndexOf("Triangle", StringComparison.OrdinalIgnoreCase) >= 0);
            if (m != null && m.GetParameters().Length == 0)
                return Convert.ToUInt32(m.Invoke(node, null));
            var gs = node.GetShape();
            if (gs != null)
            {
                var m2 = gs.GetType().GetMethods().FirstOrDefault(x => x.Name.IndexOf("Triangle", StringComparison.OrdinalIgnoreCase) >= 0);
                if (m2 != null && m2.GetParameters().Length == 0)
                    return Convert.ToUInt32(m2.Invoke(gs, null));
            }
            return 0;
        }

        static void DumpApi(string filter)
        {
            var asms = new[] { "AnyCAD.Foundation.NET", "AnyCAD.Exchange.NET" };
            foreach (var name in asms)
            {
                System.Reflection.Assembly a;
                try { a = System.Reflection.Assembly.Load(name); }
                catch (Exception ex) { Console.WriteLine($"[{name}] 加载失败: {ex.Message}"); continue; }
                Console.WriteLine($"===== {a.GetName().Name} {a.GetName().Version}");
                foreach (var t in a.GetTypes().Where(t => t.IsPublic).OrderBy(t => t.FullName))
                {
                    if (filter != null && t.Name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0) continue;
                    Console.WriteLine($"--- {t.FullName}");
                    foreach (var m in t.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.DeclaredOnly))
                    {
                        var ps = string.Join(", ", m.GetParameters().Select(p => p.ParameterType.Name + " " + p.Name));
                        Console.WriteLine($"    {m.ReturnType.Name} {m.Name}({ps})");
                    }
                    foreach (var p in t.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.DeclaredOnly))
                        Console.WriteLine($"    prop {p.PropertyType.Name} {p.Name}");
                }
            }
        }
    }
}
