using Microsoft.AspNetCore.Mvc;

namespace Task_44.Models
{

    [BindProperties]
    public class Person
    {
        public int Id { get; set; }
        public string name { get; set; } = "";

        public int years { get; set; }
        public int months { get; set; }
        public int days { get; set; }

    }
}
