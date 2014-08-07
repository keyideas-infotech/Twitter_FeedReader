using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Xml;
using System.ServiceModel.Syndication;
using System.Threading;
using System.Web.Script.Serialization;
using System.Globalization;
using System.Configuration;
using System.Text.RegularExpressions;

namespace TwitterReader
{

    public class TRFeeds
    {
        public int RowNum { get; set; }
        public int Id { get; set; }
        public string URL { get; set; }
        public decimal PollTime { get; set; }
    }
    public class TRFeedItem
    {
        public string URL { get; set; }
        public string Title { get; set; }
        public string Summary { get; set; }
        public int FeedId { get; set; }
        public DateTime PublishDate { get; set; }
        public DateTime InsertTime { get; set; }
    }

    public class TRFeedList
    {
        public string Title { get; set; }
        public string Description { get; set; }        
        public bool IsPubDate { get; set; }
        public List<TRFeedItem> itemList { get; set; }
        public TRFeedList()
        {
            Title = "N/A";
            Description = "N/A";          
            IsPubDate = false;
            itemList = new List<TRFeedItem>();
        }
    }

    public class RSS
    {
        public static void GetTwitterFeedsAsList(string url, int FeedId, int FeedType)
        {
            try
            {
                DateTime dt1 = DateTime.Now;
                string screenName = string.Empty;
                string userName = string.Empty;
                string json = GetTwitterFeedAsJSon(url);
                JavaScriptSerializer serializer = new JavaScriptSerializer();
                dynamic items = serializer.Deserialize<object>(json);
                DateTime dt2 = DateTime.Now;
                TimeSpan ts = dt2 - dt1;
                decimal PollTime = Math.Round(Convert.ToDecimal(ts.TotalMilliseconds), 2);
                int Counter = 0;                
                foreach (Dictionary<string, object> feedItem in items)
                {
                    TRFeedItem row = new TRFeedItem();
                    Dictionary<string, object> user = (Dictionary<string, object>)feedItem["user"];
                    screenName = user["screen_name"].ToString();
                    userName = user["name"].ToString();
                    string statusId = feedItem["id"].ToString();
                    row.URL = Utility.TwitterLink + screenName + "/statuses/" + statusId;
                    row.Title = Convert.IsDBNull(feedItem["text"]) ? "" : (string)feedItem["text"];                    
                    row.Summary = row.Title;
                    row.FeedId = FeedId;
                    row.InsertTime = DateTime.Now;
                    row.PublishDate = DateTime.Today;
                    bool IsPubDate = false;
                    if (!Convert.IsDBNull(feedItem["created_at"]))
                    {
                        DateTime PubDate = DateTime.Today;
                        bool check = DateTime.TryParseExact(feedItem["created_at"].ToString(), Utility.TwitterDateFormate, CultureInfo.InvariantCulture, DateTimeStyles.None, out PubDate);
                        if (!check)
                            check = FeedDate.TryParseDateTime(feedItem["created_at"].ToString(), out PubDate);
                        if (check)
                        {
                            row.PublishDate = PubDate;
                            IsPubDate = true;
                        }
                    }
                    if (Counter > 0)
                        PollTime = 0;
                    if (row.PublishDate >= DateTime.Now.AddDays(-3))
                    {
                        FeedOperations.SaveUpdateMail(row, IsPubDate, PollTime, FeedType);
                        Counter++;
                    }
                }

            }
            catch (Exception ex)
            {
                FeedOperations.UpdateFeedFailure(FeedId, ex.Message);
            }
        }

        private static string GetTwitterFeedAsJSon(string twitterUrl)
        {
            string sContents = string.Empty;
            if (twitterUrl.ToLower().IndexOf("https:") > -1)
            { // URL
                System.Net.WebClient wc = new System.Net.WebClient();
                var HeaderFormat = "{0} {1}";
                string token_type = "bearer";
                string access_token = Utility.GetConfigValue("TwitterToken").ToString().Trim();
                wc.Headers.Add("Authorization", string.Format(HeaderFormat, token_type, access_token));
                byte[] response = wc.DownloadData(twitterUrl);
                sContents = System.Text.Encoding.ASCII.GetString(response);
            }
            else
            {
                // Regular Filename
                System.IO.StreamReader sr = new System.IO.StreamReader(twitterUrl);
                sContents = sr.ReadToEnd();
                sr.Close();
            }
            return sContents;
        }        

