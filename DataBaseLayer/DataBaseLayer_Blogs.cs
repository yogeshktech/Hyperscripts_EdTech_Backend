using Humanizer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.Elfie.Diagnostics;
using Npgsql;

namespace CareerCracker.DataBaseLayer
{
    public interface IDataBaseLayer_Blogs
    {
        Task<IActionResult> AddBlogs(IFormCollection form);
        Task<IActionResult> UpdateBlogs(int id, IFormCollection form);
        Task<IActionResult> GetAllBlogs();
        Task<IActionResult> GetBlogById(int id);
        Task<IActionResult> DeleteBlog(int id);
        Task<IActionResult> ToggleBlogStatus(int id);
    }
    public partial interface IDataBaseLayer : IDataBaseLayer_Blogs { }

    public partial class DataBaseLayer
    {
        public async Task<IActionResult> AddBlogs(IFormCollection form)
        {
            try
            {
                string name = form["blogName"];
                string slug = GenerateSlug(name);
                string blogDescription = form["blogDescription"];
                bool isActive = form.ContainsKey("is_active") && form["is_active"] == "true";

                if (string.IsNullOrEmpty(name))
                {
                    return BadRequest(new { success = false, message = "Blog name is required!" });
                }

                // -------------------------
                // IMAGE UPLOAD
                // -------------------------
                string savedImagePath = null;
                IFormFile blogImageFile = form.Files["blogImage"];

                if (blogImageFile != null && blogImageFile.Length > 0)
                {
                    string uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/blogs");
                    if (!Directory.Exists(uploadFolder)) Directory.CreateDirectory(uploadFolder);

                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(blogImageFile.FileName);
                    string savePath = Path.Combine(uploadFolder, fileName);

                    using (var stream = new FileStream(savePath, FileMode.Create))
                    {
                        await blogImageFile.CopyToAsync(stream);
                    }

                    savedImagePath = "/uploads/blogs/" + fileName;
                }

                using (var conn = new NpgsqlConnection(DbConnection))
                {
                    await conn.OpenAsync();

                    // -------------------------
                    // Check for duplicate name
                    // -------------------------
                    string checkQuery = @"SELECT COUNT(*) FROM blogs WHERE LOWER(blogs_name) = LOWER(@name)";
                    using (var checkCmd = new NpgsqlCommand(checkQuery, conn))
                    {
                        checkCmd.Parameters.AddWithValue("@name", name);
                        long exists = (long)await checkCmd.ExecuteScalarAsync();

                        if (exists > 0)
                        {
                            return Conflict(new
                            {
                                success = false,
                                message = "Blog name already exists"
                            });
                        }
                    }

                    // -------------------------
                    // Insert blog record
                    // -------------------------
                    string insertQuery = @"
                INSERT INTO blogs 
                (blogs_name, blogs_discription, blogs_slug, blogs_image, is_active, updated_at)
                VALUES 
                (@name, @desc, @slug, @image, @active, CURRENT_TIMESTAMP)
                RETURNING id";

                    using (var insertCmd = new NpgsqlCommand(insertQuery, conn))
                    {
                        insertCmd.Parameters.AddWithValue("@name", name);
                        insertCmd.Parameters.AddWithValue("@desc", (object)blogDescription ?? DBNull.Value);
                        insertCmd.Parameters.AddWithValue("@slug", slug);
                        insertCmd.Parameters.AddWithValue("@image", (object)savedImagePath ?? DBNull.Value);
                        insertCmd.Parameters.AddWithValue("@active", isActive);

                        int newId = (int)await insertCmd.ExecuteScalarAsync();

                        return Ok(new
                        {
                            success = true,
                            message = "Blog added successfully",
                            id = newId,
                            image = savedImagePath
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        public async Task<IActionResult> UpdateBlogs(int id, IFormCollection form)
        {
            try
            {
                string name = form["blogName"];
                string slug = GenerateSlug(name);
                string blogDescription = form["blogDescription"];
                bool isActive = form.ContainsKey("is_active") && form["is_active"] == "true";

                if (string.IsNullOrEmpty(name))
                {
                    return BadRequest(new { success = false, message = "Blog name is required!" });
                }

                // -------------------------
                // IMAGE UPLOAD
                // -------------------------
                string savedImagePath = null;
                IFormFile blogImageFile = form.Files["blogImage"];

                if (blogImageFile != null && blogImageFile.Length > 0)
                {
                    string uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/blogs");
                    if (!Directory.Exists(uploadFolder)) Directory.CreateDirectory(uploadFolder);

                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(blogImageFile.FileName);
                    string savePath = Path.Combine(uploadFolder, fileName);

                    using (var stream = new FileStream(savePath, FileMode.Create))
                    {
                        await blogImageFile.CopyToAsync(stream);
                    }

                    savedImagePath = "/uploads/blogs/" + fileName;
                }

                using (var conn = new NpgsqlConnection(DbConnection))
                {
                    await conn.OpenAsync();

                    // -------------------------
                    // Check if blog exists
                    // -------------------------
                    string existsQuery = @"SELECT COUNT(*) FROM blogs WHERE id = @id";
                    using (var existsCmd = new NpgsqlCommand(existsQuery, conn))
                    {
                        existsCmd.Parameters.AddWithValue("@id", id);
                        long exists = (long)await existsCmd.ExecuteScalarAsync();

                        if (exists == 0)
                        {
                            return NotFound(new { success = false, message = "Blog not found" });
                        }
                    }

                    // -------------------------
                    // Check for duplicate name
                    // -------------------------
                    string checkQuery = @"SELECT COUNT(*) FROM blogs WHERE LOWER(blogs_name) = LOWER(@name) AND id != @id";
                    using (var checkCmd = new NpgsqlCommand(checkQuery, conn))
                    {
                        checkCmd.Parameters.AddWithValue("@name", name);
                        checkCmd.Parameters.AddWithValue("@id", id);
                        long duplicate = (long)await checkCmd.ExecuteScalarAsync();

                        if (duplicate > 0)
                        {
                            return Conflict(new
                            {
                                success = false,
                                message = "Another blog with this name already exists"
                            });
                        }
                    }

                    // -------------------------
                    // Update blog record
                    // -------------------------
                    string updateQuery = @"
                UPDATE blogs
                SET blogs_name = @name,
                    blogs_discription = @desc,
                    blogs_slug = @slug,
                    blogs_image = COALESCE(@image, blogs_image),
                    is_active = @active,
                    updated_at = CURRENT_TIMESTAMP
                WHERE id = @id";

                    using (var updateCmd = new NpgsqlCommand(updateQuery, conn))
                    {
                        updateCmd.Parameters.AddWithValue("@name", name);
                        updateCmd.Parameters.AddWithValue("@desc", (object)blogDescription ?? DBNull.Value);
                        updateCmd.Parameters.AddWithValue("@slug", slug);
                        updateCmd.Parameters.AddWithValue("@image", (object)savedImagePath ?? DBNull.Value);
                        updateCmd.Parameters.AddWithValue("@active", isActive);
                        updateCmd.Parameters.AddWithValue("@id", id);

                        await updateCmd.ExecuteNonQueryAsync();

                        return Ok(new
                        {
                            success = true,
                            message = "Blog updated successfully",
                            id = id,
                            image = savedImagePath
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        public async Task<IActionResult> GetAllBlogs()
        {
            try
            {
                using (var conn = new NpgsqlConnection(DbConnection))
                {
                    await conn.OpenAsync();

                    string query = @"
                SELECT id, blogs_name, blogs_discription, blogs_slug, blogs_image, is_active, updated_at
                FROM blogs
                ORDER BY updated_at DESC";

                    using (var cmd = new NpgsqlCommand(query, conn))
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        var blogs = new List<object>();

                        while (await reader.ReadAsync())
                        {
                            blogs.Add(new
                            {
                                id = reader.GetInt32(0),
                                name = reader.GetString(1),
                                description = reader.IsDBNull(2) ? null : reader.GetString(2),
                                slug = reader.GetString(3),
                                image = reader.IsDBNull(4) ? null : reader.GetString(4),
                                isActive = reader.GetBoolean(5),
                                updatedAt = reader.GetDateTime(6)
                            });
                        }

                        return Ok(new { success = true, data = blogs });
                    }
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        public async Task<IActionResult> GetBlogById(int id)
        {
            try
            {
                using (var conn = new NpgsqlConnection(DbConnection))
                {
                    await conn.OpenAsync();

                    string query = @"
                SELECT id, blogs_name, blogs_discription, blogs_slug, blogs_image, is_active, updated_at
                FROM blogs
                WHERE id = @id";

                    using (var cmd = new NpgsqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", id);

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (!await reader.ReadAsync())
                            {
                                return NotFound(new { success = false, message = "Blog not found" });
                            }

                            var blog = new
                            {
                                id = reader.GetInt32(0),
                                name = reader.GetString(1),
                                description = reader.IsDBNull(2) ? null : reader.GetString(2),
                                slug = reader.GetString(3),
                                image = reader.IsDBNull(4) ? null : reader.GetString(4),
                                isActive = reader.GetBoolean(5),
                                updatedAt = reader.GetDateTime(6)
                            };

                            return Ok(new { success = true, data = blog });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        public async Task<IActionResult> DeleteBlog(int id)
        {
            try
            {
                using (var conn = new NpgsqlConnection(DbConnection))
                {
                    await conn.OpenAsync();

                    // -------------------------
                    // Check if blog exists & get image path
                    // -------------------------
                    string selectQuery = @"SELECT blogs_image FROM blogs WHERE id = @id";
                    string imagePath = null;

                    using (var selectCmd = new NpgsqlCommand(selectQuery, conn))
                    {
                        selectCmd.Parameters.AddWithValue("@id", id);
                        var result = await selectCmd.ExecuteScalarAsync();

                        if (result == null)
                        {
                            return NotFound(new { success = false, message = "Blog not found" });
                        }

                        imagePath = result.ToString();
                    }

                    // -------------------------
                    // Delete database record
                    // -------------------------
                    string deleteQuery = @"DELETE FROM blogs WHERE id = @id";

                    using (var deleteCmd = new NpgsqlCommand(deleteQuery, conn))
                    {
                        deleteCmd.Parameters.AddWithValue("@id", id);
                        await deleteCmd.ExecuteNonQueryAsync();
                    }

                    // -------------------------
                    // Optionally delete image file
                    // -------------------------
                    if (!string.IsNullOrEmpty(imagePath))
                    {
                        string fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", imagePath.TrimStart('/'));

                        if (System.IO.File.Exists(fullPath))
                        {
                            System.IO.File.Delete(fullPath);
                        }
                    }

                    return Ok(new
                    {
                        success = true,
                        message = "Blog deleted successfully",
                        id = id
                    });
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        public async Task<IActionResult> ToggleBlogStatus(int id)
        {
            try
            {
                using (var conn = new NpgsqlConnection(DbConnection))
                {
                    await conn.OpenAsync();

                    // -------------------------
                    // Get current status
                    // -------------------------
                    string selectQuery = @"SELECT is_active FROM blogs WHERE id = @id";

                    bool? currentStatus = null;

                    using (var cmd = new NpgsqlCommand(selectQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        var result = await cmd.ExecuteScalarAsync();

                        if (result == null)
                        {
                            return NotFound(new { success = false, message = "Blog not found" });
                        }

                        currentStatus = (bool)result;
                    }

                    // -------------------------
                    // Toggle status
                    // -------------------------
                    bool newStatus = !currentStatus.Value;

                    string updateQuery = @"
                UPDATE blogs 
                SET is_active = @newStatus, updated_at = CURRENT_TIMESTAMP
                WHERE id = @id";

                    using (var updateCmd = new NpgsqlCommand(updateQuery, conn))
                    {
                        updateCmd.Parameters.AddWithValue("@newStatus", newStatus);
                        updateCmd.Parameters.AddWithValue("@id", id);
                        await updateCmd.ExecuteNonQueryAsync();
                    }

                    return Ok(new
                    {
                        success = true,
                        message = newStatus ? "Blog activated successFully" : "Blog deactivated successFully",
                        id = id,
                        is_active = newStatus
                    });
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }


    }
}
