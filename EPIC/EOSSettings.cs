using Epic.OnlineServices;
using Epic.OnlineServices.Achievements;
using Epic.OnlineServices.Platform;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.ObjectModel;
using Epic.OnlineServices.Auth;
using Godot;

namespace EosAchiForGodot.EPIC
{
    public class EOSSettings
    {
        public string ProductId { get; private set; } = "YOUR_DATA";

        public string ClientId { get; private set; } = "YOUR_DATA";

        public string ClientSecret { get; private set; } = "YOUR_DATA";

        public string SandboxId { get; private set; } = "YOUR_DATA";  

        public string DeploymentId { get; private set; } = "YOUR_DATA"; 

        public string AccountId { get; set; }

        public string DisplayName { get; set; }

        public string ErrorCode { get; set; }

        public Result ErrorResult { get; set; }

        public Utf8String AuthToken { get; set; }

        public string ProductUserId { get; set; } = "";

        public EOSState EOSState { get; set; }

        public DefinitionV2 SelectedAchievement { get; set; }

        public ObservableCollection<DefinitionV2> Achievements { get; set; }

        public ObservableCollection<PlayerAchievement> PlayerAchievements { get; set; }

        public LoginCredentialType LoginCredentialType { get; private set; } = LoginCredentialType.PersistentAuth;

        public ExternalCredentialType ExternalCredentialType { get; private set; } = ExternalCredentialType.Epic;

        public string Id { get; private set; } = "";
        public string Token { get; private set; } = "";

        public bool KeepLoggedOut { get; set; }

        public AuthScopeFlags ScopeFlags
        {
            get
            {
                return AuthScopeFlags.BasicProfile;
            }
        }

        public ulong ConnectAuthExpirationNotificationId { get; set; }

        public ulong ConnectLoginStatusChangedNotificationId { get; set; }

        public PlatformInterface PlatformInterface { get; set; }

        public void Initialize()
        {
            Achievements = new ObservableCollection<DefinitionV2>();

            PlayerAchievements = new ObservableCollection<PlayerAchievement>();

            KeepLoggedOut = false;

            ErrorCode = "";

            var Args = OS.GetCmdlineArgs();

            var commandLineArgs = new Dictionary<string, string>();

            if (Args.Length > 1)
            {
                for (int index = 0; index < Args.Length; index += 2)
                {
                    commandLineArgs.Add(Args[index], Args[index + 1]);
                }
            }

            ProductId = commandLineArgs.ContainsKey("-productid") ? commandLineArgs.GetValueOrDefault("-productid") : ProductId;

            SandboxId = commandLineArgs.ContainsKey("-sandboxid") ? commandLineArgs.GetValueOrDefault("-sandboxid") : SandboxId;

            DeploymentId = commandLineArgs.ContainsKey("-deploymentid") ? commandLineArgs.GetValueOrDefault("-deploymentid") : DeploymentId;

            ClientId = commandLineArgs.ContainsKey("-clientid") ? commandLineArgs.GetValueOrDefault("-clientid") : ClientId;

            ClientSecret = commandLineArgs.ContainsKey("-clientsecret") ? commandLineArgs.GetValueOrDefault("-clientsecret") : ClientSecret;

            LoginCredentialType = commandLineArgs.ContainsKey("-logincredentialtype") ? (LoginCredentialType)Enum.Parse(typeof(LoginCredentialType), commandLineArgs.GetValueOrDefault("-logincredentialtype")) : LoginCredentialType;

            Id = commandLineArgs.ContainsKey("-id") ? commandLineArgs.GetValueOrDefault("-id") : Id;

            Token = commandLineArgs.ContainsKey("-token") ? commandLineArgs.GetValueOrDefault("-token") : Token;

            ExternalCredentialType = commandLineArgs.ContainsKey("-externalcredentialtype") ? (ExternalCredentialType)Enum.Parse(typeof(ExternalCredentialType), commandLineArgs.GetValueOrDefault("-externalcredentialtype")) : ExternalCredentialType;
        }

        public void AchievementsUnlockedCallback(ref OnAchievementsUnlockedCallbackV2Info data)
        {
            var achievementId = data.AchievementId;

            Console.WriteLine("Achievement unlocked: " + achievementId, ConsoleColor.Green);

            EOSState = EOSState.UnhandledAchievementNotification;

            SelectedAchievement = Achievements.Where(x => x.AchievementId == achievementId).FirstOrDefault();

            var playerachi = PlayerAchievements.Where(x => x.AchievementId == achievementId).FirstOrDefault();

            playerachi.Progress = 1;
        }
    }
}
