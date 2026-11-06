using AnyCAD.Foundation;
using AnyCAD.Simulate;
using System;

namespace AnyCAD.Demo.Graphics
{
    class Graphics_LissajousTable : TestCase
    {
        public Graphics_LissajousTable()
        {
            Title = "利萨如曲线";
            Description = "网格排列的利萨如曲线动态生长动画";
            Tags.Add("显示");
            Tags.Add("动画");
            Level = 1;
        }
        const int GRID_SIZE = 7;
        const float CELL_SIZE = 60;
        const float CURVE_SIZE = 40;
        const float MARGIN = 40;

        BufferGeometry mCurveGeometry;
        float mTime = 0;
        float mProgress = 0;

        public override void Run(IRenderView render)
        {
            CreateDynamicCurves(render);
            render.EnableAnimation(true);
        }

        void CreateDynamicCurves(IRenderView render)
        {
            int segments = 100;
            // int curvesPerRow = GRID_SIZE; // unused, removed
            int totalCurves = GRID_SIZE * GRID_SIZE;
            int pointsPerCurve = segments + 1;
            int totalPoints = totalCurves * pointsPerCurve;

            float startX = -GRID_SIZE * CELL_SIZE / 2 + MARGIN;
            float startY = GRID_SIZE * CELL_SIZE / 2 - MARGIN;

            // 创建顶点缓冲区
            Float32Buffer vertices = new Float32Buffer(0);
            vertices.Reserve((uint)(totalPoints * 3));

            // 初始化所有曲线顶点
            for (int a = 1; a <= GRID_SIZE; a++)
            {
                for (int b = 1; b <= GRID_SIZE; b++)
                {
                    float centerX = startX + b * CELL_SIZE;
                    float centerY = startY - a * CELL_SIZE;
                    for (int i = 0; i <= segments; i++)
                    {
                        float t = (float)(i * 2 * Math.PI / segments);
                        float x = centerX + (float)Math.Sin(a * t) * CURVE_SIZE / 2;
                        float y = centerY + (float)Math.Sin(b * t) * CURVE_SIZE / 2;
                        vertices.Append(x, y, 0);
                    }
                }
            }

            // 创建索引缓冲区 - 使用LINES模式，需要定义每个线段的两个顶点索引
            Uint32Buffer indices = new Uint32Buffer(0);
            for (int curveIdx = 0; curveIdx < totalCurves; curveIdx++)
            {
                uint baseIdx = (uint)(curveIdx * pointsPerCurve);
                for (int i = 0; i < segments; i++)
                {
                    indices.Append(baseIdx + (uint)i);
                    indices.Append(baseIdx + (uint)i + 1);
                }
            }

            // 创建几何体
            mCurveGeometry = new BufferGeometry(EnumPrimitiveType.LINES);
            mCurveGeometry.AddAttribute(EnumAttributeSemantic.Position, EnumAttributeComponents.Three, vertices);
            mCurveGeometry.SetIndex(indices);

            // 创建颜色属性
            ColorLookupTable colorTable = new ColorLookupTable();
            colorTable.SetMaxValue(totalCurves);
            colorTable.SetColorMap(ColorMapKeyword.Create(EnumSystemColorMap.Rainbow));

            Float32Buffer colors = new Float32Buffer(0);
            colors.Reserve((uint)(totalPoints * 3));

            for (int a = 1; a <= GRID_SIZE; a++)
            {
                for (int b = 1; b <= GRID_SIZE; b++)
                {
                    var clr = colorTable.GetColor((float)((a - 1) * GRID_SIZE + (b - 1)));
                    for (int i = 0; i <= segments; i++)
                    {
                        colors.Append(clr.x, clr.y, clr.z);
                    }
                }
            }
            mCurveGeometry.AddAttribute(EnumAttributeSemantic.Color, EnumAttributeComponents.Three, colors);

            var curveMaterial = BasicMaterial.Create("lissajous-curve");
            curveMaterial.GetTemplate().SetVertexColors(true);

            var node = new PrimitiveSceneNode(mCurveGeometry, curveMaterial);
            render.ShowSceneNode(node);
        }

        public override void Exit(IRenderView render)
        {
            render.EnableAnimation(false);
        }

        public override void Animation(IRenderView render, float time)
        {
            mTime = time;

            var position = mCurveGeometry.GetAttribute(0);
            position.SetDataUsage(EnumBufferDataUsage.DYNAMIC_DRAW);
            var positions = new Float32Array(position.GetData());

            float startX = -GRID_SIZE * CELL_SIZE / 2 + MARGIN;
            float startY = GRID_SIZE * CELL_SIZE / 2 - MARGIN;
            int segments = 100;
            int pointsPerCurve = segments + 1;
            uint idx = 0;

            // 计算当前进度（0-1），所有曲线同时成长
            mProgress = (time % 8) / 8;

            for (int a = 1; a <= GRID_SIZE; a++)
            {
                for (int b = 1; b <= GRID_SIZE; b++)
                {
                    float centerX = startX + b * CELL_SIZE;
                    float centerY = startY - a * CELL_SIZE;

                    for (int i = 0; i <= segments; i++)
                    {
                        float t = (float)(i * 2 * Math.PI / segments);
                        
                        // 根据全局进度控制曲线成长
                        if ((float)i / segments <= mProgress)
                        {
                            float x = centerX + (float)Math.Sin(a * t + mTime) * CURVE_SIZE / 2;
                            float y = centerY + (float)Math.Sin(b * t) * CURVE_SIZE / 2;
                            positions.SetValue(idx * 3, x);
                            positions.SetValue(idx * 3 + 1, y);
                            positions.SetValue(idx * 3 + 2, 0);
                        }
                        else
                        {
                            // 隐藏未完成的部分
                            positions.SetValue(idx * 3, float.NaN);
                            positions.SetValue(idx * 3 + 1, float.NaN);
                            positions.SetValue(idx * 3 + 2, float.NaN);
                        }
                        idx++;
                    }
                }
            }

            position.RequestUpdate();
            mCurveGeometry.RequestUpdate();
            render.RequestDraw();
        }
    }
}
