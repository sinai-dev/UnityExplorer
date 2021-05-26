using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace UnityExplorer.UI.Models
{
    // Abstract non-generic class, handles the pool dictionary and interfacing with the generic pools.
    public abstract class Pool
    {
        protected static readonly Dictionary<Type, Pool> pools = new Dictionary<Type, Pool>();

        public static Pool GetPool(Type type)
        {
            if (!pools.TryGetValue(type, out Pool pool))
                pool = CreatePool(type);
            return pool;
        }

        protected static Pool CreatePool(Type type)
        {
            Pool pool = (Pool)Activator.CreateInstance(typeof(Pool<>).MakeGenericType(new[] { type }));
            pools.Add(type, pool);
            return pool;
        }

        public static IPooledObject Borrow(Type type)
        {
            return GetPool(type).TryBorrow();
        }

        public static void Return(Type type, IPooledObject obj)
        {
            GetPool(type).TryReturn(obj);
        }

        protected abstract IPooledObject TryBorrow();
        protected abstract void TryReturn(IPooledObject obj);
    }

    // Each generic implementation has its own pool, business logic is here
    public class Pool<T> : Pool where T : IPooledObject
    {
        public static Pool<T> GetPool() => (Pool<T>)GetPool(typeof(T));

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
            get => s_instance ?? (Pool<T>)CreatePool(typeof(T));
        }
        private static Pool<T> s_instance;

        public Pool()
        {
            s_instance = this;

            //ExplorerCore.LogWarning("Creating Pool<" + typeof(T).Name + ">");

            InactiveHolder = new GameObject($"PoolHolder_{typeof(T).Name}");
            InactiveHolder.transform.parent = UIManager.PoolHolder.transform;
            InactiveHolder.hideFlags |= HideFlags.HideAndDontSave;
            InactiveHolder.SetActive(false);

            // Create an instance (not content) to grab the default height
            var obj = (T)Activator.CreateInstance(typeof(T));
            DefaultHeight = obj.DefaultHeight;
        }

        public GameObject InactiveHolder { get; }
        public float DefaultHeight { get; }

        private readonly HashSet<T> available = new HashSet<T>();
        private readonly HashSet<T> borrowed = new HashSet<T>();

        public int AvailableCount => available.Count;

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

        protected override IPooledObject TryBorrow() => Borrow();

        protected override void TryReturn(IPooledObject obj) => Return((T)obj);
    }
}
