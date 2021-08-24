#if CPP
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnhollowerBaseLib;
using UnhollowerRuntimeLib;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Collections;
using System.IO;
using System.Diagnostics.CodeAnalysis;
using UnityExplorer.Core;
using CppType = Il2CppSystem.Type;
using BF = System.Reflection.BindingFlags;
using UnhollowerBaseLib.Attributes;
using UnityEngine;

namespace UnityExplorer
{
    public class Il2CppReflection : ReflectionUtility
    {
        protected override void Initialize()
        {
            base.Initialize();

            float start = Time.realtimeSinceStartup;
            TryLoadGameModules();
            ExplorerCore.Log($"Loaded Unhollowed modules in {Time.realtimeSinceStartup - start} seconds");

            start = Time.realtimeSinceStartup;
            BuildDeobfuscationCache();
            OnTypeLoaded += TryCacheDeobfuscatedType;
            ExplorerCore.Log($"Setup IL2CPP reflection in {Time.realtimeSinceStartup - start} seconds, " +
                $"deobfuscated types count: {DeobfuscatedTypes.Count}");
        }

#region IL2CPP Extern and pointers

        // Extern C++ methods 
        [DllImport("GameAssembly", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern bool il2cpp_class_is_assignable_from(IntPtr klass, IntPtr oklass);

        [DllImport("GameAssembly", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern IntPtr il2cpp_object_get_class(IntPtr obj);

        public static bool Il2CppTypeNotNull(Type type) => Il2CppTypeNotNull(type, out _);

        public static bool Il2CppTypeNotNull(Type type, out IntPtr il2cppPtr)
        {
            if (!cppClassPointers.TryGetValue(type.AssemblyQualifiedName, out il2cppPtr))
            {
                il2cppPtr = (IntPtr)typeof(Il2CppClassPointerStore<>)
                    .MakeGenericType(new Type[] { type })
                    .GetField("NativeClassPtr", BF.Public | BF.Static)
                    .GetValue(null);

                cppClassPointers.Add(type.AssemblyQualifiedName, il2cppPtr);
            }

            return il2cppPtr != IntPtr.Zero;
        }

#endregion


#region Deobfuscation cache

        private static readonly Dictionary<string, Type> DeobfuscatedTypes = new Dictionary<string, Type>();
        private static readonly Dictionary<string, string> reverseDeobCache = new Dictionary<string, string>();

        private static void BuildDeobfuscationCache()
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var type in asm.TryGetTypes())
                    TryCacheDeobfuscatedType(type);
            }
        }

        private static void TryCacheDeobfuscatedType(Type type)
        {
            try
            {
                if (!type.CustomAttributes.Any())
                    return;

                foreach (var att in type.CustomAttributes)
                {
                    // Thanks to Slaynash for this

                    if (att.AttributeType == typeof(ObfuscatedNameAttribute))
                    {
                        string obfuscatedName = att.ConstructorArguments[0].Value.ToString();

                        DeobfuscatedTypes.Add(obfuscatedName, type);
                        reverseDeobCache.Add(type.FullName, obfuscatedName);
                    }
                }
            }
            catch { }
        }

        internal override string Internal_ProcessTypeInString(string theString, Type type)
        {
            if (reverseDeobCache.TryGetValue(type.FullName, out string obName))
                return theString.Replace(obName, type.FullName);

            return theString;
        }

#endregion


        // Get type by name

        internal override Type Internal_GetTypeByName(string fullName)
        {
            if (DeobfuscatedTypes.TryGetValue(fullName, out Type deob))
                return deob;

            return base.Internal_GetTypeByName(fullName);
        }

#region Get actual type

        internal override Type Internal_GetActualType(object obj)
        {
            if (obj == null)
                return null;

            var type = obj.GetType();
            try
            {
                if (type.IsGenericType)
                    return type;

                if (IsString(obj))
                    return typeof(string);

                if (IsIl2CppPrimitive(type))
                    return il2cppPrimitivesToMono[type.FullName];

                if (obj is Il2CppSystem.Object cppObject)
                {
                    var cppType = cppObject.GetIl2CppType();

                    // check if type is injected
                    IntPtr classPtr = il2cpp_object_get_class(cppObject.Pointer);
                    if (RuntimeSpecificsStore.IsInjected(classPtr))
                    {
                        // Note: This will fail on injected subclasses.
                        // - {Namespace}.{Class}.{Subclass} would be {Namespace}.{Subclass} when injected.
                        // Not sure on solution yet.
                        return GetTypeByName(cppType.FullName) ?? type;
                    }

                    if (AllTypes.TryGetValue(cppType.FullName, out Type primitive) && primitive.IsPrimitive)
                        return primitive;

                    return GetUnhollowedType(cppType) ?? type;
                }
            }
            catch (Exception ex)
            {
                ExplorerCore.LogWarning("Exception in IL2CPP GetActualType: " + ex);
            }

            return type;
        }

        public static Type GetUnhollowedType(CppType cppType)
        {
            var fullname = cppType.FullName;

            if (DeobfuscatedTypes.TryGetValue(fullname, out Type deob))
                return deob;

            if (fullname.StartsWith("System."))
                fullname = $"Il2Cpp{fullname}";

            if (!AllTypes.TryGetValue(fullname, out Type monoType))
                ExplorerCore.LogWarning($"Failed to get type by name '{fullname}'!");
            return monoType;
        }

#endregion


#region Casting

        private static readonly Dictionary<string, IntPtr> cppClassPointers = new Dictionary<string, IntPtr>();

        internal override object Internal_TryCast(object obj, Type castTo)
        {
            if (obj == null)
                return null;

            var type = obj.GetType();

            if (type == castTo)
                return obj;

            // from structs
            if (type.IsValueType)
            {
                // from il2cpp primitive to system primitive
                if (IsIl2CppPrimitive(type) && castTo.IsPrimitive)
                {
                    return MakeMonoPrimitive(obj);
                }
                // from system primitive to il2cpp primitive
                else if (IsIl2CppPrimitive(castTo))
                {
                    return MakeIl2CppPrimitive(castTo, obj);
                }
                // from other structs to il2cpp object
                else if (typeof(Il2CppSystem.Object).IsAssignableFrom(castTo))
                {
                    return BoxIl2CppObject(obj).TryCast(castTo);
                }
                else
                    return obj;
            }

            // from string to il2cpp.Object / il2cpp.String
            if (obj is string && typeof(Il2CppSystem.Object).IsAssignableFrom(castTo))
            {
                return BoxStringToType(obj, castTo);
            }

            // from il2cpp objects...

            if (!(obj is Il2CppObjectBase cppObj))
                return obj;

            // from Il2CppSystem.Object to a struct
            if (castTo.IsValueType)
                return UnboxCppObject(cppObj, castTo);
            // or to system string
            else if (castTo == typeof(string))
                return UnboxString(obj);

            if (!Il2CppTypeNotNull(castTo, out IntPtr castToPtr))
                return obj;

            // Casting from il2cpp object to il2cpp object...

            IntPtr castFromPtr = il2cpp_object_get_class(cppObj.Pointer);

            if (!il2cpp_class_is_assignable_from(castToPtr, castFromPtr))
                return null;

            if (RuntimeSpecificsStore.IsInjected(castToPtr))
            {
                var injectedObj = UnhollowerBaseLib.Runtime.ClassInjectorBase.GetMonoObjectFromIl2CppPointer(cppObj.Pointer);
                return injectedObj ?? obj;
            }

            try
            {
                return Activator.CreateInstance(castTo, cppObj.Pointer);
            }
            catch
            {
                return obj;
            }
        }

        //private static bool IsAssignableFrom(Type thisType, Type fromType)
        //{
        //    if (!Il2CppTypeNotNull(fromType, out IntPtr fromTypePtr)
        //        || !Il2CppTypeNotNull(thisType, out IntPtr thisTypePtr))
        //    {
        //        // one or both of the types are not Il2Cpp types, use normal check
        //        return thisType.IsAssignableFrom(fromType);
        //    }
        //
        //    return il2cpp_class_is_assignable_from(thisTypePtr, fromTypePtr);
        //}

#endregion


#region Boxing and unboxing ValueTypes

        // cached il2cpp unbox methods
        internal static readonly Dictionary<string, MethodInfo> unboxMethods = new Dictionary<string, MethodInfo>();

        // Unbox an il2cpp object to a struct or System primitive.
        public object UnboxCppObject(Il2CppObjectBase cppObj, Type toType)
        {
            if (!toType.IsValueType)
                return null;

            try
            {
                if (toType.IsEnum)
                {
                    // Check for nullable enums
                    var type = cppObj.GetType();
                    if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Il2CppSystem.Nullable<>))
                    {
                        var nullable = cppObj.TryCast(type);
                        var nullableHasValueProperty = type.GetProperty("HasValue");
                        if ((bool)nullableHasValueProperty.GetValue(nullable, null))
                        {
                            // nullable has a value.
                            var nullableValueProperty = type.GetProperty("Value");
                            return Enum.Parse(toType, nullableValueProperty.GetValue(nullable, null).ToString());
                        }
                        // nullable and no current value.
                        return cppObj;
                    }

                    return Enum.Parse(toType, cppObj.ToString());
                }

                // Not enum, unbox with Il2CppObjectBase.Unbox

                var name = toType.AssemblyQualifiedName;

                if (!unboxMethods.ContainsKey(name))
                {
                    unboxMethods.Add(name, typeof(Il2CppObjectBase)
                                                .GetMethod("Unbox")
                                                .MakeGenericMethod(toType));
                }

                return unboxMethods[name].Invoke(cppObj, ArgumentUtility.EmptyArgs);
            }
            catch (Exception ex)
            {
                ExplorerCore.LogWarning("Exception Unboxing Il2Cpp object to struct: " + ex);
                return null;
            }
        }

