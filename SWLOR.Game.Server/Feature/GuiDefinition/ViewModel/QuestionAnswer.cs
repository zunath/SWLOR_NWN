namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    internal sealed class QuestionAnswer
    {
        public string Question { get; }
        public string Answer { get; }

        public QuestionAnswer(string question, string answer)
        {
            Question = question;
            Answer = answer;
        }
    }
}
