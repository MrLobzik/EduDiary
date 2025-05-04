public class Section
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Content { get; set; }
    public int CourseId { get; set; }
    public Course Course { get; set; }
    public ICollection<Resource> Resources { get; set; }
}