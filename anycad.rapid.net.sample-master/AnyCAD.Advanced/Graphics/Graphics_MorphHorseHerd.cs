using AnyCAD.Foundation;
using AnyCAD.Simulate;
using System;

namespace AnyCAD.Demo.Graphics
{
    /// <summary>
    /// 三匹马各自播放同一段形变动画，但进度不同。
    ///
    /// 关键点：同一个模型要出现多份、且互不同步，必须让每份实例的“可变状态”彼此独立。
    /// </summary>
    class Graphics_MorphHorseHerd : TestCase
    {
        public Graphics_MorphHorseHerd()
        {
            Title = "三匹马不同步动画";
            Description = "同一个 glTF 模型摆三份，各自播放形变动画的 0% / 45% / 90% 进度";
            Tags.Add("显示");
            Tags.Add("动画");
            Tags.Add("形变动画");
            Level = 2;
        }

        //! 三匹各自停在该剪辑的哪个位置（0~1）。Horse 的权重曲线首尾同位姿，
        //! 所以避开 0 与 1 两端，否则头尾两匹看起来一样。
        static readonly double[] PhaseFractions = { 0.05, 0.45, 0.8 };

        //! 横向间距（模型单位）。Horse 沿自身长度约 320，260 有轻微重叠感但不打架。
        const double Spacing = 260.0;

        readonly System.Collections.Generic.List<SkeletalAnimationPlayer> mPlayers =
            new System.Collections.Generic.List<SkeletalAnimationPlayer>();

        public override void Run(IRenderView render)
        {
            mPlayers.Clear();

            var fileName = GetResourcePath(System.IO.Path.Combine("models", "Horse.glb"));
            var root = SceneIO.Load(fileName);
            if (root == null)
            {
                // 常见原因：打印文件缺失，或 glTF 读取器插件没随包放到位。
                Console.WriteLine("[MorphHorseHerd] 加载失败：" + fileName);
                Console.WriteLine("[MorphHorseHerd] 请确认 data/models/Horse.glb 存在，且 AnyCAD.Exchange.GLTF.dll 已随运行时部署。");
                return;
            }

            // 含动画的模型，加载器返回的根节点就是 AnimationSceneNode（动画剪辑与骨架挂在它上面）。
            var source = AnimationSceneNode.Cast(root);
            if (source == null || source.GetAnimationCount() == 0)
            {
                Console.WriteLine("[MorphHorseHerd] 模型不含动画剪辑");
                render.ShowSceneNode(root);
                return;
            }

            var clip = source.GetAnimation(0);
            var durationMs = clip.GetDuration() * 1000.0;
            if (durationMs <= 0.0)
            {
                Console.WriteLine("[MorphHorseHerd] 剪辑时长为 0");
                return;
            }
            // 主对象
            {
                var mainPlayer = SkeletalAnimationPlayer.Create(source);
                if (mainPlayer != null)
                {
                    mainPlayer.PlayClip(clip, EnumAnimationLoopMode.Loop);
                    mainPlayer.Seek(durationMs * PhaseFractions[0]);
                    mPlayers.Add(mainPlayer);

                    render.ShowSceneNode(mainPlayer.GetSceneRoot());
                }

            }

            // 创建克隆对象
            for (int i = 1; i < PhaseFractions.Length; i++)
            {
                var player = SkeletalAnimationPlayer.CreateInstance(source, Matrix4d.makeTranslation(Spacing * i, 0, 0));
                if (player != null)
                {
                    // 剪辑是只读数据，三匹共用同一份。循环播放，让进度差长期保持。
                    player.PlayClip(clip, EnumAnimationLoopMode.Loop);

                    // 直接定位到各自的起始相位，三匹从此各走各的。
                    player.Seek(durationMs * PhaseFractions[i]);

                    mPlayers.Add(player);

                    render.ShowSceneNode(player.GetSceneRoot());
                }

            }

            if (mPlayers.Count == 0)
                return;

            // 按首帧姿态取景：形变动画会改顶点位置，用加载时的包围盒可能偏。
            // 手动摆一个斜俯视角，三匹横向排开能一次看全。
            var camera = render.Camera;
            camera.SetProjectionType(EnumProjectionType.Perspective);
            camera.SetNear(1.0f);
            camera.SetFar(8000.0f);
            camera.LookAt(new Vector3d(-Spacing - 620, -880, 420), new Vector3d(0, 0, 70), Vector3d.UNIT_Z);

            render.ShowCoordinateGrid(false);
            render.EnableAnimation(true);
        }

        public override void Animation(IRenderView render, float time)
        {
            if (mPlayers.Count == 0)
                return;

            if (mLastTime < 0f)
                mLastTime = time;
            var elapsedMs = (time - mLastTime) * 1000.0;
            mLastTime = time;

            // 首帧或长时间挂起（拖动窗口）后 elapsedMs 会异常大，钳一下别让动画跳一大段。
            if (elapsedMs <= 0.0 || elapsedMs > 200.0)
            {
                render.RequestDraw(EnumUpdateFlags.Scene);
                return;
            }

            foreach (var player in mPlayers)
                player.Advance(elapsedMs);


            render.RequestDraw(EnumUpdateFlags.Scene);
        }

        public override void Exit(IRenderView render)
        {
            render.EnableAnimation(false);
            mPlayers.Clear();
            mLastTime = -1f;
        }

        float mLastTime = -1f;
    }
}