        private static Il2CppSystem.Object BoxIl2CppObject(object cppStruct, Type structType)
        {
            return GetMethodInfo(structType, "BoxIl2CppObject", ArgumentUtility.EmptyTypes)
                   .Invoke(cppStruct, ArgumentUtility.EmptyArgs)
                   as Il2CppSystem.Object;
        }

        public Il2CppSystem.Object BoxIl2CppObject(object value)
        {
            if (value == null)
                return null;

            try
            {
                var type = value.GetType();
                if (!type.IsValueType)
                    return null;

                if (type.IsEnum)
                    return Il2CppSystem.Enum.Parse(Il2CppType.From(type), value.ToString());

                if (type.IsPrimitive && AllTypes.TryGetValue($"Il2Cpp{type.FullName}", out Type cppType))
                    return BoxIl2CppObject(MakeIl2CppPrimitive(cppType, value), cppType);

                return BoxIl2CppObject(value, type);
            }
            catch (Exception ex)
            {
                ExplorerCore.LogWarning("Exception in BoxIl2CppObject: " + ex);
                return null;
            }
        }

        // Helpers for Il2Cpp primitive <-> Mono

        internal static readonly Dictionary<string, Type> il2cppPrimitivesToMono = new Dictionary<string, Type>
        {
            { "Il2CppSystem.Boolean", typeof(bool) },
            { "Il2CppSystem.Byte",    typeof(byte) },
            { "Il2CppSystem.SByte",   typeof(sbyte) },
            { "Il2CppSystem.Char",    typeof(char) },
            { "Il2CppSystem.Double",  typeof(double) },
            { "Il2CppSystem.Single",  typeof(float) },
            { "Il2CppSystem.Int32",   typeof(int) },
            { "Il2CppSystem.UInt32",  typeof(uint) },
            { "Il2CppSystem.Int64",   typeof(long) },
            { "Il2CppSystem.UInt64",  typeof(ulong) },
            { "Il2CppSystem.Int16",   typeof(short) },
            { "Il2CppSystem.UInt16",  typeof(ushort) },
            { "Il2CppSystem.IntPtr",  typeof(IntPtr) },
            { "Il2CppSystem.UIntPtr", typeof(UIntPtr) }
        };

