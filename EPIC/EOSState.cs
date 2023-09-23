namespace EosAchiForGodot.EPIC
{
    public enum EOSState
    {
        Uninitialized = 0,
        Authenticating = 1,
        Authenticated = 2,
        Connecting = 3,
        Connected = 4,
        Persistent = 5,
        QueryingAchievements = 6,
        AchievementsLoaded = 7,
        QueryingPlayerAchievements = 8,
        PlayerAchievementsLoaded = 9,
        UnlockingAchievement = 10,
        UnhandledAchievementNotification = 11,
        InvalidUser = 12,
        CreatingUser = 13,
        UserCreated = 14,
        Ready = 15,
        WaitForLoad = 16,
        Completed = 17,
        Failed = 18,
    }
}
