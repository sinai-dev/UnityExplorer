#if CPP
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityExplorer.Runtime
{
    public class Il2CppHelper : UERuntimeHelper
    {
        public override void SetupEvents()
        {
            try
            {
                Application.add_logMessageReceived(new Action<string, string, LogType>(Application_logMessageReceived));
            }
            catch (Exception ex)
            {
                ExplorerCore.LogWarning("Exception setting up Unity log listener, make sure Unity libraries have been unstripped!");
                ExplorerCore.Log(ex);
            }
        }

        private void Application_logMessageReceived(string condition, string stackTrace, LogType type)
        {
            ExplorerCore.LogUnity(condition, type);
        }

        public override string[] DefaultReflectionBlacklist => defaultIl2CppBlacklist.ToArray();

        // These methods currently cause a crash in most il2cpp games,
        // even from doing "GetParameters()" on the MemberInfo.
        // Blacklisting until the issue is fixed in Unhollower.
        public static HashSet<string> defaultIl2CppBlacklist = new()
        {
            // These were deprecated a long time ago, still show up in some IL2CPP games for some reason
            "UnityEngine.MonoBehaviour.allowPrefabModeInPlayMode",
            "UnityEngine.MonoBehaviour.runInEditMode",
            "UnityEngine.Component.animation",
            "UnityEngine.Component.audio",
            "UnityEngine.Component.camera",
            "UnityEngine.Component.collider",
            "UnityEngine.Component.collider2D",
            "UnityEngine.Component.constantForce",
            "UnityEngine.Component.hingeJoint",
            "UnityEngine.Component.light",
            "UnityEngine.Component.networkView",
            "UnityEngine.Component.particleSystem",
            "UnityEngine.Component.renderer",
            "UnityEngine.Component.rigidbody",
            "UnityEngine.Component.rigidbody2D",
            "UnityEngine.Light.flare",
            // These can cause a crash in IL2CPP
            "Il2CppSystem.Type.DeclaringMethod",
            "Il2CppSystem.RuntimeType.DeclaringMethod",
            "Unity.Jobs.LowLevel.Unsafe.JobsUtility.CreateJobReflectionData",
            "Unity.Profiling.ProfilerRecorder.CopyTo",
            "Unity.Profiling.ProfilerRecorder.StartNew",
            "UnityEngine.Analytics.Analytics.RegisterEvent",
            "UnityEngine.Analytics.Analytics.SendEvent",
            "UnityEngine.Analytics.ContinuousEvent+ConfigureEventDelegate.Invoke",
            "UnityEngine.Analytics.ContinuousEvent.ConfigureEvent",
            "UnityEngine.Animations.AnimationLayerMixerPlayable.Create",
            "UnityEngine.Animations.AnimationLayerMixerPlayable.CreateHandle",
            "UnityEngine.Animations.AnimationMixerPlayable.Create",
            "UnityEngine.Animations.AnimationMixerPlayable.CreateHandle",
            "UnityEngine.AssetBundle.RecompressAssetBundleAsync",
            "UnityEngine.Audio.AudioMixerPlayable.Create",
            "UnityEngine.BoxcastCommand.ScheduleBatch",
            "UnityEngine.Camera.CalculateProjectionMatrixFromPhysicalProperties",
            "UnityEngine.Canvas.renderingDisplaySize",
            "UnityEngine.CapsulecastCommand.ScheduleBatch",
            "UnityEngine.Collider2D.Cast",
            "UnityEngine.Collider2D.Raycast",
            "UnityEngine.ComputeBuffer+BeginBufferWriteDelegate.Invoke",
            "UnityEngine.ComputeBuffer+EndBufferWriteDelegate.Invoke",
            "UnityEngine.ComputeBuffer.BeginBufferWrite",
            "UnityEngine.ComputeBuffer.EndBufferWrite",
            "UnityEngine.Cubemap+SetPixelDataImplArrayDelegate.Invoke",
            "UnityEngine.Cubemap+SetPixelDataImplDelegate.Invoke",
            "UnityEngine.Cubemap.SetPixelDataImpl",
            "UnityEngine.Cubemap.SetPixelDataImplArray",
            "UnityEngine.CubemapArray+SetPixelDataImplArrayDelegate.Invoke",
            "UnityEngine.CubemapArray+SetPixelDataImplDelegate.Invoke",
            "UnityEngine.CubemapArray.SetPixelDataImpl",
            "UnityEngine.CubemapArray.SetPixelDataImplArray",
            "UnityEngine.Experimental.Playables.MaterialEffectPlayable.Create",
            "UnityEngine.Experimental.Rendering.RayTracingAccelerationStructure+AddInstanceDelegate.Invoke",
            "UnityEngine.Experimental.Rendering.RayTracingAccelerationStructure+AddInstance_Procedural_InjectedDelegate.Invoke",
            "UnityEngine.Experimental.Rendering.RayTracingAccelerationStructure.AddInstance",
            "UnityEngine.Experimental.Rendering.RayTracingAccelerationStructure.AddInstance_Procedural",
            "UnityEngine.Experimental.Rendering.RayTracingAccelerationStructure.AddInstance_Procedural_Injected",
            "UnityEngine.Experimental.Rendering.RayTracingShader+DispatchDelegate.Invoke",
            "UnityEngine.Experimental.Rendering.RayTracingShader.Dispatch",
            "UnityEngine.Experimental.Rendering.RenderPassAttachment.Clear",
            "UnityEngine.GUI.DoButtonGrid",
            "UnityEngine.GUI.Slider",
            "UnityEngine.GUI.Toolbar",
            "UnityEngine.Graphics.DrawMeshInstancedIndirect",
            "UnityEngine.Graphics.DrawMeshInstancedProcedural",
            "UnityEngine.Graphics.DrawProcedural",
            "UnityEngine.Graphics.DrawProceduralIndirect",
            "UnityEngine.Graphics.DrawProceduralIndirectNow",
            "UnityEngine.Graphics.DrawProceduralNow",
            "UnityEngine.LineRenderer+BakeMeshDelegate.Invoke",
            "UnityEngine.LineRenderer.BakeMesh",
            "UnityEngine.Mesh.GetIndices",
            "UnityEngine.Mesh.GetTriangles",
            "UnityEngine.Mesh.SetIndices",
            "UnityEngine.Mesh.SetTriangles",
            "UnityEngine.Physics2D.BoxCast",
            "UnityEngine.Physics2D.CapsuleCast",
            "UnityEngine.Physics2D.CircleCast",
            "UnityEngine.PhysicsScene.BoxCast",
            "UnityEngine.PhysicsScene.CapsuleCast",
            "UnityEngine.PhysicsScene.OverlapBox",
            "UnityEngine.PhysicsScene.OverlapCapsule",
            "UnityEngine.PhysicsScene.SphereCast",
            "UnityEngine.PhysicsScene2D.BoxCast",
            "UnityEngine.PhysicsScene2D.CapsuleCast",
            "UnityEngine.PhysicsScene2D.CircleCast",
            "UnityEngine.PhysicsScene2D.GetRayIntersection",
            "UnityEngine.PhysicsScene2D.Linecast",
            "UnityEngine.PhysicsScene2D.OverlapArea",
            "UnityEngine.PhysicsScene2D.OverlapBox",
            "UnityEngine.PhysicsScene2D.OverlapCapsule",
            "UnityEngine.PhysicsScene2D.OverlapCircle",
            "UnityEngine.PhysicsScene2D.OverlapCollider",
            "UnityEngine.PhysicsScene2D.OverlapPoint",
            "UnityEngine.PhysicsScene2D.Raycast",
            "UnityEngine.Playables.Playable.Create",
            "UnityEngine.Profiling.CustomSampler.Create",
            "UnityEngine.RaycastCommand.ScheduleBatch",
            "UnityEngine.RemoteConfigSettings+QueueConfigDelegate.Invoke",
            "UnityEngine.RemoteConfigSettings.QueueConfig",
            "UnityEngine.RenderTexture.GetTemporaryImpl",
            "UnityEngine.Rendering.AsyncGPUReadback.Request",
            "UnityEngine.Rendering.AttachmentDescriptor.ConfigureClear",
            "UnityEngine.Rendering.BatchRendererGroup+AddBatch_InjectedDelegate.Invoke",
            "UnityEngine.Rendering.BatchRendererGroup.AddBatch",
            "UnityEngine.Rendering.BatchRendererGroup.AddBatch_Injected",
            "UnityEngine.Rendering.CommandBuffer+Internal_DispatchRaysDelegate.Invoke",
            "UnityEngine.Rendering.CommandBuffer.DispatchRays",
            "UnityEngine.Rendering.CommandBuffer.DrawMeshInstancedProcedural",
            "UnityEngine.Rendering.CommandBuffer.Internal_DispatchRays",
            "UnityEngine.Rendering.CommandBuffer.ResolveAntiAliasedSurface",
            "UnityEngine.Rendering.ScriptableRenderContext.BeginRenderPass",
            "UnityEngine.Rendering.ScriptableRenderContext.BeginScopedRenderPass",
            "UnityEngine.Rendering.ScriptableRenderContext.BeginScopedSubPass",
            "UnityEngine.Rendering.ScriptableRenderContext.BeginSubPass",
            "UnityEngine.Rendering.ScriptableRenderContext.SetupCameraProperties",
            "UnityEngine.Rigidbody2D.Cast",
            "UnityEngine.Scripting.GarbageCollector+CollectIncrementalDelegate.Invoke",
            "UnityEngine.Scripting.GarbageCollector.CollectIncremental",
            "UnityEngine.SpherecastCommand.ScheduleBatch",
            "UnityEngine.Texture.GetPixelDataSize",
            "UnityEngine.Texture.GetPixelDataOffset",
            "UnityEngine.Texture.GetPixelDataOffset",
            "UnityEngine.Texture2D+SetPixelDataImplArrayDelegate.Invoke",
            "UnityEngine.Texture2D+SetPixelDataImplDelegate.Invoke",
            "UnityEngine.Texture2D.SetPixelDataImpl",
            "UnityEngine.Texture2D.SetPixelDataImplArray",
            "UnityEngine.Texture2DArray+SetPixelDataImplArrayDelegate.Invoke",
            "UnityEngine.Texture2DArray+SetPixelDataImplDelegate.Invoke",
            "UnityEngine.Texture2DArray.SetPixelDataImpl",
            "UnityEngine.Texture2DArray.SetPixelDataImplArray",
            "UnityEngine.Texture3D+SetPixelDataImplArrayDelegate.Invoke",
            "UnityEngine.Texture3D+SetPixelDataImplDelegate.Invoke",
            "UnityEngine.Texture3D.SetPixelDataImpl",
            "UnityEngine.Texture3D.SetPixelDataImplArray",
            "UnityEngine.TrailRenderer+BakeMeshDelegate.Invoke",
            "UnityEngine.TrailRenderer.BakeMesh",
            "UnityEngine.WWW.LoadFromCacheOrDownload",
            "UnityEngine.XR.InputDevice.SendHapticImpulse",
        };
    }
}

#endif