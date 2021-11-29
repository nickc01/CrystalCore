﻿using System;
using System.Reflection;
using TMPro;
using UnityEngine.UI;
using WeaverCore.Utilities;

namespace WeaverCore.Editor.Settings.Elements
{
    public class StringElement : UIElement
    {
        TMP_InputField inputField;

        void Awake()
        {
            inputField = GetComponentInChildren<TMP_InputField>();
            inputField.onEndEdit.AddListener(OnInputChange);
        }

        public override bool CanWorkWithAccessor(IAccessor accessor)
        {
            return accessor.MemberType == typeof(string);
        }

        protected override void OnAccessorChanged(IAccessor accessor)
        {
            inputField.text = (string)accessor.FieldValue;
            Title = accessor.FieldName;
            base.OnAccessorChanged(accessor);
        }

        void OnInputChange(string text)
        {
            FieldAccessor.FieldValue = text;
        }
    }
}
