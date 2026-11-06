using AnyCAD.Foundation;
using AnyTubeNet.Services;
using AnyTubeNet.Visualization;

namespace AnyCAD.Demo.Graphics
{
    /// <summary>
    /// 圆管激光切割仿真：加载 AnyTube 圆管带孔激光切割轨迹 JSON，
    /// 展示管件旋转与激光头联动完成切割与打孔路径的动画。
    /// </summary>
    class Simulation_TubeLaserCutRound : TestCase
    {
        PathAnimator mAnimator;

        public Simulation_TubeLaserCutRound()
        {
            Title = "圆管激光切割仿真";
            Description = "加载 AnyTube 圆管带孔激光切割轨迹，仿真管件旋转与激光头联动切割、打孔动画";
            Tags.Add("仿真");
            Tags.Add("管材");
            Tags.Add("激光切割");
            Tags.Add("打孔");
            Level = 3;
        }

        public override void Run(IRenderView render)
        {
            var document = AnyTubeJsonLoader.Load(GetResourcePath("AnyTube/simple_round_tube_with_holes.json"));
            var scene = AnyTubeSceneBuilder.Build(render, document);

            mAnimator = new PathAnimator(scene);
            mAnimator.Speed = 8.0; // 8 倍速播放，完整轨迹约 9 秒
            mAnimator.Play();

            render.EnableAnimation(true);
            render.RequestDraw(EnumUpdateFlags.Scene);
        }

        public override void Animation(IRenderView render, float time)
        {
            if (mAnimator == null)
                return;

            if (mAnimator.Tick(time))
            {
                render.RequestDraw(EnumUpdateFlags.Scene);
                return;
            }

            // 播放结束后自动循环，方便在示例库中持续展示
            if (!mAnimator.IsPlaying && mAnimator.Progress >= 1.0)
            {
                mAnimator.Play();
                render.RequestDraw(EnumUpdateFlags.Scene);
            }
        }

        public override void Exit(IRenderView render)
        {
            render.EnableAnimation(false);
        }
    }
}
