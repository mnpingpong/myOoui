﻿using Ooui;
using System.Collections.Generic;
using System.Linq;
using TabNoc.MyOoui.Interfaces.AbstractObjects;
using TabNoc.MyOoui.Interfaces.Enums;
using TabNoc.MyOoui.UiComponents;
using TabNoc.PiWeb.DataTypes.WateringWeb.Channels;
using Button = TabNoc.MyOoui.HtmlElements.Button;

namespace TabNoc.PiWeb.Pages.WateringWeb.Channels
{
	internal class ChannelsPage : StylableElement
	{
		private readonly PageStorage<ChannelsData> _channelsData;
		private readonly Dictionary<ChannelData, Anchor> _pillDictionary = new Dictionary<ChannelData, Anchor>();
		private readonly VerticalPillNavigation _pillNavigation = new VerticalPillNavigation("col-3", "col-9", true);

		private bool _hasActivePill = false;

		public ChannelsPage(PageStorage<ChannelsData> channelsData) : base("div")
		{
			_channelsData = channelsData;

			Row row = new Row();
			AppendChild(row);

			Button addChannel = new Button(asOutline: true, size: Button.ButtonSize.Small);
			addChannel.Click += (sender, args) =>
			{
				ChannelData channelData = ChannelData.CreateNew((channelsData.StorageData.Channels.Count > 0 ? channelsData.StorageData.Channels.Max(data => data.ChannelId) : 0) + 1);
				channelsData.StorageData.Channels.Add(channelData);
				AddChannel(channelData.Name, channelData, false);
			};
			addChannel.Text = "Neuen Kanal hinzufügen";
			addChannel.AddStyling(StylingOption.MarginTop, 2);

			AddChannel("Master", channelsData.StorageData.MasterChannel, true);

			foreach (ChannelData channel in channelsData.StorageData.Channels)
			{
				AddChannel(channel.Name, channel, false);
				ApplyName(channel);
			}

			AppendChild(_pillNavigation);
			AppendChild(addChannel);
		}

		public void ApplyName(ChannelData channel)
		{
			_pillDictionary[channel].Text = channel.Name;
		}

		public void RemoveChannel(ChannelPage channelPage, ChannelData channel)
		{
			_channelsData.StorageData.Channels.Remove(channel);
			_pillNavigation.RemovePill(channel.Name, channelPage);
		}

		protected override void Dispose(bool disposing)
		{
			_channelsData.Save();
			base.Dispose(disposing);
		}

		private void AddChannel(string channelName, ChannelData channel, bool isMasterChannel)
		{
			Anchor pill = _pillNavigation.AddPill(channelName, new ChannelPage(channel, this, isMasterChannel), _hasActivePill == false);
			_pillDictionary.Add(channel, pill);
			_hasActivePill = true;
		}
	}
}
