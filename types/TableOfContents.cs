
public class TableOfContents
{
    public Entry[] Entries { get; set; }

    public class Entry
    {
        public string Title { get; set; }
        public string Link { get; set; }
        public List<Entry> Children { get; set; }
    }
}
