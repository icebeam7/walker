using System;
using System.Threading.Tasks;
using Windows.Security.Authentication.Web;
using Windows.Security.Authentication.Web.Core;
using Windows.Security.Credentials;
using Windows.Storage;

namespace Walker.Office365
{
    internal static class AuthenticationHelper
    {
        static string clientId = App.Current.Resources["ida:ClientID"].ToString();
        static string tenant = App.Current.Resources["ida:Domain"].ToString();
        static string AADInstance = App.Current.Resources["ida:AADInstance"].ToString();
        static string authority = AADInstance + tenant;
        public const string ResourceUrl = "https://graph.microsoft.com/";
        private static WebAccountProvider aadAccountProvider = null;
        private static WebAccount userAccount = null;
        public static ApplicationDataContainer _settings = ApplicationData.Current.RoamingSettings;

        public static async Task<string> GetTokenHelperAsync()
        {
            string token = null;
            aadAccountProvider = await WebAuthenticationCoreManager.FindAccountProviderAsync("https://login.microsoft.com", authority);
            var userID = _settings.Values["userID"];

            if (userID != null)
            {
                WebTokenRequest webTokenRequest = new WebTokenRequest(aadAccountProvider, string.Empty, clientId);
                webTokenRequest.Properties.Add("resource", ResourceUrl);
                userAccount = await WebAuthenticationCoreManager.FindAccountAsync(aadAccountProvider, (string)userID);
                WebTokenRequestResult webTokenRequestResult = await WebAuthenticationCoreManager.RequestTokenAsync(webTokenRequest, userAccount);

                if (webTokenRequestResult.ResponseStatus == WebTokenRequestStatus.Success || webTokenRequestResult.ResponseStatus == WebTokenRequestStatus.AccountSwitch)
                {
                    WebTokenResponse webTokenResponse = webTokenRequestResult.ResponseData[0];
                    userAccount = webTokenResponse.WebAccount;
                    token = webTokenResponse.Token;
                }
                else
                    SignOut();
            }
            else
            {
                WebTokenRequest webTokenRequest = new WebTokenRequest(aadAccountProvider, string.Empty, clientId, WebTokenRequestPromptType.ForceAuthentication);
                webTokenRequest.Properties.Add("resource", ResourceUrl);

                WebTokenRequestResult webTokenRequestResult = await WebAuthenticationCoreManager.RequestTokenAsync(webTokenRequest);
                if (webTokenRequestResult.ResponseStatus == WebTokenRequestStatus.Success)
                {
                    WebTokenResponse webTokenResponse = webTokenRequestResult.ResponseData[0];
                    userAccount = webTokenResponse.WebAccount;
                    token = webTokenResponse.Token;

                }
            }

            if (userAccount != null)
            {
                _settings.Values["userID"] = userAccount.Id;
                _settings.Values["userEmail"] = userAccount.UserName;
                _settings.Values["userName"] = userAccount.Properties["DisplayName"];
                return token;
            }

            else
            {
                SignOut();
                return null;
            }
        }

        public static void SignOut()
        {
            _settings.Values["userID"] = null;
            _settings.Values["userEmail"] = null;
            _settings.Values["userName"] = null;
        }

        public static string GetAppRedirectURI()
        {
            return string.Format("ms-appx-web://microsoft.aad.brokerplugin/{0}", WebAuthenticationBroker.GetCurrentApplicationCallbackUri().Host).ToUpper();
        }
    }
}