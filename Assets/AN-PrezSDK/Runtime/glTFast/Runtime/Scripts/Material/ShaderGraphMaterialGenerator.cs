﻿// Copyright 2020-2021 Andreas Atteneder
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//

#if USING_URP || USING_HDRP
#define GLTFAST_SHADER_GRAPH
#endif

#if GLTFAST_SHADER_GRAPH

using System;
using System.Collections.Generic;
using GLTFast.Schema;
using UnityEngine;
using UnityEngine.Rendering;
using Material = UnityEngine.Material;

namespace GLTFast.Materials {

    using AlphaMode = Schema.Material.AlphaMode;
    using Texture = Schema.Texture;

    public class ShaderGraphMaterialGenerator : MaterialGenerator {
        
        [Flags]
        public enum ShaderMode {
            Opaque = 0,
            Blend = 1,
            Premultiply = 1<<1,
        }

        [Flags]
        protected enum MetallicShaderFeatures {
            Default = 0,
            // Bits 0-1 are the shader modes
            ModeMask = 0x3,
            ModeOpaque = 0,
            ModeFade = 1,
            ModeTransparent = 1<<1,
            // Other flags
            DoubleSided = 1<<2,
            ClearCoat = 1<<3,
            Sheen = 1<<4,
        }

        
        [Flags]
        protected enum SpecularShaderFeatures {
            Default = 0,
            AlphaBlend = 1<<1,
            DoubleSided = 1<<2
        }
        
        [Flags]
        protected enum UnlitShaderFeatures {
            Default = 0,
            AlphaBlend = 1<<1,
            DoubleSided = 1<<2
        }

        const string SHADER_UNLIT = "Shader Graphs/glTF-unlit";
        const string SHADER_SPECULAR = "Shader Graphs/glTF-specular";

        // Keywords
        const string KW_OCCLUSION = "OCCLUSION";
        const string KW_EMISSION = "EMISSION";
        
        static readonly int baseColorPropId = Shader.PropertyToID("_BaseColor");
        static readonly int baseMapPropId = Shader.PropertyToID("_BaseMap");
        static readonly int baseMapScaleTransformPropId = Shader.PropertyToID("_BaseMap_ST"); //TODO: support in shader!
        static readonly int baseMapRotationPropId = Shader.PropertyToID("_BaseMapRotation"); //TODO; support in shader!
        static readonly int baseMapUVChannelPropId = Shader.PropertyToID("_BaseMapUVChannel"); //TODO; support in shader!
        static readonly int metallicRoughnessMapPropId = Shader.PropertyToID("metallicRoughnessTexture");
        static readonly int metallicRoughnessMapScaleTransformPropId = Shader.PropertyToID("metallicRoughnessTexture_ST");
        static readonly int metallicRoughnessMapRotationPropId = Shader.PropertyToID("metallicRoughnessTextureRotation");
        static readonly int metallicRoughnessMapUVChannelPropId = Shader.PropertyToID("metallicRoughnessTextureUVChannel");
        
        static readonly int smoothnessPropId = Shader.PropertyToID("_Smoothness");
        protected static readonly int transmissionFactorPropId = Shader.PropertyToID("transmissionFactor");
        protected static readonly int transmissionTexturePropId = Shader.PropertyToID("_TransmittanceColorMap");
        protected static readonly int transmissionTextureScaleTransformPropId = Shader.PropertyToID("_TransmittanceColorMap_ST");
        protected static readonly int transmissionTextureRotationPropId = Shader.PropertyToID("_TransmittanceColorMapRotation");
        protected static readonly int transmissionTextureUVChannelPropId = Shader.PropertyToID("_TransmittanceColorMapUVChannel");

#if USING_HDRP_10_OR_NEWER
        // const string KW_DISABLE_DECALS = "_DISABLE_DECALS";
        const string KW_DISABLE_SSR_TRANSPARENT = "_DISABLE_SSR_TRANSPARENT";
        const string KW_DOUBLESIDED_ON = "_DOUBLESIDED_ON";
        const string KW_ENABLE_FOG_ON_TRANSPARENT = "_ENABLE_FOG_ON_TRANSPARENT";
        const string KW_SURFACE_TYPE_TRANSPARENT = "_SURFACE_TYPE_TRANSPARENT";
        
