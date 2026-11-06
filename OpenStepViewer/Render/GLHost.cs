using System;
using System.Runtime.InteropServices;
using Avalonia.Controls;
using Avalonia.Platform;
using Avalonia.Threading;
using V3 = System.Numerics.Vector3;
using M4 = System.Numerics.Matrix4x4;

namespace OpenStepViewer.Render
{
    // Win32 子窗口 + WGL 上下文 + OpenGL 固定管线（VBO）。纯 P/Invoke，无第三方渲染库。
    public unsafe class GLRenderHost : NativeControlHost
    {
        const uint WS_CHILD = 0x40000000, WS_VISIBLE = 0x10000000, WS_CLIPSIBLINGS = 0x04000000;
        const int CS_OWNDC = 0x20;
        const uint PFD_DRAW_TO_WINDOW = 0x04, PFD_SUPPORT_OPENGL = 0x20, PFD_DOUBLEBUFFER = 0x01;

        const uint GL_DEPTH_BUFFER_BIT = 0x100, GL_COLOR_BUFFER_BIT = 0x4000;
        const uint GL_DEPTH_TEST = 0x0B71, GL_LIGHTING = 0x0B50, GL_LIGHT0 = 0x4000, GL_NORMALIZE = 0x0BA1,
                   GL_COLOR_MATERIAL = 0x0B57, GL_CULL_FACE = 0x0B44, GL_BACK = 0x0405, GL_FRONT_AND_BACK = 0x0408,
                   GL_FILL = 0x1B02, GL_LINE = 0x1B03, GL_PROJECTION = 0x1701, GL_MODELVIEW = 0x1700,
                   GL_VERTEX_ARRAY = 0x8074, GL_NORMAL_ARRAY = 0x8075, GL_ARRAY_BUFFER = 0x8892,
                   GL_ELEMENT_ARRAY_BUFFER = 0x8893, GL_STATIC_DRAW = 0x88E4, GL_FLOAT = 0x1406,
                   GL_UNSIGNED_INT = 0x1405, GL_TRIANGLES = 0x0004, GL_POSITION = 0x1203,
                   GL_DIFFUSE = 0x1201, GL_AMBIENT = 0x1200, GL_AMBIENT_AND_DIFFUSE = 0x1602,
                   GL_POLYGON_OFFSET_FILL = 0x8037, GL_BLEND = 0x0BE2;

        IntPtr _hwnd, _hdc, _hglrc;
        DispatcherTimer _timer;
        uint _vboP, _vboN, _vboI;
        int _indexCount;
        bool _needsUpload, _ready, _wire;
        bool _hasMesh;
        bool _useVbo = true;
        float[] _pendingPos, _pendingNor; int[] _pendingIdx;

        double _yaw = 45 * Math.PI / 180, _pitch = 30 * Math.PI / 180, _dist = 1000;
        V3 _target;
        int _selStart = -1, _selCount = 0;
        bool _dirty = true;
        static V3 Up = new V3(0, 0, 1);

        // ---- Win32 / GL 导入 ----
        [DllImport("user32.dll", CharSet = CharSet.Ansi)] static extern IntPtr CreateWindowEx(uint ex, string cls, string name, uint style, int x, int y, int w, int h, IntPtr parent, IntPtr menu, IntPtr inst, IntPtr param);
        [DllImport("user32.dll")] static extern bool DestroyWindow(IntPtr h);
        [DllImport("user32.dll")] static extern bool GetClientRect(IntPtr h, out RECT r);
        [DllImport("user32.dll")] static extern IntPtr GetDC(IntPtr h);
        [DllImport("user32.dll")] static extern bool ReleaseDC(IntPtr h, IntPtr dc);
        [DllImport("user32.dll", CharSet = CharSet.Ansi, EntryPoint = "RegisterClassA")] static extern ushort RegisterClassW(ref WNDCLASS wc);
        [DllImport("user32.dll")] static extern IntPtr DefWindowProc(IntPtr h, uint m, IntPtr w, IntPtr l);
        [DllImport("gdi32.dll")] static extern int ChoosePixelFormat(IntPtr dc, ref PIXELFORMATDESCRIPTOR p);
        [DllImport("gdi32.dll")] static extern bool SetPixelFormat(IntPtr dc, int fmt, ref PIXELFORMATDESCRIPTOR p);
        [DllImport("gdi32.dll")] static extern bool SwapBuffers(IntPtr dc);
        [DllImport("opengl32.dll")] static extern IntPtr wglCreateContext(IntPtr dc);
        [DllImport("opengl32.dll")] static extern bool wglMakeCurrent(IntPtr dc, IntPtr rc);
        [DllImport("opengl32.dll")] static extern bool wglDeleteContext(IntPtr rc);
        [DllImport("opengl32.dll", CharSet = CharSet.Ansi)] static extern IntPtr wglGetProcAddress(string name);
        [DllImport("kernel32.dll", CharSet = CharSet.Ansi)] static extern IntPtr GetProcAddress(IntPtr mod, string name);
        [DllImport("kernel32.dll", CharSet = CharSet.Ansi)] static extern IntPtr LoadLibraryA(string name);

