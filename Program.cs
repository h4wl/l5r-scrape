using HtmlAgilityPack;
using System.Text.Json;


string urlBase = "http://lasthaiku.wikidot.com";

var homepageHtml = await CallUrl(urlBase);

var homepage = new HtmlDocument();
homepage.LoadHtml(homepageHtml);

await ScrapeTopMenu(homepage);

await ScrapeSideMenu(homepage);


static async Task<string> CallUrl(string fullUrl)
{
	HttpClient client = new HttpClient();
	var response = await client.GetStringAsync(fullUrl);
	return response;
}

static async Task SerializeToOutputFolder<T>(string fileName, T value)
{
    string outputFolder = @"output";
    Directory.CreateDirectory(outputFolder);
    await File.WriteAllTextAsync(Path.Combine(outputFolder, fileName), JsonSerializer.Serialize(value, new JsonSerializerOptions { WriteIndented = true}));
}

static async Task ScrapeTopMenu(HtmlDocument doc)
{
    var topMenuNodes = doc.DocumentNode.SelectNodes("//div[@id = 'top-bar']/ul/li");
    var topMenuItems = new List<TopMenuItem>();

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

        topMenuItems.Add(new TopMenuItem {
            Path = path,
            Title = a.InnerText,
            Children = children.ToArray()
        });
    }
    var topMenu = new TopMenu {
        MenuItems = topMenuItems.ToArray()
    };
    await SerializeToOutputFolder("topMenu.json", topMenu);

}

static async Task ScrapeSideMenu(HtmlDocument doc)
{
    var sideMenuNodes = doc.DocumentNode.SelectNodes("//div[@id = 'side-bar']/p[text() = 'More pages']/following-sibling::ul/li/a");
    var sideMenuItems = new List<MenuItem>();

    foreach (var a in sideMenuNodes)
    {
        sideMenuItems.Add(new MenuItem {
            Title = a.InnerText,
            Path = a.GetAttributeValue("href", string.Empty)
        });
    }

    var sideMenu = new GenericMenu {
        MenuItems = sideMenuItems.ToArray() 
    };

    await SerializeToOutputFolder("sideMenu.json", sideMenu);

}