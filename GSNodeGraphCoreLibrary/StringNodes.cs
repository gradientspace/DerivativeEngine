// Copyright Gradientspace Corp. All Rights Reserved.
using Gradientspace.NodeGraph;
using System.Diagnostics;
using System.Globalization;

namespace Gradientspace.NodeGraph
{
    // https://learn.microsoft.com/en-us/dotnet/api/system.string?view=net-9.0
    // Todo:
    //    Format node (multiple args)
    //    multi-Append (node)
    //    Split
    //    IConvertible conversions (ToX), Parse, TryParse
    //    SubstringRelative

    [NodeFunctionLibrary("Core.String")]
    public static class GSStringFunctions
    {
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
        public static bool Contains(string StringIn, string Contains, bool IgnoreCase = true)
        {
            return StringIn.Contains(Contains, get_string_comparison_mode(IgnoreCase));
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

    }

}
