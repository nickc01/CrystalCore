using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions.Must;
using UnityEngine.SceneManagement;
using WeaverCore.Attributes;
using WeaverCore.DataTypes;
using WeaverCore.Enums;
using WeaverCore.Features;
using WeaverCore.Interfaces;
using WeaverCore.Utilities;

/// <summary>
/// Contains information about a specific component's data in the object's hiearchy
/// </summary>
struct HierarchicalData
{
	/// <summary>
	/// Is the component enabled on the prefab?
	/// </summary>
	public bool ComponentEnabled;

	/// <summary>
	/// The prefab version of the component
	/// </summary>
	public Component PrefabComponent;
}

/// <summary>
/// Contains information about a specific component
/// </summary>
struct ComponentTypeData
{
	/// <summary>
	/// The Awake function on a component (if it has one)
	/// </summary>
	public Action<Component> AwakeFunction;

	/// <summary>
	/// The Start function on a component (if it has one)
	/// </summary>
	public Action<Component> StartFunction;

	/// <summary>
	/// A list of copiers need to copy all the data from one component to another. This is a list because it needs to account for base classes the component inherits from
	/// </summary>
	public List<FieldCopierBuilder<Component>.ShallowCopyDelegate> Copiers;
}

namespace WeaverCore
{
	/// <summary>
	/// A pool that can be used to store old instances of objects when not in use, and can be renabled if needed. 
	/// 
	/// This is done to reduce the amount of calls to GameObject.Instantiate and GameObject.Destroy
	/// 
	/// NOTE: Every object used through this pool must have a <see cref="PoolableObject"/> component attached
	/// </summary>
    public sealed class ObjectPool : MonoBehaviour
	{
		private struct StartCallerEntry
		{
			public readonly ComponentPath[] Components;
			public readonly Dictionary<Type, ComponentTypeData> ComponentData;

			public StartCallerEntry(ComponentPath[] components, Dictionary<Type, ComponentTypeData> componentData)
			{
				Components = components;
				ComponentData = componentData;
			}
		}

		static Cache<Type, FieldCopierBuilder<Component>.ShallowCopyDelegate> CopierCache = new Cache<Type, FieldCopierBuilder<Component>.ShallowCopyDelegate>();

		public const bool DEBUG = false;
		public const bool PREFAB_PROXY = false;

		/// <summary>
		/// Can the pool work with multiple threads?
		/// </summary>
		public const bool MultiThreaded = true;

		/// <summary>
		/// Is the pool ready for use?
		/// </summary>
		private bool poolAllSet = false;

		/// <summary>
		/// A list of all currently unused objects. When the user wants to instantiate a new object, it is pulled from this list
		/// </summary>
		private readonly Queue<PoolableObject> PooledObjects = new Queue<PoolableObject>();
		/// <summary>
		/// Contains reflection information about all component types on the prefab. This is used to reset all components on the object back to a clean slate when an object is returned to the pool
		/// </summary>
		private readonly Dictionary<Type, ComponentTypeData> ComponentData = new Dictionary<Type, ComponentTypeData>();

		/// <summary>
		/// Contains hiearchy information about a specific component. The key in the dictionary is a hash of where the component is in the hierarchy. This is used to link a component on an object to the component on the prefab
		/// </summary>
		private readonly Dictionary<int, HierarchicalData> HierarchyData = new Dictionary<int, HierarchicalData>();

		/// <summary>
		/// Is the prefab all set?
		/// </summary>
		bool prefabSet = false;
		PoolableObject _prefab;

		/// <summary>
		/// The prefab this pool is instantiating
		/// </summary>
		public PoolableObject Prefab
		{
			get
			{
				return _prefab;
			}
			set
			{
				if (prefabSet == false && _prefab == null)
				{
					_prefab = value;
					prefabSet = true;
					InitPool();
				}
				else
				{
					throw new Exception("The Prefab cannot be changed on the pool once it has already been set");
				}
			}
		}

