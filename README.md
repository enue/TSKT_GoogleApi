# TSKT_GoogleApi

　Unityから非公開設定のGoogleスプレッドシートにアクセスするためのコードです。
 
　実行するとブラウザで認証ページが表示され、認証するとアプリがトークンを取得してスプレッドシートを操作することができるようになります。

## 環境

 Unity2019.2.17、UniTask、Utf8Json

## 手順
### 1. クライアントID、クライアントシークレット発行
　GoogleCloudPlatformでクライアントIDとクライアントシークレットを作る

### 2.コード
```cs
    async void Start()
    {
        // リフレッシュトークンをまだ取得していない場合は認証してトークンを取得し、保存しておく
        var client = "クライアントId";
        var secret = "クライアントシークレット";
        var scope = "https://www.googleapis.com/auth/spreadsheets.readonly";
        var refreshToken = await GoogleApi.RequestRefreshToken(scope, client, secret);

        // リフレッシュトークンからアクセストークンを取得
        var accessToken = await GoogleApi.RequestAccessToken(refreshToken.refresh_token, client, secret);

        // スプレッドシート取得
        var sheetId = "シートId";
        var sheet = await GoogleApis.SpreadSheet.LoadByAccessTokenAsync(accessToken.access_token, sheetId, "シート1");
        Debug.Log(sheet.JsonString);
    }
```
　リフレッシュトークンを保存する際には暗号化をかけておきましょう。

　Windows環境のみ対応。スマホアプリの場合、認証処理はリダイレクト処理をURIスキーマ経由にすることでうまくいく（はず）。Macは知らない

## ひっかかりどころ
　認証時に警告が出たりします。
 
 https://www.eripyon.com/mt/2017/11/this_app_is_not_verified.html
 
 # 参考
 
 https://github.com/googlesamples/oauth-apps-for-windows/blob/master/OAuthDesktopApp/OAuthDesktopApp/MainWindow.xaml.cs
 
