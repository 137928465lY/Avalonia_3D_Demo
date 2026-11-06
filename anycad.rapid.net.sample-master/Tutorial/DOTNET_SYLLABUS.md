# AnyCAD .NET 入门教程 - 课程大纲

## 📚 教程定位

**目标受众**: 
- C# 开发者想学习 CAD 开发
- 机械工程师想自动化设计流程
- 大学生学习参数化建模
- 从 Python 转向.NET 的专业开发者

**学习目标**:
- ✅ 掌握 AnyCAD .NET API 基础
- ✅ 理解 TestCase 生命周期管理
- ✅ 能够创建交互式 3D 应用
- ✅ 掌握精确几何建模（B-Rep）
- ✅ 实现动画和物理仿真
- ✅ 开发专业 CAD 工具

**前置要求**:
- C# 编程基础（类、方法、集合）
- 基础线性代数知识（向量、矩阵）
- Visual Studio 或 VS Code
- .NET 6.0+ SDK

**与 Python 版本对比**:
| 特性 | Python API4AI | .NET TestCase |
|------|--------------|---------------|
| **适用场景** | 快速原型、AI 生成 | 专业应用、完整工具 |
| **代码结构** | 脚本式 | 面向对象 |
| **执行方式** | 解释执行 | 编译运行 |
| **性能** | 一般 | 优秀 |
| **调试** | 运行时检查 | 强类型检查 |

---

## 🎯 课程结构

### **Level 1: 快速入门 (预计 3-4 小时)**

#### 第 1 课：Hello AnyCAD - 第一个三维程序
**学习目标**:
- 了解 AnyCAD .NET 框架
- 学会创建 TestCase 子类
- 理解 Run 方法的作用
- 运行第一个显示立方体的程序

**内容大纲**:

1. AnyCAD.NET 简介（10 分钟）
   - .NET API vs Python API
   - TestCase 工作机制
   - IRenderView 接口介绍

2. 环境配置（15 分钟）
   ```bash
   # 安装.NET SDK
   dotnet --version  # 确认版本 >= 6.0
   
   # 克隆项目
   git clone https://gitee.com/anycad/rapid.net.sample.git
   
   # 编译项目
   dotnet build
   ```

3. 代码模板解析（30 分钟）
```csharp
using AnyCAD.Foundation;

namespace AnyCAD.Demo.Graphics
{
    class Graphics_HelloBox : TestCase
    {
        public override void Run(IRenderView render)
        {
            // 1. 创建材质
            var material = MeshPhongMaterial.Create("box.material");
            material.SetColor(ColorTable.Blue);
            
            // 2. 创建几何体（两种方式的对比）
            
            // 方式 A: 使用 GeometryBuilder（适合可视化）
            var boxMesh = GeometryBuilder.CreateBox(100, 80, 60);
            var boxNode = new PrimitiveSceneNode(boxMesh, material);
            render.ShowSceneNode(boxNode);
            
            // 方式 B: 使用 ShapeBuilder（精确 CAD 几何）
            var boxShape = ShapeBuilder.MakeBox(GP.XOY(), 100, 80, 60);
            var shapeNode = BrepSceneNode.Create(boxShape, material, null);
            shapeNode.SetTransform(Matrix4d.makeTranslation(150, 0, 0));
            render.ShowSceneNode(shapeNode);
        }
    }
}
```

4. 关键概念讲解
   - **TestCase**: 测试用例基类，所有示例都继承自它
   - **IRenderView**: 渲染视图接口，用于显示几何体
   - **SceneNode**: 场景节点，分为 PrimitiveSceneNode 和 BrepSceneNode
   - **Material**: 材质，控制物体颜色和表面属性

5. 实践练习（30 分钟）
   - 修改立方体的尺寸和颜色
   - 创建多个立方体并排列
   - 尝试不同的材质类型

**课后作业**:
- 创建一个包含 3 种不同颜色立方体的场景
- 尝试使用 GeometryBuilder 和 ShapeBuilder 两种方式
- 编写代码注释说明两者的区别

**参考资源**:
- [Graphics_Geometry.cs](../../AnyCAD.Basic/Graphics/Graphics_Geometry.cs) - 几何体示例
- [Graphics_Box.cs](../../AnyCAD.Basic/Graphics/Graphics_Box.cs) - 盒子示例
- [AGENTS.md](../../AGENTS.md) - 开发指南

