using AnyCAD.Foundation;
using AnyCAD.Simulate;
using System;
using System.Collections.Generic;

namespace AnyCAD.Demo.Interaction
{
    /// <summary>
    /// 键盘控制人物行走（第三人称）：
    /// A/D 转身，W/S 沿人物朝向前进/后退，Shift 跑步，空格回原点待机；相机跟随人物。
    ///
    ///
    /// 分工（每个类只管一件事，互不越界）：
    ///   - CharacterInputState：键盘/鼠标的「当前状态」；
    ///   - CharacterMotion：朝向、位置与速度的运动学积分（不碰场景，不碰动画）；
    ///   - CharacterAsset：加载模型 + 取剪辑 + 建播放器 + 记根节点自带变换；
    ///   - CharacterAnimator：Idle/Walk/Run 切换与逐帧推进；
    ///   - FollowCamera：球坐标跟随相机；
    ///   - CharacterWalkEditor：把引擎输入事件翻译成 CharacterInputState；
    ///   - Interaction_CharacterWalk：只做装配与每帧调度。
    ///
    /// 位姿直接写在 glTF 根节点上：T·R·原变换。T·R 作用于顶点是 T(R(v))，
    /// 先绕原点转身再平移 ⇒ 转身轴心恒在人物脚下，不需要额外的包装节点。
    /// </summary>
    class Interaction_CharacterWalk : TestCase
    {
        public Interaction_CharacterWalk()
        {
            Title = "人物行走控制";
            Description = "A/D 转身，W/S 前进/后退，Shift 跑步，空格回原点；相机跟随人物";
            Tags.Add("交互");
            Tags.Add("动画");
            Tags.Add("骨骼动画");
            Level = 2;
        }

        readonly CharacterInputState mInput = new CharacterInputState();
        readonly CharacterMotion mMotion = new CharacterMotion();
        readonly CharacterAsset mAsset = new CharacterAsset();
        readonly CharacterAnimator mAnimator = new CharacterAnimator();
        readonly FollowCamera mCamera = new FollowCamera();

        //! 编辑器实例由本对象持有：SetEditor 会把它交给引擎，但引擎是原生侧，
        //! C# 这里留一个强引用才能保证每次 Run/Exit 成对推入与清空，
        //! 也方便 Animation 里回写帧时间戳。
        CharacterWalkEditor mEditor;

        public override void Run(IRenderView render)
        {
            mInput.Reset();
            mMotion.Reset();

            if (!mAsset.Load(GetResourcePath(System.IO.Path.Combine("models", "Soldier.glb")), render))
            {
                Console.WriteLine("[CharacterWalk] 加载失败：" + mAsset.SourcePath);
                Console.WriteLine("[CharacterWalk] 请确认 data/models/Soldier.glb 存在，"
                                  + "且 AnyCAD.Exchange.GLTF.dll 已随运行时部署。");
                return;
            }
            if (!mAnimator.Bind(mAsset.Player, mAsset.AnimationRoot))
            {
                Console.WriteLine("[CharacterWalk] 模型缺少 Idle/Walk/Run 剪辑");
                render.ShowSceneNode(mAsset.AnimationRoot);
                return;
            }

            render.ShowCoordinateGrid(true);
            ShowGround(render);
            mEditor = new CharacterWalkEditor(mInput);
            render.SetEditor(mEditor);

            render.EnableAnimation(true);

            // 透视相机：正交投影下没有近大远小，第三人称观感不对。
            var cam = render.Camera;
            cam.SetProjectionType(EnumProjectionType.Perspective);
            cam.SetFovy(FollowCamera.FovyRadians);
            cam.SetNear(0.1);
            cam.UpdateProjectionMatrix();

            // 不请求 ZoomToFit：取景由 FollowCamera 每帧接管，ZoomToFit 算出的机位
            // 会立刻被覆盖，两者打架。这里先摆一次机位，避免首帧前停在默认位置。
            mCamera.Follow(cam, mMotion.Position, mInput.Orbit);
            render.RequestDraw(EnumUpdateFlags.Scene);
        }

