using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Threading;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using System.Net;
using System.IO;

namespace Syousetsu
{
    public class Methods
    {
        public static CancellationTokenSource AddDownloadJob(Syousetsu.Constants details, ProgressBar pb)
        {
            int max = Convert.ToInt32(pb.Maximum);

            int i = 0;
            int upTo = -1;
            if (details.Start != String.Empty && details.End == String.Empty)//determine if user don't want to start at chapter 1
            {
                i = Convert.ToInt32(details.Start);
            }
            else if (details.Start == String.Empty && details.End != String.Empty)//determine if user wants to end at a specific chapter
            {
                i = 1;
                upTo = Convert.ToInt32(details.End);
            }
            else if (details.Start != String.Empty && details.End != String.Empty)//determine if user only wants to download a specifc range
            {
                i = Convert.ToInt32(details.Start);//get start of the range
                upTo = Convert.ToInt32(details.End);//get the end of the range
            }
            else
            {
                i = 1;//if both textbox are blank assume user wants to start from the first chapter "http://*.syosetu.com/xxxxxxx/1" until the latest/last one "http://*.syosetu.com/xxxxxxx/*"
            }

            CancellationTokenSource ct = new CancellationTokenSource();
            Task.Factory.StartNew(() =>
            {
                for (int ctr = i; ctr <= max; ctr++)
                {
                    string subLink = String.Format("{0}{1}", details.Link, ctr);
                    string[] chapter = Create.GenerateContents(details, GetPage(subLink,details.SyousetsuCookie) ,ctr);
                    Create.SaveFile(details, chapter, ctr);

                    pb.Dispatcher.Invoke((Action)(() => { pb.Value = ctr; }));
                    if (upTo != -1 && ctr > upTo)//stop loop if the specifed range is reached
                    {
                        break;
                    }

                    if (ct.IsCancellationRequested)
                    {
                        // another thread decided to cancel
                        break;
                    }
                }
                pb.Dispatcher.Invoke((Action)(() => { pb.Value = max; }));
            }, ct.Token);

            return ct;
        }

        public static HtmlDocument GetTableOfContents(string link)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(link);
            request.Method = "GET";

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            var stream = response.GetResponseStream();

            HtmlDocument doc = new HtmlDocument();
            using (StreamReader reader = new StreamReader(stream))
            {
                string html = reader.ReadToEnd();
                doc.LoadHtml(html);
            }

            return doc;
        }

        public static string GetTitle(HtmlDocument doc)
        {
            HtmlNode titleNode = doc.DocumentNode.SelectSingleNode("//p[@class='novel_title']");
            return (titleNode == null) ? "title" : titleNode.InnerText.TrimStart().TrimEnd();
        }

        public static string GetChapterTitle(HtmlDocument doc)
        {
            HtmlNode titleNode = doc.DocumentNode.SelectSingleNode("//p[@class='novel_subtitle']");
            return (titleNode == null) ? "title" : titleNode.InnerText.TrimStart().TrimEnd();
        }

