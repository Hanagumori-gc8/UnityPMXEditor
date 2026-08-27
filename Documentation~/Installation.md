# Installation and Upgrade

## Local file dependency

For development, keep the Unity project and package checkout separate. Use **Window >
Package Manager > Add package from disk** and select the package checkout's
`package.json`, or add a project-relative entry to `Packages/manifest.json`:

```json
"com.hanagumori.unity-pmx-editor": "file:../UnityPMXEditor"
```

Absolute file dependencies are machine-specific and should not be committed to a shared
Unity project. Do not place the package implementation under an unrelated parent Git
repository and do not copy it into `Assets`.

## Git URL dependency

Git installation requires a committed revision whose repository root contains
`package.json`. Pin releases or immutable commits:

```text
https://github.com/Hanagumori-gc8/UnityPMXEditor.git#<release-tag>
https://github.com/Hanagumori-gc8/UnityPMXEditor.git#<full-commit-sha>
```

An unpinned URL follows the repository's default branch and is not reproducible. A tag
URL cannot work until that tag exists remotely; this repository must not advertise one
before the release is actually created.

## Upgrade procedure

1. Back up or commit the consuming Unity project.
2. Read `CHANGELOG.md` and note schema or behavior changes.
3. Change the file path, commit SHA, or release tag in `Packages/manifest.json`.
4. Let Unity resolve packages and finish script compilation.
5. Reimport representative PMX assets and compare diagnostics, sub-assets, skeleton,
   materials, Morph ordering, and optional physics settings.
6. Import the package sample again with **Override previous imports** when validating a
   new package version.
7. Delete the sample from `Assets/Samples` after validation if it is not needed.

`PmxModelAsset.SchemaVersion` records metadata schema changes. Unity's successful package
resolution or C# compilation alone does not prove model import or runtime behavior.

## Removing the package

Remove the dependency through Package Manager or delete its manifest entry. Imported
sample copies under `Assets/Samples` are project assets and are not automatically deleted
with the package; remove them separately. PMX assets that depend on the scripted importer
will no longer import until the package is restored.
