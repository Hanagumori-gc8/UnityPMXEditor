# Architecture

UnityPMXEditor uses a staged, one-way import pipeline:

```text
PMX binary
    -> PmxDocument
    -> validation and normalization
    -> Unity conversion
    -> imported assets
```

## Assembly boundaries

- `Hanagumori.UnityPmx.Format` is pure C#. It must not reference `UnityEngine`
  or `UnityEditor`.
- `Hanagumori.UnityPmx.Runtime` may reference Unity runtime APIs, but must not
  reference `UnityEditor`.
- `Hanagumori.UnityPmx.Editor` is editor-only. `AssetDatabase`,
  `ScriptedImporter`, the official FBX Exporter integration, OBJ serialization, and other
  editor APIs belong here.
- The core package has no hard dependency on URP. Render-pipeline differences
  belong behind a material-conversion interface introduced with the material
  conversion stage.

## Directory layout

The repository root is the UPM package root. Unity package folders and their adjacent
`.meta` files remain at the root, while implementation files are grouped by ownership:

```text
UnityPMXEditor/
|-- Runtime/
|   |-- Format/       Pure C# PMX reader, DTOs, limits, and validation
|   |-- Conversion/   Coordinates, Mesh, skeleton, skinning, and BlendShapes
|   |-- Model/        Versioned model metadata and capability records
|   |-- Controllers/  Deterministic Morph and bone runtime evaluation
|   `-- Physics/      Optional experimental PhysX construction and control
|-- Editor/
|   |-- Importing/    ScriptedImporter, import settings, validation, and materials
|   |-- Exporting/    FBX/OBJ export services and Asset-menu commands
|   `-- Inspectors/   Model parts, bone lists, export controls, and Scene Gizmos
|-- Tests/
|   |-- Editor/       Importing, runtime-controller, and package integration tests
|   `-- Runtime/      Format and long-running physics tests
|-- Documentation~/
`-- Samples~/
```

The Runtime and Editor asmdefs stay at their assembly roots so all responsibility
subfolders remain in the same assemblies. Test asmdefs follow the same rule.

## Current 0.2.2 scope

The current package implements bounded PMX 2.0/2.1 parsing, import validation, Mesh and
approximate Material conversion, Generic skeleton and skinning, versioned metadata,
BlendShapes, deterministic approximate Morph/grant/IK runtime controllers, and optional
experimental PhysX construction. The support matrix and compatibility documents define
which source semantics are converted, approximated, preserved only, or unsupported.

Editor export is a downstream operation on imported Unity assets:

```text
imported PMX GameObject -> official Unity FBX Exporter -> FBX
                        -> bounded static-pose writer -> OBJ + MTL
```

Only `Hanagumori.UnityPmx.Editor` references `Unity.Formats.Fbx.Editor`. Format and Runtime
remain independent of the FBX SDK and UnityEditor.
