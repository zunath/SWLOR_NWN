namespace SWLOR.Game.Server.Service.MasteryService
{
    /// <summary>
    /// A single comment inside MasteryRequest.Comments. Used for the back-and-forth
    /// between the requesting player and reviewing staff on the request detail view.
    /// </summary>
    public class MasteryRequestComment
    {
        public MasteryRequestComment()
        {
            AuthorName = string.Empty;
            Text = string.Empty;
        }

        public DateTime Date { get; set; }
        public string AuthorName { get; set; }
        public bool IsStaff { get; set; }
        public string Text { get; set; }
    }
}