        struct RECT { public int L, T, R, B; }
        struct WNDCLASS { public uint Style; public IntPtr WndProc; public int ClsExtra, WndExtra; public IntPtr Instance, Icon, Cursor, Brush; public string MenuName, Name; }
        struct PIXELFORMATDESCRIPTOR { public ushort Size, Version; public uint Flags; public byte PixelType, ColorBits, RedBits, RedShift, GreenBits, GreenShift, BlueBits, BlueShift, AlphaBits, AlphaShift, AccumBits, AccumRed, AccumGreen, AccumBlue, AccumAlpha, DepthBits, StencilBits, AuxCount, LayerType, Reserved, LayerMaskLo, LayerMaskHi, VisibleMaskLo, VisibleMaskHi, DamageMaskLo, DamageMaskHi; }
        delegate IntPtr WndProcDelegate(IntPtr h, uint msg, IntPtr w, IntPtr l);
        static WndProcDelegate _wndProc = (h, m, w, l) => DefWindowProc(h, m, w, l);
        static IntPtr _classRegistered = IntPtr.Zero, _opengl32;

        // GL 1.1 直接导出
        [DllImport("opengl32.dll")] static extern void glClear(uint mask);
        [DllImport("opengl32.dll")] static extern void glClearColor(float r, float g, float b, float a);
        [DllImport("opengl32.dll")] static extern void glEnable(uint cap);
        [DllImport("opengl32.dll")] static extern void glDisable(uint cap);
        [DllImport("opengl32.dll")] static extern void glViewport(int x, int y, int w, int h);
        [DllImport("opengl32.dll")] static extern void glMatrixMode(uint mode);
        [DllImport("opengl32.dll")] static extern void glLoadIdentity();
        [DllImport("opengl32.dll")] static extern unsafe void glLoadMatrixf(float* m);
        [DllImport("opengl32.dll")] static extern unsafe void glLightfv(uint light, uint pname, float* p);
        [DllImport("opengl32.dll")] static extern void glColor3f(float r, float g, float b);
        [DllImport("opengl32.dll")] static extern void glCullFace(uint mode);
        [DllImport("opengl32.dll")] static extern void glPolygonMode(uint face, uint mode);
        [DllImport("opengl32.dll")] static extern void glPushMatrix();
        [DllImport("opengl32.dll")] static extern void glPopMatrix();
        [DllImport("opengl32.dll")] static extern unsafe void glMultMatrixf(float* m);
        [DllImport("opengl32.dll")] static extern void glPolygonOffset(float factor, float units);
        [DllImport("opengl32.dll")] static extern unsafe void glVertexPointer(int size, uint type, int stride, void* ptr);
        [DllImport("opengl32.dll")] static extern unsafe void glNormalPointer(uint type, int stride, void* ptr);
        [DllImport("opengl32.dll")] static extern void glEnableClientState(uint cap);
        [DllImport("opengl32.dll")] static extern void glDisableClientState(uint cap);
        [DllImport("opengl32.dll")] static extern unsafe void glDrawElements(uint mode, int count, uint type, void* indices);
        [DllImport("opengl32.dll")] static extern void glPointSize(float size);
        const uint GL_POINTS = 0x0000;

