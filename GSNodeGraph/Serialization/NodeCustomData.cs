using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gradientspace.NodeGraph
{
    /// <summary>
    /// This is a helper class for node custom data serialization, and is not really meant
    /// to be used for anything else. 
    /// 
    /// In Save/Load, the objects in DataItems are serialized to Json, and restored as JsonElement objects.
    /// So they have to be 'nice' when converted to/from string (and nearly always are actually just strings).
    /// 
    /// However in Undo/Redo, they are currently not converted to json (!!!)
    /// 
    /// If the object isn't a string (or other supproted type below), the RestoreCustomDataItems() will have 
    /// to handle these two cases (ie it's either the real object, or a JsonElement serialization).
    /// 
    /// </summary>
    public class NodeCustomData
    {
        public List<Tuple<string, object>> DataItems = new List<Tuple<string, object>>();

        public NodeCustomData AddItem(string Key, object Value) {
            DataItems.Add(new(Key, Value));
            return this;
        }
        public object? FindItem(string Key) {
            Tuple<string, object>? Found = DataItems.Find((x) => { return x.Item1 == Key; });
            return (Found == null) ? null : Found.Item2;
        }


        public NodeCustomData AddStringItem(string Key, string Value) {
            DataItems.Add(new(Key, Value));
            return this;
        }
        public string? FindStringItem(string Key) {
            object? Found = FindItem(Key);
            return (Found != null) ? (string)Found : null;
        }
        public string FindStringItemOrDefault(string Key, string DefaultValue) {
            object? Found = FindItem(Key);
            return (Found != null) ? (string)Found : DefaultValue;
        }
        public void FindStringItemOrDefault(string Key, in string DefaultValue, out string ResultValue) {
            ResultValue = FindStringItemOrDefault(Key, DefaultValue);
        }
        public string FindStringItemChecked(string Key, string ExceptionMessage) {
            object? Found = FindItem(Key);
            if (Found != null)
                return (string)Found;
            throw new Exception(ExceptionMessage);
        }


        public NodeCustomData AddBoolItem(string Key, bool bValue) {
            DataItems.Add(new(Key, bValue ? "True" : "False"));
            return this;
        }
        public bool FindBoolItemOrDefault(string Key, bool bDefaultValue) {
            string? Found = FindStringItem(Key);
            if (Found == null)
                return bDefaultValue;
            if (String.Compare(Found, "True", true) == 0) return true;
            if (String.Compare(Found, "False", true) == 0) return false;
            return bDefaultValue;
        }
        public void FindBoolItemOrDefault(string Key, bool bDefaultValue, out bool ResultValue) {
            ResultValue = FindBoolItemOrDefault(Key, bDefaultValue);
        }


        public NodeCustomData AddIntItem(string Key, int Value) {
            DataItems.Add(new(Key, Value.ToString()));
            return this;
        }
        public int FindIntItemOrDefault(string Key, int DefaultValue) {
            string? Found = FindStringItem(Key);
            if (Found == null)
                return DefaultValue;
            if (Int32.TryParse(Found, out int Value))
                return Value;
            return DefaultValue;
        }
        public void FindIntItemOrDefault(string Key, int DefaultValue, out int ResultValue) {
            ResultValue = FindIntItemOrDefault(Key, DefaultValue);
        }


        public NodeCustomData AddEnumItem<T>(string Key, T enumValue) where T : System.Enum
        {
            DataItems.Add(new(Key, enumValue.ToString()));
            return this;
        }
        public T FindEnumItemOrDefault<T>(string Key, T defaultValue) where T : System.Enum
        {
            string? Found = FindStringItem(Key);
            if (Found == null)
                return defaultValue;
            if (Enum.TryParse(typeof(T), Found, out object? enumValue))
                return (T)enumValue;
            return defaultValue;
        }
        public void FindEnumItemOrDefault<T>(string Key, T defaultValue, out T ResultValue) where T : System.Enum
        {
            ResultValue = FindEnumItemOrDefault<T>(Key, defaultValue);
        }




        public NodeCustomData AddTypeItem(string Key, Type type) {
            DataItems.Add(new(Key, TypeUtils.MakePartialQualifiedTypeName(type)));
            return this;
        }
        public Type? FindTypeItem(string Key, out string typeName) {
            typeName = "";
            string? FoundTypeName = FindStringItem(Key);
            if (FoundTypeName == null)
                return null;
            typeName = FoundTypeName;
            // todo possibly should be using TypeUtils.FindTypeInLoadedAssemblies() here...
            return Type.GetType(FoundTypeName);
        }
        public Type? FindTypeItemChecked(string Key, out string typeName, string ExceptionMessage) {
            typeName = "";
            string? FoundTypeName = FindStringItem(Key);
            if (FoundTypeName == null)
                throw new Exception(ExceptionMessage);
            typeName = FoundTypeName;
            // todo possibly should be using TypeUtils.FindTypeInLoadedAssemblies() here...
            return Type.GetType(FoundTypeName);
        }

    }
}
