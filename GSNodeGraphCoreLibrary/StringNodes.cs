// Copyright Gradientspace Corp. All Rights Reserved.
using Gradientspace.NodeGraph;

namespace Gradientspace.NodeGraph
{
    //
    // Todo:
    //    Format node (multiple args)
    //    IndexOf, LastIndexOf
    //    Insert, Remove  (index-based)
    //    Insert (string based - after, before variants)
    //    multi-Append (node)
    //    Split
    //    IConvertible conversions (ToX), Parse, TryParse

    [NodeFunctionLibrary("Gradientspace.String")]
    public static class GradientspaceStringFunctionLibrary
    {
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
        public static String Replace(string String, string ToReplace, string ReplaceWith, bool bIgnoreCase = true)
        {
            StringComparison useComparison = (bIgnoreCase) ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            return String.Replace(ToReplace, ReplaceWith, useComparison);
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
            return String.ToLower();
        }
        [NodeFunction(IsPure = true)]
        public static String ToUpper(string String) {
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
        public static bool Contains(string String, string Contains, bool bIgnoreCase = true)
        {
            StringComparison useComparison = (bIgnoreCase) ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            return String.Contains(Contains, useComparison);
        }

        [NodeFunction(IsPure = true)]
        public static bool StartsWith(string String, string StartsWith, bool bIgnoreCase = true)
        {
            StringComparison useComparison = (bIgnoreCase) ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            return String.StartsWith(StartsWith, useComparison);
        }

        [NodeFunction(IsPure = true)]
        public static bool EndsWith(string String, string EndsWith, bool bIgnoreCase = true)
        {
            StringComparison useComparison = (bIgnoreCase) ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            return String.EndsWith(EndsWith, useComparison);
        }

        [NodeFunction(IsPure = true)]
        public static bool IsSameString(string A, string B, bool bIgnoreCase = true)
        {
            StringComparison useComparison = (bIgnoreCase) ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            return string.Compare(A, B, useComparison) == 0;
        }


    }

}