        public static bool IsIl2CppPrimitive(object obj) => IsIl2CppPrimitive(obj.GetType());

        public static bool IsIl2CppPrimitive(Type type) => il2cppPrimitivesToMono.ContainsKey(type.FullName);

        public object MakeMonoPrimitive(object cppPrimitive)
        {
            return GetFieldInfo(cppPrimitive.GetType(), "m_value").GetValue(cppPrimitive);
        }

        public object MakeIl2CppPrimitive(Type cppType, object monoValue)
        {
            var cppStruct = Activator.CreateInstance(cppType);
            GetFieldInfo(cppType, "m_value").SetValue(cppStruct, monoValue);
            return cppStruct;
        }

#endregion


#region String boxing/unboxing

        private const string IL2CPP_STRING_FULLNAME = "Il2CppSystem.String";
        private const string STRING_FULLNAME = "System.String";

        public bool IsString(object obj)
        {
            if (obj is string || obj is Il2CppSystem.String)
                return true;
        
            if (obj is Il2CppSystem.Object cppObj)
            {
                var type = cppObj.GetIl2CppType();
                return type.FullName == IL2CPP_STRING_FULLNAME || type.FullName == STRING_FULLNAME;
            }

            return false;
        }

        public object BoxStringToType(object value, Type castTo)
        {
            if (castTo == typeof(Il2CppSystem.String))
                return (Il2CppSystem.String)(value as string);
            else
                return (Il2CppSystem.Object)(value as string);
        }

