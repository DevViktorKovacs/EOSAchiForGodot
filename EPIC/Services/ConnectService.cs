using System;
using Epic.OnlineServices.Connect;
using Epic.OnlineServices;

namespace EosAchiForGodot.EPIC.Services
{
    public static class ConnectService
    {
        public static void ConnectLogin(EOSSettings applicationSettings)
        {
            var authInterface = applicationSettings.PlatformInterface.GetAuthInterface();

            if (authInterface == null)
            {
                applicationSettings.ErrorCode = Result.NotFound.ToString();

                applicationSettings.EOSState = EOSState.Failed;

                return;
            }
            var copyIdTokenOptions = new Epic.OnlineServices.Auth.CopyIdTokenOptions()
            {
                AccountId = EpicAccountId.FromString(applicationSettings.AccountId)
            };

            var result = authInterface.CopyIdToken(ref copyIdTokenOptions, out var userAuthToken);

            if (result == Result.Success)
            {
                Connect(applicationSettings, userAuthToken);
                return;
            }
            else if (Common.IsOperationComplete(result))
            {
                Console.WriteLine("CopyIdToken failed: " + result);
            }
        }

        private static void Connect(EOSSettings applicationSettings, Epic.OnlineServices.Auth.IdToken? userAuthToken)
        {
            var connectInterface = applicationSettings.PlatformInterface.GetConnectInterface();

            if (connectInterface == null)
            {
                Console.WriteLine("Failed to get connect interface");

                applicationSettings.ErrorCode = Result.NotFound.ToString();

                applicationSettings.EOSState = EOSState.Failed;

                return;
            }

            LoginOptions loginOptions;

            loginOptions = new LoginOptions()
            {
                Credentials = new Credentials()
                {
                    Type = ExternalCredentialType.EpicIdToken,
                    Token = userAuthToken.Value.JsonWebToken
                }
            };

            connectInterface.Login(ref loginOptions, null, (ref LoginCallbackInfo loginCallbackInfo) =>
            {
                Console.WriteLine($"Connect login {loginCallbackInfo.ResultCode}");

                if (loginCallbackInfo.ResultCode == Result.Success)
                {
                    var notifyAuthExpirationOptions = new AddNotifyAuthExpirationOptions();
                    var notifyLoginStatusChangedOptions = new AddNotifyLoginStatusChangedOptions();

                    applicationSettings.ConnectAuthExpirationNotificationId = connectInterface.AddNotifyAuthExpiration(ref notifyAuthExpirationOptions, null, AuthExpirationCallback);
                    applicationSettings.ConnectLoginStatusChangedNotificationId = connectInterface.AddNotifyLoginStatusChanged(ref notifyLoginStatusChangedOptions, null, LoginStatusChangedCallback);

                    applicationSettings.ProductUserId = loginCallbackInfo.LocalUserId.ToString();

                    applicationSettings.EOSState = EOSState.Connected;
                }
                else if (loginCallbackInfo.ResultCode == Result.InvalidUser)
                {
                    Console.WriteLine("Connect login failed: " + loginCallbackInfo.ResultCode, ConsoleColor.DarkRed);

                    applicationSettings.EOSState = EOSState.InvalidUser;

                    loginCallbackInfo = HandleInvalidUser(loginCallbackInfo, connectInterface, applicationSettings);

                }
                else if (Common.IsOperationComplete(loginCallbackInfo.ResultCode))
                {
                    Console.WriteLine("Connect login failed: " + loginCallbackInfo.ResultCode);

                    applicationSettings.ErrorCode = loginCallbackInfo.ResultCode.ToString();

                    applicationSettings.EOSState = EOSState.Failed;

                    applicationSettings.ErrorResult = loginCallbackInfo.ResultCode;
                }

            });

            return;
        }

