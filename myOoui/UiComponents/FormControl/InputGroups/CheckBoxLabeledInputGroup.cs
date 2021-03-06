﻿using Ooui;
using System;
using TabNoc.MyOoui.Interfaces.AbstractObjects;

namespace TabNoc.MyOoui.UiComponents.FormControl.InputGroups
{
	internal class CheckBoxLabeledInputGroup : InputGroupControl
	{
		private readonly Input _checkBox;

		public bool Checked
		{
			get => _checkBox.IsChecked;
			set => _checkBox.IsChecked = value;
		}

		public CheckBoxLabeledInputGroup(bool @checked, string labelText)
		{
			ClassName = "input-group";

			Div checkBoxDiv1 = new Div
			{
				ClassName = "input-group-prepend"
			};
			AppendChild(checkBoxDiv1);

			Div checkBoxDiv2 = new Div
			{
				ClassName = "input-group-text"
			};
			checkBoxDiv1.AppendChild(checkBoxDiv2);

			_checkBox = new Input();
			_checkBox.SetAttribute("type", "checkbox");
			_checkBox.SetAttribute("aria-label", Guid.NewGuid().ToString());
			checkBoxDiv2.AppendChild(_checkBox);

			// end checkbox

			Div labelDiv1 = new Div
			{
				ClassName = "input-group-append"
			};
			AppendChild(labelDiv1);

			Span labelSpan = new Span(labelText)
			{
				ClassName = "input-group-text"
			};
			labelDiv1.AppendChild(labelSpan);

			Checked = @checked;
		}
	}
}
