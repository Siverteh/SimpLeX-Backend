using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace SimpLeX_Backend.Models
{
    public class ImageUploadModel
    {
        public string Image { get; set; }
        public string ProjectId { get; set; }
    }


}