        const string k_ShaderPassTransparentDepthPrepass = "TransparentDepthPrepass";
        const string k_ShaderPassTransparentDepthPostpass = "TransparentDepthPostpass";
        const string k_ShaderPassTransparentBackface = "TransparentBackface";
        const string k_ShaderPassRayTracingPrepass = "RayTracingPrepass";

        static readonly int k_DoubleSidedEnablePropId = Shader.PropertyToID("_DoubleSidedEnable");
        static readonly int k_DoubleSidedNormalModePropId = Shader.PropertyToID("_DoubleSidedNormalMode");
        static readonly int k_DoubleSidedConstantsPropId = Shader.PropertyToID("_DoubleSidedConstants");
        static readonly int k_ZTestGBufferPropId = Shader.PropertyToID("_ZTestGBuffer");
        static readonly int k_AlphaDstBlendPropId = Shader.PropertyToID("_AlphaDstBlend");
        static readonly int k_CullModeForwardPropId = Shader.PropertyToID("_CullModeForward");
#endif
        
        static Dictionary<MetallicShaderFeatures,Shader> metallicShaders = new Dictionary<MetallicShaderFeatures,Shader>();
        static Dictionary<SpecularShaderFeatures,Shader> specularShaders = new Dictionary<SpecularShaderFeatures,Shader>();
        static Dictionary<UnlitShaderFeatures,Shader> unlitShaders = new Dictionary<UnlitShaderFeatures,Shader>();

        public override Material GetDefaultMaterial() {
            return GetMetallicMaterial(MetallicShaderFeatures.Default);
        }

        Material GetMetallicMaterial( MetallicShaderFeatures metallicShaderFeatures ) {
            
            bool doubleSided = (metallicShaderFeatures & MetallicShaderFeatures.DoubleSided) != 0;
            
            if(!metallicShaders.TryGetValue(metallicShaderFeatures,value: out var shader)) {
                ShaderMode mode = (ShaderMode) (metallicShaderFeatures & MetallicShaderFeatures.ModeMask);
#if USING_HDRP_10_OR_NEWER
                mode = ShaderMode.Opaque;
                doubleSided = false;
#endif
                // TODO: add ClearCoat support
                bool coat = false; // (metallicShaderFeatures & MetallicShaderFeatures.ClearCoat) != 0;
                // TODO: add sheen support
                bool sheen = false; // (metallicShaderFeatures & MetallicShaderFeatures.Sheen) != 0;
                
                var shaderName = string.Format(
                    "Shader Graphs/glTF-metallic-{0}{1}{2}{3}",
                    mode,
                    coat ? "-coat" : "",
                    sheen ? "-sheen" : "",
                    doubleSided ? "-double" : ""
                );
                shader = FindShader(shaderName);
                metallicShaders[metallicShaderFeatures] = shader;
            }
            if(shader==null) {
                return null;
            }
            var mat = new Material(shader);
#if UNITY_EDITOR
            mat.doubleSidedGI = doubleSided; 
#endif
            return mat;
        }

        Material GetUnlitMaterial(Schema.Material gltfMaterial)
        {
            var features = GetUnlitShaderFeatures(gltfMaterial);
            bool doubleSided = (features & UnlitShaderFeatures.DoubleSided) != 0;
            Shader shader = null;
            if(!unlitShaders.TryGetValue(features, out shader)) {
                bool alphaBlend = (features & UnlitShaderFeatures.AlphaBlend) != 0;
                var shaderName = string.Format(
                    "{0}{1}{2}",
                    SHADER_UNLIT,
                    alphaBlend ? "-Blend" : "-Opaque",
                    doubleSided ? "-double" : ""
                );
                shader = FindShader(shaderName);
                unlitShaders[features] = shader;
            }
            if(shader==null) {
                return null;
            }
            var mat = new Material(shader);
#if UNITY_EDITOR
            mat.doubleSidedGI = gltfMaterial.doubleSided;
#endif
            return mat;
        }
        
        Material GetSpecularMaterial(SpecularShaderFeatures features) {
            bool doubleSided = (features & SpecularShaderFeatures.DoubleSided) != 0;
            Shader shader = null;
            if(!specularShaders.TryGetValue(features,out shader)) {
                bool alphaBlend = (features & SpecularShaderFeatures.AlphaBlend) != 0;
                var shaderName = string.Format(
                    "{0}{1}{2}",
                    SHADER_SPECULAR,
                    alphaBlend ? "-Blend" : "-Opaque",
                    doubleSided ? "-double" : ""
                    );
                shader = FindShader(shaderName);
                specularShaders[features] = shader;
            }
            if(shader==null) {
                return null;
            }
            var mat = new Material(shader);
#if UNITY_EDITOR
            mat.doubleSidedGI = doubleSided;
#endif
            return mat;
        }

