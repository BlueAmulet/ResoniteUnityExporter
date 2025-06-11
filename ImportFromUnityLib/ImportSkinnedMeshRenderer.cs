﻿extern alias Froox;

using Froox::FrooxEngine;
using ResoniteUnityExporterShared;
using System.Collections.Generic;
using MemoryMappedFileIPC;
using System.Threading.Tasks;

namespace ImportFromUnityLib
{
    public class ImportSkinnedMeshRenderer
    {
        public static void ImportSkinnedMeshRendererHelper(byte[] skinnedMeshRendererBytes, OutputBytesHolder outputBytes)
        {
            // load data from bytes
            SkinnedMeshRenderer_U2Res skinnedMeshRendererData = SerializationUtils.DecodeObject<SkinnedMeshRenderer_U2Res>(skinnedMeshRendererBytes);
            // load texture into localdb to get a url
            World focusedWorld = ImportFromUnityLib.CurrentEngine.WorldManager.FocusedWorld;
            Slot targetSlot = (Slot)ImportFromUnityUtils.LookupRefID(skinnedMeshRendererData.targetSlot);
            ImportFromUnityLib.DebugLog("Importing skinned mesh renderer on " + targetSlot.Name);
            SkinnedMeshRenderer renderer = targetSlot.AttachComponent<SkinnedMeshRenderer>();
            renderer.Mesh.Value = skinnedMeshRendererData.staticMeshAsset.id;
            Slot assetsSlot = ((StaticMesh)ImportFromUnityUtils.LookupRefID(skinnedMeshRendererData.staticMeshAsset)).Slot;
            // assign materials
            renderer.Materials.Clear();
            foreach (RefID_U2Res material in skinnedMeshRendererData.materials)
            {
                IAssetProvider<Material> frooxMat = (IAssetProvider<Material>)ImportFromUnityUtils.LookupRefID(material);
                renderer.Materials.Add(frooxMat);
            }
            // assign bones and rig
            // todo: resonite forces these to be unique by name
            // but that constraint doesn't seem necessary as far as I can tell so I did everything
            // so that we can have multiple bones named same thing
            // will that cause issues? idk
            // can't use foreach or we end up with multiple null bones using same index (for reasons I don't quite understand)
            Slot tmpBone = null;
            for(int boneI = 0; boneI < skinnedMeshRendererData.bones.Length; boneI++)
            {
                RefID_U2Res boneRefID = skinnedMeshRendererData.bones[boneI];
                // support null bones by initializing a new one at location of skinned mesh renderer
                // it's not ideal but as good as we can do
                // since just ignoring them would cause errors
                // and just removing them would have the indices be offset
                if (boneRefID.id == 0)
                {
                    if (tmpBone == null)
                    {
                        string boneName = SkinnedMeshRendererConstants.tempBonePrefix + "_" + targetSlot.Name;
                        tmpBone = renderer.Slot.AddSlot(boneName);
                    }
                    renderer.Bones.Add().Value = tmpBone.ReferenceID;
                }
                else
                {
                    renderer.Bones.Add().Value = boneRefID.id;
                }
            }
            renderer.BoundsComputeMethod.Value = SkinnedBounds.Static;

            // initialize blend shape weight list
            // this would happen by default but waits until meshx is loaded which takes too long
            // so we do it first
            while (renderer.BlendShapeWeights.Count > skinnedMeshRendererData.blendShapeWeights.Length)
            {
                renderer.BlendShapeWeights.RemoveAt(renderer.BlendShapeWeights.Count - 1);
            }

            while (renderer.BlendShapeWeights.Count < skinnedMeshRendererData.blendShapeWeights.Length)
            {
                renderer.BlendShapeWeights.Add(0f);
            }
            // copy over blend shape data
            for (int blendShapeI = 0; blendShapeI < skinnedMeshRendererData.blendShapeWeights.Length; blendShapeI++)
            {
                renderer.BlendShapeWeights[blendShapeI] = skinnedMeshRendererData.blendShapeWeights[blendShapeI];
            }
            
            // return refid of SkinnedMeshRenderer component
            RefID_U2Res result = new RefID_U2Res()
            {
                id = (ulong)renderer.ReferenceID
            };
            
            outputBytes.outputBytes = SerializationUtils.EncodeObject(result);
        }

        public static async Task<byte[]> ImportSkinnedMeshRendererFunc(byte[] skinnedMeshRendererData)
        {
            OutputBytesHolder outputBytesHolder = new OutputBytesHolder();
            await ImportFromUnityUtils.RunOnWorldThread(() => ImportSkinnedMeshRendererHelper(skinnedMeshRendererData, outputBytesHolder));
            return outputBytesHolder.outputBytes;
        }
    }
}
