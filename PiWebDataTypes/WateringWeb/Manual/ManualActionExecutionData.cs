﻿using System;
using System.Collections.Generic;
using TabNoc.MyOoui.Interfaces.AbstractObjects;
using TabNoc.PiWeb.Storage.WateringWeb.Channels;

namespace TabNoc.PiWeb.Storage.WateringWeb.Manual
{
	public class ManualActionExecutionData : PageData
	{
#pragma warning disable 169
		public string EventSource = "Manual";
#pragma warning restore 169
		public List<ManualActionExecution> ExecutionList;

		private ManualActionExecutionData()
		{
			Valid = true;
		}

		public new static ManualActionExecutionData CreateNew() => new ManualActionExecutionData();

		public class ManualActionExecution
		{
			public bool ActivateMasterChannel;
			public int ChannelId;
			public TimeSpan Duration;
			public int DurationOverride;

			public ManualActionExecution(int channelId, TimeSpan duration, bool activateMasterChannel, int durationOverride)
			{
				ChannelId = channelId;
				Duration = duration;
				ActivateMasterChannel = activateMasterChannel;
				DurationOverride = durationOverride;
			}
		}
	}
}