/*
using System;
using System.Collections;
using System.Net;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using Verse;

namespace Rimionship
{
	[Serializable]
	public class ApiCodeTokenResponse
	{
		public string access_token;
		public int expires_in;
		public string refresh_token;
		public string[] scope;
		public string token_type;
	}

	public class Dummy : MonoBehaviour
	{
	}

	public class Authentication
	{
		private const string twitchAuthUrl = "https://id.twitch.tv/oauth2/authorize";
		private const string twitchClientId = "6lhl8584kr6gksdtia7tn3pbsj9xod";
		private const string twitchClientSecret = "e0n9wytn4f5hlq6ij8bh484bs12lv6";
		private const string twitchRedirectUrl = "http://localhost/auth/twitch/callback/";
		private string twitchAuthStateVerify;

		public Action<string> tokenCallback;
		public string twitchAuthToken;

		private HttpListener localServer;
		private IAsyncResult asyncResult;

		public void InitiateTwitchAuth()
		{
			// list of scopes we want
			var scopes = new[]
			{
				"user:read:email",
				// "chat:edit",
				// "chat:read",
				// "channel:read:redemptions",
				// "channel_subscriptions",
				// "user:read:broadcast",
				// "user:edit:broadcast",
				// "channel:manage:redemptions"
			};

			// generate something for the "state" parameter.
			// this can be whatever you want it to be, it's gonna be "echoed back" to us as is and should be used to
			// verify the redirect back from Twitch is valid.
			twitchAuthStateVerify = Guid.NewGuid().ToString();

			// query parameters for the Twitch auth URL
			var query = "client_id=" + twitchClientId + "&" +
				 "redirect_uri=" + UnityWebRequest.EscapeURL(twitchRedirectUrl) + "&" +
				 "state=" + twitchAuthStateVerify + "&" +
				 "response_type=code&" +
				 "scope=" + string.Join("+", scopes);

			// start our local webserver to receive the redirect back after Twitch authenticated
			StartLocalWebserver();

			// open the users browser and send them to the Twitch auth URL
			Application.OpenURL($"{twitchAuthUrl}?{query}");
		}

		void StartLocalWebserver()
		{
			localServer = new HttpListener();
			localServer.Prefixes.Add(twitchRedirectUrl);
			localServer.Start();
			asyncResult = localServer.BeginGetContext(new AsyncCallback(IncomingHttpRequest), localServer);
		}

		void IncomingHttpRequest(IAsyncResult result)
		{
			try
			{
				// get back the reference to our http listener
				var httpListener = (HttpListener)result.AsyncState;

				// fetch the context object
				var httpContext = httpListener.EndGetContext(result);

				// if we'd like the HTTP listener to accept more incoming requests, we'd just restart the "get context" here:
				// httpListener.BeginGetContext(new AsyncCallback(IncomingHttpRequest),httpListener);
				// however, since we only want/expect the one, single auth redirect, we don't need/want this, now.
				// but this is what you would do if you'd want to implement more (simple) "webserver" functionality
				// in your project.

				// the context object has the request object for us, that holds details about the incoming request
				var httpRequest = httpContext.Request;

				Log.Warning($"httpRequest = {httpRequest.QueryString}");
				foreach (var key in httpRequest.QueryString.AllKeys)
					Log.Warning($"- {key} = {httpRequest.QueryString.Get(key)}");

				var code = httpRequest.QueryString.Get("code");
				var state = httpRequest.QueryString.Get("state");

				Log.Warning($"code = {code}");
				Log.Warning($"state = {state}");

				// check that we got a code value and the state value matches our remembered one
				if (code != null && code.Length > 0 && state == twitchAuthStateVerify)
				{
					// if all checks out, use the code to exchange it for the actual auth token at the API
					GetTokenFromCode(code);
				}

				Log.Warning($"sending response");

				// build a response to send an "ok" back to the browser for the user to see
				var httpResponse = httpContext.Response;
				var responseString = "<html><body><b>DONE!</b><br>(You can close this tab/window now)</body></html>";
				var buffer = Encoding.UTF8.GetBytes(responseString);

				// send the output to the client browser
				httpResponse.ContentLength64 = buffer.Length;
				var output = httpResponse.OutputStream;
				output.Write(buffer, 0, buffer.Length);
				output.Close();

				Log.Warning($"stopping server");

				// the HTTP listener has served it's purpose, shut it down
				httpListener.Stop();
				// obv. if we had restarted the waiting for more incoming request, above, we'd not Stop() it here.
			}
			catch (Exception ex)
			{
				Log.Error($"ex={ex}");
			}
		}

		void GetTokenFromCode(string code)
		{
			Log.Warning($"getting token using code {code}");

			// construct full URL for API call
			var form = new WWWForm();
			form.AddField("client_id", twitchClientId);
			form.AddField("client_secret", twitchClientSecret);
			form.AddField("code", code);
			form.AddField("grant_type", "authorization_code");
			form.AddField("redirect_uri", twitchRedirectUrl);

			var obj = new GameObject();
			var dummy = obj.AddComponent<Dummy>();
			_ = dummy.StartCoroutine(GetRequest("https://id.twitch.tv/oauth2/token", form, request =>
			{
				if (request.isNetworkError || request.isHttpError)
				{
					Debug.Log($"request error: {request.error} / {request.downloadHandler.text}");
				}
				else
				{
					var apiResponseJson = request.downloadHandler.text;

					Log.Warning($"got {apiResponseJson} ({request.downloadHandler.data?.Length})");

					// parse the return JSON into a more usable data object
					var apiResponseData = JsonUtility.FromJson<ApiCodeTokenResponse>(apiResponseJson);

					// fetch the token from the response data
					twitchAuthToken = apiResponseData.access_token;
					tokenCallback?.Invoke(twitchAuthToken);
				}

				UnityEngine.Object.Destroy(obj);
			}));
		}

		static IEnumerator GetRequest(string url, WWWForm form, Action<UnityWebRequest> callback)
		{
			using UnityWebRequest request = UnityWebRequest.Post(url, form);
			yield return request.SendWebRequest();
			callback(request);
		}
	}
}
*/
