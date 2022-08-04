
public class MenuItem
{
    public string Path { get; set; }
    public string Title { get; set; }
}

public class TopMenuItem : MenuItem
{
    public MenuItem[] Children { get; set; }
}

