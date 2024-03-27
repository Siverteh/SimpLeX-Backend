using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace SimpLeX_Backend.Models
{
    public class Project
    {
        [Key]
        public int ProjectId { get; set; }

        [Required]
        public string Title { get; set; } = null!;

        public string? LatexCode { get; set; } // Nullable, assuming it can be empty.

        [Required]
        public DateTime CreationDate { get; set; }

        [Required]
        public DateTime LastModifiedDate { get; set; }

        [Required]
        public string UserId { get; set; } = null!;

        // Navigation property for the related User. 
        // [ForeignKey("UserId")] is used to specify the foreign key this navigation property relates to.
        [ForeignKey(nameof(UserId))]
        public virtual ApplicationUser User { get; set; } = null!;
    }
}