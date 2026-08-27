# Third Party Notices

UnityPMXEditor does not bundle a third-party PMX parser, MMD model, character asset,
texture, shader, or physics backend.

The PMX reader was independently implemented from the publicly documented binary field
layout. Development consulted Felix Jones' community English PMX 2.1 format description:

- <https://gist.github.com/felixjones/f8a06bd48f9da9a4539f>

That document is a format reference, not redistributed source code. See
`Documentation~/PmxFormat.md` for the implemented validation boundary and fixture
provenance.

The package uses Unity APIs and its tests use Unity Test Framework/NUnit through the host
Unity installation. Version 0.2.0 also declares `com.unity.formats.fbx` 4.2.1 as an Editor
dependency. Unity Package Manager resolves that package and its `com.autodesk.fbx` 4.2.1
dependency; their source and binaries are not relicensed under this repository's MIT
license.

- Unity FBX Exporter is copyright Unity Technologies ApS and is distributed under the
  Unity Companion License for Unity-dependent projects.
- The Autodesk FBX SDK component is copyright Autodesk, Inc. and is governed by the FBX
  SDK License and Service Agreement: <https://unity3d.com/legal/autodesk-fbx>.

This software uses Autodesk FBX SDK code supplied through Unity's official package.
Autodesk FBX code is provided as-is; Autodesk disclaims warranties and liability as
described in the SDK agreement. Installed copies of both packages contain their complete
license and third-party-notice files.

The file `Samples~/MinimalPmxFixture/MinimalFixture.pmx` is authored by this project from
the PMX field layout, contains only one synthetic triangle/material/bone, and is released
under the repository MIT license. It is not derived from a character or MMD model.

Any PMX model imported by a user remains under its own model, texture, character, and
distribution licenses. The UnityPMXEditor MIT license does not apply to user models merely
because the plugin reads or converts them.
