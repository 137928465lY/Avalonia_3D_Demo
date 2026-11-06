using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace Bench
{
    // 直接 P/Invoke OpenCASCADE（TKDESTEP / TKXSBase / TKMesh / TKDESTL），不依赖 AnyCAD。
    // 需要精确的 MSVC C++ mangled 导出名与 x64 ABI（this 指针 + sret 隐藏返回缓冲）。
    static unsafe class OcctNative
    {
        const string NATIVE = @"d:\科能慧智\RAE逆向\StepViewer\bin\Debug\net8.0\runtimes\win-x64\native";

        static string P(string dll) => Path.Combine(NATIVE, dll);

        [DllImport("kernel32.dll", CharSet = CharSet.Ansi, SetLastError = true)]
        static extern bool SetDllDirectoryA(string path);

        // STEPControl_Reader 构造 / 析构
        [DllImport("__DESTEP__", EntryPoint = "??0STEPControl_Reader@@QEAA@XZ", CallingConvention = CallingConvention.Cdecl)]
        static extern void StepReaderCtor(IntPtr self);

        // XSControl_Reader::ReadFile(const char*) -> IFSelect_ReturnStatus
        [DllImport("__XS__", EntryPoint = "?ReadFile@XSControl_Reader@@UEAA?AW4IFSelect_ReturnStatus@@QEBD@Z", CallingConvention = CallingConvention.Cdecl)]
        static extern int ReadFile(IntPtr self, IntPtr fileName);

        // XSControl_Reader::TransferRoots(const Message_ProgressRange&) -> int
        [DllImport("__XS__", EntryPoint = "?TransferRoots@XSControl_Reader@@QEAAHAEBVMessage_ProgressRange@@@Z", CallingConvention = CallingConvention.Cdecl)]
        static extern int TransferRoots(IntPtr self, IntPtr progress);

        // XSControl_Reader::OneShape() 返回 TopoDS_Shape（24B，走 sret：RCX=返回缓冲, RDX=this）
        [DllImport("__XS__", EntryPoint = "?OneShape@XSControl_Reader@@QEBA?AVTopoDS_Shape@@XZ", CallingConvention = CallingConvention.Cdecl)]
        static extern void OneShape(IntPtr retBuf, IntPtr self);

        // XSControl_Reader::NbShapes()
        [DllImport("__XS__", EntryPoint = "?NbShapes@XSControl_Reader@@QEBAHXZ", CallingConvention = CallingConvention.Cdecl)]
        static extern int NbShapes(IntPtr self);

        // XSControl_Reader::WS() 返回 Handle(XSControl_WorkSession)（8B，指针返回）
        [DllImport("__XS__", EntryPoint = "?WS@XSControl_Reader@@QEBA?AV?$handle@VXSControl_WorkSession@@@opencascade@@XZ", CallingConvention = CallingConvention.Cdecl)]
        static extern IntPtr GetWorkSession(IntPtr self);

        // BRepMesh_IncrementalMesh(const TopoDS_Shape&, double, bool, double, bool) —— 构造即执行
        [DllImport("__MESH__", EntryPoint = "??0BRepMesh_IncrementalMesh@@QEAA@AEBVTopoDS_Shape@@N_NN1@Z", CallingConvention = CallingConvention.Cdecl)]
        static extern void MeshCtor(IntPtr self, IntPtr shape, double deflection, [MarshalAs(UnmanagedType.U1)] bool relative, double angle, [MarshalAs(UnmanagedType.U1)] bool parallel);

        // StlAPI_Writer 构造 / Write(const TopoDS_Shape&, const char*, const Message_ProgressRange&)
        [DllImport("__STL__", EntryPoint = "??0StlAPI_Writer@@QEAA@XZ", CallingConvention = CallingConvention.Cdecl)]
        static extern void StlWriterCtor(IntPtr self);

        [DllImport("__STL__", EntryPoint = "?Write@StlAPI_Writer@@QEAA_NAEBVTopoDS_Shape@@QEBDAEBVMessage_ProgressRange@@@Z", CallingConvention = CallingConvention.Cdecl)]
        static extern bool StlWrite(IntPtr self, IntPtr shape, IntPtr fileName, IntPtr progress);

        // TopExp_Explorer(const TopoDS_Shape&, TopAbs_ShapeEnum, TopAbs_ShapeEnum)
        [DllImport("__BREP__", EntryPoint = "??0TopExp_Explorer@@QEAA@AEBVTopoDS_Shape@@W4TopAbs_ShapeEnum@@1@Z", CallingConvention = CallingConvention.Cdecl)]
        static extern void ExplorerCtor(IntPtr self, IntPtr shape, int toFind, int toAvoid);

        // 注意：TopExp_Explorer::More() 是 inline，无导出符号；
        // 用 Current() 返回的 TopoDS_Shape 引用判空（TShape 指针 == 0 即结束），语义等价
        [DllImport("__BREP__", EntryPoint = "?Next@TopExp_Explorer@@QEAAXXZ", CallingConvention = CallingConvention.Cdecl)]
        static extern void ExplorerNext(IntPtr self);

        // TopExp_Explorer::Current() 返回 const TopoDS_Shape&（8B 指针返回，RAX）
        [DllImport("__BREP__", EntryPoint = "?Current@TopExp_Explorer@@QEBAAEBVTopoDS_Shape@@XZ", CallingConvention = CallingConvention.Cdecl)]
        static extern IntPtr ExplorerCurrent(IntPtr self);

        public static void Run(string file, double deflection = 0.5, double angle = 0.5, bool parallel = true)
        {
            SetDllDirectoryA(NATIVE);
            NativeLibrary.SetDllImportResolver(typeof(OcctNative).Assembly, Resolve);
            Console.WriteLine($"== OCCT 原生 P/Invoke  {Path.GetFileName(file)}");
            Console.WriteLine($"  猜测: TopoDS_Shape=24B, STEPControl_Reader 对象缓冲=1024B");

            IntPtr pReader = Marshal.AllocHGlobal(1024);
            IntPtr pShape = Marshal.AllocHGlobal(64);
            IntPtr pProg = Marshal.AllocHGlobal(64);
            byte* pb = (byte*)pShape;
            for (int i = 0; i < 64; i++) pb[i] = 0;
            byte* pp = (byte*)pProg;
            for (int i = 0; i < 64; i++) pp[i] = 0;
            byte* pr = (byte*)pReader;
            for (int i = 0; i < 1024; i++) pr[i] = 0;

            var sw = Stopwatch.StartNew();
            try
            {
                StepReaderCtor(pReader);
                ulong* dump = (ulong*)pReader;
                Console.Write("  对象[0..11]=");
                for (int i = 0; i < 12; i++) Console.Write($"0x{dump[i]:X} ");
                Console.WriteLine();
                var modBase = new System.Collections.Generic.List<(ulong lo, ulong hi)> { (0x7FF000000000UL, 0x800000000000UL) };
                for (int i = 0; i < 12; i++)
                {
                    ulong v = dump[i];
                    if (v == 0) continue;
                    bool isModule = v >= 0x7FF000000000UL && v < 0x800000000000UL;
                    Console.WriteLine($"    [{i}] 0x{v:X}  {(isModule ? "模块/vtable区" : "堆指针?")}");
                }
            }
            catch (Exception ex) { Console.WriteLine("  StepReaderCtor 失败: " + ex.GetType().Name + ": " + ex.Message); return; }

            IntPtr pFile = Marshal.StringToHGlobalAnsi(file);
            try
            {
                int st = ReadFile(pReader, pFile);
                Console.WriteLine($"  ReadFile -> {st}  ({sw.ElapsedMilliseconds} ms)");
                if (st != 1 && st != 0) Console.WriteLine("  (1=RetDone, 0=RetVoid, 2=RetFail)");
            }
            catch (Exception ex) { Console.WriteLine("  ReadFile 失败: " + ex.GetType().Name + ": " + ex.Message); return; }
            finally { Marshal.FreeHGlobal(pFile); }

            long tRead = sw.ElapsedMilliseconds;

            try
            {
                int n = TransferRoots(pReader, pProg);
                int ns = NbShapes(pReader);
                Console.WriteLine($"  TransferRoots -> {n}, NbShapes={ns}  ({sw.ElapsedMilliseconds - tRead} ms)");
            }
            catch (Exception ex) { Console.WriteLine("  TransferRoots 失败: " + ex.GetType().Name + ": " + ex.Message); return; }

            try
            {
                OneShape(pShape, pReader);
                ulong tshape = *(ulong*)pShape;
                ulong loc = *(ulong*)(pShape + 8);
                int orient = *(int*)(pShape + 16);
                Console.WriteLine($"  OneShape: TShape=0x{tshape:X} Loc=0x{loc:X} Orient={orient}");
                if (tshape == 0) { Console.WriteLine("  shape 为空，终止"); return; }
            }
            catch (Exception ex) { Console.WriteLine("  OneShape 失败: " + ex.GetType().Name + ": " + ex.Message); return; }

            long tParse = sw.ElapsedMilliseconds;

            // 面数（TopoDS_Face 枚举值 = 4）
            int faceCount = CountSubShapes(pShape, 4);
            Console.WriteLine($"  TopExp_Explorer FACE 计数={faceCount}");

            sw.Restart();
            IntPtr pMesh = Marshal.AllocHGlobal(1024);
            try
            {
                MeshCtor(pMesh, pShape, deflection, false, angle, parallel);
                Console.WriteLine($"  BRepMesh_IncrementalMesh 完成 ({sw.ElapsedMilliseconds} ms)");
            }
            catch (Exception ex) { Console.WriteLine("  BRepMesh 失败: " + ex.GetType().Name + ": " + ex.Message); return; }

            // STL 导出验证三角化确实产生数据
            string stl = Path.Combine(Path.GetTempPath(), "occt_spike.stl");
            if (File.Exists(stl)) File.Delete(stl);
            IntPtr pWriter = Marshal.AllocHGlobal(256);
            try
            {
                StlWriterCtor(pWriter);
                IntPtr pStl = Marshal.StringToHGlobalAnsi(stl);
                bool ok = StlWrite(pWriter, pShape, pStl, pProg);
                Marshal.FreeHGlobal(pStl);
                long size = File.Exists(stl) ? new FileInfo(stl).Length : 0;
                Console.WriteLine($"  StlAPI_Writer.Write -> {ok}, 文件 {size:N0} 字节, 三角形≈{size / 50:N0}");
            }
            catch (Exception ex) { Console.WriteLine("  STL 导出失败: " + ex.GetType().Name + ": " + ex.Message); }

            Console.WriteLine($"  —— 解析 {tParse} ms, 三角化+导出 {sw.ElapsedMilliseconds} ms");
        }

        static int CountSubShapes(IntPtr pShape, int type)
        {
            IntPtr pExp = Marshal.AllocHGlobal(256);
            int n = 0;
            try
            {
                ExplorerCtor(pExp, pShape, type, 0);
                while (true)
                {
                    IntPtr cur = ExplorerCurrent(pExp);
                    if (cur == IntPtr.Zero || *(ulong*)cur == 0) break;
                    n++;
                    ExplorerNext(pExp);
                }
            }
            catch (Exception ex) { Console.WriteLine("    枚举失败: " + ex.Message); }
            finally { Marshal.FreeHGlobal(pExp); }
            return n;
        }

        static IntPtr Resolve(string name, System.Reflection.Assembly asm, DllImportSearchPath? path)
        {
            string map = name switch
            {
                "__DESTEP__" => "TKDESTEP.dll",
                "__XS__" => "TKXSBase.dll",
                "__MESH__" => "TKMesh.dll",
                "__STL__" => "TKDESTL.dll",
                "__BREP__" => "TKBRep.dll",
                _ => null
            };
            if (map == null) return IntPtr.Zero;
            var full = Path.Combine(NATIVE, map);
            return File.Exists(full) ? NativeLibrary.Load(full) : IntPtr.Zero;
        }
    }
}
