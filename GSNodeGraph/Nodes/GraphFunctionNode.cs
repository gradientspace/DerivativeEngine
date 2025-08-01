using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using static Gradientspace.NodeGraph.FunctionDefinitionNode;

namespace Gradientspace.NodeGraph
{


    [SystemNode]
    public class FunctionDefinitionNode : NodeBase
    {
        public override string GetDefaultNodeName() {
            return "Function";
        }
        public override string GetCustomNodeName() {
            return functionName;
        }

        public FunctionDefinitionNode() : base()
        {
            FunctionID = Guid.NewGuid().ToString();
        }


        // unique ID that can be used to store links to function in other graph nodes (eg return nodes)
        public string FunctionID { get; private set; }

        public string FunctionName { get { return functionName; } }

        [JsonConverter(typeof(FunctionArgJsonConverter))]
        public struct FunctionArg
        {
            public string ArgName { get; set; }
            public Type ArgType { get; set; }
            public object? DefaultValue { get; set; }
        }
        public IEnumerable<FunctionArg> Arguments { get { return functionArgs; } }
        public IEnumerable<FunctionArg> ReturnArguments { get { return returnArgs; } }


        protected string functionName = "NewFunction";
        protected List<FunctionArg> functionArgs = new List<FunctionArg>();
        protected List<FunctionArg> returnArgs = new List<FunctionArg>();

        public void UpdateFunctionName(string NewFunctionName)
        {
            functionName = NewFunctionName;
            PublishNodeModifiedNotification();
        }

        public void UpdateArguments( IEnumerable<FunctionArg> NewArguments )
        {
            List<FunctionArg> newArguments = new List<FunctionArg>();

            Inputs.Clear();
            Outputs.Clear();
            foreach (FunctionArg arg in NewArguments) 
            {
                newArguments.Add(arg);

                INodeInput argInput = FunctionNodeUtils.BuildInputNodeForType(arg.ArgType, arg.DefaultValue);
                if (argInput is StandardNodeInputBase argInputBase)
                    argInputBase.Flags = ENodeInputFlags.IsNodeConstant;
                bool bInputOK = AddInput(arg.ArgName, argInput);
                if (!bInputOK)
                    GlobalGraphOutput.AppendError($"FunctionDefinitionNode.UpdateArguments: input pin could not be created for {arg.ArgName} with Type {arg.ArgType}");

                INodeOutput argOutput = new StandardNodeOutputBase(arg.ArgType);
                bool bOutputOK = AddOutput(arg.ArgName, argOutput);
                if (!bOutputOK)
                    GlobalGraphOutput.AppendError($"FunctionDefinitionNode.UpdateArguments: output pin could not be created for {arg.ArgName} with Type {arg.ArgType}");
            }

            functionArgs = newArguments;

            PublishNodeModifiedNotification();
        }


        public void UpdateReturnArguments(IEnumerable<FunctionArg> NewReturnArguments)
        {
            returnArgs = NewReturnArguments.ToList();
        }


        public const string FunctionIDKey = "FunctionID";
        public const string FunctionNameKey = "FunctionName";
        public const string FunctionArgsKey = "Arguments";
        public const string FunctionReturnsKey = "Returns";
        public override void CollectCustomDataItems(out NodeCustomData? DataItems)
        {
            DataItems = new NodeCustomData().
                AddStringItem(FunctionIDKey, FunctionID).
                AddStringItem(FunctionNameKey, functionName).
                AddItem(FunctionArgsKey, functionArgs).
                AddItem(FunctionReturnsKey, returnArgs);
        }
        public override void RestoreCustomDataItems(NodeCustomData DataItems)
        {
            FunctionID = DataItems.FindStringItemOrDefault(FunctionIDKey, "(invalid)");
            functionName = DataItems.FindStringItemOrDefault(FunctionNameKey, "NoNameFunc");

            List<FunctionArg> restoredArgs = RestoreArgs(DataItems, FunctionArgsKey);
            UpdateArguments(restoredArgs);
            List<FunctionArg> restoredRets = RestoreArgs(DataItems, FunctionReturnsKey);
            UpdateReturnArguments(restoredRets);
        }



        // does function node ever actually get evaluated??
        public override void Evaluate(ref readonly NamedDataMap DataIn, NamedDataMap RequestedDataOut)
        {
            throw new NotImplementedException();
        }


