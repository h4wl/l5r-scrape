using HtmlAgilityPack;
using System.Text.Json;
using System.Collections.Generic;
using System.Linq;



var homepageHtml = await CallUrl();

var homepage = new HtmlDocument();
homepage.LoadHtml(homepageHtml);

var topMenuLinkedPages = await ScrapeTopMenu(homepage);
Console.WriteLine($"Top Menu links to {topMenuLinkedPages.Count()}");
Thread.Sleep(1000);

var sideMenuLinkedPages = await ScrapeSideMenu(homepage);
Console.WriteLine($"Side Menu links to {sideMenuLinkedPages.Count()}");
Thread.Sleep(1000);

var allLinkedPages = topMenuLinkedPages.Union(sideMenuLinkedPages).ToList();
Console.WriteLine($"Both Menus links to {allLinkedPages.Count()}");
allLinkedPages.Sort((x, y) => string.Compare(x, y));
foreach (var page in allLinkedPages)
{
    Console.WriteLine(page);
}


foreach (var pageUrl in allLinkedPages)
{
    var pageName = pageUrl.Remove(0, 1);
    var doc = new HtmlDocument();
    if (pageUrl == "/history")
    {
        var pageHtml = await CallUrl(pageUrl);
        doc.LoadHtml(pageHtml);
        var tocNodes = doc.DocumentNode.SelectNodes("//div[@id = 'toc-list']/div");
        var hasToc = tocNodes.Count > 0;

        var page = new Page();

        if (hasToc)
        {
            var toc = new TableOfContents{};
            var entries = new List<TableOfContents.Entry>();
            foreach (var n in tocNodes)
            {
                var style = n.GetAttributeValue("style", string.Empty);
                style = style.Replace("margin-left: ", string.Empty);
                style = style.Replace("em;", string.Empty);
                _ = int.TryParse(style, out int level);
                var a = n.ChildNodes.FirstOrDefault(x => x.Name == "a");
                var title = a.InnerText;
                var link = a.GetAttributeValue("href", string.Empty);

                link = $"/l5r/{pageName}{link}";

                var entry = new TableOfContents.Entry {
                    Title = title,
                    Link = link
                };
                if(level <= 1)
                {
                    entries.Add(entry);
                }
                else if (level == 2)
                {
                    var last = entries.Last();
                    last.Children ??= new List<TableOfContents.Entry>();
                    last.Children.Add(entry);
                }
                else if (level == 3)
                {
                    var last = entries.Last();
                    var lastChild = last.Children.Last();
                    lastChild.Children ??= new List<TableOfContents.Entry>();
                    lastChild.Children.Add(entry);
                }
                else
                {
                    throw new NotImplementedException("Table of Contents Depth > 3 not supported");
                }
            }
            toc.Entries = entries.ToArray();

            page.TableOfContents = toc;

            using var sw = new StringWriter();
            sw.WriteLine("# test");

            

            await WriteToOutputFolder($"_{pageName}.json", SerializeIndented(page));
            await WriteToOutputFolder($"{pageName}.md", sw.ToString());
        }
    }
}

static async Task<string> CallUrl(string page = "")
{
    string urlBase = "http://lasthaiku.wikidot.com";
    string fullUrl = $"{urlBase}{page}";
	HttpClient client = new HttpClient();
	var response = await client.GetStringAsync(fullUrl);
	return response;
}

static string SerializeIndented<T>(T value)
{
    return JsonSerializer.Serialize(value, new JsonSerializerOptions { WriteIndented = true});
}

static async Task WriteToOutputFolder(string fileName, string text)
{
    string outputFolder = @"output";
    Directory.CreateDirectory(outputFolder);
    string dataFolder = $"{outputFolder}/data";
    Directory.CreateDirectory(dataFolder);
    await File.WriteAllTextAsync(Path.Combine(outputFolder, fileName), text);
}

static async Task SerializeToOutputFolder<T>(string fileName, T value)
{
    await WriteToOutputFolder(fileName, SerializeIndented(value));
}

static async Task<List<string>> ScrapeTopMenu(HtmlDocument doc)
{
    var topMenuNodes = doc.DocumentNode.SelectNodes("//div[@id = 'top-bar']/ul/li");
    var topMenuItems = new List<TopMenuItem>();
    var linkedPages = new List<string>();
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
                var childPath = childLink.GetAttributeValue("href", string.Empty);
                children.Add(new MenuItem {
                    Title = childLink.InnerText,
                    Path = childPath
                });
                linkedPages.Add(childPath);
            }   
        }

        topMenuItems.Add(new TopMenuItem {
            Path = path,
            Title = a.InnerText,
            Children = children.ToArray()
        });
        linkedPages.Add(path);
    }
    var topMenu = new TopMenu {
        MenuItems = topMenuItems.ToArray()
    };
    await SerializeToOutputFolder("data/topMenu.json", topMenu);
    return linkedPages.Distinct().ToList();
}

static async Task<List<string>> ScrapeSideMenu(HtmlDocument doc)
{
    var sideMenuNodes = doc.DocumentNode.SelectNodes("//div[@id = 'side-bar']/p[text() = 'More pages']/following-sibling::ul/li/a");
    var sideMenuItems = new List<MenuItem>();
    var linkedPages = new List<string>();
    foreach (var a in sideMenuNodes)
    {
        var path = a.GetAttributeValue("href", string.Empty);
        sideMenuItems.Add(new MenuItem {
            Title = a.InnerText,
            Path = path
        });
        linkedPages.Add(path);
    }

    var sideMenu = new GenericMenu {
        MenuItems = sideMenuItems.ToArray() 
    };

    await SerializeToOutputFolder("data/sideMenu.json", sideMenu);
    return linkedPages.Distinct().ToList();
}