        private static LoginCallbackInfo HandleInvalidUser(LoginCallbackInfo loginCallbackInfo, ConnectInterface connectInterface, EOSSettings applicationSettings)
        {
            var createUserOptions = new CreateUserOptions()
            {
                ContinuanceToken = loginCallbackInfo.ContinuanceToken
            };

            applicationSettings.EOSState = EOSState.CreatingUser;

            Console.WriteLine("Creating user...");

            connectInterface.CreateUser(ref createUserOptions, null, (ref CreateUserCallbackInfo createUserCallbackInfo) =>
            {
                if (createUserCallbackInfo.ResultCode == Result.Success)
                {
                    Console.WriteLine("User successfully created.", ConsoleColor.Green);

                    var notifyAuthExpirationOptions = new AddNotifyAuthExpirationOptions();
                    var notifyLoginStatusChangedOptions = new AddNotifyLoginStatusChangedOptions();

                    applicationSettings.ConnectAuthExpirationNotificationId = connectInterface.AddNotifyAuthExpiration(ref notifyAuthExpirationOptions, null, AuthExpirationCallback);
                    applicationSettings.ConnectLoginStatusChangedNotificationId = connectInterface.AddNotifyLoginStatusChanged(ref notifyLoginStatusChangedOptions, null, LoginStatusChangedCallback);

                    applicationSettings.ProductUserId = createUserCallbackInfo.LocalUserId.ToString();

                    applicationSettings.EOSState = EOSState.UserCreated;
                }
                else if (Common.IsOperationComplete(createUserCallbackInfo.ResultCode))
                {
                    Console.WriteLine("User creation failed: " + createUserCallbackInfo.ResultCode);

                    applicationSettings.ErrorCode = createUserCallbackInfo.ResultCode.ToString();

                    applicationSettings.EOSState = EOSState.Failed;

                    applicationSettings.ErrorResult = createUserCallbackInfo.ResultCode;
                }

            });

            return loginCallbackInfo;
        }

        private static void AuthExpirationCallback(ref AuthExpirationCallbackInfo data)
        {
            // Handle 10-minute warning prior to token expiration by calling Connect.Login()
        }
        private static void LoginStatusChangedCallback(ref LoginStatusChangedCallbackInfo data)
        {
            switch (data.CurrentStatus)
            {
                case LoginStatus.NotLoggedIn:
                    if (data.PreviousStatus == LoginStatus.LoggedIn)
                    {
                        // Handle token expiration
                    }
                    break;
                case LoginStatus.UsingLocalProfile:
                    break;
                case LoginStatus.LoggedIn:
                    break;
            }
        }

        public static void RemoveNotifications(EOSSettings applicationSettings)
        {
            applicationSettings.PlatformInterface.GetConnectInterface().RemoveNotifyAuthExpiration(applicationSettings.ConnectAuthExpirationNotificationId);
            applicationSettings.PlatformInterface.GetConnectInterface().RemoveNotifyLoginStatusChanged(applicationSettings.ConnectLoginStatusChangedNotificationId);
        }

        public static void GetToken(EOSSettings applicationSettings)
        {
            var authInterface = applicationSettings.PlatformInterface.GetAuthInterface();

            if (authInterface == null)
            {
                Console.WriteLine("Failed to get auth interface");
                return;
            }
            var copyIdTokenOptions = new Epic.OnlineServices.Auth.CopyIdTokenOptions()
            {
                AccountId = EpicAccountId.FromString(applicationSettings.AccountId)
            };

            var result = authInterface.CopyIdToken(ref copyIdTokenOptions, out var userAuthToken);

            if (result == Result.Success)
            {
                var connectInterface = applicationSettings.PlatformInterface.GetConnectInterface();
                if (connectInterface == null)
                {
                    Console.WriteLine("Failed to get connect interface");
                    return;
                }

                var loginOptions = new LoginOptions()
                {
                    Credentials = new Credentials()
                    {
                        Type = ExternalCredentialType.EpicIdToken,
                        Token = userAuthToken.Value.JsonWebToken
                    }
                };

                applicationSettings.AuthToken = userAuthToken.Value.JsonWebToken;

            }
        }
    }
}
