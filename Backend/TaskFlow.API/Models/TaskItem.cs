using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaskFlow.API.Models
{
    public class TaskItem
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "ToDo"; // ToDo, InProgress, Done

        [Required]
        [MaxLength(20)]
        public string Priority { get; set; } = "Medium"; // Low, Medium, High

        public DateTime? DueDate {get;set;}
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Foreign Keys
        [Required]
        public int CreatedByUserId { get; set; }

        public int? AssignedToUserId { get; set; }

        // Navigation properties
        [ForeignKey("CreatedByUserId")]
        public User CreatedBy { get; set; } = null!;

        [ForeignKey("AssignedToUserId")]
        public User? AssignedTo { get; set; }
    }
}