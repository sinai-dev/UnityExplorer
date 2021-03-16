using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UnityExplorer.Runtime
{
    // Work in progress, this will be used to replace all the "if CPP / if MONO" 
    // pre-processor directives all over the codebase.

    public abstract class RuntimeProvider
    {
        public static RuntimeProvider Instance;

        public RuntimeProvider()
        {
            Initialize();

            SetupEvents();
        }

        public static void Init() =>
#if CPP
            Instance = new Il2Cpp.Il2CppProvider();
#else
            Instance = new Mono.MonoProvider();
#endif


        public abstract void Initialize();

        public abstract void SetupEvents();

    }
}
