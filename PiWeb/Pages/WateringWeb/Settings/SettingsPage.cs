﻿using Ooui;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using TabNoc.MyOoui;
using TabNoc.MyOoui.Interfaces.AbstractObjects;
using TabNoc.MyOoui.Interfaces.Enums;
using TabNoc.MyOoui.Storage;
using TabNoc.MyOoui.UiComponents;
using TabNoc.MyOoui.UiComponents.FormControl;
using TabNoc.MyOoui.UiComponents.FormControl.InputGroups;
using TabNoc.MyOoui.UiComponents.FormControl.InputGroups.Components;
using TabNoc.PiWeb.DataTypes.WateringWeb.Settings;
using Button = TabNoc.MyOoui.HtmlElements.Button;
using Color = System.Drawing.Color;
using JsonConvert = Newtonsoft.Json.JsonConvert;

namespace TabNoc.PiWeb.Pages.WateringWeb.Settings
{
	internal class SettingsPage : StylableElement
	{
		private readonly Dictionary<string, StylableTextInput> _backendPathTextInputDictionary = new Dictionary<string, StylableTextInput>();
		private readonly Dropdown _humidityDropdown;
		private readonly TextInputGroup _humiditySensorTextInputGroup;
		private readonly OverrideInputGroup _overrideInputGroup;
		private readonly PageStorage<SettingsData> _settingsData;

