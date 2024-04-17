using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SimpLeX_Backend.Models;

public class ChatMessage
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public string MessageId { get; set; }

    [Required] [ForeignKey("Project")] public string ProjectId { get; set; }
    public Project Project { get; set; }

    [Required] [ForeignKey("User")] public string UserId { get; set; }
    public ApplicationUser User { get; set; }

    [Required] public string Message { get; set; }

    [Required] public DateTime Timestamp { get; set; }
}