        public string UnboxString(object value)
        {
            if (value is string s)
                return s;

            s = null;
            if (value is Il2CppSystem.Object cppObject)
                s = cppObject.ToString();
            else if (value is Il2CppSystem.String cppString)
                s = cppString;

            return s;
        }

#endregion


#region Singleton finder

        internal override void Internal_FindSingleton(string[] possibleNames, Type type, BF flags, List<object> instances)
        {
            PropertyInfo pi;
            foreach (var name in possibleNames)
            {
                pi = type.GetProperty(name, flags);
                if (pi != null)
                {
                    var instance = pi.GetValue(null, null);
                    if (instance != null)
                    {
                        instances.Add(instance);
                        return;
                    }
                }
            }

            base.Internal_FindSingleton(possibleNames, type, flags, instances);
        }

#endregion


#region Force-loading game modules

        // Helper for IL2CPP to try to make sure the Unhollowed game assemblies are actually loaded.

        // Force loading all il2cpp modules

        internal void TryLoadGameModules()
        {
            var dir = ExplorerCore.Loader.UnhollowedModulesFolder;
            if (Directory.Exists(dir))
            {
                foreach (var filePath in Directory.GetFiles(dir, "*.dll"))
                    DoLoadModule(filePath);
            }
            else
                ExplorerCore.LogWarning($"Expected Unhollowed folder path does not exist: '{dir}'. " +
                    $"If you are using the standalone release, you can specify the Unhollowed modules path when you call CreateInstance().");
        }

        internal bool DoLoadModule(string fullPath)
        {
            if (string.IsNullOrEmpty(fullPath) || !File.Exists(fullPath))
                return false;

            try
            {
                Assembly.LoadFile(fullPath);
                return true;
            }
            catch //(Exception e)
            {
                //ExplorerCore.LogWarning($"Failed loading module '{Path.GetFileName(fullPath)}'! {e.ReflectionExToString()}");
                return false;
            }
        }

#endregion


#region Il2cpp reflection blacklist

        public override string[] DefaultReflectionBlacklist => defaultIl2CppBlacklist.ToArray();

        // These methods currently cause a crash in most il2cpp games,
        // even from doing "GetParameters()" on the MemberInfo.
        // Blacklisting until the issue is fixed in Unhollower.
        public static HashSet<string> defaultIl2CppBlacklist = new HashSet<string>
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

#endregion


#region IL2CPP IEnumerable and IDictionary

        protected override bool Internal_TryGetEntryType(Type enumerableType, out Type type)
        {
            // Check for system types (not unhollowed)
            if (base.Internal_TryGetEntryType(enumerableType, out type))
                return true;

            // Type is either an IL2CPP enumerable, or its not generic.

            if (type.IsGenericType)
            {
                // Temporary naive solution until IL2CPP interface support improves.
                // This will work fine for most cases, but there are edge cases which would not work.
                type = type.GetGenericArguments()[0];
                return true;
            }

            // Unable to determine entry type
            type = typeof(object);
            return false;
        }

        protected override bool Internal_TryGetEntryTypes(Type type, out Type keys, out Type values)
        {
            if (base.Internal_TryGetEntryTypes(type, out keys, out values))
                return true;

            // Type is either an IL2CPP dictionary, or its not generic.
            if (type.IsGenericType)
            {
                // Naive solution until IL2CPP interfaces improve.
                var args = type.GetGenericArguments();
                if (args.Length == 2)
                {
                    keys = args[0];
                    values = args[1];
                    return true;
                }
            }

            keys = typeof(object);
            values = typeof(object);
            return false;
        }

