using System;
using System.Text;
using UniverseLib;
using UniverseLib.Utility;

namespace UnityExplorer.UI.Widgets
{
    public class GenericArgumentHandler : BaseArgumentHandler
    {
        private Type genericType;

        public void OnBorrowed(EvaluateWidget evaluator, Type genericConstraint)
        {
            this.evaluator = evaluator;
            this.genericType = genericConstraint;

            typeCompleter.Enabled = true;
            typeCompleter.BaseType = genericType;
            typeCompleter.CacheTypes();

            Type[] constraints = genericType.GetGenericParameterConstraints();
            typeCompleter.GenericConstraints = constraints;

            StringBuilder sb = new($"<color={SignatureHighlighter.CONST}>{genericType.Name}</color>");

            for (int j = 0; j < constraints.Length; j++)
            {
                if (j == 0) sb.Append(' ').Append('(');
                else sb.Append(',').Append(' ');

                sb.Append(SignatureHighlighter.Parse(constraints[j], false));

                if (j + 1 == constraints.Length)
                    sb.Append(')');
            }

            argNameLabel.text = sb.ToString();
        }

        public void OnReturned()
        {
            this.evaluator = null;
            this.genericType = null;

            this.typeCompleter.Enabled = false;

            this.inputField.Text = "";
        }

        public Type Evaluate()
        {
            return ReflectionUtility.GetTypeByName(this.inputField.Text)
                    ?? throw new Exception($"Could not find any type by name '{this.inputField.Text}'!");
        }

        public override void CreateSpecialContent()
        {
        }
    }
}
