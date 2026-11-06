using AnyCAD.Demo;
using AnyCAD.Foundation;
using AnyCAD.QuickSolid;
namespace AnyCAD.Test
{

    class Geometry_ProjectionTestCase : TestCase
    {
        public Geometry_ProjectionTestCase()
        {
            Title = "曲面投影";
            Description = "将线框投影到曲面上并取法向点";
            Tags.Add("几何");
            Tags.Add("投影");
            Level = 2;
        }
        public void Run2(IRenderView render)
        {
            var cylinder = ShapeBuilder.MakeCylinder(GP.XOY(), 10, 10, 0);
            var line = CurveBuilder.MakeLine(new GPnt(10, 0, 0), new GPnt(0, 10, 5));
            render.ShowSceneNode(BrepSceneNode.Create(cylinder, ColorTable.Blue, true));

            var faces = cylinder.GetChildren(EnumTopoShapeType.Topo_FACE);
            foreach (var face in faces)
            {
                ParametricSurface surface = new ParametricSurface(face);
                if (surface.GetSurfaceType() != EnumSurfaceType.SurfaceType_Plane)
                {
                    var prjWire = ProjectionTool.ProjectOnSurface(line, face);
                    if (prjWire != null)
                    {
                       var vertexList = prjWire.GetChildren(EnumTopoShapeType.Topo_VERTEX);
                       foreach(var vertex in vertexList)
                        {
                            ExtremaPointSurface extrema = new ExtremaPointSurface();
                            if(extrema.Initialize(face, vertex))
                            {
                                var len = extrema.GetPointCount();
                                for (int ii=0; ii<len; ++ii)
                                {
                                    var position = extrema.GetPoint(ii);
                                    var uv = extrema.GetParameter(ii);
                                    var dir = surface.GetNormal(uv.X(), uv.Y());

                                    ArrowWidget arrow = ArrowWidget.Create(0.2f, 1.0f, null);
                                    arrow.SetLocation(Vector3.From(position), Vector3.From(dir));

                                    render.ShowSceneNode(arrow);

                                    break;
                                }
                            }
                        }


                        render.ShowSceneNode(BrepSceneNode.Create(prjWire, ColorTable.Red, true));
                    }
                }
            }
        }