        public override Material GenerateMaterial(Schema.Material gltfMaterial, IGltfReadable gltf) {

            Material material;

            MaterialType? materialType = null;
            ShaderMode shaderMode = ShaderMode.Opaque;

            if (gltfMaterial.extensions?.KHR_materials_unlit!=null) {
                material = GetUnlitMaterial(gltfMaterial);
                materialType = MaterialType.Unlit;
                shaderMode = gltfMaterial.alphaModeEnum != AlphaMode.OPAQUE ? ShaderMode.Blend : ShaderMode.Opaque;
            } else {
                bool isMetallicRoughness = gltfMaterial.extensions?.KHR_materials_pbrSpecularGlossiness == null;
                if (isMetallicRoughness) {
                    materialType = MaterialType.MetallicRoughness;
                    var metallicShaderFeatures = GetMetallicShaderFeatures(gltfMaterial);
                    material = GetMetallicMaterial(metallicShaderFeatures);
                    shaderMode = (ShaderMode)(metallicShaderFeatures & MetallicShaderFeatures.ModeMask);
                }
                else {
                    materialType = MaterialType.SpecularGlossiness;
                    var specularShaderFeatures = GetSpecularShaderFeatures(gltfMaterial);
                    material = GetSpecularMaterial(specularShaderFeatures);
                    if ((specularShaderFeatures & SpecularShaderFeatures.AlphaBlend) != 0) {
                        shaderMode = ShaderMode.Blend;
                    }
                }
            }

            if(material==null) return null;

            material.name = gltfMaterial.name;

            Color baseColorLinear = Color.white;
            RenderQueue? renderQueue = null;
            
            //added support for KHR_materials_pbrSpecularGlossiness
            if (gltfMaterial.extensions != null) {
                Schema.PbrSpecularGlossiness specGloss = gltfMaterial.extensions.KHR_materials_pbrSpecularGlossiness;
                if (specGloss != null) {
                    baseColorLinear = specGloss.diffuseColor;
                    material.SetVector(specColorPropId, specGloss.specularColor);
                    material.SetFloat(smoothnessPropId, specGloss.glossinessFactor);

                    TrySetTexture(
                        specGloss.diffuseTexture,
                        material,
                        gltf,
                        baseMapPropId,
                        baseMapScaleTransformPropId,
                        baseMapRotationPropId,
                        baseMapUVChannelPropId
                        );

                    if (TrySetTexture(
                        specGloss.specularGlossinessTexture,
                        material,
                        gltf,
                        specGlossMapPropId,
                        specGlossScaleTransformMapPropId,
                        specGlossMapRotationPropId,
                        specGlossMapUVChannelPropId
                        ))
                    {
                        // material.EnableKeyword();
                    }
                }
            }

            if (gltfMaterial.pbrMetallicRoughness!=null
                // If there's a specular-glossiness extension, ignore metallic-roughness
                // (according to extension specification)
                && gltfMaterial.extensions?.KHR_materials_pbrSpecularGlossiness == null)
            {
                baseColorLinear = gltfMaterial.pbrMetallicRoughness.baseColor;

                if (materialType != MaterialType.SpecularGlossiness) {
                    // baseColorTexture can be used by both MetallicRoughness AND Unlit materials
                    TrySetTexture(
                        gltfMaterial.pbrMetallicRoughness.baseColorTexture,
                        material,
                        gltf,
                        baseMapPropId,
                        baseMapScaleTransformPropId,
                        baseMapRotationPropId,
                        baseMapUVChannelPropId
                        );
                }

                if (materialType==MaterialType.MetallicRoughness)
                {
                    material.SetFloat(metallicPropId, gltfMaterial.pbrMetallicRoughness.metallicFactor );
                    material.SetFloat(smoothnessPropId, 1-gltfMaterial.pbrMetallicRoughness.roughnessFactor );

                    if(TrySetTexture(
                        gltfMaterial.pbrMetallicRoughness.metallicRoughnessTexture,
                        material,
                        gltf,
                        metallicRoughnessMapPropId,
                        metallicRoughnessMapScaleTransformPropId,
                        metallicRoughnessMapRotationPropId,
                        metallicRoughnessMapUVChannelPropId
                        )) {
                        // material.EnableKeyword(KW_METALLIC_ROUGHNESS_MAP);
                    }

                    // TODO: When the occlusionTexture equals the metallicRoughnessTexture, we could sample just once instead of twice.
                    // if (!DifferentIndex(gltfMaterial.occlusionTexture,gltfMaterial.pbrMetallicRoughness.metallicRoughnessTexture)) {
                    //    ...
                    // }
                }
            }

            if(TrySetTexture(
                gltfMaterial.normalTexture,
                material,
                gltf,
                bumpMapPropId,
                bumpMapScaleTransformPropId,
                bumpMapRotationPropId,
                bumpMapUVChannelPropId
                )) {
                // material.EnableKeyword(Constants.kwNormalMap);
                material.SetFloat(bumpScalePropId,gltfMaterial.normalTexture.scale);
            }
            
            if(TrySetTexture(
                gltfMaterial.occlusionTexture,
                material,
                gltf,
                occlusionMapPropId,
                occlusionMapScaleTransformPropId,
                occlusionMapRotationPropId,
                occlusionMapUVChannelPropId
                )) {
                material.EnableKeyword(KW_OCCLUSION);
                material.SetFloat(occlusionStrengthPropId,gltfMaterial.occlusionTexture.strength);
            }

            if(TrySetTexture(
                gltfMaterial.emissiveTexture,
                material,
                gltf,
                emissionMapPropId,
                emissionMapScaleTransformPropId,
                emissionMapRotationPropId,
                emissionMapUVChannelPropId
                )) {
                material.EnableKeyword(KW_EMISSION);
            }
            
            if (gltfMaterial.extensions != null) {

                // Transmission - Approximation
                var transmission = gltfMaterial.extensions.KHR_materials_transmission;
                if (transmission != null) {
                    renderQueue = ApplyTransmission(ref baseColorLinear, gltf, transmission, material, renderQueue);
                }
            }

            if (gltfMaterial.alphaModeEnum == AlphaMode.MASK) {
                material.SetFloat(cutoffPropId, gltfMaterial.alphaCutoff);
            } else {
                material.SetFloat(cutoffPropId, 0);
                // double sided opaque would make errors in HDRP 7.3 otherwise
                material.SetOverrideTag("MotionVector","User");
                material.SetShaderPassEnabled("MOTIONVECTORS",false);
            }
            if (!renderQueue.HasValue) {
                if(shaderMode == ShaderMode.Opaque) {
                    renderQueue = gltfMaterial.alphaModeEnum == AlphaMode.MASK
                        ? RenderQueue.AlphaTest
                        : RenderQueue.Geometry;
                } else {
                    renderQueue = RenderQueue.Transparent;
                }
            }

            material.renderQueue = (int) renderQueue.Value;

#if USING_HDRP_10_OR_NEWER
            if (gltfMaterial.doubleSided) {
                material.EnableKeyword(KW_DOUBLESIDED_ON);
                material.SetFloat(k_DoubleSidedEnablePropId, 1);
                
                // UnityEditor.Rendering.HighDefinition.DoubleSidedNormalMode.Flip
                material.SetFloat(k_DoubleSidedNormalModePropId, 0);
                material.SetVector(k_DoubleSidedConstantsPropId, new Vector4(-1,-1,-1,0));
                
                material.SetFloat("_CullMode", (int)CullMode.Off);
            }
            
            switch (shaderMode) {
                case ShaderMode.Opaque:
                    break;
                case ShaderMode.Blend:
                    material.EnableKeyword(KW_ALPHATEST_ON);
                    material.EnableKeyword(KW_SURFACE_TYPE_TRANSPARENT);
                    // material.EnableKeyword(KW_DISABLE_DECALS);
                    material.EnableKeyword(KW_DISABLE_SSR_TRANSPARENT);
                    material.EnableKeyword(KW_ENABLE_FOG_ON_TRANSPARENT);
                    material.SetOverrideTag(TAG_RENDER_TYPE, TAG_RENDER_TYPE_TRANSPARENT);
                    material.SetShaderPassEnabled(k_ShaderPassTransparentDepthPrepass, false);
                    material.SetShaderPassEnabled(k_ShaderPassTransparentDepthPostpass, false);
                    material.SetShaderPassEnabled(k_ShaderPassTransparentBackface, false);
                    material.SetShaderPassEnabled(k_ShaderPassRayTracingPrepass, false);
                    material.SetFloat(k_ZTestGBufferPropId, (int)CompareFunction.Equal); //3
                    material.SetFloat(k_AlphaDstBlendPropId, (int)BlendMode.OneMinusSrcAlpha);//10
                    material.SetFloat(dstBlendPropId, (int)BlendMode.OneMinusSrcAlpha);//10
                    material.SetFloat(srcBlendPropId, (int) BlendMode.SrcAlpha);//5
                    material.SetFloat(cullModePropId, (int)CullMode.Off);
                    material.SetFloat(k_CullModeForwardPropId, (int)CullMode.Off);
                    break;
                case ShaderMode.Premultiply:
                    break;
            }
#endif

            material.SetVector(baseColorPropId, baseColorLinear);
            
            if(gltfMaterial.emissive != Color.black) {
                material.SetColor(emissionColorPropId, gltfMaterial.emissive);
                material.EnableKeyword(KW_EMISSION);
            }

            return material;
        }

