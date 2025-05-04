public class TestAttempt
{
    public int Id { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public int? Score { get; set; }
    public int TestId { get; set; }
    public Test Test { get; set; }
    public string StudentId { get; set; }
    public User Student { get; set; }
    public ICollection<StudentAnswer> Answers { get; set; }
}