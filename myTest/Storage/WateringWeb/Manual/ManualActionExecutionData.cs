﻿using System;
using System.Collections.Generic;
using TabNoc.Ooui.Interfaces.AbstractObjects;
using TabNoc.Ooui.Pages.WateringWeb.Overview;
using TabNoc.Ooui.Storage.WateringWeb.Channels;

namespace TabNoc.Ooui.Storage.WateringWeb.Manual
{
	internal class ManualActionExecutionData : PageData
	{
		public List<ManualActionExecution> ExecutionList;
#pragma warning disable 169
		public string EventSource = "Manual";
#pragma warning restore 169

		private ManualActionExecutionData()
		{
			Valid = false;
		}

		public new static ManualActionExecutionData CreateNew() => new ManualActionExecutionData();

		public static void CreateBatchAction(BatchEntry batch, int durationOverride)
		{
			PageStorage<ManualActionExecutionData>.Instance.StorageData.ExecutionList = new List<ManualActionExecution>()
			{
				new ManualActionExecution(batch.ChannelId, batch.Duration, batch.ActivateMasterChannel, durationOverride)
			};
			PageStorage<ManualActionExecutionData>.Instance.StorageData.Valid = true;
		}

		public static void CreateChannelAction(ChannelData channel, TimeSpan duration, bool activateMasterChannel, int durationOverride = 100)
		{
			PageStorage<ManualActionExecutionData>.Instance.StorageData.ExecutionList = new List<ManualActionExecution>()
			{
				new ManualActionExecution(channel.ChannelId, duration, activateMasterChannel, durationOverride)
			};
			PageStorage<ManualActionExecutionData>.Instance.StorageData.Valid = true;
		}

		public static void CreateJobAction(JobEntry job, int durationOverride)
		{
			PageStorage<ManualActionExecutionData>.Instance.StorageData.ExecutionList = new List<ManualActionExecution>();
			foreach (BatchEntry jobBatchEntry in job.BatchEntries)
			{
				PageStorage<ManualActionExecutionData>.Instance.StorageData.ExecutionList.Add(
					new ManualActionExecution(jobBatchEntry.ChannelId, jobBatchEntry.Duration,
						jobBatchEntry.ActivateMasterChannel, durationOverride));
			}

			PageStorage<ManualActionExecutionData>.Instance.StorageData.Valid = true;
		}

		public static void ExecuteAction()
		{
			
			PageStorage<ManualActionExecutionData>.Instance.Save();
			PageStorage<ManualActionExecutionData>.Instance.StorageData.ExecutionList = null;
			PageStorage<ManualActionExecutionData>.Instance.StorageData.Valid = false;
		}

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
