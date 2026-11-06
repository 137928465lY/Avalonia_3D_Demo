using AnyCAD.Demo;
using AnyCAD.Foundation;
using System.Collections.Generic;
using System;

namespace AnyCAD.Basic.Geometry
{
    internal class Geometry_BooleanCut : TestCase
    {
        public Geometry_BooleanCut()
        {
            Title = "轮廓差集";
            Description = "由多段圆弧轮廓构造平面并做差集。";
            Tags.Add("几何");
            Tags.Add("布尔");
            Level = 1;
        }
        private TopoShape CreateDiaphragm(IRenderView vewer, GPntList outPoints, List<int> arcBoolListOut, GPntList innerPoints, List<int> arcBoolListInner)
        {

            //outPoints.Reverse();
            //arcBoolListOut.Reverse();

            //断面外周边板
            var topoShapeOut = CreateClapPlate(outPoints, arcBoolListOut, 100);
            //vewer.ShowShape(topoShapeOut, ColorTable.LightGreen);
            if (innerPoints != null && innerPoints.Count > 0)
            {
                //内孔板
                //innerPoints.Reverse();
                //arcBoolListInner.Reverse();
                var topoShapeInner = CreateClapPlate(innerPoints, arcBoolListInner, 100);
                //vewer.ShowShape(topoShapeInner, ColorTable.LightCyan);

                //布尔切割
                var diaphragmshape = BooleanTool.Cut(topoShapeOut, topoShapeInner);

                return diaphragmshape;

            }
            return topoShapeOut;

        }

        //创建隔板
        public TopoShape CreateClapPlate(GPntList points, List<int> arcBoolList, double thickness)
        {
            if (points.Count < 2)
                return null;

            var curveList = new TopoShapeList();

            for (int i = 0; i < points.Count - 1; i++)
            {
                if (arcBoolList[i] == 0 && arcBoolList[i + 1] == 0)
                {
                    curveList.Add(CurveBuilder.MakeLine(points[i], points[i + 1]));
                }
                else if (arcBoolList[i] == 0 && arcBoolList[i + 1] == 1)
                {
                    curveList.Add(CurveBuilder.MakeArcBy3Points(points[i], points[i + 2], points[i + 1]));
                }
            }

            //断面周边曲线组成整体
            var contour = ShapeBuilder.MakeCompound(curveList);

            var trsf1 = new GTrsf();
            trsf1.SetRotation(new GAx1(new GPnt(), new GDir(0, 0, 1)), 0 / 180.0 * Math.PI);
            var transFace1 = TransformTool.Transform(contour, trsf1);

            var trsf2 = new GTrsf();
            trsf2.SetTranslation(new GVec(thickness, 0, 0));
            var transFace2 = TransformTool.Transform(transFace1, trsf2);

            var section = new TopoShapeList();
            section.Add(transFace1);
            section.Add(transFace2);
            var topoShape = FeatureTool.Loft(section, true, false);
            return topoShape;
        }

