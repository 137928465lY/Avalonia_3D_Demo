using AnyCAD.Foundation;
using AnyCAD.Simulate;
using System;

namespace AnyCAD.Demo.Graphics
{
    class Graphics_RosslerAttractor : TestCase
    {
        public Graphics_RosslerAttractor()
        {
            Title = "罗斯勒吸引子";
            Description = "数值求解罗斯勒方程绘制混沌吸引子轨迹";
            Tags.Add("显示");
            Tags.Add("动画");
            Level = 1;
        }
        BufferGeometry mGeometry;
        float mTime = 0;
        float mProgress = 0;

        // 罗斯勒吸引子参数
        const float a = 0.2f;
        const float b = 0.2f;
        const float c = 5.7f;
        const float dt = 0.01f;
        const int maxSteps = 5000;

        public override void Run(IRenderView render)
        {
            CreateAttractorGeometry(render);
            render.EnableAnimation(true);
        }

        void CreateAttractorGeometry(IRenderView render)
        {
            // 预计算完整轨迹
            float[] x = new float[maxSteps];
            float[] y = new float[maxSteps];
            float[] z = new float[maxSteps];

            x[0] = 0.1f;
            y[0] = 0.0f;
            z[0] = 0.0f;

            // 使用欧拉法求解微分方程
            for (int i = 0; i < maxSteps - 1; i++)
            {
                x[i + 1] = x[i] + (-y[i] - z[i]) * dt;
                y[i + 1] = y[i] + (x[i] + a * y[i]) * dt;
                z[i + 1] = z[i] + (b + z[i] * (x[i] - c)) * dt;
            }

            // 找到边界以缩放
            float minX = x[0], maxX = x[0];
            float minY = y[0], maxY = y[0];
            float minZ = z[0], maxZ = z[0];

            for (int i = 1; i < maxSteps; i++)
            {
                if (x[i] < minX) minX = x[i];
                if (x[i] > maxX) maxX = x[i];
                if (y[i] < minY) minY = y[i];
                if (y[i] > maxY) maxY = y[i];
                if (z[i] < minZ) minZ = z[i];
                if (z[i] > maxZ) maxZ = z[i];
            }

            float scaleX = 60 / (maxX - minX);
            float scaleY = 60 / (maxY - minY);
            float scaleZ = 60 / (maxZ - minZ);
            float centerX = (maxX + minX) / 2;
            float centerY = (maxY + minY) / 2;
            float centerZ = (maxZ + minZ) / 2;

            // 创建顶点缓冲区 - 三维
            Float32Buffer vertices = new Float32Buffer(0);
            vertices.Reserve((uint)(maxSteps * 3));

            for (int i = 0; i < maxSteps; i++)
            {
                float px = (x[i] - centerX) * scaleX;
                float py = (y[i] - centerY) * scaleY;
                float pz = (z[i] - centerZ) * scaleZ;
                vertices.Append(px, py, pz);
            }

            // 创建索引缓冲区
            Uint32Buffer indices = new Uint32Buffer(0);
            for (int i = 0; i < maxSteps - 1; i++)
            {
                indices.Append((uint)i);
                indices.Append((uint)i + 1);
            }

            // 创建几何体
            mGeometry = new BufferGeometry(EnumPrimitiveType.LINES);
            mGeometry.AddAttribute(EnumAttributeSemantic.Position, EnumAttributeComponents.Three, vertices);
            mGeometry.SetIndex(indices);

            // 创建颜色属性 - 使用彩虹渐变
            ColorLookupTable colorTable = new ColorLookupTable();
            colorTable.SetMaxValue(maxSteps);
            colorTable.SetColorMap(ColorMapKeyword.Create(EnumSystemColorMap.Rainbow));

            Float32Buffer colors = new Float32Buffer(0);
            colors.Reserve((uint)(maxSteps * 3));

            for (int i = 0; i < maxSteps; i++)
            {
                var clr = colorTable.GetColor((float)i);
                colors.Append(clr.x, clr.y, clr.z);
            }
            mGeometry.AddAttribute(EnumAttributeSemantic.Color, EnumAttributeComponents.Three, colors);

            var material = BasicMaterial.Create("rossler");
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

            // 重新计算轨迹，添加一点动画效果（轻微抖动）
            float[] x = new float[maxSteps];
            float[] y = new float[maxSteps];
            float[] z = new float[maxSteps];

            x[0] = 0.1f + (float)Math.Sin(time * 0.5f) * 0.01f;
            y[0] = 0.0f;
            z[0] = 0.0f;

            float[] xHistory = new float[maxSteps];
            float[] yHistory = new float[maxSteps];
            float[] zHistory = new float[maxSteps];

            xHistory[0] = x[0];
            yHistory[0] = y[0];
            zHistory[0] = z[0];

            for (int i = 0; i < maxSteps - 1; i++)
            {
                x[i + 1] = x[i] + (-y[i] - z[i]) * dt;
                y[i + 1] = y[i] + (x[i] + a * y[i]) * dt;
                z[i + 1] = z[i] + (b + z[i] * (x[i] - c)) * dt;
                xHistory[i + 1] = x[i + 1];
                yHistory[i + 1] = y[i + 1];
                zHistory[i + 1] = z[i + 1];
            }

            // 缩放
            float minX = xHistory[0], maxX = xHistory[0];
            float minY = yHistory[0], maxY = yHistory[0];
            float minZ = zHistory[0], maxZ = zHistory[0];

            for (int i = 1; i < maxSteps; i++)
            {
                if (xHistory[i] < minX) minX = xHistory[i];
                if (xHistory[i] > maxX) maxX = xHistory[i];
                if (yHistory[i] < minY) minY = yHistory[i];
                if (yHistory[i] > maxY) maxY = yHistory[i];
                if (zHistory[i] < minZ) minZ = zHistory[i];
                if (zHistory[i] > maxZ) maxZ = zHistory[i];
            }

            float scaleX = 60 / (maxX - minX);
            float scaleY = 60 / (maxY - minY);
            float scaleZ = 60 / (maxZ - minZ);
            float centerX = (maxX + minX) / 2;
            float centerY = (maxY + minY) / 2;
            float centerZ = (maxZ + minZ) / 2;

            // 计算进度（0-1），20秒完成一轮
            mProgress = (time % 20) / 20;

            for (int i = 0; i < maxSteps; i++)
            {
                uint idx = (uint)i;
                if ((float)i / maxSteps <= mProgress)
                {
                    float px = (xHistory[i] - centerX) * scaleX;
                    float py = (yHistory[i] - centerY) * scaleY;
                    float pz = (zHistory[i] - centerZ) * scaleZ;
                    positions.SetValue(idx * 3, px);
                    positions.SetValue(idx * 3 + 1, py);
                    positions.SetValue(idx * 3 + 2, pz);
                }
                else
                {
                    positions.SetValue(idx * 3, float.NaN);
                    positions.SetValue(idx * 3 + 1, float.NaN);
                    positions.SetValue(idx * 3 + 2, float.NaN);
                }
            }

            position.RequestUpdate();
            mGeometry.RequestUpdate();
            render.RequestDraw();
        }
    }
}
