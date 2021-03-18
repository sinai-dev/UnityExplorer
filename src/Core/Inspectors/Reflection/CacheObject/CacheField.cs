using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using UnityExplorer.UI;
using UnityEngine;

namespace UnityExplorer.Core.Inspectors.Reflection
{
    public class CacheField : CacheMember
    {
        public override bool IsStatic => (MemInfo as FieldInfo).IsStatic;

        public override Type FallbackType => (MemInfo as FieldInfo).FieldType;

        public CacheField(FieldInfo fieldInfo, object declaringInstance, GameObject parent) : base(fieldInfo, declaringInstance, parent)
        {
            CreateIValue(null, fieldInfo.FieldType);
        }

        public override void UpdateReflection()
        {
            var fi = MemInfo as FieldInfo;
            IValue.Value = fi.GetValue(fi.IsStatic ? null : DeclaringInstance);

            m_evaluated = true;
            ReflectionException = null;
        }

        public override void SetValue()
        {
            var fi = MemInfo as FieldInfo;
            fi.SetValue(fi.IsStatic ? null : DeclaringInstance, IValue.Value);

            if (this.ParentInspector?.ParentMember != null)
                this.ParentInspector.ParentMember.SetValue();
        }
    }
}
