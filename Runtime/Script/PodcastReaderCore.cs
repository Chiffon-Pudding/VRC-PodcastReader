using UnityEngine;
using nekomimiStudio.parser.xml;
using VRC.SDKBase;
using VRC.SDK3.StringLoading;
using VRC.SDK3.Data;

namespace nekomimiStudio.PodcastReader
{
    public class PodcastReaderCore : XMLParser_Callback
    {
        private XMLParser parser;
        [SerializeField] bool loadOnStart = false;
        public VRCUrl[] FeedURL;

        private object[] errorlog;

        private string[][][][] res;
        /*
            Result res = Feed[];
            Feed = {
                Header,
                Entry[]
            }

            Header = {
                Title[1],
                SubTitle[1],
                Summary[1],
                Id[1],
                Link[1],
                Updated[1],
                Rights[1],
                AuthorName[1],
                AuthorUri[1]
            }

            Entry = {
                Title,
                SubTitle,
                Summary,
                Id,
                Link,
                Updated,
            }
        */

        private string[] str;
        private bool strDone = true;
        private int strLoadIttr = 0;
        private int parseIttr = 0;

        private bool active = false;

        private float parseProgress = 0;

        public void Start()
        {
            parser = this.GetComponentInChildren<XMLParser>();
            if (loadOnStart) Load();
        }

        public void Load()
        {
            res = new string[FeedURL.Length][][][];
            str = new string[FeedURL.Length];
            errorlog = new object[FeedURL.Length];

            strDone = true;
            strLoadIttr = 0;
            parseIttr = 0;

            parseProgress = 0;

            active = true;
        }

        public void Update()
        {
            if (active)
            {
                if (strDone && strLoadIttr < FeedURL.Length)
                {
                    strDone = false;
                    VRCStringDownloader.LoadUrl(FeedURL[strLoadIttr], (VRC.Udon.Common.Interfaces.IUdonEventReceiver)this);
                }
                if (parseIttr < FeedURL.Length && str[parseIttr] != null && str[parseIttr] != "")
                {
                    parser.ParseAsync(str[parseIttr], this, this.GetInstanceID() + "_" + parseIttr);
                    str[parseIttr] = "";
                }
                if (isReady()) active = false;
            }
        }

        public override void OnStringLoadSuccess(IVRCStringDownload result)
        {
            str[strLoadIttr] = result.Result;
            strLoadIttr++;
            strDone = true;
        }

        public override void OnStringLoadError(IVRCStringDownload result)
        {
            errorlog[strLoadIttr] = result;
            strLoadIttr++;
            strDone = true;
            parseProgress = 0;
            parseIttr++;
            Debug.Log("FeedReader: ERR: " + strLoadIttr);
        }

        public override void OnXMLParseEnd(DataDictionary result, string callbackId)
        {
            parseProgress = 0;
            parseIttr++;
            var id = callbackId.Split('_');
            var ittr = int.Parse(id[id.Length - 1]);

            var root = XMLParser.GetChildNodes(result);

            bool found = false;
            for (int i = 0; i < root.Count && !found; i++)
            {
                var elem = (DataDictionary)root[i];
                switch (XMLParser.GetNodeName(elem))
                {
                    case "rdf:RDF":
                        parseRSS1(ittr, elem);
                        found = true;
                        break;
                    case "rss":
                        parseRSS2(ittr, elem);
                        found = true;
                        break;
                    case "feed":
                        parseAtom(ittr, elem);
                        found = true;
                        break;
                }
            }
            if (!found) Debug.LogWarning("RSS / Atom start tag not found");
        }

        public override void OnXMLParseIteration(int processing, int total, string callbackId)
        {
            parseProgress = processing / (float)total;
        }

        private string GetNodeValueByName(DataDictionary data, string nodeName)
        {
            return XMLParser.GetText(XMLParser.GetNodeByPath(data, "/" + nodeName));
        }

        private string GetAttributeValueByName(DataDictionary data, string nodeName, string attribute)
        {
            return  XMLParser.GetAttributes(XMLParser.GetNodeByPath( data, "/" + nodeName ))[attribute].ToString();
        }