        public override void Animation(IRenderView render, float time)
        {
            if (!mAsset.IsLoaded)
                return;

            // 编辑器回调里没有帧时间，先把本帧时刻交出去，事件才有时间戳可比。
            if (mEditor != null)
                mEditor.LastFrameTime = time;

            // 兜底（默认关闭，见 StaleKeyTimeout）：窗口失焦或被系统快捷键抢走时
            // KeyUp 不会送达，按键会永远卡在按下集合里。打开阈值后超过该时长
            // 没有任何事件就丢弃按键状态，宁可漏一次输入也不要无限转。
            if (mInput.DropStaleKeys(time))
                Console.WriteLine("[CharacterWalk] 按键事件中断，已丢弃残留按键状态");

            double elapsedMs = mInput.UpdateFrameTime(time);

            if (mInput.ResetRequested)
                mMotion.ResetToOrigin();

            mMotion.Update(mInput, elapsedMs);
            mAnimator.Play(mMotion.CurrentState);
            mAsset.ApplyPose(mMotion.Position, mMotion.FacingAngle);

            mCamera.Follow(render.Camera, mMotion.Position, mInput.Orbit);

            mAsset.Advance(elapsedMs);
            render.RequestDraw(EnumUpdateFlags.Scene);
        }

        public override void Exit(IRenderView render)
        {
            render.SetEditor(null); // 恢复默认编辑器
            render.EnableAnimation(false);
            mEditor = null;
            mInput.Reset();
            mAsset.Release();
            mAnimator.Release();
        }

        //! 地面参考：一块淡色地板，让人有"走在地上"的感觉。
        static void ShowGround(IRenderView render)
        {
            var material = MeshPhongMaterial.Create("character.ground");
            material.SetColor(new Vector3(0.55f, 0.55f, 0.58f));
            render.ShowSceneNode(new PrimitiveSceneNode(GeometryBuilder.CreatePlane(60, 60), material));
        }
    }

    /// <summary>行走/跑步状态：决定速度与剪辑。</summary>
    enum CharacterLocomotion
    {
        Idle,
        Walk,
        Run
    }

    /// <summary>
    /// 输入状态：键盘按下集合、Shift 修饰键、鼠标轨道参数与帧计时。
    /// 只保存状态，不做任何运动学或场景操作。
    /// </summary>
    class CharacterInputState
    {
        //! 按住即持续移动，抬起即停。
        readonly HashSet<EnumKeyCode> mPressedKeys = new HashSet<EnumKeyCode>();

        //! Shift 是修饰键，随每个按键事件携带（HasShiftKey），不进按键表。
        bool mRunHeld;

        //! 上一帧的引擎时刻（秒）。
        float mLastTime = -1f;

        //! 最近一次按键/鼠标事件发生的引擎时刻（秒）。
        float mLastEventTime = -1f;

        //! 按键陈旧阈值（秒）。窗口失焦、被系统快捷键抢走时会丢 KeyUp，
        //! 按键永远留在集合里。但「按住不动」同样没有新事件，事件层无法区分
        //! 二者，所以这个兜底默认关闭（置 0 表示不启用）；排查输入问题时再打开。
        const float StaleKeyTimeout = 0f;

        public OrbitControlParams Orbit { get; } = new OrbitControlParams();

        public void Reset()
        {
            mPressedKeys.Clear();
            mRunHeld = false;
            mLastTime = -1f;
            mLastEventTime = -1f;
            Orbit.Reset();
        }

        public void KeyDown(EnumKeyCode key, bool runHeld, float eventTime)
        {
            mRunHeld = runHeld;
            mPressedKeys.Add(key);
            mLastEventTime = eventTime;
        }

        public void KeyUp(EnumKeyCode key, bool runHeld, float eventTime)
        {
            mRunHeld = runHeld;
            mPressedKeys.Remove(key);
            mLastEventTime = eventTime;
        }

        public void TouchMouse(float eventTime)
        {
            mLastEventTime = eventTime;
        }

        /// <summary>
        /// 丢弃不可信的按键状态。仅在 StaleKeyTimeout 大于 0 时启用。
        /// 返回是否真的丢弃了按键。
        /// </summary>
        public bool DropStaleKeys(float now)
        {
            if (StaleKeyTimeout <= 0f || mPressedKeys.Count == 0)
                return false;

            // 还没收到过事件时不做判断，避免刚启动就清空。
            if (mLastEventTime < 0f)
                return false;

            if (now - mLastEventTime <= StaleKeyTimeout)
                return false;

            mPressedKeys.Clear();
            mRunHeld = false;
            return true;
        }

        public bool IsPressed(EnumKeyCode key)
        {
            return mPressedKeys.Contains(key);
        }

        public int PressedKeyCount
        {
            get { return mPressedKeys.Count; }
        }

        public bool RunHeld
        {
            get { return mRunHeld; }
        }