**预计时间**: 85 分钟

---

#### 第 2 课：基本几何体 - 搭建三维世界
**学习目标**:
- 掌握 6 种基本几何体的创建方法
- 理解 GP、GAx2、GDir 等坐标系类
- 学会使用 GeometryBuilder 和 ShapeBuilder

**内容大纲**:

1. 立方体 (Box)
```csharp
// 方式 1: GeometryBuilder（网格表示）
var box = GeometryBuilder.CreateBox(length, width, height);
var node = new PrimitiveSceneNode(box, material);

// 方式 2: ShapeBuilder（B-Rep 精确表示）
var shape = ShapeBuilder.MakeBox(GP.XOY(), length, width, height);
var node = BrepSceneNode.Create(shape, material, null);

// 自定义位置和方向
var ax2 = new GAx2(new GPnt(0, 0, 0), new GDir(0, 0, 1));
var shaped_box = ShapeBuilder.MakeBox(ax2, length, width, height);
```

2. 球体 (Sphere)
```csharp
// 网格球体（segmentsW, segmentsH 控制细分度）
var sphere = GeometryBuilder.CreateSphere(radius, 32, 32);

// 精确球体
var sphereShape = ShapeBuilder.MakeSphereByRadius(center, radius);

// 部分球体（角度范围）
var sphereSegment = ShapeBuilder.MakeSphere(ax2, radius, Math.PI / 2);
```

3. 圆柱体 (Cylinder)
```csharp
// 网格圆柱
var cylinder= GeometryBuilder.CreateCylinder(radius, height, segments);

// 精确圆柱
var cylinderShape = ShapeBuilder.MakeCylinder(GP.XOY(), radius, height, Math.PI * 2);

// 扇形柱体（1/4 圆柱）
var partialCylinder= ShapeBuilder.MakeCylinder(GP.XOY(), radius, height, Math.PI / 2);
```

4. 圆锥体 (Cone)
```csharp
// 标准圆锥（顶部半径为 0）
var cone = ShapeBuilder.MakeCone(GP.XOY(), bottomRadius, 0, height, Math.PI * 2);

// 圆台（上下都有半径）
var frustum = ShapeBuilder.MakeCone(GP.XOY(), rBottom, rTop, height, angle);
```

5. 圆环体 (Torus)
```csharp
var torus = ShapeBuilder.MakeTorus(ax2, innerRadius, outerRadius, angle1, angle2);
```

6. 平面 (Plane)
```csharp
// 创建地面或墙面
var plane = GeometryBuilder.CreatePlane(width, height);
var planeNode = new PrimitiveSceneNode(plane, material);
planeNode.SetTransform(Matrix4d.makeTranslation(x, y, z));
```

7. 综合案例：几何体展示台
```csharp
class Graphics_GeometryShowcase : TestCase
{
    public override void Run(IRenderView render)
    {
        var material = MeshPhongMaterial.Create("showcase.mat");
        material.SetColor(ColorTable.LightBlue);
        
        int spacing = 120;
        
        // 创建立方体
        var box = ShapeBuilder.MakeBox(GP.XOY(), 80, 80, 80);
        var boxNode = BrepSceneNode.Create(box, material, null);
        boxNode.SetTransform(Matrix4d.makeTranslation(-spacing * 2, 0, 0));
        render.ShowSceneNode(boxNode);
        
        // 创建球体
        var sphere = ShapeBuilder.MakeSphereByRadius(new GPnt(0, 0, 40), 40);
        var sphereNode = BrepSceneNode.Create(sphere, material, null);
        sphereNode.SetTransform(Matrix4d.makeTranslation(-spacing, 0, 0));
        render.ShowSceneNode(sphereNode);
        
        // 创建圆柱
        var cylinder= ShapeBuilder.MakeCylinder(GP.XOY(), 40, 80, Math.PI * 2);
        var cylinderNode = BrepSceneNode.Create(cylinder, material, null);
        render.ShowSceneNode(cylinderNode);
        
        // 创建圆锥
        var cone = ShapeBuilder.MakeCone(GP.XOY(), 40, 0, 80, Math.PI * 2);
        var coneNode = BrepSceneNode.Create(cone, material, null);
        coneNode.SetTransform(Matrix4d.makeTranslation(spacing, 0, 0));
        render.ShowSceneNode(coneNode);
        
        // 创建圆环
        var torus = ShapeBuilder.MakeTorus(GP.XOY(), 25, 40, Math.PI * 2, Math.PI * 2);
        var torusNode = BrepSceneNode.Create(torus, material, null);
        torusNode.SetTransform(Matrix4d.makeTranslation(spacing * 2, 0, 0));
        render.ShowSceneNode(torusNode);
    }
}
```

