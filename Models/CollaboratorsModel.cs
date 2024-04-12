using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SimpLeX_Backend.Models
{
    public class Collaborator
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string CollaborationId { get; set; } = null!;

        [Required]
        public string ProjectId { get; set; } = null!;

        public string UserId { get; set; } = null;  // Make nullable to initially create without a user

        [Required]
        [MaxLength(50)]
        public string AccessType { get; set; } = "editor";  // Default access type

        // Navigation properties
        public virtual Project Project { get; set; }
        public virtual ApplicationUser User { get; set; }
    }

}