		public SettingsPage(PageStorage<SettingsData> settingsData) : base("div")
		{
			bool useSafeLoading = false;
			if (settingsData.TryLoad() == false)
			{
				settingsData.UseSafeLoading();
				useSafeLoading = true;
			}
			if (PageStorage<HumiditySensorData>.Instance.TryLoad() == false)
			{
				PageStorage<HumiditySensorData>.Instance.UseSafeLoading();
				useSafeLoading = true;
			}
			this.AddScriptDependency("/lib/bootstrap3-typeahead.min.js");
			const int labelSize = 180;

			_settingsData = settingsData;

			#region Initialize Grid

			Container wrappingContainer = new Container(this);
			Grid grid = new Grid(wrappingContainer);

			grid.AddStyling(StylingOption.MarginRight, 2);
			grid.AddStyling(StylingOption.MarginLeft, 2);
			grid.AddStyling(StylingOption.MarginTop, 4);
			grid.AddStyling(StylingOption.MarginBottom, 2);

			#endregion Initialize Grid

			if (useSafeLoading)
			{
				grid.AddRow().AppendCollum(new Heading(3, "Wegen Verbindungsproblemem wurden keine Daten geladen!") { Style = { Color = Color.Red } });
			}

			#region AutoEnabled

			MultiInputGroup autoEnabledMultiInputGroup = new MultiInputGroup();
			autoEnabledMultiInputGroup.AppendLabel("Automatik", labelSize);
			autoEnabledMultiInputGroup
				.AppendCustomElement(
					new TwoStateButtonGroup("Aktiv", "Inaktiv", settingsData.StorageData.Enabled,
						!settingsData.StorageData.Enabled), false).FirstButtonStateChange += (sender, args) =>
				settingsData.StorageData.Enabled = args.NewButtonState;
			autoEnabledMultiInputGroup.AddStyling(StylingOption.MarginBottom, 2);
			autoEnabledMultiInputGroup.AppendCustomElement(new Button(StylingColor.Danger, true, Button.ButtonSize.Normal, false, "Alle Kanäle ausschalten", fontAwesomeIcon: "stop"), false).Click += (sender, args) =>
			{
				ServerConnection.DeleteAsync("settings", "stopall");
			};
			grid.AddRow().AppendCollum(autoEnabledMultiInputGroup, autoSize: true);

			#endregion AutoEnabled

			#region WeatherEnabled

			MultiInputGroup weatherEnabledMultiInputGroup = new MultiInputGroup();
			weatherEnabledMultiInputGroup.AppendLabel("Wetterdaten verwenden", labelSize);
			weatherEnabledMultiInputGroup
				.AppendCustomElement(
					new TwoStateButtonGroup("Aktiv", "Inaktiv", settingsData.StorageData.WeatherEnabled,
						!settingsData.StorageData.WeatherEnabled), false).FirstButtonStateChange += (sender, args) =>
				settingsData.StorageData.WeatherEnabled = args.NewButtonState;
			weatherEnabledMultiInputGroup.AddStyling(StylingOption.MarginBottom, 2);
			grid.AddRow().AppendCollum(weatherEnabledMultiInputGroup, autoSize: true);

			#endregion WeatherEnabled

			#region Location

			Row locationRow = grid.AddRow();
			locationRow.AddStyling(StylingOption.MarginBottom, 2);
			MultiInputGroup weatherLocationMultiInputGroup = new MultiInputGroup();
			weatherLocationMultiInputGroup.AppendLabel("Standort", labelSize);

			StylableTextInput weatherLocationTextInput = weatherLocationMultiInputGroup.AppendTextInput("Bitte Eintragen...", false);
			weatherLocationTextInput.Value = settingsData.StorageData.LocationFriendlyName;

			#region Hidden TextInputs

			TextInput weatherLocationChangeTextInput = new TextInput { IsHidden = true, Value = settingsData.StorageData.Location };

			locationRow.AppendChild(weatherLocationChangeTextInput);
			TextInput weatherLocationNameChangeTextInput = new TextInput { IsHidden = true, Value = settingsData.StorageData.LocationFriendlyName };

			locationRow.AppendChild(weatherLocationNameChangeTextInput);

			#endregion Hidden TextInputs

			#region Autocomplete

			weatherLocationTextInput.ActivateAutocomplete("/settings/WeatherLocations.json", new Dictionary<string, TextInput>()
			{
				{"location", weatherLocationChangeTextInput },
				{"name", weatherLocationNameChangeTextInput }
			});

			#endregion Autocomplete

			locationRow.AppendCollum(weatherLocationMultiInputGroup, autoSize: true);

			#region Save Button

			Button saveLocationButton = new Button(StylingColor.Success, true, text: "Übernehmen");
			saveLocationButton.Click += (sender, args) =>
			{
				if (weatherLocationChangeTextInput.Value == "")
				{
					weatherLocationTextInput.SetValidation(false, true);
				}
				else
				{
					weatherLocationTextInput.SetValidation(false, false);
					settingsData.StorageData.Location = weatherLocationChangeTextInput.Value;
					settingsData.StorageData.LocationFriendlyName = weatherLocationNameChangeTextInput.Value;
					weatherLocationTextInput.Value = settingsData.StorageData.LocationFriendlyName;
				}
			};
			locationRow.AppendCollum(saveLocationButton, autoSize: true);

			#endregion Save Button

			#endregion Location

			#region Override

			_overrideInputGroup = new OverrideInputGroup(_settingsData.StorageData.OverrideValue, labelSizeInPx: labelSize);
			grid.AddRow().AppendCollum(_overrideInputGroup, autoSize: true);

			#endregion Override

			#region Rename HumiditySensors

			Row humidityRow = grid.AddRow();
			humidityRow.AppendChild(new Heading(3, "Feuchigkeitssensoren Umbenennen"));
			humidityRow.AddNewLine();

			#region Sync Server HumidityList with Storage

			foreach (string humiditySensor in PageStorage<HumiditySensorData>.Instance.StorageData.HumiditySensors)
			{
				if (settingsData.StorageData.HumiditySensors.ContainsKey(humiditySensor) == false)
				{
					settingsData.StorageData.HumiditySensors.Add(humiditySensor, humiditySensor);
				}
			}

			List<string> removeList = new List<string>();
			foreach ((string realSensorName, string _) in settingsData.StorageData.HumiditySensors)
			{
				if (PageStorage<HumiditySensorData>.Instance.StorageData.HumiditySensors.Contains(realSensorName) == false)
				{
					removeList.Add(realSensorName);
				}
			}

			foreach (string s in removeList)
			{
				settingsData.StorageData.HumiditySensors.Remove(s);
			}

			#endregion Sync Server HumidityList with Storage

			_humidityDropdown = new Dropdown(new Button(StylingColor.Secondary, true, widthInPx: 150));
			humidityRow.AppendCollum(_humidityDropdown, autoSize: true);

			foreach (string humiditySensor in PageStorage<HumiditySensorData>.Instance.StorageData.HumiditySensors)
			{
				StylableAnchor stylableAnchor = _humidityDropdown.AddEntry(humiditySensor);
				stylableAnchor.Click += (sender, args) => SelectHumiditySensor(humiditySensor);
			}

			_humiditySensorTextInputGroup = new TextInputGroup("Freundlicher Name", "Bitte Eingeben!");
			humidityRow.AppendCollum(_humiditySensorTextInputGroup, autoSize: true);

			Button button = new Button(StylingColor.Success, true, text: "Übernehmen");
			button.Click += (sender, args) =>
			{
				if (_humidityDropdown.Button.Text != "")
				{
					_settingsData.StorageData.HumiditySensors[_humidityDropdown.Button.Text] = _humiditySensorTextInputGroup.TextInput.Value;
				}
			};

			humidityRow.AppendCollum(button, autoSize: true);
			if (PageStorage<HumiditySensorData>.Instance.StorageData.HumiditySensors.Count > 0)
			{
				SelectHumiditySensor(PageStorage<HumiditySensorData>.Instance.StorageData.HumiditySensors.First());
			}
			else
			{
				humidityRow.IsHidden = true;
			}
			humidityRow.AddStyling(StylingOption.MarginBottom, 2);

			#endregion Rename HumiditySensors

			#region Backend Server Path

			grid.AddRow().AppendCollum(new Heading(3, "Backend Server Schnittstelle einstellen") { ClassName = "text-center mb-4" });
			Row backendServerRow = grid.AddRow();
			Row backendServerConfigurationSingeApiRow = grid.AddRow();
			Row backendServerConfigurationMultiApiRow = grid.AddRow();
			backendServerRow.AddNewLine();

			BackendData backendInstanceStorageData = PageStorage<BackendData>.Instance.StorageData;

			MultiInputGroup backendConfigurationSourceSwitchingMultiInputGroup = backendServerRow.AppendCollum(new MultiInputGroup());
			backendConfigurationSourceSwitchingMultiInputGroup.AppendLabel("Quelle Auswählen", labelSize);
			TwoStateButtonGroup backendConfigurationSourceSwitchingTwoStateButton = backendConfigurationSourceSwitchingMultiInputGroup.AppendCustomElement(new TwoStateButtonGroup("Sammelkonfiguration", "einzele Konfiguration", !backendInstanceStorageData.SingleApiConfiguration, backendInstanceStorageData.SingleApiConfiguration), false);

			void OnBackendConfigurationSourceSwitchingTwoStateButtonOnFirstButtonStateChange(object sender, ButtonChangeEventHandlerArgs args)
			{
				if (args.NewButtonState == true)
				{
					backendServerConfigurationSingeApiRow.Style.Display = "none";
					backendServerConfigurationMultiApiRow.Style.Display = null;
				}
				else
				{
					backendServerConfigurationSingeApiRow.Style.Display = null;
					backendServerConfigurationMultiApiRow.Style.Display = "none";
				}

				backendInstanceStorageData.SingleApiConfiguration = !args.NewButtonState;
			}

			backendConfigurationSourceSwitchingTwoStateButton.FirstButtonStateChange += OnBackendConfigurationSourceSwitchingTwoStateButtonOnFirstButtonStateChange;
			OnBackendConfigurationSourceSwitchingTwoStateButtonOnFirstButtonStateChange(null, new ButtonChangeEventHandlerArgs(false, !backendInstanceStorageData.SingleApiConfiguration));
			backendConfigurationSourceSwitchingMultiInputGroup.AddStyling(StylingOption.MarginBottom, 5);

			#region backendServerConfigurationSingeApiRow

			foreach ((string name, BackendProperty backedProperties) in backendInstanceStorageData.BackendProperties)
			{
				backendServerConfigurationSingeApiRow.AddNewLine();
				backendServerConfigurationSingeApiRow.AppendCollum(CreateSingleBackendCollum(name, backedProperties), autoSize: true);
			}

			backendServerConfigurationSingeApiRow.AddNewLine();
			backendServerConfigurationSingeApiRow.AddNewLine();
			backendServerConfigurationSingeApiRow.AppendCollum(new Button(StylingColor.Light, false, Button.ButtonSize.Normal, false, "Standardkonfiguration eintragen")).Click += (sender, args) =>
			{
				foreach ((string name, BackendProperty _) in backendInstanceStorageData.BackendProperties)
				{
					if (_backendPathTextInputDictionary[name].Value == "")
					{
						_backendPathTextInputDictionary[name].Value = $"http://{Dns.GetHostAddresses("WebPiServer.PiWeb")[0].ToString()}:5000/api/{name}";
					}
				}
			};

			#endregion backendServerConfigurationSingeApiRow

			#region backendServerConfigurationMultiApiRow

			backendServerConfigurationMultiApiRow.AppendCollum(CreateMultiBackendCollum(backendInstanceStorageData, out StylableTextInput backendServerConfigurationMultiApiTextInput), autoSize: true);
			backendServerConfigurationMultiApiRow.AddNewLine();
			backendServerConfigurationMultiApiRow.AppendCollum(new Button(StylingColor.Light, false, Button.ButtonSize.Normal, false, "Standardkonfiguration eintragen")).Click += (sender, args) =>
			{
				if (backendServerConfigurationMultiApiTextInput.Value == "")
				{
					backendServerConfigurationMultiApiTextInput.Value = $"http://{Dns.GetHostAddresses("WebPiServer.PiWeb")[0].ToString()}:5000/api";
				}
			};

			#endregion backendServerConfigurationMultiApiRow

			#endregion Backend Server Path
		}

