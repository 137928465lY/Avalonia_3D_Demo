# AnyCAD Rapid .NET Sample

本代码仓库包含包含基础功能、运动仿真和高级建模的演示。

## 0 整体说明

### 0.1 程序入口
界面可以选用 WinForms、WPF、AvaloniaUI，分别对应不同的 App 程序。

- WinForms 简单应用：AnyCAD.WinForms.App
- WPF 简单应用：AnyCAD.WPF.App
- Avalonia 简单应用：AnyCAD.AvaloniaApp

### 0.2 功能实现
- 基础功能：位于 AnyCAD.Basic 工程中
- 运动仿真：位于 AnyCAD.Advanced 工程中。
- 高级建模：位于 AnyCAD.QuickSolid 工程中

### 0.3 分支说明:
 - R2022: 2022 分支
 - R2023: 2023 分支
 - R2024: 2024 分支
 - R2025: 2025 分支
 - master: 2026 分支

### 0.4 API4AI 子模块

**API4AI** 是一个独立的子模块，专注于通过 Python 脚本和 AI 辅助生成参数化 3D 模型。

#### 主要特性
- 🤖 **AI 友好**: 为 AI 模型训练和优化设计的 API 接口
- 📝 **Python 脚本**: 使用简洁的 Python 语法创建复杂 3D 模型
- 🔧 **参数化设计**: 支持交互式参数调整
- 🌐 **HTTP 服务**: 可通过网络与 AnyCAD 主程序通信
- 📚 **完整文档**: 包含详细的 API 定义和使用示例

#### 快速开始
```bash
# 进入 API4AI 目录
cd API4AI

# 运行示例脚本
python tools/run_script.py sample.py

# 或在 VS Code 中按 Ctrl+Shift+B 发送当前脚本到 AnyCAD
```

#### 项目结构
```
API4AI/
├── doc/                    # API 文档（YAML 格式）
│   ├── AnyCAD_Foundation/  # 基础几何 API
│   ├── AnyCAD_Platform/    # 平台相关 API
│   ├── AnyCAD_QuickSolid/  # 快速建模 API
│   ├── AnyCAD_Simulate/    # 仿真 API
│   └── AnyCAD_PointCloud/  # 点云处理 API
├── tools/                  # 工具脚本
│   └── run_script.py       # 脚本执行工具
├── sample.py               # 基础示例模板
├── chair.py                # 参数化椅子示例
└── README.md               # 详细文档
```

#### 应用场景
- 🎯 **AI 代码生成**: 训练 AI 模型自动生成参数化建模脚本
- 📐 **参数化设计**: 快速创建可调整尺寸的 3D 模型
- 🎨 **设计探索**: 通过修改参数快速迭代设计方案
- 📖 **教学演示**: 学习 AnyCAD API 的交互式环境

**详细文档**: [API4AI/README.md](API4AI/README.md)

## 1 环境准备

### 1.1 Windows
#### 1.1.1 Microsoft Visual C++ Runtime Library

