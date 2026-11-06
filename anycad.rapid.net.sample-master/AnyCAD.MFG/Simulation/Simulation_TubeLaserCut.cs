using AnyCAD.Foundation;
using AnyTubeNet.Services;
using AnyTubeNet.Visualization;

namespace AnyCAD.Demo.Graphics
{
    /// <summary>
    /// 方管激光切割仿真：加载 AnyTube 方管激光切割轨迹 JSON，
    /// 展示管件旋转与激光头联动完成切割路径的动画。
    /// </summary>
    class Simulation_TubeLaserCut : TestCase
    {
        PathAnimator mAnimator;

        public Simulation_TubeLaserCut()
        {
            Title = "方管激光切割仿真";
            Description = "加载 AnyTube 方管激光切割轨迹，仿真管件旋转与激光头联动切割动画";
            Tags.Add("仿真");
            Tags.Add("管材");
            Tags.Add("激光切割");
            Level = 2;
        }

        public override void Run(IRenderView render)
        {
            var document = AnyTubeJsonLoader.Load(GetResourcePath("AnyTube/simple_rect_tube.json"));
            var scene = AnyTubeSceneBuilder.Build(render, document);

            mAnimator = new PathAnimator(scene);
            mAnimator.Speed = 8.0; // 8 倍速播放，完整轨迹约 5 秒
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
