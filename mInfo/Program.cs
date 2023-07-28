using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using ARSoft.Tools.Net;
using ARSoft.Tools.Net.Dns;

namespace mInfo
{
    class Program
    {
        static List<IPAddress> DnsList = new()
        {
            IPAddress.Parse("114.114.114.114"), IPAddress.Parse("223.6.6.6"), IPAddress.Parse("180.76.76.76"),
            IPAddress.Parse("119.29.29.29"), IPAddress.Parse("1.2.4.8"), IPAddress.Parse("117.50.10.10"),
            IPAddress.Parse("52.80.52.52"), IPAddress.Parse("101.226.4.6"), IPAddress.Parse("218.30.118.6"),
            IPAddress.Parse("114.114.115.115"), IPAddress.Parse("223.5.5.5"), IPAddress.Parse("119.28.28.28")
        };

        static void Main(string[] args)
        {
            // if (!File.Exists("GeoLite2-Country.mmdb"))
            //     new WebClient().DownloadFile(
            //         "https://mmdb.mili.one/GeoLite2-Country.mmdb",
            //         "GeoLite2-Country.mmdb");
            // if (!File.Exists("GeoLite2-ASN.mmdb"))
            //     new WebClient().DownloadFile(
            //         "https://mmdb.mili.one/GeoLite2-ASN.mmdb",
            //         "GeoLite2-ASN.mmdb");

            if (!File.Exists("result-10k.csv"))
                new WebClient().DownloadFile(
                    "https://ghproxy.com/raw.githubusercontent.com/NovaXNS/nEvo-Popularity/master/result/result-10k.csv",
                    "result-10k.csv");

            var lines = File.ReadAllLines("result-10k.csv");
            var list = new ConcurrentBag<string>();

            //var dohList = new List<string>()
            //{
            //    "https://dns.pub/dns-query?name=",
            //    "https://doh.pub/dns-query?name=",
            //    "https://1.12.12.12/dns-query?name=",
            //    "https://120.53.53.53/dns-query?name=",
            //};

            Parallel.ForEachAsync(lines, async (item, token) =>
            {
                try
                {
                    var ns = new List<DomainName>();
                    var count = 0;
                    var name = DomainName.Parse(item.Split(',')[1]);
                    while (ns != null && !ns.Any())
                    {
                        try
                        {
                            var res = await new DnsClient(new[] { GetRandomDnsAddress(), GetRandomDnsAddress(), GetRandomDnsAddress() }, 1500)
                                .ResolveAsync(name, RecordType.Ns, token: token).ConfigureAwait(false);
                            if (res == null || !res.AnswerRecords.Any()) continue;
                            foreach (var i in res.AnswerRecords)
                            {
                                if (i is not NsRecord nsRecord) continue;
                                ns.Add(nsRecord.NameServer);
                                await Task.Run(() => Console.WriteLine(nsRecord.NameServer), token).ConfigureAwait(false);
                            }
                        }
                        catch (Exception)
                        {
                            try
                            {
                                var res = await new DnsClient(new []{ GetRandomDnsAddress() , GetRandomDnsAddress() , GetRandomDnsAddress() }, 1500)
                                    .ResolveAsync(name, RecordType.Ns, token:token).ConfigureAwait(false);
                                if (res == null || !res.AnswerRecords.Any()) continue;
                                foreach (var i in res.AnswerRecords)
                                {
                                    if (i is not NsRecord nsRecord) continue;
                                    ns.Add(nsRecord.NameServer);
                                    await Task.Run(() => Console.WriteLine(nsRecord.NameServer), token).ConfigureAwait(false);
                                }
                            }
                            catch (Exception e)
                            {
                                count += 1;
                                ns = null;
                                await Task.Run(()=> Console.WriteLine($"ERR:{count}:{item}:{e.Message}"), token).ConfigureAwait(false);
                                //await Task.Delay(500, token);
                                if (count >= 1) break;
                            }
                        }
                    }

                    if (ns.Any())
                    {
                        list.Add(ns.ToString());
                    }

                    //if (ip == null) return;
                    //var country = new DatabaseReader("GeoLite2-Country.mmdb").Country(ip).Country;
                    //var asn = new DatabaseReader("GeoLite2-ASN.mmdb").Asn(ip);
                    //Console.WriteLine(string.Join(",", item,ip.ToString(), country.IsoCode,
                    //    asn.AutonomousSystemNumber.ToString(), asn.AutonomousSystemOrganization));
                    //list.Add(string.Join(",", item, ip.ToString(), country.IsoCode,
                    //    asn.AutonomousSystemNumber.ToString(),
                    //    asn.AutonomousSystemOrganization));

                    //File.WriteAllLines("cnlist.csv", list);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }).Wait();
            File.WriteAllLines("info.csv", list);
        }

        public static IPAddress GetRandomDnsAddress()
        {
            return DnsList[new Random(DateTime.Now.Second).Next(DnsList.Count)];
        }
    }
}