        // Temp fix until Unhollower interface support improves

        internal static readonly Dictionary<string, MethodInfo> getEnumeratorMethods = new Dictionary<string, MethodInfo>();
        internal static readonly Dictionary<string, EnumeratorInfo> enumeratorInfos = new Dictionary<string, EnumeratorInfo>();
        internal static readonly HashSet<string> notSupportedTypes = new HashSet<string>();

        // IEnumerables

        internal static IntPtr cppIEnumerablePointer;

        protected override bool Internal_IsEnumerable(Type type)
        {
            if (base.Internal_IsEnumerable(type))
                return true;

            try
            {
                if (cppIEnumerablePointer == IntPtr.Zero)
                    Il2CppTypeNotNull(typeof(Il2CppSystem.Collections.IEnumerable), out cppIEnumerablePointer);

                if (cppIEnumerablePointer != IntPtr.Zero
                    && Il2CppTypeNotNull(type, out IntPtr assignFromPtr)
                    && il2cpp_class_is_assignable_from(cppIEnumerablePointer, assignFromPtr))
                {
                    return true;
                }
            }
            catch { }

            return false;
        }

        internal class EnumeratorInfo
        {
            internal MethodInfo moveNext;
            internal PropertyInfo current;
        }

        protected override bool Internal_TryGetEnumerator(object list, out IEnumerator enumerator)
        {
            if (list is IEnumerable)
                return base.Internal_TryGetEnumerator(list, out enumerator);

            try
            {
                PrepareCppEnumerator(list, out object cppEnumerator, out EnumeratorInfo info);
                enumerator = EnumerateCppList(info, cppEnumerator);
                return true;
            }
            catch //(Exception ex)
            {
                //ExplorerCore.LogWarning($"Exception enumerating IEnumerable: {ex.ReflectionExToString()}");
                enumerator = null;
                return false;
            }
        }

        private static void PrepareCppEnumerator(object list, out object cppEnumerator, out EnumeratorInfo info)
        {
            info = null;
            cppEnumerator = null;
            if (list == null)
                throw new ArgumentNullException("list");

            // Some ugly reflection to use the il2cpp interface for the instance type

            var type = list.GetActualType();
            var key = type.AssemblyQualifiedName;

            if (!getEnumeratorMethods.ContainsKey(key))
            {
                var method = type.GetMethod("GetEnumerator")
                             ?? type.GetMethod("System_Collections_IEnumerable_GetEnumerator", FLAGS);
                getEnumeratorMethods.Add(key, method);

                // ensure the enumerator type is supported
                try
                {
                    var test = getEnumeratorMethods[key].Invoke(list, null);
                    test.GetActualType().GetMethod("MoveNext").Invoke(test, null);
                }
                catch (Exception ex)
                {
                    ExplorerCore.Log($"IEnumerable failed to enumerate: {ex}");
                    notSupportedTypes.Add(key);
                }
            }

            if (notSupportedTypes.Contains(key))
                throw new NotSupportedException($"The IEnumerable type '{type.FullName}' does not support MoveNext.");

            cppEnumerator = getEnumeratorMethods[key].Invoke(list, null);
            var enumeratorType = cppEnumerator.GetActualType();

            var enumInfoKey = enumeratorType.AssemblyQualifiedName;

            if (!enumeratorInfos.ContainsKey(enumInfoKey))
            {
                enumeratorInfos.Add(enumInfoKey, new EnumeratorInfo
                {
                    current = enumeratorType.GetProperty("Current"),
                    moveNext = enumeratorType.GetMethod("MoveNext"),
                });
            }

            info = enumeratorInfos[enumInfoKey];
        }

        internal static IEnumerator EnumerateCppList(EnumeratorInfo info, object enumerator)
        {
            // Yield and return the actual entries
            while ((bool)info.moveNext.Invoke(enumerator, null))
                yield return info.current.GetValue(enumerator);
        }

        // IDictionary

        internal static IntPtr cppIDictionaryPointer;

