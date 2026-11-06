# AnyCAD Rapid.NET Sample - AI 开发指南

## 📚 项目概述

本项目是 AnyCAD Rapid.NET 框架的示例代码库，展示了三维建模、运动仿真和高级几何处理的核心功能。

### 技术栈
- **语言**: C# .NET (.NET 6.0/8.0/10.0) / Python (API4AI)
- **UI 框架**: WinForms / WPF / Avalonia (跨平台)
- **核心库**: AnyCAD.Foundation, AnyCAD.Simulate, AnyCAD.QuickSolid
- **坐标系**: 右手坐标系，Z 轴向上

---

## 🏗️ 项目结构

```
anycad.rapid.net.sample/
├── API4AI/                  # 🤖 AI 辅助参数化建模子模块
│   ├── doc/                # API 文档（YAML 格式）
│   │   ├── AnyCAD_Foundation/   # 基础几何 API
│   │   ├── AnyCAD_Platform/     # 平台相关 API
│   │   ├── AnyCAD_QuickSolid/   # 快速建模 API
│   │   ├── AnyCAD_Simulate/     # 仿真 API
│   │   └── AnyCAD_PointCloud/   # 点云处理 API
│   ├── tools/              # 工具脚本
│   │   └── run_script.py   # 脚本执行工具
│   ├── sample.py           # 基础示例模板
│   ├── chair.py            # 参数化椅子示例
│   └── README.md           # 详细文档
├── AnyCAD.Basic/              # 基础功能示例
│   ├── Analysis/             # 几何分析（曲线、曲面、距离等）
│   ├── Geometry/             # 几何造型（布尔运算、拉伸、旋转等）
│   ├── Graphics/             # 图形显示（材质、纹理、粒子等）
│   ├── Interaction/          # 交互操作（拾取、测量等）
│   └── TestCaseLoader.cs     # 测试用例加载器
├── AnyCAD.Advanced/           # 高级功能示例
│   ├── Analysis/             # 高级分析
│   ├── Graphics/             # 高级图形（CAE、点云等）
│   ├── Interaction/          # 复杂交互
│   ├── Interop/              # 数据交换（DXF/DWG 导入）
│   ├── Simulation/           # 物理仿真（碰撞、光线等）
│   └── TestCaseLoader.cs
├── AnyCAD.QuickSolid/         # 快速建模工具
│   ├── Analysis/             # 曲率分析、孔洞检测
│   ├── Curve/                # 曲线处理
│   └── Geometry/             # 特殊造型（展开、投影等）
├── AnyCAD.WPF.App/            # WPF 应用程序入口
├── AnyCAD.WinForms.App/       # WinForms 应用程序入口
├── AnyCAD.AvaloniaApp/        # Avalonia 跨平台应用入口
└── data/                      # 资源数据文件
```

---

## 🎯 核心概念

### TestCase 基类 (.NET)

所有 C# 示例都继承自 `TestCase` 抽象类，提供以下生命周期方法：

```csharp
class MyTestCase : TestCase
{
    // 初始化：创建场景、几何体、材质等
  public override void Run(IRenderView render)
    {
        // 在此处创建和显示几何体
    }
    
    // 动画回调（如果启用动画）
  public override void Animation(IRenderView render, float time)
    {
        // time: 毫秒，从 0 开始递增
        // 用于更新物体位置、变形等
    }
    
    // 清理资源
  public override void Exit(IRenderView render)
    {
        // 禁用动画、释放资源等
    }
    
    // 选择变化回调
  public override void OnSelectionChanged(IRenderView render, PickedResult result)
    {
        // 处理用户选择的物体
    }
}
```

### API4AI 脚本结构 (Python)

API4AI 子模块使用 Python 脚本进行参数化建模：

```python
# 定义参数
length = ctx.GetDouble("长度", 100.0)
width = ctx.GetDouble("宽度", 50.0)
height = ctx.GetDouble("高度", 30.0)

# 创建几何体
box = MakeBox(length, width, height)

# 设置结果
ctx.SetShape(box)
```

---

## 📖 示例分类与模板

### C# .NET 模板

#### 类型 1: 静态几何显示

