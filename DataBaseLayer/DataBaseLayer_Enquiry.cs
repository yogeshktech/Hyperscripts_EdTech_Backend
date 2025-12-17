using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;
using Npgsql;
using System.Data;
using System.Xml.Linq;

namespace CareerCracker.DataBaseLayer
{
    public interface IDataBaseLayer_Enquiry
    {
        Task<IActionResult> ContactEnquiry(IFormCollection form);
        Task<IActionResult> GetAllEnquiries();
        Task<IActionResult> GetEnquiryById(int id);
        Task<IActionResult> DeleteEnquiry(int id);

        //Career JOb 
        Task<IActionResult> Careerjob(IFormCollection form);
        Task<IActionResult> GetAllResume();
        Task<IActionResult> GetResumeById(int id);
        Task<IActionResult> DeleteCareer(int id);
    }

    public partial interface IDataBaseLayer : IDataBaseLayer_Enquiry { }

    public partial class DataBaseLayer 
    {
        // INSERT NEW ENQUIRY
        public async Task<IActionResult> ContactEnquiry(IFormCollection form)
        {
            try
            {
                string name = form["name"];
                string email = form["email"];
                string contact = form["contact"];
                string message = form["message"];

                if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email))
                {
                    return new BadRequestObjectResult(new { success = false, message = "Name and Email are required." });
                }

                await using var con = new NpgsqlConnection(DbConnection);
                await con.OpenAsync();

                string query = @"
                    INSERT INTO enquiries (name, email, contact, message)
                    VALUES (@name, @email, @contact, @message);
                ";

                await using var cmd = new NpgsqlCommand(query, con);
                cmd.Parameters.AddWithValue("@name", name);
                cmd.Parameters.AddWithValue("@email", email);
                cmd.Parameters.AddWithValue("@contact", string.IsNullOrWhiteSpace(contact) ? (object)DBNull.Value : contact);
                cmd.Parameters.AddWithValue("@message", string.IsNullOrWhiteSpace(message) ? (object)DBNull.Value : message);

                int rows = await cmd.ExecuteNonQueryAsync();

