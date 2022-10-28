using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using ARSoft.Tools.Net;
using ARSoft.Tools.Net.Dns;
using MaxMind.GeoIP2;
using Newtonsoft.Json.Linq;

namespace mChnList
{
    class Program
    {
        static void Main(string[] args)
        {
            if (!File.Exists("GeoLite2-Country.mmdb"))
                new WebClient().DownloadFile(
                    "https://mmdb.mili.one/GeoLite2-Country.mmdb",
                    "GeoLite2-Country.mmdb");
            if (!File.Exists("GeoLite2-ASN.mmdb"))
                new WebClient().DownloadFile(
                    "https://mmdb.mili.one/GeoLite2-ASN.mmdb",
                    "GeoLite2-ASN.mmdb");
            if (!File.Exists("result-10k.csv"))
                new WebClient().DownloadFile(
                    "https://ghproxy.com/raw.githubusercontent.com/NovaXNS/popularity/master/result/result-10k.csv",
                    "result-10k.csv");

            var lines = File.ReadAllLines("result-10k.csv");
            var list = new ConcurrentBag<string>();
            var dnsList = new List<IPAddress>()
            {
                IPAddress.Parse("114.114.114.114"), IPAddress.Parse("223.6.6.6"), IPAddress.Parse("180.76.76.76"),
                IPAddress.Parse("119.29.29.29"), IPAddress.Parse("1.2.4.8"), IPAddress.Parse("117.50.10.10"),
                IPAddress.Parse("52.80.52.52"), IPAddress.Parse("101.226.4.6"), IPAddress.Parse("218.30.118.6"),
                IPAddress.Parse("114.114.115.115"), IPAddress.Parse("223.5.5.5"), IPAddress.Parse("119.28.28.28")
            };

            var dohList = new List<string>()
            {
                "https://dns.pub/dns-query?name=",
                "https://doh.pub/dns-query?name=",
                "https://1.12.12.12/dns-query?name=",
                "https://120.53.53.53/dns-query?name=",
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
                            if (new DnsClient(dnsList[new Random(DateTime.Now.Second).Next(dnsList.Count)], 1500)
                                    .Resolve(name).AnswerRecords.FirstOrDefault() is ARecord aRecord)
                            {
                                ip = aRecord.Address;
                            }
                        }
                        catch (Exception)
                        {
                            try
                            {
                                Console.WriteLine($"HTTP:{count}:{item}");
                                var jobj = JObject.Parse(new WebClient()
                                    .DownloadString(dohList[new Random(DateTime.Now.Second).Next(dohList.Count)]
                                                    + name.ToString().TrimEnd('.')));
                                if (jobj["Status"].ToString() == "0" && jobj["Answer"].Any())
                                    ip = IPAddress.Parse(jobj["Answer"].First["data"].ToString());
                            }
                            catch (Exception e)
                            {
                                count += 1;
                                ip = null;
                                Console.WriteLine($"ERR:{count}:{item}:{e.Message}");
                                Thread.Sleep(1500);
                                //if (count >= 3) break;
                            }
                        }
                    }

                    if (ip == null) return;
                    var country = new DatabaseReader("GeoLite2-Country.mmdb").Country(ip).Country;
                    var asn = new DatabaseReader("GeoLite2-ASN.mmdb").Asn(ip);
                    Console.WriteLine(string.Join(",", ip.ToString(), country.IsoCode,
                        asn.AutonomousSystemNumber.ToString(), asn.AutonomousSystemOrganization));
                    list.Add(string.Join(",", ip.ToString(), country.IsoCode, asn.AutonomousSystemNumber.ToString(),
                        asn.AutonomousSystemOrganization));
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
