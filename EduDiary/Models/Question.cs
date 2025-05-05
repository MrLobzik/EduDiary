namespace EduDiary.Models
{
    public class Question
    {
        public int Id { get; set; }
        public string Text { get; set; }
        public QuestionType Type { get; set; }
        public int Points { get; set; }
        public int TestId { get; set; }
        public Test Test { get; set; }
        public ICollection<AnswerOption> AnswerOptions { get; set; }
    }

    public enum QuestionType
    {
        SingleChoice,
        MultipleChoice,
        TextAnswer
    }
}