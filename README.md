# UnityPMXEditor

[中文](#中文说明) | [English](#english)

## 中文说明

UnityPMXEditor 是一个开源 Unity Package Manager（UPM）插件，用于读取 PMX 2.0/2.1
模型并将其导入为 Unity 资产。插件面向 Unity 2022.3 LTS，UPM 包名为
`com.hanagumori.unity-pmx-editor`，公开 C# 命名空间为 `Hanagumori.UnityPmx`。

### 安装方法

#### 从磁盘安装

克隆或准备独立的 UnityPMXEditor 仓库后，在 Unity 中打开 **Window > Package
Manager**，选择 **Add package from disk**，然后选择仓库根目录的 `package.json`。

也可以在 Unity 工程的 `Packages/manifest.json` 中添加相对路径依赖：

```json
{
  "dependencies": {
    "com.hanagumori.unity-pmx-editor": "file:../UnityPMXEditor"
  }
}
```

建议让 Unity 工程与插件仓库保持为两个独立目录。不要把插件源码复制到 `Assets`
目录，也不要把机器相关的绝对路径提交到共享工程。

#### Git URL 安装

当 GitHub 仓库中的目标 commit 或 tag 已经包含根目录 `package.json` 时，可在
Package Manager 中选择 **Add package from git URL**，并使用固定的 tag 或 commit：

```text
https://github.com/Hanagumori-gc8/UnityPMXEditor.git#<tag-or-commit>
```

未固定版本的 URL 会跟随默认分支，不具备可复现性。发布环境建议固定 release tag
或完整 commit SHA。完整升级和删除流程见[安装与升级说明](Documentation~/Installation.md)。

### 操作指南

#### 导入 PMX 模型

1. 将 `.pmx` 文件及其纹理放入 Unity 工程的 `Assets` 目录。纹理路径应保持相对
   于 PMX 文件的位置关系。
2. 等待 Unity 导入完成，然后在 Project 窗口中选择该 `.pmx` 资产。
3. 在 Inspector 中设置导入比例、SDEF/QDEF 策略、运行时能力路径和可选物理模式。
4. 将导入得到的根 `GameObject` 拖入场景，或通过代码实例化。
5. 展开 PMX 资产并选择 `PmxModelAsset` sub-asset，查看原始 PMX 元数据、功能状态
   和导入诊断。

导入器会创建根 `GameObject`、Mesh、Material sub-assets、版本化 `PmxModelAsset`、
Generic 骨骼、蒙皮、BlendShapes、Morph/骨骼运行时控制器，以及可选的实验性 PhysX
组件。sub-asset ID 使用结构化索引，不依赖可能重复或变化的日文名称。

#### 查看模型部件和骨骼

Project 窗口中的 `.pmx` 是 Unity 管理的导入源资产，其内部节点会被 Unity 标记为
不可直接编辑。选择其 `PmxModelAsset` sub-asset 后点击
**Instantiate Editable Scene Model**，或者把 PMX 主资产拖入场景，获得可见、可选、
可编辑的 Hierarchy 实例：

```text
模型根节点
|-- PMX Mesh             SkinnedMeshRenderer
`-- PMX Skeleton
    `-- PMX Bone 000000 - 来源骨骼名
```

展开 `.pmx` 资产并选择 `PmxModelAsset`，或选择场景实例上的
`PmxRuntimeController`。Inspector 的 **Model Parts (Material Submeshes)** 会按稳定材质
索引列出部件名称、三角形数量和 Unity Material；**Bones** 会显示骨骼原始名称、父级
索引和 deformation layer。场景实例中可用 **Select** 定位骨骼 Transform。选中 PMX
根对象、`PMX Mesh` 或任一骨骼时，Scene 视图会持续绘制骨骼；点击关节点 Gizmo 会
直接选中对应骨骼，可使用 Move/Rotate 工具摆姿势。

Play Mode 下默认的 **Runtime Evaluation** 会每帧执行 Morph、grant 和 IK，因此会覆盖
手动骨骼姿态。需要手动摆姿势时先关闭该开关；重新开启时，当前手动姿态会成为新的
运行时基线。

导入设置提供两种可切换的 **Part Hierarchy Mode**：

```text
ProxyNodes（默认）              SeparateRenderers
模型根节点                      模型根节点
`-- PMX Model Parts             `-- PMX Model Parts
    |-- PMX Part 000000             |-- PMX Part 000000  SkinnedMeshRenderer
    |-- PMX Part 000001             |-- PMX Part 000001  SkinnedMeshRenderer
    `-- ...                         `-- ...
`-- PMX Mesh  SkinnedMeshRenderer
```

`ProxyNodes` 为每个材质 submesh 创建可选的代理节点，但几何仍由一个共享
`SkinnedMeshRenderer` 绘制；移动代理节点不会独立移动几何。`SeparateRenderers`
为每个材质分区创建真正独立的 `SkinnedMeshRenderer` 和 Mesh sub-asset，可在
Hierarchy、Scene Gizmo 和 Inspector 中单独选中、移动、隐藏或赋材质，代价是每个
部件增加一个渲染对象。两种模式都使用稳定的 `PMX Part {index:D6}` 标识，原始
日文/英文名称仅作为显示后缀。根 Inspector 的 **Show Only** / **Show All** 和
场景部件 Inspector 在两种模式下均可用。

#### 导出 FBX 或 OBJ

选择导入的 PMX 主资产、`PmxModelAsset` sub-asset 或场景中的 PMX 实例，然后使用
Inspector 的 **Export FBX...** / **Export OBJ...**，也可以使用：

- **Assets > UnityPMXEditor > Export Selected as FBX...**
- **Assets > UnityPMXEditor > Export Selected as OBJ...**

FBX 通过 Unity 2022.3 对应的官方 `com.unity.formats.fbx` 4.2.1 导出，保留 Generic
骨骼层级、蒙皮、材质以及官方 Exporter 能表达的数据。OBJ 会烘焙当前静态姿态，并
导出顶点、法线、UV、材质分组和 `.mtl`；OBJ 不包含骨骼、蒙皮、BlendShape、动画、
物理或 PMX 元数据。两种格式都不能把近似或仅保留的 MMD 语义变成精确支持。详见
[部件、骨骼与导出说明](Documentation~/Exporting.md)。

#### 主要导入设置

- **Scale**：统一控制顶点、骨骼、Morph 位移和物理距离的缩放。
- **Advanced Deform Mode**：SDEF/QDEF 可选择 `Strict`、`Approximate` 或
  `PreserveOnly`。当前没有专用 SDEF/QDEF 后端。
- **Runtime Capability**：默认使用 `StandardApproximate`。`MmdCompatible` 仍受
  已记录的 SDEF/QDEF、IK、grant、材质和物理差异限制，不代表逐帧精确兼容 MMD。
- **Part Hierarchy Mode**：`ProxyNodes` 使用一个共享蒙皮渲染器和可选代理节点；
  `SeparateRenderers` 为每个材质分区创建独立的 `SkinnedMeshRenderer`，可分别
  移动和隐藏，但会增加渲染对象数量。
- **Physics Mode**：默认 `None`，不创建物理组件；`Experimental` 会把部分 PMX
  Bullet 语义近似转换为 Unity PhysX。

#### 纹理要求

纹理表路径必须能够解析到 Unity 工程内的 `Assets` 或 `Packages`。插件拒绝目录
穿越、绝对路径、URI、NUL 字符和不可移植路径。缺失纹理会产生明确 Warning，材质
仍会以无该纹理的近似形式导入。

#### Package Manager Sample

在 Package Manager 的 UnityPMXEditor 页面中导入 **Minimal PMX Fixture** Sample，
即可获得一个自制的最小 PMX 2.0 模型。它仅包含一个三角形、一个材质和一个骨骼，
不包含第三方角色、模型或纹理，可用于验证安装、导入和重导入。

### 环境要求与验证范围

- 最低目标版本：Unity 2022.3 LTS
- 已验证 Editor：Unity 2022.3.60f1
- 已验证渲染管线：URP 14.0.12
- 核心包不依赖 URP，也不引用 URP API
- FBX 导出依赖 Unity 官方 `com.unity.formats.fbx` 4.2.1，仅由 Editor 程序集引用
- Built-in Render Pipeline：未验证
- HDRP：未验证

默认材质转换器会请求当前渲染管线的默认材质 Shader，并在必要时按名称回退到
内置 Shader。这个回退机制不等于 Built-in 或 HDRP 的材质外观已经验证。

### 支持状态

README 和 Inspector 使用以下互不混淆的状态：

- **已解析（Parsed）**：二进制数据已经严格验证，并进入 `PmxDocument` 和元数据。
- **已转换（Converted）**：已经生成 Unity 原生资产或预期的直接运行时效果。
- **近似支持（Approximate）**：存在可运行的 Unity 映射，但语义或数值不等同于 MMD。
- **仅保留（Preserved only）**：数据保存在 `PmxModelAsset`，但没有运行时效果。
- **未支持（Unsupported）**：没有对应后端，导入诊断会明确报告。

| PMX 功能 | 0.2.2 状态 | 说明 |
| --- | --- | --- |
| PMX 2.0/2.1、UTF-16LE/UTF-8、动态索引 | 已解析 | 严格有界读取；仅支持小端 |
| 顶点、法线、基础 UV、三角形 | 已转换 | 坐标手性、绕序、法线、缩放和 UV V 方向集中处理 |
| 材质 surface 范围与 submesh | 已转换 | 必须精确覆盖完整 Surface 索引 |
| BDEF1/BDEF2/BDEF4 | 已转换 | 权重归一化并提供确定性回退 |
| SDEF/QDEF | 已解析；近似或仅保留 | 没有专用 SDEF/QDEF 变形后端 |
| 骨骼、层级、bindpose | 已转换 | 只生成 Generic 骨骼，不伪造 Humanoid Avatar |
| Vertex Morph | 已转换 | 稳定、完整长度的 Unity BlendShapes |
| Group/Flip/Bone/基础 UV/Material Morph | 近似支持 | 使用确定性运行时控制器，语义差异已记录 |
| Additional UV 1-4、Impulse Morph | 仅保留 | 0.2.2 没有运行时效果 |
| Display Frame | 仅保留 | 可通过模型元数据和 Inspector 查看 |
| 材质部件与骨骼 Gizmo | 已转换 | Inspector 部件/骨骼列表；Scene 视图显示骨骼 Gizmo |
| Part Hierarchy Mode | 已转换 | `ProxyNodes` 或 `SeparateRenderers`，可在 PMX Import Settings 切换 |
| FBX 导出 | 已转换 | 使用 Unity 官方 FBX Exporter；保留 Generic 骨骼和蒙皮 |
| OBJ/MTL 导出 | 已转换 | 当前静态姿态及材质分组；不包含骨骼、Morph 或动画 |
| inherit/grant、deformation layer、IK | 近似支持 | 有界且确定性，但不保证与 MMD 数值一致 |
| MMD Toon、Sphere Map、描边 | 仅保留 | 默认材质只是明确标注的漫反射/高光近似 |
| Sphere/Box/Capsule 刚体 | 近似支持，可选 | 实验性 Bullet 到 PhysX 映射；默认关闭 |
| PMX Spring 6DOF type 0 | 近似支持，可选 | 映射到 `ConfigurableJoint`，limit/drive 存在差异 |
| 其他 PMX 2.1 Joint 类型 | 未支持 | 保留原始类型和完整元数据 |
| PMX 2.1 SoftBody | 未支持 | 完整保留元数据，但没有运行时后端 |

详细表格见[支持矩阵](Documentation~/SupportMatrix.md)。

### 架构边界

```text
PMX binary -> PmxDocument -> validation/normalization
           -> Unity conversion -> imported assets
```

- `Hanagumori.UnityPmx.Format` 是纯 C# 程序集，设置
  `noEngineReferences: true`，不引用 Unity API。
- `Hanagumori.UnityPmx.Runtime` 只引用 Format 和 Unity runtime API，不引用
  `UnityEditor`。
- `Hanagumori.UnityPmx.Editor` 仅在 Editor 平台编译，负责 `AssetDatabase`、
  `ScriptedImporter`、Inspector/Gizmo 和 FBX/OBJ 导出。
- 渲染管线差异通过 `IMaterialConverter` 隔离，核心包没有 URP 硬依赖。

### 重要限制

- 只支持小端 PMX 2.0/2.1，并执行可配置的文件、字符串、section、嵌套集合和总项目
  数量上限。截断、超限、循环、尾随字节和非法引用会给出 section 与 byte offset。
- 默认材质不能还原 MMD Toon、Sphere Map、描边和全部 Material Morph 语义。
- `MmdCompatible` 不会自动让 SDEF/QDEF、IK、grant、材质或物理变成精确实现；缺少
  必要后端时会拒绝或明确降级。
- 实验物理使用 Unity PhysX，不是 Bullet，不能声称与 MMD 逐帧一致。
- Built-in 和 HDRP 尚未验证，不能从“没有编译依赖”推导出视觉结果正确。

更多内容见[格式限制](Documentation~/PmxFormat.md)、
[导入流程](Documentation~/ImportPipeline.md)、
[运行时兼容性](Documentation~/RuntimeCompatibility.md)和
[物理兼容性](Documentation~/PhysicsCompatibility.md)。

### 测试与贡献

在测试工程的 `testables` 中加入包名后，可通过 Unity Test Framework 运行 EditMode
和 PlayMode 测试。测试 PMX 由代码生成或由项目自行制作，不提交第三方 MMD 模型。

### 许可证

UnityPMXEditor 源码和自制最小 fixture 使用 [MIT License](LICENSE)。
详见 [Third Party Notices](Third%20Party%20Notices.md)。

> 插件的 MIT 许可证不会自动覆盖用户导入的 PMX 模型、纹理、动作、角色权利或其他
> 数据。导入行为不会把模型重新许可为 MIT。使用模型前必须单独检查其再分发、商用、
> 修改、署名和平台限制。

---

## English

UnityPMXEditor is an open-source Unity Package Manager package that reads PMX 2.0/2.1
models and imports them as Unity assets. It targets Unity 2022.3 LTS, uses the package
name `com.hanagumori.unity-pmx-editor`, and exposes the C# namespace
`Hanagumori.UnityPmx`.

### Installation

#### Install from disk

Keep UnityPMXEditor in an independent repository. In Unity, open **Window > Package
Manager**, choose **Add package from disk**, and select the repository-root
`package.json`.

Alternatively, add a relative dependency to the Unity project's
`Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.hanagumori.unity-pmx-editor": "file:../UnityPMXEditor"
  }
}
```

Keep the Unity project and package checkout in separate directories. Do not copy the
package implementation into `Assets`, and do not commit machine-specific absolute paths.

#### Git URL installation

After the target GitHub commit or tag contains the repository-root `package.json`, choose
**Add package from git URL** and use a pinned tag or commit:

```text
https://github.com/Hanagumori-gc8/UnityPMXEditor.git#<tag-or-commit>
```

An unpinned URL follows the default branch and is not reproducible. Pin a release tag or
full commit SHA for production. See
[Installation and Upgrade](Documentation~/Installation.md) for upgrade and removal details.

### Usage Guide

#### Importing a PMX model

1. Place the `.pmx` file and its textures under the Unity project's `Assets` directory.
   Preserve texture paths relative to the PMX file.
2. Wait for Unity to finish importing, then select the `.pmx` asset in the Project window.
3. Configure import scale, SDEF/QDEF policy, runtime capability, and optional physics in
   the Inspector.
4. Drag the imported root `GameObject` into a scene or instantiate it from code.
5. Expand the PMX asset and inspect the `PmxModelAsset` sub-asset for original metadata,
   feature status, and import diagnostics.

The importer creates a root `GameObject`, Mesh, Material sub-assets, a versioned
`PmxModelAsset`, a Generic skeleton, skinning, BlendShapes, runtime Morph/bone
controllers, and optional experimental PhysX components. Structural indices provide
stable sub-asset IDs; Japanese display names never determine asset identity.

#### Inspecting model parts and bones

The `.pmx` in the Project window is a Unity-managed imported source asset. Unity marks
its internal objects as non-editable. Select its `PmxModelAsset` sub-asset and click
**Instantiate Editable Scene Model**, or drag the PMX main asset into a scene, to create
a visible and editable Hierarchy instance:

```text
Model root
|-- PMX Mesh             SkinnedMeshRenderer
`-- PMX Skeleton
    `-- PMX Bone 000000 - source bone name
