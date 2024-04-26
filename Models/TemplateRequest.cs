namespace SimpLeX_Backend.Models
{
    public class TemplateRequest
    {
        public string TemplateName { get; set; }
        
        public string XMLContent { get; set; }
        
        public string? ImagePath { get; set; }
        
        public bool IsCustom { get; set; }
        
        public string? UserId { get; set; }
    }
}