        public static void GetXMLFeedAsList(string rssUrl, int FeedId, int FeedType)
        {
            try
            {
                DateTime dt1 = DateTime.Now;
                XmlReaderSettings settings = new XmlReaderSettings();
                settings.XmlResolver = null;
                settings.DtdProcessing = DtdProcessing.Parse;
                settings.ValidationType = ValidationType.None;
                settings.ProhibitDtd = false;
                settings.IgnoreWhitespace = true;  
                XmlDocument doc = new XmlDocument();
                string strXML = "";
                using (XmlReader myReader = XmlReader.Create(rssUrl, settings))
                {
                    myReader.Read();                   
                    myReader.MoveToContent();                    
                    strXML = myReader.ReadOuterXml();                    
                }
                doc.LoadXml(strXML);
                //XmlReader reader = XmlReader.Create(rssUrl, settings);
                //doc.Load(reader);
                TRFeedList FeedList = null;
                if (FeedType == 3)
                    FeedList = ParseCraiglistDocument(doc);
                else if (FeedType == 4)
                    FeedList = ParseAtomDocument(doc);
                else
                    FeedList = ParseXMLDocument(doc);
                if (FeedList != null)
                {
                    List<TRFeedItem> itemLists = FeedList.itemList;
                    if (itemLists != null && itemLists.Count > 0)
                    {
                        DateTime dt2 = DateTime.Now;
                        TimeSpan ts = dt2 - dt1;
                        decimal PollTime = Math.Round(Convert.ToDecimal(ts.TotalMilliseconds), 2);
                        int Counter = 0;
                        foreach (TRFeedItem row in itemLists)
                        {
                            row.FeedId = FeedId;
                            row.InsertTime = DateTime.Now;
                            if (Counter > 0)
                                PollTime = 0;
                            if (row.PublishDate >= DateTime.Now.AddDays(-3))
                            {
                                FeedOperations.SaveUpdateMail(row, FeedList.IsPubDate, PollTime, FeedType);
                                Counter++;
                            }
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                if (FeedType == 5)
                    RSS.GetFeedAsList(rssUrl, FeedId, FeedType);
                else
                    FeedOperations.UpdateFeedFailure(FeedId, ex.Message);
            }


        }        

        public static TRFeedList ParseCraiglistDocument(XmlDocument doc)
        {
            var objCraiglistFeed = new TRFeedList();
            bool IsPubDate = false;
            XmlNodeList nodeLists = doc.GetElementsByTagName("rdf:RDF");
            foreach (XmlNode node in nodeLists)
            {
                foreach (XmlNode chielNode in node.ChildNodes)
                {
                    if (chielNode.Name == "channel")
                    {
                        objCraiglistFeed.Title = chielNode.InnerText;
                        foreach (XmlNode cchielNode in chielNode.ChildNodes)
                        {
                            if (cchielNode.Name == "title")
                            {
                                objCraiglistFeed.Title = cchielNode.InnerText;
                            }
                            else if (cchielNode.Name == "description")
                            {
                                objCraiglistFeed.Description = cchielNode.InnerText;
                            }                            
                        }
                    }
                    else if (chielNode.Name == "item")
                    {
                        TRFeedItem item = new TRFeedItem();
                        item.Title = string.Empty;
                        item.Summary = string.Empty;
                        item.URL = string.Empty;
                        item.InsertTime = DateTime.Now;
                        item.PublishDate = DateTime.Today;
                        foreach (XmlNode cchielNode in chielNode.ChildNodes)
                        {
                            if (cchielNode.Name == "title")
                                item.Title = cchielNode.InnerText;
                            else if (cchielNode.Name == "link")
                                item.URL = cchielNode.InnerText;
                            else if (cchielNode.Name == "description")
                                item.Summary = cchielNode.InnerText;
                            else if (cchielNode.Name == "dc:date")
                            {
                                if (!string.IsNullOrEmpty(cchielNode.InnerText))
                                {
                                    DateTime PubDate = DateTime.Today;
                                    bool check = DateTime.TryParse(cchielNode.InnerText, null, DateTimeStyles.RoundtripKind, out PubDate);
                                    if (!check)
                                        check = FeedDate.TryParseDateTime(cchielNode.InnerText, out PubDate);
                                    if (check)
                                    {
                                        item.PublishDate = PubDate;
                                        IsPubDate = true;
                                    }                              
                                }                                
                            }
                        }
                        objCraiglistFeed.itemList.Add(item);
                    }
                }
            }
            objCraiglistFeed.IsPubDate = IsPubDate;
            return objCraiglistFeed;

        }

        public static TRFeedList ParseAtomDocument(XmlDocument doc)
        {
            var objCraiglistFeed = new TRFeedList();
            bool IsPubDate = false;
            XmlNodeList nodeLists = doc.GetElementsByTagName("feed");
            foreach (XmlNode node in nodeLists)
            {
                foreach (XmlNode chielNode in node.ChildNodes)
                {
                    if (chielNode.Name == "title")
                    {
                        objCraiglistFeed.Title = chielNode.InnerText;
                    }
                    else if (chielNode.Name == "description")
                    {
                        objCraiglistFeed.Description = chielNode.InnerText;
                    }
                    else if (chielNode.Name == "entry")
                    {
                        TRFeedItem item = new TRFeedItem();
                        item.Title = string.Empty;
                        item.Summary = string.Empty;
                        item.URL = string.Empty;
                        item.InsertTime = DateTime.Now;
                        item.PublishDate = DateTime.Today;
                        foreach (XmlNode cchielNode in chielNode.ChildNodes)
                        {
                            if (cchielNode.Name == "title")
                                item.Title = cchielNode.InnerText;
                            else if (cchielNode.Name == "link")
                            {
                                if (cchielNode.InnerText == "" || cchielNode.InnerText == "N/A" || cchielNode.InnerText == "NA")
                                    item.URL = cchielNode.NextSibling.InnerText;
                                else
                                    item.URL = cchielNode.InnerText;
                            }
                            else if (cchielNode.Name == "summary")
                            {
                                if (cchielNode.InnerText == "" || cchielNode.InnerText == "N/A" || cchielNode.InnerText == "NA")
                                    item.Summary = cchielNode.InnerXml;
                                else
                                    item.Summary = cchielNode.InnerText;
                            }
                            else if (cchielNode.Name == "content" && !string.IsNullOrEmpty(cchielNode.InnerText))
                                item.Summary = cchielNode.InnerText;
                            else if (cchielNode.Name == "updated")
                            {                                
                                if (!string.IsNullOrEmpty(cchielNode.InnerText))
                                {
                                    DateTime PubDate = DateTime.Today;
                                    bool check = DateTime.TryParse(cchielNode.InnerText, null, DateTimeStyles.RoundtripKind, out PubDate);
                                    if (!check)
                                        check = FeedDate.TryParseDateTime(cchielNode.InnerText, out PubDate);
                                    if (check)
                                    {
                                        item.PublishDate = PubDate;
                                        IsPubDate = true;
                                    }    
                                }   
                            }
                        }
                        objCraiglistFeed.itemList.Add(item);
                    }
                }
            }
            objCraiglistFeed.IsPubDate = IsPubDate;
            return objCraiglistFeed;
        }

        public static TRFeedList ParseXMLDocument(XmlDocument doc)
        {
            var objCraiglistFeed = new TRFeedList();
            bool IsPubDate = false;
            XmlNodeList nodeLists = doc.GetElementsByTagName("rss");
            foreach (XmlNode node in nodeLists)
            {
                foreach (XmlNode chielNode in node.ChildNodes)
                {
                    if (chielNode.Name == "channel")
                    {
                        objCraiglistFeed.Title = chielNode.InnerText;
                        foreach (XmlNode cchieNode in chielNode.ChildNodes)
                        {
                            if (cchieNode.Name == "title")
                            {
                                objCraiglistFeed.Title = cchieNode.InnerText;
                            }
                            else if (cchieNode.Name == "description")
                            {
                                objCraiglistFeed.Description = cchieNode.InnerText;
                            }
                            else if (cchieNode.Name == "link")
                            {
                                //
                            }
                            else if (cchieNode.Name == "item")
                            {
                                TRFeedItem item = new TRFeedItem();
                                item.Title = string.Empty;
                                item.Summary = string.Empty;
                                item.URL = string.Empty;
                                item.InsertTime = DateTime.Now;
                                item.PublishDate = DateTime.Today;                                
                                foreach (XmlNode cchielNode in cchieNode.ChildNodes)
                                {
                                    if (cchielNode.Name == "title")
                                        item.Title = cchielNode.InnerText;
                                    else if (cchielNode.Name == "link")
                                        item.URL = cchielNode.InnerText;
                                    else if (cchielNode.Name == "description")
                                        item.Summary = cchielNode.InnerText;
                                    else if (cchielNode.Name == "content:encoded" && !string.IsNullOrEmpty(cchielNode.InnerText))
                                        item.Summary = cchielNode.InnerText;
                                    else if (cchielNode.Name == "pubDate" || cchielNode.Name == "dc:date")
                                    {                                        
                                        if (!string.IsNullOrEmpty(cchielNode.InnerText))
                                        {
                                            DateTime PubDate = DateTime.Now;
                                            bool check = DateTime.TryParse(cchielNode.InnerText, null, DateTimeStyles.RoundtripKind, out PubDate);
                                            if (!check)
                                                check = FeedDate.TryParseDateTime(cchielNode.InnerText, out PubDate);
                                            if (check)
                                            {
                                                item.PublishDate = PubDate;
                                                IsPubDate = true;
                                            }    
                                        }                                        
                                    }
                                }
                                objCraiglistFeed.itemList.Add(item);
                            }
                        }
                    }
                }
            }
            objCraiglistFeed.IsPubDate = IsPubDate;
            return objCraiglistFeed;
        }

        public static void GetFeedAsList(string rssUrl, int FeedId, int FeedType)
        {
            try
            {
                DateTime dt1 = DateTime.Now;
                IEnumerable<SyndicationItem> items;
                SyndicationFeed rareBookNews = SyndicationFeed.Load(XmlReader.Create(rssUrl));
                items = rareBookNews.Items;
                if (items != null && items.Count() > 0)
                {
                    DateTime dt2 = DateTime.Now;
                    TimeSpan ts = dt2 - dt1;
                    decimal PollTime = Math.Round(Convert.ToDecimal(ts.TotalMilliseconds), 2);
                    int Counter = 0;                    
                    foreach (SyndicationItem feed in items)
                    {
                        if (feed.SourceFeed == null || feed.SourceFeed.ToString() == string.Empty)
                        {
                            TRFeedItem row = new TRFeedItem();
                            row.URL = string.Empty;
                            if (feed.Links.Count > 0)
                                row.URL = feed.Links[0].Uri.ToString().Trim();
                            row.Title = string.Empty;
                            if (feed.Title != null)
                                row.Title = feed.Title.Text.Trim();
                            string feedSummary = string.Empty;
                            if (feed.Summary != null)
                                feedSummary = feed.Summary.Text;
                            if (string.IsNullOrEmpty(feedSummary) && feed.Content != null)
                            {
                                TextSyndicationContent cont = (TextSyndicationContent)feed.Content;
                                if (!string.IsNullOrEmpty(cont.Text))
                                    feedSummary = cont.Text;
                            }
                            row.Summary = feedSummary;
                            row.FeedId = FeedId;
                            row.InsertTime = DateTime.Now;
                            row.PublishDate = DateTime.Today;
                            bool IsPubDate = false;
                            if (!Convert.IsDBNull(feed.PublishDate.DateTime) && feed.PublishDate.DateTime > DateTime.MinValue)
                            {
                                row.PublishDate = feed.PublishDate.DateTime;
                                IsPubDate = true;
                            }
                            if (Counter > 0)
                                PollTime = 0;
                            if (row.PublishDate >= DateTime.Now.AddDays(-3))
                            {
                                FeedOperations.SaveUpdateMail(row, IsPubDate, PollTime, FeedType);
                                Counter++;
                            }
                        }
                    }

                }
            }
            catch (Exception ex)
            {
                FeedOperations.UpdateFeedFailure(FeedId, ex.Message);
            }
        }        

    }

    public class Utility
    {
        public const string TwitterDateFormate = "ddd MMM dd HH:mm:ss zzzz yyyy";
        public static string TwitterLink = "https://twitter.com/";
        public static string RootFolder = Utility.GetConfigValue("RootFolder");
        public static string GetConfigValue(string strKey)
        {
            string strKeyValue = string.Empty;
            try
            {
                strKeyValue = ConfigurationManager.AppSettings[strKey].ToString();
                return strKeyValue;
            }
            catch (Exception ex)
            {
                strKeyValue = ex.ToString();
            }
            return strKeyValue;
        }
        public static void LogText(string strProcess, string strMessage)
        {
            string strMess = Environment.NewLine + strProcess + " :- " + strMessage + " Time:- " + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
            System.IO.File.AppendAllText(RootFolder + "\\" + "log.txt", strMess);
        }

    }
}
