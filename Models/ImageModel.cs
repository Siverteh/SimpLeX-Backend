using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace SimpLeX_Backend.Models
{
    public class Image
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string ImageId { get; set; }
        [Required]
        public string ProjectId { get; set; }
        [Required]
        public string ImagePath { get; set; } // Base64 encoded image data
        [Required]
        public DateTime CreationDate { get; set; }
    }

}