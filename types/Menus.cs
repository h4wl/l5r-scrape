using System.Text.Json.Serialization;

public class TopMenu
{
    public TopMenuItem[] MenuItems { get; set; }
}

public class GenericMenu
{
    public MenuItem[] MenuItems { get; set; }
}

public class MenuItem
{
    [JsonPropertyOrder(1)]
    public string Title { get; set; }
    [JsonPropertyOrder(2)]
    public string Path { get; set; }
    
}

public class TopMenuItem : MenuItem
{
    [JsonPropertyOrder(99)]
    public MenuItem[] Children { get; set; }
}

