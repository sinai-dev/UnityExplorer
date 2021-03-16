#if CPP
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityExplorer.Helpers;

namespace UnityExplorer.Runtime.Il2Cpp
{
    public class Il2CppProvider : RuntimeProvider
    {
        public override void Initialize()
        {
            ReflectionHelpers.TryLoadGameModules();
        }

        public override void SetupEvents()
        {
            Application.add_logMessageReceived(
                new Action<string, string, LogType>(ExplorerCore.Instance.OnUnityLog));

            //SceneManager.add_sceneLoaded(
            //    new Action<Scene, LoadSceneMode>(ExplorerCore.Instance.OnSceneLoaded1));

            //SceneManager.add_activeSceneChanged(
            //    new Action<Scene, Scene>(ExplorerCore.Instance.OnSceneLoaded2));
        }
    }
}

public static class UnityEventExtensions
{
    public static void AddListener(this UnityEvent action, Action listener)
    {
        action.AddListener(listener);
    }

    public static void AddListener<T>(this UnityEvent<T> action, Action<T> listener)
    {
        action.AddListener(listener);
    }
}

#endif