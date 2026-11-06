using AnyCAD.Foundation;
using AnyCAD.Manufacturing;
using System;

namespace AnyCAD.Demo.Graphics
{
    class Simulation_Additive : TestCase
    {
        public Simulation_Additive()
        {
            Title = "3D打印仿真";
            Description = "读取GCode播放打印头沉积动画并支持播放控制";
            Tags.Add("仿真");
            Tags.Add("3D打印");
            Level = 2;
        }
        AdditiveSimulationSession mSimulation;

        public override void Run(IRenderView render)
        {
            // AnimationPlayerSetting 是全局设置，控制动画播放速度和快捷键。
            // 播放倍率用于调整速度快捷键的步进，播放速度范围为 0.25 到 8 倍。
            var animationSetting = AnimationPlayerSetting.Instance();
            animationSetting.SetPlaybackRateFactor(2.0);
            animationSetting.SetMinPlaybackRate(0.25);
            animationSetting.SetMaxPlaybackRate(8.0);
            animationSetting.SetSeekStepMilliseconds(1000.0);

            animationSetting.SetKey(
                EnumAnimationPlayerShortcutAction.TogglePlayback,
                EnumKeyCode.KEY_SPACE);
            animationSetting.SetKey(
                EnumAnimationPlayerShortcutAction.Stop,
                EnumKeyCode.KEY_ESCAPE);
            animationSetting.SetKey(
                EnumAnimationPlayerShortcutAction.SeekBackward,
                EnumKeyCode.KEY_a);
            animationSetting.SetKey(
                EnumAnimationPlayerShortcutAction.SeekForward,
                EnumKeyCode.KEY_d);
            animationSetting.SetKey(
                EnumAnimationPlayerShortcutAction.SlowDown,
                EnumKeyCode.KEY_s);
            animationSetting.SetKey(
                EnumAnimationPlayerShortcutAction.SpeedUp,
                EnumKeyCode.KEY_w);
            animationSetting.SetKey(
                EnumAnimationPlayerShortcutAction.ToggleLoop,
                EnumKeyCode.KEY_l);

            var baseShape = ShapeBuilder.MakeBox(
                new GAx2(new GPnt(0.0, 0.0, -2.0), GDir.D.Z),
                120.0, 120.0, 2.0);
            var headShape = ShapeBuilder.MakeCone(GP.XOY(), 1.2, 0.25, 8.0, 0.0);

            var baseMaterial = MeshPhongMaterial.Create("test.additive.base");
            baseMaterial.SetColor(new Vector3(0.28f, 0.30f, 0.34f));
            var headMaterial = MeshPhongMaterial.Create("test.additive.head");
            headMaterial.SetColor(new Vector3(0.88f, 0.88f, 0.90f));

            var baseNode = BrepSceneNode.Create(baseShape, baseMaterial, null);
            var headNode = BrepSceneNode.Create(headShape, headMaterial, null);
            baseNode.SetChildrenPickable(false);
            headNode.SetChildrenPickable(false);

            var heads = new AdditiveHeadDefinitionList();
            heads.Add(new AdditiveHeadDefinition
            {
                Id = 0,
                DisplayNode = headNode,
                DepositWidth = 0.4,
                DepositHeight = 0.1,
                DepositColor = new Vector3(1.0f, 0.45f, 0.1f)
            });

            var program = new AdditiveProgram { Heads = heads };
            var fileName = GetResourcePath("gcode/cura_3D_printer.gcode");
            if (!program.ReadGCodeFile(new AnyCAD.Foundation.Path(fileName)))
            {
                DialogUtil.ShowMessageBox("警告", "无法读取GCode文件: " + fileName);
                return;
            }

            mSimulation = new AdditiveSimulationSession();
            mSimulation.SetViewer(render.Viewer);
            mSimulation.Start(baseNode, program);
            render.EnableAnimation(true);

            DialogUtil.ShowMessageBox(
                "增材仿真动画控制",
                "空格：播放/暂停\n" +
                "Esc：停止\n" +
                "a/d：后退/前进 1 秒\n" +
                "s/w：降低/提高播放速度\n" +
                "L：切换循环播放\n" +
                "速度范围：0.25～8 倍，速度键步进倍率：2");
        }

        public override void Exit(IRenderView render)
        {
            render.EnableAnimation(false);
            if (mSimulation != null)
            {
                mSimulation.Close();
                mSimulation.SetViewer(null);
                mSimulation.Dispose();
                mSimulation = null;
            }

            AnimationPlayerSetting.Instance().Reset();
        }
    }
}
