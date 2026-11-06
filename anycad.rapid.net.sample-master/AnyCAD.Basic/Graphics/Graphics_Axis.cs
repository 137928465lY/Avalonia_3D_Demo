using AnyCAD.Foundation;

namespace AnyCAD.Demo.Graphics
{
    class Graphics_Axis : TestCase
    {
        public Graphics_Axis()
        {
            Title = "坐标轴标注";
            Description = "射线与球面求交并在交点放置坐标轴小部件";
            Tags.Add("显示");
            Tags.Add("射线");
            Level = 1;
        }
        public override void Run(IRenderView render)
        {
            Sphere sp = new Sphere(Vector3d.Zero, 10);

            var widget = AxisWidget.Create(0.05f, new Vector3(0.4f));

            for (int ii = -5; ii < 5; ++ii)
                for (int jj = -5; jj < 5; ++jj)
                {
                    Ray ray = new Ray(new Vector3d(ii, jj, 20), new Vector3d(0, 0, -1));
                    var rst = ray.intersects(sp);
                    if (!rst.first)
                    {
                        continue;
                    }

                    var pt = ray.getPoint(rst.second);
                    var n = pt.normalized();

                    var trf = Matrix4d.makeTranslation(pt) * Matrix4d.makeRotation(Vector3d.UNIT_Z, n);

                    var ax = AxisWidget.Cast(widget.Clone());
                    ax.ShowArrow('z', false);
                    ax.ShowText('z', false);
                    ax.SetTransform(trf);

                    render.ShowSceneNode(ax);

                }
        }
    }
}
