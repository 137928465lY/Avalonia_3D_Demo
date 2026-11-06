using AnyCAD.Foundation;

namespace AnyCAD.Demo.Graphics
{
    class MyDistanceMeasureEditor : DistanceMeasureEditor
    {
        public MyDistanceMeasureEditor()
        {
        }
        public override bool Apply(ViewContext ctx)
        {
            var node = GetNode();

            // 获取测量结果

            return base.Apply(ctx);
        }
    }

    class Interaction_Measure : TestCase
    {
        public Interaction_Measure()
        {
            Title = "距离测量";
            Description = "加载模型并用测量编辑器拾取两点测距";
            Tags.Add("交互");
            Tags.Add("测量");
            Level = 2;
        }
        public override void Run(IRenderView render)
        {             
            var shape = ShapeIO.Open(GetResourcePath("HP004-FS.stp"));
            if (shape == null)
                return;
            render.ShowSceneNode(BrepSceneNode.Create(shape, ColorTable.AliceBlue, true));

            render.SetEditor(new MyDistanceMeasureEditor());
        }

        public override void Exit(IRenderView render)
        {
            render.SetEditor(null);
        }
    }
}