		internal void RefreshPrefab(PoolableObject prefab)
		{
			_prefab = prefab;
        }

		/// <summary>
		/// The name given to each instance in the pool
		/// </summary>
		private string InstanceName;

		/// <summary>
		/// Should the pool reset all components on an object to a clean slate when returned back to the pool?
		/// </summary>
		public bool ResetComponents = true;

		/// <summary>
		/// Should the pool reset the transform data on an object back to a clean slate when returned back to the pool?
		/// </summary>
		public bool ResetPositions = false;

		/// <summary>
		/// Contains a list of Start() functions to be called
		/// </summary>
		private Queue<StartCallerEntry> StartCallers;

		public int AmountInPool
		{
			get
			{
				return PooledObjects.Count;
			}
		}

		/// <summary>
		/// In the LateUpdate function, call any Start() functions that need to be called from the <see cref="StartCallers"/> list. This is to replicate the behaviour where the Start() function on an object is always called a frame after the Awake() function
		/// </summary>
		private void LateUpdate()
		{
			if (StartCallers != null)
			{
				while (StartCallers.Count > 0)
				{
					StartCallerEntry caller = StartCallers.Dequeue();
					for (int i = 0; i < caller.Components.GetLength(0); i++)
					{
						ComponentPath componentPath = caller.Components[i];
						//Component component = componentPath.Component;
						//if (component != null)
						//{
							//Type cType = component.GetType();
							ComponentTypeData cData;

							if (caller.ComponentData.TryGetValue(componentPath.ComponentType, out cData))
							{
								if (componentPath.Component != null && cData.StartFunction != null)
								{
									cData.StartFunction(componentPath.Component);
								}
							}
						//}
					}
				}
			}
		}

		static void DebugPrint(string message)
		{
			if (DEBUG)
			{
				Debug.Log(message);
			}
		}

		void InitPool()
		{
			InstanceName = Prefab.gameObject.name + " (Clone)";

			ComponentPath[] components = Prefab.GetAllComponents();

			if (!MultiThreaded)
			{
				DebugPrint($"{name} Calling LoadPoolData Single-Threaded");
                LoadPoolData(components);
			}
			else
			{
                DebugPrint($"{name} Calling LoadPoolData Multi-Threaded");
				Task.Run(() => LoadPoolData(components));
                //ThreadPool.QueueUserWorkItem(LoadPoolData, components);
			}
		}

		/// <summary>
		/// Clears the pool of all it's stored objects
		/// </summary>
		public void ClearPool()
		{
            DebugPrint($"{name} Calling ClearPool");
            PooledObjects.Clear();
		}

		/// <summary>
		/// Creates a new pool for a prefab
		/// </summary>
		/// <param name="prefab">The prefab the pool will be instantiating</param>
		/// <param name="boundToScene">If true, this ObjectPool will be destroyed when the current scene changes</param>
		/// <returns>Returns a new pool for the prefab</returns>
		/// <exception cref="Exception">Throws if the prefab is null</exception>
		public static ObjectPool Create(GameObject prefab, bool boundToScene = false)
		{
			var poolComponent = prefab.GetComponent<PoolableObject>();
			if (poolComponent == null)
			{
				throw new Exception("The gameObject " + prefab.name + " does not have PoolableObject component attached");
			}
			return Create(poolComponent, boundToScene);
		}

