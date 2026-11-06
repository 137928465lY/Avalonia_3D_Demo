using AnyCAD.Foundation;
using System;

namespace AnyCAD.Demo.Graphics
{
    class Graphics_LineStyle : TestCase
    {
        public Graphics_LineStyle()
        {
            Title = "自定义线型";
            Description = "用点划组合构建自定义虚线线型材质";
            Tags.Add("显示");
            Tags.Add("线条");
            Level = 1;
        }
        public override void Run(IRenderView render)
        {
            Random random = new Random();
            for(int ii=0; ii<10; ii++)
            {
                LinePatternBuilder builder = new LinePatternBuilder();
                builder.Add(EnumLineStyleComponentType.DASH, random.Next(5, 30));
                double space = random.Next(3, 10);
                builder.Add(EnumLineStyleComponentType.SPACE, space);
                for(int jj=0; jj<random.Next(1, 5); ++jj)
                {
                    builder.Add(EnumLineStyleComponentType.DOT, 1);
                    builder.Add(EnumLineStyleComponentType.SPACE, 1);
                }
                builder.Add(EnumLineStyleComponentType.SPACE, space-1);

                LinePatternMaterial material = LinePatternMaterial.Create("my.dashedline");
                material.SetPattern(builder.CreateTexture());
                //material.SetLineWidth(2);
                var node = PrimitiveSceneNode.Create(GeometryBuilder.CreateLine(new Vector3(0, 10 + ii, 0), new Vector3(10, 10 + ii, 0)), material);
                node.SetColor(ColorTable.RandomColor());
                render.ShowSceneNode(node);           
            }
        }

    }
}
