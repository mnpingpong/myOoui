﻿using Ooui;
using System;
using TabNoc.MyOoui.HtmlElements;
using TabNoc.MyOoui.Interfaces.AbstractObjects;

namespace TabNoc.MyOoui.UiComponents.FormControl.InputGroups
{
	public class RadioButtonLabeledInputGroup : InputGroupControl
	{
		public readonly string RadioButtonGroupName;
		private readonly RadioButton _radioButton;

		public bool Checked
		{
			get => _radioButton.IsChecked;
			set => _radioButton.IsChecked = value;
		}

		public RadioButtonLabeledInputGroup(bool @checked, string labelText, string radioButtonGroupName = "")
		{
			if (string.IsNullOrWhiteSpace(radioButtonGroupName))
			{
				radioButtonGroupName = Guid.NewGuid().ToString();
			}

			RadioButtonGroupName = radioButtonGroupName;

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

			_radioButton = new RadioButton(radioButtonGroupName, @checked);
			checkBoxDiv2.AppendChild(_radioButton);

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
