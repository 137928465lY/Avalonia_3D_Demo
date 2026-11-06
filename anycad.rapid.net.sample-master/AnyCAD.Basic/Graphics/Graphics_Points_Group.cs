using AnyCAD.Foundation;
using System;



namespace AnyCAD.Demo.Graphics
{
    class Graphics_PointsGroup : TestCase
    {
        public Graphics_PointsGroup()
        {
            Title = "点组与选择";
            Description = "将多个点加入场景组并演示选中项";
            Tags.Add("显示");
            Tags.Add("拾取");
            Level = 1;
        }
        public override void Run(IRenderView render)
        {
            
            // prepare points data
            const int COUNT = 10;

            var material = PointsMaterial.Create("points-material");
            material.SetPointSize(4.0f);
            material.SetColor(ColorTable.IndianRed);

            var group = new GroupSceneNode();
            PrimitiveSceneNode node = null;
            Random random = new Random();
            for (int i = 0; i < COUNT; i++)
            {

                float x = 2000 * (float)random.NextDouble() - 1000;
                float y = 2000 * (float)random.NextDouble() - 1000;
                float z = 2000 * (float)random.NextDouble() - 1000;

                node = new PrimitiveSceneNode(GeometryBuilder.AtomPoint(), material);
                node.SetTransform(Matrix4d.makeTranslation(x, y, z));

                group.AddNode(node);
            }

            render.ShowSceneNode(group);


            var picker = new PickedItem(node, node.GetBoundingBox().getCenter());

            render.ViewContext.GetSelectionManager().GetSelection().Add(picker, true);
        }
    }
}
