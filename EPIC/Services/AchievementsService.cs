using System;
using Epic.OnlineServices.Achievements;
using Epic.OnlineServices;
using System.Linq;

namespace EosAchiForGodot.EPIC.Services
{
    public static class AchievementsService
    {
        public static void QueryDefinitions(EOSSettings applicationSettings)
        {
            var queryLeaderboardDefinitionsOptions = new QueryDefinitionsOptions()
            {
                LocalUserId = ProductUserId.FromString(applicationSettings.ProductUserId)
            };

            Console.WriteLine($"Querying achievement for {applicationSettings.ProductUserId.Substring(0, 8)}...", ConsoleColor.Yellow);

            applicationSettings.PlatformInterface.GetAchievementsInterface().QueryDefinitions(ref queryLeaderboardDefinitionsOptions, null, (ref OnQueryDefinitionsCompleteCallbackInfo onQueryDefinitionsCompleteCallbackInfo) =>
            {
                Console.WriteLine($"QueryDefinitions {onQueryDefinitionsCompleteCallbackInfo.ResultCode}", ConsoleColor.Green);

                if (onQueryDefinitionsCompleteCallbackInfo.ResultCode == Result.Success)
                {
                    var getAchievementDefinitionCountOptions = new GetAchievementDefinitionCountOptions();
                    var achievementDefinitionCount = applicationSettings.PlatformInterface.GetAchievementsInterface().GetAchievementDefinitionCount(ref getAchievementDefinitionCountOptions);

                    for (uint i = 0; i < achievementDefinitionCount; i++)
                    {
                        var copyAchievementDefinitionByIndexOptions = new CopyAchievementDefinitionV2ByIndexOptions()
                        {
                            AchievementIndex = i
                        };
                        var result = applicationSettings.PlatformInterface.GetAchievementsInterface().CopyAchievementDefinitionV2ByIndex(ref copyAchievementDefinitionByIndexOptions, out var achievementDefinition);

                        if (result == Result.Success)
                        {
                            applicationSettings.Achievements.Add(achievementDefinition.Value);
                        }
                    }

                    applicationSettings.EOSState = EOSState.AchievementsLoaded;
                }
                else
                {
                    applicationSettings.ErrorCode = onQueryDefinitionsCompleteCallbackInfo.ResultCode.ToString();

                    applicationSettings.EOSState = EOSState.Failed;

                    applicationSettings.ErrorResult = onQueryDefinitionsCompleteCallbackInfo.ResultCode;
                }

                AddNotification(applicationSettings);

            });
        }

        public static void QueryPlayerAchievements(EOSSettings applicationSettings)
        {
            var queryPlayerAchievementsOptions = new QueryPlayerAchievementsOptions()
            {
                LocalUserId = ProductUserId.FromString(applicationSettings.ProductUserId),
                TargetUserId = ProductUserId.FromString(applicationSettings.ProductUserId)
            };

            applicationSettings.PlatformInterface.GetAchievementsInterface().QueryPlayerAchievements(ref queryPlayerAchievementsOptions, null, (ref OnQueryPlayerAchievementsCompleteCallbackInfo onQueryPlayerAchievementsCompleteCallbackInfo) =>
            {


                if (onQueryPlayerAchievementsCompleteCallbackInfo.ResultCode == Result.Success)
                {
                    Console.WriteLine($"QueryPlayerAchievements {onQueryPlayerAchievementsCompleteCallbackInfo.ResultCode}", ConsoleColor.Green);

                    var getPlayerAchievementCountOptions = new GetPlayerAchievementCountOptions()
                    {
                        UserId = ProductUserId.FromString(applicationSettings.ProductUserId)
                    };
                    var playerAchievementCount = applicationSettings.PlatformInterface.GetAchievementsInterface().GetPlayerAchievementCount(ref getPlayerAchievementCountOptions);

                    for (uint i = 0; i < playerAchievementCount; i++)
                    {
                        var copyPlayerAchievementByIndexOptions = new CopyPlayerAchievementByIndexOptions()
                        {
                            LocalUserId = ProductUserId.FromString(applicationSettings.ProductUserId),
                            TargetUserId = ProductUserId.FromString(applicationSettings.ProductUserId),
                            AchievementIndex = i
                        };
                        var result = applicationSettings.PlatformInterface.GetAchievementsInterface().CopyPlayerAchievementByIndex(ref copyPlayerAchievementByIndexOptions, out var playerAchievement);

                        if (result == Result.Success)
                        {
                            applicationSettings.PlayerAchievements.Add(playerAchievement.Value);
                        }
                    }

                    applicationSettings.EOSState = EOSState.PlayerAchievementsLoaded;
                }

            });
        }

        public static void UnlockAchievements(DefinitionV2 achievementDefinition, EOSSettings applicationSettings)
        {
            var unlockAchievementsOptions = new UnlockAchievementsOptions()
            {
                UserId = ProductUserId.FromString(applicationSettings.ProductUserId),
                AchievementIds = new Utf8String[] { achievementDefinition.AchievementId }
            };

            applicationSettings.PlatformInterface.GetAchievementsInterface().UnlockAchievements(ref unlockAchievementsOptions, null, (ref OnUnlockAchievementsCompleteCallbackInfo onUnlockAchievementsCompleteCallbackInfo) =>
            {
                Console.WriteLine($"UnlockAchievements {onUnlockAchievementsCompleteCallbackInfo.ResultCode}");

                if (onUnlockAchievementsCompleteCallbackInfo.ResultCode == Result.Success)
                {
                    Console.WriteLine("Successfully unlocked.");
                }
                else
                {
                    Console.WriteLine("Unlock failed: " + onUnlockAchievementsCompleteCallbackInfo.ResultCode);
                }
            });
        }

        public static ulong AddNotification(EOSSettings applicationSettings)
        {
            var addNotifyAchievementsUnlockedV2Options = new AddNotifyAchievementsUnlockedV2Options();

            return applicationSettings.PlatformInterface.GetAchievementsInterface().AddNotifyAchievementsUnlockedV2(ref addNotifyAchievementsUnlockedV2Options, null, applicationSettings.AchievementsUnlockedCallback);
        }


        public static void RemoveNotification(ulong notificationId, EOSSettings applicationSettings)
        {
            applicationSettings.PlatformInterface.GetAchievementsInterface().RemoveNotifyAchievementsUnlocked(notificationId);
        }

        public static DefinitionV2 GetAchievementDefinitionFromStringKey(EOSSettings applicationSettings, string key)
        {
            var result = applicationSettings.Achievements.Where(x => string.Equals(x.AchievementId, key)).FirstOrDefault();

            return result;
        }

        public static bool IsUnlocked(EOSSettings applicationSettings, DefinitionV2 definition)
        {
            var playerAchievementDefinition = applicationSettings.PlayerAchievements.Where(x => x.AchievementId == definition.AchievementId).FirstOrDefault();

            Console.WriteLine($"Achivement progress for {playerAchievementDefinition.DisplayName}: {playerAchievementDefinition.Progress}");

            return (playerAchievementDefinition.Progress > 0);
        }
    }
}