```

Expand the `.pmx` asset and select `PmxModelAsset`, or select the
`PmxRuntimeController` on a scene instance. **Model Parts (Material Submeshes)** lists
each stable material index, part name, triangle count, and Unity Material. **Bones** lists
the original bone name, parent index, and deformation layer. On a scene instance,
**Select** focuses the corresponding bone Transform. Selecting the PMX root, `PMX Mesh`,
or any bone keeps the skeleton visible. Clicking a joint Gizmo selects that bone for the
Move and Rotate tools.

In Play Mode, **Runtime Evaluation** applies Morphs, grants, and IK every frame and
therefore overwrites manual bone poses. Disable it while posing manually. Re-enabling it
captures the current manual pose as the new runtime baseline.

The importer deliberately retains one `SkinnedMeshRenderer`; it does not split and
duplicate skinning and BlendShape state merely to expose parts. OBJ export emits one
stable `g` group per material submesh when individually selectable static parts are
needed.

#### Exporting FBX or OBJ

Select the imported PMX main asset, its `PmxModelAsset` sub-asset, or a PMX scene
instance. Use **Export FBX...** / **Export OBJ...** in the Inspector, or:

- **Assets > UnityPMXEditor > Export Selected as FBX...**
- **Assets > UnityPMXEditor > Export Selected as OBJ...**

FBX uses Unity's official `com.unity.formats.fbx` 4.2.1 package for Unity 2022.3 and
preserves the Generic bone hierarchy, skinning, materials, and data supported by the
official exporter. OBJ bakes the current static pose and writes vertices, normals, UVs,
material groups, and an `.mtl`; it contains no bones, skinning, BlendShapes, animation,
physics, or PMX metadata. Neither format turns approximate or preserved-only MMD
semantics into exact support. See
[Model Parts, Bone Gizmos, and Export](Documentation~/Exporting.md).

#### Main import settings

- **Scale** controls vertex, bone, Morph-translation, and physics-distance scaling in one
  place.
- **Advanced Deform Mode** selects `Strict`, `Approximate`, or `PreserveOnly` for
  SDEF/QDEF. There is no dedicated SDEF/QDEF backend in 0.2.2.
- **Runtime Capability** defaults to `StandardApproximate`. `MmdCompatible` remains
  subject to documented SDEF/QDEF, IK, grant, material, and physics differences and does
  not claim frame-identical MMD behavior.
- **Physics Mode** defaults to `None` and creates no physics components. `Experimental`
  approximately maps selected PMX Bullet concepts to Unity PhysX.

#### Texture requirements

Texture-table paths must resolve inside the Unity project's `Assets` or `Packages`.
Directory traversal, absolute paths, URIs, NUL characters, and non-portable paths are
rejected. Missing textures produce an explicit warning while the approximate material is
still imported without that texture.

#### Package Manager Sample

Import **Minimal PMX Fixture** from the UnityPMXEditor Package Manager page to get a
self-authored PMX 2.0 model containing one triangle, one material, and one bone. It
contains no third-party character, model, or texture and can validate installation,
import, and reimport behavior.

### Requirements and Validation Scope

- Minimum target: Unity 2022.3 LTS
- Validated Editor: Unity 2022.3.60f1
- Validated render pipeline: URP 14.0.12
- The core package has no URP dependency or URP API reference
- FBX export depends on Unity's official `com.unity.formats.fbx` 4.2.1 package in Editor only
- Built-in Render Pipeline: not validated
- HDRP: not validated

The default material converter requests the active render pipeline's default material
shader and can fall back to a built-in shader name. That fallback is not evidence that
Built-in or HDRP material appearance has been validated.

### Support Status

README and Inspector status terms are intentionally distinct:

- **Parsed**: binary data is strictly validated and represented in `PmxDocument` and
  metadata.
- **Converted**: a Unity-native asset or intended direct runtime effect is created.
- **Approximate**: an operational Unity mapping exists but differs semantically or
  numerically from MMD.
- **Preserved only**: source data remains in `PmxModelAsset` without a runtime effect.
- **Unsupported**: no backend exists and import diagnostics report that explicitly.

| PMX feature | 0.2.2 status | Notes |
| --- | --- | --- |
| PMX 2.0/2.1, UTF-16LE/UTF-8, dynamic indices | Parsed | Strict bounded reader; little-endian only |
| Vertex, normal, base UV, triangle geometry | Converted | Centralized handedness, winding, normal, scale, and UV V conversion |
| Material surface ranges and submeshes | Converted | Exact complete Surface-index coverage is required |
| BDEF1/BDEF2/BDEF4 | Converted | Normalized weights with deterministic fallbacks |
| SDEF/QDEF | Parsed; approximate or preserved only | No dedicated SDEF/QDEF deformation backend |
| Skeleton, hierarchy, bindposes | Converted | Generic only; no fabricated Humanoid Avatar |
| Vertex Morph | Converted | Stable full-length Unity BlendShapes |
| Group/Flip/Bone/base UV/Material Morph | Approximate | Deterministic runtime controllers with documented differences |
| Additional UV 1-4 and Impulse Morph | Preserved only | No runtime effect in 0.2.2 |
| Display frames | Preserved only | Available through model metadata and Inspector |
| Material parts and bone Gizmos | Converted | Inspector part/bone lists and Scene-view bone Gizmos |
| FBX export | Converted | Official Unity FBX Exporter; Generic hierarchy and skinning |
| OBJ/MTL export | Converted | Current static pose and material groups; no bones, Morphs, or animation |
| Inherit/grant, deformation layer, and IK | Approximate | Bounded and deterministic, not numerically identical to MMD |
| MMD toon, sphere map, and edge rendering | Preserved only | Default material is a labeled diffuse/specular approximation |
| Sphere/Box/Capsule rigid bodies | Approximate, opt-in | Experimental Bullet-to-PhysX mapping; disabled by default |
| PMX Spring 6DOF type 0 | Approximate, opt-in | Mapped to `ConfigurableJoint` with limit/drive differences |
| Other PMX 2.1 Joint types | Unsupported | Raw type and complete metadata are retained |
| PMX 2.1 SoftBody | Unsupported | Complete metadata retained; no runtime backend |

See the detailed [Support Matrix](Documentation~/SupportMatrix.md).

### Architecture Boundaries

```text
PMX binary -> PmxDocument -> validation/normalization
           -> Unity conversion -> imported assets
