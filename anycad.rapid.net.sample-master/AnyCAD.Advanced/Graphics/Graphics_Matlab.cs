using AnyCAD.Foundation;
using AnyCAD.Simulate;
using System;


namespace AnyCAD.Demo.Graphics
{
    class Graphics_Matlab : TestCase
    {
        public Graphics_Matlab()
        {
            Title = "函数曲面绘制";
            Description = "用Matplot绘制二元函数彩色曲面";
            Tags.Add("显示");
            Tags.Add("函数曲面");
            Level = 2;
        }
        public override void Run(IRenderView render)
        {
            var matplot = Matplot.Create("MyMatlab");

            var xRange = new PlotRange(0, 3.14f * 2, 0.1f);
            var yRange = new PlotRange(0, 3.14f * 2, 0.1f);
            matplot.AddSurface(xRange, yRange, (idxU, idxV, u, v) =>
            {
                double x = u;
                double y = v;
                double z = Math.Sin(u) + Math.Cos(v);

                return new GPnt(x, y, z);
            }, true);

            var node = matplot.Build(ColorMapKeyword.Create(EnumSystemColorMap.Rainbow));
            node.SetPickable(false);
            render.ShowSceneNode(node);
        }
    }
}