        /// <summary>
        /// 空格：回原点并待机。按住期间每帧都回原点（与 three.js 示例一致），
        /// 松开才允许移动。
        /// </summary>
        public bool ResetRequested
        {
            get { return IsPressed(EnumKeyCode.KEY_SPACE); }
        }

        /// <summary>
        /// 推进帧计时并返回本帧耗时（毫秒）。异常跳变（首帧、卡顿、时间回退）
        /// 统一归零，避免转身/位移一次跨出很远。
        /// </summary>
        public double UpdateFrameTime(float time)
        {
            if (mLastTime < 0f)
                mLastTime = time;
            var elapsedMs = (time - mLastTime) * 1000.0;
            mLastTime = time;
            // 首帧、卡顿、时间回退一律归零，避免转身/位移一次跨出很远。
            return (elapsedMs > 0.0 && elapsedMs <= 200.0) ? elapsedMs : 0.0;
        }
    }

    /// <summary>
    /// OrbitControls 风格的相机参数，Z-up 世界（场景竖直轴是 +Z，人物站在 XY 平面上）。
    /// 用球坐标描述机位：方位角 Yaw 绕世界 +Z 轴，俯仰 Pitch 是与 XY 平面的夹角。
    /// 鼠标拖拽/滚轮只改这里，不直接碰相机。
    /// </summary>
    class OrbitControlParams
    {
        //! 初始机位距离。FOV 50°、人物身高约 1.8 单位：
        //! 距离 9 时垂直可见高度约 8.4 单位，人物占屏约 22%，
        //! 周围留出地面与空间，能看到全貌。
        public const double DistInit = 9.0;

        //! 初始俯角（弧度），与 XY 平面的夹角。0.28 约 16°，
        //! 接近标准第三人称视角，人物全身与脚下地面都能带住。
        public const double PitchInit = 0.28;

        public const double MinDist = 2.0;
        public const double MaxDist = 340.0;

        //! 俯仰范围：略微允许负值（从下方仰视），上限接近天顶但不给到 90°，
        //! 否则视线与 +Z 共线，lookAt 的 up 向量退化、画面会翻转。
        public const double MinPitch = -0.2;
        public const double MaxPitch = 1.35;

        //! 方位角（弧度），绕世界 +Z 轴。0 = 相机在 -Y 侧，
        //! 即人物背后（人物 θ=0 时面朝 +Y）。
        public double Yaw { get; set; }

        //! 俯仰角（弧度），机位与 XY 平面的夹角。正值 = 相机高于注视点。
        public double Pitch { get; set; }

        public double Distance { get; set; }

        public void Reset()
        {
            Yaw = 0.0;
            Pitch = PitchInit;
            Distance = DistInit;
        }

        //! 绕世界 +Z 轴公转（左右拖）。dx 为像素位移。
        public void OrbitBy(int dx)
        {
            Yaw += dx * 0.008;
        }

        //! 上下拖：改与 XY 平面的夹角，钳制在 [MinPitch, MaxPitch]。
        public void PitchBy(int dy)
        {
            Pitch = Math.Max(MinPitch, Math.Min(MaxPitch, Pitch + dy * 0.005));
        }

        //! 滚轮推拉：乘性缩放，靠近/远离人物。
        public void ZoomBy(int delta)
        {
            Distance *= (delta > 0) ? 0.9 : 1.0 / 0.9;
            Distance = Math.Max(MinDist, Math.Min(MaxDist, Distance));
        }

        /// <summary>
        /// 球坐标 → 世界坐标偏移（Z-up）：水平面内的方向绕 +Z 旋转，
        /// 竖直分量沿 +Z。返回的向量从注视点指向机位。
        /// 方位角 0 时水平方向为 (0, -1)，即位于人物背后（人物面朝 +Y）。
        /// </summary>
        public Vector3d GetOffset()
        {
            double cosPitch = Math.Cos(Pitch);
            double sinPitch = Math.Sin(Pitch);
            double distance = Math.Max(MinDist, Math.Min(MaxDist, Distance));

            return new Vector3d(
                Math.Sin(Yaw) * cosPitch * distance,
                -Math.Cos(Yaw) * cosPitch * distance,
                sinPitch * distance);
        }
    }

