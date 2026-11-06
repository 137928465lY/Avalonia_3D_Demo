using AnyCAD.Foundation;
using AnyCAD.PointCloud;
using AnyCAD.Simulate;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace AnyCAD.Demo.Graphics
{
    /// <summary>
    /// 点云栅格高度场压平示例
    /// 迁移自 Featured.ArrayPoints（WPF 版），去掉所有 GUI 依赖：
    /// 1. 从文本读取 256 x 256 的栅格点云（每行 "X Y Z"，X/Y 为 1 基网格索引，Z 为高度）
    /// 2. 以彩色点云显示原始数据
    /// 3. 用 Matplot 生成高度着色曲面（Cooltowarm 色表）
    /// 4. 把选区内的点统一压平到该区域的平均高度，并原地刷新显示
    /// 选区来源：GUI 环境下可直接框选/套索选点，本例在 Run 中先压平中心区块做演示
    /// </summary>
    class Graphics_PointCloudGridFlatten : TestCase
    {
        public Graphics_PointCloudGridFlatten()
        {
            Title = "点云栅格高度场压平";
            Description = "读取 256x256 栅格点云，以冷暖色曲面显示高度场，并把选区内的点压平到平均高度";
            Tags.Add("显示");
            Tags.Add("点云");
            Tags.Add("交互");
            Level = 2;
        }

        PointCloudNode mPointNode;
        GroupSceneNode mMeshNode;

        int mWidth;
        int mHeight;
        float[] mHeights;
        float mMinZ = float.MaxValue;
        float mMaxZ = float.MinValue;

        // 防止刷新显示时清空选择触发的重入
        bool mUpdating;

        public override void Run(IRenderView render)
        {
            if (!Load(GetResourcePath(@"points\grid_xyz.txt")))
                return;

            // GUI 环境下允许框选点云
            render.ViewContext.SetRectPick(true);

            // 演示：把中心区块压平到平均高度
            Flatten(GetCenterBlock());

            Show(render);
            render.ZoomAll();
        }

        /// <summary>
        /// 响应选择变化：把选中的点压平到平均高度
        /// </summary>
        public override void OnSelectionChanged(IRenderView render, PickedResult result)
        {
            if (mUpdating || mHeights == null || mPointNode == null)
                return;

            var indices = new List<int>();
            var itr = render.ViewContext.GetSelectionManager().GetSelection().CreateIterator();
            for (; itr.More(); itr.Next())
            {
                var idx = itr.Current().GetPrimitiveIndex();
                if (idx < (uint)mHeights.Length)
                    indices.Add((int)idx);
            }

            if (indices.Count == 0)
                return;

            Flatten(indices);
            Show(render);
            render.RequestDraw(EnumUpdateFlags.Scene);
        }

        /// <summary>
        /// 读取栅格点云文本文件
        /// </summary>
        bool Load(string fileName)
        {
            if (!File.Exists(fileName))
                return false;

            var triples = new List<(int X, int Y, float Z)>();
            int maxX = 0;
            int maxY = 0;

            foreach (var line in File.ReadLines(fileName))
            {
                var parts = line.Split(new[] { '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 3)
                    continue;

                if (!int.TryParse(parts[0], out int x) || !int.TryParse(parts[1], out int y))
                    continue;
                if (!float.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float z))
                    continue;

                triples.Add((x, y, z));
                if (x > maxX) maxX = x;
                if (y > maxY) maxY = y;
            }

            if (triples.Count == 0 || maxX <= 0 || maxY <= 0)
                return false;

            mWidth = maxX;
            mHeight = maxY;
            mHeights = new float[mWidth * mHeight];

            foreach (var p in triples)
            {
                mHeights[Index(p.X - 1, p.Y - 1)] = p.Z;
                if (p.Z < mMinZ) mMinZ = p.Z;
                if (p.Z > mMaxZ) mMaxZ = p.Z;
            }

            return true;
        }

        /// <summary>
        /// 网格索引 -> 数组下标（行优先，先行后列）
        /// </summary>
        int Index(int col, int row)
        {
            return row * mWidth + col;
        }

        /// <summary>
        /// 中心 1/4 尺寸的矩形区块
        /// </summary>
        List<int> GetCenterBlock()
        {
            var indices = new List<int>();
            int c0 = mWidth * 3 / 8;
            int c1 = mWidth * 5 / 8;
            int r0 = mHeight * 3 / 8;
            int r1 = mHeight * 5 / 8;

            for (int row = r0; row < r1; ++row)
                for (int col = c0; col < c1; ++col)
                    indices.Add(Index(col, row));

            return indices;
        }

        /// <summary>
        /// 把指定下标的点全部压平到它们的平均高度
        /// </summary>
        void Flatten(IReadOnlyList<int> indices)
        {
            if (indices == null || indices.Count == 0)
                return;

            double total = 0;
            foreach (var i in indices)
                total += mHeights[i];

            float average = (float)(total / indices.Count);
            foreach (var i in indices)
                mHeights[i] = average;
        }

        /// <summary>
        /// 按高度取冷暖色（与 Cooltowarm 一致的近似渐变）
        /// </summary>
        Vector3 MapColor(float z)
        {
            double span = mMaxZ - mMinZ;
            double t = span > 1e-9 ? (z - mMinZ) / span : 0.0;

            if (t < 0.33)
            {
                double k = t / 0.33;
                return new Vector3(
                    (float)(0.23 + (0.35 - 0.23) * k),
                    (float)(0.30 + (0.85 - 0.30) * k),
                    (float)(0.75 + (0.85 - 0.75) * k));
            }
            if (t < 0.66)
            {
                double k = (t - 0.33) / 0.33;
                return new Vector3(
                    (float)(0.35 + (0.95 - 0.35) * k),
                    (float)(0.85 + (0.90 - 0.85) * k),
                    (float)(0.85 + (0.55 - 0.85) * k));
            }

            double s = (t - 0.66) / 0.34;
            return new Vector3(
                (float)(0.95 + (0.80 - 0.95) * s),
                (float)(0.90 + (0.20 - 0.90) * s),
                (float)(0.55 + (0.20 - 0.55) * s));
        }

        /// <summary>
        /// 重建并显示点云与高度曲面
        /// </summary>
        void Show(IRenderView view)
        {
            mUpdating = true;
            try
            {
                var positions = new Float32Buffer(0);
                var colors = new Float32Buffer(0);
                positions.Reserve((uint)(mHeights.Length * 3));
                colors.Reserve((uint)(mHeights.Length * 3));

                for (int row = 0; row < mHeight; ++row)
                {
                    for (int col = 0; col < mWidth; ++col)
                    {
                        float z = mHeights[Index(col, row)];
                        positions.Append(col + 1, row + 1, z);
                        colors.Append3(MapColor(z));
                    }
                }

                if (mPointNode != null)
                    view.RemoveSceneNode(mPointNode.GetUuid());

                // 清掉指向旧节点的选择，避免悬空索引
                view.ViewContext.ClearSelection();

                mPointNode = PointCloudNode.Create(positions, colors, null, 2.0f);
                view.ShowSceneNode(mPointNode);

                var matplot = new Matplot("PointCloudGrid");
                matplot.AddMesh((uint)mWidth, (uint)mHeight, (uint i, uint j, double u, double v) =>
                {
                    return new GPnt(i + 1, j + 1, mHeights[Index((int)i, (int)j)]);
                }, false);

                if (mMeshNode != null)
                    view.RemoveSceneNode(mMeshNode.GetUuid());

                mMeshNode = matplot.Build(ColorMapKeyword.Create(EnumSystemColorMap.Cooltowarm));
                mMeshNode.SetPickable(false);
                view.ShowSceneNode(mMeshNode);
            }
            finally
            {
                mUpdating = false;
            }
        }
    }
}
