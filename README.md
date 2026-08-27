# UnityPMXEditor

[中文](#中文说明) | [English](#english)

## 中文说明

UnityPMXEditor 是一个开源 Unity Package Manager（UPM）插件，用于读取 PMX 2.0/2.1
模型并将其导入为 Unity 资产。插件面向 Unity 2022.3 LTS，UPM 包名为
`com.hanagumori.unity-pmx-editor`，公开 C# 命名空间为 `Hanagumori.UnityPmx`。

### 安装方法

#### 本地开发安装

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

未固定版本的 URL 会跟随默认分支，不具备可复现性。在 release commit 或 tag
真正推送到远端之前，不应假定对应 Git URL 已经可安装。完整升级和删除流程见
[安装与升级说明](Documentation~/Installation.md)。

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

#### 主要导入设置

- **Scale**：统一控制顶点、骨骼、Morph 位移和物理距离的缩放。
- **Advanced Deform Mode**：SDEF/QDEF 可选择 `Strict`、`Approximate` 或
  `PreserveOnly`。当前没有专用 SDEF/QDEF 后端。
- **Runtime Capability**：默认使用 `StandardApproximate`。`MmdCompatible` 仍受
  已记录的 SDEF/QDEF、IK、grant、材质和物理差异限制，不代表逐帧精确兼容 MMD。
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

| PMX 功能 | 0.1.0 状态 | 说明 |
| --- | --- | --- |
| PMX 2.0/2.1、UTF-16LE/UTF-8、动态索引 | 已解析 | 严格有界读取；仅支持小端 |
| 顶点、法线、基础 UV、三角形 | 已转换 | 坐标手性、绕序、法线和缩放集中处理 |
| 材质 surface 范围与 submesh | 已转换 | 必须精确覆盖完整 Surface 索引 |
| BDEF1/BDEF2/BDEF4 | 已转换 | 权重归一化并提供确定性回退 |
| SDEF/QDEF | 已解析；近似或仅保留 | 没有专用 SDEF/QDEF 变形后端 |
| 骨骼、层级、bindpose | 已转换 | 只生成 Generic 骨骼，不伪造 Humanoid Avatar |
| Vertex Morph | 已转换 | 稳定、完整长度的 Unity BlendShapes |
| Group/Flip/Bone/基础 UV/Material Morph | 近似支持 | 使用确定性运行时控制器，语义差异已记录 |
| Additional UV 1-4、Impulse Morph | 仅保留 | 0.1.0 没有运行时效果 |
| Display Frame | 仅保留 | 可通过模型元数据和 Inspector 查看 |
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
- `Hanagumori.UnityPmx.Editor` 仅在 Editor 平台编译，负责 `AssetDatabase` 和
  `ScriptedImporter`。
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

### 开发与测试

在测试工程的 `testables` 中加入包名后，可通过 Unity Test Framework 运行 EditMode
和 PlayMode 测试。测试 PMX 由代码生成或由项目自行制作，不提交第三方 MMD 模型。
当前发布审计范围见[发布检查清单](Documentation~/ReleaseChecklist.md)。

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

#### Local development installation

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

An unpinned URL follows the default branch and is not reproducible. Do not assume that a
Git URL works before its release commit or tag has actually been pushed. See
[Installation and Upgrade](Documentation~/Installation.md) for upgrade and removal
details.

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

#### Main import settings

- **Scale** controls vertex, bone, Morph-translation, and physics-distance scaling in one
  place.
- **Advanced Deform Mode** selects `Strict`, `Approximate`, or `PreserveOnly` for
  SDEF/QDEF. There is no dedicated SDEF/QDEF backend in 0.1.0.
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

| PMX feature | 0.1.0 status | Notes |
| --- | --- | --- |
| PMX 2.0/2.1, UTF-16LE/UTF-8, dynamic indices | Parsed | Strict bounded reader; little-endian only |
| Vertex, normal, base UV, triangle geometry | Converted | Centralized handedness, winding, normal, and scale conversion |
| Material surface ranges and submeshes | Converted | Exact complete Surface-index coverage is required |
| BDEF1/BDEF2/BDEF4 | Converted | Normalized weights with deterministic fallbacks |
| SDEF/QDEF | Parsed; approximate or preserved only | No dedicated SDEF/QDEF deformation backend |
| Skeleton, hierarchy, bindposes | Converted | Generic only; no fabricated Humanoid Avatar |
| Vertex Morph | Converted | Stable full-length Unity BlendShapes |
| Group/Flip/Bone/base UV/Material Morph | Approximate | Deterministic runtime controllers with documented differences |
| Additional UV 1-4 and Impulse Morph | Preserved only | No runtime effect in 0.1.0 |
| Display frames | Preserved only | Available through model metadata and Inspector |
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
- `Hanagumori.UnityPmx.Editor` compiles only for Editor and owns `AssetDatabase` and
  `ScriptedImporter` usage.
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

### Development and Tests

Add the package name to the consuming project's `testables` list, then run EditMode and
PlayMode suites through Unity Test Framework. Test PMX files are generated from code or
self-authored; no third-party MMD model is committed. See the current
[Release Checklist](Documentation~/ReleaseChecklist.md).

### License

UnityPMXEditor source code and the self-authored minimal fixture use the
[MIT License](LICENSE). See [Third Party Notices](Third%20Party%20Notices.md).

> The plugin's MIT license does not automatically cover user-imported PMX models,
> textures, motions, character rights, or other data. Importing a model does not
> relicense it under MIT. Check its redistribution, commercial-use, modification,
> attribution, and platform restrictions separately.