    /// <summary>
    /// 运动学：朝向角 + 位置 + 当前动作状态。纯数学，不引用场景节点。
    /// 转身与位移互斥（按着 A/D 只转身不走），与 three.js 第三人称一致。
    /// </summary>
    class CharacterMotion
    {
        //! 行走/跑步速度（场景单位/秒）。Soldier 根节点 scale 0.01，
        //! 骨架原始身高约 180cm，落地后实际身高约 1.8 个场景单位。
        //! 按身高校准：成年人步行约 1.0-1.4 m/s、慢跑约 3-4 m/s，
        //! 这里 1 单位≈1 米，取步行 1.1、跑步 3.2，与真实步频匹配。
        const double WalkSpeed = 1.1;
        const double RunSpeed = 3.2;

        //! 转身角速度（弧度/秒）：A 逆时针、D 顺时针。
        const double TurnSpeed = 2.5;

        //! 行进符号：+1 前进、-1 后退（后退时保持面朝方向，只倒着走）。
        double mMoveSign = 1.0;

        //! 人物朝向：模型面朝 +Y（glTF 惯例），行走方向由这个角度绕 Z 旋转得到。
        public double FacingAngle { get; private set; }

        //! 当前位置（世界坐标，Z-up）。
        public Vector3d Position { get; private set; }

        public CharacterLocomotion CurrentState { get; private set; }

        public CharacterMotion()
        {
            Reset();
        }

        public void Reset()
        {
            FacingAngle = 0.0;
            Position = new Vector3d(0, 0, 0);
            CurrentState = CharacterLocomotion.Idle;
            mMoveSign = 1.0;
        }

        //! 空格回原点待机：位置与朝向同时归零。
        public void ResetToOrigin()
        {
            Reset();
        }

        public void Update(CharacterInputState input, double elapsedMs)
        {
            if (elapsedMs <= 0.0)
                return;

            double dt = elapsedMs * 0.001;

            if (input.IsPressed(EnumKeyCode.KEY_a) || input.IsPressed(EnumKeyCode.KEY_d))
            {
                // 转身方向：FacingAngle 是「Z-up 俯视下的视觉朝向角」，
                // 增大 = 逆时针 = 向左转，所以 A 加、D 减。
                if (input.IsPressed(EnumKeyCode.KEY_a))
                    FacingAngle += TurnSpeed * dt;
                if (input.IsPressed(EnumKeyCode.KEY_d))
                    FacingAngle -= TurnSpeed * dt;
                CurrentState = CharacterLocomotion.Idle; // 原地转身：本帧不产生位移
                return;
            }

            bool forward = input.IsPressed(EnumKeyCode.KEY_w);
            bool back = input.IsPressed(EnumKeyCode.KEY_s);
            if (!forward && !back)
            {
                CurrentState = CharacterLocomotion.Idle;
                return;
            }

            CurrentState = input.RunHeld ? CharacterLocomotion.Run : CharacterLocomotion.Walk;
            mMoveSign = back ? -1.0 : 1.0;

            // 面朝方向必须与 ApplyPose 的 Rz(-θ) 严格同源：
            // Rz(-θ) 作用于模型前向 (0,1) 得 (sinθ, cosθ)，位移就沿这个方向走。
            // 两处任何一处单独改符号，都会出现「转身正常但 W 走反」。
            double fx = Math.Sin(FacingAngle);
            double fy = Math.Cos(FacingAngle);
            double speed = (CurrentState == CharacterLocomotion.Run) ? RunSpeed : WalkSpeed;
            double step = mMoveSign * speed * dt;
            Position = new Vector3d(Position.x + fx * step, Position.y + fy * step, 0);
        }
    }

    /// <summary>
    /// 角色资产：加载模型、建播放器、记住 glTF 根节点自带的局部变换。
    /// 位姿写回也在这里——它是唯一知道"根节点原有变换不能被抹掉"的角色。
    /// </summary>
    class CharacterAsset
    {
        SkeletalAnimationPlayer mPlayer;

        public string SourcePath { get; private set; }


        public AnimationSceneNode AnimationRoot { get; private set; }

        //public GroupSceneNode SceneRooot { get; private set; }

        public SkeletalAnimationPlayer Player
        {
            get { return mPlayer; }
        }

        public bool IsLoaded
        {
            get { return mPlayer != null; }
        }

        public bool Load(string fileName, IRenderView render)
        {
            SourcePath = fileName;

            var root = SceneIO.Load(fileName);
            if (root == null)
                return false;

            // 原地动画 + 程序控位：播放器只管姿势，位移由 CharacterMotion 推进。
            var player = SkeletalAnimationPlayer.Create(root);
            if (player == null)
                return false;

            //SceneRooot = new GroupSceneNode();
            //SceneRooot.AddNode(root);

            AnimationRoot = player.GetSceneRoot();
            mPlayer = player;

            render.ShowSceneNode(AnimationRoot);
            return true;
        }

