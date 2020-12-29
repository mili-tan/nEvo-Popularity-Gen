using ARSoft.Tools.Net;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace mFamousDomain
{
    class Program
    {
        static void Main()
        {
            var tasks = new List<Task>();
            if (!Directory.Exists("result")) Directory.CreateDirectory("result");
            if (!Directory.Exists("temp")) Directory.CreateDirectory("temp");

            tasks.Add(Task.Run(() =>
            {
                try
                {
                    Console.WriteLine("umbrella...");
                    new WebClient().DownloadFile("http://s3-us-west-1.amazonaws.com/umbrella-static/top-1m.csv.zip", "umbrella.top-1m.csv.zip");
                    ZipFile.Open("umbrella.top-1m.csv.zip", ZipArchiveMode.Read).GetEntry("top-1m.csv")
                        .ExtractToFile("./result/ciscoumbrella.top-1m.csv", true);
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
                    new WebClient().DownloadFile("http://s3.amazonaws.com/alexa-static/top-1m.csv.zip", "alexa.top-1m.csv.zip");
                    ZipFile.Open("alexa.top-1m.csv.zip", ZipArchiveMode.Read).GetEntry("top-1m.csv")
                        .ExtractToFile("./result/alexa.top-1m.csv", true);
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
                    new WebClient().DownloadFile("https://tranco-list.s3.amazonaws.com/top-1m.csv.zip", "tranco.top-1m.csv.zip");
                    ZipFile.Open("tranco.top-1m.csv.zip", ZipArchiveMode.Read).GetEntry("top-1m.csv")
                        .ExtractToFile("./result/tranco.top-1m.csv", true);
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
                        "domcop.top-10m.csv.zip");
                    Console.WriteLine("domcop processing...");
                    ZipFile.Open("domcop.top-10m.csv.zip", ZipArchiveMode.Read).GetEntry("top10milliondomains.csv")
                        .ExtractToFile("./temp/domcop.top10milliondomains.csv", true);
                    var texts = new List<string>();
                    using (var stream = new StreamReader("./temp/domcop.top10milliondomains.csv"))
                        for (int i = 0; i < 1000001; i++)
                            texts.Add(stream.ReadLine());

                    var list = texts.Skip(1).Select(item => item.Replace("\"", string.Empty).Split(',').ToList())
                        .Select(i => string.Join(',', i[0], i[1])).ToList();

                    GC.Collect();
                    File.WriteAllLines("./result/domcop.top-1m.csv", list);
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

                    File.WriteAllLines("./result/majestic.top-1m.csv", list);
                    File.Delete("./temp/majestic.csv");
                    Console.WriteLine("majestic ok");
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
                    Console.WriteLine("moz...");
                    new WebClient().DownloadFile("https://moz.com/top-500/download/?table=top500Domains", "./temp/moz.csv");
                    var texts = File.ReadLines("./temp/moz.csv").ToList().Skip(1);
                    var list = texts.Select(item => item.Replace("\"", string.Empty).Split(',').ToList())
                        .Select(i => string.Join(',', i[0], i[1])).ToList();

                    File.WriteAllLines("./result/moz.top-500.csv", list);
                    File.Delete("./temp/moz.csv");
                    Console.WriteLine("moz ok");
                    GC.Collect();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }));

            Task.WaitAll(tasks.ToArray());

            try
            {
                File.Delete("umbrella.top-1m.csv.zip");
                File.Delete("alexa.top-1m.csv.zip");
                File.Delete("tranco.top-1m.csv.zip");
                File.Delete("domcop.top-10m.csv.zip");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            GC.Collect();

            var resultList = new List<string>();
            foreach (var item in Directory.GetFiles("./result"))
            {
                resultList.AddRange(File.ReadAllLines(item).ToList().Select(
                    s => s.Split(',').LastOrDefault()).ToList());
                resultList = resultList.Distinct().ToList();
            }
            File.WriteAllLines("./original.txt", resultList);
            GC.Collect();
            
            resultList = resultList.SkipLast(resultList.Count - 1000000).ToList();
            File.WriteAllLines("./original-1m.txt", resultList);
            GC.Collect();

            resultList = resultList.SkipLast(resultList.Count - 100000).ToList();
            File.WriteAllLines("./original-100k.txt", resultList);
            GC.Collect();

            var resultList2 = new List<string>();
            //Parallel.ForEach(resultList, item =>
            foreach (var item in resultList)
            {
                var domainName = DomainName.Parse(item);
                var name = DomainName.Parse(item);
                for (var i = 0; i < domainName.LabelCount - 1; i++)
                {
                    name = name.GetParentName();
                    if (!resultList.Contains(name.ToString().TrimEnd('.')))
                        resultList2.Add(domainName.ToString().TrimEnd('.'));
                    else
                        Console.WriteLine(domainName);
                }
            }

            File.WriteAllLines("./result-100k.txt", resultList2);
            File.WriteAllLines("./result-10k.txt", resultList2.SkipLast(resultList.Count - 10000).ToList());
        }
    }
}