        protected virtual RenderQueue? ApplyTransmission(
            ref Color baseColorLinear,
            IGltfReadable gltf,
            Transmission transmission,
            Material material,
            RenderQueue? renderQueue
            )
        {
#if UNITY_EDITOR
            // ReSharper disable once Unity.PerformanceCriticalCodeInvocation
            logger?.Warning(LogCode.MaterialTransmissionApproxURP);
#endif
            // Correct transmission is not supported in Built-In renderer
            // This is an approximation for some corner cases
            if (transmission.transmissionFactor > 0f && transmission.transmissionTexture.index < 0) {
                var premul = TransmissionWorkaroundShaderMode(transmission, ref baseColorLinear);
            }
            return renderQueue;
        }

        protected MetallicShaderFeatures GetMetallicShaderFeatures(Schema.Material gltfMaterial) {

            var feature = MetallicShaderFeatures.Default;
            ShaderMode? sm = null;

            if (gltfMaterial.extensions != null) {

                if (gltfMaterial.extensions.KHR_materials_clearcoat != null &&
                    gltfMaterial.extensions.KHR_materials_clearcoat.clearcoatFactor > 0) feature |= MetallicShaderFeatures.ClearCoat;
                if (gltfMaterial.extensions.KHR_materials_sheen != null &&
                    gltfMaterial.extensions.KHR_materials_sheen.sheenColor.maxColorComponent > 0) feature |= MetallicShaderFeatures.Sheen;

                if (
                    gltfMaterial.extensions.KHR_materials_transmission != null
                    && gltfMaterial.extensions.KHR_materials_transmission.transmissionFactor > 0
                ) {
                    sm = ApplyTransmissionShaderFeatures(gltfMaterial);
                }
            }

            if (gltfMaterial.doubleSided) feature |= MetallicShaderFeatures.DoubleSided;

            if (!sm.HasValue) {
                sm = gltfMaterial.alphaModeEnum != AlphaMode.OPAQUE ? ShaderMode.Blend : ShaderMode.Opaque;
            } 
            
            feature |= (MetallicShaderFeatures)sm;

            return feature;
        }

