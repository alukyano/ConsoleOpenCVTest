using System;
using System.Net;
using System.Collections.Specialized;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace PKK
{
    /// <summary>
    /// Proxies class
    /// </summary>
    public sealed class Proxies {
        // Variables
        private List<Dictionary<string, string>>? ProxiesList { get; set; }
        private Random prxrnd = new Random();
  
        public async Task GetProxiesAsync()
        {
            if (this.ProxiesList == null)
            {
                string pattern = @"\d*\.\d*\.\d*\.\d*\</td><td>\d*";
                string pattern2 = @"\d*\.\d*\.\d*\.\d*\</td><td>\d*\</td><td>\D*\</td><td>\D*\</td><td";
                string response =  await getRawProxies();
                ProxiesList = new List<Dictionary<string, string>> ();
                // Создаем экземпляр Regex   
                Regex rg = new Regex(pattern); 
                MatchCollection matched = rg.Matches(response); 
                for (int count = 0; count < matched.Count; count++)  
                {
                    string[] parts = matched[count].Value.Replace("</td><td>",":").Split(":");
                    
                    ProxiesList.Add(
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
                    //Console.WriteLine(matched2[count].Value);
                    Console.WriteLine(pass4);
                }
            }      
        }

        public WebProxy? getRandomProxy()
        {
            if (this.ProxiesList != null)
            { 
                var randomIndex = prxrnd.Next(ProxiesList.Count);

                var proxy = new WebProxy
                {
                    Address = new Uri($"{ProxiesList[randomIndex]["protocol"]}://{ProxiesList[randomIndex]["host"]}:{ProxiesList[randomIndex]["port"]}"),
                    BypassProxyOnLocal = false,
                };
                Console.WriteLine($"{ProxiesList[randomIndex]["protocol"]}://{ProxiesList[randomIndex]["host"]}:{ProxiesList[randomIndex]["port"]}");
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