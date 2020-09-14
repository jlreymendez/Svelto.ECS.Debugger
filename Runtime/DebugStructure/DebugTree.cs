using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Svelto.DataStructures;
using Svelto.ECS.Internal;
using UnityEngine;

namespace Svelto.ECS.Debugger.DebugStructure
{
    public class DebugTree
    {
        public List<DebugRoot> DebugRoots = new List<DebugRoot>();

        public delegate void UpdateHandler();
        public event UpdateHandler OnUpdate;

        public DebugRoot AddRootToTree(EnginesRoot root)
        {
            var debugRoot = new DebugRoot(root);
            DebugRoots.Add(debugRoot);
            return debugRoot;
        }

        public void RemoveRootFromTree(EnginesRoot root)
        {
            DebugRoots.RemoveAll(debug => debug.EnginesRoot == root);
        }

        public void Update()
        {
            foreach (var debugRoot in DebugRoots)
            {
                debugRoot.Process();
            }
            OnUpdate?.Invoke();
        }

        public void Clear()
        {
            DebugRoots.Clear();
        }
    }

    public class DebugRoot
    {
        #region Static

        private static FieldInfo EnginesField;
        private static FieldInfo EntityDBField;
        private static FieldInfo EgidMapField;

        static DebugRoot()
        {
            var typeFields = typeof(EnginesRoot).GetAllFields().ToList();
            EnginesField = typeFields.First(f => f.Name == "_enginesSet");
            EntityDBField = typeFields.First(f => f.Name == "_groupEntityComponentsDB");
            EgidMapField = typeFields.First(f => f.Name == "_egidToLocatorMap");
        }

        #endregion

        public EnginesRoot EnginesRoot;
        public FasterDictionary<uint, FasterDictionary<RefWrapper<Type>, ITypeSafeDictionary>> Root;
        public FasterDictionary<uint, FasterDictionary<uint, EntityLocator>> LocatorMap;
        public FasterList<IEngine> Engines;
        public List<DebugGroup> DebugGroups = new List<DebugGroup>();

        public DebugRoot(EnginesRoot root)
        {
            EnginesRoot = root;
            Engines = (FasterList<IEngine>) EnginesField.GetValue(root);
            Root = (FasterDictionary<uint, FasterDictionary<RefWrapper<Type>, ITypeSafeDictionary>>) EntityDBField.GetValue(root);
            LocatorMap = (FasterDictionary<uint, FasterDictionary<uint, EntityLocator>>) EgidMapField.GetValue(root);
            Process();
        }

        public void Process()
        {
            DebugGroups.Clear();
            var enu = Root.GetEnumerator();
            while (enu.MoveNext())
            {
                var current = enu.Current;
                var key = current.Key;
                var val = current.Value;
                DebugGroups.Add(new DebugGroup(key, val, this));
            }
        }
    }

    public class DebugGroup
    {
        public DebugRoot Parent;
        public uint Id;
        private FasterDictionary<RefWrapper<Type>, ITypeSafeDictionary> GroupDB;
        FasterDictionary<uint, FasterDictionary<uint, EntityLocator>> EgidToLocatorMap;

        public List<DebugEntity> DebugEntities = new List<DebugEntity>();

        public DebugGroup(uint key, FasterDictionary<RefWrapper<Type>, ITypeSafeDictionary> val, DebugRoot debugRoot)
        {
            Id = key;
            GroupDB = val;
            Parent = debugRoot;
            Process();
        }

        public void Process()
        {
            if (Parent.LocatorMap.TryGetValue(Id, out var groupMap) == false) return;

            foreach (var entityStructs in GroupDB)
            {
                var valTypeSafe = entityStructs.Value;

                var methods = valTypeSafe.GetType().GetMethods();
                var tryGetValueMethod = methods.First(s => s.Name == "TryGetValue");


                foreach (var groupMapEntry in groupMap)
                {
                    var parameters = new object[] { groupMapEntry.Key, null };
                    var hasValue = (bool)tryGetValueMethod.Invoke(valTypeSafe, parameters);
                    if (hasValue)
                    {
                        var entity = GetOrAddEntity(groupMapEntry.Key);
                        entity.AddStruct(parameters[1]);
                    }
                }
            }
        }

        private DebugEntity GetOrAddEntity(uint key)
        {
            var entity = DebugEntities.Find(f => f.Id == key);
            if (entity == null)
            {
                entity = new DebugEntity(key);
                DebugEntities.Add(entity);
            }
            return entity;
        }
    }

    public class DebugEntity
    {
        public uint Id;

        public List<DebugStruct> DebugStructs = new List<DebugStruct>();

        public DebugEntity(uint key)
        {
            Id = key;
        }

        public void AddStruct(object value)
        {
            DebugStructs.Add(new DebugStruct(value));
        }
    }

    [System.Serializable]
    public class DebugStruct
    {
        public object Value;

        public DebugStruct(object value)
        {
            Value = value;
        }
    }
}