        private void parseRSS1(int feedNum, DataDictionary contentRoot)
        {
            var headerRoot = XMLParser.GetNodeByPath(contentRoot, "/channel");

            res[feedNum] = new string[2][][];
            res[feedNum][0] = new string[10][];

            res[feedNum][0][(int)FeedHeader.Title] = new string[] { GetNodeValueByName(headerRoot, "title") };
            res[feedNum][0][(int)FeedHeader.SubTitle] = new string[] { GetNodeValueByName(headerRoot, "subtitle") };
            res[feedNum][0][(int)FeedHeader.Summary] = new string[] { GetNodeValueByName(headerRoot, "description") };
            res[feedNum][0][(int)FeedHeader.Id] = new string[] { "" };
            res[feedNum][0][(int)FeedHeader.Updated] = new string[] { GetNodeValueByName(headerRoot, "dc:date") };
            res[feedNum][0][(int)FeedHeader.Rights] = new string[] { GetNodeValueByName(headerRoot, "dc:rights") };
            res[feedNum][0][(int)FeedHeader.Link] = new string[] { GetNodeValueByName(headerRoot, "link") };
            res[feedNum][0][(int)FeedHeader.AuthorName] = new string[] { "" };
            res[feedNum][0][(int)FeedHeader.AuthorUri] = new string[] { "" };

            var items = XMLParser.GetChildNodes(contentRoot);
            string[][] entries = new string[items.Count][];

            int cnt = 0;
            for (int i = 0; i < items.Count; i++)
            {
                var entry = (DataDictionary)items[i];
                if (XMLParser.GetNodeName(entry) == "item")
                {
                    entries[cnt] = new string[6];
                    entries[cnt][(int)FeedEntry.Title] = GetNodeValueByName(entry, "title");
                    entries[cnt][(int)FeedEntry.SubTitle] = GetNodeValueByName(entry, "subtitle");
                    entries[cnt][(int)FeedEntry.Link] = GetNodeValueByName(entry, "link");
                    entries[cnt][(int)FeedEntry.Summary] = GetNodeValueByName(entry, "description");
                    entries[cnt][(int)FeedEntry.Id] = "";
                    entries[cnt][(int)FeedEntry.Updated] = GetNodeValueByName(entry, "dc:date");
                    cnt++;
                }
            }
            res[feedNum][1] = new string[cnt][];

            System.Array.Copy(entries, res[feedNum][1], cnt);
        }