		public void SelectHumiditySensor(string humiditySensor)
		{
			_humidityDropdown.Button.Text = humiditySensor;
			_humiditySensorTextInputGroup.TextInput.Value = _settingsData.StorageData.HumiditySensors[humiditySensor];
		}

		protected override void Dispose(bool disposing)
		{
			_settingsData.StorageData.OverrideValue = _overrideInputGroup.Value;
			_settingsData.Save();
			PageStorage<BackendData>.Instance.Save();
			base.Dispose(disposing);
		}

		private MultiInputGroup CreateMultiBackendCollum(BackendData backendData, out StylableTextInput backendPath)
		{
			MultiInputGroup backendMultiInputGroup = new MultiInputGroup();
			backendMultiInputGroup.AppendLabel("Server Api Pfad", 115 + 80);
			TwoStateButtonGroup backendEnabled = backendMultiInputGroup.AppendCustomElement(new TwoStateButtonGroup("Vom Server", "Als Debug", backendData.MultiApiRequestDataFromBackend, !backendData.MultiApiRequestDataFromBackend), false);
			backendPath = backendMultiInputGroup.AppendTextInput("Pfad zur WebAPI", startText: backendData.MultiApiDataSourcePath);

			backendMultiInputGroup.AppendValidation("Einstellungen OK", "Einstellungen sind nicht OK", false);
			Button backendSaveSettings = backendMultiInputGroup.AppendCustomElement(new Button(StylingColor.Success, true, text: "Speichern", fontAwesomeIcon: "save"), false);

			StylableTextInput path = backendPath;
			backendSaveSettings.Click += (sender, args) =>
			{
				path.SetValidation(false, false);

				if (backendEnabled.FirstButtonActive && Uri.IsWellFormedUriString(path.Value, UriKind.Absolute))
				{
					foreach ((string name, BackendProperty _) in backendData.BackendProperties)
					{
						try
						{
							if (JsonConvert.DeserializeObject<bool>(new HttpClient().GetAsync(path.Value + "/" + name + "/enabled").EnsureResultSuccessStatusCode().Result.Content.ReadAsStringAsync().Result) == false)
							{
								//TODO: ich brauche eine Messagebox
								path.Value = "Der Server hat diese API verweigert! Pfad:" + path.Value;
								throw new Exception(path.Value);
							}
						}
						catch (Exception e)
						{
							path.Value = "Der Verbindungsversuch ist fehlgeschlagen! Pfad:" + path.Value;
							Console.ForegroundColor = ConsoleColor.Yellow;
							Console.WriteLine("Beim Versuch die neuen BackendEinstellungen zu Testen ist ein Fehler aufgetreten.");
							Console.ResetColor();

							Logging.WriteLog("System", "Warn", $"Beim Versuch die Backendeinstellungen für {name} des Servers zu validieren ist es zu folgendem Fehler gekommen:\r\n{e.Message}");

							path.SetValidation(false, true);
							//TODO: ich brauche eine Messagebox
							return;
						}
					}
					path.SetValidation(true, false);
					backendData.MultiApiRequestDataFromBackend = backendEnabled.FirstButtonActive;
					backendData.MultiApiDataSourcePath = path.Value;
				}
				else if (backendEnabled.SecondButtonActive)
				{
					path.SetValidation(true, false);
					backendData.MultiApiRequestDataFromBackend = backendEnabled.FirstButtonActive;
				}
				else
				{
					path.SetValidation(false, true);
				}
			};
			backendMultiInputGroup.AddStyling(StylingOption.MarginBottom, 2);
			return backendMultiInputGroup;
		}

