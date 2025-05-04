public class CourseStudent
{
    public int CourseId { get; set; }
    public Course Course { get; set; }
    public string StudentId { get; set; }
    public User Student { get; set; }
}