using static System.Collections.Specialized.BitVector32;
using static System.Net.Mime.MediaTypeNames;

public class Course
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public string TeacherId { get; set; }
    public User Teacher { get; set; }
    public ICollection<Section> Sections { get; set; }
    public ICollection<CourseStudent> Students { get; set; }
    public ICollection<Assignment> Assignments { get; set; }
    public ICollection<Test> Tests { get; set; }
}