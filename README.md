# tsB podcastReader

[tsB podcastReader](https://github.com/Chiffon-Pudding/VRC-PodcastReader/) は [m-hayabusa](https://nekomimi.studio/) 氏によって制作された [nS feedReader](https://github.com/m-hayabusa/VRC-FeedReader) のForkです。

Podcast feed を読み込みます。対応しているのは RSS2.0 の Podcast のみです。

対応する要素は [Podcast Standards Project](https://github.com/Podcast-Standards-Project/PSP-1-Podcast-RSS-Specification) を参考にいくつかピックアップしたものですが、
同名の要素が複数存在した場合は最初の1つのみが読み込まれるようになっている他、
[Podcastindex.org](https://github.com/Podcastindex-org) で提供されている [example.xml](https://github.com/Podcastindex-org/podcast-namespace/blob/main/example.xml) を読み込むと UdonVM が halt することが確認されている等、対応は完全ではありません。

Usage :

- 1. PodcastReader のプレハブを追加
  2. 追加した PodcastReader の Feed URL を設定
  3. 下のサンプルコードを参考にしてください ↓

```csharp
using UnityEngine;
using UdonSharp;
using tinysidheBower.PodcastReader;

public class example : UdonSharpBehaviour
{
    [SerializeField] private PodcastReaderCore feeds;
    private bool done = false;

    public override void Interact()
    {
        done = false;
        feeds.Load();
    }

    void Update()
    {
        if (!done && feeds != null)
        {
            if (feeds.isReady())
            {
                Debug.Log($"entries: {feeds.getTotalEntryCount()}");
                for (int i = 0; i < feeds.getFeedLength(); i++)
                {
                    if (feeds.errors()[i] != null)
                    {
                        var error = (VRC.SDK3.StringLoading.IVRCStringDownload)feeds.errors()[i];
                        Debug.Log($"{error.Url.ToString()}: {error.ErrorCode.ToString()}: {error.Error}");
                        if (error.Error.StartsWith("Not trusted url hit"))
                            Debug.Log("Check Settings -> Comfort & safety -> Safety -> Allow Untrusted URLs");
                    }
                    else
                    {
                        Debug.Log(feeds.getFeedHeaderItem(i, FeedHeader.Title));

                        for (int j = 0; j < feeds.getFeedEntryLength(i); j++)
                        {
                            Debug.Log(feeds.getFeedEntryItem(i, j, FeedEntry.Title));
                            Debug.Log(feeds.getFeedEntryItem(i, j, FeedEntry.Summary));
                            Debug.Log(feeds.getFeedEntryItem(i, j, FeedEntry.EnclosureUrl));
                        }
                    }
                }
                done = true;
            }
            else if (feeds.isLoading())
            {
                Debug.Log($"loading... {feeds.GetProgress()}");
            }
            /*
                FeedHeader and FeedEntry are written in ./Runtime/Script/PodcastReaderCore.cs (scroll to end of the file)
            */
        }
    }
}
```
