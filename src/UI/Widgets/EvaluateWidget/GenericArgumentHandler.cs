using System;
using System.Text;
using UniverseLib;
using UniverseLib.Utility;

namespace UnityExplorer.UI.Widgets
{
    public class GenericArgumentHandler : BaseArgumentHandler
    {
        private Type genericArgument;

        public void OnBorrowed(Type genericArgument)
        {
            this.genericArgument = genericArgument;

            typeCompleter.Enabled = true;
            typeCompleter.BaseType = this.genericArgument;

            Type[] constraints = this.genericArgument.GetGenericParameterConstraints();
            typeCompleter.GenericConstraints = constraints;

            typeCompleter.CacheTypes();

            StringBuilder sb = new($"<color={SignatureHighlighter.CONST}>{this.genericArgument.Name}</color>");

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
            this.genericArgument = null;

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
