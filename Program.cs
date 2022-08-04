using HtmlAgilityPack;
using System.Text.Json;

string urlBase = "http://lasthaiku.wikidot.com";

var homepageHtml = await CallUrl(urlBase);

var homepage = new HtmlDocument();
homepage.LoadHtml(homepageHtml);

var topMenuNodes = homepage.DocumentNode.SelectNodes("//div[@id = 'top-bar']/ul/li");
var topMenu = new List<TopMenuItem>();


foreach (var n in topMenuNodes)
{
    var a = n.ChildNodes.FirstOrDefault(x => x.Name == "a");
    var path = a?.GetAttributeValue("href", string.Empty);

    if (string.IsNullOrEmpty(path) || path == "/start" ) continue;

    var children = new List<MenuItem>();

    var childItems = n.Descendants("li")?.ToList();

    if (childItems is not null)
    {
        foreach (var item in childItems)
        {
            var childLink = item.ChildNodes.FirstOrDefault(x => x.Name == "a");
            children.Add(new MenuItem {
                Title = childLink.InnerText,
                Path = childLink.GetAttributeValue("href", string.Empty)
            });
        }
        
    }

    topMenu.Add(new TopMenuItem {
        Path = path,
        Title = a.InnerText,
        Children = children.ToArray()
    });
}

Console.WriteLine(JsonSerializer.Serialize(topMenu));


static async Task<string> CallUrl(string fullUrl)
{
	HttpClient client = new HttpClient();
	var response = await client.GetStringAsync(fullUrl);
	return response;
}