using UnityExplorer.Inspectors;

namespace UnityExplorer.CacheObject
{
    public class CacheConstructor : CacheMember
    {
        public ConstructorInfo CtorInfo { get; }
        readonly Type typeForStructConstructor;

        public override Type DeclaringType => typeForStructConstructor ?? CtorInfo.DeclaringType;
        public override bool IsStatic => true;
        public override bool ShouldAutoEvaluate => false;
        public override bool CanWrite => false;

        public CacheConstructor(ConstructorInfo ci)
        {
            this.CtorInfo = ci;
        }

        public CacheConstructor(Type typeForStructConstructor)
        {
            this.typeForStructConstructor = typeForStructConstructor;
        }

        public override void SetInspectorOwner(ReflectionInspector inspector, MemberInfo member)
        {
            Type ctorReturnType;
            // if is parameterless struct ctor
            if (typeForStructConstructor != null)
            {
                ctorReturnType = typeForStructConstructor;
                this.Owner = inspector;

                // eg. Vector3.Vector3()
                this.NameLabelText = SignatureHighlighter.Parse(typeForStructConstructor, false);
                NameLabelText += $".{NameLabelText}()";

                this.NameForFiltering = SignatureHighlighter.RemoveHighlighting(NameLabelText);
                this.NameLabelTextRaw = NameForFiltering;
                return;
            }
            else
            {
                base.SetInspectorOwner(inspector, member);

                Arguments = CtorInfo.GetParameters();
                ctorReturnType = CtorInfo.DeclaringType;
            }

            if (ctorReturnType.IsGenericTypeDefinition)
                GenericArguments = ctorReturnType.GetGenericArguments();
        }

        protected override object TryEvaluate()
        {
            try
            {
                Type returnType = DeclaringType;

                if (returnType.IsGenericTypeDefinition)
                    returnType = DeclaringType.MakeGenericType(Evaluator.TryParseGenericArguments());

                object ret;
                if (HasArguments)
                    ret = Activator.CreateInstance(returnType, Evaluator.TryParseArguments());
                else
                    ret = Activator.CreateInstance(returnType, ArgumentUtility.EmptyArgs);

                LastException = null;
                return ret;
            }
            catch (Exception ex)
            {
                LastException = ex;
                return null;
            }
        }

        protected override void TrySetValue(object value) => throw new NotImplementedException("You can't set a constructor");
    }
}
