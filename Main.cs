using EosAchiForGodot.EPIC.Services;
using Epic.OnlineServices;
using Godot;
using System;
using System.Linq;

public partial class Main : Node2D
{
	public override void _Ready()
	{
		EOSManager EOSManager = (EOSManager)this.GetChild(0);

		EOSManager.InitializeEOS();

		EOSManager.EOSLoaded += EOSManager_EOSLoaded;

		EOSManager.StatusUpdated += EOSManager_StatusUpdated;

		((Button)GetChild(7)).Disabled = true;

		((Button)GetChild(8)).Disabled = true;
	}

	private void EOSManager_StatusUpdated(object sender, EventArgs e)
	{
		((Label)this.GetChild(2)).Text = $"{(sender as EOSManager).ApplicationSettings.EOSState}...";
	}

	private void EOSManager_EOSLoaded(object sender, EventArgs e)
	{
		var achievements = (sender as EOSManager).ApplicationSettings.Achievements.Select(x => $"{x.AchievementId} {x.UnlockedDisplayName} {x.UnlockedDescription}");

		var achievementsListbox = (ItemList)this.GetChild(1);

		achievementsListbox.Clear();

		foreach (var item in achievements)
		{
			achievementsListbox.AddItem(item);
		}

		var playerAchievements = (sender as EOSManager).ApplicationSettings.PlayerAchievements
			.Where(x=>x.Progress >0)
			.Select(x => $"{x.AchievementId} {x.DisplayName} Progress: {x.Progress} Unlocked: {x.UnlockTime}");

		var playerAchievementListbox = (ItemList)this.GetChild(4);

		playerAchievementListbox.Clear();

		foreach (var item in playerAchievements)
		{
			playerAchievementListbox.AddItem(item);
		}

		((Button)GetChild(7)).Disabled = false;

		((Button)GetChild(8)).Disabled = false;
	}

	Utf8String selectedAchievementID = string.Empty;
	
	private void _on_item_list_item_selected(long index)
	{
		try
		{
			selectedAchievementID = ((EOSManager)this.GetChild(0)).ApplicationSettings.Achievements[Convert.ToInt32(index)].AchievementId;

			((Label)GetChild(6)).Text = $"Selected: {selectedAchievementID}";
		}
		catch (Exception e)
		{
			Console.WriteLine(e.Message);
		}

	}
	
	private void _on_button_button_up()
	{
		var ApplicationSettings = ((EOSManager)this.GetChild(0)).ApplicationSettings;

		var achievementDefinition = AchievementsService.GetAchievementDefinitionFromStringKey(ApplicationSettings, selectedAchievementID.ToString());

		if (AchievementsService.IsUnlocked(ApplicationSettings, achievementDefinition))
		{
			Console.WriteLine($"Achivement {achievementDefinition.UnlockedDisplayName} is already unlocked!", ConsoleColor.DarkRed);

			return;
		}

		AchievementsService.UnlockAchievements(achievementDefinition, ApplicationSettings);

		((Button)GetChild(7)).Disabled = true;

		((Button)GetChild(8)).Disabled = true;
	}
	
	private void _on_button_2_button_up()
	{
		AuthService.AuthLogout(((EOSManager)this.GetChild(0)).ApplicationSettings);
	}
}








