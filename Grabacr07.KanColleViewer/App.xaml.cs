﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Grabacr07.KanColleViewer.Composition;
using Grabacr07.KanColleViewer.Models;
using Grabacr07.KanColleViewer.ViewModels;
using Grabacr07.KanColleViewer.Views;
using Grabacr07.KanColleWrapper;
using Livet;
using MetroRadiance;
using Microsoft.Win32;
using AppSettings = Grabacr07.KanColleViewer.Properties.Settings;
using Settings = Grabacr07.KanColleViewer.Models.Settings;

namespace Grabacr07.KanColleViewer
{
	public partial class App
	{
		public static ProductInfo ProductInfo { get; private set; }
		public static MainWindowViewModel ViewModelRoot { get; private set; }

		static App()
		{
			AppDomain.CurrentDomain.UnhandledException += (sender, args) => ReportException(sender, args.ExceptionObject as Exception);
		}

		protected override void OnStartup(StartupEventArgs e)
		{
			base.OnStartup(e);

			this.DispatcherUnhandledException += (sender, args) => ReportException(sender, args.Exception);

			DispatcherHelper.UIDispatcher = this.Dispatcher;
			ProductInfo = new ProductInfo();

			Settings.Load();
			PluginHost.Instance.Initialize();
			NotifierHost.Instance.Initialize(KanColleClient.Current);
			Helper.SetRegistryFeatureBrowserEmulation();
			Helper.SetMMCSSTask();

			KanColleClient.Current.Proxy.Startup(AppSettings.Default.LocalProxyPort);
			KanColleClient.Current.Proxy.UpstreamProxySettings = Settings.Current.ProxySettings;

			ResourceService.Current.ChangeCulture(Settings.Current.Culture);
			// Initialize translations
			KanColleClient.Current.Translations.EnableTranslations = Settings.Current.EnableTranslations;
			KanColleClient.Current.Translations.EnableAddUntranslated = Settings.Current.EnableAddUntranslated;
			KanColleClient.Current.Translations.ChangeCulture(Settings.Current.Culture);

			// Update notification and download new translations (if enabled)
			if (KanColleClient.Current.Updater.LoadVersion(AppSettings.Default.KCVUpdateUrl.AbsoluteUri))
			{
				if (Settings.Current.EnableUpdateNotification && KanColleClient.Current.Updater.IsOnlineVersionGreater(0, ProductInfo.Version.ToString()))
				{
					PluginHost.Instance.GetNotifier().Show(NotifyType.Other,
						KanColleViewer.Properties.Resources.Updater_Notification_Title,
						string.Format(KanColleViewer.Properties.Resources.Updater_Notification_NewAppVersion, KanColleClient.Current.Updater.GetOnlineVersion(0)),
						() => Process.Start(KanColleClient.Current.Updater.GetOnlineVersion(0, true)));
				}

				if (Settings.Current.EnableUpdateTransOnStart)
				{
					if (KanColleClient.Current.Updater.UpdateTranslations(AppSettings.Default.XMLTransUrl.AbsoluteUri, Settings.Current.Culture, KanColleClient.Current.Translations) > 0)
					{
						PluginHost.Instance.GetNotifier().Show(NotifyType.Other,
							KanColleViewer.Properties.Resources.Updater_Notification_Title,
							KanColleViewer.Properties.Resources.Updater_Notification_TransUpdate_Success,
							() => App.ViewModelRoot.Activate());

						KanColleClient.Current.Translations.ChangeCulture(Settings.Current.Culture);
					}
				}
			}

			ThemeService.Current.Initialize(this, Theme.Dark, Accent.Purple);

			ViewModelRoot = new MainWindowViewModel();
			this.MainWindow = new MainWindow { DataContext = ViewModelRoot };
			this.MainWindow.Show();

			// Check if Adobe Flash is installed in Microsoft Explorer
			if (GetFlashVersion() == "")
			{
				MessageBoxResult MB = MessageBox.Show(KanColleViewer.Properties.Resources.System_Flash_Not_Installed_Text, KanColleViewer.Properties.Resources.System_Flash_Not_Installed_Caption, MessageBoxButton.YesNo, MessageBoxImage.Error, MessageBoxResult.Yes);
				if (MB == MessageBoxResult.Yes) {
					Process.Start("IExplore.exe", @"http://get.adobe.com/flashplayer/");
					this.MainWindow.Close();
				}
			}
		}

		protected override void OnExit(ExitEventArgs e)
		{
			base.OnExit(e);

			KanColleClient.Current.Proxy.Shutdown();

			PluginHost.Instance.Dispose();
			Settings.Current.Save();
		}


		private static void ReportException(object sender, Exception exception)
		{
			#region const
			const string messageFormat = @"
===========================================================
ERROR, date = {0}, sender = {1},
{2}
";
			const string path = "error.log";
			#endregion

			try
			{
				var message = string.Format(messageFormat, DateTimeOffset.Now, sender, exception);

				Debug.WriteLine(message);
				File.AppendAllText(path, message);
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex);
			}
		}

		/// <summary>
		/// Obtains Adobe Flash Player's ActiveX element version.
		/// </summary>
		/// <returns>Empty string if Flash is not installed, otherwise the currently installed version.</returns>
		private static string GetFlashVersion()
		{
			string sVersion = "";

			RegistryKey FlashRK = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Macromedia\FlashPlayerActiveX");
			if (FlashRK != null)
			{
				sVersion = FlashRK.GetValue("Version", "").ToString();
			}

			return sVersion;
		}
	}
}