低于VS2022的版本需要下载C++运行时库，下载地址: 
- [vc_redist.x64](https://aka.ms/vs/17/release/vc_redist.x64.exe)
- [vc_redist.x86](https://aka.ms/vs/17/release/vc_redist.x86.exe)

#### 1.2.2 .NET Framework
支持 .NET Framework 4.8
#### 1.3.2 .NET 6.0、8.0、10.0
推荐使用8.0/10.0


## 2 程序示例

本仓库共包含 **120** 个可运行示例，全部通过以下 App 的目录树切换查看：AnyCAD.WinForms.App / AnyCAD.WPF.App / AnyCAD.AvaloniaApp。
下表列出了全部可正常运行并截图成功的示例：每个示例均给出对应的源码文件（相对链接）与自动截图生成的效果图。
另有 **10** 个示例因原生崩溃或托管异常无法截图，未列入下表（清单见 [scripts/capture_skip.txt](scripts/capture_skip.txt)）。效果图存放于 [showcase](showcase) 目录，
由 [scripts](scripts/README.md) 中的工具自动运行每个示例并截图生成（三维模型自动采用等轴测视角）。

| 模块 | 工程 | 数量 | 说明 |
| --- | --- | --- | --- |
| 基础功能 | AnyCAD.Basic | 83 | 几何造型、分析、显示、交互等基础功能示例 |
| 运动仿真与高级功能 | AnyCAD.Advanced | 22 | 运动仿真（碰撞、动力学、机器人）、CAE、点云、交互测量等高级功能示例 |
| 高级造型 | AnyCAD.QuickSolid | 15 | 快速建模与高级造型（放样、展开、船体、管件等）示例 |

### 2.1 基础功能（AnyCAD.Basic，共 83 个）

**造型（37）**

| 示例 | 源码 | 效果图 |
| --- | --- | --- |
| ArcBoard | [Geometry_ArcBoard.cs](AnyCAD.Basic/Geometry/Geometry_ArcBoard.cs) | <img src="showcase/geometry.arcboard.png" width="240" alt="ArcBoard"/> |
| Boolean | [Geometry_Boolean.cs](AnyCAD.Basic/Geometry/Geometry_Boolean.cs) | <img src="showcase/geometry.boolean.png" width="240" alt="Boolean"/> |
| BooleanCut | [Geometry_BooleanCut.cs](AnyCAD.Basic/Geometry/Geometry_BooleanCut.cs) | <img src="showcase/geometry.booleancut.png" width="240" alt="BooleanCut"/> |
| BoolenCommon | [Geometry_BooleanCommon.cs](AnyCAD.Basic/Geometry/Geometry_BooleanCommon.cs) | <img src="showcase/geometry.boolencommon.png" width="240" alt="BoolenCommon"/> |
| BSpline | [Geometry_BSpline.cs](AnyCAD.Basic/Geometry/Geometry_BSpline.cs) | <img src="showcase/geometry.bspline.png" width="240" alt="BSpline"/> |
| Complex | [Geometry_Complex.cs](AnyCAD.Basic/Geometry/Geometry_Complex.cs) | <img src="showcase/geometry.complex.png" width="240" alt="Complex"/> |
| CurveOnSurface | [Geometry_CurveOnSurface.cs](AnyCAD.Basic/Geometry/Geometry_CurveOnSurface.cs) | <img src="showcase/geometry.curveonsurface.png" width="240" alt="CurveOnSurface"/> |
| Cut | [Geometry_Cut.cs](AnyCAD.Basic/Geometry/Geometry_Cut.cs) | <img src="showcase/geometry.cut.png" width="240" alt="Cut"/> |
| Extrema | [Geometry_Extrema.cs](AnyCAD.Basic/Geometry/Geometry_Extrema.cs) | <img src="showcase/geometry.extrema.png" width="240" alt="Extrema"/> |
| Fillet | [Geometry_Fillet.cs](AnyCAD.Basic/Geometry/Geometry_Fillet.cs) | <img src="showcase/geometry.fillet.png" width="240" alt="Fillet"/> |
| HalfSpace | [Geometry_Half.cs](AnyCAD.Basic/Geometry/Geometry_Half.cs) | <img src="showcase/geometry.halfspace.png" width="240" alt="HalfSpace"/> |
| HeartCurve | [Geometry_HeartCurve.cs](AnyCAD.Basic/Geometry/Geometry_HeartCurve.cs) | <img src="showcase/geometry.heartcurve.png" width="240" alt="HeartCurve"/> |
| Hole | [Geometry_Hole.cs](AnyCAD.Basic/Geometry/Geometry_Hole.cs) | <img src="showcase/geometry.hole.png" width="240" alt="Hole"/> |
| Loft | [Geometry_Loft.cs](AnyCAD.Basic/Geometry/Geometry_Loft.cs) | <img src="showcase/geometry.loft.png" width="240" alt="Loft"/> |
| LoftM | [Geometry_LoftM.cs](AnyCAD.Basic/Geometry/Geometry_LoftM.cs) | <img src="showcase/geometry.loftm.png" width="240" alt="LoftM"/> |
| OffsetSolid | [Geometry_OffsetSolid.cs](AnyCAD.Basic/Geometry/Geometry_OffsetSolid.cs) | <img src="showcase/geometry.offsetsolid.png" width="240" alt="OffsetSolid"/> |
| OffsetWire | [Geometry_OffsetWire.cs](AnyCAD.Basic/Geometry/Geometry_OffsetWire.cs) | <img src="showcase/geometry.offsetwire.png" width="240" alt="OffsetWire"/> |
| Pipe | [Geometry_Pipe.cs](AnyCAD.Basic/Geometry/Geometry_Pipe.cs) | <img src="showcase/geometry.pipe.png" width="240" alt="Pipe"/> |
| Polygon | [Geometry_Polygon.cs](AnyCAD.Basic/Geometry/Geometry_Polygon.cs) | <img src="showcase/geometry.polygon.png" width="240" alt="Polygon"/> |
| PosAxis | [Geometry_Defeature.cs](AnyCAD.Basic/Geometry/Geometry_Defeature.cs) | <img src="showcase/geometry.posaxis.png" width="240" alt="PosAxis"/> |
| Projection | [Geometry_Projection.cs](AnyCAD.Basic/Geometry/Geometry_Projection.cs) | <img src="showcase/geometry.projection.png" width="240" alt="Projection"/> |
| Pyramid | [Geometry_Pyramid.cs](AnyCAD.Basic/Geometry/Geometry_Pyramid.cs) | <img src="showcase/geometry.pyramid.png" width="240" alt="Pyramid"/> |
| RectPipeStep | [Geometry_RectPipeStep.cs](AnyCAD.Basic/Geometry/Geometry_RectPipeStep.cs) | <img src="showcase/geometry.rectpipestep.png" width="240" alt="RectPipeStep"/> |
| Section | [Geometry_Section.cs](AnyCAD.Basic/Geometry/Geometry_Section.cs) | <img src="showcase/geometry.section.png" width="240" alt="Section"/> |
| Sew | [Geometry_Sew.cs](AnyCAD.Basic/Geometry/Geometry_Sew.cs) | <img src="showcase/geometry.sew.png" width="240" alt="Sew"/> |
| Sketch | [Geometry_Sketch.cs](AnyCAD.Basic/Geometry/Geometry_Sketch.cs) | <img src="showcase/geometry.sketch.png" width="240" alt="Sketch"/> |
| Slice | [Geometry_Slice.cs](AnyCAD.Basic/Geometry/Geometry_Slice.cs) | <img src="showcase/geometry.slice.png" width="240" alt="Slice"/> |
| Spring | [Geometry_Spring.cs](AnyCAD.Basic/Geometry/Geometry_Spring.cs) | <img src="showcase/geometry.spring.png" width="240" alt="Spring"/> |
| Surface | [Geometry_Surface.cs](AnyCAD.Basic/Geometry/Geometry_Surface.cs) | <img src="showcase/geometry.surface.png" width="240" alt="Surface"/> |
| Sweep | [Geometry_Sweep.cs](AnyCAD.Basic/Geometry/Geometry_Sweep.cs) | <img src="showcase/geometry.sweep.png" width="240" alt="Sweep"/> |
| SweepByFrenet | [Geometry_SweepByFrenet.cs](AnyCAD.Basic/Geometry/Geometry_SweepByFrenet.cs) | <img src="showcase/geometry.sweepbyfrenet.png" width="240" alt="SweepByFrenet"/> |
| SweepBySections | [Geometry_SweepBySections.cs](AnyCAD.Basic/Geometry/Geometry_SweepBySections.cs) | <img src="showcase/geometry.sweepbysections.png" width="240" alt="SweepBySections"/> |
| SweepLoft | [Geometry_SweepLoft.cs](AnyCAD.Basic/Geometry/Geometry_SweepLoft.cs) | <img src="showcase/geometry.sweeploft.png" width="240" alt="SweepLoft"/> |
| TopoExplor | [Geometry_TopoExplor.cs](AnyCAD.Basic/Geometry/Geometry_TopoExplor.cs) | <img src="showcase/geometry.topoexplor.png" width="240" alt="TopoExplor"/> |
| Transform | [Geometry_Transform.cs](AnyCAD.Basic/Geometry/Geometry_Transform.cs) | <img src="showcase/geometry.transform.png" width="240" alt="Transform"/> |
| Window | [Geometry_Window.cs](AnyCAD.Basic/Geometry/Geometry_Window.cs) | <img src="showcase/geometry.window.png" width="240" alt="Window"/> |
| Wire | [Geometry_Wire.cs](AnyCAD.Basic/Geometry/Geometry_Wire.cs) | <img src="showcase/geometry.wire.png" width="240" alt="Wire"/> |

**Welcome（1）**

| 示例 | 源码 | 效果图 |
| --- | --- | --- |
| AnyCAD | [TestCaseLoader.cs](AnyCAD.Basic/TestCaseLoader.cs) | <img src="showcase/welcome.anycad.png" width="240" alt="AnyCAD"/> |

**显示（33）**

| 示例 | 源码 | 效果图 |
| --- | --- | --- |
| 100000Box | [Graphics_100000Box.cs](AnyCAD.Basic/Graphics/Graphics_100000Box.cs) | <img src="showcase/graphics.100000box.png" width="240" alt="100000Box"/> |
| 100000Sphere | [Graphics_100000Sphere.cs](AnyCAD.Basic/Graphics/Graphics_100000Sphere.cs) | <img src="showcase/graphics.100000sphere.png" width="240" alt="100000Sphere"/> |
| Axis | [Graphics_Axis.cs](AnyCAD.Basic/Graphics/Graphics_Axis.cs) | <img src="showcase/graphics.axis.png" width="240" alt="Axis"/> |
| BinaryFractalTree | [Graphics_BinaryFractalTree.cs](AnyCAD.Basic/Graphics/Graphics_BinaryFractalTree.cs) | <img src="showcase/graphics.binaryfractaltree.png" width="240" alt="BinaryFractalTree"/> |
| Clip | [Graphics_Clip.cs](AnyCAD.Basic/Graphics/Graphics_Clip.cs) | <img src="showcase/graphics.clip.png" width="240" alt="Clip"/> |
| DashedLines | [Graphics_DashedLines.cs](AnyCAD.Basic/Graphics/Graphics_DashedLines.cs) | <img src="showcase/graphics.dashedlines.png" width="240" alt="DashedLines"/> |
| Delete | [Graphics_Delete.cs](AnyCAD.Basic/Graphics/Graphics_Delete.cs) | <img src="showcase/graphics.delete.png" width="240" alt="Delete"/> |
| Dimension | [Graphics_Dimension.cs](AnyCAD.Basic/Graphics/Graphics_Dimension.cs) | <img src="showcase/graphics.dimension.png" width="240" alt="Dimension"/> |
| FaceStyle | [Graphics_FaceStyle.cs](AnyCAD.Basic/Graphics/Graphics_FaceStyle.cs) | <img src="showcase/graphics.facestyle.png" width="240" alt="FaceStyle"/> |
| FourierPi | [Graphics_FourierPi.cs](AnyCAD.Basic/Graphics/Graphics_FourierPi.cs) | <img src="showcase/graphics.fourierpi.png" width="240" alt="FourierPi"/> |
| Geometry | [Graphics_Geometry.cs](AnyCAD.Basic/Graphics/Graphics_Geometry.cs) | <img src="showcase/graphics.geometry.png" width="240" alt="Geometry"/> |
| GeometryDynamic | [Graphics_GeometryDynamic.cs](AnyCAD.Basic/Graphics/Graphics_GeometryDynamic.cs) | <img src="showcase/graphics.geometrydynamic.png" width="240" alt="GeometryDynamic"/> |
| JacobsLadder | [Graphics_JacobsLadder.cs](AnyCAD.Basic/Graphics/Graphics_JacobsLadder.cs) | <img src="showcase/graphics.jacobsladder.png" width="240" alt="JacobsLadder"/> |
| KochCurve | [Graphics_KochCurve.cs](AnyCAD.Basic/Graphics/Graphics_KochCurve.cs) | <img src="showcase/graphics.kochcurve.png" width="240" alt="KochCurve"/> |
| LineStyle | [Graphics_LineStyle.cs](AnyCAD.Basic/Graphics/Graphics_LineStyle.cs) | <img src="showcase/graphics.linestyle.png" width="240" alt="LineStyle"/> |
| LissajousTable | [Graphics_LissajousTable.cs](AnyCAD.Basic/Graphics/Graphics_LissajousTable.cs) | <img src="showcase/graphics.lissajoustable.png" width="240" alt="LissajousTable"/> |
| LSystem | [Graphics_LSystem.cs](AnyCAD.Basic/Graphics/Graphics_LSystem.cs) | <img src="showcase/graphics.lsystem.png" width="240" alt="LSystem"/> |
| Material | [Graphics_Material.cs](AnyCAD.Basic/Graphics/Graphics_Material.cs) | <img src="showcase/graphics.material.png" width="240" alt="Material"/> |
| ModMultiplication | [Graphics_ModMultiplication.cs](AnyCAD.Basic/Graphics/Graphics_ModMultiplication.cs) | <img src="showcase/graphics.modmultiplication.png" width="240" alt="ModMultiplication"/> |
| Motion | [Graphics_Motion.cs](AnyCAD.Basic/Graphics/Graphics_Motion.cs) | <img src="showcase/graphics.motion.png" width="240" alt="Motion"/> |
| OverrideMaterial | [Graphics_OverrideMaterial.cs](AnyCAD.Basic/Graphics/Graphics_OverrideMaterial.cs) | <img src="showcase/graphics.overridematerial.png" width="240" alt="OverrideMaterial"/> |
| Points | [Graphics_Points.cs](AnyCAD.Basic/Graphics/Graphics_Points.cs) | <img src="showcase/graphics.points.png" width="240" alt="Points"/> |
| PointsGroup | [Graphics_Points_Group.cs](AnyCAD.Basic/Graphics/Graphics_Points_Group.cs) | <img src="showcase/graphics.pointsgroup.png" width="240" alt="PointsGroup"/> |
| PointsHit | [Graphics_PointsHit.cs](AnyCAD.Basic/Graphics/Graphics_PointsHit.cs) | <img src="showcase/graphics.pointshit.png" width="240" alt="PointsHit"/> |
| Ray | [Graphics_Ray.cs](AnyCAD.Basic/Graphics/Graphics_Ray.cs) | <img src="showcase/graphics.ray.png" width="240" alt="Ray"/> |
| RenderOrder | [Graphics_RenderOrder.cs](AnyCAD.Basic/Graphics/Graphics_RenderOrder.cs) | <img src="showcase/graphics.renderorder.png" width="240" alt="RenderOrder"/> |
| RosslerAttractor | [Graphics_RosslerAttractor.cs](AnyCAD.Basic/Graphics/Graphics_RosslerAttractor.cs) | <img src="showcase/graphics.rosslerattractor.png" width="240" alt="RosslerAttractor"/> |
| SceneIterator | [Graphics_SceneIterator.cs](AnyCAD.Basic/Graphics/Graphics_SceneIterator.cs) | <img src="showcase/graphics.sceneiterator.png" width="240" alt="SceneIterator"/> |
| ShaderBlackHole | [Graphics_ShaderBlackHole.cs](AnyCAD.Basic/Graphics/Graphics_ShaderBlackHole.cs) | <img src="showcase/graphics.shaderblackhole.png" width="240" alt="ShaderBlackHole"/> |
| Shape2D | [Graphics_Shape2D.cs](AnyCAD.Basic/Graphics/Graphics_Shape2D.cs) | <img src="showcase/graphics.shape2d.png" width="240" alt="Shape2D"/> |
| SierpinskiTriangle | [Graphics_SierpinskiTriangle.cs](AnyCAD.Basic/Graphics/Graphics_SierpinskiTriangle.cs) | <img src="showcase/graphics.sierpinskitriangle.png" width="240" alt="SierpinskiTriangle"/> |
| Text | [Graphics_Text.cs](AnyCAD.Basic/Graphics/Graphics_Text.cs) | <img src="showcase/graphics.text.png" width="240" alt="Text"/> |
| Texture | [Graphics_Texture.cs](AnyCAD.Basic/Graphics/Graphics_Texture.cs) | <img src="showcase/graphics.texture.png" width="240" alt="Texture"/> |

**交互（2）**

| 示例 | 源码 | 效果图 |
| --- | --- | --- |
| PickEdge | [Interaction_PickEdge.cs](AnyCAD.Basic/Interaction/Interaction_PickEdge.cs) | <img src="showcase/interaction.pickedge.png" width="240" alt="PickEdge"/> |
| PickFace | [Interaction_PickFace.cs](AnyCAD.Basic/Interaction/Interaction_PickFace.cs) | <img src="showcase/interaction.pickface.png" width="240" alt="PickFace"/> |

**分析（10）**

| 示例 | 源码 | 效果图 |
| --- | --- | --- |
| Curve | [Analysis_Curve.cs](AnyCAD.Basic/Analysis/Analysis_Curve.cs) | <img src="showcase/analysis.curve.png" width="240" alt="Curve"/> |
| Distance | [Analysis_Distance.cs](AnyCAD.Basic/Analysis/Analysis_Distance.cs) | <img src="showcase/analysis.distance.png" width="240" alt="Distance"/> |
| PointPlane | [Analysis_PointPlane.cs](AnyCAD.Basic/Analysis/Analysis_PointPlane.cs) | <img src="showcase/analysis.pointplane.png" width="240" alt="PointPlane"/> |
| PosAxis | [Analysis_PosAxis.cs](AnyCAD.Basic/Analysis/Analysis_PosAxis.cs) | <img src="showcase/analysis.posaxis.png" width="240" alt="PosAxis"/> |
| Shape | [Analysis_Shape.cs](AnyCAD.Basic/Analysis/Analysis_Shape.cs) | <img src="showcase/analysis.shape.png" width="240" alt="Shape"/> |
| Solid | [Analysis_Solid.cs](AnyCAD.Basic/Analysis/Analysis_Solid.cs) | <img src="showcase/analysis.solid.png" width="240" alt="Solid"/> |
| SplitCurve | [Analysis_SplitCurve.cs](AnyCAD.Basic/Analysis/Analysis_SplitCurve.cs) | <img src="showcase/analysis.splitcurve.png" width="240" alt="SplitCurve"/> |
| Surface | [Analysis_Surface.cs](AnyCAD.Basic/Analysis/Analysis_Surface.cs) | <img src="showcase/analysis.surface.png" width="240" alt="Surface"/> |
| TangentLine | [Analysis_TangentLine.cs](AnyCAD.Basic/Analysis/Analysis_TangentLine.cs) | <img src="showcase/analysis.tangentline.png" width="240" alt="TangentLine"/> |
| Wire | [Analysis_Wire.cs](AnyCAD.Basic/Analysis/Analysis_Wire.cs) | <img src="showcase/analysis.wire.png" width="240" alt="Wire"/> |

### 2.2 运动仿真与高级功能（AnyCAD.Advanced，共 22 个）

**交互（5）**

| 示例 | 源码 | 效果图 |
| --- | --- | --- |
| CurveOnSurface | [Interaction_CurveOnSurface.cs](AnyCAD.Advanced/Interaction/Interaction_CurveOnSurface.cs) | <img src="showcase/interaction.curveonsurface.png" width="240" alt="CurveOnSurface"/> |
| DrawLine | [Interaction_DrawLine.cs](AnyCAD.Advanced/Interaction/Interaction_DrawLine.cs) | <img src="showcase/interaction.drawline.png" width="240" alt="DrawLine"/> |
| Measure | [Interaction_Measure.cs](AnyCAD.Advanced/Interaction/Interaction_Measure.cs) | <img src="showcase/interaction.measure.png" width="240" alt="Measure"/> |
| MoveAndRotate | [Interaction_MoveAndRotate.cs](AnyCAD.Advanced/Interaction/Interaction_MoveAndRotate.cs) | <img src="showcase/interaction.moveandrotate.png" width="240" alt="MoveAndRotate"/> |
| PointOnSurface | [Interaction_PointOnSurface.cs](AnyCAD.Advanced/Interaction/Interaction_PointOnSurface.cs) | <img src="showcase/interaction.pointonsurface.png" width="240" alt="PointOnSurface"/> |

**Interop（1）**

| 示例 | 源码 | 效果图 |
| --- | --- | --- |
| ImportDXF | [Interop_ImportDXF.cs](AnyCAD.Advanced/Interop/Interop_ImportDXF.cs) | <img src="showcase/interop.importdxf.png" width="240" alt="ImportDXF"/> |

**显示（8）**

| 示例 | 源码 | 效果图 |
| --- | --- | --- |
| CAE | [Graphics_CAE.cs](AnyCAD.Advanced/Graphics/Graphics_CAE.cs) | <img src="showcase/graphics.cae.png" width="240" alt="CAE"/> |
| CAELine | [Graphics_CAELine.cs](AnyCAD.Advanced/Graphics/Graphics_CAELine.cs) | <img src="showcase/graphics.caeline.png" width="240" alt="CAELine"/> |
| CutStl | [Graphics_CutStl.cs](AnyCAD.Advanced/Graphics/Graphics_CutStl.cs) | <img src="showcase/graphics.cutstl.png" width="240" alt="CutStl"/> |
| ForceDiagram | [Graphics_ForceDiagram.cs](AnyCAD.Advanced/Graphics/Graphics_ForceDiagram.cs) | <img src="showcase/graphics.forcediagram.png" width="240" alt="ForceDiagram"/> |
| Gravity | [Graphics_Gravity.cs](AnyCAD.Advanced/Graphics/Graphics_Gravity.cs) | <img src="showcase/graphics.gravity.png" width="240" alt="Gravity"/> |
| Matlab | [Graphics_Matlab.cs](AnyCAD.Advanced/Graphics/Graphics_Matlab.cs) | <img src="showcase/graphics.matlab.png" width="240" alt="Matlab"/> |
| PointCloud | [Graphics_PointCloud.cs](AnyCAD.Advanced/Graphics/Graphics_PointCloud.cs) | <img src="showcase/graphics.pointcloud.png" width="240" alt="PointCloud"/> |
| PointCloudWithNormal | [Graphics_PointCloud_Normals.cs](AnyCAD.Advanced/Graphics/Graphics_PointCloud_Normals.cs) | <img src="showcase/graphics.pointcloudwithnormal.png" width="240" alt="PointCloudWithNormal"/> |

**仿真（7）**

| 示例 | 源码 | 效果图 |
| --- | --- | --- |
| Additive | [Simulation_Additive.cs](AnyCAD.Advanced/Simulation/Simulation_Additive.cs) | <img src="showcase/simulation.additive.png" width="240" alt="Additive"/> |
| Collision | [Simulation_Collision.cs](AnyCAD.Advanced/Simulation/Simulation_Collision.cs) | <img src="showcase/simulation.collision.png" width="240" alt="Collision"/> |
| DynamicRay | [Simulation_DynamicRay.cs](AnyCAD.Advanced/Simulation/Simulation_DynamicRay.cs) | <img src="showcase/simulation.dynamicray.png" width="240" alt="DynamicRay"/> |
| Flying | [Simulation_Flying.cs](AnyCAD.Advanced/Simulation/Simulation_Flying.cs) | <img src="showcase/simulation.flying.png" width="240" alt="Flying"/> |
| Mill | [Simulation_Mill.cs](AnyCAD.Advanced/Simulation/Simulation_Mill.cs) | <img src="showcase/simulation.mill.png" width="240" alt="Mill"/> |
| MotionTrail | [Simulation_MotionTrail.cs](AnyCAD.Advanced/Simulation/Simulation_MotionTrail.cs) | <img src="showcase/simulation.motiontrail.png" width="240" alt="MotionTrail"/> |
| RayHit | [Simulation_RayHit.cs](AnyCAD.Advanced/Simulation/Simulation_RayHit.cs) | <img src="showcase/simulation.rayhit.png" width="240" alt="RayHit"/> |

**分析（1）**

| 示例 | 源码 | 效果图 |
| --- | --- | --- |
| Points | [Analysis_Points.cs](AnyCAD.Advanced/Analysis/Analysis_Points.cs) | <img src="showcase/analysis.points.png" width="240" alt="Points"/> |

### 2.3 高级造型（AnyCAD.QuickSolid，共 15 个）

**造型（13）**

| 示例 | 源码 | 效果图 |
| --- | --- | --- |
| Bending | [Geometry_Bending.cs](AnyCAD.QuickSolid/Geometry/Geometry_Bending.cs) | <img src="showcase/geometry.bending.png" width="240" alt="Bending"/> |
| Hatcher | [Geometry_Hatcher.cs](AnyCAD.QuickSolid/Geometry/Geometry_Hatcher.cs) | <img src="showcase/geometry.hatcher.png" width="240" alt="Hatcher"/> |
| HLR | [Geometry_HLR.cs](AnyCAD.QuickSolid/Geometry/Geometry_HLR.cs) | <img src="showcase/geometry.hlr.png" width="240" alt="HLR"/> |
| Mirror | [Geometry_Mirror.cs](AnyCAD.QuickSolid/Geometry/Geometry_Mirror.cs) | <img src="showcase/geometry.mirror.png" width="240" alt="Mirror"/> |
| ProjectionTestCase | [Geometry_Projection.cs](AnyCAD.QuickSolid/Geometry/Geometry_Projection.cs) | <img src="showcase/geometry.projectiontestcase.png" width="240" alt="ProjectionTestCase"/> |
| ShipHullFromGrid | [Geometry_ShipHullFromGrid.cs](AnyCAD.QuickSolid/Geometry/Geometry_ShipHullFromGrid.cs) | <img src="showcase/geometry.shiphullfromgrid.png" width="240" alt="ShipHullFromGrid"/> |
| ShipShell | [Geometry_ShipShell.cs](AnyCAD.QuickSolid/Geometry/Geometry_ShipShell.cs) | <img src="showcase/geometry.shipshell.png" width="240" alt="ShipShell"/> |
| SweepByPath | [Geometry_SweepByPath.cs](AnyCAD.QuickSolid/Geometry/Geometry_SweepByPath.cs) | <img src="showcase/geometry.sweepbypath.png" width="240" alt="SweepByPath"/> |
| TextOutline | [Geometry_TextOutline.cs](AnyCAD.QuickSolid/Geometry/Geometry_TextOutline.cs) | <img src="showcase/geometry.textoutline.png" width="240" alt="TextOutline"/> |
| Tube | [Geometry_Tube.cs](AnyCAD.QuickSolid/Geometry/Geometry_Tube.cs) | <img src="showcase/geometry.tube.png" width="240" alt="Tube"/> |
| Unify | [Geometry_Unify.cs](AnyCAD.QuickSolid/Geometry/Geometry_Unify.cs) | <img src="showcase/geometry.unify.png" width="240" alt="Unify"/> |
| Unwrap | [Geometry_Unwrap.cs](AnyCAD.QuickSolid/Geometry/Geometry_Unwrap.cs) | <img src="showcase/geometry.unwrap.png" width="240" alt="Unwrap"/> |
| VesselHead | [Geometry_VesselHead.cs](AnyCAD.QuickSolid/Geometry/Geometry_VesselHead.cs) | <img src="showcase/geometry.vesselhead.png" width="240" alt="VesselHead"/> |

**分析（2）**

| 示例 | 源码 | 效果图 |
| --- | --- | --- |
| Curvature | [Analysis_Curvature.cs](AnyCAD.QuickSolid/Analysis/Analysis_Curvature.cs) | <img src="showcase/analysis.curvature.png" width="240" alt="Curvature"/> |
| Holes | [Analysis_Holes.cs](AnyCAD.QuickSolid/Analysis/Analysis_Holes.cs) | <img src="showcase/analysis.holes.png" width="240" alt="Holes"/> |

## 3 Documentation

- [入门说明](http://www.anycad.cn/guide/)
- [API手册](http://www.anycad.cn/api/classes.html)

## 4 更多示例
### 入门示例：
https://gitee.com/anycad/rapid.net.starter
### 高级示例：
https://gitee.com/anycad/RapidCAX
### 机器人
[AnyRobot.NET](https://gitee.com/anycad/anyrobot)

![pCY0WZT.md.png](https://s1.ax1x.com/2023/06/23/pCY0WZT.md.png)
### 二维绘图
[AnyDraw.NET](https://gitee.com/anycad/anydraw)

![](https://z1.ax1x.com/2023/10/17/piPQCOP.png)
## 5 关于
http://www.anycad.cn

![Weixin](weixin.jpg)