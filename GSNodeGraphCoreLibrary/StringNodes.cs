// Copyright Gradientspace Corp. All Rights Reserved.
using Gradientspace.NodeGraph;
using System.Diagnostics;

namespace Gradientspace.NodeGraph
{
    // https://learn.microsoft.com/en-us/dotnet/api/system.string?view=net-9.0
    // Todo:
    //    IConvertible conversions (ToX), Parse, TryParse
    //    SubstringRelative

    [NodeFunctionLibrary("Core.String")]
    public static class GSStringFunctions
    {
        // library-wide configuration of culture mode

        public enum ECultureMode { Ordinal = 0, Current = 1, Invariant = 2 };
        public static ECultureMode string_funcs_culture_mode = ECultureMode.Invariant;

        [NodeFunction]
        public static void ConfigureStringMode(ECultureMode CultureMode = ECultureMode.Invariant)
        {
            string_funcs_culture_mode = CultureMode;
        }

        public static StringComparison get_string_comparison_mode(bool IgnoreCase)
        {
            switch (string_funcs_culture_mode) {
                case ECultureMode.Ordinal: return (IgnoreCase) ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
                case ECultureMode.Current: return (IgnoreCase) ? StringComparison.CurrentCultureIgnoreCase : StringComparison.CurrentCulture;
                default:
                case ECultureMode.Invariant: return (IgnoreCase) ? StringComparison.InvariantCultureIgnoreCase : StringComparison.InvariantCulture;
            }
        }






        [NodeFunction(IsPure = true)]
        public static string Append(string A, string B) {
            return A + B;
        }

        [NodeFunction(IsPure = true)]
        public static string Join(IEnumerable<string> Strings, string Separator = "")
        {
            return string.Join(Separator, Strings);
        }

        [NodeFunction(IsPure = true)]
        public static String Replace(string String, string ToReplace, string ReplaceWith, bool IgnoreCase = true)
        {
            return String.Replace(ToReplace, ReplaceWith, get_string_comparison_mode(IgnoreCase));
        }


        [NodeFunction(IsPure = true)]
        public static string InsertAt(string String, int AtIndex, string Insert)
        {
            return String.Insert(AtIndex, Insert);
        }

        [NodeFunction(IsPure = true)]
        public static string RemoveRange(string String, int StartIndex, int Count = -1)
        {
            if (Count <= 0)
                return String.Remove(StartIndex);
            else
                return String.Remove(StartIndex, Count);
        }

        [NodeFunction(IsPure = true)]
        public static string InsertRelative(string String, string Find, string Insert, out bool WasFound, bool InsertBefore = true, bool IgnoreCase = true)
        {
            WasFound = false;
            int FoundIndex = String.IndexOf(Find, get_string_comparison_mode(true));
            if (FoundIndex <= 0)
                return String;
            WasFound = true;
            if (InsertBefore)
                return String.Insert(FoundIndex, Insert);
            else
                return String.Insert(FoundIndex+Find.Length, Insert);
        }

        [NodeFunction(IsPure = true)]
        public static String TrimSpaces(string String, bool bStart = true, bool bEnd = true)
        {
            if (bStart && bEnd)     return String.Trim();
            else if (bStart)        return String.TrimStart();
            else if (bEnd)          return String.TrimEnd();
            return String;
        }

        [NodeFunction(IsPure = true)]
        public static String First(string String, int Count = 1)
        {
            return String.Substring(0, Count);
        }
        [NodeFunction(IsPure = true)]
        public static String Last(string String, int Count = 1)
        {
            return String.Substring(String.Length-Count, Count);
        }
        [NodeFunction(IsPure = true)]
        public static String Substring(string String, int StartIndex, int Count = -1)
        {
            if (StartIndex >= 0 && Count > 0)
                return String.Substring(StartIndex, Count);
            else if (StartIndex >= 0 && Count < 0)
                return String.Substring(StartIndex);
            return "";
        }


        [NodeFunction(IsPure = true)]
        public static int Length(string String) {
            return String.Length;
        }


        [NodeFunction(IsPure = true)]
        public static String ToLower(string String) {
            if (string_funcs_culture_mode == ECultureMode.Invariant)
                return String.ToLowerInvariant();
            else
                return String.ToLower();
        }

        [NodeFunction(IsPure = true)]
        public static String ToUpper(string String) {
            if (string_funcs_culture_mode == ECultureMode.Invariant)
                return String.ToUpperInvariant();
            else
                return String.ToUpper();
        }


        [NodeFunction(IsPure = true)]
        public static bool IsEmpty(string String, bool bWhitespace = true)
        {
            if (string.IsNullOrEmpty(String)) return true;
            if (bWhitespace && string.IsNullOrWhiteSpace(String)) return true;
            return false;
        }

        [NodeFunction(IsPure = true)]
        public static bool Contains(string String, string Contains, bool IgnoreCase = true)
        {
            return String.Contains(Contains, get_string_comparison_mode(IgnoreCase));
        }

        [NodeFunction(IsPure = true)]
        public static bool StartsWith(string String, string StartsWith, bool IgnoreCase = true)
        {
            return String.StartsWith(StartsWith, get_string_comparison_mode(IgnoreCase));
        }

        [NodeFunction(IsPure = true)]
        public static bool EndsWith(string String, string EndsWith, bool IgnoreCase = true)
        {
            return String.EndsWith(EndsWith, get_string_comparison_mode(IgnoreCase));
        }

        [NodeFunction(IsPure = true)]
        public static bool IsSameString(string A, string B, bool IgnoreCase = true)
        {
            return string.Compare(A, B, get_string_comparison_mode(IgnoreCase)) == 0;
        }