        /// <summary>
        /// 写回位姿：单矩阵 T·R·原变换。T·R 作用于顶点是 T(R(v))，
        /// 先绕原点转身、再平移到当前位置 ⇒ 转身轴心恒为人物脚下原点。
        /// 转角取 -facingAngle。模型根节点自带绕 X 轴 -90° 的 Y-up→Z-up 旋转，
        /// 它使绕 +Z 的正向旋转在画面上呈逆时针；取负后 facingAngle 即
        /// 「Z-up 俯视下的视觉朝向角」，与 CharacterMotion 的 dir=(sinθ, cosθ) 同源。
        /// 改动这两处中的任何一处都必须同步改另一处，否则会出现转身正常但 W 走反。
        /// </summary>
        public void ApplyPose(Vector3d position, double facingAngle)
        {
            if (AnimationRoot == null)
                return;

            var pose = Matrix4d.makeTranslation(position.x, position.y, position.z)
                       * Matrix4d.makeRotationAxis(new Vector3d(0, 0, 1), -facingAngle);
            AnimationRoot.SetTransform(pose);
        }

        public void Advance(double elapsedMs)
        {
            if (mPlayer != null)
                mPlayer.Advance(elapsedMs);
        }

        public void Release()
        {
            mPlayer = null;
            //SceneRooot = null;
            AnimationRoot = null;
        }
    }

    /// <summary>
    /// 动画调度：按运动状态切换 Idle/Walk/Run 并逐帧推进。
    /// 全程复用同一个播放器（加载模型时建的那一个），切剪辑只是换它播的内容：
    /// PlayClip 会停掉其他动作、把新剪辑从 0 开始播，所以任意时刻只有一条剪辑生效。
    /// 同一条剪辑重复 PlayClip 是安全的（ClipAction 复用同一 action），
    /// 且会重建时钟，所以这里显式记住当前剪辑、只在变化时切换。
    /// </summary>
    class CharacterAnimator
    {
        SkeletalAnimationPlayer mPlayer;
        AnimationClip mClipIdle;
        AnimationClip mClipWalk;
        AnimationClip mClipRun;

        //! 当前正在播的剪辑，避免每帧重复 PlayClip 重置时间轴。
        AnimationClip mCurrentClip;

        //! 按名字取三条剪辑。Soldier.glb 固定带 Idle/Walk/Run
        //! （TPose 是绑定姿势，只用于建模，不拿来播）。
        public bool Bind(SkeletalAnimationPlayer player, AnimationSceneNode animRoot)
        {
            if (player == null || animRoot == null)
                return false;

            var idle = animRoot.FindAnimation("Idle");
            var walk = animRoot.FindAnimation("Walk");
            var run = animRoot.FindAnimation("Run");
            if (idle == null || walk == null || run == null)
                return false;

            mPlayer = player;
            mClipIdle = idle;
            mClipWalk = walk;
            mClipRun = run;
            PlayClip(mClipIdle); // 开场待机
            return true;
        }

        public void Play(CharacterLocomotion state)
        {
            switch (state)
            {
                case CharacterLocomotion.Run:
                    PlayClip(mClipRun);
                    break;
                case CharacterLocomotion.Walk:
                    PlayClip(mClipWalk);
                    break;
                default:
                    PlayClip(mClipIdle);
                    break;
            }
        }

        //! 切剪辑：复用同一个播放器，只换它播的剪辑。PlayClip 内部保证独占
        //! （停掉其他动作 + 从 0 开始），所以不需要为每个状态另建播放器。
        void PlayClip(AnimationClip clip)
        {
            if (clip == null || clip == mCurrentClip)
                return;
            mCurrentClip = clip;
            mPlayer.PlayClip(clip, EnumAnimationLoopMode.Loop);
        }

        public void Release()
        {
            mPlayer = null;
            mCurrentClip = null;
            mClipIdle = null;
            mClipWalk = null;
            mClipRun = null;
        }
    }

    /// <summary>
    /// 第三人称跟随相机（Z-up）：注视点锁人物胸口，机位按球坐标绕人物布置。
    /// 只读角色位置与轨道参数，不改角色状态。
    /// </summary>
    class FollowCamera
    {
        public const double FovyRadians = 50.0 * Math.PI / 180.0;