        public override void Run(IRenderView render)
        {

            GPntList OutPoints4 = new();
            GPntList InnerPoints4 = new();
            List<int> ArcBoolList4 = new();
            List<int> ArcBoolInList4 = new();

            OutPoints4.Add(new GPnt(0, -2257.000, 35.000));
            OutPoints4.Add(new GPnt(0, -2281.749, 24.749));
            OutPoints4.Add(new GPnt(0, -2292.000, 0.000));
            OutPoints4.Add(new GPnt(0, -5406.000, 0.000));
            OutPoints4.Add(new GPnt(0, -5419.805, 27.853));
            OutPoints4.Add(new GPnt(0, -5450.330, 33.734));
            OutPoints4.Add(new GPnt(0, -5925.000, 1750.000));
            OutPoints4.Add(new GPnt(0, -6150.000, 2545.500));
            OutPoints4.Add(new GPnt(0, -5865.950, 2551.181));
            OutPoints4.Add(new GPnt(0, -5853.527, 2533.070));
            OutPoints4.Add(new GPnt(0, -5841.104, 2532.178));
            OutPoints4.Add(new GPnt(0, -5791.395, 2333.317));
            OutPoints4.Add(new GPnt(0, -5800.126, 2329.118));
            OutPoints4.Add(new GPnt(0, -5808.858, 2304.213));
            OutPoints4.Add(new GPnt(0, -5773.213, 2258.317));
            OutPoints4.Add(new GPnt(0, -5737.568, 2249.688));
            OutPoints4.Add(new GPnt(0, -5650.455, 2251.430));
            OutPoints4.Add(new GPnt(0, -5615.928, 2261.042));
            OutPoints4.Add(new GPnt(0, -5581.401, 2308.762));
            OutPoints4.Add(new GPnt(0, -5590.708, 2333.001));
            OutPoints4.Add(new GPnt(0, -5600.014, 2337.144));
            OutPoints4.Add(new GPnt(0, -5558.296, 2537.834));
            OutPoints4.Add(new GPnt(0, -5546.263, 2539.069));
            OutPoints4.Add(new GPnt(0, -5534.230, 2557.815));
            OutPoints4.Add(new GPnt(0, -5266.070, 2563.179));
            OutPoints4.Add(new GPnt(0, -5253.647, 2545.068));
            OutPoints4.Add(new GPnt(0, -5241.224, 2544.176));
            OutPoints4.Add(new GPnt(0, -5191.515, 2345.314));
            OutPoints4.Add(new GPnt(0, -5200.246, 2341.116));
            OutPoints4.Add(new GPnt(0, -5208.978, 2316.210));
            OutPoints4.Add(new GPnt(0, -5173.333, 2270.315));
            OutPoints4.Add(new GPnt(0, -5137.688, 2261.685));
            OutPoints4.Add(new GPnt(0, -5050.575, 2263.428));
            OutPoints4.Add(new GPnt(0, -5016.048, 2273.040));
            OutPoints4.Add(new GPnt(0, -4981.521, 2320.760));
            OutPoints4.Add(new GPnt(0, -4990.828, 2344.999));
            OutPoints4.Add(new GPnt(0, -5000.134, 2349.142));
            OutPoints4.Add(new GPnt(0, -4958.416, 2549.832));
            OutPoints4.Add(new GPnt(0, -4946.383, 2551.067));
            OutPoints4.Add(new GPnt(0, -4934.349, 2569.813));
            OutPoints4.Add(new GPnt(0, -4666.190, 2575.176));
            OutPoints4.Add(new GPnt(0, -4653.767, 2557.065));
            OutPoints4.Add(new GPnt(0, -4641.344, 2556.173));
            OutPoints4.Add(new GPnt(0, -4591.635, 2357.312));
            OutPoints4.Add(new GPnt(0, -4600.366, 2353.113));
            OutPoints4.Add(new GPnt(0, -4609.098, 2328.208));
            OutPoints4.Add(new GPnt(0, -4573.453, 2282.313));
            OutPoints4.Add(new GPnt(0, -4537.808, 2273.683));
            OutPoints4.Add(new GPnt(0, -4450.695, 2275.425));
            OutPoints4.Add(new GPnt(0, -4416.168, 2285.038));
            OutPoints4.Add(new GPnt(0, -4381.641, 2332.757));
            OutPoints4.Add(new GPnt(0, -4390.948, 2356.997));
            OutPoints4.Add(new GPnt(0, -4400.254, 2361.139));
            OutPoints4.Add(new GPnt(0, -4358.536, 2561.829));
            OutPoints4.Add(new GPnt(0, -4346.503, 2563.064));
            OutPoints4.Add(new GPnt(0, -4334.469, 2581.811));
            OutPoints4.Add(new GPnt(0, -4066.310, 2587.174));
            OutPoints4.Add(new GPnt(0, -4053.887, 2569.063));
            OutPoints4.Add(new GPnt(0, -4041.464, 2568.171));
            OutPoints4.Add(new GPnt(0, -3991.755, 2369.309));
            OutPoints4.Add(new GPnt(0, -4000.486, 2365.111));
            OutPoints4.Add(new GPnt(0, -4009.218, 2340.206));
            OutPoints4.Add(new GPnt(0, -3973.573, 2294.310));
            OutPoints4.Add(new GPnt(0, -3937.928, 2285.681));
            OutPoints4.Add(new GPnt(0, -3850.815, 2287.423));
            OutPoints4.Add(new GPnt(0, -3816.288, 2297.035));
            OutPoints4.Add(new GPnt(0, -3781.761, 2344.755));
            OutPoints4.Add(new GPnt(0, -3791.068, 2368.994));
            OutPoints4.Add(new GPnt(0, -3800.374, 2373.137));
            OutPoints4.Add(new GPnt(0, -3758.656, 2573.827));
            OutPoints4.Add(new GPnt(0, -3746.623, 2575.062));
            OutPoints4.Add(new GPnt(0, -3734.589, 2593.808));
            OutPoints4.Add(new GPnt(0, -3466.430, 2599.171));
            OutPoints4.Add(new GPnt(0, -3454.007, 2581.060));
            OutPoints4.Add(new GPnt(0, -3441.584, 2580.168));
            OutPoints4.Add(new GPnt(0, -3391.875, 2381.307));
            OutPoints4.Add(new GPnt(0, -3400.606, 2377.109));
            OutPoints4.Add(new GPnt(0, -3409.338, 2352.203));
            OutPoints4.Add(new GPnt(0, -3373.693, 2306.308));
            OutPoints4.Add(new GPnt(0, -3338.048, 2297.678));
            OutPoints4.Add(new GPnt(0, -3250.935, 2299.421));
            OutPoints4.Add(new GPnt(0, -3216.408, 2309.033));
            OutPoints4.Add(new GPnt(0, -3181.881, 2356.752));
            OutPoints4.Add(new GPnt(0, -3191.188, 2380.992));
            OutPoints4.Add(new GPnt(0, -3200.494, 2385.135));
            OutPoints4.Add(new GPnt(0, -3158.776, 2585.824));
            OutPoints4.Add(new GPnt(0, -3146.743, 2587.059));
            OutPoints4.Add(new GPnt(0, -3134.709, 2605.806));
            OutPoints4.Add(new GPnt(0, -2866.550, 2611.169));
            OutPoints4.Add(new GPnt(0, -2854.127, 2593.058));
            OutPoints4.Add(new GPnt(0, -2841.704, 2592.166));
            OutPoints4.Add(new GPnt(0, -2791.995, 2393.305));
            OutPoints4.Add(new GPnt(0, -2800.726, 2389.106));
            OutPoints4.Add(new GPnt(0, -2809.458, 2364.201));
            OutPoints4.Add(new GPnt(0, -2773.813, 2318.305));
            OutPoints4.Add(new GPnt(0, -2738.168, 2309.676));
            OutPoints4.Add(new GPnt(0, -2651.054, 2311.418));
            OutPoints4.Add(new GPnt(0, -2616.528, 2321.030));
            OutPoints4.Add(new GPnt(0, -2582.001, 2368.750));
            OutPoints4.Add(new GPnt(0, -2591.308, 2392.989));
            OutPoints4.Add(new GPnt(0, -2600.614, 2397.132));
            OutPoints4.Add(new GPnt(0, -2558.896, 2597.822));
            OutPoints4.Add(new GPnt(0, -2546.863, 2599.057));
            OutPoints4.Add(new GPnt(0, -2534.829, 2617.803));
            OutPoints4.Add(new GPnt(0, -2291.993, 2622.660));
            OutPoints4.Add(new GPnt(0, -2281.500, 2598.365));
            OutPoints4.Add(new GPnt(0, -2257.000, 2588.360));
            OutPoints4.Add(new GPnt(0, -2257.000, 1823.500));
            OutPoints4.Add(new GPnt(0, -2257.000, 35.000));

            ArcBoolList4.Add(0);
            ArcBoolList4.Add(1);
            ArcBoolList4.Add(0);
            ArcBoolList4.Add(0);
            ArcBoolList4.Add(1);
            ArcBoolList4.Add(0);
            ArcBoolList4.Add(0);
            ArcBoolList4.Add(0);
            ArcBoolList4.Add(0);
            ArcBoolList4.Add(1);
            ArcBoolList4.Add(0);
            ArcBoolList4.Add(0);
            ArcBoolList4.Add(1);
            ArcBoolList4.Add(0);
            ArcBoolList4.Add(1);
            ArcBoolList4.Add(0);
            ArcBoolList4.Add(0);
            ArcBoolList4.Add(1);
            ArcBoolList4.Add(0);
            ArcBoolList4.Add(1);
            ArcBoolList4.Add(0);
            ArcBoolList4.Add(0);
            ArcBoolList4.Add(1);
            ArcBoolList4.Add(0);
            ArcBoolList4.Add(0);
            ArcBoolList4.Add(1);
            ArcBoolList4.Add(0);
            ArcBoolList4.Add(0);
            ArcBoolList4.Add(1);
            ArcBoolList4.Add(0);
            ArcBoolList4.Add(1);
            ArcBoolList4.Add(0);
            ArcBoolList4.Add(0);
            ArcBoolList4.Add(1);
            ArcBoolList4.Add(0);
            ArcBoolList4.Add(1);
            ArcBoolList4.Add(0);
            ArcBoolList4.Add(0);
            ArcBoolList4.Add(1);
            ArcBoolList4.Add(0);
            ArcBoolList4.Add(0);
            ArcBoolList4.Add(1);
            ArcBoolList4.Add(0);
            ArcBoolList4.Add(0);
            ArcBoolList4.Add(1);
            ArcBoolList4.Add(0);
            ArcBoolList4.Add(1);
            ArcBoolList4.Add(0);
            ArcBoolList4.Add(0);
            ArcBoolList4.Add(1);
            ArcBoolList4.Add(0);
            ArcBoolList4.Add(1);
            ArcBoolList4.Add(0);
            ArcBoolList4.Add(0);
            ArcBoolList4.Add(1);
            ArcBoolList4.Add(0);
            ArcBoolList4.Add(0);
            ArcBoolList4.Add(1);
            ArcBoolList4.Add(0);
            ArcBoolList4.Add(0);
            ArcBoolList4.Add(1);
            ArcBoolList4.Add(0);
            ArcBoolList4.Add(1);
            ArcBoolList4.Add(0);
            ArcBoolList4.Add(0);
            ArcBoolList4.Add(1);
            ArcBoolList4.Add(0);
            ArcBoolList4.Add(1);
            ArcBoolList4.Add(0);
            ArcBoolList4.Add(0);
            ArcBoolList4.Add(1);
            ArcBoolList4.Add(0);
            ArcBoolList4.Add(0);
            ArcBoolList4.Add(1);
            ArcBoolList4.Add(0);
            ArcBoolList4.Add(0);
            ArcBoolList4.Add(1);
            ArcBoolList4.Add(0);
            ArcBoolList4.Add(1);
            ArcBoolList4.Add(0);
            ArcBoolList4.Add(0);
            ArcBoolList4.Add(1);
            ArcBoolList4.Add(0);
            ArcBoolList4.Add(1);
            ArcBoolList4.Add(0);
            ArcBoolList4.Add(0);
            ArcBoolList4.Add(1);
            ArcBoolList4.Add(0);
            ArcBoolList4.Add(0);
            ArcBoolList4.Add(1);
            ArcBoolList4.Add(0);
            ArcBoolList4.Add(0);
            ArcBoolList4.Add(1);
            ArcBoolList4.Add(0);
            ArcBoolList4.Add(1);
            ArcBoolList4.Add(0);
            ArcBoolList4.Add(0);
            ArcBoolList4.Add(1);
            ArcBoolList4.Add(0);
            ArcBoolList4.Add(1);
            ArcBoolList4.Add(0);
            ArcBoolList4.Add(0);
            ArcBoolList4.Add(1);
            ArcBoolList4.Add(0);
            ArcBoolList4.Add(0);
            ArcBoolList4.Add(1);
            ArcBoolList4.Add(0);
            ArcBoolList4.Add(0);
            ArcBoolList4.Add(0);

            InnerPoints4.Add(new GPnt(0.000, -4250.000, 1252.000));
            InnerPoints4.Add(new GPnt(0.000, -4285.355, 1237.355));
            InnerPoints4.Add(new GPnt(0.000, -4300.000, 1202.000));
            InnerPoints4.Add(new GPnt(0.000, -4300.000, 650.000));
            InnerPoints4.Add(new GPnt(0.000, -4285.355, 614.645));
            InnerPoints4.Add(new GPnt(0.000, -4250.000, 600.000));
            InnerPoints4.Add(new GPnt(0.000, -3550.000, 600.000));
            InnerPoints4.Add(new GPnt(0.000, -3514.645, 614.645));
            InnerPoints4.Add(new GPnt(0.000, -3500.000, 650.000));
            InnerPoints4.Add(new GPnt(0.000, -3500.000, 1202.000));
            InnerPoints4.Add(new GPnt(0.000, -3514.645, 1237.355));
            InnerPoints4.Add(new GPnt(0.000, -3550.000, 1252.000));
            InnerPoints4.Add(new GPnt(0.000, -4250.000, 1252.000));


            ArcBoolInList4.Add(0);
            ArcBoolInList4.Add(1);
            ArcBoolInList4.Add(0);
            ArcBoolInList4.Add(0);
            ArcBoolInList4.Add(1);
            ArcBoolInList4.Add(0);
            ArcBoolInList4.Add(0);
            ArcBoolInList4.Add(1);
            ArcBoolInList4.Add(0);
            ArcBoolInList4.Add(0);
            ArcBoolInList4.Add(1);
            ArcBoolInList4.Add(0);
            ArcBoolInList4.Add(0);


            var part = CreateDiaphragm(render, OutPoints4, ArcBoolList4, InnerPoints4, ArcBoolInList4);
            render.ShowSceneNode(BrepSceneNode.Create(part, ColorTable.LightGreen, true));
        }
    }
}