        public static List<FunctionArg> RestoreArgs(NodeCustomData DataItems, string Key)
        {
            List<FunctionArg> restoredArgs = new List<FunctionArg>();
            object? FoundArgs = DataItems.FindItem(Key);
            if (FoundArgs is JsonElement jsonFoundArgs) {
                Debug.Assert(jsonFoundArgs.ValueKind == JsonValueKind.Array);
                foreach (JsonElement elem in jsonFoundArgs.EnumerateArray()) {
                    FunctionArg arg = elem.Deserialize<FunctionArg>();
                    restoredArgs.Add(arg);
                }

            } else
                restoredArgs = (List<FunctionArg>)FoundArgs!;
            return restoredArgs;
        }

    }



    [SystemNode]
    public class FunctionReturnNode : NodeBase
    {
        public override string GetDefaultNodeName() {
            return "Return";
        }

        public virtual void LinkToFunction(FunctionDefinitionNode functionNode) {
            this.FunctionID = functionNode.FunctionID;
            UpdateArguments(functionNode.ReturnArguments);
        }

        public string FunctionID { get; private set; } = "";


        protected List<FunctionDefinitionNode.FunctionArg> returnArgs = new List<FunctionDefinitionNode.FunctionArg>();

        public IEnumerable<FunctionDefinitionNode.FunctionArg> Arguments { get { return returnArgs; } }


        public void UpdateArguments(IEnumerable<FunctionDefinitionNode.FunctionArg> NewArguments)
        {
            List<FunctionDefinitionNode.FunctionArg> newArguments = new List<FunctionDefinitionNode.FunctionArg>();

            Inputs.Clear();
            Outputs.Clear();
            foreach (FunctionDefinitionNode.FunctionArg arg in NewArguments) {
                newArguments.Add(arg);

                INodeInput argInput = FunctionNodeUtils.BuildInputNodeForType(arg.ArgType, arg.DefaultValue);
                bool bInputOK = AddInput(arg.ArgName, argInput);
                if (!bInputOK)
                    GlobalGraphOutput.AppendError($"FunctionReturnNode.UpdateArguments: input pin could not be created for {arg.ArgName} with Type {arg.ArgType}");
            }

            returnArgs = newArguments;

            PublishNodeModifiedNotification();
        }


        public const string FunctionIDKey = "FunctionID";
        public const string FunctionReturnsKey = "Returns";
        public override void CollectCustomDataItems(out NodeCustomData? DataItems)
        {
            DataItems = new NodeCustomData().
                AddStringItem(FunctionIDKey, FunctionID).
                AddItem(FunctionReturnsKey, returnArgs);
        }
        public override void RestoreCustomDataItems(NodeCustomData DataItems)
        {
            FunctionID = DataItems.FindStringItemOrDefault(FunctionIDKey, "(invalid)");

            List<FunctionArg> restoredRets = FunctionDefinitionNode.RestoreArgs(DataItems, FunctionReturnsKey);
            UpdateArguments(restoredRets);
        }



        // does return node ever actually get evaluated??
        public override void Evaluate(ref readonly NamedDataMap DataIn, NamedDataMap RequestedDataOut)
        {
            throw new NotImplementedException();
        }
    }






    [SystemNode]
    public class FunctionCallNode : NodeBase
    {
        public override string GetDefaultNodeName() {
            return "FunctionCall";
        }
        public override string GetCustomNodeName()
        {
            return functionName;
        }

        public virtual void LinkToFunction(FunctionDefinitionNode functionNode) {
            this.FunctionID = functionNode.FunctionID;
            this.functionName = functionNode.FunctionName;
            UpdateArguments(functionNode.Arguments, functionNode.ReturnArguments);
        }

        public string FunctionName { get { return functionName; } }
        public string FunctionID { get; private set; } = "";

        protected string functionName = "NewFunction";


        protected List<FunctionDefinitionNode.FunctionArg> inputArgs = new List<FunctionDefinitionNode.FunctionArg>();
        protected List<FunctionDefinitionNode.FunctionArg> returnArgs = new List<FunctionDefinitionNode.FunctionArg>();

        public void UpdateFunctionName(string NewFunctionName)
        {
            functionName = NewFunctionName;
            PublishNodeModifiedNotification();
        }