```

- `Hanagumori.UnityPmx.Format` is pure C# with `noEngineReferences: true` and no Unity
  API reference.
- `Hanagumori.UnityPmx.Runtime` references Format and Unity runtime APIs, never
  `UnityEditor`.
- `Hanagumori.UnityPmx.Editor` compiles only for Editor and owns `AssetDatabase`,
  `ScriptedImporter`, Inspector/Gizmo, and FBX/OBJ export usage.
- `IMaterialConverter` isolates render-pipeline policy; the core package has no hard URP
  dependency.

### Important Limitations

- Only little-endian PMX 2.0/2.1 is supported. Configurable file, string, section, nested
  collection, and total-item limits are enforced. Truncation, oversized data, cycles,
  trailing bytes, and invalid references produce section and byte-offset errors.
- Default materials do not reproduce MMD toon, sphere-map, edge, or every Material Morph
  semantic.
- `MmdCompatible` does not make SDEF/QDEF, IK, grants, materials, or physics exact. A
  missing required backend causes rejection or an explicit downgrade.
- Experimental physics uses Unity PhysX rather than Bullet and cannot claim frame-by-
  frame MMD equivalence.
- Built-in and HDRP remain unvalidated. Absence of a compile dependency does not prove
  correct visual output.

See [PMX Format](Documentation~/PmxFormat.md),
[Import Pipeline](Documentation~/ImportPipeline.md),
[Runtime Compatibility](Documentation~/RuntimeCompatibility.md), and
[Physics Compatibility](Documentation~/PhysicsCompatibility.md).

### Tests and Contributing

Add the package name to the consuming project's `testables` list, then run EditMode and
PlayMode suites through Unity Test Framework. Test PMX files are generated from code or
self-authored; no third-party MMD model is committed.

### License

UnityPMXEditor source code and the self-authored minimal fixture use the
[MIT License](LICENSE). See [Third Party Notices](Third%20Party%20Notices.md).

> The plugin's MIT license does not automatically cover user-imported PMX models,
> textures, motions, character rights, or other data. Importing a model does not
> relicense it under MIT. Check its redistribution, commercial-use, modification,
> attribution, and platform restrictions separately.
