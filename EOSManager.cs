using EosAchiForGodot.EPIC;
using EosAchiForGodot.EPIC.Services;
using Epic.OnlineServices;
using Epic.OnlineServices.Logging;
using Epic.OnlineServices.Platform;
using Godot;
using System;

public partial class EOSManager : Node
{
	Timer timer;

	public EOSSettings ApplicationSettings;

	public event EventHandler<EventArgs> EOSLoaded;

	public event EventHandler<EventArgs> StatusUpdated;
	public override void _Ready()
	{
		timer = (Timer)GetChild(0);

		ApplicationSettings = new EOSSettings();

		ApplicationSettings.EOSState = EOSState.Uninitialized;

		ApplicationSettings.Initialize();
	}

	public void InitializeEOS()
	{
		var initializeOptions = new InitializeOptions()
		{
			ProductName = "EOSACHIFORGODOT",
			ProductVersion = "1.0.0",
		};

		var result = PlatformInterface.Initialize(ref initializeOptions);

		Console.WriteLine($"EOS init result: {result}");

		_ = LoggingInterface.SetLogLevel(LogCategory.AllCategories, LogLevel.Info);

		_ = LoggingInterface.SetCallback((ref LogMessage message) => Console.WriteLine($"[{message.Level}] {message.Category} - {message.Message}"));

		var options = new Options()
		{
			ProductId = ApplicationSettings.ProductId,
			SandboxId = ApplicationSettings.SandboxId,
			ClientCredentials = new ClientCredentials()
			{
				ClientId = ApplicationSettings.ClientId,
				ClientSecret = ApplicationSettings.ClientSecret
			},
			DeploymentId = ApplicationSettings.DeploymentId,
			Flags = PlatformFlags.DisableOverlay,
			IsServer = false
		};

		PlatformInterface platformInterface = PlatformInterface.Create(ref options);

		if (platformInterface == null)
		{
			Console.WriteLine($"Failed to create platform. Ensure the relevant settings are set.");

			ApplicationSettings.ErrorCode = Result.NotFound.ToString();

			ApplicationSettings.EOSState = EOSState.Failed;

		}
		else
		{
			ApplicationSettings.EOSState = EOSState.Ready;
		}

		ApplicationSettings.PlatformInterface = platformInterface;

		timer.WaitTime = 0.4f;

		timer.Start();
	}

	public bool TryToSetAchievement(string achievementName)
	{
		if (ApplicationSettings.PlayerAchievements.Count == 0) return false;

		var achievementDefinition = AchievementsService.GetAchievementDefinitionFromStringKey(ApplicationSettings, achievementName);

		if (AchievementsService.IsUnlocked(ApplicationSettings, achievementDefinition))
		{
			Console.WriteLine($"Achivement {achievementDefinition.UnlockedDisplayName} is already unlocked!", ConsoleColor.DarkRed);

			return false;
		}

		AchievementsService.UnlockAchievements(achievementDefinition, ApplicationSettings);

		return true;
	}

	EOSState previousState;

	private void _on_timer_timeout()
	{
		ApplicationSettings.PlatformInterface?.Tick();

		if (ApplicationSettings.EOSState != previousState)
		{
			previousState = ApplicationSettings.EOSState;

			StatusUpdated?.Invoke(this, new EventArgs());
		}

		switch (ApplicationSettings.EOSState)
		{
			case EOSState.Completed:
				return;

			case EOSState.Uninitialized:
				return;

			case EOSState.Ready:
				ApplicationSettings.EOSState = EOSState.WaitForLoad;
				return;

			case EOSState.WaitForLoad:
				ApplicationSettings.EOSState = EOSState.Authenticating;
				AuthService.AuthLogin(ApplicationSettings);
				return;

			case EOSState.Authenticating:
				return;

			case EOSState.Authenticated:
				ConnectService.ConnectLogin(ApplicationSettings);
				ApplicationSettings.EOSState = EOSState.Connecting;
				return;

			case EOSState.Connecting:
				return;

			case EOSState.CreatingUser:
				return;

			case EOSState.UserCreated:
				ConnectService.ConnectLogin(ApplicationSettings);
				ApplicationSettings.EOSState = EOSState.Connecting;
				return;

			case EOSState.Connected:
				ApplicationSettings.EOSState = EOSState.QueryingAchievements;
				AchievementsService.QueryDefinitions(ApplicationSettings);
				return;

			case EOSState.QueryingAchievements:
				return;

			case EOSState.AchievementsLoaded:
				ApplicationSettings.EOSState = EOSState.QueryingPlayerAchievements;
				AchievementsService.QueryPlayerAchievements(ApplicationSettings);
				return;

			case EOSState.QueryingPlayerAchievements:
				return;

			case EOSState.PlayerAchievementsLoaded:
				EOSLoaded?.Invoke(this, new EventArgs());
				ApplicationSettings.EOSState = EOSState.Completed;
				return;

			case EOSState.Failed:
				HandleFailState();
				return;
		}

		if (ApplicationSettings.EOSState == EOSState.UnhandledAchievementNotification)
		{
			ApplicationSettings.EOSState = EOSState.AchievementsLoaded;

			ApplicationSettings.PlayerAchievements.Clear();

			return;
		}

	}

	private void HandleFailState()
	{
		Console.WriteLine($"EOS failed with code: {ApplicationSettings.ErrorCode}");

		if (ApplicationSettings.ErrorResult == Result.InvalidAuth)
		{
			ApplicationSettings.EOSState = EOSState.Authenticating;

			AuthService.AuthLogin(ApplicationSettings);
		}
	}

}


