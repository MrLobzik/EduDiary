namespace EduDiary.Models
{
    public class StudentAnswer
    {
        public int Id { get; set; }
        public string AnswerText { get; set; }
        public int QuestionId { get; set; }
        public int? PointsAwarded { get; set; }
        public Question Question { get; set; }
        public int TestAttemptId { get; set; }
        public TestAttempt TestAttempt { get; set; }
        public ICollection<SelectedOption> SelectedOptions { get; set; }
    }
}