        protected virtual ShaderMode? ApplyTransmissionShaderFeatures(Schema.Material gltfMaterial) {
            // Makeshift approximation
            Color baseColorLinear = Color.white;
            var premul = TransmissionWorkaroundShaderMode(gltfMaterial.extensions.KHR_materials_transmission, ref baseColorLinear);
            ShaderMode? sm = premul ? ShaderMode.Premultiply : ShaderMode.Blend;
            return sm;
        }

        static SpecularShaderFeatures GetSpecularShaderFeatures(Schema.Material gltfMaterial) {

            var feature = SpecularShaderFeatures.Default;
            if (gltfMaterial.doubleSided) feature |= SpecularShaderFeatures.DoubleSided;

            if (gltfMaterial.alphaModeEnum != AlphaMode.OPAQUE) {
                feature |= SpecularShaderFeatures.AlphaBlend;
            }
            return feature;
        }
        
        static UnlitShaderFeatures GetUnlitShaderFeatures(Schema.Material gltfMaterial) {

            var feature = UnlitShaderFeatures.Default;
            if (gltfMaterial.doubleSided) feature |= UnlitShaderFeatures.DoubleSided;

            if (gltfMaterial.alphaModeEnum != AlphaMode.OPAQUE) {
                feature |= UnlitShaderFeatures.AlphaBlend;
            }
            return feature;
        }
    }
}
#endif // GLTFAST_SHADER_GRAPH
