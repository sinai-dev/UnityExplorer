#if MONO
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UnityExplorer.Core.Runtime.Mono
{
    public class MonoReflection : ReflectionProvider
    {
        // Mono doesn't need to explicitly cast things.
        public override object Cast(object obj, Type castTo)
            => obj;

        // Vanilla GetType is fine for mono
        public override Type GetActualType(object obj)
            => obj.GetType();

        public override bool IsAssignableFrom(Type toAssignTo, Type toAssignFrom)
            => toAssignTo.IsAssignableFrom(toAssignFrom);

        public override bool IsReflectionSupported(Type type)
            => true;

        public override bool LoadModule(string module)
            => true;

        public override string ProcessTypeNameInString(Type type, string theString, ref string typeName)
            => theString;

        // not necessary
        public override void BoxStringToType(ref object _string, Type castTo) { }
    }
}

#endif