**对比总结**:
| 特性 | GeometryBuilder | ShapeBuilder |
|------|----------------|--------------|
| **输出类型** | Mesh（网格） | TopoShape（拓扑形状） |
| **精度** | 近似（离散化） | 精确（数学定义） |
| **用途** | 可视化、游戏 | CAD、工程图 |
| **性能** | 快速渲染 | 精确计算 |
| **后续操作** | 有限 | 支持布尔运算等 |

**课后作业**:
- 创建一个包含所有基本体的展示场景
- 为每个几何体添加尺寸标注
- 尝试创建部分几何体（如 1/4 圆柱、半球等）

**预计时间**: 120 分钟

---

#### 第 3 课：变换操作 - 让物体动起来
**学习目标**:
- 掌握 Matrix4d 变换矩阵
- 学会平移、旋转、缩放操作
- 理解局部坐标和全局坐标

**内容大纲**:

1. 平移变换 (Translation)
```csharp
// 方式 1: 使用 makeTranslation
var matrix = Matrix4d.makeTranslation(x, y, z);
node.SetTransform(matrix);

// 方式 2: 使用 Vector3d
var translation = Matrix4d.makeTranslation(new Vector3d(50, 0, 100));
node.SetTransform(translation);

// 示例：沿 X 轴移动
node.SetTransform(Matrix4d.makeTranslation(50, 0, 0));
```

2. 旋转变换 (Rotation)
```csharp
// 绕指定轴旋转
var rotation = Matrix4d.makeRotationAxis(axis, angle);
// axis: Vector3d, angle: 弧度

// 绕任意轴旋转
var axis = new Vector3d(1, 1, 0).normalize();
var rotation = Matrix4d.makeRotationAxis(axis, Math.PI / 3);
```

3. 缩放变换 (Scale)
```csharp
// 均匀缩放
var scale = Matrix4d.makeScale(2.0);  // 放大 2 倍

// 非均匀缩放
var nonUniformScale = Matrix4d.makeScale(sx, sy, sz);

// 示例：压扁球体变成椭球
var ellipsoid = Matrix4d.makeScale(1, 0.5, 0.3);
```

4. 组合变换
```csharp
// 链式调用（注意顺序！）
var transform = Matrix4d.makeTranslation(100, 0, 0)
              * Matrix4d.makeRotationZ(Math.PI / 4)
              * Matrix4d.makeScale(1.5);

// 先缩放，再旋转，最后平移
node.SetTransform(transform);

// 或者分步设置
node.SetTransform(Matrix4d.makeScale(1.5));
node.SetTransform(node.GetTransform() * Matrix4d.makeRotationZ(Math.PI / 4));
node.SetTransform(node.GetTransform() * Matrix4d.makeTranslation(100, 0, 0));
```

5. 实战案例：创建风车
```csharp
class Graphics_Windmill : TestCase
{
    MaterialInstance mBladeMaterial;
    BrepSceneNode[] mBlades = new BrepSceneNode[4];
    
    public override void Run(IRenderView render)
    {
        // 创建材质
        mBladeMaterial = MeshPhongMaterial.Create("blade.mat");
        mBladeMaterial.SetColor(ColorTable.Red);
        
        // 创建中心轴
        var hub = ShapeBuilder.MakeCylinder(GP.XOY(), 10, 100, Math.PI * 2);
        var hubNode = BrepSceneNode.Create(hub, mBladeMaterial, null);
        render.ShowSceneNode(hubNode);
        
        // 创建 4 个叶片
        for (int i = 0; i < 4; i++)
        {
            var blade = ShapeBuilder.MakeBox(GP.XOY(), 80, 20, 2);
            var bladeNode = BrepSceneNode.Create(blade, mBladeMaterial, null);
            
            // 旋转到正确角度
            double angle = i * Math.PI / 2;
            var transform = Matrix4d.makeRotationZ(angle)
                          * Matrix4d.makeTranslation(0, 40, 50);
            bladeNode.SetTransform(transform);
            
            render.ShowSceneNode(bladeNode);
            mBlades[i] = bladeNode;
        }
        
        // 启用动画
        render.EnableAnimation(true);
    }
    
    public override void Animation(IRenderView render, float time)
    {
        // 让叶片旋转
        for (int i = 0; i < 4; i++)
        {
            double angle = time * 0.001; // 随时间旋转
            var transform = Matrix4d.makeRotationZ(angle + i * Math.PI / 2)
                          * Matrix4d.makeTranslation(0, 40, 50);
            mBlades[i].SetTransform(transform);
            mBlades[i].RequestUpdate();
        }
        
        render.RequestDraw(EnumUpdateFlags.Scene);
    }
    
    public override void Exit(IRenderView render)
    {
        render.EnableAnimation(false);
    }
}
```

