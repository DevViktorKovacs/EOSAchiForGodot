using Epic.OnlineServices;
using Epic.OnlineServices.Auth;
using Epic.OnlineServices.UserInfo;
using System;

namespace EosAchiForGodot.EPIC.Services
{
    public static class AuthService
    {
        public static void AuthLogin(EOSSettings applicationSettings)
        {
            Console.WriteLine("Getting auth interface...", ConsoleColor.DarkYellow);

            var authInterface = applicationSettings.PlatformInterface.GetAuthInterface();

            if (authInterface == null)
            {
                Console.WriteLine("Failed to get auth interface");

                applicationSettings.ErrorCode = Result.NotFound.ToString();

                applicationSettings.EOSState = EOSState.Failed;

                return;
            }

            var loginOptions = new LoginOptions()
            {
                Credentials = new Credentials()
                {
                    Type = applicationSettings.LoginCredentialType,
                    Id = null,
                    Token = null,
                    ExternalType = applicationSettings.ExternalCredentialType
                },
                ScopeFlags = applicationSettings.ScopeFlags
            };

            var firstLoginOptions = new LoginOptions()
            {
                Credentials = new Credentials()
                {
                    Type = LoginCredentialType.AccountPortal,
                    Id = applicationSettings.Id,
                    Token = applicationSettings.Token,
                    ExternalType = applicationSettings.ExternalCredentialType
                },
                ScopeFlags = applicationSettings.ScopeFlags
            };

            if (applicationSettings.ErrorResult == Result.InvalidAuth)
            {
                loginOptions = firstLoginOptions;

                applicationSettings.ErrorCode = string.Empty;

                applicationSettings.ErrorResult = Result.Success;
            }

            Console.WriteLine($"Trying to auth with: {loginOptions.Credentials.Value.Type}", ConsoleColor.DarkYellow);

            // Ensure platform tick is called on an interval, or the following call will never callback.
            authInterface.Login(ref loginOptions, null, (ref LoginCallbackInfo loginCallbackInfo) =>
            {
                Console.WriteLine($"Auth login {loginCallbackInfo.ResultCode}");

                if (loginCallbackInfo.ResultCode == Result.Success)
                {
                    applicationSettings.AccountId = loginCallbackInfo.LocalUserId.ToString();

                    var userInfoInterface = applicationSettings.PlatformInterface.GetUserInfoInterface();
                    if (userInfoInterface == null)
                    {
                        Console.WriteLine("Failed to get user info interface");

                        applicationSettings.ErrorCode = Result.InvalidUser.ToString();

                        applicationSettings.EOSState = EOSState.Failed;

                        return;
                    }

                    // https://dev.epicgames.com/docs/services/en-US/Interfaces/UserInfo/index.html#retrievinguserinfobyaccountidentifier
                    // The first step in dealing with user info is to call EOS_UserInfo_QueryUserInfo with an EOS_UserInfo_QueryUserInfoOptions data structure.
                    // This will download the most up-to-date version of a user's information into the local cache.
                    // To perform the EOS_UserInfo_QueryUserInfo call, create and initialize an EOS_UserInfo_QueryUserInfoOptions
                    var queryUserInfoOptions = new QueryUserInfoOptions()
                    {
                        LocalUserId = loginCallbackInfo.LocalUserId,
                        TargetUserId = loginCallbackInfo.LocalUserId
                    };

                    userInfoInterface.QueryUserInfo(ref queryUserInfoOptions, null, (ref QueryUserInfoCallbackInfo queryUserInfoCallbackInfo) =>
                    {
                        Console.WriteLine($"QueryUserInfo {queryUserInfoCallbackInfo.ResultCode}", ConsoleColor.DarkYellow);

                        if (queryUserInfoCallbackInfo.ResultCode == Result.Success)
                        {
                            // https://dev.epicgames.com/docs/services/en-US/Interfaces/UserInfo/index.html#examininguserinformation
                            // Once you have retrieved information about a specific user from the online service, you can request a copy of that data with the EOS_UserInfo_CopyUserInfo function.
                            // This function requires an EOS_UserInfo_CopyUserInfoOptions structure 
                            var copyUserInfoOptions = new CopyUserInfoOptions()
                            {
                                LocalUserId = queryUserInfoCallbackInfo.LocalUserId,
                                TargetUserId = queryUserInfoCallbackInfo.TargetUserId
                            };

                            var result = userInfoInterface.CopyUserInfo(ref copyUserInfoOptions, out var userInfoData);
                            Console.WriteLine($"CopyUserInfo: {result}");

                            if (userInfoData != null)
                            {
                                applicationSettings.DisplayName = userInfoData.Value.DisplayName;
                            }

                            applicationSettings.EOSState = EOSState.Authenticated;
                        }
                    });
                }
                else if (Common.IsOperationComplete(loginCallbackInfo.ResultCode))
                {
                    Console.WriteLine("Login failed: " + loginCallbackInfo.ResultCode);

                    applicationSettings.ErrorCode = loginCallbackInfo.ResultCode.ToString();

                    applicationSettings.EOSState = EOSState.Failed;

                    applicationSettings.ErrorResult = loginCallbackInfo.ResultCode;
                }
            });
        }

        public static void AuthLogout(EOSSettings applicationSettings)
        {
            // https://dev.epicgames.com/docs/services/en-US/Interfaces/Auth/index.html#logout
            // To log out, make a call to EOS_Auth_Logout with an EOS_Auth_LogoutOptions data structure. When the operation completes, your callback EOS_Auth_OnLogoutCallback will run.
            var logoutOptions = new LogoutOptions()
            {
                LocalUserId = EpicAccountId.FromString(applicationSettings.AccountId)
            };

            applicationSettings.PlatformInterface.GetAuthInterface().Logout(ref logoutOptions, null, (ref LogoutCallbackInfo logoutCallbackInfo) =>
            {
                Console.WriteLine($"Logout {logoutCallbackInfo.ResultCode}");

                if (logoutCallbackInfo.ResultCode == Result.Success)
                {
                    // If the EOS_LCT_PersistentAuth login type has been used, call the function EOS_Auth_DeletePersistentAuth to revoke the cached authentication as well.
                    // This permanently erases the local user login on PC Desktop and Mobile.
                    var deletePersistentAuthOptions = new DeletePersistentAuthOptions();
                    applicationSettings.PlatformInterface.GetAuthInterface().DeletePersistentAuth(ref deletePersistentAuthOptions, null, (ref DeletePersistentAuthCallbackInfo deletePersistentAuthCallbackInfo) =>
                    {
                        Console.WriteLine($"DeletePersistentAuth {deletePersistentAuthCallbackInfo.ResultCode}");

                        if (deletePersistentAuthCallbackInfo.ResultCode == Result.Success)
                        {
                            Console.WriteLine("Persistent auth deleted.");
                        }
                    });
                }
                else if (Common.IsOperationComplete(logoutCallbackInfo.ResultCode))
                {
                    Console.WriteLine("Logout failed: " + logoutCallbackInfo.ResultCode, ConsoleColor.DarkRed);
                }
            });
        }
    }
}