**文件位置**: `AnyCAD.Basic/Graphics/` 或 `AnyCAD.Basic/Geometry/`

```csharp
using AnyCAD.Foundation;

namespace AnyCAD.Demo.Graphics
{
    class Graphics_MyExample: TestCase
    {
      public override void Run(IRenderView render)
        {
            // 1. 创建几何体
            var shape = ShapeBuilder.MakeCylinder(GP.XOY(), 10, 50, Math.PI * 2);
            
            // 2. 创建材质
            var material = MeshPhongMaterial.Create("my-material");
            material.SetColor(ColorTable.Blue);
            
            // 3. 创建场景节点并显示
            var node = BrepSceneNode.Create(shape, material, null);
            render.ShowSceneNode(node);
        }
    }
}
```

#### 类型 2: 动画模拟

**文件位置**: `AnyCAD.Advanced/Simulation/` 或 `AnyCAD.Basic/Graphics/`

```csharp
using AnyCAD.Foundation;
using System.Collections.Generic;

namespace AnyCAD.Demo.Graphics
{
    class Graphics_AnimationExample : TestCase
    {
        List<BrepSceneNode> mObjects = new List<BrepSceneNode>();
        
      public override void Run(IRenderView render)
        {
            // 创建初始物体
            var shape = GeometryBuilder.CreateSphere(5, 32, 32);
            var material = MeshPhongMaterial.Create("sphere.mat");
            material.SetColor(ColorTable.Red);
            
            var node = new PrimitiveSceneNode(shape, material);
            render.ShowSceneNode(node);
            mObjects.Add(node);
            
            // 启用动画
            render.EnableAnimation(true);
        }
        
      public override void Animation(IRenderView render, float time)
        {
            // time 单位：毫秒
            foreach (var obj in mObjects)
            {
                // 创建变换矩阵
                var rotation = Matrix4d.makeRotationAxis(Vector3d.UNIT_Y, time * 0.001);
                var translation = Matrix4d.makeTranslation(0, Math.Sin(time * 0.002) * 20, 0);
                
                // 应用变换
                obj.SetTransform(rotation * translation);
                obj.RequestUpdate();
            }
            
            // 请求重绘
            render.RequestDraw(EnumUpdateFlags.Scene);
        }
        
      public override void Exit(IRenderView render)
        {
            render.EnableAnimation(false);
        }
    }
}
```

### Python API4AI 模板

#### 类型 3: 参数化建模

**文件位置**: `API4AI/*.py`

```python
# 1. 定义参数
length = ctx.GetDouble("长度", 100.0)
width = ctx.GetDouble("宽度", 50.0)
thickness = ctx.GetDouble("厚度", 5.0)

# 2. 创建基础形状
base = MakeBox(length, width, thickness)

# 3. 添加特征（可选）
if ctx.GetBool("添加孔洞", True):
    hole = MakeCylinder(width/4, thickness * 2)
    hole = Move(hole, 0, 0, thickness)
    base = Cut(base, hole)

# 4. 设置结果
ctx.SetShape(base)
```

#### 类型 4: 复杂参数化组件

```python
# 参数化椅子示例
def create_chair():
    # 主要尺寸
    seat_height = ctx.GetDouble("座椅高度", 450.0)
    seat_width = ctx.GetDouble("座椅宽度", 500.0)
    seat_depth = ctx.GetDouble("座椅深度", 500.0)
    backrest_height = ctx.GetDouble("靠背高度", 900.0)
    
    # 1. 创建座椅面
    seat = MakeBox(seat_width, seat_depth, 20)
    seat = Move(seat, -seat_width/2, 0, seat_height)
    
    # 2. 创建椅腿
    leg_profile = 40.0
    legs = TopoShapeList()
    
    for x in [-1, 1]:
        for y in [-1, 1]:
            leg = MakeBox(leg_profile, leg_profile, seat_height)
            leg = Move(leg, x * (seat_width/2 - leg_profile/2), 
                      y * (seat_depth/2 - leg_profile/2), 0)
            legs.append(leg)
    
    # 3. 创建靠背
    backrest = MakeBox(seat_width, 20, backrest_height - seat_height)
    backrest = Move(backrest, -seat_width/2, -seat_depth/2, seat_height)
    
    # 4. 合并所有部分
    result = Fuse(seat, legs)
    result = Fuse(result, backrest)
    
    # 5. 倒角（可选）
    if ctx.GetBool("倒角", True):
        result = Fillet(result, [seat_edges], 5.0)
    
    ctx.SetShape(result)

create_chair()
```

