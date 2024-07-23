using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace Task_47.Models
{
    public class Form 
    {
        public string Id { get; set; }
        public string Owner { get; set; }
        [NotMapped]
        public IFormFile Img {  get; set; }
    }
}