        //! 世界竖直轴。场景是 Z-up（glTF 的 Y-up 已由根节点旋转转换过来），
        //! 相机 up 必须跟着用 +Z，否则 lookAt 会以 Y 为「上」，画面侧躺。
        public static readonly Vector3d WorldUp = new Vector3d(0, 0, 1);

        //! 注视点高度：人物身高约 1.8 单位，取 0.9（胸口偏下）作为画面中心。
        const double TargetHeight = 0.9;

        //! 机位高度下限：Pitch 允许负值（仰视），但机位不能钻到地面以下，
        //! 否则会看到地板背面。留一点余量避免擦着地面。
        const double MinCameraHeight = 1.3;

        public void Follow(Camera camera, Vector3d position, OrbitControlParams orbit)
        {
            if (camera == null)
                return;

            var target = new Vector3d(position.x, position.y, position.z + TargetHeight);
            var offset = orbit.GetOffset();

            camera.SetTarget(target);
            camera.SetUp(WorldUp);
            camera.SetPosition(new Vector3d(
                target.x + offset.x,
                target.y + offset.y,
                Math.Max(MinCameraHeight, target.z + offset.z)));
            camera.UpdateViewMatrix();
        }
    }

    /// <summary>
    /// 输入翻译器：把引擎的键鼠事件写进 CharacterInputState。
    /// 鼠标部分按 OrbitControls 语义处理（左键拖拽环绕、滚轮推拉），
    /// 右键/中键不消费，交给引擎默认行为。
    /// </summary>
    class CharacterWalkEditor : Editor
    {
        readonly CharacterInputState mInput;

        //! 最近一次收到的引擎时刻（秒）。事件回调里拿不到帧时间，
        //! 由 Animation 每帧写入，用来给按键状态打时间戳。
        public float LastFrameTime { get; set; }

        //! 鼠标拖拽起点（区分「拖拽中」与「刚按下」）。
        bool mDragging;
        int mLastMouseX;
        int mLastMouseY;

        public CharacterWalkEditor(CharacterInputState input)
        {
            mInput = input;
        }

        //! 按键事件诊断开关：设环境变量 ANYCAD_TRACE_KEYS=1 即可打开，
        //! 每次 KeyDown/KeyUp 打印一行，能直接看出 KeyUp 有没有送达。
        static readonly bool TraceKeyEvents =
            Environment.GetEnvironmentVariable("ANYCAD_TRACE_KEYS") == "1";

        public override EnumEditorCode OnKeyDown(ViewContext ctx, InputEvent evt)
        {
            mInput.KeyDown(evt.GetKey(), evt.HasShiftKey(), LastFrameTime);
            if (TraceKeyEvents)
                Console.WriteLine("[CharacterWalk] KeyDown " + evt.GetKey());
            return EnumEditorCode.Processed;
        }

        public override EnumEditorCode OnKeyUp(ViewContext ctx, InputEvent evt)
        {
            mInput.KeyUp(evt.GetKey(), evt.HasShiftKey(), LastFrameTime);
            if (TraceKeyEvents)
                Console.WriteLine("[CharacterWalk] KeyUp   " + evt.GetKey());
            return EnumEditorCode.Processed;
        }

        public override EnumEditorCode OnMouseDown(ViewContext ctx, InputEvent evt)
        {
            mInput.TouchMouse(LastFrameTime);
            if (evt.GetButtons() == EnumMouseButton.Left)
            {
                mDragging = true;
                mLastMouseX = evt.GetX();
                mLastMouseY = evt.GetY();
            }
            return EnumEditorCode.Processed;
        }

        public override EnumEditorCode OnMouseMove(ViewContext ctx, InputEvent evt)
        {
            if (!mDragging)
                return EnumEditorCode.Ignored;

            mInput.TouchMouse(LastFrameTime);

            int dx = evt.GetX() - mLastMouseX;
            int dy = evt.GetY() - mLastMouseY;
            mLastMouseX = evt.GetX();
            mLastMouseY = evt.GetY();

            mInput.Orbit.OrbitBy(dx);
            mInput.Orbit.PitchBy(dy);
            return EnumEditorCode.Processed;
        }

        public override EnumEditorCode OnMouseUp(ViewContext ctx, InputEvent evt)
        {
            mInput.TouchMouse(LastFrameTime);
            mDragging = false;
            return EnumEditorCode.Processed;
        }

        public override EnumEditorCode OnMouseWheel(ViewContext ctx, InputEvent evt, int delta)
        {
            mInput.TouchMouse(LastFrameTime);
            mInput.Orbit.ZoomBy(delta);
            return EnumEditorCode.Processed;
        }
    }
}
