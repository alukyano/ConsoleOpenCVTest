// C# 8
// Edge detection by Canny algorithm
using OpenCvSharp;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Text.Json;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PKK;
class Program 
{
    static Proxies proxyService = new Proxies();
    static async Task Main(string[] Args) 
    {
        string response;
        int parcelType;
        string? parcelValue;

        
        await proxyService.GetProxiesAsync(true);
        // Command line parsing
        Arguments CommandLine=new Arguments(Args);

        response = await MakeProxiedRequest("https://www.sberbank.ru");
        Console.WriteLine(response);

        response = await MakeProxiedRequest("https://pkk.rosreestr.ru");
        Console.WriteLine(response);
        // check for type parameter
        if(CommandLine["type"] != null) 
        {
            Console.WriteLine("parcel type value: " + 
                CommandLine["type"]);
            if (Int32.TryParse(CommandLine["type"], out int i))
            parcelType = i;
        } else {
            Console.WriteLine("type not defined/recognized ! Default type set to 1.");
            parcelType = 1;
        }

        if(CommandLine["parcel"] != null) 
        {
            Console.WriteLine("parcel value: " + 
                CommandLine["parcel"]);
            parcelValue = CommandLine["parcel"];
            response = await MakeProxiedParcelRequest(parcelValue);
            Console.WriteLine(response);
        }
        else
        {
            Console.WriteLine("parcel not defined !");
            Environment.Exit(-1);
        }

        //response = await MakeDirectRequest("50:11:0050116:42");
        
        //response = await MakeProxiedRequest("https://www.sberbank.ru");
        //response = await MakeDirectRequest("https://www.sberbank.ru");
        //response = await MakeDirectRequest("https://pkk.rosreestr.ru");
        //response = await MakeDirectParcelRequest("50:11:0050116:42");
        
    }

    static void testOpenCV()
    {
        using var src = new Mat("test.jpg", ImreadModes.Grayscale);
        using var dst = new Mat();
        
        Console.WriteLine("Hello, World!");
        Cv2.Canny(src, dst, 50, 200);
        using (new Window("src image", src)) 
        using (new Window("dst image", dst)) 
        {
            Cv2.WaitKey();
        }
    }

