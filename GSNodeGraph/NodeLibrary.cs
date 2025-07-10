// Copyright Gradientspace Corp. All Rights Reserved.
using Gradientspace.NodeGraph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace Gradientspace.NodeGraph
{
    public class NodeLibrary
    {
        public struct NodeTypeInfo
        {
            public NodeType nodeType;
            public bool bIsSystemNode = false;
            public NodeTypeInfo(NodeType type) { nodeType = type; }

            public List<string>? MappedClassTypes = null;
            public List<string>? MappedVariants = null;
        }
        public List<NodeTypeInfo> Library;

        // do these need to be accessed via Interlocked or some Atomic mechanism??
        bool IsLibraryBuilt = false;
        protected Task? WaitForLibraryBuildTask = null;
        protected object lockObj = new object();

        public NodeLibrary(bool bLaunchAutoBackgroundBuild = false) 
        {
            Library = new List<NodeTypeInfo>();

			if (bLaunchAutoBackgroundBuild)
				begin_library_build();
        }

		public virtual void Build()
		{
			begin_library_build();
			wait_for_library_build();
		}
		public virtual void BuildAsync()
		{
			begin_library_build();
		}


		public virtual IEnumerable<NodeType> EnumerateAllNodes()
        {
			wait_for_library_build();

			foreach (NodeTypeInfo info in Library) {
                if (info.bIsSystemNode == false )
                    yield return info.nodeType;
            }
        }


        public virtual IEnumerable<NodeType> EnumerateAllNodes(Predicate<NodeType> NodeFilter)
        {
			wait_for_library_build();

			foreach (NodeTypeInfo info in Library)
            {
                if (info.bIsSystemNode == false &&  NodeFilter(info.nodeType) == true )
                    yield return info.nodeType;
            }
        }

        public virtual IEnumerable<NodeType> EnumerateAllNodesWithFirstPinType(Type firstInputPinType)
        {
			wait_for_library_build();

			foreach (NodeTypeInfo info in Library)
            {
                if (info.nodeType.NodeArchetype == null || info.bIsSystemNode) continue;

                foreach (INodeInputInfo inputInfo in info.nodeType.NodeArchetype.EnumerateInputs())
                {
                    if (inputInfo.DataType.DataType == firstInputPinType)
                        yield return info.nodeType;
                    break;
                }
            }
        }


        public virtual NodeType? FindNodeType(Type ClassType)
        {
            return (ClassType.FullName != null) ? FindNodeType(ClassType.FullName!, "") : null;
        }

        public virtual NodeType? FindNodeType(string ClassName, string Variant)
        {
            wait_for_library_build();

			// todo this will become expensive and maybe we should construct some kind of dictionary/hash?

			foreach (NodeTypeInfo info in Library)
            {
                bool bClassTypeMatch = (info.nodeType.ClassType.FullName == ClassName);
                if (!bClassTypeMatch && info.MappedClassTypes != null && info.MappedClassTypes.Contains(ClassName))
                    bClassTypeMatch = true;
                if (!bClassTypeMatch) continue;

                if (info.nodeType.Variant == Variant)
                    return info.nodeType;

                if (info.MappedVariants != null && info.MappedVariants.Contains(Variant))
                    return info.nodeType;
            }

            return null;
        }







        protected virtual void begin_library_build()
        {
            lock(lockObj) { 
                if (WaitForLibraryBuildTask != null || IsLibraryBuilt)
                    return;
			    WaitForLibraryBuildTask = Task.Run(run_library_build);
            }
		}
		protected virtual void wait_for_library_build()
        {
            if (IsLibraryBuilt)     // fast early-out to avoid always locking on all library access
                return;
            lock (lockObj) {
                if (IsLibraryBuilt)     // other thread may have set to true outside of lock
                    return;
                Debug.Assert(WaitForLibraryBuildTask != null);
                WaitForLibraryBuildTask?.Wait();
                WaitForLibraryBuildTask = null;
				IsLibraryBuilt = true;
            }
        }
		protected virtual void run_library_build()
        {
            Debug.Assert(Library.Count == 0);       // can only call once, for now...

            Type BaseNodeType = typeof(NodeBase);

			Stopwatch watch = new Stopwatch();
			watch.Start();

			List<Assembly> TryAssemblies = FindPotentialNodeAssemblies();

			int FoundNodeClasses = 0;
            int FoundFunctionLibraries = 0;
            int FoundNodeFunctions = 0;

			Action<Type> processNodeType = (type) => {
				if (AddPotentialNodeToLibrary(type))
					Interlocked.Increment(ref FoundNodeClasses);
			};
			Action<(Type, string)> processLibraryType = (values) => {
				Interlocked.Increment(ref FoundFunctionLibraries);
				int Functions = AddFunctionLibraryToLibrary(values.Item1, values.Item2);
				Interlocked.Add(ref FoundNodeFunctions, Functions);
			};

            // seems to be no benefit from parallel here? but takes about 1s even for small library...what is the cost?
            // (possibly just loading assemblies??)

            foreach (Assembly assembly in TryAssemblies) { 
            //Parallel.ForEach<Assembly>(TryAssemblies, (assembly) => {

                // filter types in this assembly
                List<Type> nodeTypes = new List<Type>();
                List<(Type, string)> libraryTypes = new List<(Type, string)>();
                foreach (Type type in assembly.GetTypes()) {
                    if (type.IsSubclassOf(BaseNodeType) && type.IsAbstract == false)
                        nodeTypes.Add(type);
                    if (IsFunctionLibraryClass(type, out string LibraryNamespace))
                        libraryTypes.Add(new(type, LibraryNamespace));
                }

                //Parallel.ForEach<Type>(nodeTypes, processNodeType);
                foreach (Type type in nodeTypes)
                    processNodeType(type);

                //Parallel.ForEach<(Type, string)>(libraryTypes, processLibraryType);
                foreach ((Type type, string Namespace) in libraryTypes)
                    processLibraryType((type, Namespace));

            }//);

            watch.Stop();
			GlobalGraphOutput.AppendLog($"[NodeLibrary.Build] Found {FoundNodeClasses} Node Classes, {FoundFunctionLibraries} Function Libraries with {FoundNodeFunctions} Functions in {watch.Elapsed.TotalSeconds}s");
		}



        protected virtual List<Assembly> FindPotentialNodeAssemblies()
        {
			List<Assembly> AllAssemblies = new List<Assembly>(AppDomain.CurrentDomain.GetAssemblies());

			// filter out known system assemblies
			List<Assembly> TryAssemblies = new List<Assembly>();
			foreach (Assembly assembly in AllAssemblies)
			{
				if (assembly.FullName?.StartsWith("System.", StringComparison.InvariantCultureIgnoreCase) ?? true)
					continue;
				if (assembly.FullName?.StartsWith("Microsoft.", StringComparison.InvariantCultureIgnoreCase) ?? true)
					continue;

				// libraries used to build apps...can we identify these some other way?
				if (assembly.FullName?.StartsWith("Avalonia.", StringComparison.InvariantCultureIgnoreCase) ?? true)
					continue;
				if (assembly.FullName?.StartsWith("SkiaSharp", StringComparison.InvariantCultureIgnoreCase) ?? true)
					continue;
				if (assembly.FullName?.StartsWith("HarfBuzz", StringComparison.InvariantCultureIgnoreCase) ?? true)
					continue;
				if (assembly.FullName?.StartsWith("Python", StringComparison.InvariantCultureIgnoreCase) ?? true)
					continue;
				if (assembly.FullName?.StartsWith("geometry3Sharp", StringComparison.InvariantCultureIgnoreCase) ?? true)
					continue;

				TryAssemblies.Add(assembly);
			}

            return TryAssemblies;
		}



        protected virtual bool AddPotentialNodeToLibrary(Type type)
        {
            if (type.GetCustomAttribute<ClassHierarchyNode>() != null)
                return false;

            bool bIsSystemNode = (type.GetCustomAttribute<SystemNode>() != null);

            // todo much more filtering here...

            string UseName = type.Name;
            foreach (GraphNodeUIName UINameAttrib in type.GetCustomAttributes<GraphNodeUIName>()) {
                if (UINameAttrib.UIName.Length > 0)
                    UseName = UINameAttrib.UIName;
            }

            try
            {
                INode? nodeArchetype = Activator.CreateInstance(type) as INode;
                Debug.Assert(nodeArchetype != null);
                if (nodeArchetype != null)
                {
                    NodeType nodeType = new NodeType(type, UseName);
                    nodeType.NodeArchetype = nodeArchetype;

                    if (GetNodeNamespace(type, out string Namespace))
                        nodeType.UICategory = Namespace;

                    NodeTypeInfo typeInfo = new NodeTypeInfo(nodeType);
                    typeInfo.bIsSystemNode = bIsSystemNode;

                    // todo support additional class name remapping options (currently only full names supported)
                    AddMappedNodeVariantNames(ref typeInfo);

                    lock (Library) {
                        Library.Add(typeInfo);
                    }
                }
            } catch (Exception ex) {
                Debug.WriteLine("[NodeLibrary.Build] caught exception trying to build node of type " + type.Name + " : " + ex.Message);
#if DEBUG
                Debugger.Break();
#endif  
                return false;
            }
            return true;
        }


        protected virtual int AddFunctionLibraryToLibrary(Type type, string Namespace)
        {
			List<string>? AdditionalLibraryNames = CollectMappedLibraryNames(type);

            int FunctionCount = 0;
			foreach (MethodInfo methodInfo in type.GetMethods())
			{
				if (methodInfo.IsStatic == false) continue;

				Attribute? isNodeFunction = methodInfo.GetCustomAttribute(typeof(NodeFunction));
				if (isNodeFunction == null) continue;

				string NodeName = methodInfo.GetCustomAttribute<NodeFunctionUIName>()?.UIName ?? methodInfo.Name;

				try
				{
					Type functionNodeClassType = typeof(LibraryFunctionNode);
					INode? nodeArchetype = null;
					// disabling this for now...
					//if (methodInfo.ReturnType == typeof(void))
					//{
					//    functionNodeClassType = typeof(LibraryFunctionSinkNode);
					//    nodeArchetype = new LibraryFunctionSinkNode(type, methodInfo, PublicName);
					//}
					//else
					{
						nodeArchetype = new LibraryFunctionNode(type, methodInfo, NodeName);
					}
					NodeType nodeType = new NodeType(functionNodeClassType, NodeName);
					nodeType.NodeArchetype = nodeArchetype;
					nodeType.UICategory = Namespace;
					nodeType.Variant = Namespace + "." + methodInfo.Name;
					NodeTypeInfo nodeTypeInfo = new NodeTypeInfo(nodeType);

					// temporary code to correct an early error where LibraryFunctionNode was in the wrong namespace :(
					//nodeTypeInfo.MappedClassTypes = new List<string>();
					//nodeTypeInfo.MappedClassTypes.Add("GSNodeGraph.LibraryFunctionNode");

					AddMappedFunctionNodeVariantNames(ref nodeTypeInfo, Namespace, methodInfo, AdditionalLibraryNames);

                    FunctionCount++;
					lock (Library) {
                        Library.Add(nodeTypeInfo);
                    }
					
				} catch (Exception ex) {
					Debug.WriteLine("[NodeLibrary.Build] caught exception trying to build node from function " + methodInfo.Name + " : " + ex.Message);
#if DEBUG
					Debugger.Break();
#endif
				}
			}
            return FunctionCount;
		}



		protected static bool IsFunctionLibraryClass(Type type, out string LibraryName)
        {
            LibraryName = type.FullName!;
            GraphNodeFunctionLibrary? LibraryAttrib = type.GetCustomAttribute<GraphNodeFunctionLibrary>();
            if (LibraryAttrib == null)
                return false;
            if (LibraryAttrib.LibraryName == null || LibraryAttrib.LibraryName.Length == 0)
                return false;
            LibraryName = LibraryAttrib.LibraryName;
            return true;
        }


		protected static bool GetNodeNamespace(Type type, out string Namespace)
		{
			Namespace = type.FullName!;
			GraphNodeNamespace? NamespaceAttrib = type.GetCustomAttribute<GraphNodeNamespace>();
			if (NamespaceAttrib == null)
				return false;
			if (NamespaceAttrib.Namespace == null || NamespaceAttrib.Namespace.Length == 0)
				return false;
			Namespace = NamespaceAttrib.Namespace;
			return true;
		}

		protected static void append_to_list(ref List<string>? list, string append)
        {
            if (list == null) list = new List<string>();
            list.Add(append);
        }
        protected static void append_permutations(ref List<string>? list, string baseString, List<string>? suffixes)
        {
            if (suffixes == null) return;
            if (list == null) list = new List<string>();
            foreach (string suffix in suffixes)
                list.Add(baseString + suffix);
        }

        protected static List<string>? CollectMappedLibraryNames(Type type)
        {
            List<string>? result = null;
            foreach (MappedLibraryName mappedName in type.GetCustomAttributes<MappedLibraryName>())
                append_to_list(ref result, mappedName.MappedName);
            return result;
        }


        protected static void AddMappedFunctionNodeVariantNames(ref NodeTypeInfo typeInfo, string LibraryName, MethodInfo methodInfo, List<string>? AdditionalLibraryNames)
        {
            List<string>? mappedShortNames = null;
            List<string>? mappedFullNames = null;
            foreach (MappedNodeFunctionName name in methodInfo.GetCustomAttributes<MappedNodeFunctionName>()) {
                if (name.MappedName.Contains('.'))
                    append_to_list(ref mappedFullNames, name.MappedName);
                else
                    append_to_list(ref mappedShortNames, name.MappedName);
            }

            append_permutations(ref typeInfo.MappedVariants, LibraryName + ".", mappedShortNames);

            if (AdditionalLibraryNames != null) {
                foreach ( string libraryName in AdditionalLibraryNames) {
                    append_to_list(ref typeInfo.MappedVariants, libraryName + "." + methodInfo.Name);
                    append_permutations(ref typeInfo.MappedVariants, libraryName + ".", mappedShortNames);
                }
            }

            if (mappedFullNames != null) {
                foreach (string fullName in mappedFullNames)
                    append_to_list(ref typeInfo.MappedVariants, fullName);
            }
        }


        protected static void AddMappedNodeVariantNames(ref NodeTypeInfo typeInfo)
        {
            foreach (MappedNodeTypeName mappedName in typeInfo.nodeType.ClassType.GetCustomAttributes<MappedNodeTypeName>())
            {
                if (mappedName.MappedName.Contains('.'))
                    append_to_list(ref typeInfo.MappedClassTypes, mappedName.MappedName);
                //else
                //    append_to_list(ref mappedShortNames, name.MappedName);
            }
        }






    }




    public sealed class DefaultNodeLibrary
    {
        private static readonly NodeLibrary instance = new NodeLibrary(true);

        static DefaultNodeLibrary() { }
        private DefaultNodeLibrary() { }

        public static NodeLibrary Instance {
            get {
                return instance;
            }
        }
    }

}