        public static string GetNovelBody(HtmlDocument doc, Constants.FileType fileType)
        {
            HtmlNode novelNode = doc.DocumentNode.SelectSingleNode("//div[@id='novel_honbun']");
            if (fileType == Constants.FileType.Text)
            {
                return novelNode.InnerText;
            }
            else if (fileType == Constants.FileType.HTML)
            {
                string[] s = novelNode.InnerText.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None);

                StringBuilder sb = new StringBuilder();
                foreach (String str in s)
                {
                    string temp = (str != "") ? ("<p>" + str + "</p>\n") : ("<p><br/></p>\n");
                    sb.Append(temp);
                }
                return sb.ToString();
            }
            return String.Empty;
        }

        public static int GetTotalChapters(HtmlDocument doc)
        {
            string pattern = "(href=\"/)(?<series>.+)/(?<num>.+)/\">(?<title>.+)(?=</a>)";
            Regex r = new Regex(pattern);

            HtmlNodeCollection chapterNode = doc.DocumentNode.SelectNodes("//div[@class='index_box']/dl/dd[@class='subtitle']");
            Match m = r.Match(chapterNode.Last().OuterHtml);

            return Convert.ToInt32(m.Groups["num"].Value);
        }

        private static HtmlDocument GetPage(string link, CookieContainer cookies)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(link);
            request.Method = "GET";
            request.CookieContainer = cookies;

            try
            {
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                var stream = response.GetResponseStream();

                //When you get the response from the website, the cookies will be stored
                //automatically in "_cookies".

                using (StreamReader reader = new StreamReader(stream))
                {
                    string html = reader.ReadToEnd();
                    HtmlDocument doc = new HtmlDocument();
                    doc.LoadHtml(html);
                    return doc;
                }
            }
            catch (WebException e)
            {
                StringBuilder html = new StringBuilder();
                html.Append("<html>");
                html.Append("<head>");
                html.Append("<title>エラー</title>");
                html.Append("</head>");
                html.Append("<body>");
                html.Append("</body>");
                html.Append("</html>");

                var doc = new HtmlAgilityPack.HtmlDocument();
                doc.LoadHtml(html.ToString());
                return doc;
            }
        }

        public static bool IsValid(HtmlDocument doc)
        {
            return (GetTitle(doc) != "エラー") ? true : false;
        }

        public static bool IsValidLink(string link)
        {
            string pattern = @"[^.]+[\.|]*syosetu.com/[\s\S]";
            Regex r = new Regex(pattern);
            Match m = r.Match(link);

            return m.Success;
        }

        public static string GetSeriesCode(string link)
        {
            string pattern = @"[^.]+[\.|]*syosetu.com/(?<seriesCode>.+)(?=/)";
            Regex r = new Regex(pattern);
            Match m = r.Match(link);

            return m.Groups["seriesCode"].Value;
        }
    }

    public class Create
    {
        public static string[] GenerateContents(Syousetsu.Constants details, HtmlDocument doc, int current)
        {
            string[] chapter = new string[2];
            if (details.CurrentFileType == Constants.FileType.Text)
            {
                chapter[0] = Methods.GetChapterTitle(doc).TrimStart().TrimEnd();
                chapter[1] = Methods.GetNovelBody(doc, details.CurrentFileType);

                if (doc.DocumentNode.SelectSingleNode("//div[@id='novel_honbun']").InnerHtml.Contains("<img"))
                {
                    string subLink = String.Format("{0}{1}", details.Link, current);
                    chapter[1] += String.Format("\n\n===\n\nContains image(s): {0}\n\n===", subLink);
                }
            }
            else if (details.CurrentFileType == Constants.FileType.HTML)
            {
                chapter[0] = Methods.GetChapterTitle(doc).TrimStart().TrimEnd();
                chapter[1] = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n";
                chapter[1] += "<!DOCTYPE html PUBLIC \"-//W3C//DTD XHTML 1.1//EN\"\n";
                chapter[1] += "\"http://www.w3.org/TR/xhtml11/DTD/xhtml11.dtd\">\n\n";
                chapter[1] += "<html xmlns=\"http://www.w3.org/1999/xhtml\">\n";
                chapter[1] += "<head>\n";
                chapter[1] += "\t<title></title>\n";
                chapter[1] += "\t<link href=\"../Styles/Style0001.css\" rel=\"stylesheet\" type=\"text/css\" />\n";
                chapter[1] += "</head>\n";
                chapter[1] += "<body>\n";
                chapter[1] += "\n<h2>" + chapter[0] + "</h2>\n\n";
                chapter[1] += Methods.GetNovelBody(doc, details.CurrentFileType);

                if (doc.DocumentNode.SelectSingleNode("//div[@id='novel_honbun']").InnerHtml.Contains("<img"))
                {
                    string subLink = String.Format("{0}{1}", details.Link, current);
                    chapter[1] += String.Format("\n\n===\n\nContains image(s): {0}\n\n===", subLink);
                }
            }
            return chapter;
        }

        public static void SaveFile(Syousetsu.Constants details, string[] chapter, int current)
        {
            //create novel folder if it doesn't exist
            string path = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), details.Title);
            bool folderExists = Directory.Exists(path);
            if (!folderExists) { Directory.CreateDirectory(path); }

            //replace illigal character(s) with "□"
            string regexSearch = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
            Regex r = new Regex(string.Format("[{0}]", System.Text.RegularExpressions.Regex.Escape(regexSearch)));
            chapter[0] = r.Replace(chapter[0], "□");

            //save the chapter
            if (details.CurrentFileType == Constants.FileType.Text)
            {
                string fileName = String.Format("Chapter {0} - {1}.txt", current, chapter[0]);
                fileName = Path.Combine(path, fileName); //String.Format(@"{0}{3}Chapter {1} - {2}.txt", path, current, chapter[0], System.IO.Path.DirectorySeparatorChar);
                File.WriteAllLines(fileName, chapter, Encoding.Unicode);
            }
            else if (details.CurrentFileType == Constants.FileType.HTML)
            {
                string fileName = String.Format("Chapter {0} - {1}.htm", current, chapter[0]);
                fileName = Path.Combine(path, fileName);//String.Format(@"{0}{3}Chapter {1} - {2}.htm", path, current, chapter[0], System.IO.Path.DirectorySeparatorChar);
                File.WriteAllText(fileName, chapter[1], Encoding.Unicode);
            }
        }
    }
}
