using HtmlAgilityPack;

public static class HttpClientLoader
{
    private static readonly HttpClient Client;

    static HttpClientLoader()
    {
        var handler = new HttpClientHandler { UseProxy = false, MaxConnectionsPerServer = 100 };
        Client = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(30)
        };

        Client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/101.0.4951.54 Safari/537.36");
    }

    public static HtmlDocument Run(string url)
    {
        Console.WriteLine($"Downloading: {url}");

        var html = Client.GetStringAsync(url).GetAwaiter().GetResult();

        return LoadHtml(html);
    }

    private static HtmlDocument LoadHtml(string input)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(input);

        return doc;
    }
}