        /// <summary>
        /// Creates a new pool for a prefab
        /// </summary>
        /// <param name="prefab">The prefab the pool will be instantiating</param>
        /// <param name="boundToScene">If true, this ObjectPool will be destroyed when the current scene changes</param>
        /// <returns>Returns a new pool for the prefab</returns>
        public static ObjectPool Create(PoolableObject prefab, bool boundToScene = false)
		{
			if (prefab == null)
			{
				throw new ArgumentNullException(nameof(prefab));
			}
			var pool = new GameObject().AddComponent<ObjectPool>();
			pool.gameObject.name = "Object Pool - " + prefab.name;
            pool.gameObject.hideFlags = HideFlags.HideInHierarchy;
            DebugPrint($"{pool.name} Creating Pool...");

            if (!boundToScene)
            {
                GameObject.DontDestroyOnLoad(pool.gameObject);
            }

            if (PREFAB_PROXY)
			{
                var sourcePrefabContainer = new GameObject("PREFAB_SOURCE_CONTAINER");
                sourcePrefabContainer.SetActive(false);
                sourcePrefabContainer.transform.SetParent(pool.transform);
                var sourcePrefab = GameObject.Instantiate(prefab, sourcePrefabContainer.transform);
                pool.Prefab = sourcePrefab;
            }
			else
			{
				pool.Prefab = prefab;
			}  
            DebugPrint($"{pool.name} Created Pool with prefab = " + pool.Prefab);
            return pool;
		}

        /// <summary>
        /// Creates a new pool for a prefab
        /// </summary>
        /// <param name="prefab">The prefab the pool will be instantiating</param>
        /// <param name="boundToScene">If true, this ObjectPool will be destroyed when the current scene changes</param>
        /// <returns>Returns a new pool for the prefab</returns>
        /// <exception cref="Exception">Throws if the prefab is null</exception>
        public static ObjectPool Create(Component prefab, bool boundToScene = false)
		{
			var poolComponent = prefab.GetComponent<PoolableObject>();
			if (poolComponent == null)
			{
				throw new Exception("The gameObject " + prefab.gameObject.name + " does not have PoolableObject component attached");
			}
			return Create(poolComponent, boundToScene);
		}


		/// <summary>
		/// Takes all the components from the prefab and uses them to build the <see cref="ComponentData"/> list and <see cref="HierarchicalData"/> list
		/// </summary>
		/// <param name="componentsRaw"></param>
		private void LoadPoolData(object componentsRaw)
		{
			try
			{
				if (!MultiThreaded)
				{
					WeaverLog.Log($"LOADING POOL DATA FOR {Prefab.name}");
				}
				ComponentPath[] components = (ComponentPath[])componentsRaw;
				//COMPONENTS CAN'T BE NULL HERE
				for (int i = 0; i < components.GetLength(0); i++)
				{
					ComponentPath componentPath = components[i];
					//COMPONENT MAYBE NULL?
					Type type = componentPath.ComponentType;

					//COMPONENTDATA CANT BE NULL
					if (!ComponentData.ContainsKey(type))
					{
						ComponentTypeData cData = new ComponentTypeData();
						if (typeof(MonoBehaviour).IsAssignableFrom(type))
						{
							if (type != typeof(PoolableObject))
							{
								Action<Component> awake = GetAwakeDelegate(type);
								if (awake != null)
								{
									cData.AwakeFunction = awake;
								}
								Action<Component> start = GetStartDelegate(type);
								if (start != null)
								{
									cData.StartFunction = start;
								}
							}

							cData.Copiers = new System.Collections.Generic.List<FieldCopierBuilder<Component>.ShallowCopyDelegate>();

							var currentType = type;

							while (currentType != null && currentType != typeof(Component))
							{
                                if (!MultiThreaded)
                                {
                                    WeaverLog.Log($"Starting field copier for {currentType.Name}");
                                }
                                cData.Copiers.Add(CreateFieldCopier(currentType));

                                if (!MultiThreaded)
                                {
                                    WeaverLog.Log($"Finished field copier for {currentType.Name}");
                                }
                                currentType = currentType.BaseType;
							}
						}
						ComponentData.Add(type, cData);
					}

					int hierarchyHash = CreateHierarchyHash(componentPath);
					if (!HierarchyData.ContainsKey(hierarchyHash))
					{
						HierarchicalData hData = new HierarchicalData();
						hData.ComponentEnabled = componentPath.Enabled;
						hData.PrefabComponent = componentPath.Component;
						HierarchyData.Add(hierarchyHash, hData);
					}
				}
				poolAllSet = true;
			}
			catch (Exception e)
			{
				Debug.LogError("Error Creating Pool");
				Debug.LogException(e);
			}
		}

