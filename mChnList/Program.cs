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
                    "https://github.com/mili-tan/maxmind-geoip/releases/latest/download/GeoLite2-Country.mmdb",
                    "GeoLite2-Country.mmdb");
            if (!File.Exists("result-10k.csv"))
                new WebClient().DownloadFile(
                    "https://raw.githubusercontent.com/NovaXNS/popularity/master/result/result-10k.csv",
                    "result-10k.csv");

            var countryReader = new DatabaseReader("GeoLite2-Country.mmdb");
            var lines = File.ReadAllLines("result-10k.csv");
            var list = new List<string>();
            var dnsList = new List<IPAddress>()
            {
                IPAddress.Parse("8.8.8.8"), IPAddress.Parse("8.8.4.4"), IPAddress.Parse("1.0.0.1"),
                IPAddress.Parse("208.67.222.222"), IPAddress.Parse("185.121.177.177"), IPAddress.Parse("9.9.9.9"),
                IPAddress.Parse("119.29.29.29"), IPAddress.Parse("223.5.5.5"), IPAddress.Parse("180.76.76.76"),
                IPAddress.Parse("185.222.222.222"), IPAddress.Parse("114.114.114.114"), IPAddress.Parse("117.50.10.10")
            };
            Parallel.ForEach(lines, item =>
            {
                try
                {
                    IPAddress ip = null;
                    var count = 0;
                    var name = DomainName.Parse(item.Split(',')[1]);
                    while (ip == null)
                    {
                        try
                        {
                            ip = (new DnsClient(dnsList[new Random(DateTime.Now.Second).Next(dnsList.Count)], 1500)
                                .Resolve(name).AnswerRecords.FirstOrDefault() as ARecord)?.Address;
                        }
                        catch (Exception)
                        {
                            try
                            {
                                Console.WriteLine($"HTTP:{count}:{item}");
                                ip = IPAddress.Parse(new WebClient()
                                    .DownloadString("http://119.29.29.29/d?dn=" + name.ToString().TrimEnd('.'))
                                    .Split(';').FirstOrDefault());
                            }
                            catch (Exception)
                            {
                                count += 1;
                                ip = null;
                                Console.WriteLine($"ERR:{count}:{item}");
                                Thread.Sleep(1500);
                                if (count >= 3) break;
                            }
                        }
                    }

                    if (ip == null) return;
                    var country = countryReader.Country(ip).Country;
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