                return rows > 0
                    ? new OkObjectResult(new { success = true, message = "Enquiry submitted successfully." })
                    : new BadRequestObjectResult(new { success = false, message = "Failed to submit enquiry." });
            }
            catch (Exception ex)
            {
                return new ObjectResult(new { success = false, message = ex.Message }) { StatusCode = 500 };
            }
        }

        // GET ALL ENQUIRIES
        public async Task<IActionResult> GetAllEnquiries()
        {
            try
            {
                var list = new List<object>();

                await using var con = new NpgsqlConnection(DbConnection);
                await con.OpenAsync();

                string query = "SELECT id, name, email, contact, message, created_at FROM enquiries ORDER BY id DESC";

                await using var cmd = new NpgsqlCommand(query, con);
                await using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    list.Add(new
                    {
                        id = reader.GetInt32(0),
                        name = reader.GetString(1),
                        email = reader.GetString(2),
                        contact = reader.IsDBNull(3) ? null : reader.GetString(3),
                        message = reader.IsDBNull(4) ? null : reader.GetString(4),
                        created_at = reader.GetDateTime(5)
                    });
                }

                return new OkObjectResult(list);
            }
            catch (Exception ex)
            {
                return new ObjectResult(new { success = false, message = ex.Message }) { StatusCode = 500 };
            }
        }

        // GET ENQUIRY BY ID
        public async Task<IActionResult> GetEnquiryById(int id)
        {
            try
            {
                await using var con = new NpgsqlConnection(DbConnection);
                await con.OpenAsync();

                string query = "SELECT id, name, email, contact, message, created_at FROM enquiries WHERE id=@id";

                await using var cmd = new NpgsqlCommand(query, con);
                cmd.Parameters.AddWithValue("@id", id);

                await using var reader = await cmd.ExecuteReaderAsync();

                if (!await reader.ReadAsync())
                    return new NotFoundObjectResult(new { success = false, message = "Enquiry not found." });

                var data = new
                {
                    id = reader.GetInt32(0),
                    name = reader.GetString(1),
                    email = reader.GetString(2),
                    contact = reader.IsDBNull(3) ? null : reader.GetString(3),
                    message = reader.IsDBNull(4) ? null : reader.GetString(4),
                    created_at = reader.GetDateTime(5)
                };

                return new OkObjectResult(data);
            }
            catch (Exception ex)
            {
                return new ObjectResult(new { success = false, message = ex.Message }) { StatusCode = 500 };
            }
        }

        // DELETE ENQUIRY
        public async Task<IActionResult> DeleteEnquiry(int id)
        {
            try
            {
                await using var con = new NpgsqlConnection(DbConnection);
                await con.OpenAsync();

                string query = "DELETE FROM enquiries WHERE id=@id";

                await using var cmd = new NpgsqlCommand(query, con);
                cmd.Parameters.AddWithValue("@id", id);

                int rows = await cmd.ExecuteNonQueryAsync();

                return rows > 0
                    ? new OkObjectResult(new { success = true, message = "Deleted successfully." })
                    : new NotFoundObjectResult(new { success = false, message = "Enquiry not found." });
            }
            catch (Exception ex)
            {
                return new ObjectResult(new { success = false, message = ex.Message }) { StatusCode = 500 };
            }
        }


        //Carrer JOb 

        public async Task<IActionResult> Careerjob(IFormCollection form)
        {
            try
            {
                // Extract form fields
                string name = form["name"];
                string email = form["email"];
                string contact = form["contact"];
                string subject = form["subject"];
                string message = form["message"];

                // file input name = "resume"
                IFormFile resumeFile = form.Files["resume"];
                string savedResumePath = null;

                // Validation
                if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email) || resumeFile == null)
                {
                    return new BadRequestObjectResult(new
                    {
                        success = false,
                        message = "Name, Email, and Resume are required."
                    });
                }

                // Save File
                if (resumeFile != null && resumeFile.Length > 0)
                {
                    string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/resumes");

                    if (!Directory.Exists(uploadsFolder))
                        Directory.CreateDirectory(uploadsFolder);

                    string uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(resumeFile.FileName);
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await resumeFile.CopyToAsync(stream);
                    }

                    savedResumePath = $"/uploads/resumes/{uniqueFileName}";
                }

                // Insert into Database
                await using var con = new NpgsqlConnection(DbConnection);
                await con.OpenAsync();

                string query = @"
            INSERT INTO carrers (name, email, contact, subject, resume, message)
            VALUES (@name, @email, @contact, @subject, @resume, @message);
        ";

                await using var cmd = new NpgsqlCommand(query, con);
                cmd.Parameters.AddWithValue("@name", name);
                cmd.Parameters.AddWithValue("@email", email);
                cmd.Parameters.AddWithValue("@contact", contact);
                cmd.Parameters.AddWithValue("@subject", subject);
                cmd.Parameters.AddWithValue("@resume", savedResumePath ?? "");
                cmd.Parameters.AddWithValue("@message", message);

                int rows = await cmd.ExecuteNonQueryAsync();

                return new OkObjectResult(new
                {
                    success = true,
                    message = "Career job enquiry submitted successfully."
                });
            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        public async Task<IActionResult> GetAllResume()
        {
            try
            {
                await using var con = new NpgsqlConnection(DbConnection);
                await con.OpenAsync();

                string query = "SELECT * FROM carrers ORDER BY id DESC;";

                await using var cmd = new NpgsqlCommand(query, con);
                await using var reader = await cmd.ExecuteReaderAsync();

                var list = new List<object>();

                while (await reader.ReadAsync())
                {
                    list.Add(new
                    {
                        id = reader["id"],
                        name = reader["name"],
                        email = reader["email"],
                        contact = reader["contact"],
                        subject = reader["subject"],
                        resume = reader["resume"],
                        message = reader["message"],
                        created_at = reader["created_at"]
                    });
                }

                return new OkObjectResult(new { success = true, data = list });
            }
            catch (Exception ex)
            {
                return new ObjectResult(new { success = false, message = ex.Message }) { StatusCode = 500 };
            }
        }

        public async Task<IActionResult> GetResumeById(int id)
        {
            try
            {
                await using var con = new NpgsqlConnection(DbConnection);
                await con.OpenAsync();

                string query = "SELECT * FROM carrers WHERE id = @id LIMIT 1;";

                await using var cmd = new NpgsqlCommand(query, con);
                cmd.Parameters.AddWithValue("@id", id);

                await using var reader = await cmd.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    var career = new
                    {
                        id = reader["id"],
                        name = reader["name"],
                        email = reader["email"],
                        contact = reader["contact"],
                        subject = reader["subject"],
                        resume = reader["resume"],
                        message = reader["message"],
                        created_at = reader["created_at"]
                    };

                    return new OkObjectResult(new { success = true, data = career });
                }

                return new NotFoundObjectResult(new { success = false, message = "Resume not found." });
            }
            catch (Exception ex)
            {
                return new ObjectResult(new { success = false, message = ex.Message }) { StatusCode = 500 };
            }
        }

        public async Task<IActionResult> DeleteCareer(int id)
        {
            try
            {
                await using var con = new NpgsqlConnection(DbConnection);
                await con.OpenAsync();

                string query = "DELETE FROM carrers WHERE id = @id;";

                await using var cmd = new NpgsqlCommand(query, con);
                cmd.Parameters.AddWithValue("@id", id);

                int rows = await cmd.ExecuteNonQueryAsync();

                return rows > 0
                    ? new OkObjectResult(new { success = true, message = "Resume deleted successfully." })
                    : new NotFoundObjectResult(new { success = false, message = "Resume not found." });
            }
            catch (Exception ex)
            {
                return new ObjectResult(new { success = false, message = ex.Message }) { StatusCode = 500 };
            }
        }


    }
}
