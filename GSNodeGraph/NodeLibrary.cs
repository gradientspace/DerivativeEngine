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

        public NodeLibrary(bool bAutoBuild = false) 
        {
            Library = new List<NodeTypeInfo>();
            if (bAutoBuild)
                Build();
        }

        public virtual void Build()
        {
            Type BaseNodeType = typeof(NodeBase);

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (Type type in assembly.GetTypes())
                {
                    if (type.IsSubclassOf(BaseNodeType) == false)
                        continue;
                    if (type.IsAbstract)
                        continue;
                    if (type.GetCustomAttribute<ClassHierarchyNode>() != null)
                        continue;

                    bool bIsSystemNode = (type.GetCustomAttribute<SystemNode>() != null);

                    // todo much more filtering here...

                    string UseName = type.Name;
                    foreach (GraphNodeUIName UINameAttrib in type.GetCustomAttributes<GraphNodeUIName>())
                    {
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

                            if (IsFunctionLibraryClass(type, out string LibraryName))
                                nodeType.UICategory = LibraryName;

                            NodeTypeInfo typeInfo = new NodeTypeInfo(nodeType);
                            typeInfo.bIsSystemNode = bIsSystemNode;

                            // todo support additioanl class name remapping options (currently only full names supported)
                            AddMappedNodeVariantNames(ref typeInfo);

                            Library.Add(typeInfo);
                        }
                    } catch (Exception ex) {
                        Debug.WriteLine("NodeLibrary: caught exception trying to build node of type " + type.Name + " : " + ex.Message);
#if DEBUG
                        Debugger.Break();
#endif
                    }
                }

                foreach (Type type in assembly.GetTypes())
                {
                    if ( IsFunctionLibraryClass(type, out string LibraryName) )
                    {
                        List<string>? AdditionalLibraryNames = CollectMappedLibraryNames(type);

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
                                nodeType.UICategory = LibraryName;
                                nodeType.Variant = LibraryName + "." + methodInfo.Name;
                                NodeTypeInfo nodeTypeInfo = new NodeTypeInfo(nodeType);

                                // temporary code to correct an early error where LibraryFunctionNode was in the wrong namespace :(
                                //nodeTypeInfo.MappedClassTypes = new List<string>();
                                //nodeTypeInfo.MappedClassTypes.Add("GSNodeGraph.LibraryFunctionNode");

                                AddMappedFunctionNodeVariantNames(ref nodeTypeInfo, LibraryName, methodInfo, AdditionalLibraryNames);

                                Library.Add(nodeTypeInfo);
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine("NodeLibrary: caught exception trying to build node from function " + methodInfo.Name + " : " + ex.Message);
#if DEBUG
                                Debugger.Break();
#endif
                            }
                        }

                    }
                }
            }
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


        public virtual IEnumerable<NodeType> EnumerateAllNodes()
        {
            foreach (NodeTypeInfo info in Library) {
                if (info.bIsSystemNode == false )
                    yield return info.nodeType;
            }
                
        }


        public virtual IEnumerable<NodeType> EnumerateAllNodes(Predicate<NodeType> NodeFilter)
        {
            foreach (NodeTypeInfo info in Library)
            {
                if (info.bIsSystemNode == false &&  NodeFilter(info.nodeType) == true )
                    yield return info.nodeType;
            }
        }

        public virtual IEnumerable<NodeType> EnumerateAllNodesWithFirstPinType(Type firstInputPinType)
        {
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