        public void Run1(IRenderView render)
        {
            //将textbox中的数据读入
            double F1 = 4;
            double H = 0.2;
            double W = 2.4;
            double L = 3;
            double wr1 = 0.8;
            double lr1 = 1;
            int wc1 = 3;
            int lc1 = 4;

            //得到有关锯齿边的变量
            int lgap = 120;
            int wgap = 120;
            //长度方向上
            double half_length = L / 2;
            double gap = L / lgap;
            double temp = half_length / lc1;
            double start_data = -gap * lc1 / 2 + temp;
            double[] l = new double[lc1];
            double sum = 0;
            for (int i = 0; i < lc1 - 1; i++)
            {
                l[i] = start_data + gap * (i - 1);
                sum = sum + l[i];
            }
            l[lc1 - 1] = half_length - sum;
            //宽度方向上
            double half_width = W / 2;
            gap = W / wgap;
            temp = half_width / wc1;
            start_data = -gap * wc1 / 2 + temp;
            double[] w = new double[wc1];
            sum = 0;
            for (int i = 0; i < wc1 - 1; i++)
            {
                w[i] = start_data + gap * (i - 1);
                sum = sum + w[i];
            }
            w[wc1 - 1] = half_width - sum;

            /***********************我是分割线，构建宽度渐变直边齿**********************/

            //建立原点，x轴，y轴
            GPnt origin = new GPnt(0, 0, 10);// GP.Origin();
            GDir DX = GP.DX();
            GDir DY = GP.DY();
            GAx2 gax = new GAx2(origin, DX);
            GAx2 gay = new GAx2(origin, DY);

            //建立锯齿边
            //[[对于长边]]
            GAx1 Y = new GAx1(GP.Origin(), GP.DY());
            GPnt P = new GPnt(0, H + wr1 + W / 2, 0);
            GAx1 XP = new GAx1(P, GP.DX());
            sum = 0;
            TopoShape[] sort = new TopoShape[8 * (wc1 + lc1)];
            TopoShapeList shapes = new TopoShapeList();
            for (int i = 0; i < lc1; i++)
            {
                sum = sum + l[i];
                GPnt one = new GPnt(sum - l[i], H + W + wr1, 0);
                GPnt two = new GPnt(sum, H + W + wr1, 0);
                GPnt three = new GPnt(0 + sum, H + W + 2 * wr1, 0);

                TopoShape line1 = CurveBuilder.MakeLine(three, one);
                sort[2 * i] = line1;
                TopoShape line2 = CurveBuilder.MakeLine(two, three);
                sort[2 * i + 1] = line2;

                var line11 = TransformTool.Mirror(line1, XP);
                sort[4 * wc1 + 4 * lc1 - 2 * i - 1] = line11;
                var line21 = TransformTool.Mirror(line2, XP);
                sort[4 * wc1 + 4 * lc1 - 2 * i - 2] = line21;

                var line12 = TransformTool.Mirror(line1, Y);
                sort[8 * wc1 + 8 * lc1 - 2 * i - 1] = line12;
                var line22 = TransformTool.Mirror(line2, Y);
                sort[8 * wc1 + 8 * lc1 - 2 * i - 2] = line22;

                var line111 = TransformTool.Mirror(line11, Y);
                sort[4 * wc1 + 4 * lc1 + 2 * i] = line111;
                var line211 = TransformTool.Mirror(line21, Y);
                sort[4 * wc1 + 4 * lc1 + 2 * i + 1] = line211;
            }

            //[[对于短边]]
            sum = 0;
            for (int i = 0; i < wc1; i++)
            {
                sum = sum + w[i];

                GPnt one = new GPnt(L / 2, H + wr1 + W / 2 + sum - w[i], 0);
                GPnt two = new GPnt(L / 2, H + wr1 + W / 2 + sum, 0);
                GPnt three = new GPnt(L / 2 + lr1, H + wr1 + W / 2 + sum, 0);

                TopoShape line1 = CurveBuilder.MakeLine(three, one);
                sort[2 * wc1 + 2 * lc1 - 2 * i - 1] = line1;
                TopoShape line2 = CurveBuilder.MakeLine(two, three);
                sort[2 * wc1 + 2 * lc1 - 2 * i - 2] = line2;

                var line11 = TransformTool.Mirror(line1, XP);
                sort[2 * wc1 + 2 * lc1 + 2 * i] = line11;
                var line21 = TransformTool.Mirror(line2, XP);
                sort[2 * wc1 + 2 * lc1 + 2 * i + 1] = line21;

                var line12 = TransformTool.Mirror(line1, Y);
                sort[6 * wc1 + 6 * lc1 + 2 * i] = line12;
                var line22 = TransformTool.Mirror(line2, Y);
                sort[6 * wc1 + 6 * lc1 + 2 * i + 1] = line22;

                var line111 = TransformTool.Mirror(line11, Y);
                sort[6 * wc1 + 6 * lc1 - 2 * i - 1] = line111;
                var line211 = TransformTool.Mirror(line21, Y);
                sort[6 * wc1 + 6 * lc1 - 2 * i - 2] = line211;
            }

            for (int i = 0; i < 8 * lc1 + 8 * wc1 - 1; i++)
            {
                shapes.Add(sort[i]);
            }
            GPnt one1 = new GPnt(0, H + W + wr1, 0);
            GPnt three1 = new GPnt(-l[0], H + W + 2 * wr1, 0);
            TopoShape line56 = CurveBuilder.MakeLine(three1, one1);

            shapes.Add(line56);
            var wire = CurveBuilder.MakeWire(shapes);
            render.ShowSceneNode(BrepSceneNode.Create(wire, ColorTable.Green, true));

            //构建一个抛物面
            GParab parab = new GParab(gax, F1);
            GParab parab1 = new GParab(gay, F1);
            TopoShape parablic = CurveBuilder.MakeParab(parab, 0, 6);
            TopoShape circle = CurveBuilder.MakeCircle(origin, 8, GP.DZ());
            var paraShape = FeatureTool.Sweep(parablic, circle, EnumGeomFillTrihedron.ConstantNormal);
            //render.ShowShape(paraShape, Vector3.Red);

            var projWire = ProjectionTool.ProjectOnShape(wire, paraShape, new GDir(0, 0, -1));
            render.ShowSceneNode(BrepSceneNode.Create(projWire, ColorTable.Green, true));

            var faces = paraShape.GetChildren(EnumTopoShapeType.Topo_FACE);

            foreach (var face in faces)
            {
                var ss = SurfaceBuilder.MakeSurface(face, projWire);
                render.ShowSceneNode(BrepSceneNode.Create(ss, ColorTable.Blue, true));
            }
        }
        public override void Run(IRenderView render)
        {
            Run1(render);
        }
        public void Run0(IRenderView render)
        {
            GPnt one = new GPnt(0, 0, -5);
            GPnt two = new GPnt(2, 0, -5);
            GPnt three = new GPnt(1, 1, -5);
            TopoShape line1 = CurveBuilder.MakeLine(one, three);
            TopoShape line2 = CurveBuilder.MakeLine(two, three);
            TopoShape line3 = CurveBuilder.MakeLine(two, one);
            var shapes = new TopoShapeList();
            shapes.Add(line1);
            shapes.Add(line2);
            shapes.Add(line3);
            var wire = CurveBuilder.MakeWire(shapes);

            GAx2 gax = new GAx2(GP.Origin(), GP.DX());
            GAx2 gay = new GAx2(GP.Origin(), GP.DY());
            double F1 = 4;
            GParab parab = new GParab(gax, F1);
            GParab parab1 = new GParab(gay, F1);
            TopoShape parablic = CurveBuilder.MakeParab(parab, 0, 6);
            TopoShape circle = CurveBuilder.MakeCircle(GP.Origin(), 8, GP.DZ());
            var paraShape = FeatureTool.Sweep(parablic, circle, EnumGeomFillTrihedron.ConstantNormal);

            
            var projWire = ProjectionTool.ProjectOnShape(wire, paraShape, new GDir(0,0,-1));


            var faces = paraShape.GetChildren(EnumTopoShapeType.Topo_FACE);
            foreach (var face in faces)
            {
                var ss = SurfaceBuilder.MakeSurface(face, projWire);
                render.ShowSceneNode(BrepSceneNode.Create(ss, ColorTable.Red, true));
            }

            render.ShowSceneNode(BrepSceneNode.Create(wire, ColorTable.LightGrey, true));
            render.ShowSceneNode(BrepSceneNode.Create(paraShape, ColorTable.LightGray, true));
        }
    }
}