		private static T GetComponentOrThrow<T>(GameObject obj)
		{
			if (obj == null)
			{
				throw new NullReferenceException("The object passed in is null");
			}
			var c = obj.GetComponent<T>();
			if (c == null)
			{
				throw new Exception("The prefab [" + obj.name + "] does not have a [ " + typeof(T).FullName + " ] component");
			}
			return c;
		}

		private static T GetComponentOrThrow<T>(Component obj)
		{
			if (obj == null)
			{
				throw new NullReferenceException("The object passed in is null");
			}
			var c = obj.GetComponent<T>();
			if (c == null)
			{
				throw new Exception("The prefab [" + obj.name + "] does not have a [ " + typeof(T).FullName + " ] component");
			}
			return c;
		}

		/// <summary>
		/// Generates a hash that identifies a component in the object's hierachy
		/// </summary>
		/// <param name="componentPath">The component to generate the hash for</param>
		private static int CreateHierarchyHash(ComponentPath componentPath)
		{
			int hierarchyHash = 0;
			Utilities.HashUtilities.AdditiveHash(ref hierarchyHash, componentPath.SiblingHash);
			Utilities.HashUtilities.AdditiveHash(ref hierarchyHash, componentPath.ComponentType.GetHashCode());
			return hierarchyHash;
		}

		/// <summary>
		/// Fills the pool with a certain amount of objects
		/// </summary>
		/// <param name="amount">The amount to fill the pool with</param>
		public void FillPool(int amount)
		{
			for (int i = 0; i < amount; i++)
			{
				PoolableObject obj = InstantiateNewObject();
				obj.startCalledAutomatically = true;
				obj.gameObject.SetActive(false);
			}
		}

		/// <summary>
		/// Fills the pool with a certain amount of objects. This function will add 1 object to the pool each frame to stagger instantiations
		/// </summary>
		/// <param name="amount">The amount to fill the pool with</param>
		public void FillPoolAsync(int amount)
		{
			if (amount > 0)
			{
				StartCoroutine(FillPoolRoutine(amount));
			}
		}

		private IEnumerator FillPoolRoutine(int amount)
		{
			for (int i = 0; i < amount; i++)
			{
				if (this != null)
				{
					PoolableObject obj = InstantiateNewObject();
					obj.startCalledAutomatically = true;
					obj.gameObject.SetActive(false);
					yield return null;
				}
			}
		}

		/// <summary>
		/// Returns an object to the pool
		/// </summary>
		/// <param name="gameObject">The object to return</param>
		public void ReturnToPool(GameObject gameObject)
		{
			ReturnToPool(gameObject, 0f);
		}

		/// <summary>
		/// Returns an object to the pool
		/// </summary>
		/// <param name="poolableObject">The object to return</param>
		public void ReturnToPool(PoolableObject poolableObject)
		{
			ReturnToPool(poolableObject, 0f);
		}

		/// <summary>
		/// Returns an object to the pool
		/// </summary>
		/// <typeparam name="T">The type of the component</typeparam>
		/// <param name="component">The object to return</param>
		public void ReturnToPool<T>(T component) where T : Component
		{
			ReturnToPool(component, 0f);
		}

		/// <summary>
		/// Returns an object to the pool
		/// </summary>
		/// <param name="gameObject">The object to return</param>
		/// <param name="time">The delay before the object is put back in the pool</param>
		/// <exception cref="Exception">Throws if the returning object doesn't have a <see cref="PoolableObject"/> component attached</exception>
		public void ReturnToPool(GameObject gameObject, float time)
		{
			if (gameObject == null)
			{
				return;
			}
			PoolableObject poolComponent = gameObject.GetComponent<PoolableObject>();
			if (poolComponent == null)
			{
				throw new Exception("The object is not poolable. It does not contain a PoolableObject component");
			}
			ReturnToPool(poolComponent, time);
		}