        // GL 1.5 缓冲对象（GetProcAddress）
        delegate void glGenBuffers_t(int n, uint* buffers);
        delegate void glBindBuffer_t(uint target, uint buffer);
        delegate void glBufferData_t(uint target, nuint size, void* data, uint usage);
        static glGenBuffers_t _glGenBuffers;
        static glBindBuffer_t _glBindBuffer;
        static glBufferData_t _glBufferData;

        static T GetProc<T>(string name) where T : Delegate
        {
            var p = wglGetProcAddress(name);
            if (p == IntPtr.Zero)
            {
                _opengl32 = _opengl32 != IntPtr.Zero ? _opengl32 : LoadLibraryA("opengl32.dll");
                p = GetProcAddress(_opengl32, name);
            }
            if (p == IntPtr.Zero) return null;
            return Marshal.GetDelegateForFunctionPointer<T>(p);
        }

        public GLRenderHost() { }

        protected override IPlatformHandle CreateNativeControlCore(IPlatformHandle parent)
        {
            if (_classRegistered == IntPtr.Zero)
            {
                var wc = new WNDCLASS { Style = (uint)CS_OWNDC, Name = "OpenStepViewerGL", WndProc = Marshal.GetFunctionPointerForDelegate(_wndProc) };
                _classRegistered = (IntPtr)RegisterClassW(ref wc);
            }
            var parentHwnd = parent?.Handle ?? IntPtr.Zero;
            _hwnd = CreateWindowEx(0, "OpenStepViewerGL", "", WS_CHILD | WS_VISIBLE | WS_CLIPSIBLINGS, 0, 0, 100, 100, parentHwnd, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
            _hdc = GetDC(_hwnd);
            var pfd = new PIXELFORMATDESCRIPTOR
            {
                Size = (ushort)Marshal.SizeOf<PIXELFORMATDESCRIPTOR>(),
                Version = 1,
                Flags = PFD_DRAW_TO_WINDOW | PFD_SUPPORT_OPENGL | PFD_DOUBLEBUFFER,
                PixelType = 0,
                ColorBits = 24,
                DepthBits = 24
            };
            int fmt = ChoosePixelFormat(_hdc, ref pfd);
            SetPixelFormat(_hdc, fmt, ref pfd);
            _hglrc = wglCreateContext(_hdc);
            wglMakeCurrent(_hdc, _hglrc);

            _glGenBuffers = GetProc<glGenBuffers_t>("glGenBuffers");
            _glBindBuffer = GetProc<glBindBuffer_t>("glBindBuffer");
            _glBufferData = GetProc<glBufferData_t>("glBufferData");

            InitGL();
            _ready = true;

            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(33) };
            _timer.Tick += (s, e) => { if (_dirty) RenderFrame(); };
            _timer.Start();

            return new PlatformHandle(_hwnd, "HWND");
        }

        protected override void DestroyNativeControlCore(IPlatformHandle control)
        {
            _timer?.Stop();
            _ready = false;
            if (_hglrc != IntPtr.Zero) { wglDeleteContext(_hglrc); _hglrc = IntPtr.Zero; }
            if (_hdc != IntPtr.Zero) { ReleaseDC(_hwnd, _hdc); _hdc = IntPtr.Zero; }
            if (_hwnd != IntPtr.Zero) { DestroyWindow(_hwnd); _hwnd = IntPtr.Zero; }
            base.DestroyNativeControlCore(control);
        }

        void InitGL()
        {
            glEnable(GL_DEPTH_TEST);
            glEnable(GL_LIGHTING);
            glEnable(GL_LIGHT0);
            glEnable(GL_NORMALIZE);
            glEnable(GL_COLOR_MATERIAL);
            // 双面渲染：C# 网格绕向未统一，先不剔除背面
            glDisable(GL_CULL_FACE);
            glLightModelfvStub();
            float[] lightPos = { 0.4f, 0.5f, 1.0f, 0 };
            float[] lightDiff = { 0.85f, 0.85f, 0.85f, 1 };
            float[] lightAmb = { 0.38f, 0.38f, 0.38f, 1 };
            fixed (float* p = lightPos) glLightfv(GL_LIGHT0, GL_POSITION, p);
            fixed (float* p = lightDiff) glLightfv(GL_LIGHT0, GL_DIFFUSE, p);
            fixed (float* p = lightAmb) glLightfv(GL_LIGHT0, GL_AMBIENT, p);
            glClearColor(0.22f, 0.24f, 0.27f, 1f);
        }

