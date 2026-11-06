using System;

namespace AnyCAD.Math.Test
{
    /// <summary>
    /// Matrix4d 乘法顺序 / 转置 / 平移布局的验证集。
    /// 目的：定位 CharacterWalk 示例中 T*R 组合矩阵绕偏心点旋转的 bug。
    ///
    /// 判定基准（glm 列向量约定，v' = M·v）：
    ///   - makeTranslation(x,y,z) 应把平移放在第 4 列（get(0,3)/get(1,3)/get(2,3)）。
    ///   - applyMatrix4d(T) 应把点平移到 (x,y,z)。
    ///   - m = T.clone(); m.multiply(R) 得到 T·R：
    ///     作用在点 p 上应等价于 T(R(p))——先旋转后平移，轴心在自身原点。
    ///     若实测等价于 R(T(p))（轴心跑到世界原点），即复现示例中的公转 bug。
    /// </summary>
    [TestClass]
    public class Matrix4dTest
    {
        const double Eps = 1e-9;

        static void AssertVector(Vector3d v, double x, double y, double z, string msg)
        {
            Assert.AreEqual(x, v.x, 1e-6, msg + $" (实际 x={v.x})");
            Assert.AreEqual(y, v.y, 1e-6, msg + $" (实际 y={v.y})");
            Assert.AreEqual(z, v.z, 1e-6, msg + $" (实际 z={v.z})");
        }

        //! T = makeTranslation(10,20,30) 作用在原点 → (10,20,30)
        [TestMethod]
        public void TranslationMovesOrigin()
        {
            var t = Matrix4d.makeTranslation(10, 20, 30);
            var p = new Vector3d(0, 0, 0);
            p.applyMatrix4d(t);
            AssertVector(p, 10, 20, 30, "平移矩阵作用于原点");
        }

        //! ★ 已发现的 API 陷阱：get/set(i,j) 的第一个下标是「列」！
        //! glm 存储是 m[column][row]，SWIG 直接透传了 glm 的 operator[] 语义，
        //! 即 get(col, row)。平移量落在 get(3,0)/get(3,1)/get(3,2)，
        //! 而不是数学记号 M(0,3) 的行×列读法。与 Eigen/OpenCV 等行主序
        //! 访问习惯相反，混用必错。
        [TestMethod]
        public void TranslationLayout_ColumnMajor()
        {
            var t = Matrix4d.makeTranslation(10, 20, 30);
            // 第 4 列（下标 3 的列）装平移量：get(列=3, 行=0..2)
            Assert.AreEqual(10, t.get(3, 0), Eps, "get(3,0) 应为 10（第 3 列第 0 行）");
            Assert.AreEqual(20, t.get(3, 1), Eps, "get(3,1) 应为 20");
            Assert.AreEqual(30, t.get(3, 2), Eps, "get(3,2) 应为 30");
            // 按行×列读法读 (0,3) 拿到的是第 0 列第 3 行 = 0
            Assert.AreEqual(0, t.get(0, 3), Eps, "get(0,3) 是第 0 列第 3 行，不是平移量");
            // 对角线（列==行）不受影响
            Assert.AreEqual(1, t.get(0, 0), Eps, "get(0,0) 应为 1");
            Assert.AreEqual(1, t.get(1, 1), Eps, "get(1,1) 应为 1");
            Assert.AreEqual(1, t.get(2, 2), Eps, "get(2,2) 应为 1");
        }

        //! R = 绕 Z 转 90°：(1,0,0) → (0,1,0)
        [TestMethod]
        public void RotationAxisZ90()
        {
            var r = Matrix4d.makeRotationAxis(new Vector3d(0, 0, 1), System.Math.PI / 2);
            var p = new Vector3d(1, 0, 0);
            p.applyMatrix4d(r);
            AssertVector(p, 0, 1, 0, "绕 Z 转 90° 作用于 (1,0,0)");
        }

