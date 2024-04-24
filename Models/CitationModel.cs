using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace SimpLeX_Backend.Models
{
    public class Citation
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string CitationId { get; set; } = Guid.NewGuid().ToString();
        
        public string? CitationKey { get; set; }  // Field to store the citation key

        [Required] 
        public string ProjectId { get; set; } = null!;

        [MaxLength(50)] 
        public string ReferenceType { get; set; } = null!;  // e.g., Article, Book, etc.

        public string? Authors { get; set; }  // Authors are stored as a single string; could be split into a separate table if necessary

        public string? Title { get; set; }

        [MaxLength(4)]
        public string? Year { get; set; }

        public string? Journal { get; set; }

        public string? BookTitle { get; set; }

        public string? Publisher { get; set; }

        [MaxLength(50)]
        public string? Edition { get; set; }

        [MaxLength(50)]
        public string? Volume { get; set; }

        [MaxLength(50)]
        public string? Number { get; set; }

        public string? Pages { get; set; }

        [MaxLength(255)]
        public string? DOI { get; set; }

        [MaxLength(255)]
        public string? URL { get; set; }

        public string? Misc { get; set; }  // Miscellaneous details

        [Required]
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        [Required]
        public DateTime LastModifiedDate { get; set; } = DateTime.UtcNow;

        // Utility method to represent this citation in a readable format, perhaps for debugging
        public override string ToString()
        {
            return $"Citation: {Title}, {Year}, {Authors}";
        }
    }
}
