// Copyright Gradientspace Corp. All Rights Reserved.
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Xml.Linq;


namespace Gradientspace.NodeGraph
{
    public struct DataItem
    {
        public string Name;
        public Type DataType;
        public object? Value;
    }


    // todo could be a struct?
    public class NamedDataMap
    {
        public DataItem[] Items;

        public NamedDataMap(int NumItems)
        {
            Items = new DataItem[NumItems];
        }

        public void SetItem(int Index, string name, object value)
        {
            Items[Index] = new DataItem() { Name = name, DataType = value.GetType(), Value = value };
        }

        public void SetItemNull(int Index, string name)
        {
            Items[Index] = new DataItem() { Name = name, DataType = typeof(void), Value = null };
        }

        public void SetItem(int Index, string name, Type type, object? value)
        {
            if (value != null && value.GetType() != type)
                throw new Exception("NamedDataMap.SetItem - provided Type and object Type do not match!!");
            Items[Index] = new DataItem() { Name = name, DataType = type, Value = value };
        }

        public void SetItemName(int Index, string name)
        {
            Items[Index].Name = name;
        }

        public void SetItemValue(int Index, object value)
        {
            Items[Index].Value = value;
        }

        public int IndexOfItem(string Name)
        {
            int N = Items.Length;
            for (int i = 0; i < N; ++i)
            {
                if (Items[i].Name == Name)
                    return i;
            }
            return -1;
        }


        public bool FindItemValueStrict<T>(string Name, ref T ValueOut, bool bThrowException = true)
        {
            bool bFoundName = false, bHasValue = false, bCorrectType = false;
            Type? invalidType = null;
            int N = Items.Length;
            for ( int i = 0; i < N; ++i )
            {
                if (Items[i].Name == Name)
                {
                    bFoundName = true;
                    if (Items[i].Value != null ) 
                    {
                        bHasValue = true;
                        if (Items[i].DataType == typeof(T))
                        {
                            bCorrectType = true;
                            ValueOut = (T)Items[i].Value!;
                            return true;
                        }
                        else
                            invalidType = Items[i].DataType;
                    }
                }
            }
            if (bThrowException)
            {
                if (!bFoundName) throw new Exception("Input Value for " + Name + " could not be found");
                else if (!bHasValue) throw new Exception("Input Value for " + Name + " is null");
                else if (!bCorrectType) throw new Exception("Input Value for " + Name + " has incorrect type " + invalidType!.Name + ", expected " + typeof(T).Name);
                else throw new Exception("FindItemValueStrict: unknown error");
            }

            return false;
        }


        public object? FindItemValue(string Name)
        {
            int N = Items.Length;
            for (int i = 0; i < N; ++i) {
                if (Items[i].Name == Name)
                    return Items[i].Value;
            }
            return false;
        }


        public object? FindItemValueAsType(string Name, Type asType)
        {
            object? Found = FindItemValue(Name);
            if (Found == null) return null;
            Type FoundType = Found.GetType();
            if (FoundType == asType || FoundType.IsAssignableTo(asType)) 
                return Found;

            return Convert.ChangeType(Found, asType);
        }


        public void SetItemValueChecked(string Name, object value)
        {
            int N = Items.Length;
            for (int i = 0; i < N; ++i) {
                if (Items[i].Name == Name) { 
                    Items[i].Value = value;
                    return;
                }
            }
            throw new Exception("Could not find output named " + Name);
        }

    }
}
