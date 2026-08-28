using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace Hanagumori.UnityPmx
{
    [ScriptedImporter(5, "pmx")]
    public sealed class PmxScriptedImporter : ScriptedImporter
    {
        private const string RootAssetId = "main/root";
        private const string MeshAssetId = "mesh/000000";
        private const string MetadataAssetId = "model/metadata";

        [SerializeField] private PmxImportSettings settings = new PmxImportSettings();

        public PmxImportSettings Settings => settings;

        public override void OnImportAsset(AssetImportContext context)
        {
            var ownedObjects = new List<UnityEngine.Object>();
            try
            {
                PmxDocument document;
                using (FileStream stream = File.OpenRead(context.assetPath))
                using (var reader = new PmxBinaryReader(stream))
                    document = reader.ReadDocument();

                var validator = new PmxValidator();
                validator.ValidateForImport(document, settings);
                var diagnostics = CreateInitialDiagnostics(document);

                string[] texturePaths = ResolveTexturePaths(context, validator, document);
                Texture2D[] textures = LoadTextureAssets(context, texturePaths, diagnostics);

                var coordinates = new PmxCoordinateConverter(settings.Scale);
                Mesh mesh = new MeshConverter().Convert(document, coordinates);
                ownedObjects.Add(mesh);
                BlendShapeConversionResult blendShapes = new BlendShapeConverter().Convert(
                    document, mesh, coordinates);

                var materialConverter = new DefaultMaterialConverter();
                Material[] materials = ConvertMaterials(document, textures, materialConverter, ownedObjects);

                var root = new GameObject(ChooseRootName(document));
                ownedObjects.Add(root);
                SkeletonConversionResult skeleton = new SkeletonConverter().Convert(
                    document, root.transform, coordinates);
                var skinningConverter = new SkinningConverter();
                SkinningConversionResult skinning = skinningConverter.Convert(
                    document, settings.AdvancedDeformMode);
                skinningConverter.ApplyToMesh(mesh, skinning, skeleton);
                string skinningStatusWarning = AppendSkinningDiagnostics(
                    diagnostics, skinning, settings.AdvancedDeformMode);
                string compatibilityWarning = AppendCapabilityDiagnostics(
                    diagnostics, settings, skinning);
                string physicsWarning = AppendPhysicsDiagnostics(diagnostics, document, settings);

                PartHierarchyResult partHierarchy = CreatePartHierarchy(context, root,
                    document, mesh, materials, skeleton, settings.PartHierarchyMode);

                var metadata = ScriptableObject.CreateInstance<PmxModelAsset>();
                ownedObjects.Add(metadata);
                metadata.name = "PMX Model Metadata";
                metadata.Initialize(document, mesh, materials, texturePaths, true,
                    skeleton, settings.AdvancedDeformMode, skinning, blendShapes,
                    settings.PhysicsMode, diagnostics.ToArray());

                var morphController = root.AddComponent<PmxMorphController>();
                morphController.Configure(metadata, partHierarchy.Renderers, settings.Scale);
                var boneController = root.AddComponent<PmxBoneController>();
                boneController.Configure(metadata, skeleton.Bones);
                var runtimeController = root.AddComponent<PmxRuntimeController>();
                runtimeController.Configure(metadata, morphController, boneController,
                    settings.RuntimeCapability, settings.MmdCompatibilityFallback);

                PmxPhysicsBuildResult physics = null;
                if (settings.PhysicsMode == PmxPhysicsImportMode.Experimental)
                {
                    physics = new PmxPhysicsBuilder().Build(metadata.RigidBodyMetadata,
                        metadata.JointMetadata, root.transform, skeleton.Bones, coordinates,
                        settings.PhysicsSettings);
                    for (int i = 0; i < physics.Materials.Length; i++)
                    {
                        ownedObjects.Add(physics.Materials[i]);
                        context.AddObjectToAsset($"physics/material/{i:D6}", physics.Materials[i]);
                    }
                }

                context.AddObjectToAsset(MeshAssetId, mesh);
                for (int i = 0; i < materials.Length; i++)
                    context.AddObjectToAsset($"material/{i:D6}", materials[i]);
                context.AddObjectToAsset(MetadataAssetId, metadata);
                context.AddObjectToAsset(RootAssetId, root);
                context.SetMainObject(root);

                context.LogImportWarning(
                    "UnityPMXEditor default materials are an approximate diffuse/texture/specular mapping. " +
                    "MMD toon shading and sphere-map behavior are not reproduced.");
                if (!string.IsNullOrEmpty(skinningStatusWarning))
                    context.LogImportWarning(skinningStatusWarning);
                if (!string.IsNullOrEmpty(compatibilityWarning))
                    context.LogImportWarning(compatibilityWarning);
                if (!string.IsNullOrEmpty(physicsWarning))
                    context.LogImportWarning(physicsWarning);
                ownedObjects.Clear();
            }
            catch (Exception exception)
            {
                for (int i = ownedObjects.Count - 1; i >= 0; i--)
                {
                    if (ownedObjects[i] != null) DestroyImmediate(ownedObjects[i]);
                }
                context.LogImportError($"PMX import failed: {exception.Message}");
            }
        }

        private static string[] ResolveTexturePaths(AssetImportContext context,
            PmxValidator validator, PmxDocument document)
        {
            var paths = new string[document.Textures.Count];
            for (int i = 0; i < paths.Length; i++)
                paths[i] = validator.NormalizeTextureAssetPath(context.assetPath, document.Textures[i].Path);
            return paths;
        }

        private static Texture2D[] LoadTextureAssets(AssetImportContext context, string[] paths,
            List<PmxImportDiagnostic> diagnostics)
        {
            var textures = new Texture2D[paths.Length];
            for (int i = 0; i < paths.Length; i++)
            {
                UnityEngine.Object asset = AssetDatabase.LoadMainAssetAtPath(paths[i]);
                if (asset == null)
                {
                    string message = $"PMX texture {i} was not found as a Unity asset: '{paths[i]}'.";
                    diagnostics.Add(new PmxImportDiagnostic(PmxDiagnosticSeverity.Warning,
                        PmxFeatureSupportStatus.Preserved, "TEXTURE_MISSING", message, "Texture"));
                    context.LogImportWarning(message);
                    continue;
                }

                context.DependsOnSourceAsset(paths[i]);
                Texture2D texture = asset as Texture2D;
                if (texture == null)
                {
                    string message =
                        $"PMX texture {i} exists but is not imported as Texture2D: '{paths[i]}'.";
                    diagnostics.Add(new PmxImportDiagnostic(PmxDiagnosticSeverity.Warning,
                        PmxFeatureSupportStatus.Preserved, "TEXTURE_TYPE", message, "Texture"));
                    context.LogImportWarning(message);
                    continue;
                }
                textures[i] = texture;
            }
            return textures;
        }

        private static Material[] ConvertMaterials(PmxDocument document, Texture2D[] textures,
            IMaterialConverter converter, List<UnityEngine.Object> ownedObjects)
        {
            var materials = new Material[document.Materials.Count];
            for (int i = 0; i < materials.Length; i++)
            {
                PmxMaterial source = document.Materials[i];
                Texture2D mainTexture = ResolveTexture(textures, source.TextureIndex);
                Texture2D environmentTexture = ResolveTexture(textures, source.EnvironmentTextureIndex);
                Texture2D toonTexture = source.UsesSharedToonTexture
                    ? null
                    : ResolveTexture(textures, source.ToonTextureIndex);
                materials[i] = converter.Convert(source,
                    new PmxMaterialConversionContext(i, mainTexture, environmentTexture, toonTexture));
                ownedObjects.Add(materials[i]);
            }
            return materials;
        }

        private static Texture2D ResolveTexture(Texture2D[] textures, int index)
            => index >= 0 && index < textures.Length ? textures[index] : null;

        private static PartHierarchyResult CreatePartHierarchy(AssetImportContext context,
            GameObject root,
            PmxDocument document, Mesh mesh, Material[] materials,
            SkeletonConversionResult skeleton, PmxPartHierarchyMode mode)
        {
            var controller = root.AddComponent<PmxModelPartsController>();
            var partsRoot = new GameObject("PMX Model Parts");
            partsRoot.transform.SetParent(root.transform, false);

            if (mode == PmxPartHierarchyMode.ProxyNodes)
            {
                var meshObject = new GameObject("PMX Mesh");
                meshObject.transform.SetParent(root.transform, false);
                SkinnedMeshRenderer renderer = CreateRenderer(meshObject, mesh, materials,
                    skeleton, 0);
                var parts = new PmxModelPart[materials.Length];
                for (int i = 0; i < parts.Length; i++)
                {
                    var partObject = new GameObject(CreatePartName(document.Materials[i], i));
                    partObject.transform.SetParent(partsRoot.transform, false);
                    parts[i] = partObject.AddComponent<PmxModelPart>();
                }

                var renderers = new[] { renderer };
                controller.Configure(mode, mesh, materials, renderer, renderers, parts);
                for (int i = 0; i < parts.Length; i++)
                    parts[i].Configure(i, controller, renderer);
                return new PartHierarchyResult(renderers);
            }

            if (materials.Length == 0)
            {
                var meshObject = new GameObject("PMX Mesh");
                meshObject.transform.SetParent(root.transform, false);
                SkinnedMeshRenderer renderer = CreateRenderer(meshObject, mesh, materials,
                    skeleton, 0);
                var renderers = new[] { renderer };
                controller.Configure(mode, mesh, materials, renderer, renderers,
                    Array.Empty<PmxModelPart>());
                return new PartHierarchyResult(renderers);
            }

            var separateRenderers = new SkinnedMeshRenderer[materials.Length];
            var separateParts = new PmxModelPart[materials.Length];
            for (int i = 0; i < materials.Length; i++)
            {
                var partObject = new GameObject(CreatePartName(document.Materials[i], i));
                partObject.transform.SetParent(partsRoot.transform, false);
                Mesh partMesh = PmxSubmeshMeshBuilder.BuildFullVertexPart(mesh, i);
                partMesh.name = $"PMX Mesh Part {i:D6}";
                context.AddObjectToAsset($"mesh/part/{i:D6}", partMesh);
                separateRenderers[i] = CreateRenderer(partObject, partMesh,
                    new[] { materials[i] }, skeleton, 0);
                separateParts[i] = partObject.AddComponent<PmxModelPart>();
            }

            controller.Configure(mode, mesh, materials, null, separateRenderers, separateParts);
            for (int i = 0; i < separateParts.Length; i++)
                separateParts[i].Configure(i, controller, separateRenderers[i]);
            return new PartHierarchyResult(separateRenderers);
        }

        private static SkinnedMeshRenderer CreateRenderer(GameObject target, Mesh mesh,
            Material[] materials, SkeletonConversionResult skeleton, int unusedSubMeshIndex)
        {
            var renderer = target.AddComponent<SkinnedMeshRenderer>();
            renderer.sharedMesh = mesh;
            renderer.sharedMaterials = materials;
            renderer.bones = skeleton.RendererBones;
            renderer.rootBone = skeleton.RootBone;
            renderer.localBounds = mesh.bounds;
            return renderer;
        }

        private static string CreatePartName(PmxMaterial material, int index)
        {
            string sourceName = !string.IsNullOrWhiteSpace(material.Name)
                ? material.Name
                : material.EnglishName;
            if (string.IsNullOrWhiteSpace(sourceName)) sourceName = "Unnamed";
            char[] chars = sourceName.Trim().ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                if (char.IsControl(chars[i]) || chars[i] == '/' || chars[i] == '\\')
                    chars[i] = '_';
            }
            sourceName = new string(chars);
            if (sourceName.Length > 80) sourceName = sourceName.Substring(0, 80);
            return $"PMX Part {index:D6} - {sourceName}";
        }

        private sealed class PartHierarchyResult
        {
            public PartHierarchyResult(SkinnedMeshRenderer[] renderers)
            {
                Renderers = renderers;
            }

            public SkinnedMeshRenderer[] Renderers { get; }
        }

        private static string ChooseRootName(PmxDocument document)
        {
            if (!string.IsNullOrWhiteSpace(document.EnglishName)) return document.EnglishName;
            if (!string.IsNullOrWhiteSpace(document.Name)) return document.Name;
            return "PMX Model";
        }

        private static List<PmxImportDiagnostic> CreateInitialDiagnostics(PmxDocument document)
        {
            var values = new List<PmxImportDiagnostic>
            {
                new PmxImportDiagnostic(PmxDiagnosticSeverity.Warning,
                    PmxFeatureSupportStatus.Approximated, "MATERIAL_APPROXIMATION",
                    "Default materials approximate PMX diffuse, texture and specular semantics. " +
                    "Toon, sphere-map and edge behavior are preserved as metadata only.", "Material")
            };
            for (int i = 0; i < document.Morphs.Count; i++)
            {
                PmxMorph morph = document.Morphs[i];
                PmxFeatureSupportStatus status = PmxMetadataFactory.MorphSupport(morph.Type);
                values.Add(new PmxImportDiagnostic(PmxDiagnosticSeverity.Info,
                    status,
                    status == PmxFeatureSupportStatus.Supported
                        ? "MORPH_VERTEX_SUPPORTED"
                        : status == PmxFeatureSupportStatus.Approximated
                            ? "MORPH_RUNTIME_APPROXIMATED"
                            : "MORPH_PRESERVED",
                    status == PmxFeatureSupportStatus.Supported
                        ? $"Morph {i} is imported as a deterministic Unity BlendShape."
                        : status == PmxFeatureSupportStatus.Approximated
                            ? $"Morph {i} ({morph.Type}) has a documented approximate runtime effect."
                            : $"Morph {i} ({morph.Type}) is preserved as metadata without a runtime effect.",
                    "Morph"));
            }
            return values;
        }

        private static string AppendSkinningDiagnostics(List<PmxImportDiagnostic> diagnostics,
            SkinningConversionResult skinning, PmxAdvancedDeformMode mode)
        {
            string warning = null;
            if (!string.IsNullOrEmpty(skinning.Warning))
            {
                warning = skinning.Warning;
                diagnostics.Add(new PmxImportDiagnostic(PmxDiagnosticSeverity.Warning,
                    PmxFeatureSupportStatus.Approximated, "SKINNING_APPROXIMATED", warning, "Vertex"));
            }
            else if (skinning.AdvancedDeformVertexCount > 0 &&
                     mode == PmxAdvancedDeformMode.PreserveOnly)
            {
                warning = $"Preserved {skinning.AdvancedDeformVertexCount} SDEF/QDEF vertices without " +
                          "approximating them. They are fixed to a model-space preservation anchor " +
                          "and do not receive SDEF/QDEF deformation in PreserveOnly mode.";
                diagnostics.Add(new PmxImportDiagnostic(PmxDiagnosticSeverity.Warning,
                    PmxFeatureSupportStatus.Preserved, "SKINNING_PRESERVED", warning, "Vertex"));
            }
            if (skinning.FallbackVertexCount > 0)
                diagnostics.Add(new PmxImportDiagnostic(PmxDiagnosticSeverity.Warning,
                    PmxFeatureSupportStatus.Approximated, "SKINNING_FALLBACK",
                    $"Used deterministic fallback bone weights for {skinning.FallbackVertexCount} vertices.",
                    "Vertex"));
            return warning;
        }

        private static string AppendCapabilityDiagnostics(List<PmxImportDiagnostic> diagnostics,
            PmxImportSettings settings, SkinningConversionResult skinning)
        {
            if (settings.RuntimeCapability == PmxRuntimeCapabilityPath.MmdCompatible &&
                skinning.AdvancedDeformVertexCount > 0)
            {
                const string reason =
                    "MmdCompatible requires a dedicated SDEF/QDEF backend, which is not implemented.";
                if (settings.MmdCompatibilityFallback == PmxMmdCompatibilityFallback.Reject)
                    throw new PmxImportValidationException(reason);
                string warning = reason + " Downgraded to StandardApproximate.";
                diagnostics.Add(new PmxImportDiagnostic(PmxDiagnosticSeverity.Warning,
                    PmxFeatureSupportStatus.Approximated, "MMD_COMPATIBILITY_DOWNGRADED",
                    warning, "Runtime"));
                return warning;
            }

            bool compatible = settings.RuntimeCapability == PmxRuntimeCapabilityPath.MmdCompatible;
            diagnostics.Add(new PmxImportDiagnostic(PmxDiagnosticSeverity.Info,
                PmxFeatureSupportStatus.Approximated,
                compatible ? "MMD_COMPATIBLE_ACTIVE" : "STANDARD_APPROXIMATE_ACTIVE",
                compatible
                    ? "MmdCompatible morph/grant/IK ordering is active, but documented semantic differences remain."
                    : "StandardApproximate runtime path is active; documented PMX/MMD differences apply.",
                "Runtime"));
            return null;
        }

        private static string AppendPhysicsDiagnostics(List<PmxImportDiagnostic> diagnostics,
            PmxDocument document, PmxImportSettings settings)
        {
            string warning = null;
            if (settings.PhysicsMode == PmxPhysicsImportMode.None)
            {
                diagnostics.Add(new PmxImportDiagnostic(PmxDiagnosticSeverity.Info,
                    PmxFeatureSupportStatus.Preserved, "PHYSICS_DISABLED",
                    "Rigid-body and joint metadata is preserved, but no Unity physics components were created.",
                    "Physics"));
            }
            else
            {
                warning = "Experimental PMX physics maps Bullet concepts to Unity PhysX approximately; " +
                          "it is not expected to reproduce MMD frame-by-frame.";
                diagnostics.Add(new PmxImportDiagnostic(PmxDiagnosticSeverity.Warning,
                    PmxFeatureSupportStatus.Approximated, "PHYSICS_EXPERIMENTAL",
                    warning, "Physics"));
            }

            for (int i = 0; i < document.RigidBodies.Count; i++)
            {
                if (document.RigidBodies[i].RawShape <= 2) continue;
                diagnostics.Add(new PmxImportDiagnostic(PmxDiagnosticSeverity.Warning,
                    PmxFeatureSupportStatus.Unsupported, "PHYSICS_SHAPE_UNSUPPORTED",
                    $"Rigid body {i} shape value {document.RigidBodies[i].RawShape} is preserved but has no Unity collider mapping.",
                    "RigidBody"));
            }
            for (int i = 0; i < document.Joints.Count; i++)
            {
                if (document.Joints[i].RawType == 0) continue;
                diagnostics.Add(new PmxImportDiagnostic(PmxDiagnosticSeverity.Warning,
                    PmxFeatureSupportStatus.Unsupported, "PHYSICS_JOINT_UNSUPPORTED",
                    $"Joint {i} type value {document.Joints[i].RawType} is preserved; only PMX Spring 6DOF type 0 is mapped.",
                    "Joint"));
            }
            for (int i = 0; i < document.SoftBodies.Count; i++)
            {
                diagnostics.Add(new PmxImportDiagnostic(PmxDiagnosticSeverity.Warning,
                    PmxFeatureSupportStatus.Unsupported, "SOFTBODY_UNSUPPORTED",
                    $"SoftBody {i} is preserved as metadata and has no runtime backend.",
                    "SoftBody"));
            }
            return warning;
        }
    }
}