        private void parseRSS2(int feedNum, DataDictionary contentRoot)
        {
            contentRoot = XMLParser.GetNodeByPath(contentRoot, "/channel");

            res[feedNum] = new string[2][][];
            res[feedNum][0] = new string[18][];

            res[feedNum][0][(int)FeedHeader.Title] = new string[] { GetNodeValueByName(contentRoot, "title") };
            res[feedNum][0][(int)FeedHeader.SubTitle] = new string[] { GetNodeValueByName(contentRoot, "subtitle") };
            res[feedNum][0][(int)FeedHeader.Summary] = new string[] { GetNodeValueByName(contentRoot, "description") };
            res[feedNum][0][(int)FeedHeader.Id] = new string[] { "" };
            res[feedNum][0][(int)FeedHeader.Updated] = new string[] { GetNodeValueByName(contentRoot, "lastBuildDate") };
            res[feedNum][0][(int)FeedHeader.Rights] = new string[] { GetNodeValueByName(contentRoot, "copyright") };
            res[feedNum][0][(int)FeedHeader.Link] = new string[] { GetNodeValueByName(contentRoot, "link") };

            res[feedNum][0][(int)FeedHeader.AuthorName] = new string[] { "" };
            res[feedNum][0][(int)FeedHeader.AuthorUri] = new string[] { "" };

            res[feedNum][0][(int)FeedHeader.Language] = new string[] { GetNodeValueByName(contentRoot, "language") };
            res[feedNum][0][(int)FeedHeader.Category_itunes] = new string[] { GetAttributeValueByName(contentRoot, "itunes:category", "text") };  // first element only.
            res[feedNum][0][(int)FeedHeader.SubCategory_itunes] = new string[] { GetAttributeValueByName( XMLParser.GetNodeByPath(contentRoot, "itunes:category"), "itunes:category", "text") };  // first element only.
            res[feedNum][0][(int)FeedHeader.Explicit_itunes] = new string[] { GetNodeValueByName(contentRoot, "itunes:explicit") };
            res[feedNum][0][(int)FeedHeader.Image_itunes] = new string[] { GetAttributeValueByName(contentRoot, "itunes:image", "href") };
            res[feedNum][0][(int)FeedHeader.Locked_podcast] = new string[] { GetNodeValueByName(contentRoot, "podcast:locked") };
            res[feedNum][0][(int)FeedHeader.Author_itunes] = new string[] { GetNodeValueByName(contentRoot, "itunes:author") };
            res[feedNum][0][(int)FeedHeader.Copyright] = new string[] { GetNodeValueByName(contentRoot, "copyright") };

            var items = XMLParser.GetChildNodes(contentRoot);
            string[][] entries = new string[items.Count][];

            int cnt = 0;
            for (int i = 0; i < items.Count; i++)
            {
                var entry = (DataDictionary)items[i];
                if (XMLParser.GetNodeName(entry) == "item")
                {
                    entries[cnt] = new string[13];
                    entries[cnt][(int)FeedEntry.Title] = GetNodeValueByName(entry, "title");
                    entries[cnt][(int)FeedEntry.SubTitle] = GetNodeValueByName(entry, "subtitle");
                    entries[cnt][(int)FeedEntry.Link] = GetNodeValueByName(entry, "link");
                    entries[cnt][(int)FeedEntry.Summary] = GetNodeValueByName(entry, "description");
                    entries[cnt][(int)FeedEntry.Id] = GetNodeValueByName(entry, "guid");
                    entries[cnt][(int)FeedEntry.Updated] = GetNodeValueByName(entry, "pubDate");

                    entries[cnt][(int)FeedEntry.EnclosureLength] = GetAttributeValueByName(entry, "enclosure", "length");
                    entries[cnt][(int)FeedEntry.EnclosureType] = GetAttributeValueByName(entry, "enclosure", "type");
                    entries[cnt][(int)FeedEntry.EnclosureUrl] = GetAttributeValueByName(entry, "enclosure", "url");
                    entries[cnt][(int)FeedEntry.Duration_itunes] = GetNodeValueByName(entry, "itunes:duration");
                    entries[cnt][(int)FeedEntry.Image_itunes] = GetAttributeValueByName(entry, "itunes:image", "href");
                    entries[cnt][(int)FeedEntry.Explicit_itunes] = GetNodeValueByName(entry, "itunes:explicit");
                    entries[cnt][(int)FeedEntry.Block_itunes] = GetNodeValueByName(entry, "itunes:block");

                    cnt++;
                }
            }
            res[feedNum][1] = new string[cnt][];

            System.Array.Copy(entries, res[feedNum][1], cnt);
        }

