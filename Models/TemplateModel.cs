using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace SimpLeX_Backend.Models
{
    public class Template
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string TemplateId { get; set; }

        [Required]
        [StringLength(255)]
        public string TemplateName { get; set; }

        [Required]
        public string XMLContent { get; set; }  // XML data that configures the workspace

        public string ImagePath { get; set; }  // Optional path to an image preview of the template

        [Required]
        public bool IsCustom { get; set; }  // False for global templates, true for user-created

        // Nullable: only set for user-specific templates
        public string UserId { get; set; } 

        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; }  // Assuming use of ASP.NET Core Identity

        [Required]
        public DateTime CreatedDate { get; set; }

        [Required]
        public DateTime ModifiedDate { get; set; }
    }
}