        public void UpdateArguments(IEnumerable<FunctionDefinitionNode.FunctionArg> newInputArgs, 
            IEnumerable<FunctionDefinitionNode.FunctionArg> newReturnArgs)
        {
            List<FunctionDefinitionNode.FunctionArg> newArguments = new List<FunctionDefinitionNode.FunctionArg>();
            List<FunctionDefinitionNode.FunctionArg> newReturns = new List<FunctionDefinitionNode.FunctionArg>();

            Inputs.Clear();
            Outputs.Clear();
            foreach (FunctionDefinitionNode.FunctionArg arg in newInputArgs) {
                newArguments.Add(arg);
                INodeInput argInput = FunctionNodeUtils.BuildInputNodeForType(arg.ArgType, arg.DefaultValue);
                bool bInputOK = AddInput(arg.ArgName, argInput);
                if (!bInputOK)
                    GlobalGraphOutput.AppendError($"FunctionCallNode.UpdateArguments: input pin could not be created for argument {arg.ArgName} with Type {arg.ArgType}");
            }
            foreach (FunctionDefinitionNode.FunctionArg arg in newReturnArgs) {
                newReturns.Add(arg);
                INodeOutput argOutput = new StandardNodeOutputBase(arg.ArgType);
                bool bOutputOK = AddOutput(arg.ArgName, argOutput);
                if (!bOutputOK)
                    GlobalGraphOutput.AppendError($"FunctionCallNode.UpdateArguments: output pin could not be created for return {arg.ArgName} with Type {arg.ArgType}");
            }

            inputArgs = newArguments;
            returnArgs = newReturns;

            PublishNodeModifiedNotification();
        }



        public const string FunctionIDKey = "FunctionID";
        public const string FunctionNameKey = "FunctionName";
        public const string FunctionArgsKey = "Arguments";
        public const string FunctionReturnsKey = "Returns";
        public override void CollectCustomDataItems(out NodeCustomData? DataItems)
        {
            DataItems = new NodeCustomData().
                AddStringItem(FunctionIDKey, FunctionID).
                AddStringItem(FunctionNameKey, functionName).
                AddItem(FunctionArgsKey, inputArgs).
                AddItem(FunctionReturnsKey, returnArgs);
        }
        public override void RestoreCustomDataItems(NodeCustomData DataItems)
        {
            FunctionID = DataItems.FindStringItemOrDefault(FunctionIDKey, "(invalid)");
            functionName = DataItems.FindStringItemOrDefault(FunctionNameKey, "NoNameFunc");

            List<FunctionArg> restoredArgs = FunctionDefinitionNode.RestoreArgs(DataItems, FunctionArgsKey);
            List<FunctionArg> restoredRets = FunctionDefinitionNode.RestoreArgs(DataItems, FunctionReturnsKey);
            UpdateArguments(restoredArgs, restoredRets);
        }



        // does call node ever actually get evaluated??
        public override void Evaluate(EvaluationContext EvalContext,
            ref readonly NamedDataMap DataIn,
            NamedDataMap RequestedDataOut)
        {
            throw new NotImplementedException();
        }
    }











    // json read and write for FunctionArg type
    public class FunctionArgJsonConverter : JsonConverter<FunctionDefinitionNode.FunctionArg>
    {
        //public string ArgName { get; set; }
        //public Type ArgType { get; set; }
        //public object? DefaultValue { get; set; }

        public override FunctionDefinitionNode.FunctionArg Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            JsonNode? node = JsonNode.Parse(ref reader);
            if (node == null)
                return new FunctionDefinitionNode.FunctionArg() { ArgName = "(invalid)", ArgType = typeof(object) };

            string argName = node["ArgName"]?.GetValue<string>() ?? "(invalid)";
            string argType = node["ArgType"]?.GetValue<string>() ?? "(invalid)";
            Type foundType = Type.GetType(argType) ?? typeof(object);

            object? defaultValue = null;
            string defaultJson = node["DefaultValue"]?.GetValue<string>() ?? "";
            if (defaultJson.Length > 0) {
                throw new NotImplementedException("haven't done this yet...");
            }

            return new FunctionDefinitionNode.FunctionArg() {
                ArgName = argName, ArgType = foundType, DefaultValue = defaultValue
            };
        }


        public override void Write(Utf8JsonWriter writer, FunctionDefinitionNode.FunctionArg value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteString("ArgName", value.ArgName);
            writer.WriteString("ArgType", TypeUtils.MakePartialQualifiedTypeName(value.ArgType));
            if (value.DefaultValue != null) {
                string jsonDefaultValue = JsonSerializer.Serialize(value.DefaultValue, options);
                writer.WriteString("DefaultValue", jsonDefaultValue);
            }
            writer.WriteEndObject();
        }
    }


}
