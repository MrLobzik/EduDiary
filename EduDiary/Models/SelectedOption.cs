public class SelectedOption
{
    public int Id { get; set; }
    public int AnswerOptionId { get; set; }
    public AnswerOption AnswerOption { get; set; }
    public int StudentAnswerId { get; set; }
    public StudentAnswer StudentAnswer { get; set; }
}