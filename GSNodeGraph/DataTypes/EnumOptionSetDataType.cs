using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gradientspace.NodeGraph
{
    /**
     *  EnumList is a data type intended to represent a fixed set of choices that 
     *  is determined at runtime. For example the list of fields available on a
     *  dynamically-selected class type, etc.
     */
    public class EnumOptionSet
    {
        public struct OptionItem
        {
            public string Label;
            public int Value;
            public object? TransientData;
        }

        public OptionItem[] Items { get; private set; }
        public int Count { get { return Items.Length; } }

        public EnumOptionSet(IEnumerable<string> FromItems)
        {
            Items = new OptionItem[FromItems.Count()];
            int i = 0;
            foreach (var itemString in FromItems)
            {
                Items[i] = new() { Label = itemString, Value = i, TransientData = null };
                i++;
            }
        }

        public EnumOptionSet(int Count, Func<int, Tuple<string,int,object?>> GetItemFunc)
        {
            Items = new OptionItem[Count];
            for ( int i = 0; i < Count; ++i ) {
                (string label, int value, object? data) = GetItemFunc(i);
                Items[i] = new() { Label = label, Value = value, TransientData = data};
            }
        }

        public string this[int i]
        {
            get { return Items[i].Label; }
        }

        public int DefaultIndex { get; private set; } = 0;

        public string DefaultValue {
            get {
                return (DefaultIndex < Count) ? Items[DefaultIndex].Label : "";
            }
        }

        public bool FindIndexFromLabel(string Label, out int Index)
        {
            Index = -1;
            int N = Items.Length;
            for ( int i = 0; i < N; ++i) {
                if (Items[i].Label == Label) {
                    Index = i;
                    return true;
                }
            }
            return false;
        }
    }



    public struct EnumOptionItem
    {
        public string ItemString { get; init; }
        public EnumOptionItem(string itemString) { ItemString = itemString; }

        public static implicit operator string(EnumOptionItem item) => item.ItemString;
        public static explicit operator EnumOptionItem(string s) => new EnumOptionItem(s);

        public static readonly EnumOptionItem Default = new EnumOptionItem("");

        public static bool operator ==(EnumOptionItem A, EnumOptionItem B) {
            return A.ItemString == B.ItemString;
        }
        public static bool operator !=(EnumOptionItem A, EnumOptionItem B) { return !(A == B); }
        readonly public override bool Equals(object? obj) {
            if (obj == null || GetType() != obj.GetType()) return false;
            return ItemString == ((EnumOptionItem)obj).ItemString;
        }
        public override int GetHashCode() { return ItemString.GetHashCode(); }
    }




}