---

## 🔧 常用 API 参考

### C# API

#### 几何造型 (ShapeBuilder)

```csharp
// 基础几何体
ShapeBuilder.MakeBox(axis, lx, ly, lz)           // 立方体
ShapeBuilder.MakeSphere(center, radius)           // 球体
ShapeBuilder.MakeCylinder(axis, radius, height, angle)  // 圆柱
ShapeBuilder.MakeCone(axis, r1, r2, height, angle)     // 圆锥
ShapeBuilder.MakeTorus(axis, r1, r2, angle1, angle2)   // 圆环

// 扫描和放样
FeatureTool.Extrude(shape, distance, direction)   // 拉伸
FeatureTool.Revolve(shape, axis, angle)           // 旋转
FeatureTool.Loft(wire1, wire2, solid)             // 放样
FeatureTool.Sweep(profile, path, mode)            // 扫掠

// 布尔运算
BooleanTool.Unify(shape1, shape2, glue, fuse)     // 并集
BooleanTool.Cut(shape1, shape2)                   // 差集
BooleanTool.Intersect(shape1, shape2)             // 交集

// 特征操作
FeatureTool.Fillet(shape, edges, radius)          // 倒圆角
FeatureTool.Chamfer(shape, edges, distance)       // 倒角
FeatureTool.Shell(shape, faces, thickness)        // 抽壳
```

#### 草图绘制 (SketchBuilder)

```csharp
// 曲线
SketchBuilder.MakeLine(p1, p2)                    // 直线
SketchBuilder.MakeCircle(center, radius, dir)     // 圆
SketchBuilder.MakeArc(p1, p2, p3)                 // 圆弧
SketchBuilder.MakeEllipse(center, r1, r2, xDir, zDir)  // 椭圆
SketchBuilder.MakeBSpline(points, weights, knots) // B 样条

// 面
SketchBuilder.MakePlanarFace(wire)                // 平面
SketchBuilder.MakeRectangle(axis, lx, ly, r, closed)  // 矩形

// 特征
SketchBuilder.MakeVertex(point)                   // 顶点
```

#### 变换操作

```csharp
// 平移
Matrix4d.makeTranslation(x, y, z)
Matrix4d.makeTranslation(vector)

// 旋转
Matrix4d.makeRotationAxis(axis, angle)            // 绕轴旋转

// 缩放
Matrix4d.makeScale(sx, sy, sz)
Matrix4d.makeScale(factor)                        // 均匀缩放

// 组合变换
var transform = Matrix4d.makeTranslation(10, 0, 0) 
              * Matrix4d.makeRotationZ(Math.PI / 4)
              * Matrix4d.makeScale(2);
```

### Python API4AI API

#### 基础几何创建

```python
# 立方体
box = MakeBox(length, width, height)

# 圆柱
cylinder = MakeCylinder(radius, height)

# 圆锥
cone = MakeCone(radius_bottom, radius_top, height)

# 球体
sphere = MakeSphere(radius)

# 圆环
torus = MakeTorus(inner_radius, outer_radius)
```

#### 布尔运算

```python
# 并集
result = Fuse(shape1, shape2)

# 差集
result = Cut(shape1, shape2)

# 交集
result = Common(shape1, shape2)

# 多个形状的并集
shapes = TopoShapeList()
shapes.append(shape1)
shapes.append(shape2)
result = Fuse(shapes)
```

#### 变换操作

```python
# 平移
moved = Move(shape, dx, dy, dz)

# 旋转
rotated = Rotate(shape, angle_degrees, axis_x, axis_y, axis_z)

# 镜像
mirrored = Mirror(shape, plane_normal_x, plane_normal_y, plane_normal_z)

# 缩放
scaled = Scale(shape, factor)
```

#### 特征操作

