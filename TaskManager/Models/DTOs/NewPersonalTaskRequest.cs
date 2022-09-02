namespace TaskManager.Models.DTOs
{
    public class NewPersonalTaskRequest
    {
        public string Title { get; set; }
        public string? Description { get; set; }
        public DateTime? DueDate { get; set; }
    }
}
