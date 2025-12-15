using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace CareerCracker.DataBaseLayer
{
    public interface IDataBaseLayer_Faculty
    {
        Task<IActionResult> InsertFaculty(IFormCollection form);
        Task<IActionResult> UpdateFacultyBySlug(string slug, IFormCollection form);
        Task<IActionResult> GetAllFaculties();
        Task<IActionResult> GetFacultyBySlug(string slug);
        Task<IActionResult> DeleteFacultyBySlug(string slug);
        Task<IActionResult> ToggleFacultyStatusBySlug(string slug);
    }

    public partial interface IDataBaseLayer : IDataBaseLayer_Faculty { }

    public partial class DataBaseLayer : ControllerBase
    {
        public async Task<IActionResult> InsertFaculty(IFormCollection form)
        {
            try
            {
                if (form == null || form.Count == 0)
                    return BadRequest(new { success = false, message = "Form data is missing" });

                string name = form["name"];
                string email = form["email"];
                string position = form["position"];
                string experience = form["experience"];
                string specialization = form["specialization"];
                string createdBy = form["created_by"];

                if (string.IsNullOrWhiteSpace(name))
                    return BadRequest(new { success = false, message = "Faculty name is required" });

                string slug = GenerateSlug(name);

                // ------------ Image Upload ------------
                IFormFile imageFile = form.Files["profile_image"];
                string? savedImagePath = null;

                if (imageFile != null && imageFile.Length > 0)
                {
                    string uploadsFolder = Path.Combine(
                        Directory.GetCurrentDirectory(),
                        "wwwroot/uploads/facultyImages"
                    );

                    if (!Directory.Exists(uploadsFolder))
                        Directory.CreateDirectory(uploadsFolder);

                    string uniqueFileName = Guid.NewGuid() + Path.GetExtension(imageFile.FileName);
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                        await imageFile.CopyToAsync(stream);

                    savedImagePath = $"/uploads/facultyImages/{uniqueFileName}";
                }

                using (var con = new NpgsqlConnection(DbConnection))
                {
                    await con.OpenAsync();

                    // Check duplicate name
                    string checkName = "SELECT COUNT(*) FROM faculties WHERE LOWER(name)=LOWER(@name)";
                    using (var cmd = new NpgsqlCommand(checkName, con))
                    {
                        cmd.Parameters.AddWithValue("@name", name);
                        if ((long)await cmd.ExecuteScalarAsync() > 0)
                            return BadRequest(new { success = false, message = "Faculty name already exists" });
                    }

                    // Check duplicate slug
                    string checkSlug = "SELECT COUNT(*) FROM faculties WHERE LOWER(slug)=LOWER(@slug)";
                    using (var cmd = new NpgsqlCommand(checkSlug, con))
                    {
                        cmd.Parameters.AddWithValue("@slug", slug);
                        if ((long)await cmd.ExecuteScalarAsync() > 0)
                            return BadRequest(new { success = false, message = "Faculty slug already exists" });
                    }

                    string insertQuery = @"
          INSERT INTO faculties 
          (name, slug, email, position, experience, specialization,
           profile_image, status, created_by, created_at, updated_at)
          VALUES
          (@name, @slug, @email, @position, @experience, @special,
           @image, TRUE, @created_by, NOW(), NOW());
      ";

                    using (var cmd = new NpgsqlCommand(insertQuery, con))
                    {
                        cmd.Parameters.AddWithValue("@name", name);
                        cmd.Parameters.AddWithValue("@slug", slug);
                        cmd.Parameters.AddWithValue("@email", (object?)email ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@position", (object?)position ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@experience", (object?)experience ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@special", (object?)specialization ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@image", (object?)savedImagePath ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@created_by", (object?)createdBy ?? DBNull.Value);

                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                return Ok(new { success = true, message = "Faculty inserted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }
        public async Task<IActionResult> UpdateFacultyBySlug(string slug, IFormCollection form)
        {
            try
            {
                string name = form["name"];
                string newSlug = GenerateSlug(name);
                string email = form["email"];
                string position = form["position"];
                string experience = form["experience"];
                string specialization = form["specialization"];

                if (string.IsNullOrWhiteSpace(name))
                    return BadRequest(new { success = false, message = "Faculty name is required" });

                string? oldImagePath = null;
                string? newImagePath = null;

                IFormFile imageFile = form.Files["profile_image"];

                using (var con = new NpgsqlConnection(DbConnection))
                {
                    await con.OpenAsync();

                    string fetchQuery = "SELECT profile_image FROM faculties WHERE slug=@slug";

                    using (var cmd = new NpgsqlCommand(fetchQuery, con))
                    {
                        cmd.Parameters.AddWithValue("@slug", slug);
                        var reader = await cmd.ExecuteReaderAsync();

                        if (!reader.HasRows)
                            return NotFound(new { success = false, message = "Faculty not found" });

                        await reader.ReadAsync();
                        oldImagePath = reader["profile_image"]?.ToString();
                        reader.Close();
                    }

                    if (imageFile != null && imageFile.Length > 0)
                    {
                        string uploadsFolder = Path.Combine(
                            Directory.GetCurrentDirectory(),
                            "wwwroot/uploads/facultyImages"
                        );

                        if (!Directory.Exists(uploadsFolder))
                            Directory.CreateDirectory(uploadsFolder);

                        string uniqueFileName = Guid.NewGuid() + Path.GetExtension(imageFile.FileName);
                        string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                            await imageFile.CopyToAsync(stream);

                        newImagePath = $"/uploads/facultyImages/{uniqueFileName}";
                    }
                    else
                    {
                        newImagePath = oldImagePath;
                    }

                    string updateQuery = @"
          UPDATE faculties SET
              name=@name,
              slug=@newSlug,
              email=@email,
              position=@position,
              experience=@experience,
              specialization=@special,
              profile_image=@image,
              updated_at=NOW()
          WHERE slug=@slug;
      ";

                    using (var cmd = new NpgsqlCommand(updateQuery, con))
                    {
                        cmd.Parameters.AddWithValue("@slug", slug);
                        cmd.Parameters.AddWithValue("@name", name);
                        cmd.Parameters.AddWithValue("@newSlug", newSlug);
                        cmd.Parameters.AddWithValue("@email", (object?)email ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@position", (object?)position ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@experience", (object?)experience ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@special", (object?)specialization ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@image", (object?)newImagePath ?? DBNull.Value);

                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                return Ok(new { success = true, message = "Faculty updated successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }



        public async Task<IActionResult> GetAllFaculties()
        {
            try
            {
                using (var con = new NpgsqlConnection(DbConnection))
                {
                    await con.OpenAsync();

                    string query = @"
                SELECT * FROM faculties ORDER BY id DESC;
            ";

                    using (var cmd = new NpgsqlCommand(query, con))
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        var list = new List<object>();

                        while (await reader.ReadAsync())
                        {
                            list.Add(new
                            {
                                id = reader["id"],
                                name = reader["name"],
                                slug = reader["slug"],
                                email = reader["email"],
                                courseId = reader["course_id"],
                                position = reader["position"],
                                experience = reader["experience"],
                                specialization = reader["specialization"],
                                profileImage = reader["profile_image"],
                                status = reader["status"],
                                createdAt = reader["created_at"],
                                updatedAt = reader["updated_at"]
                            });
                        }

                        return Ok(new { success = true, data = list });
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }


        public async Task<IActionResult> GetFacultyBySlug(string slug)
        {
            try
            {
                using (var con = new NpgsqlConnection(DbConnection))
                {
                    await con.OpenAsync();

                    string query = "SELECT * FROM faculties WHERE slug=@slug";

                    using (var cmd = new NpgsqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@slug", slug);

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (!reader.HasRows)
                                return NotFound(new { success = false, message = "Faculty not found" });

                            await reader.ReadAsync();

                            var faculty = new
                            {
                                id = reader["id"],
                                name = reader["name"],
                                slug = reader["slug"],
                                email = reader["email"],
                                courseId = reader["course_id"],
                                position = reader["position"],
                                experience = reader["experience"],
                                specialization = reader["specialization"],
                                profileImage = reader["profile_image"],
                                status = reader["status"],
                                createdAt = reader["created_at"],
                                updatedAt = reader["updated_at"]
                            };

                            return Ok(new { success = true, data = faculty });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }



        public async Task<IActionResult> DeleteFacultyBySlug(string slug)
        {
            try
            {
                string? imagePath = null;

                using (var con = new NpgsqlConnection(DbConnection))
                {
                    await con.OpenAsync();

                    // Fetch image path
                    string fetch = "SELECT profile_image FROM faculties WHERE slug=@slug";

                    using (var cmd = new NpgsqlCommand(fetch, con))
                    {
                        cmd.Parameters.AddWithValue("@slug", slug);

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (!reader.HasRows)
                                return NotFound(new { success = false, message = "Faculty not found" });

                            await reader.ReadAsync();
                            imagePath = reader["profile_image"]?.ToString();
                        }
                    }

                    // Delete faculty row
                    string deleteQuery = "DELETE FROM faculties WHERE slug=@slug";

                    using (var cmd = new NpgsqlCommand(deleteQuery, con))
                    {
                        cmd.Parameters.AddWithValue("@slug", slug);
                        await cmd.ExecuteNonQueryAsync();
                    }

                    // Delete image file
                    if (!string.IsNullOrEmpty(imagePath))
                    {
                        string fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", imagePath.TrimStart('/'));
                        if (System.IO.File.Exists(fullPath))
                            System.IO.File.Delete(fullPath);
                    }
                }

                return Ok(new { success = true, message = "Faculty deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }



        public async Task<IActionResult> ToggleFacultyStatusBySlug(string slug)
        {
            try
            {
                using (var con = new NpgsqlConnection(DbConnection))
                {
                    await con.OpenAsync();

                    string query = @"
                UPDATE faculties 
                SET status = NOT status, updated_at = NOW()
                WHERE slug=@slug
                RETURNING status;
            ";

                    using (var cmd = new NpgsqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@slug", slug);

                        var result = await cmd.ExecuteScalarAsync();

                        if (result == null)
                            return NotFound(new { success = false, message = "Faculty not found" });

                        bool newStatus = (bool)result;

                        return Ok(new
                        {
                            success = true,
                            message = newStatus ? "Faculty Activated" : "Faculty Deactivated",
                            status = newStatus
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }


    }

}