```python
# 倒圆角
filleted = Fillet(shape, edges, radius)

# 倒角
chamfered = Chamfer(shape, edges, distance)

# 抽壳
shelled = Shell(shape, faces_to_remove, thickness)

# 拉伸
extruded = Extrude(face, distance, direction)

# 旋转
revolved = Revolve(face, axis, angle_degrees)
```

#### 参数输入

```python
# 数值参数
length = ctx.GetDouble("长度", 100.0)  # (名称，默认值)
width = ctx.GetDouble("宽度", 50.0)

# 布尔参数
add_hole = ctx.GetBool("添加孔洞", True)

# 整数参数
segments = ctx.GetInt("分段数", 32)

# 字符串参数
name = ctx.GetString("名称", "MyPart")

# 颜色参数
color = ctx.GetColor("颜色", 255, 0, 0)
```

#### 材质和显示

```python
# 设置材质颜色
SetColor(shape, r, g, b)  # RGB 0-255

# 设置透明度
SetTransparency(shape, alpha)  # 0-1

# 设置显示名称
SetName(shape, "MyComponent")
```

---

## 📝 命名约定

### C# 类名
- **Graphics_***: 图形显示相关
- **Geometry_***: 几何造型相关
- **Analysis_***: 分析计算相关
- **Simulation_***: 物理仿真相关
- **Interaction_***: 交互操作相关

### Python 函数和变量
- `snake_case`: 函数名和变量名
- `PascalCase`: 类名（如果定义类）
- 使用中文注释说明功能

### 文件组织
- C# 示例：每个示例一个独立的 `.cs` 文件
- Python 脚本：每个模型一个 `.py` 文件
- 文件名与功能一致

---

## 🚀 快速开始

### C# .NET 示例开发步骤

1. **确定分类**: 根据功能选择目录（Graphics/Geometry/Simulation 等）

2. **创建文件**: 在对应目录下创建新的 `.cs` 文件

3. **继承 TestCase**: 
```csharp
using AnyCAD.Foundation;

namespace AnyCAD.Demo.Graphics
{
    class Graphics_MyExample: TestCase
    {
      public override void Run(IRenderView render)
        {
            // 你的代码
        }
    }
}
```

4. **实现功能**: 参考现有示例编写代码

5. **测试运行**: 编译并运行对应的 App 项目

6. **添加文档**: 在类上方添加 XML 注释说明功能

### Python API4AI 脚本开发步骤

1. **复制模板**: 从 `sample.py` 开始

2. **定义参数**: 使用 `ctx.GetDouble()` 等方法定义可调参数

3. **创建几何**: 调用 API 函数创建 3D 模型

4. **设置结果**: 使用 `ctx.SetShape()` 存储最终模型

5. **测试运行**: 
   - 方式 1: 在 VS Code 中按 `Ctrl+Shift+B`
   - 方式 2: 运行 `python tools/run_script.py your_script.py`

6. **调试优化**: 调整参数直到获得满意的结果

---

## 🎨 最佳实践

### C# 性能优化

```csharp
// ✅ 好的做法：使用 GroupSceneNode 管理多个物体
var group = new GroupSceneNode();
for (int i = 0; i < 100; i++)
{
    var node = CreateNode(i);
    group.AddNode(node);
}
render.ShowSceneNode(group);

// ❌ 避免：创建过多独立场景节点
for (int i = 0; i < 100; i++)
{
    var node = CreateNode(i);
    render.ShowSceneNode(node);  // 降低性能
}
```

### Python 参数化设计最佳实践

```python
# ✅ 好的做法：模块化设计
def create_leg(position_x, position_y, height):
    """创建单条椅腿"""
    leg = MakeBox(40, 40, height)
    leg = Move(leg, position_x, position_y, 0)
    return leg

def create_seat(width, depth, height):
    """创建座椅面"""
    seat = MakeBox(width, depth, 20)
    seat = Move(seat, -width/2, 0, height)
    return seat

# 主函数
def main():
    width = ctx.GetDouble("宽度", 500.0)
    depth = ctx.GetDouble("深度", 500.0)
    height = ctx.GetDouble("高度", 450.0)
    
    seat = create_seat(width, depth, height)
    
    legs = TopoShapeList()
    for x in [-1, 1]:
        for y in [-1, 1]:
            leg_pos_x = x * (width/2 - 20)
            leg_pos_y = y * (depth/2 - 20)
            legs.append(create_leg(leg_pos_x, leg_pos_y, height))
    
    result = Fuse(seat, legs)
    ctx.SetShape(result)

main()
```

