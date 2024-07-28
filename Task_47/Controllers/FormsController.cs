using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis;
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

        // Filter Type enum
        /// <summary>
        /// Filter Type &#xA;
        /// 0 - Modification Date &#xA;
        /// 1 - Creation Date Desc &#xA;
        /// 2 - Creation Date Asc &#xA;
        /// 3 - Owner
        /// </summary>
        public enum FilterType
        {
            ByModificatonDate = 0,
            ByCreationDateDescending = 1,
            ByCreationDateAscending = 2,
            ByOwner = 3
        }

        // json metadata type
        public class MetaData
        {
            public string owner;
            public DateTime creationTime;
            public DateTime modificationTime;
        }

        public class FileInfo
        {
            public string filename;
            public string owner;
            public DateTime creationTime;
            public DateTime lastModified;
        }
        

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

        // POST : Create 
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

        // GET Delete
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


        // POST Update
        [HttpPost("~/Update")]
        public async Task<ActionResult> Update([FromForm] Form form)
        {
            if (form == null)
                return BadRequest();

            if (form.Img == null || form.Img.Length == 0)
                return BadRequest("No file selected");

            var extension = Path.GetExtension(form.Img.FileName).ToLowerInvariant();

            if (string.IsNullOrEmpty(extension) || (extension != ".png" && extension != ".jpg" && extension != ".jpeg"))
                return BadRequest("Invalid file type");

            // Check if there are files uploaded before
            var files = Directory.GetFiles("uploads/");
            if (files.Length == 0)
                return BadRequest("No files uploaded yet");

            // Search for the owner provided in the uploaded files
            bool ownerExist = false;
            string fileName = "";
            MetaData imgMetaData = new MetaData();
            foreach (var file in files)
            {
                // read all the metadata files
                var fileExtention = Path.GetExtension(file).ToLowerInvariant();
                if (fileExtention.CompareTo(".json") == 0)
                {
                    using (StreamReader sr = new StreamReader(file))
                    {
                        string json = sr.ReadToEnd();
                        MetaData metaData = JsonConvert.DeserializeObject<MetaData>(json);
                        if (metaData.owner.CompareTo(form.Owner) == 0)
                        {
                            // save the metadata
                            imgMetaData = metaData;
                            ownerExist = true;
                            fileName = file;
                        }
                    }
                }
            }

                if(!ownerExist)
                    return BadRequest("There are no files uploaded with the owner: " + form.Owner);

                // delete the old image and metdadata files
                System.IO.File.Delete(fileName);
                System.IO.File.Delete(fileName.Substring(0,fileName.LastIndexOf('.')) + ".png");


            // save the uploaded image
            using (var stream = new FileStream("uploads/" + form.Img.FileName, FileMode.Create))
                {
                    await form.Img.CopyToAsync(stream);
                }

                // create the metadata json file and save the information then update the modification time
                JObject metaDataJSON = new JObject(
                    new JProperty("owner", form.Owner),
                    new JProperty("creationTime", imgMetaData.creationTime),
                    new JProperty("modificationTime", DateTime.Now)
                    );

                System.IO.File.WriteAllText("uploads/" + form.Img.FileName.Substring(0, form.Img.FileName.LastIndexOf('.')) + ".json", metaDataJSON.ToString());

                return Ok("File: " + fileName + " is replaced by: " + form.Img.FileName + " and metadata created.");
            }


        // GET Retrieve
        [HttpGet("~/Retrieve")]
        public async Task<ActionResult> Retrieve(string fileName = "", string fileOwner = "")
        {
            if (fileName.Length == 0) return BadRequest("No file name specified");
            if (fileOwner.Length == 0) return BadRequest("No file owner specified");

            var files = Directory.GetFiles("uploads/", fileName + ".*");

            // no files with the given name
            if (files.Length == 0)
                return BadRequest("File not found");

            bool sameOwner = false;
            string uploadFileName = "";
            foreach (var file in files)
            {
                if (Path.GetExtension(file).CompareTo(".json") == 0)
                {
                    using (StreamReader sr = new StreamReader(file))
                    {
                        string json = sr.ReadToEnd();
                        MetaData metadata = JsonConvert.DeserializeObject<MetaData>(json);
                        if (metadata.owner.CompareTo(fileOwner) == 0)
                        {
                            sameOwner = true;
                            uploadFileName = file.Substring(0, file.LastIndexOf("."));
                        }
                    }
                }
            }

            if (!sameOwner)
                return BadRequest("The owner specified is not the same as the owner of the file");

            Stream stream = System.IO.File.OpenRead(uploadFileName + ".png");

            // return the file in the respone body
            return new FileStreamResult(stream, "application/octet-stream");
            

        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="creationTime"></param>
        /// <param name="lastModificationTime"></param>
        /// <param name="owner"></param>
        /// <param name="filter"></param>
        /// <remarks>
        /// Creation Time and Modification Time format should be the following:&#xA;
        /// YYYY MM DD HH MM SS    or&#xA;
        /// YYYY MM DD&#xA;
        /// the numbers should be seperated by a space
        /// </remarks>
        /// <returns></returns>
        // POST Filter
        [HttpPost("~/Filter")]
        public async Task<ActionResult> Filter([FromForm] string creationTime, [FromForm] string lastModificationTime, [FromForm] string owner,[FromForm] FilterType filter)
        {
            if (!DateTime.TryParse(creationTime, out DateTime ct))
                return BadRequest("Please enter the creation time in the correct format");

            if (!DateTime.TryParse(lastModificationTime, out DateTime lmt))
                return BadRequest("Please enter the last modification time in the correct format");

            if (owner == null)
                return BadRequest("Please enter an owner");

            var files = Directory.GetFiles("uploads");

            if (files.Length == 0)
                return Ok("there are no files uploaded yet");

            List<FileInfo> filesInfoArr = new List<FileInfo>();

            foreach (var file in files)
            {
                
                if (Path.GetExtension(file).CompareTo(".json") == 0)
                {
                    using (StreamReader sr = new StreamReader(file))
                    {
                        string json = sr.ReadToEnd();
                        MetaData metadata = JsonConvert.DeserializeObject<MetaData>(json);
                        FileInfo fi = new FileInfo();
                        fi.creationTime = metadata.creationTime;
                        fi.lastModified = metadata.modificationTime;
                        fi.owner = metadata.owner;
                        fi.filename = file.Substring(0,file.LastIndexOf('.'));

                        filesInfoArr.Add(fi);
                    }
                }
            }
            Console.WriteLine(filesInfoArr[0].filename);
            List<FileInfo> outputJsonArr;
            switch (filter)
            {
                case FilterType.ByModificatonDate:
                    {
                        outputJsonArr = FilterByModificationTime(lmt, filesInfoArr);
                        if (outputJsonArr.Count == 0)
                            return Ok("There are no files modified after: " + lastModificationTime);
                    }
                    break;
                case FilterType.ByCreationDateDescending:
                    {
                        outputJsonArr = FilterByCreationTime(false,ct, filesInfoArr);
                        if (outputJsonArr.Count == 0)
                            return Ok("There are no files created after: " + creationTime);
                    }
                    break;
                case FilterType.ByCreationDateAscending:
                    {
                        outputJsonArr = FilterByCreationTime(true, ct, filesInfoArr);
                        if (outputJsonArr.Count == 0)
                            return Ok("There are no files created after: " + creationTime);
                    }
                    break;
                case FilterType.ByOwner:
                    {
                        outputJsonArr = FilterByOwner(owner, filesInfoArr);
                        if (outputJsonArr.Count == 0)
                            return Ok("There are no files that are owned by: " + owner);
                    }
                    break;
                default:
                    {
                        outputJsonArr = new List<FileInfo>();
                    }
                    break;
            }

            // select only the file name and owner in the list
            var finalJson = outputJsonArr.Select(f => new {FileName = f.filename, Owner = f.owner});

            // cast the list back into json
            string jsonStr = JsonConvert.SerializeObject(finalJson, Formatting.Indented);

            return Ok(jsonStr);
        }
        
        /// <summary>
        /// given a sort order, a date and a list of FileInfo Json objects
        /// return all the elements in the list that have a creationTime after the given creation tiem
        /// sorted in an ascending or descending order
        /// </summary>
        /// <param name="ascending"></param>
        /// <param name="creationTime"></param>
        /// <param name="allFilesJson"></param>
        /// <returns></returns>
        private List<FileInfo> FilterByCreationTime(bool ascending, DateTime creationTime, List<FileInfo> allFilesJson)
        {

            for (int i = 0; i < allFilesJson.Count; i++)
            {
                // if the file creation time is before the given creation time
                if(allFilesJson.ElementAt(i).creationTime.CompareTo(creationTime) < 0)
                {
                    // remove it from the array
                    allFilesJson.RemoveAt(i);
                    i--;
                }    
            }
            
            // order the list
            if (!ascending)
            {
                return allFilesJson.OrderByDescending(x => x.creationTime).ToList();
            }
            else
            {
                return allFilesJson.OrderBy(x => x.creationTime).ToList();
            }

        }

        /// <summary>
        /// given a modification time and a list of FileInfo 
        /// return all the elements in the list that have a been modified after the given modification time
        /// </summary>
        /// <param name="modificationTime"></param>
        /// <param name="allFilesJson"></param>
        /// <returns></returns>
        private List<FileInfo> FilterByModificationTime(DateTime modificationTime, List<FileInfo> allFilesJson)
        {
            for (int i = 0; i < allFilesJson.Count; i++)
            {
                // if the file modification time is before the given modification time
                if (allFilesJson.ElementAt(i).lastModified.CompareTo(modificationTime) < 0)
                {
                    // remove it from the array
                    allFilesJson.RemoveAt(i);
                    i--;
                }
            }

            return allFilesJson;

        }

        /// <summary>
        /// given an owner and a list of FileInfo
        /// return all the elements in the list that have the same owner as the given one
        /// </summary>
        /// <param name="owner"></param>
        /// <param name="allFilesJson"></param>
        /// <returns></returns>
        private List<FileInfo> FilterByOwner(string owner, List<FileInfo> allFilesJson)
        {
            // remove the elements where the owners dont match
            for (int i = 0; i < allFilesJson.Count; i++)
            {
                // if the file owner is not the same as the given owner
                if (allFilesJson.ElementAt(i).owner.CompareTo(owner) != 0)
                {
                    allFilesJson.RemoveAt(i);
                    i--;
                }
            }

            return allFilesJson;
        }


        /// <summary>
        /// </summary>
        /// <param name="oldOwner"></param>
        /// <param name="newOwner"></param>
        /// <returns></returns>
        [HttpGet("~/TransferOwnership")]
        public async Task<ActionResult> TransferOwnership(string oldOwner, string newOwner)
        {
            if(oldOwner == null || newOwner == null)
                 return BadRequest("Please provide an owner");

            var files = Directory.GetFiles("uploads");

            if(files.Length == 0)
                return Ok("there are no files uploaded yet");

            List<FileInfo> allFiles = new List<FileInfo>();
            foreach (var file in files)
            {
                // if the file is a JSON file
                if(Path.GetExtension(file).ToLowerInvariant().CompareTo(".json") == 0)
                {
                    using (StreamReader sr = new StreamReader(file))
                    {
                        string json = sr.ReadToEnd();
                        MetaData metadata = JsonConvert.DeserializeObject<MetaData>(json);
                        if(metadata != null && metadata.owner.CompareTo(oldOwner) == 0)
                        {
                            metadata.owner = newOwner;
                            FileInfo fi = new FileInfo();
                            fi.filename = file.Substring(0, file.LastIndexOf('.'));
                            fi.owner = metadata.owner;
                            fi.creationTime = metadata.creationTime;
                            fi.lastModified = metadata.modificationTime;
                            allFiles.Add(fi);
                            sr.Close();
                            System.IO.File.WriteAllText(file,JsonConvert.SerializeObject(metadata,Formatting.Indented).ToString());
                        }
                         // old files that are owned by the given new owner
                        else if(metadata != null && metadata.owner.CompareTo(newOwner) == 0)
                        {
                            FileInfo fi = new FileInfo();
                            fi.filename = file.Substring(0, file.LastIndexOf('.'));
                            fi.owner = metadata.owner;
                            fi.creationTime = metadata.creationTime;
                            fi.lastModified = metadata.modificationTime;
                            allFiles.Add(fi);
                        }
                    }
                }
            }

            if(allFiles.Count == 0)
                return Ok("there are no files owned by: " + oldOwner + " or " +  newOwner);

            var jsonOutput = allFiles.Select(x => new { FileName = x.filename, Owner = x.owner, });
            string jsonStr = JsonConvert.SerializeObject(jsonOutput, Formatting.Indented);
            return Ok(jsonStr);
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
