using ARSoft.Tools.Net;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
// ReSharper disable AccessToModifiedClosure

namespace mFamousDomain
{
    class Program
    {
        static void Main()
        {
            var tasks = new List<Task>();
            if (!Directory.Exists("source")) Directory.CreateDirectory("source");
            if (!Directory.Exists("result")) Directory.CreateDirectory("result");
            if (!Directory.Exists("temp")) Directory.CreateDirectory("temp");

            tasks.Add(Task.Run(() =>
            {
                try
                {
                    Console.WriteLine("umbrella...");
                    new WebClient().DownloadFile("http://s3-us-west-1.amazonaws.com/umbrella-static/top-1m.csv.zip",
                        "./temp/umbrella.top-1m.csv.zip");
                    ZipFile.Open("./temp/umbrella.top-1m.csv.zip", ZipArchiveMode.Read).GetEntry("top-1m.csv")
                        .ExtractToFile("./source/ciscoumbrella.top-1m.csv", true);
                    Console.WriteLine("umbrella ok");
                    GC.Collect();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }));

            tasks.Add(Task.Run(() =>
            {
                try
                {
                    Console.WriteLine("alexa...");
                    new WebClient().DownloadFile("http://s3.amazonaws.com/alexa-static/top-1m.csv.zip", "./temp/alexa.top-1m.csv.zip");
                    ZipFile.Open("./temp/alexa.top-1m.csv.zip", ZipArchiveMode.Read).GetEntry("top-1m.csv")
                        .ExtractToFile("./source/alexa.top-1m.csv", true);
                    Console.WriteLine("alexa ok");
                    GC.Collect();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }));

            tasks.Add(Task.Run(() =>
            {
                try
                {
                    Console.WriteLine("tranco...");
                    new WebClient().DownloadFile("https://tranco-list.s3.amazonaws.com/top-1m.csv.zip", "./temp/tranco.top-1m.csv.zip");
                    ZipFile.Open("./temp/tranco.top-1m.csv.zip", ZipArchiveMode.Read).GetEntry("top-1m.csv")
                        .ExtractToFile("./source/tranco.top-1m.csv", true);
                    Console.WriteLine("tranco ok");
                    GC.Collect();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }));

            tasks.Add(Task.Run(() =>
            {
                try
                {
                    Console.WriteLine("domcop...");
                    new WebClient().DownloadFile("https://www.domcop.com/files/top/top10milliondomains.csv.zip",
                        "./temp/domcop.top-10m.csv.zip");
                    Console.WriteLine("domcop processing...");
                    ZipFile.Open("./temp/domcop.top-10m.csv.zip", ZipArchiveMode.Read).GetEntry("top10milliondomains.csv")
                        .ExtractToFile("./temp/domcop.top10milliondomains.csv", true);
                    var texts = new List<string>();
                    using (var stream = new StreamReader("./temp/domcop.top10milliondomains.csv"))
                        for (int i = 0; i < 1000001; i++)
                            texts.Add(stream.ReadLine());

                    var list = texts.Skip(1).Select(item => item.Replace("\"", string.Empty).Split(',').ToList())
                        .Select(i => string.Join(',', i[0], i[1])).ToList();

                    GC.Collect();
                    File.WriteAllLines("./source/domcop.top-1m.csv", list);
                    File.Delete("./temp/domcop.top10milliondomains.csv");
                    Console.WriteLine("domcop ok");
                    GC.Collect();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }));

            tasks.Add(Task.Run(() =>
            {
                try
                {
                    Console.WriteLine("majestic...");
                    new WebClient().DownloadFile("https://downloads.majestic.com/majestic_million.csv", "./temp/majestic.csv");
                    var texts = File.ReadLines("./temp/majestic.csv").ToList().Skip(1);
                    var list = texts.Select(item => item.Replace("\"", string.Empty).Split(',').ToList())
                        .Select(i => string.Join(',', i[0], i[2])).ToList();

                    File.WriteAllLines("./source/majestic.top-1m.csv", list);
                    File.Delete("./temp/majestic.csv");
                    Console.WriteLine("majestic ok");
                    GC.Collect();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }));

            //tasks.Add(Task.Run(() =>
            //{
            //    try
            //    {
            //        Console.WriteLine("moz...");
            //        new WebClient().DownloadFile("https://moz.com/top-500/download/?table=top500Domains", "./temp/moz.csv");
            //        var texts = File.ReadLines("./temp/moz.csv").ToList().Skip(1);
            //        var list = texts.Select(item => item.Replace("\"", string.Empty).Split(',').ToList())
            //            .Select(i => string.Join(',', i[0], i[1])).ToList();

            //        File.WriteAllLines("./source/moz.top-500.csv", list);
            //        File.Delete("./temp/moz.csv");
            //        Console.WriteLine("moz ok");
            //        GC.Collect();
            //    }
            //    catch (Exception e)
            //    {
            //        Console.WriteLine(e);
            //    }
            //}));

            Task.WaitAll(tasks.ToArray());

            try
            {
                File.Delete("./temp/umbrella.top-1m.csv.zip");
                File.Delete("./temp/alexa.top-1m.csv.zip");
                File.Delete("./temp/tranco.top-1m.csv.zip");
                File.Delete("./temp/domcop.top-10m.csv.zip");
                Directory.Delete("./temp/", true);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            GC.Collect();
            Console.WriteLine("DL DONE!");

            var resultDict = new ConcurrentDictionary<string, int>();
            foreach (var item in Directory.GetFiles("./source"))
            {
                foreach (var iStr in File.ReadAllLines(item).ToList().Select(i => i.Split(',')))
                    if (!resultDict.ContainsKey(iStr[1])) resultDict.TryAdd(iStr[1], Convert.ToInt32(iStr[0]));
            }

            GC.Collect();
            Console.WriteLine("Read DONE!");

            var resultOrderBy = resultDict.OrderBy(o => o.Value).ToDictionary(o => o.Key, p => p.Value);
            File.WriteAllLines("./result/result-origin.csv", resultOrderBy.Select(item => $"{item.Value},{item.Key}").ToList());
            GC.Collect();
            Console.WriteLine("Select DONE!");

            var resultDict2 = new ConcurrentDictionary<string, int>();
            Parallel.ForEach(resultOrderBy, item =>
            {
                var domainName = DomainName.Parse(item.Key);
                var name = DomainName.Parse(item.Key);
                var b = true;
                for (var i = 0; i < domainName.LabelCount - 1; i++)
                {
                    name = name.GetParentName();
                    if (resultOrderBy.Keys.Contains(name.ToString().TrimEnd('.'))) b = false;
                }

                var nameStr = domainName.ToString().TrimEnd('.');
                if (b && !resultDict2.ContainsKey(nameStr)) resultDict2.TryAdd(nameStr, item.Value);
            });

            GC.Collect();
            Console.WriteLine("Result DONE!");
            var resultDict3 = resultDict2.OrderBy(o => o.Value).ToDictionary(o => o.Key, p => p.Value);
            var resultList2 = resultDict3.Select(item => $"{item.Value},{item.Key}").ToList();
            File.WriteAllLines("./result/result.csv", resultList2);
            File.WriteAllLines("./result/result-1m.csv", resultList2.SkipLast(resultList2.Count - 1000000).ToList());
            File.WriteAllLines("./result/result-100k.csv", resultList2.SkipLast(resultList2.Count - 100000).ToList());
            File.WriteAllLines("./result/result-10k.csv", resultList2.SkipLast(resultList2.Count - 10000).ToList());
            File.WriteAllLines("./result/result-10k.txt",
                resultDict2.SkipLast(resultDict2.Count - 10000).ToDictionary(o => o.Key, p => p.Value).Keys.ToList());
        }
    }
}
