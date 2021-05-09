using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace UnityExplorer
{
    public static class ParseUtility
    {
        public static CultureInfo en_US = new CultureInfo("en-US");

        private static readonly HashSet<Type> nonPrimitiveTypes = new HashSet<Type>
        {
            typeof(string),
            typeof(decimal),
            typeof(DateTime),
        };

        public static bool CanParse(Type type)
        {
            if (string.IsNullOrEmpty(type.FullName))
                return false;
            return type.IsPrimitive || type.IsEnum || nonPrimitiveTypes.Contains(type) || customTypes.ContainsKey(type.FullName);
        }

        public static bool TryParse(string input, Type type, out object obj, out Exception parseException)
        {
            obj = null;
            parseException = null;

            if (type == null)
                return false;

            if (type == typeof(string))
            {
                obj = input;
                return true;
            }

            if (type.IsEnum)
            {
                try
                {
                    obj = Enum.Parse(type, input);
                    return true;
                }
                catch (Exception ex)
                {
                    parseException = ex.GetInnerMostException();
                    return false;
                }
            }

            try
            {
                if (customTypes.ContainsKey(type.FullName))
                {
                    obj = customTypes[type.FullName].Invoke(input);
                }
                else
                {
                    obj = ReflectionUtility.GetMethodInfo(type, "Parse", ArgumentUtility.ParseArgs)
                        .Invoke(null, new object[] { input });
                }
                
                return true;
            }
            catch (Exception ex)
            {
                ex = ex.GetInnerMostException();
                parseException = ex;
            }

            return false;
        }

        private static readonly HashSet<Type> nonFormattedTypes = new HashSet<Type>
        {
            typeof(IntPtr),
            typeof(UIntPtr),
        };

        public static string ToStringForInput(object obj, Type type)
        {
            if (type == null || obj == null)
                return null;

            if (type == typeof(string))
                return obj as string;

            if (type.IsEnum)
            {
                return Enum.IsDefined(type, obj)
                        ? Enum.GetName(type, obj)
                        : obj.ToString();
            }

            try
            {
                if (customTypes.ContainsKey(type.FullName))
                {
                    return customTypesToString[type.FullName].Invoke(obj);
                }
                else
                {
                    if (nonFormattedTypes.Contains(type))
                        return obj.ToString();
                    else
                        return ReflectionUtility.GetMethodInfo(type, "ToString", new Type[] { typeof(IFormatProvider) })
                                .Invoke(obj, new object[] { en_US })
                                as string;
                }
            }
            catch (Exception ex)
            {
                ExplorerCore.LogWarning($"Exception formatting object for input: {ex}");
                return null;
            }
        }

        private static readonly Dictionary<string, string> typeInputExamples = new Dictionary<string, string>();

        public static string GetExampleInput(Type type)
        {
            if (!typeInputExamples.ContainsKey(type.AssemblyQualifiedName))
            {
                try
                {
                    if (type.IsEnum)
                    {
                        typeInputExamples.Add(type.AssemblyQualifiedName, Enum.GetNames(type).First());
                    }
                    else
                    {
                        var instance = Activator.CreateInstance(type);
                        typeInputExamples.Add(type.AssemblyQualifiedName, ToStringForInput(instance, type));
                    }
                }
                catch (Exception ex)
                {
                    ExplorerCore.LogWarning("Exception generating default instance for example input for '" + type.FullName + "'");
                    ExplorerCore.Log(ex);
                    return "";
                }
            }

            return typeInputExamples[type.AssemblyQualifiedName];
        }

        #region Custom parse methods

        internal delegate object ParseMethod(string input);

        private static readonly Dictionary<string, ParseMethod> customTypes = new Dictionary<string, ParseMethod>
        {
            { typeof(Vector2).FullName,    TryParseVector2 },
            { typeof(Vector3).FullName,    TryParseVector3 },
            { typeof(Vector4).FullName,    TryParseVector4 },
            { typeof(Quaternion).FullName, TryParseQuaternion },
            { typeof(Rect).FullName,       TryParseRect },
            { typeof(Color).FullName,      TryParseColor },
            { typeof(Color32).FullName,    TryParseColor32 },
            { typeof(LayerMask).FullName,  TryParseLayerMask },
        };

        internal delegate string ToStringMethod(object obj);

        private static readonly Dictionary<string, ToStringMethod> customTypesToString = new Dictionary<string, ToStringMethod>
        {
            { typeof(Vector2).FullName,    Vector2ToString },
            { typeof(Vector3).FullName,    Vector3ToString },
            { typeof(Vector4).FullName,    Vector4ToString },
            { typeof(Quaternion).FullName, QuaternionToString },
            { typeof(Rect).FullName,       RectToString },
            { typeof(Color).FullName,      ColorToString },
            { typeof(Color32).FullName,    Color32ToString },
            { typeof(LayerMask).FullName,  LayerMaskToString },
        };

        // Vector2

        public static object TryParseVector2(string input)
        {
            Vector2 vector = default;

            var split = input.Split(',');

            vector.x = float.Parse(split[0].Trim(), en_US);
            vector.y = float.Parse(split[1].Trim(), en_US);

            return vector;
        }

        public static string Vector2ToString(object obj)
        {
            if (!(obj is Vector2 vector))
                return null;

            return string.Format(en_US, "{0}, {1}", new object[]
            {
                vector.x,
                vector.y
            });
        }

        // Vector3

        public static object TryParseVector3(string input)
        {
            Vector3 vector = default;

            var split = input.Split(',');

            vector.x = float.Parse(split[0].Trim(), en_US);
            vector.y = float.Parse(split[1].Trim(), en_US);
            vector.z = float.Parse(split[2].Trim(), en_US);

            return vector;
        }

        public static string Vector3ToString(object obj)
        {
            if (!(obj is Vector3 vector))
                return null;

            return string.Format(en_US, "{0}, {1}, {2}", new object[]
            {
                vector.x,
                vector.y,
                vector.z
            });
        }

        // Vector4

        public static object TryParseVector4(string input)
        {
            Vector4 vector = default;

            var split = input.Split(',');

            vector.x = float.Parse(split[0].Trim(), en_US);
            vector.y = float.Parse(split[1].Trim(), en_US);
            vector.z = float.Parse(split[2].Trim(), en_US);
            vector.w = float.Parse(split[3].Trim(), en_US);

            return vector;
        }

        public static string Vector4ToString(object obj)
        {
            if (!(obj is Vector4 vector))
                return null;

            return string.Format(en_US, "{0}, {1}, {2}, {3}", new object[]
            {
                vector.x,
                vector.y,
                vector.z,
                vector.w
            });
        }

        // Quaternion

        public static object TryParseQuaternion(string input)
        {
            Vector3 vector = default;

            var split = input.Split(',');

            if (split.Length == 4)
            {
                Quaternion quat = default;
                quat.x = float.Parse(split[0].Trim(), en_US);
                quat.y = float.Parse(split[1].Trim(), en_US);
                quat.z = float.Parse(split[2].Trim(), en_US);
                quat.w = float.Parse(split[3].Trim(), en_US);
                return quat;
            }
            else
            {
                vector.x = float.Parse(split[0].Trim(), en_US);
                vector.y = float.Parse(split[1].Trim(), en_US);
                vector.z = float.Parse(split[2].Trim(), en_US);
                return Quaternion.Euler(vector);
            }
        }

        public static string QuaternionToString(object obj)
        {
            if (!(obj is Quaternion quaternion))
                return null;

            Vector3 vector = quaternion.eulerAngles;

            return string.Format(en_US, "{0}, {1}, {2}", new object[]
            {
                vector.x,
                vector.y,
                vector.z,
            });
        }

        // Rect

        public static object TryParseRect(string input)
        {
            Rect rect = default;

            var split = input.Split(',');

            rect.x = float.Parse(split[0].Trim(), en_US);
            rect.y = float.Parse(split[1].Trim(), en_US);
            rect.width = float.Parse(split[2].Trim(), en_US);
            rect.height = float.Parse(split[3].Trim(), en_US);

            return rect;
        }

        public static string RectToString(object obj)
        {
            if (!(obj is Rect rect))
                return null;

            return string.Format(en_US, "{0}, {1}, {2}, {3}", new object[]
            {
                rect.x,
                rect.y,
                rect.width,
                rect.height
            });
        }

        // Color

        public static object TryParseColor(string input)
        {
            Color color = default;

            var split = input.Split(',');

            color.r = float.Parse(split[0].Trim(), en_US);
            color.g = float.Parse(split[1].Trim(), en_US);
            color.b = float.Parse(split[2].Trim(), en_US);
            if (split.Length > 3)
                color.a = float.Parse(split[3].Trim(), en_US);
            else
                color.a = 1;

            return color;
        }

        public static string ColorToString(object obj)
        {
            if (!(obj is Color color))
                return null;

            return string.Format(en_US, "{0}, {1}, {2}, {3}", new object[]
            {
                color.r,
                color.g,
                color.b,
                color.a
            });
        }

        // Color32

        public static object TryParseColor32(string input)
        {
            Color32 color = default;

            var split = input.Split(',');

            color.r = byte.Parse(split[0].Trim(), en_US);
            color.g = byte.Parse(split[1].Trim(), en_US);
            color.b = byte.Parse(split[2].Trim(), en_US);
            if (split.Length > 3)
                color.a = byte.Parse(split[3].Trim(), en_US);
            else
                color.a = 255;

            return color;
        }

        public static string Color32ToString(object obj)
        {
            if (!(obj is Color32 color))
                return null;

            return string.Format(en_US, "{0}, {1}, {2}, {3}", new object[]
            {
                color.r,
                color.g,
                color.b,
                color.a
            });
        }

        // Layermask (Int32)

        public static object TryParseLayerMask(string input)
        {
            return (LayerMask)int.Parse(input);
        }

        public static string LayerMaskToString(object obj)
        {
            if (!(obj is LayerMask mask))
                return null;

            return mask.ToString();
        }

        #endregion
    }
}