        // 材质环境色使用 glColor；这里设置光照模型
        [DllImport("opengl32.dll")] static extern void glLightModelfv(uint pname, float* p);
        void glLightModelfvStub()
        {
            float[] amb = { 0.15f, 0.15f, 0.15f, 1 };
            fixed (float* p = amb) glLightModelfv(0x0B53 /*GL_LIGHT_MODEL_AMBIENT*/, p);
        }

        public void SetMesh(float[] pos, float[] nor, int[] idx, V3 bmin, V3 bmax)
        {
            _dist = (bmax - bmin).Length() * 1.2;
            _target = (bmin + bmax) * 0.5f;
            _pendingPos = pos; _pendingNor = nor; _pendingIdx = idx;
            _needsUpload = true;
            _hasMesh = true;
            _dirty = true;
        }

        public void ClearMesh() { _pendingIdx = null; _needsUpload = true; _hasMesh = false; _dirty = true; }

        // 逐实体绘制：每个实体一段索引 + 一串关节矩阵（根→叶，依次右乘）
        int[] _partStart = Array.Empty<int>();
        int[] _partCount = Array.Empty<int>();
        System.Numerics.Matrix4x4[][] _partChain;

        public void SetParts(int[] starts, int[] counts)
        {
            _partStart = starts ?? Array.Empty<int>();
            _partCount = counts ?? Array.Empty<int>();
            _dirty = true;
        }

        public void SetPartChains(System.Numerics.Matrix4x4[][] chains)
        {
            _partChain = chains;
            _dirty = true;
        }
        public bool Wire { set { _wire = value; _dirty = true; } }
        public void SelectFace(int iStart, int iCount) { _selStart = iStart; _selCount = iCount; _dirty = true; }

        public void Orbit(double dx, double dy)
        {
            _yaw += dx * 0.01;
            _pitch = Math.Clamp(_pitch + dy * 0.01, -Math.PI / 2 + 0.05, Math.PI / 2 - 0.05);
            _dirty = true;
        }
        public void Zoom(double factor) { _dist = Math.Max(1e-3, _dist * factor); _dirty = true; }
        public void Pan(double dx, double dy)
        {
            var eye = Eye;
            var fwd = V3.Normalize(_target - eye);
            var right = V3.Normalize(V3.Cross(fwd, Up));
            var upv = V3.Cross(right, fwd);
            _target += right * (float)(dx * _dist * 0.001) + upv * (float)(dy * _dist * 0.001);
            _dirty = true;
        }
        V3 Eye
        {
            get
            {
                var d = new V3((float)(_dist * Math.Cos(_pitch) * Math.Cos(_yaw)), (float)(_dist * Math.Cos(_pitch) * Math.Sin(_yaw)), (float)(_dist * Math.Sin(_pitch)));
                return _target + d;
            }
        }

        void Upload()
        {
            if (_vboP == 0) { uint b = 0; _glGenBuffers(1, &b); _vboP = b; _glGenBuffers(1, &b); _vboN = b; _glGenBuffers(1, &b); _vboI = b; }
            _glBindBuffer(GL_ARRAY_BUFFER, _vboP);
            fixed (float* p = _pendingPos) _glBufferData(GL_ARRAY_BUFFER, (nuint)(_pendingPos.Length * 4), p, GL_STATIC_DRAW);
            _glBindBuffer(GL_ARRAY_BUFFER, _vboN);
            fixed (float* p = _pendingNor) _glBufferData(GL_ARRAY_BUFFER, (nuint)(_pendingNor.Length * 4), p, GL_STATIC_DRAW);
            _glBindBuffer(GL_ELEMENT_ARRAY_BUFFER, _vboI);
            fixed (int* p = _pendingIdx) _glBufferData(GL_ELEMENT_ARRAY_BUFFER, (nuint)(_pendingIdx.Length * 4), p, GL_STATIC_DRAW);
            _indexCount = _pendingIdx.Length;
            _needsUpload = false;
        }