		/// <summary>
		/// Returns an object to the pool
		/// </summary>
		/// <param name="poolableObject">The object to return</param>
		/// <param name="time">The delay before the object is put back in the pool</param>
		/// <exception cref="Exception">Throws if the returning object doesn't have a <see cref="PoolableObject"/> component attached</exception>
		public void ReturnToPool(PoolableObject poolableObject, float time)
		{
			if (poolableObject == null)
			{
				return;
			}
			if (poolableObject.SourcePool != this)
			{
				throw new Exception("The passed in object did not originate from this pool");
			}

			if (DEBUG)
			{
                if (time > 0f)
                {
                    DebugPrint($"{name} Returning {poolableObject.name} back to pool in {time} seconds");
                }
                else
                {
                    DebugPrint($"{name} Returning {poolableObject.name} back to pool");
                }
            }

            if (time > 0)
			{
				StartCoroutine(ReturnToPoolRoutine(poolableObject, time));
			}
			else
			{

				SendBackToPool(poolableObject);
			}
		}

		/// <summary>
		/// Sends an object back into the pool and resets all components on the object back to what they originally were.
		/// </summary>
		/// <param name="poolableObject">The object being returned</param>
		private void SendBackToPool(PoolableObject poolableObject)
		{
			if (!poolAllSet)
			{
				UnityEngine.Object.Destroy(poolableObject.gameObject);
				return;
			}

			if (poolableObject.TryGetComponent<Enemy>(out var enemy))
			{
				enemy.StopAllBoundRoutines();
			}


			ComponentPath[] objComponents = poolableObject.GetAllComponents();
			ComponentPath[] prefabComponents = Prefab.GetAllComponents();

			HashSet<int> PositionsSet = null;

			if (ResetPositions)
			{
				PositionsSet = new HashSet<int>();
			}

			for (int i = 0; i < objComponents.GetLength(0); i++)
			{
				ComponentPath componentPath = objComponents[i];
				Component component = componentPath.Component;
				Type type = componentPath.ComponentType;

				if (component is IOnPool && component != null)
				{
					((IOnPool)component).OnPool();
				}
				if (component is MonoBehaviour && component != null)
				{
					((MonoBehaviour)component).StopAllCoroutines();
				}

				int hash = CreateHierarchyHash(componentPath);
				HierarchicalData hData;
				if (HierarchyData.TryGetValue(hash, out hData))
				{
					if (component is Behaviour)
					{
						((Behaviour)component).enabled = hData.ComponentEnabled;
					}

					ComponentTypeData cData;
					if (ComponentData.TryGetValue(type, out cData))
					{
						if (cData.Copiers != null)
						{
							for (int u = 0; u < cData.Copiers.Count; u++)
							{
								cData.Copiers[u](component, hData.PrefabComponent);
							}
						}
					}

					if (ResetPositions)
					{
						Transform t = component.transform;
						if (PositionsSet.Add(t.GetInstanceID()))
						{
							Transform prefabT = hData.PrefabComponent.transform;
							t.SetPositionAndRotation(prefabT.position, prefabT.rotation);
							t.localScale = prefabT.localScale;
						}
					}

				}
			}
			poolableObject.InPool = true;
			poolableObject.gameObject.SetActive(false);
			poolableObject.transform.SetParent(transform);
			PooledObjects.Enqueue(poolableObject);
		}

