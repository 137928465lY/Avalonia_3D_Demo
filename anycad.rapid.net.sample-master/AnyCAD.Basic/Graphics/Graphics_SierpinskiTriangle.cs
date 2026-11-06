using AnyCAD.Foundation;
using AnyCAD.Simulate;
using System;
using System.Collections.Generic;

namespace AnyCAD.Demo.Graphics
{
    class Graphics_SierpinskiTriangle : TestCase
    {
        public Graphics_SierpinskiTriangle()
        {
            Title = "谢尔宾斯基三角";
            Description = "递归生成谢尔宾斯基三角形分形线框";
            Tags.Add("显示");
            Tags.Add("分形");
            Level = 1;
        }
        int mIterations = 6;
        float mSize = 200;

        List<Vector3> mVertices = new List<Vector3>();
        ColorLookupTable mColorTable = new ColorLookupTable();

        BufferGeometry mGeometry;
        PrimitiveSceneNode mNode;
        IRenderView mRender;

        public override void Run(IRenderView render)
        {
            mRender = render;
            mVertices.Clear();

            // 初始化颜色查找表 - 彩虹色
            mColorTable.SetMaxValue(mSize);
            mColorTable.SetColorMap(ColorMapKeyword.Create(EnumSystemColorMap.Rainbow));

            // 生成分形
            GenerateSierpinski();

            // 创建几何体
            CreateGeometry();

            // 创建材质
            var material = BasicMaterial.Create("sierpinski");
            material.SetUniform("diffuse", ColorTable.Hex(0xFFFFFF));
            material.GetTemplate().SetVertexColors(true);

            mNode = new PrimitiveSceneNode(mGeometry, material);
            render.ShowSceneNode(mNode);

            // 显示颜色条
            ShowColorPalette();
        }

        void ShowColorPalette()
        {
            PaletteWidget pw = new PaletteWidget();
            pw.Update(mColorTable);
            mRender.ShowSceneNode(pw);
        }

        void GenerateSierpinski()
        {
            // 等边三角形顶点
            float h = mSize * (float)Math.Sqrt(3) / 2; // 等边三角形高度
            Vector3 p1 = new Vector3(0, h * 2 / 3, 0);           // 顶部
            Vector3 p2 = new Vector3(-mSize / 2, -h / 3, 0);     // 左下
            Vector3 p3 = new Vector3(mSize / 2, -h / 3, 0);      // 右下

            SierpinskiRecursive(p1, p2, p3, mIterations);
        }

        void SierpinskiRecursive(Vector3 p1, Vector3 p2, Vector3 p3, int depth)
        {
            if (depth == 0)
            {
                // 绘制三角形（3条边）
                mVertices.Add(p1);
                mVertices.Add(p2);
                mVertices.Add(p2);
                mVertices.Add(p3);
                mVertices.Add(p3);
                mVertices.Add(p1);
                return;
            }

            // 计算各边中点
            Vector3 m12 = (p1 + p2) * 0.5;
            Vector3 m23 = (p2 + p3) * 0.5;
            Vector3 m31 = (p3 + p1) * 0.5;

            // 递归处理三个子三角形
            SierpinskiRecursive(p1, m12, m31, depth - 1);
            SierpinskiRecursive(m12, p2, m23, depth - 1);
            SierpinskiRecursive(m31, m23, p3, depth - 1);
        }

        void CreateGeometry()
        {
            Float32Buffer vb = new Float32Buffer(0);
            Float32Buffer cb = new Float32Buffer(0);

            float h = mSize * (float)Math.Sqrt(3) / 2;

            for (int i = 0; i < mVertices.Count; i += 2)
            {
                Vector3 p1 = mVertices[i];
                Vector3 p2 = mVertices[i + 1];
                vb.Append((float)p1.x, (float)p1.y, (float)p1.z);
                vb.Append((float)p2.x, (float)p2.y, (float)p2.z);

                // 根据Y坐标获取颜色
                float midY = (float)((p1.y + p2.y) / 2.0);
                float normalizedY = (midY + h / 3) / mSize; // 归一化到0-1
                var clr = mColorTable.GetColor(normalizedY * mSize);
                cb.Append(clr.x, clr.y, clr.z);
                cb.Append(clr.x, clr.y, clr.z);
            }

            mGeometry = GeometryBuilder.CreateLines(new Float32Array(vb), null, new Float32Array(cb));
        }

        public override void Exit(IRenderView render)
        {
        }

        public override void Animation(IRenderView render, float time)
        {
        }
    }
}
