using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using ARSoft.Tools.Net;
using ARSoft.Tools.Net.Dns;
using MaxMind.GeoIP2;

namespace mChnList
{
    class Program
    {
        static void Main(string[] args)
        {
            if (!File.Exists("GeoLite2-Country.mmdb"))
                new WebClient().DownloadFile(
                    "https://ghproxy.com/github.com/mili-tan/maxmind-geoip/releases/latest/download/GeoLite2-Country.mmdb",
                    "GeoLite2-Country.mmdb");
            if (!File.Exists("result-10k.csv"))
                new WebClient().DownloadFile(
                    "https://ghproxy.com/raw.githubusercontent.com/NovaXNS/popularity/master/result/result-10k.csv",
                    "result-10k.csv");

            var countryReader = new DatabaseReader("GeoLite2-Country.mmdb");
            var lines = File.ReadAllLines("result-10k.csv");
            var list = new List<string>();
            Parallel.ForEach(lines, item =>
            {
                try
                {
                    ARecord rRecord = null;
                    int count = 0;
                    while (rRecord == null)
                    {
                        try
                        {
                            rRecord = new DnsClient(IPAddress.Parse("119.29.29.29"), 1500)
                                .Resolve(DomainName.Parse(item.Split(',')[1]))
                                .AnswerRecords.FirstOrDefault() as ARecord;
                        }
                        catch (Exception)
                        {
                            count += 1;
                            Console.WriteLine($"ERR:{count}:{item}");
                            rRecord = null;
                            Thread.Sleep(1000);
                            if (count >= 6) break;
                        }
                    }

                    if (rRecord == null) return;
                    var country = countryReader.Country(rRecord.Address).Country;
                    Console.WriteLine(item + "," + country.IsoCode);
                    if (country.IsoCode == "CN") list.Add(item);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            });
            File.WriteAllLines("cnlist.csv", list);
        }
    }
}
