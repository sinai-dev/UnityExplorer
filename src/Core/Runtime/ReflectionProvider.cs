using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UnityExplorer.Core.Runtime
{
    public abstract class ReflectionProvider
    {
        public static ReflectionProvider Instance;

        public ReflectionProvider()
        {
            Instance = this;
        }

        public abstract Type GetActualType(object obj);

        public abstract object Cast(object obj, Type castTo);

        public abstract T TryCast<T>(object obj);

        public abstract bool IsAssignableFrom(Type toAssignTo, Type toAssignFrom);

        public abstract bool IsReflectionSupported(Type type);

        public abstract string ProcessTypeNameInString(Type type, string theString, ref string typeName);

        public abstract bool LoadModule(string module);

        public abstract void BoxStringToType(ref object _string, Type castTo);

        public virtual string UnboxString(object value) => (string)value;

        public virtual IDictionary EnumerateDictionary(object value, Type typeOfKeys, Type typeOfValues)
            => null;

        public virtual IEnumerable EnumerateEnumerable(object value)
            => null;
    }
}
