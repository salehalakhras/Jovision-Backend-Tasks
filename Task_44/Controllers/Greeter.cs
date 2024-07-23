using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Task_44.Models;

namespace Task_44.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class Greeter : ControllerBase
    {
        private readonly PersonContext _context;

        public Greeter(PersonContext context)
        {
            _context = context;
        }

        // GET: api/Greeter
        [HttpGet("~/Greet")]
        public async Task<ActionResult<string>> Greet(string name = "")
        {
            return "Hello " + (name.Length != 0? name : "Anonymous");
        }


        [HttpGet("~/BirthDate")]
        public async Task<ActionResult<string>> BirthDate(string name = "",int years = 0, int months = 0, int days = 0 )
        {
            DateTime now = DateTime.Today;
            TimeSpan age = new TimeSpan();
            bool birthDateExist = years != 0 && months != 0 && days != 0;
            if (birthDateExist) {
                age = now.Subtract(new DateTime(years, months, days));
            }
            
            string response = "Hello " + (name.Length != 0 ? name : "Anonymous") + ", ";
            response += (birthDateExist ? ("Your age is " + (int)(age.TotalDays / 365) ) : "I can’t calculate your age without knowing your birthdate!");
            

            return response;
        }

        // GET: api/Greeter/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Person>> GetPerson(int id)
        {
            var person = await _context.Person.FindAsync(id);

            if (person == null)
            {
                return NotFound();
            }

            return person;
        }



        // PUT: api/Greeter/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutPerson(int id, Person person)
        {
            if (id != person.Id)
            {
                return BadRequest();
            }

            _context.Entry(person).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PersonExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/Greeter
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Person>> PostPerson(Person person)
        {
            _context.Person.Add(person);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetPerson", new { id = person.Id }, person);
        }

        // DELETE: api/Greeter/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePerson(int id)
        {
            var person = await _context.Person.FindAsync(id);
            if (person == null)
            {
                return NotFound();
            }

            _context.Person.Remove(person);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool PersonExists(int id)
        {
            return _context.Person.Any(e => e.Id == id);
        }
    }
}
