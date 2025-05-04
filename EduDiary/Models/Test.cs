public class Test
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public DateTime AvailableFrom { get; set; }
    public DateTime AvailableTo { get; set; }
    public int TimeLimitMinutes { get; set; }
    public int CourseId { get; set; }
    public Course Course { get; set; }
    public ICollection<Question> Questions { get; set; }
    public ICollection<TestAttempt> Attempts { get; set; }
}