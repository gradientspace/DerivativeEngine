// Copyright Gradientspace Corp. All Rights Reserved.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gradientspace.NodeGraph
{
    // perhaps this interface should be promoted to NodeGraphCore? Requries also moving EnumOptionSet
    public interface IEnumOptionSetNodeInput
    {
        EnumOptionSet GetOptionSet();
    }

    public class EnumOptionSetNodeInput : StandardNodeInputBaseWithConstant, IEnumOptionSetNodeInput
    {
        public EnumOptionSet OptionSet { get; init; }

        public EnumOptionSet GetOptionSet() { return OptionSet; }

        public delegate void OnEnumListConstantChangedEvent(EnumOptionSetNodeInput Input);
        public OnEnumListConstantChangedEvent? OnSelectedValueChanged;

        public EnumOptionSetNodeInput(EnumOptionSet optionSet, string InitialValue = "") : base(typeof(EnumOptionItem), EnumOptionItem.Default)
        {
            OptionSet = optionSet;
            OptionSet.FindIndexFromLabel(InitialValue, out int FoundIndex);
            string UseString = (FoundIndex == -1) ? OptionSet.DefaultValue : OptionSet[FoundIndex];
            this.ConstantValue = new EnumOptionItem(UseString);
        }

        public override void SetConstantValue(object NewValue)
        {
            if ((NewValue is EnumOptionItem) == false)
                throw new Exception("EnumOptionSetNodeInput.SetConstantValue: value has incorrect type");

            string Selected = (string)(EnumOptionItem)NewValue;
            OptionSet.FindIndexFromLabel(Selected, out int FoundIndex);
            string UseString = (FoundIndex == -1) ? OptionSet.DefaultValue : OptionSet[FoundIndex];
            this.ConstantValue = new EnumOptionItem(UseString);
            OnSelectedValueChanged?.Invoke(this);
        }
    }
}