### 动画优化

```csharp
// ✅ 好的做法：限制更新频率
float mLastTime = 0;
public override void Animation(IRenderView render, float time)
{
    if (time - mLastTime < 16) return;  // ~60fps
    mLastTime = time;
    
    UpdateScene();
    render.RequestDraw(EnumUpdateFlags.Scene);
}

// ✅ 使用 RequestUpdate 标记需要更新的节点
node.SetTransform(transform);
node.RequestUpdate();  // 只更新变化的节点
```

### 内存管理

```csharp
// ✅ 好的做法：缓存材质和几何体
MaterialInstance mCachedMaterial;

public override void Run(IRenderView render)
{
    mCachedMaterial = MeshPhongMaterial.Create("shared.mat");
    mCachedMaterial.SetColor(ColorTable.Blue);
}

for (int i = 0; i < 100; i++)
{
    // 重复使用同一材质
    var node = CreateNode(mCachedMaterial);
}

// ❌ 避免：为每个物体创建新材质
for (int i = 0; i < 100; i++)
{
    var mat = MeshPhongMaterial.Create($"mat{i}");  // 浪费内存
    var node = CreateNode(mat);
}
```

---

## 🔍 调试技巧

### C# 调试

```csharp
// 查看坐标系
var axis = ArrowWidget.Create(0.1f, 10, null);
axis.SetLocation(Vector3.Zero, Vector3.UNIT_X);
render.ShowSceneNode(axis);

// 输出信息
System.Diagnostics.Debug.WriteLine($"Value: {value}");
Console.WriteLine($"Time: {time}");

// 可视化调试
var points = GeometryBuilder.CreatePoints(positions, colors, null);
render.ShowSceneNode(new PrimitiveSceneNode(points, null));
```

### Python 调试

```python
# 打印调试信息
print(f"Length: {length}")
print(f"Created {len(shapes)} shapes")

# 显示中间结果
ctx.SetShape(intermediate_shape)  # 临时显示

# 参数验证
if radius <= 0:
    print("错误：半径必须大于 0")
    radius = 10.0  # 使用默认值
```

---

## 📦 依赖和环境

### C# .NET 环境

#### 必需依赖
- Microsoft Visual C++ Runtime Library
- .NET SDK 6.0 / 8.0 / 10.0

#### 可选依赖
- Avalonia UI (用于跨平台支持)

#### 构建命令
```bash
dotnet build
dotnet run --project AnyCAD.WPF.App
```

### Python API4AI 环境

#### 系统要求
- AnyCAD NEXT (下载链接见 API4AI/README.md)
- Python 3.7+
- VS Code (推荐)

#### 配置步骤
1. 安装 AnyCAD NEXT
2. 启动 AnyCAD HTTP 服务（监听 8080 端口）
3. 在 VS Code 中打开 API4AI 目录
4. 配置任务（已包含 .vscode/tasks.json）

#### 运行脚本
```bash
# 方式 1: 使用工具脚本
python tools/run_script.py your_script.py

# 方式 2: VS Code 快捷键
# Ctrl+Shift+B (发送当前文件到 AnyCAD)

# 方式 3: 直接在 AnyCAD 中打开脚本运行
```

---

## 🌐 参考资料

