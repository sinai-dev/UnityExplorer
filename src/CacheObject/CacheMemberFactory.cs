using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityExplorer.Inspectors;
using UnityExplorer.Runtime;
using UniverseLib;

namespace UnityExplorer.CacheObject
{
    public static class CacheMemberFactory
    {
        public static List<CacheMember> GetCacheMembers(Type type, ReflectionInspector inspector)
        {
            //var list = new List<CacheMember>();
            HashSet<string> cachedSigs = new();
            List<CacheMember> props = new();
            List<CacheMember> fields = new();
            List<CacheMember> ctors = new();
            List<CacheMember> methods = new();

            Type[] types = ReflectionUtility.GetAllBaseTypes(type);

            BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static;
            if (!inspector.StaticOnly)
                flags |= BindingFlags.Instance;

            if (!type.IsAbstract)
            {
                // Get non-static constructors of the main type.
                // There's no reason to get the static cctor, it will be invoked when we inspect the class.
                // Also no point getting ctors on inherited types.
                foreach (ConstructorInfo ctor in type.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                    TryCacheMember(ctor, ctors, cachedSigs, type, inspector);

                // structs always have a parameterless constructor
                if (type.IsValueType)
                {
                    CacheConstructor cached = new(type);
                    cached.SetFallbackType(type);
                    cached.SetInspectorOwner(inspector, null);
                    ctors.Add(cached);
                }
            }

            foreach (Type declaringType in types)
            {
                foreach (PropertyInfo prop in declaringType.GetProperties(flags))
                    if (prop.DeclaringType == declaringType)
                        TryCacheMember(prop, props, cachedSigs, declaringType, inspector);

                foreach (FieldInfo field in declaringType.GetFields(flags))
                    if (field.DeclaringType == declaringType)
                        TryCacheMember(field, fields, cachedSigs, declaringType, inspector);

                foreach (MethodInfo method in declaringType.GetMethods(flags))
                    if (method.DeclaringType == declaringType)
                        TryCacheMember(method, methods, cachedSigs, declaringType, inspector);

            }

            List<CacheMember> sorted = new();
            sorted.AddRange(props.OrderBy(it => Array.IndexOf(types, it.DeclaringType))
                                 .ThenBy(it => it.NameForFiltering));
            sorted.AddRange(fields.OrderBy(it => Array.IndexOf(types, it.DeclaringType))
                                 .ThenBy(it => it.NameForFiltering));
            sorted.AddRange(ctors.OrderBy(it => Array.IndexOf(types, it.DeclaringType))
                                 .ThenBy(it => it.NameForFiltering));
            sorted.AddRange(methods.OrderBy(it => Array.IndexOf(types, it.DeclaringType))
                                 .ThenBy(it => it.NameForFiltering));
            return sorted;
        }

        static void TryCacheMember<T>(MemberInfo member, List<T> list, HashSet<string> cachedSigs,
            Type declaringType, ReflectionInspector inspector, bool ignorePropertyMethodInfos = true)
            where T : CacheMember
        {
            try
            {
                if (UERuntimeHelper.IsBlacklisted(member))
                    return;

                string sig = member switch
                {
                    MethodBase mb => mb.FullDescription(), // (method or constructor)
                    PropertyInfo or FieldInfo => $"{member.DeclaringType.FullDescription()}.{member.Name}",
                    _ => throw new NotImplementedException(),
                };

                if (cachedSigs.Contains(sig))
                    return;

                // ExplorerCore.Log($"Trying to cache member {sig}... ({member.MemberType})");

                CacheMember cached;
                Type returnType;

                switch (member.MemberType)
                {
                    case MemberTypes.Constructor:
                        {
                            ConstructorInfo ci = member as ConstructorInfo;
                            cached = new CacheConstructor(ci);
                            returnType = ci.DeclaringType;
                        }
                        break;

                    case MemberTypes.Method:
                        {
                            MethodInfo mi = member as MethodInfo;
                            if (ignorePropertyMethodInfos
                                && (mi.Name.StartsWith("get_") || mi.Name.StartsWith("set_")))
                                return;

                            cached = new CacheMethod(mi);
                            returnType = mi.ReturnType;
                            break;
                        }

                    case MemberTypes.Property:
                        {
                            PropertyInfo pi = member as PropertyInfo;

                            if (!pi.CanRead && pi.CanWrite)
                            {
                                // write-only property, cache the set method instead.
                                MethodInfo setMethod = pi.GetSetMethod(true);
                                if (setMethod != null)
                                    TryCacheMember(setMethod, list, cachedSigs, declaringType, inspector, false);
                                return;
                            }

                            cached = new CacheProperty(pi);
                            returnType = pi.PropertyType;
                            break;
                        }

                    case MemberTypes.Field:
                        {
                            FieldInfo fi = member as FieldInfo;
                            cached = new CacheField(fi);
                            returnType = fi.FieldType;
                            break;
                        }

                    default:
                        throw new NotImplementedException();
                }

                cachedSigs.Add(sig);

                cached.SetFallbackType(returnType);
                cached.SetInspectorOwner(inspector, member);

                list.Add((T)cached);
            }
            catch (Exception e)
            {
                ExplorerCore.LogWarning($"Exception caching member {member.DeclaringType.FullName}.{member.Name}!");
                ExplorerCore.Log(e);
            }
        }
    }
}
