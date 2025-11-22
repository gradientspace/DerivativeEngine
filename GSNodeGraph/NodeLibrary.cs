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
using Gradientspace.NodeGraph.Util;
using System.Numerics;

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
        protected List<NodeTypeInfo> Library;
        protected List<NodeType> OldVersions;

        // do these need to be accessed via Interlocked or some Atomic mechanism??
        bool IsLibraryBuilt = false;
        protected Task? WaitForLibraryBuildTask = null;
        protected object lockObj = new object();

        public NodeLibrary(bool bLaunchAutoBackgroundBuild = false) 
        {
            Library = new List<NodeTypeInfo>();
            OldVersions = new List<NodeType>();

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

        public virtual IEnumerable<NodeType> EnumerateAllNodesWithFirstAssignablePinType(GraphDataType fromPinDataType)
        {
			wait_for_library_build();

			foreach (NodeTypeInfo info in Library)
            {
                if (info.nodeType.NodeArchetype == null || info.bIsSystemNode) continue;
       
                ENodeInputFlags ignoreFlags = ENodeInputFlags.IsNodeConstant | ENodeInputFlags.Hidden;
                foreach (INodeInputInfo inputInfo in info.nodeType.NodeArchetype.EnumerateInputs())
                {
                    ENodeInputFlags flags = inputInfo.Input.GetInputFlags();
                    if ((flags & ignoreFlags) != 0)
                        continue;

                    // TODO: BaseGraph.CanConnectTypes(GraphDataType, GraphDataType) is probably what we actually
                    // want to call here, because that considers conversion extensions, etc. However
                    // then we need ExecGraph. Could pass as argument? Or pass check funciton as argument?

                    bool bCanConnectTypes = TypeUtils.CanConnectFromTo(fromPinDataType, inputInfo.DataType);

                    if (bCanConnectTypes)
                        yield return info.nodeType;
                    break;
                }
            }
        }


        public virtual NodeType? FindNodeType(Type ClassType)
        {
            return (ClassType.FullName != null) ? FindNodeType(ClassType.FullName!, "", NodeVersion.MostRecent) : null;
        }

        public virtual NodeType? FindNodeType(string ClassName, string Variant, NodeVersion Version)
        {
            wait_for_library_build();

            bool bIgnoreVersion = Version.IsMostRecent;

            // todo this will become expensive and maybe we should construct some kind of dictionary/hash?

            NodeType? found = null;
			foreach (NodeTypeInfo info in Library)
            {
                bool bClassTypeMatch = (info.nodeType.ClassType.FullName == ClassName);
                if (!bClassTypeMatch && info.MappedClassTypes != null && info.MappedClassTypes.Contains(ClassName))
                    bClassTypeMatch = true;
                if (!bClassTypeMatch) continue;

                if (info.nodeType.Variant == Variant) {
                    found = info.nodeType;
                    break;
                }

                if (info.MappedVariants != null && info.MappedVariants.Contains(Variant)) {
                    found = info.nodeType;
                    break;
                }
            }

            // try version resolution
            if (found != null && bIgnoreVersion == false && found.Version != Version) 
            {
                bool bFoundVersion = false;
                foreach (NodeType nodeType in OldVersions) {
                    if (nodeType.Version == Version && nodeType.VersionOf == found) {
                        found = nodeType;
                        bFoundVersion = true;
                    }
                }
                if (!bFoundVersion)
                    GlobalGraphOutput.AppendError($"Version {Version} of Node {found.UIName} could not be located - current version is {found.Version}");
            }

            return found;
        }





        protected List<(NodeType,string)> pending_version_resolution = new();
        protected void mark_for_version_processing(NodeType nodeType, string versionOf)
        {
            lock(pending_version_resolution) {
                pending_version_resolution.Add((nodeType,versionOf));
            }
        }


        protected virtual void begin_library_build()
        {
            lock (lockObj) { 
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

            Library = new List<NodeTypeInfo>();
            OldVersions = new List<NodeType>();
            pending_version_resolution = new();

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
                    if (AssemblyUtils.IsFunctionLibraryClass(type, out string LibraryNamespace))
                        libraryTypes.Add(new(type, LibraryNamespace));
                }

                //Parallel.ForEach<Type>(nodeTypes, processNodeType);
                foreach (Type type in nodeTypes)
                    processNodeType(type);

                //Parallel.ForEach<(Type, string)>(libraryTypes, processLibraryType);
                foreach ((Type type, string Namespace) in libraryTypes)
                    processLibraryType((type, Namespace));

            }//);

            // process version-of tags
            foreach ( (NodeType nodeType, string versionOf) in pending_version_resolution) 
            {
                NodeType? found = null;
                if (nodeType.ClassType == typeof(LibraryFunctionNode)) {
                    foreach (NodeTypeInfo info in Library) {
                        if (info.nodeType == nodeType) 
                            continue;
                        if (info.nodeType.ClassType != typeof(LibraryFunctionNode))
                            continue;
                        if (info.nodeType.Variant == versionOf) {
                            found = info.nodeType;
                            break;
                        }
                    }
                    if (found != null) {
                        nodeType.VersionOf = found;
                        OldVersions.Add(nodeType);
                    } else
                        GlobalGraphOutput.AppendError($"Specified version of {nodeType.UIName}.VersionOf = [{versionOf}] could not be not found");

                } else {
                    throw new NotImplementedException();        // not supporting on nodes yet
                }
            }

            watch.Stop();
			GlobalGraphOutput.AppendLog($"[NodeLibrary.Build] Found {FoundNodeClasses} Node Classes, {FoundFunctionLibraries} Function Libraries with {FoundNodeFunctions} Functions in {watch.Elapsed.TotalSeconds}s");
		}



        protected virtual List<Assembly> FindPotentialNodeAssemblies()
        {
            return AssemblyUtils.FindPotentialNodeAssemblies();
		}



        protected virtual bool AddPotentialNodeToLibrary(Type type)
        {
            if (type.GetCustomAttribute<ClassHierarchyNode>() != null)
                return false;

            bool bIsSystemNode = (type.GetCustomAttribute<SystemNode>() != null);

            // todo much more filtering here...

            string? CustomName = null;
            foreach (GraphNodeUIName UINameAttrib in type.GetCustomAttributes<GraphNodeUIName>()) {
                if (UINameAttrib.UIName.Length > 0)
                    CustomName = UINameAttrib.UIName;
            }

            try
            {
                INode? nodeArchetype = Activator.CreateInstance(type) as INode;
                Debug.Assert(nodeArchetype != null);
                if (nodeArchetype != null)
                {
                    string NodeUIName = (CustomName != null) ? CustomName : nodeArchetype.GetNodeName();
                    NodeType nodeType = new NodeType(type, NodeUIName);
                    nodeType.NodeArchetype = nodeArchetype;

                    if (GetNodeNamespace(type, nodeArchetype, out string Namespace))
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
			List<string>? AdditionalLibraryNames = CollectMappedLibraryNames(type, Namespace);

            int FunctionCount = 0;
			foreach (MethodInfo methodInfo in type.GetMethods())
			{
				if (methodInfo.IsStatic == false) continue;

                NodeFunction? nodeFunctionInfo = methodInfo.GetCustomAttribute(typeof(NodeFunction)) as NodeFunction;
				if (nodeFunctionInfo == null) continue;

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
                        LibraryFunctionNode funcNode = new LibraryFunctionNode(type, methodInfo, NodeName);
                        if (nodeFunctionInfo.IsPure)
                            funcNode.Flags |= ENodeFlags.IsPure;
                        if (nodeFunctionInfo.Hidden)
                            funcNode.Flags |= ENodeFlags.Hidden;
                        nodeArchetype = funcNode;
					}
					NodeType nodeType = new NodeType(functionNodeClassType, NodeName);
					nodeType.NodeArchetype = nodeArchetype;
					nodeType.UICategory = Namespace;
					nodeType.Variant = Namespace + "." + methodInfo.Name;
                    nodeType.Flags = nodeArchetype.GetNodeFlags();

                    if ( NodeVersion.ValidateVersion(nodeFunctionInfo.Version) == false ) {
                        // Should we be trying to set some invalid version here?? Is it safe to fall back to default?
                        GlobalGraphOutput.AppendError($"Invalid NodeFunction.Version value on {type.Namespace}.{type.Name}.{methodInfo.Name} - must be Major.Minor integers (eg 1.1)");
                        nodeType.Version = NodeVersion.Default;
                    } else {
                        nodeType.Version = NodeVersion.Parse(nodeFunctionInfo.Version);
                        if (nodeFunctionInfo.VersionOf != null) {
                            mark_for_version_processing(nodeType, nodeFunctionInfo.VersionOf);
                        } //else
                            //GlobalGraphOutput.AppendError($"Missing VersionOf tag on {type.Namespace}.{type.Name}.{methodInfo.Name}");
                            // this prints for every function that doesn't have VersionOf...clearly wrong, what was the intention here??
                    }

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




        protected static bool GetNodeNamespace(Type nodeType, INode archetypeNode, out string Namespace)
        {
            Namespace = archetypeNode.GetNodeNamespace() ?? "Unknown";
            GraphNodeNamespace? NamespaceAttrib = nodeType.GetCustomAttribute<GraphNodeNamespace>();
            if (NamespaceAttrib != null)
                Namespace = NamespaceAttrib.Namespace;
            return (Namespace.Length > 0);
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

        protected static List<string>? CollectMappedLibraryNames(Type type, string Namespace)
        {
            List<string>? result = null;
            foreach (MappedFunctionLibraryName mappedName in type.GetCustomAttributes<MappedFunctionLibraryName>())
                append_to_list(ref result, mappedName.MappedName);

            // hardcoded handling of major Gradientspace. -> Core. library renaming
            // (can probably be removed after release...)
            if (Namespace.StartsWith("Core."))
                append_to_list(ref result, Namespace.Replace("Core.", "Gradientspace."));

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
        private static NodeLibrary instance = new NodeLibrary(true);

        static DefaultNodeLibrary() { }
        private DefaultNodeLibrary() { }

        public static NodeLibrary Instance {
            get {
                return instance;
            }
        }


        // todo really should support partial rebuild, or rebuild from a specific assembly...
        public static void ForceFullRebuild(bool bWait = false)
        {
            instance = new NodeLibrary(false);
            if (bWait)
                instance.Build();
            else
                instance.BuildAsync();
        }
    }

}
