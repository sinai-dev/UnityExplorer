using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Mono.CSharp;

// Thanks to ManlyMarco for most of this

namespace UnityExplorer.Core.CSharp
{
    public class ScriptEvaluator : Evaluator, IDisposable
    {
        private static readonly HashSet<string> StdLib = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase)
        {
            "mscorlib", "System.Core", "System", "System.Xml"
        };

        internal static TextWriter _textWriter;
        internal static StreamReportPrinter _reportPrinter;

        public ScriptEvaluator(TextWriter tw) : base(BuildContext(tw))
        {
            _textWriter = tw;

            ImportAppdomainAssemblies(ReferenceAssembly);
            AppDomain.CurrentDomain.AssemblyLoad += OnAssemblyLoad;
        }

        public void Dispose()
        {
            AppDomain.CurrentDomain.AssemblyLoad -= OnAssemblyLoad;
            _textWriter.Dispose();
        }

        private void OnAssemblyLoad(object sender, AssemblyLoadEventArgs args)
        {
            string name = args.LoadedAssembly.GetName().Name;

            if (StdLib.Contains(name))
                return;

            ReferenceAssembly(args.LoadedAssembly);
        }

        private static CompilerContext BuildContext(TextWriter tw)
        {
            _reportPrinter = new StreamReportPrinter(tw);

            var settings = new CompilerSettings
            {
                Version = LanguageVersion.Experimental,
                GenerateDebugInfo = false,
                StdLib = true,
                Target = Target.Library,
                WarningLevel = 0,
                EnhancedWarnings = false
            };

            return new CompilerContext(settings, _reportPrinter);
        }

        private static void ImportAppdomainAssemblies(Action<Assembly> import)
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                string name = assembly.GetName().Name;
                if (StdLib.Contains(name))
                {
                    continue;
                }

                import(assembly);
            }
        }
    }
}