		private PoolableObject InstantiateInternal(Vector3 position, Quaternion rotation, Transform parent)
		{
            PoolableObject obj = null;
			if (poolAllSet)
			{
				while (PooledObjects.Count > 0 && obj == null)
				{
					obj = PooledObjects.Dequeue();
				}
			}

			//If there was a valid object in the queue
			if (obj != null && poolAllSet)
			{
				if (obj.TryGetComponent<Enemy>(out var enemy))
				{
					enemy.StopAllBoundRoutines();
				}
				//WeaverLog.Log(obj);
                DebugPrint($"Spawning Object from pool... {Prefab}");
                obj.gameObject.name = InstanceName;
				obj.SourcePool = this;
				obj.InPool = false;
				Transform t = obj.transform;
				t.SetParent(parent);
				t.position = position;
				t.rotation = rotation;
				obj.gameObject.SetActive(Prefab.gameObject.activeSelf);

				ComponentPath[] objComponents = obj.GetAllComponents();

				for (int i = 0; i < objComponents.GetLength(0); i++)
				{
					ComponentPath componentPath = objComponents[i];
					Component component = componentPath.Component;
					//Type cType = component.GetType();

					ComponentTypeData cData;
					if (ComponentData.TryGetValue(componentPath.ComponentType, out cData))
					{
						if (cData.AwakeFunction != null)
						{
							cData.AwakeFunction(component);
						}
					}
				}
				if (obj.startCalledAutomatically)
				{
					obj.startCalledAutomatically = false;
				}
				else
				{
					AddStartCallerEntry(objComponents, ComponentData);
				}
                DebugPrint($"{name} Spawned Object from pool");
                return obj;
			}
			else
			{
                DebugPrint($"{name} Instantiating new object... {Prefab}");
                obj = UnityEngine.Object.Instantiate(Prefab, position, rotation, parent);
				obj.SourcePool = this;
				obj.InPool = false;
				obj.gameObject.name = InstanceName;
				Transform t = obj.transform;
				t.SetParent(parent);
				t.position = position;
				t.rotation = rotation;
                DebugPrint($"{name} Instantiated new object");
                return obj;
			}
		}

		/// <summary>
		/// Instantiates a new object from the pool
		/// </summary>
		/// <param name="position">The position of the instantiated object</param>
		/// <param name="rotation">The rotation of the instantiated object</param>
		/// <param name="parent">The parent transform of the instantiated object</param>
		/// <returns>The new object instance</returns>
		public GameObject Instantiate(Vector3 position, Quaternion rotation, Transform parent)
		{
			return InstantiateInternal(position, rotation, parent).gameObject;
		}

		/// <summary>
		/// Instantiates a new object from the pool
		/// </summary>
		/// <param name="parent">The parent transform of the instantiated object</param>
		/// <returns>The new object instance</returns>
		public GameObject Instantiate(Transform parent)
		{
			return InstantiateInternal(Prefab.transform.position, Prefab.transform.rotation, parent).gameObject;
		}

		/// <summary>
		/// Instantiates a new object from the pool
		/// </summary>
		/// <returns>The new object instance</returns>
		public GameObject Instantiate()
		{
			return InstantiateInternal(Prefab.transform.position, Prefab.transform.rotation, null).gameObject;
		}

		/// <summary>
		/// Instantiates a new object from the pool
		/// </summary>
		/// <param name="position">The position of the instantiated object</param>
		/// <param name="rotation">The rotation of the instantiated object</param>
		/// <returns>The new object instance</returns>
		public GameObject Instantiate(Vector3 position, Quaternion rotation)
		{
			return InstantiateInternal(position, rotation, null).gameObject;
		}

		/// <summary>
		/// Instantiates a new object from the pool
		/// </summary>
		/// <param name="parent">The parent transform of the instantiated object</param>
		/// <param name="instantiateInWorldSpace">Should the object be positioned in world space? If false, it will be positioned relative to the parent</param>
		/// <returns>The new object instance</returns>
		public GameObject Instantiate(Transform parent, bool instantiateInWorldSpace)
		{
			if (instantiateInWorldSpace)
			{
				return InstantiateInternal(Prefab.transform.position, Prefab.transform.rotation, parent).gameObject;
			}
			else
			{
				return InstantiateInternal(Prefab.transform.position + parent.transform.position, parent.transform.rotation * Prefab.transform.rotation, parent).gameObject;
			}
		}

