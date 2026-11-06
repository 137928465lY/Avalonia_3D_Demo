using AnyCAD.Foundation;
using AnyCAD.QuickSolid;
using System;
using System.Collections.Generic;

namespace AnyCAD.Demo.Geometry
{
    class Geometry_Unify : TestCase
    {
        public Geometry_Unify()
        {
            Title = "布尔并集";
            Description = "对两组胶囊体做并集与交运算";
            Tags.Add("几何");
            Tags.Add("布尔运算");
            Level = 2;
        }

        public override void Run(IRenderView render)
        {
            GroupSceneNode groupsnA = new GroupSceneNode();
            GroupSceneNode groupsnB = new GroupSceneNode();
            // 构造初始状态
            MeshStandardMaterial mat = MeshStandardMaterial.Create("my-material");
            mat.SetFaceSide(EnumFaceSide.DoubleSide);
            mat.SetOpacity(0.5f);
            mat.SetTransparent(true);

            var cylinder = ShapeBuilder.MakeCylinder(new GAx2(new GPnt(0, 0, 0), new GDir(0, 0, 1)), 150, 500, Math.PI * 2);
            var fillet = FeatureTool.Fillet(cylinder, new Uint32List(new uint[] { 0, 2 }), new DoubleList(new double[] { 150, 150 }));
            var capsule = BrepSceneNode.Create(fillet, null, null);
            capsule.SetFaceMaterial(mat);


            groupsnA.AddNode(capsule);

            cylinder = ShapeBuilder.MakeCylinder(new GAx2(new GPnt(100, 0, 0), new GDir(0, 0, 1)), 150, 500, Math.PI * 2);
            fillet = FeatureTool.Fillet(cylinder, new Uint32List(new uint[] { 0, 2 }), new DoubleList(new double[] { 150, 150 }));
            capsule = BrepSceneNode.Create(fillet, null, null);
            capsule.SetFaceMaterial(mat);


            groupsnB.AddNode(capsule);

            //render.ShowSceneNode(groupsnA);
            //render.ShowSceneNode(groupsnB);



            List<TopoShape> tsList_A = new List<TopoShape>();
            for (var itr = groupsnA.CreateIterator(); itr.More(); itr.Next())
            {
                var current = BrepSceneNode.Cast(itr.Current());
                TopoShape tp = current.GetTopoShape();
                tsList_A.Add(tp);
            }

            List<TopoShape> tsList_B = new List<TopoShape>();
            for (var itr = groupsnB.CreateIterator(); itr.More(); itr.Next())
            {
                var current = BrepSceneNode.Cast(itr.Current());
                TopoShape tp = current.GetTopoShape();
                tsList_B.Add(tp);
            }

            var m0 = MeshStandardMaterial.Create("xx");
            m0.SetColor(ColorTable.Red);
            m0.SetTransparent(true);
            foreach (var item in tsList_A)
            {
                foreach (var item1 in tsList_B)
                {
                    ProcessExtrudeAndCut(item, item1, m0, render);
                }
            }
        }
        void ProcessExtrudeAndCut(TopoShape extrude, TopoShape extrude_Cut, MaterialInstance material, IRenderView mRenderView)
        {
            extrude = BooleanTool.Unify(extrude, true, true, false);
            extrude_Cut = BooleanTool.Unify(extrude_Cut, true, true, false);
            if (extrude != null && extrude_Cut != null)
            {
                TopoShape boolCommon = BooleanTool.Common(extrude_Cut, extrude);
                var node0 = BrepSceneNode.Create(boolCommon, material, null);
                mRenderView.ShowSceneNode(node0);
            }
        }
    }
}
