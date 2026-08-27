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
  `ScriptedImporter`, and other editor APIs belong here.
- The core package has no hard dependency on URP. Render-pipeline differences
  belong behind a material-conversion interface introduced with the material
  conversion stage.

## Current 0.1.0 scope

The current package implements bounded PMX 2.0/2.1 parsing, import validation, Mesh and
approximate Material conversion, Generic skeleton and skinning, versioned metadata,
BlendShapes, deterministic approximate Morph/grant/IK runtime controllers, and optional
experimental PhysX construction. The support matrix and compatibility documents define
which source semantics are converted, approximated, preserved only, or unsupported.
