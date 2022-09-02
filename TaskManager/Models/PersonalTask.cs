namespace TaskManager.Models
{
    public class PersonalTask
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string? Description { get; set; }
        public PersonalTaskStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? DueDate { get; set; }
        public bool IsAvailable { get; set; }
    }
}
