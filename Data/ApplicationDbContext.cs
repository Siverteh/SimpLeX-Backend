using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using SimpLeX_Backend.Models;

namespace SimpLeX_Backend.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<ApplicationUser> Users { get; set; }
        
        public DbSet<Project> Projects { get; set; }
        
        public DbSet<Collaborator> Collaborators { get; set; }
        
        public DbSet<ShareLink> ShareLinks { get; set; }
        
        public DbSet<ChatMessage> ChatMessages { get; set; }

        public DbSet<Image> Images { get; set; }
        
        public DbSet<Citation> Citations { get; set; }
        
        public DbSet<Template> Templates { get; set; }
    }
}