    static async Task <string> MakeDirectParcelRequest(string parcelCode)
    {
        X509Certificate2 certificate1 = new X509Certificate2("russian_trusted_root_ca_pem.crt");
        X509Certificate2 certificate2 = new X509Certificate2("russian_trusted_sub_ca_pem.crt");
        var httpClientHandler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };
        httpClientHandler.ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) =>
        {
            Console.WriteLine(message);
            //Console.WriteLine(cert);
            //Console.WriteLine(chain);
            //Console.WriteLine(sslPolicyErrors);
            return true;
        };
        using HttpClient client = new HttpClient(handler: httpClientHandler, disposeHandler: true);
        client.DefaultRequestHeaders.Accept.Clear();
        client.Timeout = TimeSpan.FromSeconds(10);
        client.DefaultRequestHeaders.Add("pragma", "no-cache");
        client.DefaultRequestHeaders.Add("referer", "https://pkk.rosreestr.ru/");
        client.DefaultRequestHeaders.Add("user-agent", PKKConstants.USER_AGENT);
        client.DefaultRequestHeaders.Add("x-requested-with", "XMLHttpRequest");
        //client.DefaultRequestHeaders.Add("X-ARR-ClientCert", certificate2.GetRawCertDataString());
        string url = PKKConstants.SEARCH_URL + "1/" + parcelCode;
        Console.WriteLine(url);
      
        HttpResponseMessage response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
        HttpContent content = response.Content;

        // ... Check Status Code                                
        Console.WriteLine("Response StatusCode: " + (int)response.StatusCode);

        // ... Read the string.
        string result = await content.ReadAsStringAsync();
        return result;
    }

    static async Task <string> MakeDirectRequest(string url)
    {
        X509Certificate2 certificate1 = new X509Certificate2("russian_trusted_root_ca_pem.crt");
        X509Certificate2 certificate2 = new X509Certificate2("russian_trusted_sub_ca_pem.crt");
        var httpClientHandler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };
        httpClientHandler.ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) =>
        {
            //Console.WriteLine(message);
            //Console.WriteLine(cert);
            //Console.WriteLine(chain);
            //Console.WriteLine(sslPolicyErrors);
            return true;
        };
        //httpClientHandler.ClientCertificateOptions = ClientCertificateOption.Automatic;
        //httpClientHandler.ClientCertificateOptions = ClientCertificateOption.Manual;
        //httpClientHandler.SslProtocols = SslProtocols.Tls12;
        //httpClientHandler.ClientCertificates.Add(certificate1);
        //httpClientHandler.ClientCertificates.Add(new X509Certificate2("russian_trusted_sub_ca_pem.crt"));

        using HttpClient client = new HttpClient(handler: httpClientHandler, disposeHandler: true);
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Add("User-Agent", PKKConstants.USER_AGENT);
        //client.DefaultRequestHeaders.Add("X-ARR-ClientCert", certificate1.GetRawCertDataString());
        client.DefaultRequestHeaders.Add("X-ARR-ClientCert", certificate2.GetRawCertDataString());
        client.DefaultRequestHeaders.Add("referer", "https://pkk.rosreestr.ru/");
        Console.WriteLine(url);
      
        using Stream stream = await client.GetStreamAsync(url);
        StreamReader reader = new StreamReader(stream); 
        string content = await reader.ReadToEndAsync();
        return content;
    }

    static async Task <string> MakeProxiedRequest(string? url)
    {
        ArgumentNullException.ThrowIfNull(url);

        HttpResponseMessage response;
        HttpContent content;
        HttpClientHandler httpClientHandler;

        X509Certificate2 rootCertificate = new X509Certificate2(@"russian_trusted_root_ca_pem.crt");
        X509Certificate2 intermediateCertificate = new X509Certificate2(@"russian_trusted_sub_ca_pem.crt");
        httpClientHandler = new HttpClientHandler();

        System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
        httpClientHandler.ServerCertificateCustomValidationCallback = (message, serverCert, chain, sslPolicyErrors) =>
        {
            if ((sslPolicyErrors & ~SslPolicyErrors.RemoteCertificateChainErrors) != 0) return false;
            if(chain is object)
            {
                chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
                chain.ChainPolicy.CustomTrustStore.Add(rootCertificate);
                chain.ChainPolicy.ExtraStore.Add(intermediateCertificate);
                if(serverCert is object)
                {
                    Console.WriteLine(chain.Build(serverCert));
                    return chain.Build(serverCert);
                } else return false;
            } else return false;
        };

        //httpClientHandler.Proxy = new WebProxy("95.79.53.19", 8080);
        //httpClientHandler.Proxy = new WebProxy("158.160.56.149", 8080);
        //httpClientHandler.Proxy = new WebProxy("185.15.172.212", 3128);
        //httpClientHandler.Proxy = new WebProxy("103.157.117.227", 8080);
        httpClientHandler.Proxy = proxyService.getRandomProxy();
        string? proxystr = httpClientHandler.Proxy.GetProxy(new Uri(url)).ToString();
        Console.WriteLine("Using proxy "+ proxystr);
        httpClientHandler!.UseProxy = true;
        using HttpClient client = new HttpClient(handler: httpClientHandler, disposeHandler: true);
        client.Timeout = TimeSpan.FromSeconds(10);
        client.DefaultRequestHeaders.Clear();
        client.DefaultRequestHeaders.Add("X-requested-with", "XMLHttpRequest");
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/plain"));
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));
        client.DefaultRequestHeaders.AcceptLanguage.Clear();
        client.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue("ru"));
        client.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue("ru-RU", 0.9));
        client.DefaultRequestHeaders.AcceptEncoding.Clear();
        client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
        client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("deflate"));
        client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("br"));
        client.DefaultRequestHeaders.Pragma.Clear();
        client.DefaultRequestHeaders.Pragma.Add(new NameValueHeaderValue("no-cache"));
        client.DefaultRequestHeaders.Referrer = new Uri(url);
        client.DefaultRequestHeaders.UserAgent.Clear();
        client.DefaultRequestHeaders.UserAgent.ParseAdd(PKKConstants.USER_AGENT);
        Console.WriteLine(client.DefaultRequestHeaders.ToString());

        try
        {
            response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode(); 
            content = response.Content;
            // ... Check Status Code                                
            Console.WriteLine("Response StatusCode: " + (int)response.StatusCode);
            // ... Read the string.
            string result = await content.ReadAsStringAsync();
            //dispose resources
            httpClientHandler.Dispose();
            client.Dispose();

            return result;
        }
        catch (HttpRequestException exception)
        {
            LogInfo(exception.Message);
            httpClientHandler.Dispose();
            client.Dispose();
            return "";
            // Handle exception.
        }
        catch (TimeoutException exception)
        {
            LogInfo(exception.Message);
            httpClientHandler.Dispose();
            client.Dispose();
            return "";
        }
        catch (OperationCanceledException exception)
        {
            LogInfo(exception.Message);
            httpClientHandler.Dispose();
            client.Dispose();
            return "";
        }
        catch (Exception exception)
        {
            LogInfo(exception.Message);
            httpClientHandler.Dispose();
            client.Dispose();
            return "";
        }
    }
    static async Task<string> MakeProxiedParcelRequest(string? parcelCode)
    {
        X509Certificate2 rootCertificate = new X509Certificate2(@"russian_trusted_root_ca_pem.crt");
        X509Certificate2 intermediateCertificate = new X509Certificate2(@"russian_trusted_sub_ca_pem.crt");
        HttpClientHandler httpClientHandler = new HttpClientHandler();
        httpClientHandler.ServerCertificateCustomValidationCallback = (message, serverCert, chain, sslPolicyErrors) =>
        {
            if ((sslPolicyErrors & ~SslPolicyErrors.RemoteCertificateChainErrors) != 0) return false;
            if(chain is object)
            {
                chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
                chain.ChainPolicy.CustomTrustStore.Add(rootCertificate);
                chain.ChainPolicy.ExtraStore.Add(intermediateCertificate);
                if(serverCert is object)
                {
                    Console.WriteLine(chain.Build(serverCert));
                    return chain.Build(serverCert);
                } else return false;
            } else return false;
        };

        //httpClientHandler.Proxy = new WebProxy("95.79.53.19", 8080);
        //httpClientHandler.Proxy = new WebProxy("158.160.56.149", 8080);
        //httpClientHandler.Proxy = new WebProxy("185.15.172.212", 3128);
        httpClientHandler.Proxy = proxyService.getRandomProxy();
        Console.WriteLine("Using proxy "+ httpClientHandler.Proxy.GetProxy(new Uri("https://pkk.rosreestr.ru/")).ToString());
        httpClientHandler.UseProxy = true;

        using HttpClient client = new HttpClient(handler: httpClientHandler, disposeHandler: true);
        client.Timeout = TimeSpan.FromSeconds(15);
        client.DefaultRequestHeaders.Clear();
        client.DefaultRequestHeaders.Add("X-requested-with", "XMLHttpRequest");
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/plain"));
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));
        client.DefaultRequestHeaders.AcceptLanguage.Clear();
        client.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue("ru"));
        client.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue("ru-RU", 0.9));
        client.DefaultRequestHeaders.AcceptEncoding.Clear();
        client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
        client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("deflate"));
        client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("br"));
        client.DefaultRequestHeaders.Pragma.Clear();
        client.DefaultRequestHeaders.Pragma.Add(new NameValueHeaderValue("no-cache"));
        client.DefaultRequestHeaders.Referrer = new Uri("https://pkk.rosreestr.ru/");
        client.DefaultRequestHeaders.UserAgent.Clear();
        client.DefaultRequestHeaders.UserAgent.ParseAdd(PKKConstants.USER_AGENT);
        Console.WriteLine(client.DefaultRequestHeaders.ToString());
        string url = "https://pkk.rosreestr.ru/api/features/1/" + parcelCode;
        LogInfo(url);

        HttpResponseMessage response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
        HttpContent content = response.Content;

        //dispose resources
        httpClientHandler.Dispose();
        client.Dispose();

        // ... Check Status Code                                
        Console.WriteLine("Response StatusCode: " + (int)response.StatusCode);

        // ... Read the string.
        string result = await content.ReadAsStringAsync();
        return result;
    }
    

    public static void LogError(string message)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        LogInfo(message);
        Console.ResetColor();
    }

    public static void LogInfo(string message)
    {
        Console.WriteLine($"{DateTime.Now:HH:mm:ss}: {message}");
    }

}