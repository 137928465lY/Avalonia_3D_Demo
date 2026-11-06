using AnyCAD.Foundation;
using AnyCAD.Simulate;
using System;

namespace AnyCAD.Demo.Graphics
{
    class Graphics_ModMultiplication : TestCase
    {
        public Graphics_ModMultiplication()
        {
            Title = "模乘花结";
            Description = "圆周点模乘连线随参数演变心形图案";
            Tags.Add("显示");
            Tags.Add("动画");
            Level = 1;
        }
        BufferGeometry mGeometry;
        float mTime = 0;
        const int numPoints = 200;
        const float radius = 100.0f;

        public override void Run(IRenderView render)
        {
            CreateGeometry(render);
            render.EnableAnimation(true);
        }

        void CreateGeometry(IRenderView render)
        {
            // 计算圆周上的点位置
            float[] px = new float[numPoints];
            float[] py = new float[numPoints];

            for (int i = 0; i < numPoints; i++)
            {
                float angle = (float)(2 * Math.PI * i / numPoints);
                px[i] = (float)Math.Cos(angle) * radius;
                py[i] = (float)Math.Sin(angle) * radius;
            }

            // 创建顶点缓冲区 - 每条线两个顶点
            int totalVertices = numPoints * 2;
            Float32Buffer vertices = new Float32Buffer(0);
            vertices.Reserve((uint)(totalVertices * 3));

            // 初始 t=1
            for (int i = 0; i < numPoints; i++)
            {
                int j = (i * 1) % numPoints;
                vertices.Append(px[i], py[i], 0);
                vertices.Append(px[j], py[j], 0);
            }

            // 创建索引缓冲区
            Uint32Buffer indices = new Uint32Buffer(0);
            for (int i = 0; i < numPoints; i++)
            {
                indices.Append((uint)(i * 2));
                indices.Append((uint)(i * 2 + 1));
            }

            // 创建几何体
            mGeometry = new BufferGeometry(EnumPrimitiveType.LINES);
            mGeometry.AddAttribute(EnumAttributeSemantic.Position, EnumAttributeComponents.Three, vertices);
            mGeometry.SetIndex(indices);

            // 创建颜色属性 - 彩虹渐变
            ColorLookupTable colorTable = new ColorLookupTable();
            colorTable.SetMaxValue(numPoints);
            colorTable.SetColorMap(ColorMapKeyword.Create(EnumSystemColorMap.Rainbow));

            Float32Buffer colors = new Float32Buffer(0);
            colors.Reserve((uint)(totalVertices * 3));

            for (int i = 0; i < numPoints; i++)
            {
                var clr = colorTable.GetColor((float)i);
                colors.Append(clr.x, clr.y, clr.z);
                colors.Append(clr.x, clr.y, clr.z);
            }
            mGeometry.AddAttribute(EnumAttributeSemantic.Color, EnumAttributeComponents.Three, colors);

            var material = BasicMaterial.Create("mod-mult");
            material.GetTemplate().SetVertexColors(true);

            var node = new PrimitiveSceneNode(mGeometry, material);
            render.ShowSceneNode(node);
        }

        public override void Exit(IRenderView render)
        {
            render.EnableAnimation(false);
        }

        public override void Animation(IRenderView render, float time)
        {
            mTime = time;

            var position = mGeometry.GetAttribute(0);
            position.SetDataUsage(EnumBufferDataUsage.DYNAMIC_DRAW);
            var positions = new Float32Array(position.GetData());

            // 预计算圆周上的点
            float[] px = new float[numPoints];
            float[] py = new float[numPoints];

            for (int i = 0; i < numPoints; i++)
            {
                float angle = (float)(2 * Math.PI * i / numPoints);
                px[i] = (float)Math.Cos(angle) * radius;
                py[i] = (float)Math.Sin(angle) * radius;
            }

            // t 随时间变化，从 0 到 50，循环（120秒完成一轮，慢4倍）
            float t = (time % 120) / 120.0f * 50.0f;

            for (int i = 0; i < numPoints; i++)
            {
                uint idx1 = (uint)(i * 2);
                uint idx2 = (uint)(i * 2 + 1);
                
                int j = (int)(i * t) % numPoints;
                if (j < 0) j += numPoints;
                
                // 起点
                positions.SetValue(idx1 * 3, px[i]);
                positions.SetValue(idx1 * 3 + 1, py[i]);
                positions.SetValue(idx1 * 3 + 2, 0);
                
                // 终点
                positions.SetValue(idx2 * 3, px[j]);
                positions.SetValue(idx2 * 3 + 1, py[j]);
                positions.SetValue(idx2 * 3 + 2, 0);
            }

            position.RequestUpdate();
            mGeometry.RequestUpdate();
            render.RequestDraw();
        }
    }
}
