namespace TaskManager.Models.DTOs
{
    public class PersonalTaskDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string? Description { get; set; }
        public PersonalTaskStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? DueDate { get; set; }

        public static PersonalTaskDto? Create(PersonalTask task)
        {
            if (task == null)
                return null;

            return new PersonalTaskDto
            {
                Id = task.Id,
                Title = task.Title,
                Description = task.Description,
                Status = task.Status,
                CreatedAt = task.CreatedAt,
                DueDate = task.DueDate
            };
        }
    }
}
