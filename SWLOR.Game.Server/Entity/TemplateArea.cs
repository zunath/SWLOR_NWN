namespace SWLOR.Game.Server.Entity
{
    internal class TemplateArea : EntityBase
    {
        [Indexed]
        public string TemplateAreaData { get; set; }
        [Indexed]
        public string TemplateAreaName { get; set; }
        [Indexed]
        public string TemplateAreaTag { get; set; }
        [Indexed]
        public string TemplateAreaResRef { get; set; }
    }
}