		/// <summary>
		/// Instantiates a new object from the pool
		/// </summary>
		/// <typeparam name="T">The component type to get from the instantiated object</typeparam>
		/// <param name="position">The position of the instantiated object</param>
		/// <param name="rotation">The rotation of the instantiated object</param>
		/// <param name="parent">The parent transform of the instantiated object</param>
		/// <returns>The new object instance</returns>
		/// <exception cref="Exception">Throws if the component type <typeparamref name="T"/> doesn't exist on the instantiated object</exception>
		public T Instantiate<T>(Vector3 position, Quaternion rotation, Transform parent)
		{
			var instance = InstantiateInternal(position, rotation, parent);
			var component = instance.GetCommonComponent<T>();
			if (component == null)
			{
				throw new Exception("The objects in this pool do not have a " + typeof(T).FullName + " component");
			}
			return component;
		}

		/// <summary>
		/// Instantiates a new object from the pool
		/// </summary>
		/// <typeparam name="T">The component type to get from the instantiated object</typeparam>
		/// <param name="parent">The parent transform of the instantiated object</param>
		/// <returns>The new object instance</returns>
		public T Instantiate<T>(Transform parent)
		{
			return Instantiate<T>(Prefab.transform.position, Prefab.transform.rotation, parent);
		}

		/// <summary>
		/// Instantiates a new object from the pool
		/// </summary>
		/// <typeparam name="T">The component type to get from the instantiated object</typeparam>
		/// <returns>The new object instance</returns>
		public T Instantiate<T>()
		{
			return Instantiate<T>(Prefab.transform.position, Prefab.transform.rotation, null);
		}

		/// <summary>
		/// Instantiates a new object from the pool
		/// </summary>
		/// <typeparam name="T">The component type to get from the instantiated object</typeparam>
		/// <param name="position">The position of the instantiated object</param>
		/// <param name="rotation">The rotation of the instantiated object</param>
		/// <returns>The new object instance</returns>
		public T Instantiate<T>(Vector3 position, Quaternion rotation)
		{
			return Instantiate<T>(position, rotation, null);
		}

		/// <summary>
		/// Instantiates a new object from the pool
		/// </summary>
		/// <typeparam name="T">The component type to get from the instantiated object</typeparam>
		/// <param name="parent">The parent transform of the instantiated object</param>
		/// <param name="instantiateInWorldSpace">Should the object be positioned in world space? If false, it will be positioned relative to the parent</param>
		/// <returns>The new object instance</returns>
		public T Instantiate<T>(Transform parent, bool instantiateInWorldSpace)
		{
			if (instantiateInWorldSpace)
			{
				return Instantiate<T>(Prefab.transform.position, Prefab.transform.rotation, parent);
			}
			else
			{
				return Instantiate<T>(Prefab.transform.position + parent.transform.position, parent.transform.rotation * Prefab.transform.rotation, parent);
			}
		}

		void AddStartCallerEntry(ComponentPath[] componentsToCall, Dictionary<Type, ComponentTypeData> componentData)
		{
			if (StartCallers == null)
			{
				StartCallers = new Queue<StartCallerEntry>();
			}
			StartCallers.Enqueue(new StartCallerEntry(componentsToCall, componentData));
		}

		private IEnumerator ReturnToPoolRoutine(PoolableObject poolableObject, float time)
		{
			for (float i = 0; i < time; i += Time.deltaTime)
			{
				if (poolableObject == null || poolableObject.gameObject == null || poolableObject.InPool)
				{
					yield break;
				}
				yield return null;
			}
			SendBackToPool(poolableObject);
		}

		/// <summary>
		/// Returns an object back to the pool
		/// </summary>
		/// <typeparam name="T">The type of the component</typeparam>
		/// <param name="component">The component on the object being returned</param>
		/// <param name="time">A delay before the object is returned to the pool</param>
		/// <exception cref="Exception">Throws if the returning object doesn't have a <see cref="PoolableObject"/> component attached</exception>
		public void ReturnToPool<T>(T component, float time) where T : Component
		{
			if (component == null)
			{
				return;
			}

			PoolableObject obj = null;
			if (component is PoolableObject)
			{
				obj = component as PoolableObject;
			}
			else
			{
				obj = component.GetComponent<PoolableObject>();
			}
			if (obj == null)
			{
				throw new Exception("The object is not poolable. It does not contain a PoolableObject component");
			}
			ReturnToPool(obj, time);
		}

