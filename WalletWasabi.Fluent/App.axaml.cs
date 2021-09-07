using System;
using System.Reactive.Concurrency;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data;
using Avalonia.Logging;
using Avalonia.Markup.Xaml;
using ReactiveUI;
using WalletWasabi.Fluent.Behaviors;
using WalletWasabi.Fluent.Providers;
using WalletWasabi.Fluent.ViewModels;
using WalletWasabi.Fluent.Views;
using WalletWasabi.Logging;
using Logger = WalletWasabi.Logging.Logger;

namespace WalletWasabi.Fluent
{
	public class App : Application
	{
		private Func<Task> _backendInitialiseAsync;

		/// <summary>
		/// Defines the <see cref="CanShutdownProvider"/> property.
		/// </summary>/
		public static readonly StyledProperty<ICanShutdownProvider?> CanShutdownProviderProperty =
			AvaloniaProperty.Register<App, ICanShutdownProvider?>(nameof(CanShutdownProvider), null, defaultBindingMode: BindingMode.TwoWay);

		public App()
		{
			Name = "Wasabi Wallet";
			DataContext = new ApplicationViewModel();
		}

		public App(Func<Task> backendInitialiseAsync) : this()
		{
			_backendInitialiseAsync = backendInitialiseAsync;
		}

		public ICanShutdownProvider? CanShutdownProvider
		{
			get => GetValue(CanShutdownProviderProperty);
			set => SetValue(CanShutdownProviderProperty, value);
		}

		public override void Initialize()
		{
			AvaloniaXamlLoader.Load(this);
		}

		public override void OnFrameworkInitializationCompleted()
		{
			AutoBringIntoViewExtension.Initialise();

			if (!Design.IsDesignMode)
			{
				MainViewModel.Instance = new MainViewModel();

				if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
				{
					desktop.ShutdownRequested += DesktopOnShutdownRequested;

					desktop.MainWindow = new MainWindow
					{
						DataContext = MainViewModel.Instance
					};

					RxApp.MainThreadScheduler.Schedule(
						async () =>
						{
							await _backendInitialiseAsync();

							MainViewModel.Instance!.Initialize();
						});
				}
			}

			base.OnFrameworkInitializationCompleted();
		}

		private void DesktopOnShutdownRequested(object? sender, ShutdownRequestedEventArgs e)
		{
			if (CanShutdownProvider is { } provider)
			{
				e.Cancel = !provider.CanShutdown();
				Logger.LogDebug($"Cancellation of the shutdown set to: {e.Cancel}.");
			}
		}
	}
}