        //! ★ 核心：multiply 的语义。m=T.clone(); m.multiply(R) 应得 T·R：
        //!   列向量约定下 (T·R)(p) = T(R(p)) = (10, 21, 30)。
        //!   若得到 (-20, 11, 30)，说明实际算的是 R·T——轴心跑到世界原点，
        //!   这正是 CharacterWalk 里人物公转的根因。
        [TestMethod]
        public void Multiply_IsTranslateThenRotate_TR()
        {
            var t = Matrix4d.makeTranslation(10, 20, 30);
            var r = Matrix4d.makeRotationAxis(new Vector3d(0, 0, 1), System.Math.PI / 2);

            var m = t.clone();
            m.multiply(r);   // 期望 m = T·R

            var p = new Vector3d(1, 0, 0);
            p.applyMatrix4d(m);

            // T(R(p)): R 把 (1,0,0) 转到 (0,1,0)，再平移 → (10,21,30)
            AssertVector(p, 10, 21, 30,
                "multiply 应为 this = this·trf（T·R，先旋转后平移）。" +
                "若实际为 (-20,11,30) 则是 R·T（先平移后绕原点旋转）");
        }

        //! 对照组：显式构造 R·T，确认与上面区分开
        [TestMethod]
        public void Multiply_ReversedOrder_RPivotAtWorldOrigin()
        {
            var t = Matrix4d.makeTranslation(10, 20, 30);
            var r = Matrix4d.makeRotationAxis(new Vector3d(0, 0, 1), System.Math.PI / 2);

            var m = r.clone();
            m.multiply(t);   // m = R·T

            var p = new Vector3d(1, 0, 0);
            p.applyMatrix4d(m);
            // (R·T)(p) = R(T(p)) = R(11,20,30) = (-20,11,30)
            AssertVector(p, -20, 11, 30, "R·T 对照组：先平移后绕原点旋转");
        }

        //! operator*（若可用）应与 multiply 一致：T * R = T·R
        [TestMethod]
        public void OperatorStar_MatchesMultiply()
        {
            var t = Matrix4d.makeTranslation(10, 20, 30);
            var r = Matrix4d.makeRotationAxis(new Vector3d(0, 0, 1), System.Math.PI / 2);

            var mOp = t * r;              // 若编译不过说明 C# 未导出 operator*
            var mMul = t.clone();
            mMul.multiply(r);

            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    Assert.AreEqual(mMul.get(i, j), mOp.get(i, j), Eps,
                        $"operator* 与 multiply 在 ({i},{j}) 不一致");
                }
            }
        }

        //! decomposeTRS 应从 T·R 中还原出平移与旋转
        [TestMethod]
        public void DecomposeTRS_FromTranslateRotate()
        {
            var t = Matrix4d.makeTranslation(10, 20, 30);
            var r = Matrix4d.makeRotationAxis(new Vector3d(0, 0, 1), System.Math.PI / 2);
            var m = t.clone();
            m.multiply(r);

            var d = m.decomposeTRS();
            Assert.AreEqual(10, d.translation.x, 1e-6, "decomposeTRS 平移 x");
            Assert.AreEqual(20, d.translation.y, 1e-6, "decomposeTRS 平移 y");
            Assert.AreEqual(30, d.translation.z, 1e-6, "decomposeTRS 平移 z");

            var q = d.rotation;
            // 四元数绕 Z 转 90°：(0, 0, sin45°, cos45°)
            Assert.AreEqual(0, System.Math.Abs(q.x), 1e-6, "四元数 x");
            Assert.AreEqual(0, System.Math.Abs(q.y), 1e-6, "四元数 y");
            Assert.AreEqual(System.Math.Sin(System.Math.PI / 4), System.Math.Abs(q.z), 1e-6, "四元数 z");
            Assert.AreEqual(System.Math.Cos(System.Math.PI / 4), System.Math.Abs(q.w), 1e-6, "四元数 w");
        }

        //! transpose 来回转置应复原；注意 get(列,行) 语义下，转置后
        //! 平移量在 get(0,3)（数学记号的 (3,0) 位）。
        [TestMethod]
        public void Transpose_RoundTrip()
        {
            var t = Matrix4d.makeTranslation(10, 20, 30);
            var tt = t.transposed();
            // 转置把第 3 列的平移搬到第 3 行：get(列=0, 行=3) = 10
            Assert.AreEqual(10, tt.get(0, 3), Eps, "转置后 get(0,3) 应为 10");

            tt.transpose();
            Assert.AreEqual(10, tt.get(3, 0), Eps, "再转置回来 get(3,0) 应为 10");
        }
    }
}