**数学知识点**:
- 弧度制：`degrees * Math.PI / 180 = radians`
- 变换顺序：通常先缩放，再旋转，最后平移
- 矩阵乘法不满足交换律：`A * B ≠ B * A`

**课后作业**:
- 创建一个旋转的陀螺
- 设计一个太阳系模型（地球绕太阳转）
- 制作一个升降电梯动画

**预计时间**: 120 分钟

---

### **Level 2: 核心技能 (预计 6-8 小时)**

#### 第 4 课：布尔运算 - 创建复杂零件
**学习目标**:
- 掌握 BooleanTool 的使用方法
- 学会 Fuse、Cut、Common 运算
- 能够设计带孔洞和凹槽的零件

**内容大纲**:

1. 并集 (Fuse)
```csharp
// 两个形状的并集
var result = BooleanTool.FuseTwo(shapeA, shapeB);

// 多个形状的并集
var shapes = new TopoShapeList();
shapes.Add(shape1);
shapes.Add(shape2);
shapes.Add(shape3);
var fused = BooleanTool.Fuse(shapes);
```

2. 差集 (Cut)
```csharp
// 从 A 减去 B
var cutResult = BooleanTool.CutTwo(shapeA, shapeB);

// 示例：在板上打孔
var plate = ShapeBuilder.MakeBox(GP.XOY(), 200, 150, 20);
var hole = ShapeBuilder.MakeCylinder(GP.XOY(), 15, 25, Math.PI * 2);
hole = BooleanTool.CutTwo(plate, hole);
```

3. 交集 (Common)
```csharp
// 取两个形状的重叠部分
var common = BooleanTool.Common(shapeA, shapeB);
```

4. 分割 (Section)
```csharp
// 用平面剖切
var section = BooleanTool.Section(shape, position, direction);
```

5. 实战案例：轴承座设计
```csharp
class Geometry_BearingHousing : TestCase
{
    public override void Run(IRenderView render)
    {
        var material = MeshStandardMaterial.Create("housing.mat");
        material.SetColor(ColorTable.Hex(0xCCCCCC));
        material.SetMetalness(0.3f);
        
        // 1. 创建底座
        var basePlate = ShapeBuilder.MakeBox(GP.XOY(), 300, 200, 30);
        
        // 2. 创建四个安装孔
        var holes = new TopoShapeList();
        foreach (var pos in new[] {
            new GPnt(-120, -70, 0),
            new GPnt(120, -70, 0),
            new GPnt(-120, 70, 0),
            new GPnt(120, 70, 0)
        })
        {
            var hole = ShapeBuilder.MakeCylinder(
                new GAx2(pos, GP.DZ()), 12, 35, Math.PI * 2);
            holes.Add(hole);
        }
        var holeGroup = BooleanTool.Fuse(holes);
        basePlate = BooleanTool.CutTwo(basePlate, holeGroup);
        
        // 3. 创建轴承孔
        var bearingHole = ShapeBuilder.MakeCylinder(
            new GAx2(new GPnt(0, 0, 30), GP.DZ()), 50, 100, Math.PI * 2);
        basePlate = BooleanTool.CutTwo(basePlate, bearingHole);
        
        // 4. 创建加强筋
        var rib1 = ShapeBuilder.MakeBox(new GAx2(new GPnt(0, 60, 30), GP.DZ()), 200, 15, 40);
        var rib2 = ShapeBuilder.MakeBox(new GAx2(new GPnt(0, -60, 30), GP.DZ()), 200, 15, 40);
        
        // 5. 合并所有部分
        var final = BooleanTool.FuseTwo(basePlate, rib1);
        final = BooleanTool.FuseTwo(final, rib2);
        
        // 6. 倒角（可选）
        // final = FeatureTool.Fillet(final, edges, 5.0);
        
        var node = BrepSceneNode.Create(final, material, null);
        render.ShowSceneNode(node);
    }
}
```