        void RenderFrame()
        {
            if (!_ready) return;
            if (_needsUpload)
            {
                if (_pendingIdx == null) { _indexCount = 0; _needsUpload = false; }
                else Upload();
            }

            GetClientRect(_hwnd, out var rc);
            int w = rc.R - rc.L, h = rc.B - rc.T;
            if (w <= 0 || h <= 0) return;
            glViewport(0, 0, w, h);
            glClear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT);
            if (!_hasMesh) { SwapBuffers(_hdc); _dirty = false; return; }

            var proj = M4.CreatePerspectiveFieldOfView((float)(50 * Math.PI / 180), w / (float)h, (float)(_dist * 0.005), (float)(_dist * 100));
            var view = M4.CreateLookAt(Eye, _target, Up);

            glMatrixMode(GL_PROJECTION);
            LoadMatrix(proj);
            glMatrixMode(GL_MODELVIEW);
            // 光固定在眼睛空间：加载 view 后直接画（光源位置在 view 空间固定方向，可接受）
            LoadMatrix(view);

            if (_indexCount > 0)
            {
                glEnableClientState(GL_VERTEX_ARRAY);
                glEnableClientState(GL_NORMAL_ARRAY);
                _glBindBuffer(GL_ARRAY_BUFFER, _vboP);
                glVertexPointer(3, GL_FLOAT, 0, null);
                _glBindBuffer(GL_ARRAY_BUFFER, _vboN);
                glNormalPointer(GL_FLOAT, 0, null);
                _glBindBuffer(GL_ELEMENT_ARRAY_BUFFER, _vboI);

                glPolygonMode(GL_FRONT_AND_BACK, _wire ? GL_LINE : GL_FILL);
                if (!_wire) { glEnable(GL_POLYGON_OFFSET_FILL); glPolygonOffset(1, 1); } else glDisable(GL_POLYGON_OFFSET_FILL);
                glColor3f(0.62f, 0.66f, 0.72f);
                if (_partStart.Length > 0 && _partChain != null && _partChain.Length == _partStart.Length)
                {
                    // 逐实体绘制并叠加关节变换：GL 的 current = current * M，故按根→叶顺序右乘
                    for (int pi = 0; pi < _partStart.Length; pi++)
                    {
                        int cnt = _partCount[pi];
                        if (cnt <= 0) continue;
                        glPushMatrix();
                        var chain = _partChain[pi];
                        if (chain != null)
                            for (int k = 0; k < chain.Length; k++)
                            {
                                var m = chain[k];
                                glMultMatrixf(&m.M11);
                            }
                        glDrawElements(GL_TRIANGLES, cnt, GL_UNSIGNED_INT, (void*)(_partStart[pi] * 4));
                        glPopMatrix();
                    }
                }
                else
                    glDrawElements(GL_TRIANGLES, _indexCount, GL_UNSIGNED_INT, null);

                if (_selStart >= 0 && _selCount > 0)
                {
                    glDisable(GL_LIGHTING);
                    glColor3f(1f, 0.45f, 0.15f);
                    glDrawElements(GL_TRIANGLES, _selCount, GL_UNSIGNED_INT, (void*)(_selStart * 4));
                    glEnable(GL_LIGHTING);
                }
                glDisableClientState(GL_VERTEX_ARRAY);
                glDisableClientState(GL_NORMAL_ARRAY);
            }

            SwapBuffers(_hdc);
            _dirty = false;
        }

        // System.Numerics 行主序内存直接上传：GL 按列主序读取，等价于加载转置矩阵，
        // 而列向量约定需要的正是行向量矩阵的转置，因此不能再手动 Transpose。
        void LoadMatrix(M4 m)
        {
            glLoadMatrixf(&m.M11);
        }
    }
}
