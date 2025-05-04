public class AssignmentSubmission
{
    public int Id { get; set; }
    public string FilePath { get; set; }
    public string Text { get; set; }
    public DateTime SubmissionDate { get; set; }
    public int? Grade { get; set; }
    public string Feedback { get; set; }
    public int AssignmentId { get; set; }
    public Assignment Assignment { get; set; }
    public string StudentId { get; set; }
    public User Student { get; set; }
}