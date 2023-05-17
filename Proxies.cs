using System;
using System.Net;
using System.Collections.Specialized;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace PKK
{
    struct ProxyRecord
        {   
        public string address;
        public int port;
        public string shortCountry;
        public string longCountry;
        public string proxyType;
        public bool isGoogle;
        public bool isHttps;
    
        public ProxyRecord(string address, string port,string shortCountry,string longCountry,string proxyType,string isGoogleS, string isHttpsS)
        {
            this.address = address;
             if (Int32.TryParse(port, out int i))
                this.port = i;
            else 
                this.port = 0;
            this.shortCountry = shortCountry;
            this.longCountry = longCountry;
            this.proxyType = proxyType;
            this.isGoogle = false;
            this.isHttps = false;
            if (isGoogleS == "yes") 
                this.isGoogle = true;
            if (isHttpsS == "yes") 
                this.isHttps = true;    
        }

        public void Print()
        {
            Console.WriteLine($"{this.address}:{this.port}:{this.shortCountry} {this.isHttps}");
        }
    }
    /// <summary>
    /// Proxies class
    /// </summary>
    public sealed class Proxies {
        // Variables
        private List<ProxyRecord>? ProxiesList { get; set; }
        private List<Dictionary<string, string>>? ProxiesList2 { get; set; }
        private Random prxrnd = new Random();
  
        public async Task GetProxiesAsync(bool isLocal)
        {
            if (this.ProxiesList == null)
            {
                string pattern = @"\d*\.\d*\.\d*\.\d*\</td><td>\d*";
                string pattern2 = @"\d*\.\d*\.\d*\.\d*\</td><td>\d*\</td><td>\D*\</td><td>\D*\</td><td";
                string response =  await getRawProxies();
                ProxiesList = new List<ProxyRecord> ();
                ProxiesList2 = new List<Dictionary<string, string>> ();
                // Создаем экземпляр Regex   
                Regex rg = new Regex(pattern); 
                MatchCollection matched = rg.Matches(response); 
                for (int count = 0; count < matched.Count; count++)  
                {
                    string[] parts = matched[count].Value.Replace("</td><td>",":").Split(":");
                    
                    ProxiesList2.Add(
                        new Dictionary<string, string> {
                        {"protocol", "http"},
                        {"host", parts[0]},
                        {"port", parts[1]},
                    });
                    //Console.WriteLine("http://"+parts[0]+":"+parts[1]);
                }

                Regex rg2 = new Regex(pattern2); 
                MatchCollection matched2 = rg2.Matches(response); 
                for (int count = 0; count < matched2.Count; count++)  
                {
                    string pass1 = matched2[count].Value.Replace("</td><td>",":");
                    string pass2 = pass1.Replace("</td><td class=\"hm\">",":");
                    string pass3 = pass2.Replace("</td><td class=\"hx\">",":");
                    string pass4 = pass3.Replace("</td><td","");
                    //Console.WriteLine(pass4);
                    string[] parts = pass4.Split(":");
                    //Console.WriteLine($"{parts[0]} {parts[1]} {parts[2]} {parts[3]} {parts[4]} {parts[5]} {parts[6]}");
                    if(ProxiesList != null && parts[6] == "yes")
                    //if(ProxiesList != null)
                    {
                        var tmp = new ProxyRecord(parts[0],parts[1],parts[2],parts[3],parts[4],parts[5],parts[6]);
                        if(isLocal)
                        {
                            if(parts[2] == "RU")
                            {
                                 ProxiesList.Add(tmp);
                                 tmp.Print();
                            }
                        }
                        else
                        {
                            ProxiesList.Add(tmp);
                            tmp.Print();
                        }
                    }
                }
            }      
        }

        public WebProxy? getRandomProxy2()
        {
            if (this.ProxiesList2 != null)
            { 
                var randomIndex = prxrnd.Next(ProxiesList2.Count);

                var proxy = new WebProxy
                {
                    Address = new Uri($"{ProxiesList2[randomIndex]["protocol"]}://{ProxiesList2[randomIndex]["host"]}:{ProxiesList2[randomIndex]["port"]}"),
                    BypassProxyOnLocal = false,
                };
                Console.WriteLine($"{ProxiesList2[randomIndex]["protocol"]}://{ProxiesList2[randomIndex]["host"]}:{ProxiesList2[randomIndex]["port"]}");
                return proxy;
            } else return null;
        }

        public WebProxy? getRandomProxy()
        {
            if (this.ProxiesList != null)
            { 
                var randomIndex = prxrnd.Next(ProxiesList.Count);
                var proxy = new WebProxy(ProxiesList[randomIndex].address, ProxiesList[randomIndex].port);
                return proxy;
            } else return null;
        }
        private static async Task <string>  getRawProxies()
        {
            var httpClientHandler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            };
            httpClientHandler.ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) =>
            {
                return true;
            };
            using HttpClient client = new HttpClient(handler: httpClientHandler, disposeHandler: true);
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Add("User-Agent", PKKConstants.USER_AGENT);
            //client.DefaultRequestHeaders.Add("referer", "https://pkk.rosreestr.ru/");
        
            using Stream stream = await client.GetStreamAsync("https://free-proxy-list.net/");
            StreamReader reader = new StreamReader(stream); 
            string content = await reader.ReadToEndAsync();
            return content;
        }
    }
}