using Gradientspace.NodeGraph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Gradientspace.NodeGraph
{
    public class DebugManager
    {
        public static bool GlobalEnableGraphDebugging = false;

        private static readonly DebugManager instance = new DebugManager();
        static DebugManager() { }
        private DebugManager() { }
        public static DebugManager Instance { get { return instance; } }


        public bool IsDebugging { get; private set; } = false;


        public void BeginGraphExecutionDebugSession()
        {
            if (!GlobalEnableGraphDebugging) return;

            Debug.Assert(IsDebugging == false);
            IsDebugging = true;
            ActiveNodeIdentifiers.Clear();
        }


        public void EndGraphExecutionDebugSession()
        {
            if (!GlobalEnableGraphDebugging) return;

            Debug.Assert(IsDebugging);
            IsDebugging = false;
            ActiveNodeIdentifiers.Clear();
        }


        private struct NodeFrame
        {
            public int NodeIdentifier = -1;
            public List<int> DependentNodes = new List<int>();
            public NodeFrame() { }
        }

        private List<NodeFrame> ActiveFrames = new List<NodeFrame>();
        private HashSet<int> ActiveNodeIdentifiers = new HashSet<int>();

        public void PushActiveNode_Async(int NodeIdentifier) {
            if (!GlobalEnableGraphDebugging) return;
            lock (ActiveFrames) {
                ActiveFrames.Add(new NodeFrame() { NodeIdentifier = NodeIdentifier });
                ActiveNodeIdentifiers.Add(NodeIdentifier);
            }
        }
        public void PopActiveNode_Async(int NodeIdentifier) {
            if (!GlobalEnableGraphDebugging) return;
            lock (ActiveFrames) {
                Debug.Assert(ActiveFrames.Count > 0 && ActiveFrames.Last().NodeIdentifier == NodeIdentifier);
                foreach (int id in ActiveFrames.Last().DependentNodes)
                    ActiveNodeIdentifiers.Remove(id);
                ActiveFrames.RemoveAt(ActiveFrames.Count - 1);
                ActiveNodeIdentifiers.Remove(NodeIdentifier);
            }
        }

        public void MarkNodeTouchedThisFrame(int NodeIdentifier)
        {
            if (!GlobalEnableGraphDebugging || !IsDebugging) return;
            lock (ActiveFrames)
            {
                ActiveFrames.Last().DependentNodes.Add(NodeIdentifier);
                ActiveNodeIdentifiers.Add(NodeIdentifier);
            }
        }

        public bool IsNodeActive(int NodeIdentifier) {
            if (!GlobalEnableGraphDebugging || !IsDebugging) return false;
            lock (ActiveNodeIdentifiers) {
                return ActiveNodeIdentifiers.Contains(NodeIdentifier);
            }
        }




    }


}
