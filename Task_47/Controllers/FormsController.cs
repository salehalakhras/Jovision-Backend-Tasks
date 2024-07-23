using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Task_47.Models;

namespace Task_47.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FormsController : ControllerBase
    {
        private readonly FormContext _context;

        public FormsController(FormContext context)
        {
            _context = context;
        }

        // GET: api/Forms
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Form>>> GetFormData()
        {
            return await _context.FormData.ToListAsync();
        }

        // GET: api/Forms/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Form>> GetForm(string id)
        {
            var form = await _context.FormData.FindAsync(id);

            if (form == null)
            {
                return NotFound();
            }

            return form;
        }

        // PUT: api/Forms/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutForm(string id, Form form)
        {
            if (id != form.Id)
            {
                return BadRequest();
            }

            _context.Entry(form).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!FormExists(id))
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

        [HttpPost("~/Create")]
        public async Task<ActionResult> Create([FromForm] Form form)
        {
            if (form == null)
                return BadRequest();

            if (form.Img == null || form.Img.Length == 0)
                return BadRequest("No file selected");

            var extension = Path.GetExtension(form.Img.FileName).ToLowerInvariant();

            if (string.IsNullOrEmpty(extension) || (extension != ".png" && extension != ".jpg" && extension != ".jpeg"))
                return BadRequest("Invalid file type");

            var fileName = form.Img.FileName;
            var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "uploads");
            var filePath = Path.Combine(folderPath, fileName);
            System.IO.Directory.CreateDirectory(folderPath);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await form.Img.CopyToAsync(stream);
            }
            var fileUrl = Url.Content($"~/uploads/{fileName}");
            JObject imgMetaData = new JObject(
                new JProperty("owner", form.Owner),
                new JProperty("creationTime", DateTime.Now),
                new JProperty("modificationTime", DateTime.Now)
                );
            System.IO.File.WriteAllText("uploads/" + fileName.Substring(0, fileName.LastIndexOf('.')) + ".json", imgMetaData.ToString());

            return Ok("File: " + fileName + " is uploaded and its metadata saved." );

        }

        [HttpGet("~/Delete")]
        public async Task<ActionResult> Delete(string fileName = "", string owner = "")
        {
            if (string.IsNullOrEmpty(fileName))
                return BadRequest("No file name specified");
            if (string.IsNullOrEmpty(owner))
                return BadRequest("No owner name specified");

            var files = Directory.GetFiles("uploads/", fileName + ".*");
            bool fileExist = false;
            string fileNameWithExtension = "";
            // search files for filename without extension
            if(files.Length > 0)
            {
                foreach (var file in files)
                {
                    if(file.Substring(0,file.LastIndexOf('.')).CompareTo("uploads/" + fileName) == 0)
                    {
                        fileExist = true;
                        fileNameWithExtension = file;
                    }
                }
            }

            bool isOwner = false;
            if (!fileExist)
                return BadRequest("File name provided does not exist");

            // read the json metadata and check if the owner is the same as provided
            using (StreamReader r = new StreamReader("uploads/" + fileName + ".json"))
            {
                string json = r.ReadToEnd();
                MetaData item = JsonConvert.DeserializeObject<MetaData>(json);
                if (item.owner.CompareTo(owner) == 0)
                    isOwner = true;
            }

            if (!isOwner)
                return StatusCode(403, "The owner provided is not the owner of the file: " + fileNameWithExtension);

            System.IO.File.Delete(fileNameWithExtension);
            System.IO.File.Delete("uploads/" + fileName + ".json");

            return Ok("File: " + fileNameWithExtension + " Deleted with its metadata.");
            


        }

        // json metadata type
        public class MetaData
        {
            public string owner;
            public DateTime creationTime;
            public DateTime lastModified;
        }


        // POST: api/Forms
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Form>> PostForm(Form form)
        {
            _context.FormData.Add(form);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                if (FormExists(form.Id))
                {
                    return Conflict();
                }
                else
                {
                    throw;
                }
            }

            return CreatedAtAction("GetForm", new { id = form.Id }, form);
        }

        // DELETE: api/Forms/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteForm(string id)
        {
            var form = await _context.FormData.FindAsync(id);
            if (form == null)
            {
                return NotFound();
            }

            _context.FormData.Remove(form);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool FormExists(string id)
        {
            return _context.FormData.Any(e => e.Id == id);
        }
    }
}
