# MastodonScriptBot

Mastodon の {User,Public,Hashtag}Stream を監視していい感じに Bot っぽいことをするアプリケーション．

プログラムの動作中でも動作を変えることが出来て楽しい ✌ ('ω' ✌ )三 ✌ ('ω') ✌ 三( ✌ 'ω') ✌ 

## Usage

CSharpScript にはグローバル変数として

* MSStream : Streaming (Sample では PublicStream)
* Client : MastodonClient

が渡されているので，その 2変数 に対しての操作を行う．

CSharpScript からは Subscribe した際に返る IDisposable を return すれば良い．

### example

WatchTootAll

```csharp

MSStream.OfType<Status>()
.Subscribe(x => Console.WriteLine($"{x.Account.FullUserName}: {x.Content}"))

```

AutoFavSpecificWord

```csharp

MSStream.OfType<Status>()
.Where(x => x.Content.Contains("Xamarin"))
.Subscribe(x => 
{
    System.Console.WriteLine($"{x.Account.FullUserName}: {x.Content}");
    Client.Favourite(x.Id);
})

```

もちろん Method の定義も出来て，Toot の数が数えたい！という時は

CountAllToolOfSubscribed

```csharp

static int i = 0;

IDisposable WithMethod()
{
    return MSStream.OfType<Status>()
    .Subscribe(x => System.Console.WriteLine($"!!--{i++}--!!"));
}

return WithMethod();

```

その他にも色々出来そうなので自分だけの Stream を作ってみてください．

