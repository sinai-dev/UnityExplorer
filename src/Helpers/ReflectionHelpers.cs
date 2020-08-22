using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using UnhollowerBaseLib;
using UnhollowerRuntimeLib;
using UnityEngine;
using BF = System.Reflection.BindingFlags;
using ILBF = Il2CppSystem.Reflection.BindingFlags;
using MelonLoader;

namespace Explorer
{
    public class ReflectionHelpers
    {
        public static BF CommonFlags = BF.Public | BF.Instance | BF.NonPublic | BF.Static;
        public static ILBF CommonFlags_IL = ILBF.Public | ILBF.NonPublic | ILBF.Instance | ILBF.Static;

        public static Il2CppSystem.Type GameObjectType => Il2CppType.Of<GameObject>();
        public static Il2CppSystem.Type TransformType => Il2CppType.Of<Transform>();
        public static Il2CppSystem.Type ObjectType => Il2CppType.Of<UnityEngine.Object>();
        public static Il2CppSystem.Type ComponentType => Il2CppType.Of<Component>();

        private static readonly MethodInfo m_tryCastMethodInfo = typeof(Il2CppObjectBase).GetMethod("TryCast");

        public static object Il2CppCast(object obj, Type castTo)
        {
            if (!typeof(Il2CppSystem.Object).IsAssignableFrom(castTo)) return obj;

            var generic = m_tryCastMethodInfo.MakeGenericMethod(castTo);
            return generic.Invoke(obj, null);
        }

        public static string ExceptionToString(Exception e)
        {
            if (IsFailedGeneric(e))
            {
                return "Unable to initialize this type.";
            }
            else if (IsObjectCollected(e))
            {
                return "Garbage collected in Il2Cpp.";
            }

            return e.GetType() + ", " + e.Message;
        }

        public static bool IsFailedGeneric(Exception e)
        {
            return IsExceptionOfType(e, typeof(TargetInvocationException)) && IsExceptionOfType(e, typeof(TypeLoadException));
        }

        public static bool IsObjectCollected(Exception e)
        {
            return IsExceptionOfType(e, typeof(ObjectCollectedException));
        }

        public static bool IsExceptionOfType(Exception e, Type t, bool strict = true, bool checkInner = true)
        {
            bool isType;

            if (strict)
                isType = e.GetType() == t;
            else
                isType = t.IsAssignableFrom(e.GetType());

            if (isType) return true;

            if (e.InnerException != null && checkInner)
                return IsExceptionOfType(e.InnerException, t, strict);
            else
                return false;
        }

        public static bool IsList(Type t)
        {
            return t.IsGenericType
                && t.GetGenericTypeDefinition() is Type typeDef
                && (typeDef.IsAssignableFrom(typeof(List<>)) || typeDef.IsAssignableFrom(typeof(Il2CppSystem.Collections.Generic.List<>)));
        }

        public static Type GetTypeByName(string typeName)
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    if (asm.GetType(typeName) is Type type)
                    {
                        return type;
                    }
                }
                catch { }
            }

            return null;
        }

        public static Type GetActualType(object m_object)
        {
            if (m_object == null) return null;

            if (m_object is Il2CppSystem.Object ilObject)
            {
                var iltype = ilObject.GetIl2CppType();
                return Type.GetType(iltype.AssemblyQualifiedName);
            }
            else
            {
                return m_object.GetType();
            }
        }

        public static Type[] GetAllBaseTypes(object m_object)
        {
            var list = new List<Type>();

            if (m_object is Il2CppSystem.Object ilObject)
            {
                var ilType = ilObject.GetIl2CppType();
                if (Type.GetType(ilType.AssemblyQualifiedName) is Type ilTypeToManaged)
                {
                    list.Add(ilTypeToManaged);

                    while (ilType.BaseType != null)
                    {
                        ilType = ilType.BaseType;
                        if (Type.GetType(ilType.AssemblyQualifiedName) is Type ilBaseTypeToManaged)
                        {
                            list.Add(ilBaseTypeToManaged);
                        }
                    }
                }
            }
            else
            {
                var type = m_object.GetType();
                list.Add(type);
                while (type.BaseType != null)
                {
                    type = type.BaseType;
                    list.Add(type);
                }
            }

            return list.ToArray();
        }
    }
}
