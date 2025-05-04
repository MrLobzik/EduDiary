public class Resource
{
    public int Id { get; set; }
    public string Title { get; set; }
    public ResourceType Type { get; set; }
    public string Url { get; set; }
    public string Content { get; set; }
    public int SectionId { get; set; }
    public Section Section { get; set; }
}

public enum ResourceType
{
    Link,
    File,
    Text
}