        protected override bool Internal_IsDictionary(Type type)
        {
            if (base.Internal_IsDictionary(type))
                return true;
        
            try
            {
                if (cppIDictionaryPointer == IntPtr.Zero)
                    if (!Il2CppTypeNotNull(typeof(Il2CppSystem.Collections.IDictionary), out cppIDictionaryPointer))
                        return false;

                if (Il2CppTypeNotNull(type, out IntPtr classPtr)
                    && il2cpp_class_is_assignable_from(cppIDictionaryPointer, classPtr))
                    return true;
            }
            catch { }
        
            return false;
        }

        protected override bool Internal_TryGetDictEnumerator(object dictionary, out IEnumerator<DictionaryEntry> dictEnumerator)
        {
            if (dictionary is IDictionary)
                return base.Internal_TryGetDictEnumerator(dictionary, out dictEnumerator);

            try
            {
                var type = dictionary.GetActualType();

                if (typeof(Il2CppSystem.Collections.Hashtable).IsAssignableFrom(type))
                {
                    dictEnumerator = EnumerateCppHashTable(dictionary.TryCast<Il2CppSystem.Collections.Hashtable>());
                    return true;
                }

                var keys = type.GetProperty("Keys").GetValue(dictionary, null);

                var keyCollType = keys.GetActualType();
                var cacheKey = keyCollType.AssemblyQualifiedName;
                if (!getEnumeratorMethods.ContainsKey(cacheKey))
                {
                    var method = keyCollType.GetMethod("GetEnumerator")
                                 ?? keyCollType.GetMethod("System_Collections_IDictionary_GetEnumerator", FLAGS);
                    getEnumeratorMethods.Add(cacheKey, method);

                    // test support
                    try
                    {
                        var test = getEnumeratorMethods[cacheKey].Invoke(keys, null);
                        test.GetActualType().GetMethod("MoveNext").Invoke(test, null);
                    }
                    catch (Exception ex)
                    {
                        ExplorerCore.Log($"IDictionary failed to enumerate: {ex}");
                        notSupportedTypes.Add(cacheKey);
                    }
                }

                if (notSupportedTypes.Contains(cacheKey))
                    throw new Exception($"The IDictionary type '{type.FullName}' does not support MoveNext.");

                var keyEnumerator = getEnumeratorMethods[cacheKey].Invoke(keys, null);
                var keyInfo = new EnumeratorInfo
                {
                    current = keyEnumerator.GetActualType().GetProperty("Current"),
                    moveNext = keyEnumerator.GetActualType().GetMethod("MoveNext"),
                };

                var values = type.GetProperty("Values").GetValue(dictionary, null);
                var valueEnumerator = values.GetActualType().GetMethod("GetEnumerator").Invoke(values, null);
                var valueInfo = new EnumeratorInfo
                {
                    current = valueEnumerator.GetActualType().GetProperty("Current"),
                    moveNext = valueEnumerator.GetActualType().GetMethod("MoveNext"),
                };

                dictEnumerator = EnumerateCppDict(keyInfo, keyEnumerator, valueInfo, valueEnumerator);
                return true;
            }
            catch //(Exception ex)
            {
                //ExplorerCore.LogWarning($"Exception enumerating IDictionary: {ex.ReflectionExToString()}");
                dictEnumerator = null;
                return false;
            }
        }

        internal static IEnumerator<DictionaryEntry> EnumerateCppDict(EnumeratorInfo keyInfo, object keyEnumerator, 
            EnumeratorInfo valueInfo, object valueEnumerator)
        {
            while ((bool)keyInfo.moveNext.Invoke(keyEnumerator, null))
            {
                valueInfo.moveNext.Invoke(valueEnumerator, null);

                var key = keyInfo.current.GetValue(keyEnumerator, null);
                var value = valueInfo.current.GetValue(valueEnumerator, null);

                yield return new DictionaryEntry(key, value);
            }
        }

        internal static IEnumerator<DictionaryEntry> EnumerateCppHashTable(Il2CppSystem.Collections.Hashtable hashtable)
        {
            for (int i = 0; i < hashtable.buckets.Count; i++)
            {
                var bucket = hashtable.buckets[i];
                if (bucket == null || bucket.key == null)
                    continue;

                yield return new DictionaryEntry(bucket.key, bucket.val);
            }
        }

#endregion

    }
}

#endif