		/// <summary>
		/// Creates a function that will copy the fields of one component to another of the same type. This is used to reset a component back to what it was on the prefab
		/// </summary>
		/// <param name="componentType">The type of component to create the function for</param>
		/// <returns>Returns a function that will reset the fields of a component to another</returns>
		private FieldCopierBuilder<Component>.ShallowCopyDelegate CreateFieldCopier(Type componentType)
		{
			{
				FieldCopierBuilder<Component>.ShallowCopyDelegate func;
				if (CopierCache.GetCachedObject(componentType, out func))
				{
					return func;
				}
			}

			FieldCopierBuilder<Component> copier = new FieldCopierBuilder<Component>(componentType);

            foreach (FieldInfo field in componentType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                if (!field.IsDefined(typeof(ExcludeFieldFromPoolAttribute), false) && !field.IsInitOnly && !field.IsLiteral)
                {
                    if (field.FieldType.IsValueType || field.FieldType.IsEnum)
                    {
                        if (!MultiThreaded)
                        {
                            WeaverLog.Log($"Adding Field {field.Name}");
                        }
                        copier.AddField(field);
                    }
                    else if (field.FieldType.IsClass && (field.IsPublic || field.IsDefined(typeof(SerializeField), true)))
                    {
                        if (!typeof(Component).IsAssignableFrom(field.FieldType) && !typeof(GameObject).IsAssignableFrom(field.FieldType))
                        {
                            if (!MultiThreaded)
                            {
                                WeaverLog.Log($"Adding Field {field.Name}");
                            }
                            copier.AddField(field);
                        }
                    }
                }
            }

            var finalFunc = copier.Finish();
			CopierCache.CacheObject(componentType, finalFunc);
			return finalFunc;
		}

		/// <summary>
		/// Gets the Awake() method on a type (if it has one)
		/// </summary>
		/// <param name="sourceType">The type to find the function on</param>
		private static Action<Component> GetAwakeDelegate(Type sourceType)
		{
			return GetDelegateForMethod(sourceType, sourceType.GetMethod("Awake", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, CallingConventions.HasThis, new Type[] { }, null));
		}

		/// <summary>
		/// Gets the Start() method on a type (if it has one)
		/// </summary>
		/// <param name="sourceType">The type to find the function on</param>
		private static Action<Component> GetStartDelegate(Type sourceType)
		{
			return GetDelegateForMethod(sourceType, sourceType.GetMethod("Start", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, CallingConventions.HasThis, new Type[] { }, null));
		}

		private static Action<Component> GetDelegateForMethod(Type sourceType, MethodInfo method)
		{
			if (method == null)
			{
				return null;
			}
#if NET_4_6
			DynamicMethod awakeCaller = new DynamicMethod("", MethodAttributes.Public | MethodAttributes.Static, CallingConventions.Standard, null, new Type[1] { typeof(Component) }, sourceType, true);
			ILGenerator gen = awakeCaller.GetILGenerator();
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Castclass, sourceType);
			if (!sourceType.IsSealed && method.IsVirtual && !method.IsFinal)
			{

				gen.EmitCall(OpCodes.Call, method, null);
			}
			else
			{
				gen.EmitCall(OpCodes.Callvirt, method, null);
			}
			gen.Emit(OpCodes.Ret);
			return (Action<Component>)awakeCaller.CreateDelegate(typeof(Action<Component>));
#else
			return (c) =>
			{
				method.Invoke(c, null);
			};
#endif
		}

		private PoolableObject InstantiateNewObject()
		{
			PoolableObject instance = UnityEngine.Object.Instantiate(Prefab, transform);
			instance.gameObject.name = InstanceName;
			PooledObjects.Enqueue(instance);
			return instance;
		}
	}

}
