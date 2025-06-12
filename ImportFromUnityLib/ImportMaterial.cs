﻿extern alias Froox;

using Froox::Elements.Core;
using Froox::FrooxEngine;
using MemoryMappedFileIPC;
using ResoniteUnityExporterShared;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ImportFromUnityLib
{
    public class ImportMaterial
    {
        static bool TryGetMaterialBool(Material_U2Res material, string fieldName, out bool value)
        {
            value = default(bool);
            for (int i = 0; i < material.keywords.Length; i++)
            {
                if (material.keywords[i].ToLower() == fieldName.ToLower())
                {
                    value = material.keywordValues[i];
                    return true;
                }
            }
            // try int and float as well, depends on the shader
            if (TryGetMaterialInt(material, fieldName, out int intValue))
            {
                value = intValue == 1;
                return true;
            }
            if (TryGetMaterialFloat(material, fieldName, out float floatValue))
            {
                value = floatValue == 1.0f;
                return true;
            }
            return false;
        }


        static bool TryGetMaterialInt(Material_U2Res material, string fieldName, out int value)
        {
            value = default(int);
            for (int i = 0; i < material.intNames.Length; i++)
            {
                if (material.intNames[i].ToLower() == fieldName.ToLower())
                {
                    value = material.intValues[i];
                    return true;
                }
            }
            return false;
        }

        static bool TryGetMaterialFloat(Material_U2Res material, string fieldName, out float value)
        {
            value = default(float);
            for (int i = 0; i < material.floatNames.Length; i++)
            {
                if (material.floatNames[i].ToLower() == fieldName.ToLower())
                {
                    value = material.floatValues[i];
                    return true;
                }
            }
            return false;
        }

        static bool TryGetMaterialTextureTransform(Material_U2Res material, string fieldName, out float2 offset, out float2 scale)
        {
            offset = new float2(0, 0);
            scale = new float2(1, 1);
            if (TryGetMaterialFloat4(material, fieldName, out Float4_U2Res value))
            {
                // xy is scale, zw is offset because of unity reasons (idk)
                offset = new float2(value.z, value.w);
                scale = new float2(value.x, value.y);
                return true;
            }
            return false;
        }

        static bool TryGetMaterialFloat4(Material_U2Res material, string fieldName, out Float4_U2Res value)
        {
            value = new Float4_U2Res();
            for (int i = 0; i < material.float4Names.Length; i++)
            {
                if (material.float4Names[i].ToLower() == fieldName.ToLower())
                {
                    value = material.float4Values[i];
                    return true;
                }
            }
            return false;
        }

        static bool TryGetMaterialTexture(Material_U2Res material, string textureName, out RefID_U2Res matRefID)
        {
            matRefID = new RefID_U2Res()
            {
                id = 0
            };

            for (int i = 0; i < material.texture2DNames.Length; i++)
            {
                if (material.texture2DNames[i].ToLower() == textureName.ToLower())
                {
                    matRefID = material.texture2DValues[i];
                    return true;
                }
            }
            return false;
        }
        static void ImportToMaterialHelper(byte[] materialBytes, OutputBytesHolder outputBytes)
        {
            // Load mesh data into a meshx
            Material_U2Res materialData = SerializationUtils.DecodeObject<Material_U2Res>(materialBytes);
            ImportFromUnityLib.DebugLog("Importing material " + materialData.materialName);
            Slot assetsSlot = (Slot)ImportFromUnityUtils.LookupRefID(materialData.rootAssetsSlot);

            bool hasColor = false;
            colorX matColor = new colorX(1, 1, 1, 1);
            bool hasEmissionColor = false;
            colorX matEmissionColor = new colorX(1,1,1,1);

            bool hasMainTex = false;
            RefID_U2Res mainTexRefID = new RefID_U2Res();
            float2 mainTexOffset = new float2(0,0);
            float2 mainTexScale = new float2(1,1);

            bool hasMetallicGlossTex = false;
            RefID_U2Res metallicGlossTexRefID;
            float2 metallicGlossTexOffset;
            float2 metallicGlossTexScale = new float2(1,1);

            bool hasEmissionMapTex = false;
            RefID_U2Res emissionMapTexRefID;
            float2 emissionMapTexOffset;
            float2 emissionMapTexScale = new float2(1,1);

            bool hasOcclusionMapTex = false;
            RefID_U2Res occlusionMapTexRefID;
            float2 occlusionMapTexOffset;
            float2 occlusionMapTexScale = new float2(1,1);

            bool hasBumpMapTex = false;
            RefID_U2Res bumpMapTexRefID;
            float2 bumpMapTexOffset = new float2(0, 0);
            float2 bumpMapTexScale = new float2(1, 1);

            bool hasDetailAlbedoMapTex = false;
            RefID_U2Res detailAlbedoMapTexRefID = new RefID_U2Res();
            float2 detailAlbedoMapTexOffset = new float2(0, 0);
            float2 detailAlbedoMapTexScale = new float2(1, 1);

            bool hasDetailNormalMapTex = false;
            RefID_U2Res detailNormalMapTexRefID = new RefID_U2Res();
            float2 detailNormalMapTexOffset = new float2(0, 0);
            float2 detailNormalMapTexScale = new float2(1, 1);

            bool hasCullingMode = false;
            Culling cullingMode = Culling.Back;

            float renderingMode = 0.0f;


            bool emissionEnabled = true;

            if (TryGetMaterialBool(materialData, "_EMISSION", out bool _emissionEnabledStandard))
            {
                emissionEnabled = _emissionEnabledStandard;
            }

            if (TryGetMaterialBool(materialData, "_UseEmission", out bool _emissionEnabledLilToon))
            {
                emissionEnabled = _emissionEnabledLilToon;
            }

            if (TryGetMaterialFloat(materialData, "_Mode", out renderingMode))
            {

            }
            renderingMode = (float)Math.Round(renderingMode);
            BlendMode blendMode = BlendMode.Opaque;
            if (renderingMode == 0.0)
            {
                blendMode = BlendMode.Opaque;
            }
            else if (renderingMode == 1.0)
            {
                blendMode = BlendMode.Cutout;
            }
            else if (renderingMode == 2.0)
            {
                blendMode = BlendMode.Alpha;
            }
            else if (renderingMode == 3.0)
            {
                blendMode = BlendMode.Transparent;
            }

            // _Color,
            if (TryGetMaterialFloat4(materialData, "_Color", out Float4_U2Res color))
            {
                hasColor = true;
                //mat.Color.Value
                matColor = new colorX(color.x, color.y, color.z, color.w);
            }
            if (TryGetMaterialFloat4(materialData, "_AlbedoColor", out Float4_U2Res color2))
            {
                hasColor = true;
                //mat.Color.Value
                matColor = new colorX(color2.x, color2.y, color2.z, color2.w);
            }
            // _EmissionColor,
            if (TryGetMaterialFloat4(materialData, "_EmissionColor", out Float4_U2Res emissionColor))
            {
                hasEmissionColor = true;
                // mat.EmissionColor.Value
                matEmissionColor = new colorX(emissionColor.x, emissionColor.y, emissionColor.z, emissionColor.w);
            }
            // _MainTex_ST,_MainTex_TexelSize,_MainTex_HDR,
            if (TryGetMaterialTexture(materialData, "_MainTex", out RefID_U2Res _mainTexRefID))
            {
                hasMainTex = true;
                mainTexRefID = _mainTexRefID;
                // mat.MainTexture.Value
            }
            if (TryGetMaterialTextureTransform(materialData, "_MainTex_ST", out float2 _mainTexOffset, out float2 _mainTexScale))
            {
                mainTexOffset = _mainTexOffset;
                mainTexScale = _mainTexScale;
                //mat.MainTextureOffset.Value
                //mat.MainTextureScale.Value
            }
            // sometimes they are albedo instead
            if (TryGetMaterialTexture(materialData, "_Albedo", out RefID_U2Res _mainTexRefID2))
            {
                hasMainTex = true;
                mainTexRefID = _mainTexRefID2;
                // mat.MainTexture.Value
            }
            if (TryGetMaterialTextureTransform(materialData, "_Albedo_ST", out float2 mainTexOffset2, out float2 mainTexScale2))
            {
                mainTexOffset = mainTexOffset2;
                mainTexScale = mainTexScale2;
                //mat.MainTextureOffset.Value
                //mat.MainTextureScale.Value
            }
            // _MetallicGlossMap_ST,_MetallicGlossMap_TexelSize,_MetallicGlossMap_HDR,
            if (TryGetMaterialTexture(materialData, "_MetallicGlossMap", out metallicGlossTexRefID))
            {
                hasMetallicGlossTex = true;
                //mat.MetallicGlossMap.Value
            }
            if (TryGetMaterialTextureTransform(materialData, "_MetallicGlossMap_ST", out metallicGlossTexOffset, out metallicGlossTexScale))
            {
                //mat.MetallicGlossMapOffset.Value
                //mat.MetallicGlossMapScale.Value
            }
            
            // _EmissionMap_ST,_EmissionMap_TexelSize,_EmissionMap_HDR,
            if (TryGetMaterialTexture(materialData, "_EmissionMap", out emissionMapTexRefID))
            {
                hasEmissionMapTex = true;
                //mat.EmissionMap.Value
            }
            if (TryGetMaterialTextureTransform(materialData, "_EmissionMap_ST", out emissionMapTexOffset, out emissionMapTexScale))
            {
                //mat.EmissionMapOffset.Value
                //mat.EmissionMapScale.Value
            }

            // _OcclusionMap_ST,_OcclusionMap_TexelSize,_OcclusionMap_HDR,
            if (TryGetMaterialTexture(materialData, "_OcclusionMap", out occlusionMapTexRefID))
            {
                hasOcclusionMapTex = true;
                //mat.OcclusionMap.Value
            }
            if (TryGetMaterialTextureTransform(materialData, "_OcclusionMap_ST", out occlusionMapTexOffset, out occlusionMapTexScale))
            {
                //mat.OcclusionMapOffset.Value
                //mat.OcclusionMapScale.Value
            }

            // _BumpMap_ST,_BumpMap_TexelSize,_BumpMap_HDR,
            // "normal maps are a type of bump map" says unity
            if (TryGetMaterialTexture(materialData, "_BumpMap", out bumpMapTexRefID))
            {
                hasBumpMapTex = true;
                //mat.NormalMap.Value
            }
            if (TryGetMaterialTextureTransform(materialData, "_BumpMap_ST", out bumpMapTexOffset, out bumpMapTexScale))
            {
                //mat.NormalMapOffset.Value
                //mat.NormalMapScale.Value
            }
            // _DetailAlbedoMap_ST,_DetailAlbedoMap_TexelSize,_DetailAlbedoMap_HDR,
            if (TryGetMaterialTexture(materialData, "_DetailAlbedoMap", out detailAlbedoMapTexRefID))
            {
                hasDetailAlbedoMapTex = true;
                //mat.NormalMap.Value
            }
            if (TryGetMaterialTextureTransform(materialData, "_DetailAlbedoMap_ST", out detailAlbedoMapTexOffset, out detailAlbedoMapTexScale))
            {
                //mat.NormalMapOffset.Value
                //mat.NormalMapScale.Value
            }

            // _DetailNormalMap_ST,_DetailNormalMap_TexelSize,_DetailNormalMap_HDR
            if (TryGetMaterialTexture(materialData, "_DetailNormalMap", out detailNormalMapTexRefID))
            {
                hasDetailNormalMapTex = true;
                //mat.NormalMap.Value
            }
            if (TryGetMaterialTextureTransform(materialData, "_DetailNormalMap_ST", out detailNormalMapTexOffset, out detailNormalMapTexScale))
            {
                //mat.NormalMapOffset.Value
                //mat.NormalMapScale.Value
            }
            if (TryGetMaterialFloat(materialData, "_Cull", out float cullMode))
            {
                hasCullingMode = true;
                cullMode = (float)Math.Round(cullMode);
                if (cullMode == 0.0)
                {
                    cullingMode = Culling.Off;
                }
                else if (cullMode == 1.0)
                {
                    cullingMode = Culling.Front;
                }
                else if (cullMode == 2.0)
                {
                    cullingMode = Culling.Back;
                }
            }

            RefID_U2Res matRefId = new RefID_U2Res()
            {
                id = 0
            };
            if (materialData.materialName == MaterialNames_U2Res.XIEXE_TOON_MAT)
            {
                XiexeToonMaterial mat = assetsSlot.AttachComponent<XiexeToonMaterial>();
                mat.BlendMode.Value = blendMode;
                matRefId.id = (ulong)mat.ReferenceID;
                if (hasColor)
                {
                    mat.Color.Value = matColor;
                }
                if (hasEmissionColor)
                {
                    mat.EmissionColor.Value = emissionEnabled
                        ? matEmissionColor
                        : new colorX(0,0,0,0);
                }
                if (hasMainTex)
                {
                    mat.MainTexture.Value = mainTexRefID.id;
                    mat.MainTextureOffset.Value = mainTexOffset;
                    mat.MainTextureScale.Value = mainTexScale;
                }
                if (hasMetallicGlossTex)
                {
                    mat.MetallicGlossMap.Value = metallicGlossTexRefID.id;
                    mat.MetallicGlossMapOffset.Value = metallicGlossTexOffset;
                    mat.MetallicGlossMapScale.Value = metallicGlossTexScale;
                }
                if (hasEmissionMapTex)
                {
                    mat.EmissionMap.Value = emissionMapTexRefID.id;
                    mat.EmissionMapOffset.Value = emissionMapTexOffset;
                    mat.EmissionMapScale.Value = emissionMapTexScale;
                }
                if (hasOcclusionMapTex)
                {
                    mat.OcclusionMap.Value = occlusionMapTexRefID.id;
                    mat.OcclusionMapOffset.Value = occlusionMapTexOffset;
                    mat.OcclusionMapScale.Value = occlusionMapTexScale;
                }
                if (hasBumpMapTex)
                {
                    mat.NormalMap.Value = bumpMapTexRefID.id;
                    mat.NormalMapOffset.Value = bumpMapTexOffset;
                    mat.NormalMapScale.Value = bumpMapTexScale;
                }
                if (hasCullingMode)
                {
                    mat.Culling.Value = cullingMode;
                }
            }
            else if (materialData.materialName == MaterialNames_U2Res.PBS_SPECULAR_MAT) {
                PBS_Specular mat = assetsSlot.AttachComponent<PBS_Specular>();
                mat.BlendMode.Value = blendMode;
                matRefId.id = (ulong)mat.ReferenceID;
                if (hasColor)
                {
                    mat.AlbedoColor.Value = matColor;
                }
                if (hasEmissionColor)
                {
                    mat.EmissiveColor.Value = emissionEnabled
                        ? matEmissionColor
                        : new colorX(0,0,0,0);
                }
                if (hasMainTex)
                {
                    mat.AlbedoTexture.Value = mainTexRefID.id;
                    mat.TextureOffset.Value = mainTexOffset;
                    mat.TextureScale.Value = mainTexScale;
                }
                if (hasMetallicGlossTex)
                {
                    //mat.MetallicGlossMap.Value = metallicGlossTexRefID.id;
                    //mat.MetallicGlossMapOffset.Value = metallicGlossTexOffset;
                    //mat.MetallicGlossMapScale.Value = metallicGlossTexScale;
                }
                if (hasEmissionMapTex)
                {
                    mat.EmissiveMap.Value = emissionMapTexRefID.id;
                    //mat.EmissionMapOffset.Value = emissionMapTexOffset;
                    //mat.EmissionMapScale.Value = emissionMapTexScale;
                }
                if (hasOcclusionMapTex)
                {
                    mat.OcclusionMap.Value = occlusionMapTexRefID.id;
                    //mat.OcclusionMapOffset.Value = occlusionMapTexOffset;
                    //mat.OcclusionMapScale.Value = occlusionMapTexScale;
                }
                if (hasBumpMapTex)
                {
                    mat.NormalMap.Value = bumpMapTexRefID.id;
                    //mat.NormalMapOffset.Value = bumpMapTexOffset;
                    //mat.NormalScale.Value;
                }
                if (hasDetailAlbedoMapTex)
                {
                    mat.DetailAlbedoTexture.Value = detailAlbedoMapTexRefID.id;
                    mat.DetailTextureOffset.Value = detailAlbedoMapTexOffset;
                    mat.DetailTextureScale.Value = detailAlbedoMapTexScale;
                }

                if (hasDetailNormalMapTex)
                {
                    mat.DetailNormalMap.Value = detailNormalMapTexRefID.id;
                    //mat.DetailNormalScale.Value = detailNormalMapTexScale;
                }
            }
            else if (materialData.materialName == MaterialNames_U2Res.PBS_METALLIC_MAT)
            {
                PBS_Metallic mat = assetsSlot.AttachComponent<PBS_Metallic>();
                mat.BlendMode.Value = blendMode;
                matRefId.id = (ulong)mat.ReferenceID;
                if (hasColor)
                {
                    mat.AlbedoColor.Value = matColor;
                }
                if (hasEmissionColor)
                {
                    mat.EmissiveColor.Value = emissionEnabled
                        ? matEmissionColor
                        : new colorX(0, 0, 0, 0);
                }
                if (hasMainTex)
                {
                    mat.AlbedoTexture.Value = mainTexRefID.id;
                    mat.TextureOffset.Value = mainTexOffset;
                    mat.TextureScale.Value = mainTexScale;
                }
                if (hasMetallicGlossTex)
                {
                    mat.MetallicMap.Value = metallicGlossTexRefID.id;
                    //mat.MetallicGlossMapOffset.Value = metallicGlossTexOffset;
                    //mat.MetallicGlossMapScale.Value = metallicGlossTexScale;
                }
                if (hasEmissionMapTex)
                {
                    mat.EmissiveMap.Value = emissionMapTexRefID.id;
                    //mat.EmissionMapOffset.Value = emissionMapTexOffset;
                    //mat.EmissionMapScale.Value = emissionMapTexScale;
                }
                if (hasOcclusionMapTex)
                {
                    mat.OcclusionMap.Value = occlusionMapTexRefID.id;
                    //mat.OcclusionMapOffset.Value = occlusionMapTexOffset;
                    //mat.OcclusionMapScale.Value = occlusionMapTexScale;
                }
                if (hasBumpMapTex)
                {
                    mat.NormalMap.Value = bumpMapTexRefID.id;
                    //mat.NormalMapOffset.Value = bumpMapTexOffset;
                    //mat.NormalScale.Value;
                }
                if (hasDetailAlbedoMapTex)
                {
                    mat.DetailAlbedoTexture.Value = detailAlbedoMapTexRefID.id;
                    mat.DetailTextureOffset.Value = detailAlbedoMapTexOffset;
                    mat.DetailTextureScale.Value = detailAlbedoMapTexScale;
                }

                if (hasDetailNormalMapTex)
                {
                    mat.DetailNormalMap.Value = detailNormalMapTexRefID.id;
                    //mat.DetailNormalScale.Value = detailNormalMapTexScale;
                }
            }
            else if (materialData.materialName == MaterialNames_U2Res.UNLIT_MAT)
            {
                UnlitMaterial mat = assetsSlot.AttachComponent<UnlitMaterial>();
                mat.BlendMode.Value = blendMode;
                matRefId.id = (ulong)mat.ReferenceID;
                if (hasColor)
                {
                    mat.TintColor.Value = matColor;
                }
                if (hasEmissionColor)
                {
                    //mat.color.Value = matEmissionColor;
                }
                if (hasMainTex)
                {
                    mat.Texture.Value = mainTexRefID.id;
                    mat.TextureOffset.Value = mainTexOffset;
                    mat.TextureScale.Value = mainTexScale;
                }
                if (hasMetallicGlossTex)
                {
                    //mat.MetallicMap.Value = metallicGlossTexRefID.id;
                    //mat.MetallicGlossMapOffset.Value = metallicGlossTexOffset;
                    //mat.MetallicGlossMapScale.Value = metallicGlossTexScale;
                }
                if (hasEmissionMapTex)
                {
                    //mat.EmissiveMap.Value = emissionMapTexRefID.id;
                    //mat.EmissionMapOffset.Value = emissionMapTexOffset;
                    //mat.EmissionMapScale.Value = emissionMapTexScale;
                }
                if (hasOcclusionMapTex)
                {
                    //mat.OcclusionMap.Value = occlusionMapTexRefID.id;
                    //mat.OcclusionMapOffset.Value = occlusionMapTexOffset;
                    //mat.OcclusionMapScale.Value = occlusionMapTexScale;
                }
                if (hasBumpMapTex)
                {
                    StaticTexture2D tex = (StaticTexture2D)ImportFromUnityUtils.LookupRefID(bumpMapTexRefID);
                    mat.NormalMap = tex;
                }
                if (hasDetailAlbedoMapTex)
                {
                    //mat.DetailAlbedoTexture.Value = detailAlbedoMapTexRefID.id;
                    //mat.DetailTextureOffset.Value = detailAlbedoMapTexOffset;
                    //mat.DetailTextureScale.Value = detailAlbedoMapTexScale;
                }

                if (hasDetailNormalMapTex && !hasBumpMapTex)
                {
                    //StaticTexture2D tex = (StaticTexture2D)ImportFromUnityUtils.LookupRefID(detailNormalMapTexRefID);
                    //mat.NormalMap = tex;
                    //mat.DetailNormalScale.Value = detailNormalMapTexScale;
                }
            }

            // todo:
            // _DetailMask_ST,_DetailMask_TexelSize,_DetailMask_HDR,
            // _ParallaxMap_ST,_ParallaxMap_TexelSize,_ParallaxMap_HDR,
            // return refid of StaticMesh component
            outputBytes.outputBytes = SerializationUtils.EncodeObject(matRefId);
        }

        /// <summary>
        /// Takes static mesh data and makes a StaticMesh asset
        /// </summary>
        /// <param name="staticMeshBytes"></param>
        /// <returns>bytes representing RefID_U2Res that contains the static mesh asset component</returns>
        public static async Task<byte[]> ImportToMaterialFunc(byte[] materialBytes)
        {
            OutputBytesHolder outputBytesHolder = new OutputBytesHolder();
            await ImportFromUnityUtils.RunOnWorldThread(() => ImportToMaterialHelper(materialBytes, outputBytesHolder));
            return outputBytesHolder.outputBytes;
        }
    }
}