- [入门教程](http://www.anycad.cn/guide/)
- [API 文档](http://www.anycad.cn/api/classes.html)
- [更多示例](https://gitee.com/anycad/rapid.net.starter)
- [高级示例](https://gitee.com/anycad/RapidCAX)
- [API4AI 详细文档](API4AI/README.md)

---

## 💡 常见问题

### Q: 如何选择合适的几何创建方式？
**A**: 
- 需要精确 CAD 几何（布尔运算、工程图）→ 使用 `ShapeBuilder` + `BrepSceneNode`
- 只需要可视化（游戏、动画）→ 使用 `GeometryBuilder` + `PrimitiveSceneNode`
- 绘制曲线草图 → 使用 `SketchBuilder`
- 参数化 AI 生成 → 使用 `API4AI` Python 脚本

### Q: 动画时间单位是什么？
**A**: `Animation`方法的`time`参数单位是毫秒（ms），从 0 开始递增。

### Q: 如何实现物体跟随鼠标？
**A**: 继承 `BaseEditor` 类，重写 `OnMouseMove` 方法，使用`ctx.SnapPoint(x, y)`获取 3D 坐标。

### Q: 如何优化大量物体的渲染？
**A**: 
1. 使用 `GroupSceneNode` 统一管理
2. 共享材质实例
3. 使用实例化渲染（如果支持）
4. 减少 Draw Call（合并几何体）

### Q: API4AI 脚本如何在 AnyCAD 中运行？
**A**: 
1. 确保 AnyCAD HTTP 服务正在运行
2. 在 VS Code 中打开.py 文件
3. 按 `Ctrl+Shift+B` 发送到AnyCAD
4. 或在命令行运行 `python tools/run_script.py script.py`

### Q: 如何训练 AI 生成参数化脚本？
**A**: 
1. 收集现有的 Python 脚本作为训练数据
2. 标注参数和功能描述
3. 使用大语言模型（如 GPT）进行微调
4. 提供清晰的 API 文档和使用示例
5. 建立反馈机制优化生成质量

---

## 🎓 学习路径

### 初级
1. 阅读 `AnyCAD.Basic/Graphics/` 下的简单示例
2. 学习基本几何体创建（球、立方体、圆柱）
3. 理解材质和颜色系统
4. 掌握坐标变换
5. 尝试简单的 API4AI Python 脚本

### 中级
1. 学习 `AnyCAD.Basic/Geometry/` 中的造型示例
2. 掌握布尔运算和特征操作
3. 学习动画系统 (`Animation` 方法)
4. 理解场景图结构
5. 创建复杂的参数化 Python 脚本

### 高级
1. 研究 `AnyCAD.Advanced/Simulation/` 物理仿真
2. 学习自定义 Editor 和交互
3. 掌握 Shader 编程
4. 优化性能和内存管理
5. 开发 AI 辅助的参数化建模工具

---

## 🤖 AI 开发专项指南

### 代码生成提示词模板

```markdown
你是一位 AnyCAD 参数化建模专家。请根据以下需求创建 Python 脚本：

**功能描述**: [描述要创建的 3D 模型]
**主要参数**: [列出可调参数及其默认值]
**特殊要求**: [如倒角、孔洞等特征]

请使用 API4AI 规范编写代码，包括：
1. 参数定义（使用 ctx.GetDouble 等）
2. 几何创建
3. 布尔运算
4. 特征处理
5. 结果设置（ctx.SetShape）
```

### 训练数据格式

```python
# 示例：参数化桌子
# 参数：桌面长度、宽度、高度，桌腿数量
length = ctx.GetDouble("桌面长度", 1200.0)
width = ctx.GetDouble("桌面宽度", 600.0)
height = ctx.GetDouble("桌子高度", 750.0)
legs_count = ctx.GetInt("桌腿数量", 4)

# 创建桌面
top = MakeBox(length, width, 30)
top = Move(top, -length/2, -width/2, height)

# 创建桌腿
legs = TopoShapeList()
leg_spacing_x = length / (legs_count // 2)
leg_spacing_y = width / 2

for i in range(legs_count):
    x = -length/2 + (i % (legs_count//2)) * leg_spacing_x
    y = -width/2 if i < legs_count//2 else width/2
    leg = MakeBox(50, 50, height)
    leg = Move(leg, x, y, 0)
    legs.append(leg)

# 合并
result = Fuse(top, legs)

# 倒角
edges = GetEdges(result)
result = Fillet(result, edges, 5.0)

ctx.SetShape(result)
```

### 质量检查清单

- [ ] 所有参数都有合理的默认值
- [ ] 代码有清晰的中文注释
- [ ] 使用了模块化的函数结构
- [ ] 包含了必要的错误处理
- [ ] 符合 API4AI 编码规范
- [ ] 可以在 AnyCAD 中成功运行
- [ ] 生成的模型几何正确

---

**最后更新**: 2026-03-10  
**维护者**: AnyCAD Team