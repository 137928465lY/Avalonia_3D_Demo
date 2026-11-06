# AnyCAD Showcase 自动截图与 README 生成工具

自动遍历 AnyCAD 示例工程（AnyCAD.Basic / AnyCAD.Advanced / AnyCAD.QuickSolid）的全部 TestCase，
逐个运行并截图（1600x1040 PNG）到仓库根目录 `showcase/`，并据此生成 README 的完整示例清单。

## 环境要求

- 已用主解决方案构建出 `bin\Release\net8.0-windows\`（含 AnyCAD.Foundation.NET.dll / AnyCAD.WPF.NET.dll / AnyCAD.Basic.dll / AnyCAD.Advanced.dll / AnyCAD.QuickSolid.dll）
- .NET 8 SDK、Windows 桌面（WPF）

## 用法

```powershell
# 1. 构建运行器（输出到 bin\Release\net8.0-windows\AnyCADShowcaseRunner.dll）
dotnet build .\ShowcaseRunner\ShowcaseRunner.csproj -c Release

# 2. 全量截图 + 崩溃自愈（推荐）：守护循环自动续跑、跳过并单试崩溃用例
powershell -NoProfile -ExecutionPolicy Bypass -File .\run_capture_loop.ps1
#    或直接单次运行：
#    cd ..\bin\Release\net8.0-windows
#    dotnet AnyCADShowcaseRunner.dll            # 全量
#    dotnet AnyCADShowcaseRunner.dll --resume   # 从上次完成处续跑
#    dotnet AnyCADShowcaseRunner.dll --only Geometry/Boolean   # 单个重试
#    dotnet AnyCADShowcaseRunner.dll --viewprobe Geometry/Boolean  # 视角调试（输出各候选视角对比图）

# 3. 生成 README 第 2 节（读取 case_map.txt + showcase/，原位替换）
python .\gen_readme.py

# 4. 校验 README 链接与图片引用
python .\verify_readme.py
```

## 文件说明

| 文件 | 作用 |
| --- | --- |
| `ShowcaseRunner/` | 截图运行器（WPF 控制台）。支持 `--max N` / `--only name` / `--resume` / `--skip name` / `--viewprobe name` |
| `run_capture_loop.ps1` | 守护循环：崩溃用例自动加入 `capture_skip.txt` 并重启续跑，全部完成后逐个 `--only` 单试 |
| `gen_readme.py` | 生成 README「2 程序示例」章节（相对源码链接 + 240px 效果图），自动标注缺失截图 |
| `verify_readme.py` | 校验 README 中所有 `.cs` 链接与 `.png` 引用可访问 |
| `capture_skip.txt` | 已确认崩溃/异常、无需重试的用例清单 |
| `case_map.txt`（仓库根） | 130 个用例的「模块/分组/名称/完整类型/相对源码路径」映射表 |
| `showcase-capture.log`（仓库根） | 截图运行日志（含 [STATE]/[OK]/[FAIL]/DONE） |

## 智能视角（smart view）

默认相机为俯视（Z 轴向），三维体积模型俯视效果差。运行器对每个用例读取场景包围盒
（`SceneManager.GetSceneBBox`），按扁平比 `dz / max(dx, dy)` 判断：

- 扁平比 ≤ 0.15（近似 2D，如二维草图/平面图形）→ 保持默认俯视；
- 否则（三维体积模型）→ 切换 `EnumStandardView.VertexNxPyPz`（等轴测，注意：`VertexPxPyPz` 与 `DefaultView` 同值，实际仍是俯视，不可用作等轴测）并 `ZoomAll`。

## 注意事项

- 运行器必须位于 `bin\Release\net8.0-windows\`（TestCase.GetResourcePath 按 `AppBase\..\..\..\data` 解析资源）。
- 本机 AnyCAD.Rapid.NET 构建的 `ScreenShot.SaveFile` 返回 false（缺陷），运行器改用 `CreateMemoryImage()` 取 PNG 字节直写。
- 运行器在截图前会额外等待 2 秒（`Pump(2000)`），确保纹理、大场景等完整渲染后再截屏。
- 部分用例存在原生库崩溃（如 Geometry/SweepCable、Geometry/SweepGear、Geometry/CutByWires）或托管异常
  （如 Analysis/MiddlePath、Geometry/BiArcs、Geometry/Wrap、Geometry/Unfold 等），守护循环会自动跳过并在 README 中标注。
- `run_capture_loop.ps1` 必须保存为 **UTF-8 with BOM**（PowerShell 5.1 按 ANSI 读取无 BOM 脚本会导致中文路径乱码）。
