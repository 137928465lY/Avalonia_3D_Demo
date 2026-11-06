using AnyCAD.Foundation;
using AnyCAD.Manufacturing;
using System;

namespace AnyCAD.Demo.Graphics
{
    class Simulation_Mill : TestCase
    {
        public Simulation_Mill()
        {
            Title = "铣削加工仿真";
            Description = "模拟铣刀按刀路切削毛坯并播放动画";
            Tags.Add("仿真");
            Tags.Add("铣削");
            Level = 2;
        }
        MillSimulationSession mSimulation;

        public override void Run(IRenderView render)
        {
            var stockMaterial = MeshPhongMaterial.Create("test.mill.stock");
            stockMaterial.SetColor(new Vector3(0.85f));
            var toolMaterial = MeshPhongMaterial.Create("test.mill.tool");
            toolMaterial.SetColor(new Vector3(0.25f, 0.45f, 0.75f));

            var stockShape = ShapeBuilder.MakeBox(GP.XOY(), 280, 100, 40);
            var stock = BrepSceneNode.Create(stockShape, stockMaterial, null);
            stock.SetChildrenPickable(false);

            var toolShape = ShapeBuilder.MakeCylinder(GP.XOY(), 3, 70, 0);
            var tool = BrepSceneNode.Create(toolShape, toolMaterial, null);
            tool.SetChildrenPickable(false);

            var tools = new MillToolDefinitionList();
            tools.Add(new MillToolDefinition
            {
                Id = 1,
                ToolNode = tool,
                CutColor = new Vector3(1.0f, 0.0f, 0.0f)
            });

            var motions = new ManufacturingMotionList();

            const double pathZ = 32.0;
            AddStroke(motions, 1, pathZ,
                new Vector3d(15.0, 20.0, pathZ),
                new Vector3d(30.0, 80.0, pathZ),
                new Vector3d(45.0, 20.0, pathZ));
            AddStroke(motions, 1, pathZ,
                new Vector3d(22.0, 50.0, pathZ),
                new Vector3d(38.0, 50.0, pathZ));

            AddStroke(motions, 1, pathZ,
                new Vector3d(55.0, 20.0, pathZ),
                new Vector3d(55.0, 80.0, pathZ),
                new Vector3d(85.0, 20.0, pathZ),
                new Vector3d(85.0, 80.0, pathZ));

            AddStroke(motions, 1, pathZ,
                new Vector3d(95.0, 80.0, pathZ),
                new Vector3d(110.0, 50.0, pathZ),
                new Vector3d(125.0, 80.0, pathZ));
            AddStroke(motions, 1, pathZ,
                new Vector3d(110.0, 50.0, pathZ),
                new Vector3d(110.0, 20.0, pathZ));

            motions.Add(new ManufacturingMotion
            {
                Type = EnumManufacturingMotionType.ChangeTool,
                ToolId = 1
            });
            motions.Add(new ManufacturingMotion
            {
                Type = EnumManufacturingMotionType.SetPose,
                Position = new Vector3d(170.0, 80.0, pathZ)
            });
            motions.Add(new ManufacturingMotion
            {
                Type = EnumManufacturingMotionType.MoveLinear,
                Position = new Vector3d(162.0, 80.0, pathZ),
                LinearSpeed = 20.0,
                IsTravel = false
            });
            motions.Add(new ManufacturingMotion
            {
                Type = EnumManufacturingMotionType.MoveArcCCW,
                Position = new Vector3d(162.0, 20.0, pathZ),
                ArcCenterOffset = new Vector3d(0.0, -30.0, 0.0),
                LinearSpeed = 20.0,
                IsTravel = false
            });
            motions.Add(new ManufacturingMotion
            {
                Type = EnumManufacturingMotionType.MoveLinear,
                Position = new Vector3d(170.0, 20.0, pathZ),
                LinearSpeed = 20.0,
                IsTravel = false
            });

            AddStroke(motions, 1, pathZ,
                new Vector3d(180.0, 20.0, pathZ),
                new Vector3d(195.0, 80.0, pathZ),
                new Vector3d(210.0, 20.0, pathZ));
            AddStroke(motions, 1, pathZ,
                new Vector3d(187.0, 50.0, pathZ),
                new Vector3d(203.0, 50.0, pathZ));

            motions.Add(new ManufacturingMotion
            {
                Type = EnumManufacturingMotionType.ChangeTool,
                ToolId = 1
            });
            motions.Add(new ManufacturingMotion
            {
                Type = EnumManufacturingMotionType.SetPose,
                Position = new Vector3d(220.0, 20.0, pathZ)
            });
            motions.Add(new ManufacturingMotion
            {
                Type = EnumManufacturingMotionType.MoveLinear,
                Position = new Vector3d(220.0, 80.0, pathZ),
                LinearSpeed = 20.0,
                IsTravel = false
            });
            motions.Add(new ManufacturingMotion
            {
                Type = EnumManufacturingMotionType.MoveLinear,
                Position = new Vector3d(235.0, 80.0, pathZ),
                LinearSpeed = 20.0,
                IsTravel = false
            });
            motions.Add(new ManufacturingMotion
            {
                Type = EnumManufacturingMotionType.MoveArcCW,
                Position = new Vector3d(235.0, 20.0, pathZ),
                ArcCenterOffset = new Vector3d(0.0, -30.0, 0.0),
                LinearSpeed = 20.0,
                IsTravel = false
            });
            motions.Add(new ManufacturingMotion
            {
                Type = EnumManufacturingMotionType.MoveLinear,
                Position = new Vector3d(220.0, 20.0, pathZ),
                LinearSpeed = 20.0,
                IsTravel = false
            });

            var program = new MillProgram
            {
                Tools = tools,
                Motions = motions
            };

            mSimulation = new MillSimulationSession();
            mSimulation.SetViewer(render.Viewer);
            mSimulation.Start(stock, program);
            render.EnableAnimation(true);
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
        }

        static void AddStroke(ManufacturingMotionList motions, int toolId, double z, params Vector3d[] points)
        {
            if (points.Length == 0)
                return;

            motions.Add(new ManufacturingMotion
            {
                Type = EnumManufacturingMotionType.ChangeTool,
                ToolId = toolId
            });
            motions.Add(new ManufacturingMotion
            {
                Type = EnumManufacturingMotionType.SetPose,
                Position = new Vector3d(points[0].x, points[0].y, z)
            });

            for (var i = 1; i < points.Length; ++i)
            {
                var point = points[i];
                motions.Add(new ManufacturingMotion
                {
                    Type = EnumManufacturingMotionType.MoveLinear,
                    Position = new Vector3d(point.x, point.y, z),
                    LinearSpeed = 20.0,
                    IsTravel = false
                });
            }
        }
    }
}
