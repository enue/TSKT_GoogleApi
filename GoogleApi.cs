// このコードはApache License 2.0で公開されたコードの改変を含みます
// https://github.com/googlesamples/oauth-apps-for-windows
// http://www.apache.org/licenses/

﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UniRx.Async;
using System.Net.Sockets;
using System.Net;
using System.Threading.Tasks;
using System.Security.Cryptography;

namespace TSKT
{
    public class GoogleApi
    {
        public class Code
        {
            public string code;
            public string redirectUri;
            public string codeVerifier;
        }

        [System.Serializable]
        public class RefreshToken
        {
            public string access_token;
            public string token_type;
            public int expires_in;
            public string refresh_token;
        }

        [System.Serializable]
        public class AccessToken
        {
            public string access_token;
            public int expires_in;
            public string scope;
            public string token_type;
        }

        static public async UniTask<RefreshToken> RequestRefreshToken(string scope, string clientId, string clientSecret, string message = "Please return to the app.")
        {
            var code = await Authorize(scope, clientId, message);
            return await RequestRefreshToken(code, clientId, clientSecret);
        }

        static public async UniTask<RefreshToken> RequestRefreshToken(Code code, string clientId, string clientSecret)
        {
            var form = new WWWForm();
            form.AddField("code", code.code);
            form.AddField("code_verifier", code.codeVerifier);
            form.AddField("client_id", clientId);
            form.AddField("client_secret", clientSecret);
            form.AddField("redirect_uri", code.redirectUri);
            form.AddField("grant_type", "authorization_code");
            form.AddField("access_type", "offline");
            using (var request = UnityWebRequest.Post("https://www.googleapis.com/oauth2/v4/token", form))
            {
                request.SetRequestHeader("Content-Type", "application/x-www-form-urlencoded");

                await request.SendWebRequest();
                if (request.isNetworkError)
                {
                    Debug.LogWarning(request.error);
                    Debug.Log(request.downloadHandler.text);
                    return null;
                }
                if (request.isHttpError)
                {
                    Debug.LogWarning(request.error);
                    Debug.Log(request.downloadHandler.text);
                    return null;
                }
                var response = JsonUtility.FromJson<RefreshToken>(request.downloadHandler.text);
                return response;
            }
        }

        static public async UniTask<AccessToken> RequestAccessToken(string refreshToken, string clientId, string clientSecret)
        {
            var form = new WWWForm();
            form.AddField("refresh_token", refreshToken);
            form.AddField("client_id", clientId);
            form.AddField("client_secret", clientSecret);
            form.AddField("grant_type", "refresh_token");
            using (var request = UnityWebRequest.Post("https://www.googleapis.com/oauth2/v4/token", form))
            {
                request.SetRequestHeader("Content-Type", "application/x-www-form-urlencoded");

                await request.SendWebRequest();
                if (request.isNetworkError)
                {
                    Debug.LogWarning(request.error);
                    Debug.Log(request.downloadHandler.text);
                    return null;
                }
                if (request.isHttpError)
                {
                    Debug.LogWarning(request.error);
                    Debug.Log(request.downloadHandler.text);
                    return null;
                }
                var response = JsonUtility.FromJson<AccessToken>(request.downloadHandler.text);
                return response;
            }
        }

        public static int GetRandomUnusedPort()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }

        public static string RandomDataBase64url(uint length)
        {
            var rng = new RNGCryptoServiceProvider();
            var bytes = new byte[length];
            rng.GetBytes(bytes);
            return Base64urlencodeNoPadding(bytes);
        }

        public static string Base64urlencodeNoPadding(byte[] buffer)
        {
            string base64 = System.Convert.ToBase64String(buffer);

            // Converts base64 to base64url.
            base64 = base64.Replace("+", "-");
            base64 = base64.Replace("/", "_");
            // Strips padding.
            base64 = base64.Replace("=", "");

            return base64;
        }

        public static byte[] Sha256(string inputStirng)
        {
            byte[] bytes = System.Text.Encoding.ASCII.GetBytes(inputStirng);
            SHA256Managed sha256 = new SHA256Managed();
            return sha256.ComputeHash(bytes);
        }

        // https://github.com/googlesamples/oauth-apps-for-windows/blob/master/OAuthDesktopApp/OAuthDesktopApp/MainWindow.xaml.cs
        public static async UniTask<Code> Authorize(string scope, string clientId, string message)
        {
            var codeVerifier = RandomDataBase64url(32);
            var state = RandomDataBase64url(32);
            var codeChallenge = Base64urlencodeNoPadding(Sha256(codeVerifier));
            const string codeChallengeMethod = "S256";
            var redirectURI = string.Format("http://{0}:{1}/", IPAddress.Loopback, GetRandomUnusedPort());

            // Creates an HttpListener to listen for requests on that redirect URI.
            var http = new HttpListener();
            http.Prefixes.Add(redirectURI);
            http.Start();

            // Creates the OAuth 2.0 authorization request.
            var authorizationRequest = "https://accounts.google.com/o/oauth2/v2/auth"
                + "?scope=" + scope
                + "&redirect_uri=" + System.Uri.EscapeDataString(redirectURI)
                + "&response_type=code"
                + "&state=" + state
                + "&client_id=" + clientId
                + "&code_challenge=" + codeChallenge
                + "&code_challenge_method=" + codeChallengeMethod;

            Application.OpenURL(authorizationRequest);

            var context = await http.GetContextAsync();

            // Sends an HTTP response to the browser.
            var response = context.Response;
            var responseString = $"<html><head><meta http-equiv='refresh' content='10;url=https://google.com'></head><body>{message}</body></html>";
            var buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
            response.ContentLength64 = buffer.Length;
            var responseOutput = response.OutputStream;
            var responseTask = responseOutput.WriteAsync(buffer, 0, buffer.Length).ContinueWith((task) =>
            {
                responseOutput.Close();
                http.Stop();
            });

            if (context.Request.QueryString.Get("error") != null)
            {
                Debug.Log(string.Format("OAuth authorization error: {0}.", context.Request.QueryString.Get("error")));
                return null;
            }
            if (context.Request.QueryString.Get("code") == null
                || context.Request.QueryString.Get("state") == null)
            {
                Debug.Log("Malformed authorization response. " + context.Request.QueryString);
                return null;
            }

            // Compares the receieved state to the expected value, to ensure that
            // this app made the request which resulted in authorization.
            var incoming_state = context.Request.QueryString.Get("state");
            if (incoming_state != state)
            {
                Debug.Log(string.Format("Received request with invalid state ({0})", incoming_state));
                return null;
            }

            var code = context.Request.QueryString.Get("code");

            return new Code()
            {
                code = code,
                codeVerifier = codeVerifier,
                redirectUri = redirectURI
            };
        }
    }
}