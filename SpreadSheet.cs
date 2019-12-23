using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx.Async;
using UnityEngine.Networking;
using System.Linq;

namespace TSKT.GoogleApis
{
    // http://7081.hatenablog.com/entry/2017/02/15/005203
    // https://developers.google.com/sheets/guides/concepts?hl=ja
    public class SpreadSheet
    {
        public string[][] Cells { get; private set; }
        public string JsonString { get; private set; }

        // apiKey : GoogleCloudConsoleで発行する文字列
        // sheetId : スプレッドシートのURLに含まれてるやつ。https://docs.google.com/spreadsheets/d/ここ/edit#gid=0
        // range : シート1とかそういうの。
        static public UniTask<SpreadSheet> LoadByApiKeyAsync(string apiKey, string sheetId, string range)
        {
            var api = "https://sheets.googleapis.com/v4/spreadsheets/" + sheetId + "/values/" + range
                + "?key=" + apiKey;
            return LoadAsync(api, null);
        }

        static public UniTask<SpreadSheet> LoadByAccessTokenAsync(string accessToken, string sheetId, string range)
        {
            var api = "https://sheets.googleapis.com/v4/spreadsheets/" + sheetId + "/values/" + range;
            return LoadAsync(api, accessToken);
        }

        static public async UniTask<SpreadSheet> LoadAsync(string api, string accessToken)
        {
            using (var request = UnityWebRequest.Get(api))
            {
                if (accessToken != null)
                {
                    request.SetRequestHeader("Authorization", "Bearer " + accessToken);
                }
                await request.SendWebRequest();
                if (request.isHttpError)
                {
                    Debug.LogWarning(request.error);
                    Debug.Log(request.downloadHandler.text);
                    return null;
                }
                if (request.isNetworkError)
                {
                    Debug.LogWarning(request.error);
                    Debug.Log(request.downloadHandler.text);
                    return null;
                }
                var json = Utf8Json.JsonSerializer.Deserialize<Dictionary<string, object>>(request.downloadHandler.text);
                var rows = (List<object>)json["values"];

                var cells = rows.Cast<List<object>>()
                    .Select(_ => _.Cast<string>().ToArray())
                    .ToArray();

                return new SpreadSheet()
                {
                    Cells = cells,
                    JsonString = request.downloadHandler.text,
                };
            }
        }
    }
}