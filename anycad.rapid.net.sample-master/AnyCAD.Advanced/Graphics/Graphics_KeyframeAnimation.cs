using AnyCAD.Foundation;
using AnyCAD.Simulate;
using System;

namespace AnyCAD.Demo.Graphics
{
    /// <summary>
    /// 关键帧动画入门：不读任何模型文件，用代码搭一个会动的关节链。
    ///
    /// 整个流程：
    ///   1. 搭节点：容器根 -> Hub（绕 Z 转）-> Arm（相对 Hub 平移）
    ///   2. 建剪辑：给需要动的节点各配一条 TRS 轨道，轨道名就是「节点名.属性」
    ///   3. 播放：new AnimationMixer(根) -> ClipAction(剪辑).Play() -> 每帧 Update(dt)
    ///
    /// 轨道名里的节点按名字在根的子树上查找，所以节点名必须和轨道名前缀完全一致。
    /// 时间单位：轨道数据用秒，Update 的入参也是秒；只有 SkeletalAnimationPlayer
    /// 那一层才用毫秒。
    /// </summary>
    class Graphics_KeyframeAnimation : TestCase
    {
        public Graphics_KeyframeAnimation()
        {
            Title = "关键帧动画";
            Description = "代码构造两节关节链，用 AnimationMixer 驱动旋转与平移轨道";
            Tags.Add("显示");
            Tags.Add("动画");
            Tags.Add("关键帧");
            Level = 1;
        }

        //! 剪辑时长（秒）。所有轨道的最后一帧都落在这里。
        const float Duration = 2.0f;

        const string HubName = "Hub";
        const string ArmName = "Arm";

        //! 混合器必须存活到退出：节点属性由它每帧写回，提前释放动画就停了。
        AnimationMixer mMixer;
        AnimationAction mAction;

        //! 上一帧的引擎时刻（秒）。Animation 回调给的是自启动以来的累计时间，
        //! 而混合器要的是帧间隔，所以自己记一次差值。
        float mLastTime = -1f;

        public override void Run(IRenderView render)
        {
            mMixer = null;
            mAction = null;
            mLastTime = -1f;

            // ---- 1. 节点树 ----
            // 根节点只是容器，不带变换；Hub 绕 Z 自转，Arm 挂在 Hub 下被一起带着转，
            // 同时沿自己的局部 X 方向来回平移，于是看起来像一条伸缩短的机械臂。
            var root = new GroupSceneNode();
            root.SetName("AnimRoot");

            var hub = new GroupSceneNode();
            hub.SetName(HubName);
            root.AddNode(hub);

            var arm = new GroupSceneNode();
            arm.SetName(ArmName);
            hub.AddNode(arm);

            // 可见几何：Hub 上一个球、Arm 一节长条。几何对象本身不参与动画，
            // 动的是它们所在节点的变换矩阵。
            var hubMaterial = MeshPhongMaterial.Create("anim.hub");
            hubMaterial.SetColor(new Vector3(0.85f, 0.35f, 0.2f));
            hub.AddNode(new PrimitiveSceneNode(GeometryBuilder.CreateSphere(12, 32, 16), hubMaterial));

            var armMaterial = MeshPhongMaterial.Create("anim.arm");
            armMaterial.SetColor(new Vector3(0.25f, 0.55f, 0.9f));
            var armGeometry = GeometryBuilder.CreateBox(120, 12, 12);
            var armMesh = new PrimitiveSceneNode(armGeometry, armMaterial);
            // 长条从 Hub 中心向外伸：局部平移半个长度，让它以 Hub 为转轴。
            armMesh.SetTransform(Matrix4d.makeTranslation(new Vector3d(60, 0, 0)));
            arm.AddNode(armMesh);

            // ---- 2. 剪辑 ----
            var clip = BuildClip();

            // ---- 3. 播放 ----
            // 混合器以根节点为作用域：轨道里的节点名都在这棵子树里查找。
            mMixer = new AnimationMixer(root);

            // ClipAction 对同一 (剪辑, 根) 会复用同一个动作，重复调用不会叠加。
            mAction = mMixer.ClipAction(clip);
            mAction.SetLoop(EnumAnimationActionLoopStyles.LoopRepeat, -1); // -1 = 无限循环
            mAction.Play();

            render.ShowSceneNode(root);
            render.ShowCoordinateGrid(false);
            render.EnableAnimation(true);
            render.RequestDraw(EnumUpdateFlags.ZoomToFit);
        }

