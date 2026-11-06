using AnyCAD.Foundation;

namespace AnyCAD.Demo.Graphics
{
    class Graphics_FaceStyle : TestCase
    {
        public Graphics_FaceStyle()
        {
            Title = "填充图案样式";
            Description = "枚举展示各类面填充图案与边框效果";
            Tags.Add("显示");
            Tags.Add("材质");
            Level = 1;
        }
        public override void Run(IRenderView render)
        {
            for(EnumFillPatternStyle i = 0; i <= EnumFillPatternStyle.SlopeCrossLines; i++)
            {
                var material = MeshPatternMaterial.Create("my.facePattern");
                material.SetFillPatternStyle(i);
                material.SetColor(ColorTable.RandomColor());

                var face = GeometryBuilder.CreateRectangle(true);
                var node = PrimitiveSceneNode.Create(face, material);
                int x = (int)i;
                if(x < 5)
                {
                    node.SetTransform(Matrix4d.makeTranslate(x + 1, 1, 0));
                }
                else
                {
                    node.SetTransform(Matrix4d.makeTranslate(x + 1 - 5, 2, 0));
                }


                render.ShowSceneNode(node);

                var border = GeometryBuilder.CreateRectangle(false);
                var borderNode = PrimitiveSceneNode.Create(border, null);
                borderNode.SetColor(ColorTable.RandomColor());
                borderNode.SetTransform(node.GetTransform());
                render.ShowSceneNode(borderNode);
            }

        }

    }
}
