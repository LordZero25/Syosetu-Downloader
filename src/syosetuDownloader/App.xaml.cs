using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;
using CommandLine;
using HtmlAgilityPack;

namespace syosetuDownloader
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        class Options
        {
            [Option('l', "link", HelpText = "URL to be processed")]
            public string Link { get; set; }

            [Option('s', "start", HelpText = "Stating chapter", DefaultValue = "1")]
            public string Start { get; set; }

            [Option('e', "end", HelpText = "Ending chapter")]
            public string End { get; set; }

            [Option('f', "format", HelpText = "File format (HTML | TXT)", DefaultValue = "TXT")]
            public string Format { get; set; }

            [Option('t', "toc", HelpText = "Generates Table of contents", DefaultValue = false)]
            public bool ToC { get; set; }

            [Option('h', "help")]
            public bool Help { get; set; }
        }

        string GetFilenameFormat()
        {
            using (System.IO.StreamReader sr = new System.IO.StreamReader("format.ini"))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    if (!line.StartsWith(";"))
                    {
                        return line;
                    }
                }
            }

            return "";
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            Options o = new Options();
            if (e.Args.Any())
            {
                /* do stuff without a GUI */
                Parser.Default.ParseArguments(e.Args, o);

                if (o.Help == true)
                {
                    AttachConsole(-1);
                    Console.WriteLine(CommandLine.Text.HelpText.AutoBuild(o));
                    this.Shutdown();
                    return;
                }

                Syousetsu.Constants sc = new Syousetsu.Constants();
                HtmlDocument toc = Syousetsu.Methods.GetTableOfContents(o.Link, sc.SyousetsuCookie);

                sc.ChapterTitle.Add("");
                sc.SeriesTitle = Syousetsu.Methods.GetTitle(toc);
                sc.Link = o.Link;
                sc.Start = o.Start;
                sc.End = String.IsNullOrWhiteSpace(o.End) ? Syousetsu.Methods.GetTotalChapters(toc).ToString() : o.End;
                sc.SeriesCode = Syousetsu.Methods.GetSeriesCode(o.Link);
                sc.FilenameFormat = GetFilenameFormat();
                Syousetsu.Methods.GetAllChapterTitles(sc, toc);
                Syousetsu.Constants.FileType fileType = Syousetsu.Constants.FileType.Text;
                switch (o.Format.ToUpper())
                {
                    case "HTML": { fileType = Syousetsu.Constants.FileType.HTML; break; }
                    case "TXT": { fileType = Syousetsu.Constants.FileType.Text; break; }
                    default: { fileType = Syousetsu.Constants.FileType.Text; break; }
                }
                sc.CurrentFileType = fileType;
                Syousetsu.Methods.GetAllChapterTitles(sc, toc);

                if (o.ToC == true)
                {
                    Syousetsu.Create.GenerateTableOfContents(sc, toc);
                }

                System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
                sw.Start();
                Syousetsu.Methods.AddDownloadJob(sc);
                sw.Stop();

                AttachConsole(-1);
                Console.WriteLine(sw.Elapsed);

                this.Shutdown();
            }
        }

        [System.Runtime.InteropServices.DllImport("Kernel32.dll")]
        public static extern bool AttachConsole(int processId);
    }
}