		private MultiInputGroup CreateSingleBackendCollum(string name, BackendProperty backendProperty)
		{
			MultiInputGroup backendMultiInputGroup = new MultiInputGroup();
			backendMultiInputGroup.AppendLabel(name, 115 + 80);
			TwoStateButtonGroup backendEnabled = backendMultiInputGroup.AppendCustomElement(new TwoStateButtonGroup("Vom Server", "Als Debug", backendProperty.RequestDataFromBackend, !backendProperty.RequestDataFromBackend), false);
			StylableTextInput backendPath = backendMultiInputGroup.AppendTextInput("Pfad zur WebAPI", startText: backendProperty.DataSourcePath);
			_backendPathTextInputDictionary.Add(name, backendPath);
			backendMultiInputGroup.AppendValidation("Einstellungen OK", "Einstellungen sind nicht OK", false);
			Button backendSaveSettings = backendMultiInputGroup.AppendCustomElement(new Button(StylingColor.Success, true, text: "Speichern", fontAwesomeIcon: "save"), false);

			backendSaveSettings.Click += (sender, args) =>
			{
				backendPath.SetValidation(false, false);
				if (backendEnabled.FirstButtonActive && Uri.IsWellFormedUriString(backendPath.Value, UriKind.Absolute))
				{
					try
					{
						if (JsonConvert.DeserializeObject<bool>(new HttpClient().GetAsync(backendPath.Value + "/enabled").EnsureResultSuccessStatusCode().Result.Content.ReadAsStringAsync().Result) == false)
						{
							//TODO: ich brauche eine Messagebox
							backendPath.Value = "Der Server hat diese API verweigert! Pfad:" + backendPath.Value;
							throw new Exception(backendPath.Value);
						}
						backendPath.SetValidation(true, false);
						backendProperty.RequestDataFromBackend = backendEnabled.FirstButtonActive;
						backendProperty.DataSourcePath = backendPath.Value;
					}
					catch (Exception e)
					{
						backendPath.Value = "Der Verbindungsversuch ist fehlgeschlagen! Pfad:" + backendPath.Value;
						Console.ForegroundColor = ConsoleColor.Yellow;
						Console.WriteLine("Beim Versuch die neuen BackendEinstellungen zu Testen ist ein Fehler aufgetreten.");
						Console.ResetColor();

						Logging.WriteLog("System", "Warn", $"Beim Versuch die Backendeinstellungen für {name} des Servers zu validieren ist es zu folgendem Fehler gekommen:\r\n{e.Message}");

						backendPath.SetValidation(false, true);
						//TODO: ich brauche eine Messagebox
					}
				}
				else if (backendEnabled.SecondButtonActive)
				{
					backendPath.SetValidation(true, false);
					backendProperty.RequestDataFromBackend = backendEnabled.FirstButtonActive;
				}
				else
				{
					backendPath.SetValidation(false, true);
				}
			};
			backendMultiInputGroup.AddStyling(StylingOption.MarginBottom, 2);
			return backendMultiInputGroup;
		}
	}
}
