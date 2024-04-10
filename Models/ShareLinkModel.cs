using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SimpLeX_Backend.Models
{
    public class ShareLink
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string ShareLinkId { get; set; }

        [Required]
        public string ProjectId { get; set; }

        [Required]
        public string InvitationToken { get; set; }

        public DateTime TokenExpiry { get; set; }

        // Navigation property
        public virtual Project Project { get; set; }
    }

}