        [NodeFunction(IsPure = true)]
        public static int IndexOf(string String, string Find, bool IgnoreCase = true)
        {
            return String.IndexOf(Find, get_string_comparison_mode(IgnoreCase));
        }

        [NodeFunction(IsPure = true)]
        public static int LastIndexOf(string String, string Find, bool IgnoreCase = true)
        {
            return String.LastIndexOf(Find, get_string_comparison_mode(IgnoreCase));
        }


        [NodeFunction(ReturnName="Strings")]
        public static List<string> Apply(IEnumerable<string> Strings, Func<string,string> Modifier)
        {
            List<string> result = new List<string>();
            foreach (string str in Strings) {
                string newString = Modifier(str);
                result.Add(newString);
            }
            return result;
        }
    }




    [GraphNodeNamespace("Core.String")]
    [GraphNodeUIName("AppendStrings")]
    public class AppendStringsNode : VariableStringsInputNode
    {
        public static string SeparatorName { get { return "Separator"; } }
        public static string OutputName { get { return "String"; } }

        public override string GetDefaultNodeName() { return "AppendStrings"; }
        protected override string ElementBaseName { get { return "String"; } }

        public AppendStringsNode() {
            Flags |= ENodeFlags.IsPure;
        }

        protected override void BuildStandardInputsOutputs()
        {
            AddInput(SeparatorName, new StandardStringNodeInput());
            AddOutput(OutputName, new StandardNodeOutput<string>());
        }

        public override void Evaluate(EvaluationContext EvalContext, ref readonly NamedDataMap DataIn, NamedDataMap RequestedDataOut)
        {
            int OutputIndex = RequestedDataOut.IndexOfItem(OutputName);
            if (OutputIndex == -1)
                throw new Exception("AppendStringsNode: output not found");

            string[] AppendValues = ConstructStringArray(in DataIn);
            string Separator = DataIn.FindStringValueOrDefault(SeparatorName, "");

            string result = string.Join(Separator, AppendValues);
            RequestedDataOut.SetItemValue(OutputIndex, result);
        }
    }






    [GraphNodeNamespace("Core.String")]
    [GraphNodeUIName("Split")]
    public class SplitStringNode : VariableStringsInputNode
    {
        public static string InputName { get { return "String"; } }
        public static string TrimSpacesName { get { return "Trim Spaces"; } }
        public static string RemoveEmptyName { get { return "Remove Empty"; } }
        public static string ArrayOutputName { get { return "Strings"; } }

        public override string GetDefaultNodeName() { return "Split"; }
        protected override string ElementBaseName { get { return "Separator"; } }

        public SplitStringNode() {
            Flags |= ENodeFlags.IsPure;
        }

        protected override void BuildStandardInputsOutputs()
        {
            StandardStringNodeInput newInput = new StandardStringNodeInput();
            AddInput(InputName, newInput);
            AddInput(TrimSpacesName, new StandardNodeInputWithConstant<bool>(true) );
            AddInput(RemoveEmptyName, new StandardNodeInputWithConstant<bool>(true));

            AddOutput(ArrayOutputName, new StandardNodeOutputBase(typeof(string[])));
        }

        public override void Evaluate(EvaluationContext EvalContext, ref readonly NamedDataMap DataIn, NamedDataMap RequestedDataOut)
        {
            int OutputIndex = RequestedDataOut.IndexOfItem(ArrayOutputName);
            if (OutputIndex == -1)
                throw new Exception("SplitStringNode: output not found");

            string[] SeparatorValues = ConstructStringArray(in DataIn);

            string ToSplit = DataIn.FindStringValueOrDefault(InputName, "");
            StringSplitOptions Options = StringSplitOptions.None;
            if (DataIn.FindStructValueOrDefault<bool>(TrimSpacesName, true))
                Options = Options | StringSplitOptions.TrimEntries;
            if (DataIn.FindStructValueOrDefault<bool>(RemoveEmptyName, true))
                Options = Options | StringSplitOptions.RemoveEmptyEntries;

            string[] result = ToSplit.Split(SeparatorValues, Options);

            RequestedDataOut.SetItemValue(OutputIndex, result);
        }
    }





    [GraphNodeNamespace("Core.String")]
    [GraphNodeUIName("Format")]
    public class FormatStringNode : VariableObjectsInputNode
    {
        public static string InputName { get { return "Format"; } }
        public static string OutputName { get { return "String"; } }

        public override string GetDefaultNodeName() { return "Format"; }
        protected override string ElementBaseName { get { return "Value"; } }

        public FormatStringNode() {
            Flags |= ENodeFlags.IsPure;
        }

        protected override void BuildStandardInputsOutputs()
        {
            StandardStringNodeInput newInput = new StandardStringNodeInput();
            AddInput(InputName, newInput);

            AddOutput(OutputName, new StandardNodeOutput<string>());
        }

        public override void Evaluate(EvaluationContext EvalContext, ref readonly NamedDataMap DataIn, NamedDataMap RequestedDataOut)
        {
            int OutputIndex = RequestedDataOut.IndexOfItem(OutputName);
            if (OutputIndex == -1)
                throw new Exception("FormatStringNode: output not found");
            object[] formatValues = ConstructObjectArray(in DataIn);
            string FormatString = DataIn.FindStringValueOrDefault(InputName, "");
            string result = String.Format(FormatString, formatValues);
            RequestedDataOut.SetItemValue(OutputIndex, result);
        }
    }


}
