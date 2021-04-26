using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace UnityExplorer.UI.ObjectPool
{
    public interface IObjectPool { }

    public class Pool<T> : IObjectPool where T : IPooledObject
    {
        // internal pool management

        private static readonly Dictionary<Type, IObjectPool> pools = new Dictionary<Type, IObjectPool>();

        public static Pool<T> GetPool()
        {
            var type = typeof(T);
            if (!pools.ContainsKey(type))
                CreatePool();
            return (Pool<T>)pools[type];
        }

        private static Pool<T> CreatePool()
        {
            var pool = new Pool<T>();
            pools.Add(typeof(T), pool);
            return pool;
        }

        public static T Borrow()
        {
            return GetPool().BorrowObject();
        }

        public static void Return(T obj)
        {
            GetPool().ReturnObject(obj);
        }

        // Instance

        public static Pool<T> Instance
        {
            get => s_instance ?? CreatePool();
        }
        private static Pool<T> s_instance;

        public Pool()
        {
            s_instance = this;

            ExplorerCore.LogWarning("Creating Pool<" + typeof(T).Name + ">");

            InactiveHolder = new GameObject($"InactiveHolder_{typeof(T).Name}");
            InactiveHolder.transform.parent = UIManager.PoolHolder.transform;
            InactiveHolder.hideFlags |= HideFlags.HideAndDontSave;
            InactiveHolder.SetActive(false);

            // Create an instance (not content) to grab the default height.
            // Tiny bit wasteful, but not a big deal, only happens once per type 
            // and its just the C# wrapper class being created.
            var obj = (T)Activator.CreateInstance(typeof(T));
            DefaultHeight = obj.DefaultHeight;
        }

        public GameObject InactiveHolder { get; }
        public float DefaultHeight { get; }

        private readonly HashSet<T> available = new HashSet<T>();
        private readonly HashSet<T> borrowed = new HashSet<T>();

        public int AvailableObjects => available.Count;

        private void IncrementPool()
        {
            var obj = (T)Activator.CreateInstance(typeof(T));
            obj.CreateContent(InactiveHolder);
            available.Add(obj);
        }

        public T BorrowObject()
        {
            if (available.Count <= 0)
                IncrementPool();

            var obj = available.First();
            available.Remove(obj);
            borrowed.Add(obj);

            return obj;
        }

        public void ReturnObject(T obj)
        {
            if (!borrowed.Contains(obj))
                ExplorerCore.LogWarning($"Returning an item to object pool ({typeof(T).Name}) but the item didn't exist in the borrowed list?");
            else
                borrowed.Remove(obj);

            available.Add(obj);
            obj.UIRoot.transform.SetParent(InactiveHolder.transform, false);
        }
    }
}
