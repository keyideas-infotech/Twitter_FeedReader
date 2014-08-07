using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Mail;
using System.Data;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Threading;

namespace TwitterReader
{
    static class Program
    {        
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            AppDomain currentDomain = AppDomain.CurrentDomain;
            currentDomain.UnhandledException += new UnhandledExceptionEventHandler(MyHandler);              
            try
            {
                Utility.LogText("Scheduler Start", "");
                List<TRFeeds> finalFeeds = FeedOperations.GetValidGroupFeeds("api.twitter.com");
                int Counter = 0;
                int.TryParse(Utility.GetConfigValue("Counter"), out Counter);  
                if (finalFeeds!=null && finalFeeds.Count > 0)
                {                    
                    if (Counter > 0)
                    {
                        for (int i = 0; i < finalFeeds.Count; i = i + Counter)
                        {
                            List<TRFeeds> objFeeds = finalFeeds.FindAll(u => u.RowNum > i && u.RowNum <= i + Counter);
                            new Thread(() => StartProcess(objFeeds)).Start();
                            Thread.Sleep(5000);
                        }
                    }
                    else
                    {
                        StartProcess(finalFeeds);
                    }                    
                }                   
            }
            catch (Exception ex)
            {
                Utility.LogText("Error", ex.Message + " " + ex.StackTrace);             
            }
            Utility.LogText("Scheduler End", "");                  
        }

        private static void StartProcess(List<TRFeeds> objFeeds)
        {           
            foreach (TRFeeds objFeed in objFeeds)
            {
                if (objFeed.URL.ToLower().Contains("api.twitter.com"))
                    RSS.GetTwitterFeedsAsList(objFeed.URL, objFeed.Id, 2);
                else if (objFeed.URL.ToLower().Contains("craigslist.org"))
                    RSS.GetXMLFeedAsList(objFeed.URL, objFeed.Id, 3);
                else if (objFeed.URL.ToLower().Contains("deals.ebay.com") || objFeed.URL.ToLower().Contains("sites.google.com"))
                    RSS.GetXMLFeedAsList(objFeed.URL, objFeed.Id, 4);
                else
                    RSS.GetXMLFeedAsList(objFeed.URL, objFeed.Id, 5);              
            }            
        }

        static void MyHandler(object sender, UnhandledExceptionEventArgs args)
        {
            Exception e = (Exception)args.ExceptionObject;
            Utility.LogText("Unhandled Error", e.Message + " " + e.StackTrace);       
        }        
    }
}