        private void parseAtom(int feedNum, DataDictionary contentRoot)
        {
            res[feedNum] = new string[2][][];
            res[feedNum][0] = new string[10][];

            res[feedNum][0][(int)FeedHeader.Title] = new string[] { GetNodeValueByName(contentRoot, "title") };
            res[feedNum][0][(int)FeedHeader.SubTitle] = new string[] { GetNodeValueByName(contentRoot, "subtitle") };
            res[feedNum][0][(int)FeedHeader.Summary] = new string[] { GetNodeValueByName(contentRoot, "summary") };
            res[feedNum][0][(int)FeedHeader.Id] = new string[] { GetNodeValueByName(contentRoot, "id") };
            res[feedNum][0][(int)FeedHeader.Updated] = new string[] { GetNodeValueByName(contentRoot, "updated") };
            res[feedNum][0][(int)FeedHeader.Rights] = new string[] { GetNodeValueByName(contentRoot, "rights") };
            res[feedNum][0][(int)FeedHeader.Link] = new string[] { GetNodeValueByName(contentRoot, "link") };

            var author = XMLParser.GetNodeByPath(contentRoot, "/author");
            res[feedNum][0][(int)FeedHeader.AuthorName] = new string[] { GetNodeValueByName(author, "name") };
            res[feedNum][0][(int)FeedHeader.AuthorUri] = new string[] { GetNodeValueByName(author, "uri") };

            var items = XMLParser.GetChildNodes(contentRoot);
            string[][] entries = new string[items.Count][];

            int cnt = 0;
            for (int i = 0; i < items.Count; i++)
            {
                var entry = (DataDictionary)items[i];
                if (XMLParser.GetNodeName(entry) == "entry")
                {
                    entries[cnt] = new string[6];
                    entries[cnt][(int)FeedEntry.Title] = GetNodeValueByName(entry, "title");
                    entries[cnt][(int)FeedEntry.SubTitle] = GetNodeValueByName(entry, "subtitle");
                    entries[cnt][(int)FeedEntry.Link] = GetNodeValueByName(entry, "link");
                    entries[cnt][(int)FeedEntry.Summary] = GetNodeValueByName(entry, "summary");
                    entries[cnt][(int)FeedEntry.Id] = GetNodeValueByName(entry, "id");
                    entries[cnt][(int)FeedEntry.Updated] = GetNodeValueByName(entry, "updated");
                    cnt++;
                }
            }
            res[feedNum][1] = new string[cnt][];

            System.Array.Copy(entries, res[feedNum][1], cnt);
        }

        public bool isLoading()
        {
            return active;
        }

        public float GetProgress()
        {
            return (parseProgress + parseIttr) / FeedURL.Length;
        }

        public bool isReady()
        {
            return parseIttr >= FeedURL.Length;
        }

        // IVRCStringDownload[] -> TypeResolverException: Type referenced by 'VRCSDK3StringLoadingIVRCStringDownloadArray' could not be resolved. 
        public object[] errors()
        {
            return errorlog;
        }
        public IVRCStringDownload error(int feedNum = -1)
        {
            if (feedNum == -1)
            {
                for (int i = errorlog.Length - 1; i >= 0; i--)
                {
                    if (errorlog[i] != null)
                        return (IVRCStringDownload)errorlog[i];
                }
                return null;
            }

            if (feedNum < errorlog.Length && errorlog[feedNum] != null)
                return (IVRCStringDownload)errorlog[feedNum];
            return null;
        }
        public int getFeedLength()
        {
            if (res == null) return 0;
            return res.Length;
        }

        public int getTotalEntryCount()
        {
            int length = 0;
            for (int i = 0; i < getFeedLength(); i++)
            {
                length += getFeedEntryLength(i);
            }
            return length;
        }

        public int getFeedEntryLength(int feedNum)
        {
            if (res == null || res.Length < feedNum || res[feedNum] == null) return 0;
            return res[feedNum][1].Length;
        }
        public string getFeedEntryItem(int feedNum, int item, FeedEntry entry)
        {
            if (res.Length < feedNum || res[feedNum] == null || res[feedNum][1] == null || res[feedNum][1].Length < item || res[feedNum][1].Length < item) return null;
            return res[feedNum][1][item][(int)entry];
        }

        public string getFeedHeaderItem(int feedNum, FeedHeader entry)
        {
            if (res.Length < feedNum || res[feedNum] == null || res[feedNum][0] == null) return null;
            return res[feedNum][0][(int)entry][0];
        }
    }

    public enum FeedHeader
    {
        Title, SubTitle, Summary, Id, Link, Updated, Rights, Entries, AuthorName, AuthorUri
        , Language, Category_itunes, SubCategory_itunes, Explicit_itunes, Image_itunes, Locked_podcast, Author_itunes, Copyright,
    }
    public enum FeedEntry
    {
        Title, SubTitle, Summary, Id, Link, Updated
        , EnclosureLength, EnclosureType, EnclosureUrl, Duration_itunes, Image_itunes, Explicit_itunes,  Block_itunes
    }
}