**常见问题排查**:
1. **布尔运算失败**: 确保形状有实际重叠
2. **空结果**: 检查形状位置是否正确
3. **性能慢**: 减少小特征数量
4. **法线反转**: 检查面的方向

**课后作业**:
- 设计一个带散热片的电机外壳
- 创建一个法兰盘零件
- 制作一个手机支架

**预计时间**: 150 分钟

---

#### 第 5 课：材质与渲染 - 让模型更真实
**学习目标**:
- 掌握不同类型材质的使用
- 学会纹理贴图技术
- 理解光照和渲染设置

**内容大纲**:

1. 基础材质 (BasicMaterial)
2. Phong 材质 (MeshPhongMaterial)
3. 标准材质 (MeshStandardMaterial)
4. 点材质 (PointsMaterial)
5. 虚线材质 (LineDashedMaterial)
6. 纹理贴图

**预计时间**: 120 分钟

---

#### 第 6 课：动画系统 - 赋予生命
**学习目标**:
- 理解 Animation 回调机制
- 学会时间驱动的动画
- 实现交互动画

**内容大纲**:

1. TestCase 生命周期
2. Animation 方法详解
3. 时间控制
4. 变换动画
5. 材质动画

**预计时间**: 150 分钟

---

### **Level 3: 进阶实战 (预计 8-10 小时)**

#### 第 7-10 课：综合项目实战
- 参数化齿轮箱设计
- 机械臂运动仿真
- 建筑楼梯建模
- 艺术雕塑创作

---

## 📋 教学建议

### 标准课程流程
```
1. 引入（5-10 分钟）：展示最终效果
2. 讲解（30-40 分钟）：逐步演示代码
3. 练习（40-50 分钟）：学生动手实践
4. 总结（10 分钟）：回顾要点
5. 答疑（10 分钟）：解决问题
```

### 评估体系
- **课堂练习**: 40%
- **课后作业**: 30%
- **期末项目**: 30%

### 差异化教学策略
- **初学者**: 提供完整代码，重点理解
- **进阶者**: 给出需求，自主实现
- **高级者**: 挑战开放性问题

---

## 🎓 结业项目

### 项目要求
开发一个完整的参数化 CAD 工具，包括：
1. 至少 15 个可调参数
2. 使用 5 种以上几何操作
3. 包含动画效果
4. 有实际工程应用背景

### 评分维度
- **功能性** (40%): 是否满足需求
- **代码质量** (25%): 规范性、可读性
- **创新性** (20%): 设计新颖程度
- **文档完整性** (15%): 注释和说明

---

## 📚 附录

### A. .NET API vs Python API 对照表
```
Python                        C# .NET
─────────────────────────────────────────────
A           →  B

```

### B. 常用数学公式
```csharp
// 角度转弧度
double radians = degrees * Math.PI / 180.0;

// 三角函数
double x = radius * Math.Cos(angle);
double y = radius * Math.Sin(angle);

// 向量运算
var length = vector.Length;
var normalized = vector.Normalize();
var dot = Vector3d.Dot(v1, v2);
var cross = Vector3d.Cross(v1, v2);
```

### C. 调试技巧
```csharp
// 输出调试信息
System.Diagnostics.Debug.WriteLine($"Value: {value}");
Console.WriteLine($"Time: {time}");

// 可视化辅助
var axis = ArrowWidget.Create(0.1f, 10, null);
render.ShowSceneNode(axis);

// 断点调试
if (condition)
{
    System.Diagnostics.Debugger.Break();
}
```

### D. 性能优化建议
1. 使用 GroupSceneNode 管理多个物体
2. 共享材质实例
3. 合理使用 RequestUpdate()
4. 避免频繁创建临时对象

---

## 🔗 相关资源

- [Python 版教程](./SYLLABUS.md) - 对比学习
- [快速参考](../QUICK_REFERENCE.md) - API 速查
- [AGENTS.md](../../AGENTS.md) - 完整开发指南
- [官方文档](http://www.anycad.cn/api/classes.html)

---

**版本**: v1.0  
**更新日期**: 2026-03-10  
**作者**: AnyCAD Team  
**状态**: 草案 - 待完善
