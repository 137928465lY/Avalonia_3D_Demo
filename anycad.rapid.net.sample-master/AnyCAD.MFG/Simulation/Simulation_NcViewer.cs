using AnyCAD.Foundation;
using NcViewerNet.Services;
using NcViewerNet.Visualization;

namespace AnyCAD.Demo.Graphics
{
    /// <summary>
    /// 数控加工刀路仿真：加载 NC 代码文件（GBK/UTF-8 自动识别），
    /// 显示 1200×2400mm 板材上的全部刀路，并仿真刀具沿刀路移动。
    /// </summary>
    class Simulation_NcViewer : TestCase
    {
        ToolAnimator mAnimator;

        public Simulation_NcViewer()
        {
            Title = "数控加工刀路仿真";
            Description = "加载 NC 代码文件，显示 1200×2400mm 板材刀路并仿真刀具沿刀路移动（GBK 编码自动识别）";
            Tags.Add("仿真");
            Tags.Add("数控加工");
            Tags.Add("刀路");
            Level = 3;
        }

        public override void Run(IRenderView render)
        {
            var document = NcFileParser.Load(GetResourcePath("NCViewer/1200X2400.nc"));
            var scene = NcSceneBuilder.Build(render, document);

            mAnimator = new ToolAnimator(scene);
            mAnimator.Speed = 2.0; // 2 倍速播放，完整刀路约 15 秒
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
