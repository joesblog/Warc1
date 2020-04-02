using MediaToolkit;
using MediaToolkit.Model;
using MediaToolkit.Options;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace JS1
{
    class Program
    {
        //   static string root = @"C:\_temp\JustinTV\";
        static string root = "";
        static void Main(string[] args)
        {



            //part1();

            //    part_2a();
            // part_3a_addDates();
            part_3a_withdates();
            //part_3a();
            //part2();
            //  part3();

            //  roughDump_1();
           // warc1();
            Console.ReadLine();
        }

        private static void warc1(string arc = "justintv_20140608112343.megawarc.warc.gz")
        {
            Regex rgxId = new Regex(@"justintv_(\d+).*");

            string id = rgxId.Match(arc).Groups[1].Value;
            string vids = $@"{root}{id}\vids\";
            string thumbs = $@"{root}{id}\thumbs\";


            if (!Directory.Exists(vids)) Directory.CreateDirectory(vids);
            if (!Directory.Exists(thumbs)) Directory.CreateDirectory(thumbs);

            Warc.WarcFile wf = new Warc.WarcFile($@"{root}justintv_{id}.megawarc.warc.gz");

            List<Warc.WarcFilesystemEntry> entries = wf.FilesystemEntries.ToList();


            ParameterizedThreadStart pts = new ParameterizedThreadStart(thdThumbnail_warc1);
            Thread t = new Thread(pts);
            t.Start(id);
            var x = entries.Where(o => o.Filename.Contains(".flv") && !o.Filename.Contains("highlight"));
            foreach (var i in x)
            {
                var a1 = i.Filename;

                string realname = i.Filename;
                while (realname.IndexOf("/") > -1)
                {
                    realname = realname.Substring(realname.IndexOf("/") + 1);
                }

                if (!File.Exists(vids + realname))
                    File.WriteAllBytes(vids + realname, i.ExtractResponse());
             
            }
        }

        public static void thdThumbnail_warc1(object id)
        {
            string thumbs = $@"{root}{id}\thumbs\";
            string vids = $@"{root}{id}\vids\";




            FileSystemWatcher fsw = new FileSystemWatcher(vids);
            fsw.EnableRaisingEvents = true;
            fsw.IncludeSubdirectories = true;

            fsw.Created += (s, e) =>
            {
                string t1 = Path.GetFileName(e.FullPath);
                t1 = Path.GetFileNameWithoutExtension(t1);
                Console.WriteLine("FileFound" + t1);

                t1 = t1 + ".jpg";


                var input = new MediaFile(e.FullPath);
                var output = new MediaFile(thumbs + t1);
                using (var engine = new Engine())
                {
                    var opts = new ConversionOptions { Seek = TimeSpan.FromSeconds(15) };
                    engine.GetThumbnail(input, output, opts);
                }

            };

        }



        private static void part_3a_addDates(string miffix = "warccdx")
        {
            string pt = $"{root}{miffix}.json";
            string jsonsrc = File.ReadAllText(pt);

            List<flvStore> store = Newtonsoft.Json.JsonConvert.DeserializeObject<List<flvStore>>(jsonsrc);

            List<flvStorewithDate> store2 = new List<flvStorewithDate>();

            int c = 0;
            foreach (var i in store)
            {
                var nd = new flvStorewithDate(i);
                store2.Add(nd);
                Console.WriteLine($"Done {c++}/{store.Count}");

            }


            File.WriteAllText($"{root}{miffix}_dates.json", Newtonsoft.Json.JsonConvert.SerializeObject(store2));


        }

        private static void roughDump_1(string miffix = "warccdx")
        {

            Regex pat1 = new Regex(@".*?\)(.*)");

            string pt = $@"{root}{miffix}\f\";
            DirectoryInfo dinf = new DirectoryInfo(pt);
            FileInfo[] finfs = new DirectoryInfo(pt).GetFiles();

            foreach (var i in finfs.Take(1))
            {
                string[] a = File.ReadAllLines(i.FullName);

                foreach (string k in a)
                {

                    string[] b = k.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (b[0] == "CDX") continue;

                    string fname = b[0];
                    if (pat1.IsMatch(fname))
                    {
                        fname = pat1.Match(fname).Groups[1].Value;

                        //remove forward slashes
                        if (fname.Contains("/"))
                            fname = fname.Substring(fname.LastIndexOf("/") + 1);

                        //remove query string
                        if (fname.Contains("?"))
                        {
                            int qs = fname.IndexOf("?");
                            fname = fname.Substring(0, qs);
                        }




                    }
                    else
                    {
                        Console.WriteLine("No Match");
                    }
                }


            }
        }

        private static void part_3a(string miffix = "warccdx")
        {
            string pt = $"{root}{miffix}.json";
            string jsonsrc = File.ReadAllText(pt);

            List<flvStore> store = Newtonsoft.Json.JsonConvert.DeserializeObject<List<flvStore>>(jsonsrc);

            string cn = "";
            Console.WriteLine("Awaiting search term, type q to quit, type showfile to display which archive the flv is in. rgx will turn regex on ");
            bool showFile = false;
            bool regexOn = true;
            Console.WriteLine("regex is set to " + regexOn.ToString());
            Console.WriteLine("Showfile is set to " + showFile.ToString());

            while ((cn = Console.ReadLine()) != "q")
            {
                if (cn == "showfile")
                {
                    showFile = !showFile;
                    Console.WriteLine("Showfile is set to " + showFile.ToString());
                    continue;
                }

                if (cn == "rgx")
                {

                    Console.WriteLine("regex is set to " + regexOn.ToString());
                    continue;
                }
                List<flvStore> fil = new List<flvStore>();


                if (!regexOn)
                {
                    fil = store.Where(o => o.flvName.ToLower().Contains(cn.ToLower())).OrderBy(o => o.flvName).ToList();
                }
                else
                {
                    Regex rgx = new Regex(cn);


                    fil = store.Where(o => rgx.IsMatch(o.flvName)).OrderBy(o => o.flvName).ToList();
                }

                int fCount = 0;
                if (!showFile)
                {
                    foreach (var i in fil)
                    {
                        Console.WriteLine(i.flvName);
                        fCount++;
                    }
                }
                else
                {
                    var f1 = (from i in fil
                              group i by i.filename into g
                              select g).ToDictionary(o => o.Key, o => o.ToArray());

                    foreach (var i in f1)
                    {
                        Console.WriteLine($"----Files In: {i.Key}----");
                        foreach (var k in i.Value)
                        {
                            Console.WriteLine($"\t\t----{k.flvName}");
                            fCount++;
                        }
                    }

                }
                Console.WriteLine($"{fCount} files found using the term: '{cn}'");



            }



        }

        private static List<flvStorewithDate> part_3a_withdates(string miffix = "warccdx")
        {
            string pt = $"{root}{miffix}_dates.json";
            string jsonsrc = File.ReadAllText(pt);

            List<flvStorewithDate> store = Newtonsoft.Json.JsonConvert.DeserializeObject<List<flvStorewithDate>>(jsonsrc);
            List<flvStorewithDate> fil = new List<flvStorewithDate>();

            string cn = "";
            Console.WriteLine("Awaiting search term, type q to quit, type showfile to display which archive the flv is in. rgx will turn regex on ");
            bool showFile = false;
            bool regexOn = true;
            int? onlyYear = null;
            int? onlyMonth = null;
            string cdx = "";
            Console.WriteLine("regex is set to " + regexOn.ToString());
            Console.WriteLine("Showfile is set to " + showFile.ToString());
            Console.WriteLine("cdx is set to " + cdx.ToString());

            while ((cn = Console.ReadLine()) != "q")
            {
                if (cn == "showfile")
                {
                    showFile = !showFile;
                    Console.WriteLine("Showfile is set to " + showFile.ToString());
                    continue;
                }
                if (cn.Contains("year:"))
                {
                    string[] syr = cn.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);

                    if (syr.Length == 1) onlyYear = null;
                    else
                    {
                        onlyYear = int.Parse(syr[1]);
                    }
                    Console.WriteLine($"Set year to{onlyYear.Value}");
                    continue;
                }

                if (cn == "cls")
                {
                    Console.Clear();
                    Console.WriteLine("Awaiting search term, type q to quit, type showfile to display which archive the flv is in. rgx will turn regex on ");

                    Console.WriteLine("regex is set to " + regexOn.ToString());
                    Console.WriteLine("Showfile is set to " + showFile.ToString());
                    continue;
                }
                if (cn == "rgx")
                {

                    Console.WriteLine("regex is set to " + regexOn.ToString());
                    continue;
                }
                if (cn.StartsWith("cdx"))
                {
                    string[] scdx = cn.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);

                    if (scdx.Length == 1) cdx = "";
                    else
                    {
                        cdx = scdx[1];
                    }
                    Console.WriteLine($"cdx fitler set to {cdx}");
                    continue;
                }


                if (!regexOn)
                {
                    fil = store.Where(o => o.flvName.ToLower().Contains(cn.ToLower())).OrderBy(o => o.flvName).ToList();
                }
                else
                {
                    Regex rgx = new Regex(cn);


                    fil = store.Where(o => rgx.IsMatch(o.flvName)).OrderBy(o => o.flvName).ToList();
                }
                if (onlyYear.HasValue)
                {
                    fil = fil.Where(o => o.urlDate.Year == onlyYear.Value).ToList();
                }


                if (string.IsNullOrEmpty(cdx))
                {
                    fil = fil.Where(o => o.filename.StartsWith(cdx)).ToList();
                }
                int fCount = 0;
                if (!showFile)
                {
                    Console.WriteLine("Name\tDate\tCompressedSize");

                    foreach (var k in fil)
                    {
                        Console.WriteLine($"{k.flvName}\t{k.urlDate}\t{k.compressedRecordSize}");

                        //Console.WriteLine($"{i.flvName} - {i.urlDate}");
                        //Console.WriteLine(i.linsrc);
                        fCount++;
                    }
                }
                else
                {
                    var f1 = (from i in fil
                              group i by i.filename into g
                              select g).ToDictionary(o => o.Key, o => o.ToArray());

                    foreach (var i in f1)
                    {
                        Console.WriteLine($"----Files In: {i.Key}----\t\t");

                        flvStorewithDate[] klot = i.Value;

                        if (onlyYear.HasValue)
                            klot = klot.OrderBy(o => o.urlDate.Year).ToArray();

                        Console.WriteLine("Name\tDate\tCompressedSize");
                        foreach (var k in i.Value)
                        {
                            Console.WriteLine($"{k.flvName}\t{k.urlDate}\t{k.compressedRecordSize}");
                            fCount++;
                        }
                    }
                    /*string op = Newtonsoft.Json.JsonConvert.SerializeObject(f1);
                    Console.WriteLine(op);
                    */

                    //TextWriter tw = new StreamWriter($@"{root}\{new Random().Next(1, 1000)}.json");
                    //CsvHelper.CsvSerializer s = new CsvHelper.CsvSerializer(tw, new CsvHelper.Configuration.CsvConfiguration( System.gl) { })
                    //CsvHelper.CsvSerializer s = new CsvHelper.CsvSerializer()



                    foreach (var i in fil)
                        i.linsrc = "";
                    using (var writer = new StreamWriter($@"{root}{new Random().Next(1, 1000)}.csv"))
                    {
                        using (var csv = new CsvHelper.CsvWriter(writer, CultureInfo.InvariantCulture))
                        {
                            csv.WriteRecords(fil);
                        }

                    }
                }
                Console.WriteLine($"{fCount} files found using the term: '{cn}'\t\t");

            }


            return fil;
        }


        /// <summary>
        /// lets grab all the flv's we can find and store them in a directory
        /// </summary>
        /// <param name="miffix"></param>
        private static void part_2a(string miffix = "warccdx")
        {
            string pt = $@"{root}{miffix}\f\";
            DirectoryInfo dinf = new DirectoryInfo(pt);
            List<flvStore> store = new List<flvStore>();
            string pat0 = @".*?\/archives\/.*?\/(.*?\.flv).*";
            Regex rgx0 = new Regex(pat0);
            FileInfo[] finfs = new DirectoryInfo(pt).GetFiles();
            int cfile = 0;
            foreach (FileInfo f in finfs)
            {
                Console.WriteLine($"P2A In {cfile++} / {finfs.Length - 1}");
                string[] a = File.ReadAllLines(f.FullName);
                int ln = 0;

                foreach (string x in a)
                {
                    if (x.Contains(".flv"))
                    {
                        flvStore fl = new flvStore()
                        {
                            filename = f.Name,
                            line = ln,
                            linsrc = x
                        };

                        if (rgx0.IsMatch(x))
                        {
                            fl.flvName = rgx0.Match(x).Groups[1].Value;
                        }

                        store.Add(fl);
                    }

                    ln++;
                }
            }

            //dump to json.

            string jsonsrc = Newtonsoft.Json.JsonConvert.SerializeObject(store);

            File.WriteAllText($"{root}{miffix}.json", jsonsrc);

        }

        private static void part3()
        {
            string s1 = File.ReadAllText(@"C:\_temp\JustinTv\cdx.json");
            List<fin> fs = Newtonsoft.Json.JsonConvert.DeserializeObject<List<fin>>(s1);

            var fs2 = fs.Where(o => o.str.Contains("flv")).ToList();


            var fs3 = (from i in fs2
                       group i by i.filename into g
                       select new { fn = g.Key, str = g.ToArray() }
                       ).ToList();

            string pat0 = @".*?\/archives\/.*?\/(.*?\.flv).*";
            Regex rgx0 = new Regex(pat0);

            foreach (var i in fs3)
            {
                Console.WriteLine("--------------");
                Console.WriteLine($"{i.fn}");

                foreach (var k in i.str)
                {

                    if (rgx0.IsMatch(k.str))
                    {
                        //Console.WriteLine("MATCH");
                        string m0 = rgx0.Match(k.str).Groups[1].Value;
                        Console.WriteLine(m0);
                    }
                    else
                    {
                        Console.WriteLine("NO MATCH");
                    }
                    ///Console.WriteLine($"{k.str}");
                }
                //  Console.WriteLine("--------------");

            }
        }

        static void part2()
        {

            string pt = @"C:\_temp\JustinTV\cdx\f\";

            DirectoryInfo dinf = new DirectoryInfo(pt);
            List<fin> fs = new List<fin>();
            int c = 0;
            foreach (FileInfo f in dinf.GetFiles())
            {
                string[] a = File.ReadAllLines(f.FullName);
                int ln = 0;
                foreach (string x in a)
                {
                    if (x.ToLower().Contains("jesus"))
                    {
                        fin y = new fin()
                        {
                            filename = f.Name,
                            line = ln,
                            str = x
                        };
                        //Console.WriteLine(x);
                        fs.Add(y);
                    }
                    ln++;
                }

                Console.WriteLine($"On {++c}");
            }

            string serfin = Newtonsoft.Json.JsonConvert.SerializeObject(fs);
            File.WriteAllText(@"C:\_temp\JustinTv\cdx.json", serfin);
            Console.WriteLine("done");

        }

        static void part1()
        {
            string src1 = File.ReadAllText("res.html");
            HtmlAgilityPack.HtmlDocument dc = new HtmlAgilityPack.HtmlDocument();
            dc.LoadHtml(src1);

            var a1 = dc.DocumentNode.Descendants().Where(o => o.Name == "a").Where(o => o.Attributes["href"] != null && o.Attributes["href"].Value.Contains("details") && o.InnerText.Contains("Archive Team Justin.tv")).ToList();

            List<string> s = new List<string>();
            List<string> sd = new List<string>();
            List<string> idno = new List<string>();
            //https://archive.org/download/archiveteam_justintv_20140605222747/archiveteam_justintv_20140605222747.cdx.gz

            //https://archive.org/download/archiveteam_justintv_20140608112256/justintv_20140608112256.megawarc.warc.os.cdx.gz
            string pat1 = @"\/details\/archiveteam_justintv_([0-9]+)";
            foreach (var i in a1)
            {
                string hrf = i.Attributes["href"].Value;
                s.Add(i.Attributes["href"].Value);


                if (Regex.IsMatch(hrf, pat1))
                {

                    string g1 = Regex.Match(hrf, pat1).Groups[1].Value;
                    idno.Add(g1);
                    //string toDown = $@"https://archive.org/download/archiveteam_justintv_{g1}/archiveteam_justintv_{g1}.cdx.gz";
                    string toDown = $@"https://archive.org/download/archiveteam_justintv_{g1}/justintv_{g1}.megawarc.warc.os.cdx.gz";
                    sd.Add(toDown);
                }




                Console.WriteLine(i.InnerText.Trim());

            }


            for (int i = 0; i < sd.Count; i++)
            {

                Console.WriteLine($"Downloading {i}/{sd.Count}");
                WebRequest wr = WebRequest.Create(sd[i]);
                WebResponse resp = wr.GetResponse();
                using (Stream op = File.OpenWrite($@"C:\_temp\JustinTV\warccdx\{idno[i]}.cdx.gz"))
                using (Stream inp = resp.GetResponseStream())
                {
                    inp.CopyTo(op);
                }


            }
        }
    }

    public class fin
    {
        public string filename { get; set; }
        public int line { get; set; }
        public string str { get; set; }
    }

    public class flvStore
    {
        public string filename { get; set; }
        public int line { get; set; }
        public string linsrc { get; set; }
        public string flvName { get; set; }
    }

    public class flvStorewithDate : flvStore
    {
        static Regex rgdate = new Regex(@"\/archives\/((?<year>[0-9]{4})\-(?<month>[0-9]{1,2})-(?<day>[0-9]{1,2}))");
        public flvStorewithDate() { }
        public flvStorewithDate(flvStore i)
        {
            this.filename = i.filename;
            this.line = i.line;
            this.linsrc = i.linsrc;
            this.flvName = i.flvName;

            if (rgdate.IsMatch(this.linsrc))
            {
                Match m = rgdate.Match(this.linsrc);

                string syr = m.Groups["year"].Value;
                string smt = m.Groups["month"].Value;
                string sdy = m.Groups["day"].Value;

                DateTime d = new DateTime(int.Parse(syr), int.Parse(smt), int.Parse(sdy));

                this.urlDate = d;



            }
            else
            {
                Console.WriteLine("Error");
            }

            string[] spl = this.linsrc.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (spl.Length == 11)
            {
                this.compressedRecordSize = long.Parse(spl[9]);
            }
            else
            {
                throw new Exception("spl != 11");
            }

        }
        public DateTime urlDate { get; set; }
        public long compressedRecordSize { get; set; }
    }
}