        /// <summary>
        /// 建一条 2 秒的剪辑：Hub 绕 Z 轴匀速转一整圈，Arm 的长度来回伸缩。
        ///
        /// 轨道有三个约定：
        ///   - 名字必须在根的子树上唯一对应一个节点，格式「节点名.属性」；
        ///     属性只支持 position / quaternion / scale 三种。
        ///   - times 是所有轨道共享的时间轴（秒，必须递增）；
        ///   - values 按帧顺序平铺，position/scale 每帧 3 个分量，
        ///     quaternion 每帧 4 个分量（x,y,z,w，顺序与 glTF 一致）。
        /// </summary>
        AnimationClip BuildClip()
        {
            var clip = new AnimationClip();
            clip.SetName("ArmSwing");

            // ---- Hub：绕 Z 轴 0° -> 360° ----
            // 四元数按球面插值，两帧之间走的是最短圆弧，所以首尾写成 0 -> 360
            // 等同于不动。拆成 0/120/240/360 四帧，每段都是 120°，方向唯一，
            // 才能真正转起来。
            var angles = new[] { 0.0, 120.0, 240.0, 360.0 };
            var hubTimes = new FloatList();
            var hubQuats = new FloatList();
            for (int i = 0; i < angles.Length; i++)
            {
                hubTimes.Add(Duration * (float)i / (angles.Length - 1));

                var halfRad = angles[i] * Math.PI / 180.0 * 0.5;
                hubQuats.Add(0f);                          // x
                hubQuats.Add(0f);                          // y
                hubQuats.Add((float)Math.Sin(halfRad));    // z：绕 Z 转
                hubQuats.Add((float)Math.Cos(halfRad));    // w
            }

            clip.AddTrack(new QuaternionKeyframeTrack(HubName + ".quaternion",
                                                      Float32Buffer.Create(hubTimes),
                                                      Float32Buffer.Create(hubQuats),
                                                      EnumInterpolationModes.Linear));

            // ---- Arm：沿 X 方向来回平移 ----
            // 3 个关键帧：0 -> 40 -> 0，走到头再退回来，接缝处连续，循环不跳。
            var armTimes = new FloatList();
            armTimes.Add(0f);
            armTimes.Add(Duration * 0.5f);
            armTimes.Add(Duration);

            var armValues = new FloatList();
            armValues.Add(0f); armValues.Add(0f); armValues.Add(0f);
            armValues.Add(40f); armValues.Add(0f); armValues.Add(0f);
            armValues.Add(0f); armValues.Add(0f); armValues.Add(0f);

            clip.AddTrack(new VectorKeyframeTrack(ArmName + ".position",
                                                  Float32Buffer.Create(armTimes),
                                                  Float32Buffer.Create(armValues),
                                                  EnumInterpolationModes.Linear));

            // 时长由所有轨道里最晚的关键帧决定，写完轨道调一次即可。
            clip.ResetDuration();
            return clip;
        }

        public override void Animation(IRenderView render, float time)
        {
            if (mMixer == null)
                return;

            if (mLastTime < 0f)
                mLastTime = time;

            // 混合器吃的是「秒」，且是帧间隔，不是累计时间。
            var deltaSeconds = time - mLastTime;
            mLastTime = time;

            // 首帧与长时间挂起（拖动窗口、切到后台）后差值会异常大，
            // 不钳住的话动画会瞬间跳完一大段。
            if (deltaSeconds <= 0.0f || deltaSeconds > 0.2f)
            {
                render.RequestDraw(EnumUpdateFlags.Scene);
                return;
            }

            // Update 内部：推进全局时间 -> 按权重求值所有激活的动作 -> 写回节点 TRS。
            mMixer.Update(deltaSeconds);

            // 节点变换是原地写入的，没有 update version 变化，引擎不会自己发现。
            render.RequestDraw(EnumUpdateFlags.Scene);
        }

        public override void Exit(IRenderView render)
        {
            render.EnableAnimation(false);
            mAction = null;
            mMixer = null;
            mLastTime = -1f;
        }
    }
}
