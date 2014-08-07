using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TwitterReader
{
    public class FeedDate
    {
        public static DateTime ParseDateTime(string dateTime)
        {
            try
            {
                return DateTime.Parse(dateTime,
                System.Globalization.DateTimeFormatInfo.InvariantInfo,
                System.Globalization.DateTimeStyles.AllowWhiteSpaces);
            }
            catch (FormatException fex)
            {
                string loc = fex.Message.Substring(fex.Message.LastIndexOf(" "));
                loc = loc.Substring(0, loc.LastIndexOf("."));
                int iLoc = int.Parse(loc);
                string tz = dateTime.Substring(iLoc);
                tz = TimeZoneToOffset(tz);
                dateTime = dateTime.Substring(0, iLoc);
                DateTime ret = DateTime.Parse(dateTime,
                System.Globalization.DateTimeFormatInfo.InvariantInfo,
                System.Globalization.DateTimeStyles.AllowWhiteSpaces);

                // offset for time zone
                if (tz.Length > 0)
                {
                    try
                    {
                        if (tz.Length == 4 && tz.Substring(0, 1) != "-")
                        {
                            try
                            {
                                int.Parse(tz.Substring(0, 1));
                                tz = "+" + tz;
                            }
                            catch
                            {
                            }
                        }
                        if (tz.Length == 5 && tz.Substring(0, 1) == "-" ||
                        tz.Length == 5 && tz.Substring(0, 1) == "+")
                        {
                            try
                            {
                                int h = int.Parse(tz.Substring(1, 2));
                                int m = int.Parse(tz.Substring(3, 2));
                                if (tz.Substring(0, 1) == "-")
                                {
                                    ret = ret.AddHours((h + SysDTOffset.Hours) * -1);
                                    ret = ret.AddMinutes((m + SysDTOffset.Minutes) * -1);
                                }
                                else
                                {
                                    ret = ret.AddHours(h + SysDTOffset.Hours);
                                    ret = ret.AddMinutes(m + SysDTOffset.Minutes);
                                }
                            }
                            catch
                            {
                            }
                        }
                    }
                    catch { }
                }
                return ret;
            }
        }

        public static bool TryParseDateTime(string dateTime, out DateTime PubDate)
        {
            PubDate = DateTime.Today;
            try
            {
                PubDate = DateTime.Parse(dateTime, System.Globalization.DateTimeFormatInfo.InvariantInfo, System.Globalization.DateTimeStyles.AllowWhiteSpaces);
                return true;
            }
            catch (FormatException fex)
            {
                string loc = fex.Message.Substring(fex.Message.LastIndexOf(" "));
                loc = loc.Substring(0, loc.LastIndexOf("."));
                int iLoc = int.Parse(loc);
                string tz = dateTime.Substring(iLoc);
                tz = TimeZoneToOffset(tz);
                dateTime = dateTime.Substring(0, iLoc);
                if (!string.IsNullOrEmpty(dateTime))
                {
                    try
                    {
                        DateTime ret = DateTime.Parse(dateTime, System.Globalization.DateTimeFormatInfo.InvariantInfo, System.Globalization.DateTimeStyles.AllowWhiteSpaces);
                        // offset for time zone
                        if (tz.Length > 0)
                        {
                            try
                            {
                                if (tz.Length == 4 && tz.Substring(0, 1) != "-")
                                {
                                    try
                                    {
                                        int.Parse(tz.Substring(0, 1));
                                        tz = "+" + tz;
                                    }
                                    catch
                                    {
                                    }
                                }
                                if (tz.Length == 5 && tz.Substring(0, 1) == "-" ||
                                tz.Length == 5 && tz.Substring(0, 1) == "+")
                                {
                                    try
                                    {
                                        int h = int.Parse(tz.Substring(1, 2));
                                        int m = int.Parse(tz.Substring(3, 2));
                                        if (tz.Substring(0, 1) == "-")
                                        {
                                            ret = ret.AddHours((h + SysDTOffset.Hours) * -1);
                                            ret = ret.AddMinutes((m + SysDTOffset.Minutes) * -1);
                                        }
                                        else
                                        {
                                            ret = ret.AddHours(h + SysDTOffset.Hours);
                                            ret = ret.AddMinutes(m + SysDTOffset.Minutes);
                                        }
                                    }
                                    catch
                                    {
                                    }
                                }
                            }
                            catch { }
                        }
                        PubDate = ret;
                        return true;
                    }
                    catch
                    {
                        return false;
                    }
                }
                return false;
            }
            catch
            {
                return false;
            }
        }
        
        private static TimeSpan SysDTOffset
        {
            get
            {
                return System.TimeZone.CurrentTimeZone.GetUtcOffset(DateTime.Now);
            }
        }

        public static string TimeZoneToOffset(string tz)
        {
            tz = tz.ToUpper().Trim();
            //tz = "blaaaah";
            for (int i = 0; i < TimeZones.Length; i++)
            {
                if (((string)((string[])TimeZones.GetValue(i)).GetValue(0)) == tz)
                {
                    return ((string)((string[])TimeZones.GetValue(i)).GetValue(1));
                }
            }
            return System.TimeZone.CurrentTimeZone.GetUtcOffset(DateTime.Now).ToString()
            .Replace(":", "").Substring(0, 5);
        }

        public static string[][] TimeZones = new string[][] 
        {
            new string[] {"ACDT", "-1030", "Australian Central Daylight"},
            new string[] {"ACST", "-0930", "Australian Central Standard"},
            new string[] {"ADT", "+0300", "(US) Atlantic Daylight"},
            new string[] {"AEDT", "-1100", "Australian East Daylight"},
            new string[] {"AEST", "-1000", "Australian East Standard"},
            new string[] {"AHDT", "+0900", ""},
            new string[] {"AHST", "+1000", ""},
            new string[] {"AST", "+0400", "(US) Atlantic Standard"},
            new string[] {"AT", "+0200", "Azores"},
            new string[] {"AWDT", "-0900", "Australian West Daylight"},
            new string[] {"AWST", "-0800", "Australian West Standard"},
            new string[] {"BAT", "-0300", "Bhagdad"},
            new string[] {"BDST", "-0200", "British Double Summer"},
            new string[] {"BET", "+1100", "Bering Standard"},
            new string[] {"BST", "+0300", "Brazil Standard"},
            new string[] {"BT", "-0300", "Baghdad"},
            new string[] {"BZT2", "+0300", "Brazil Zone 2"},
            new string[] {"CADT", "-1030", "Central Australian Daylight"},
            new string[] {"CAST", "-0930", "Central Australian Standard"},
            new string[] {"CAT", "+1000", "Central Alaska"},
            new string[] {"CCT", "-0800", "China Coast"},
            new string[] {"CDT", "+0500", "(US) Central Daylight"},
            new string[] {"CED", "-0200", "Central European Daylight"},
            new string[] {"CET", "-0100", "Central European"},
            new string[] {"CST", "+0600", "(US) Central Standard"},
            new string[] {"EAST", "-1000", "Eastern Australian Standard"},
            new string[] {"EDT", "+0400", "(US) Eastern Daylight"},
            new string[] {"EED", "-0300", "Eastern European Daylight"},
            new string[] {"EET", "-0200", "Eastern Europe"},
            new string[] {"EEST", "-0300", "Eastern Europe Summer"},
            new string[] {"EST", "+0500", "(US) Eastern Standard"},
            new string[] {"FST", "-0200", "French Summer"},
            new string[] {"FWT", "-0100", "French Winter"},
            new string[] {"GMT", "+0000", "Greenwich Mean"},
            new string[] {"GST", "-1000", "Guam Standard"},
            new string[] {"HDT", "+0900", "Hawaii Daylight"},
            new string[] {"HST", "+1000", "Hawaii Standard"},
            new string[] {"IDLE", "-1200", "Internation Date Line East"},
            new string[] {"IDLW", "+1200", "Internation Date Line West"},
            new string[] {"IST", "-0530", "Indian Standard"},
            new string[] {"IT", "-0330", "Iran"},
            new string[] {"JST", "-0900", "Japan Standard"},
            new string[] {"JT", "-0700", "Java"},
            new string[] {"MDT", "+0600", "(US) Mountain Daylight"},
            new string[] {"MED", "-0200", "Middle European Daylight"},
            new string[] {"MET", "-0100", "Middle European"},
            new string[] {"MEST", "-0200", "Middle European Summer"},
            new string[] {"MEWT", "-0100", "Middle European Winter"},
            new string[] {"MST", "+0700", "(US) Mountain Standard"},
            new string[] {"MT", "-0800", "Moluccas"},
            new string[] {"NDT", "+0230", "Newfoundland Daylight"},
            new string[] {"NFT", "+0330", "Newfoundland"},
            new string[] {"NT", "+1100", "Nome"},
            new string[] {"NST", "-0630", "North Sumatra"},
            new string[] {"NZ", "-1100", "New Zealand "},
            new string[] {"NZST", "-1200", "New Zealand Standard"},
            new string[] {"NZDT", "-1300", "New Zealand Daylight"},
            new string[] {"NZT", "-1200", "New Zealand "},
            new string[] {"PDT", "+0700", "(US) Pacific Daylight"},
            new string[] {"PST", "+0800", "(US) Pacific Standard"},
            new string[] {"ROK", "-0900", "Republic of Korea"},
            new string[] {"SAD", "-1000", "South Australia Daylight"},
            new string[] {"SAST", "-0900", "South Australia Standard"},
            new string[] {"SAT", "-0900", "South Australia Standard"},
            new string[] {"SDT", "-1000", "South Australia Daylight"},
            new string[] {"SST", "-0200", "Swedish Summer"},
            new string[] {"SWT", "-0100", "Swedish Winter"},
            new string[] {"USZ3", "-0400", "USSR Zone 3"},
            new string[] {"USZ4", "-0500", "USSR Zone 4"},
            new string[] {"USZ5", "-0600", "USSR Zone 5"},
            new string[] {"USZ6", "-0700", "USSR Zone 6"},
            new string[] {"UT", "+0000", "Universal Coordinated"},
            new string[] {"UTC", "+0000", "Universal Coordinated"},
            new string[] {"UZ10", "-1100", "USSR Zone 10"},
            new string[] {"WAT", "+0100", "West Africa"},
            new string[] {"WET", "+0000", "West European"},
            new string[] {"WST", "-0800", "West Australian Standard"},
            new string[] {"YDT", "+0800", "Yukon Daylight"},
            new string[] {"YST", "+0900", "Yukon Standard"},
            new string[] {"ZP4", "-0400", "USSR Zone 3"},
            new string[] {"ZP5", "-0500", "USSR Zone 4"},
            new string[] {"ZP6", "-0600", "USSR Zone 5"}
         };
    }
}
