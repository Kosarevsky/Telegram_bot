using Data.Entities;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Services.Models
{
    public class AvailableDateModel
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public int ExecutionId { get; set; }
        public ExecutionModel Execution { get; set; } = null!;
    }
}
