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

namespace UnityExplorer.Core.Runtime.Il2Cpp
{
    [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "External methods")]
    public class Il2CppReflection : ReflectionProvider
    {
        public Il2CppReflection() : base()
        {
            Instance = this;

            TryLoadGameModules();

            BuildDeobfuscationCache();
            AppDomain.CurrentDomain.AssemblyLoad += OnAssemblyLoaded;
        }

        private void OnAssemblyLoaded(object sender, AssemblyLoadEventArgs args)
        {
            foreach (var type in args.LoadedAssembly.TryGetTypes())
                TryCacheDeobfuscatedType(type);
        }

        public override object Cast(object obj, Type castTo)
        {
            return Il2CppCast(obj, castTo);
        }

        public override T TryCast<T>(object obj)
        {
            try
            {
                return (T)Il2CppCast(obj, typeof(T));
            }
            catch
            {
                return default;
            }
        }

        public override Type GetActualType(object obj)
        {
            if (obj == null)
                return null;

            var type = obj.GetType();

            try
            {
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
                        return ReflectionUtility.GetTypeByName(cppType.FullName) ?? type;
                    }

                    return GetMonoType(cppType) ?? type;
                }
            }
            catch //(Exception ex)
            {
                //ExplorerCore.LogWarning("Exception in GetActualType: " + ex);
            }

            return type;
        }

        public static Type GetMonoType(CppType cppType)
        {
            if (DeobfuscatedTypes.TryGetValue(cppType.AssemblyQualifiedName, out Type deob))
                return deob;

            var fullname = cppType.FullName;
            if (fullname.StartsWith("System."))
                fullname = $"Il2Cpp{fullname}";

            ReflectionUtility.AllTypes.TryGetValue(fullname, out Type monoType);
            return monoType;
        }

        // cached class pointers for Il2CppCast
        private static readonly Dictionary<string, IntPtr> s_cppClassPointers = new Dictionary<string, IntPtr>();

        /// <summary>
        /// Attempt to cast the object to its underlying type.
        /// </summary>
        /// <param name="obj">The object you want to cast.</param>
        /// <returns>The object, as the underlying type if successful or the input value if not.</returns>
        public static object Il2CppCast(object obj) => Il2CppCast(obj, Instance.GetActualType(obj));

        /// <summary>
        /// Attempt to cast the object to the provided type.
        /// </summary>
        /// <param name="obj">The object you want to cast.</param>
        /// <param name="castTo">The Type you want to cast to.</param>
        /// <returns>The object, as the type (or a normal C# object) if successful or the input value if not.</returns>
        public static object Il2CppCast(object obj, Type castTo)
        {
            if (obj == null)
                return null;

            var type = obj.GetType();

            if (type.IsValueType && typeof(Il2CppSystem.Object).IsAssignableFrom(castTo))
                return BoxIl2CppObject(obj);

            if (!(obj is Il2CppSystem.Object cppObj))
                return obj;

            if (castTo.IsValueType)
            {
                if (castTo.FullName.StartsWith("Il2CppSystem."))
                    ReflectionUtility.AllTypes.TryGetValue(cppObj.GetIl2CppType().FullName, out castTo);
                return Unbox(cppObj, castTo);
            }

            if (!Il2CppTypeNotNull(castTo, out IntPtr castToPtr))
                return obj;

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

        // struct boxing

        // cached il2cpp unbox methods
        internal static readonly Dictionary<string, MethodInfo> s_unboxMethods = new Dictionary<string, MethodInfo>();

        /// <summary>
        /// Attempt to unbox the object to the underlying struct type.
        /// </summary>
        /// <param name="obj">The object which is a struct underneath.</param>
        /// <returns>The struct if successful, otherwise null.</returns>
        public static object Unbox(object obj) => Unbox(obj, Instance.GetActualType(obj));

        /// <summary>
        /// Attempt to unbox the object to the struct type.
        /// </summary>
        /// <param name="obj">The object which is a struct underneath.</param>
        /// <param name="type">The type of the struct you want to unbox to.</param>
        /// <returns>The struct if successful, otherwise null.</returns>
        public static object Unbox(object obj, Type type)
        {
            if (!type.IsValueType)
                return null;

            if (!(obj is Il2CppSystem.Object))
                return obj;

            var name = type.AssemblyQualifiedName;

            if (!s_unboxMethods.ContainsKey(name))
            {
                s_unboxMethods.Add(name, typeof(Il2CppObjectBase)
                                            .GetMethod("Unbox")
                                            .MakeGenericMethod(type));
            }

            return s_unboxMethods[name].Invoke(obj, new object[0]);
        }

        //internal static Dictionary<string, Type> monoToIl2CppType = new Dictionary<string, Type>();
        internal static Dictionary<Type, MethodInfo> structBoxMethods = new Dictionary<Type, MethodInfo>();
        internal static Dictionary<Type, FieldInfo> structValueFields = new Dictionary<Type, FieldInfo>();

        /// <summary>
        /// Try to box a value to Il2CppSystem.Object using the Il2CppSystem representation of the value type.
        /// </summary>
        /// <param name="value">The value, eg 5 (System.Int32)</param>
        /// <returns>The boxed Il2CppSystem.Object for the value, eg for '5' it would be a boxed Il2CppSytem.Int32.</returns>
        public static Il2CppSystem.Object BoxIl2CppObject(object value)
        {
            string key = $"Il2Cpp{value.GetType().FullName}";

            if (ReflectionUtility.AllTypes.TryGetValue(key, out Type type) && type.IsValueType)
            {
                var cppStruct = Activator.CreateInstance(type);

                if (!structValueFields.ContainsKey(type))
                    structValueFields.Add(type, type.GetField("m_value"));

                structValueFields[type].SetValue(cppStruct, value);

                if (!structBoxMethods.ContainsKey(type))
                    structBoxMethods.Add(type, type.GetMethod("BoxIl2CppObject"));

                return structBoxMethods[type].Invoke(cppStruct, null) as Il2CppSystem.Object;
            }
            else
            {
                ExplorerCore.LogWarning("Couldn't get type to box to");
                return null;
            }
        }

        // deobfuscation

        private static readonly Dictionary<string, Type> DeobfuscatedTypes = new Dictionary<string, Type>();
        internal static Dictionary<string, string> s_deobfuscatedTypeNames = new Dictionary<string, string>();

        private static void BuildDeobfuscationCache()
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var type in asm.TryGetTypes())
                    TryCacheDeobfuscatedType(type);
            }

            if (s_deobfuscatedTypeNames.Count > 0)
                ExplorerCore.Log($"Built deobfuscation cache, count: {s_deobfuscatedTypeNames.Count}");
        }

        private static void TryCacheDeobfuscatedType(Type type)
        {
            try
            {
                // Thanks to Slaynash for this

                if (type.CustomAttributes.Any(it => it.AttributeType.Name == "ObfuscatedNameAttribute"))
                {
                    var cppType = Il2CppType.From(type);

                    if (!DeobfuscatedTypes.ContainsKey(cppType.AssemblyQualifiedName))
                    {
                        DeobfuscatedTypes.Add(cppType.AssemblyQualifiedName, type);
                        s_deobfuscatedTypeNames.Add(cppType.FullName, type.FullName);
                    }
                }
            }
            catch { }
        }

        public override Type GetDeobfuscatedType(Type type)
        {
            try
            {
                if (DeobfuscatedTypes.ContainsKey(type.AssemblyQualifiedName))
                    return DeobfuscatedTypes[type.AssemblyQualifiedName];
            }
            catch { }

            return type;
        }

        // misc helpers

        /// <summary>
        /// Get the Il2Cpp Class Pointer for the provided Mono (Unhollowed) Type.
        /// </summary>
        /// <param name="type">The Mono/Unhollowed Type you want the Il2Cpp Class Pointer for.</param>
        /// <returns>True if successful, false if not.</returns>
        public static bool Il2CppTypeNotNull(Type type) => Il2CppTypeNotNull(type, out _);

        /// <summary>
        /// Get the Il2Cpp Class Pointer for the provided Mono (Unhollowed) Type.
        /// </summary>
        /// <param name="type">The Mono/Unhollowed Type you want the Il2Cpp Class Pointer for.</param>
        /// <param name="il2cppPtr">The IntPtr for the Il2Cpp class, or IntPtr.Zero if not found.</param>
        /// <returns>True if successful, false if not.</returns>
        public static bool Il2CppTypeNotNull(Type type, out IntPtr il2cppPtr)
        {
            if (s_cppClassPointers.TryGetValue(type.AssemblyQualifiedName, out il2cppPtr))
                return il2cppPtr != IntPtr.Zero;

            il2cppPtr = (IntPtr)typeof(Il2CppClassPointerStore<>)
                    .MakeGenericType(new Type[] { type })
                    .GetField("NativeClassPtr", BF.Public | BF.Static)
                    .GetValue(null);

            s_cppClassPointers.Add(type.AssemblyQualifiedName, il2cppPtr);

            return il2cppPtr != IntPtr.Zero;
        }

        #region EXTERN CAST METHODS

        // Extern C++ methods 
        [DllImport("GameAssembly", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern bool il2cpp_class_is_assignable_from(IntPtr klass, IntPtr oklass);

        [DllImport("GameAssembly", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern IntPtr il2cpp_object_get_class(IntPtr obj);

        #endregion

        // Strings

        private const string IL2CPP_STRING_FULLNAME = "Il2CppSystem.String";

        public override bool IsString(object obj) => obj is string || obj.GetActualType().FullName == IL2CPP_STRING_FULLNAME;

        public override void BoxStringToType(ref object value, Type castTo)
        {
            if (castTo == typeof(Il2CppSystem.String))
                value = (Il2CppSystem.String)(value as string);
            else
                value = (Il2CppSystem.Object)(value as string);
        }

        public override string UnboxString(object value)
        {
            if (value is string s)
                return s;

            s = null;
            // strings boxed as Il2CppSystem.Objects can behave weirdly.
            // GetActualType will find they are a string, but if its boxed
            // then we need to unbox it like this...
            if (value is Il2CppSystem.Object cppObject)
                s = cppObject.ToString();
            else if (value is Il2CppSystem.String cppString)
                s = cppString;

            return s;
        }

        public override string ProcessTypeFullNameInString(Type type, string theString, ref string typeName)
        {
            if (!Il2CppTypeNotNull(type))
                return theString;

            var cppType = Il2CppType.From(type);
            if (cppType != null && s_deobfuscatedTypeNames.ContainsKey(cppType.FullName))
            {
                typeName = s_deobfuscatedTypeNames[cppType.FullName];
                theString = theString.Replace(cppType.FullName, typeName);
            }

            return theString;
        }


        //// Not currently using, not sure if its necessary anymore, was necessary to prevent crashes at one point.
        //public override bool IsReflectionSupported(Type type)
        //{
        //    try
        //    {
        //        var gArgs = type.GetGenericArguments();
        //        if (!gArgs.Any())
        //            return true;
        //
        //        foreach (var gType in gArgs)
        //        {
        //            if (!Supported(gType))
        //                return false;
        //        }
        //
        //        return true;
        //
        //        bool Supported(Type t)
        //        {
        //            if (!typeof(Il2CppSystem.Object).IsAssignableFrom(t))
        //                return true;
        //
        //            if (!Il2CppTypeNotNull(t, out IntPtr ptr))
        //                return false;
        //
        //            return CppType.internal_from_handle(IL2CPP.il2cpp_class_get_type(ptr)) is CppType;
        //        }
        //    }
        //    catch
        //    {
        //        return false;
        //    }
        //}



        #region FORCE LOADING GAME MODULES

        // Helper for IL2CPP to try to make sure the Unhollowed game assemblies are actually loaded.

        internal static void TryLoadGameModules()
        {
            Instance.LoadModule("Assembly-CSharp");
            Instance.LoadModule("Assembly-CSharp-firstpass");
        }

        public override bool LoadModule(string module)
        {
#if ML
            var path = Path.Combine("MelonLoader", "Managed", $"{module}.dll");
#else
            var path = Path.Combine("BepInEx", "unhollowed", $"{module}.dll");
#endif
            return LoadModuleInternal(path);
        }

        internal static bool LoadModuleInternal(string fullPath)
        {
            if (!File.Exists(fullPath))
                return false;

            try
            {
                Assembly.Load(File.ReadAllBytes(fullPath));
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.GetType() + ", " + e.Message);
            }

            return false;
        }

        #endregion

        #region IL2CPP IENUMERABLE / IDICTIONARY

        // dictionary and enumerable cast helpers (until unhollower rewrite)

        internal static IntPtr s_cppEnumerableClassPtr;
        internal static IntPtr s_cppDictionaryClassPtr;

        public override bool IsAssignableFrom(Type toAssignTo, Type toAssignFrom)
        {
            if (toAssignTo.IsAssignableFrom(toAssignFrom))
                return true;

            if (toAssignTo == typeof(IEnumerable))
            {
                try
                {
                    if (s_cppEnumerableClassPtr == IntPtr.Zero)
                        Il2CppTypeNotNull(typeof(Il2CppSystem.Collections.IEnumerable), out s_cppEnumerableClassPtr);

                    if (s_cppEnumerableClassPtr != IntPtr.Zero
                        && Il2CppTypeNotNull(toAssignFrom, out IntPtr assignFromPtr)
                        && il2cpp_class_is_assignable_from(s_cppEnumerableClassPtr, assignFromPtr))
                    {
                        return true;
                    }
                }
                catch { }
            }
            else if (toAssignTo == typeof(IDictionary))
            {
                try
                {
                    if (s_cppDictionaryClassPtr == IntPtr.Zero)
                        if (!Il2CppTypeNotNull(typeof(Il2CppSystem.Collections.IDictionary), out s_cppDictionaryClassPtr))
                            return false;

                    if (Il2CppTypeNotNull(toAssignFrom, out IntPtr classPtr))
                    {
                        if (il2cpp_class_is_assignable_from(s_cppDictionaryClassPtr, classPtr))
                            return true;
                    }
                }
                catch { }
            }

            return false;
        }

        internal static readonly Dictionary<Type, MethodInfo> s_getEnumeratorMethods = new Dictionary<Type, MethodInfo>();

        internal static readonly Dictionary<Type, EnumeratorInfo> s_enumeratorInfos = new Dictionary<Type, EnumeratorInfo>();

        internal class EnumeratorInfo
        {
            internal MethodInfo moveNext;
            internal PropertyInfo current;
        }

        public override IEnumerable EnumerateEnumerable(object value)
        {
            if (value == null)
                return null;

            var cppEnumerable = (value as Il2CppSystem.Object)?.TryCast<Il2CppSystem.Collections.IEnumerable>();
            if (cppEnumerable != null)
            {
                var type = value.GetType();
                if (!s_getEnumeratorMethods.ContainsKey(type))
                    s_getEnumeratorMethods.Add(type, type.GetMethod("GetEnumerator"));

                var enumerator = s_getEnumeratorMethods[type].Invoke(value, null);
                var enumeratorType = enumerator.GetType();

                if (!s_enumeratorInfos.ContainsKey(enumeratorType))
                {
                    s_enumeratorInfos.Add(enumeratorType, new EnumeratorInfo
                    {
                        current = enumeratorType.GetProperty("Current"),
                        moveNext = enumeratorType.GetMethod("MoveNext"),
                    });
                }
                var info = s_enumeratorInfos[enumeratorType];

                // iterate
                var list = new List<object>();
                while ((bool)info.moveNext.Invoke(enumerator, null))
                    list.Add(info.current.GetValue(enumerator));

                return list;
            }

            return null;
        }

        public override IDictionary EnumerateDictionary(object value, Type typeOfKeys, Type typeOfValues)
        {
            var valueType = ReflectionUtility.GetActualType(value);

            var keyList = new List<object>();
            var valueList = new List<object>();

            var hashtable = value.TryCast(typeof(Il2CppSystem.Collections.Hashtable)) as Il2CppSystem.Collections.Hashtable;
            if (hashtable != null)
            {
                EnumerateCppHashtable(hashtable, keyList, valueList);
            }
            else
            {
                var keys = valueType.GetProperty("Keys").GetValue(value, null);
                var values = valueType.GetProperty("Values").GetValue(value, null);

                EnumerateCppIDictionary(keys, keyList);
                EnumerateCppIDictionary(values, valueList);
            }

            var dict = Activator.CreateInstance(typeof(Dictionary<,>)
                                .MakeGenericType(typeOfKeys, typeOfValues))
                                as IDictionary;

            for (int i = 0; i < keyList.Count; i++)
                dict.Add(keyList[i], valueList[i]);

            return dict;
        }

        private void EnumerateCppIDictionary(object collection, List<object> list)
        {
            // invoke GetEnumerator
            var enumerator = collection.GetType().GetMethod("GetEnumerator").Invoke(collection, null);
            // get the type of it
            var enumeratorType = enumerator.GetType();
            // reflect MoveNext and Current
            var moveNext = enumeratorType.GetMethod("MoveNext");
            var current = enumeratorType.GetProperty("Current");
            // iterate
            while ((bool)moveNext.Invoke(enumerator, null))
            {
                list.Add(current.GetValue(enumerator, null));
            }
        }

        private void EnumerateCppHashtable(Il2CppSystem.Collections.Hashtable hashtable, List<object> keys, List<object> values)
        {
            for (int i = 0; i < hashtable.buckets.Count; i++)
            {
                var bucket = hashtable.buckets[i];
                if (bucket == null || bucket.key == null)
                    continue;
                keys.Add(bucket.key);
                values.Add(bucket.val);
            }
        }

        public override void FindSingleton(string[] possibleNames, Type type, BF flags, List<object> instances)
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

            base.FindSingleton(possibleNames, type, flags, instances);
        }

        #endregion
    }
}

#endif