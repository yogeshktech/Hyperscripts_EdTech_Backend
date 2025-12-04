using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace CareerCracker.DataBaseLayer
{
    public interface IDataBaseLayer_Category
    {
        Task<IActionResult> InserCategory(IFormCollection form);
        Task<IActionResult> UpdataCategory(int Id, IFormCollection form);
    }

    public partial interface IDataBaseLayer : IDataBaseLayer_Category { }

    public partial class DataBaseLayer : ControllerBase
    {
        public async Task<IActionResult> InserCategory(IFormCollection form)
        {
            try
            {
                if (form == null || form.Count == 0)
                {
                    return BadRequest(new { success = false, message = "Form data is missing" });
                }

                string categoryName = form["categoryName"];
                string description = form["description"];
                bool isActive = form.ContainsKey("isActive") && form["isActive"] == "true";

                // File upload
                IFormFile categoryImageFile = form.Files["categoryImage"];
                string savedImagePath = null;

                if (string.IsNullOrEmpty(categoryName))
                {
                    return BadRequest(new { success = false, message = "Category name is required" });
                }

                
                // 1. Save Image If Uploaded
                
                if (categoryImageFile != null && categoryImageFile.Length > 0)
                {
                    string uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "categories");

                    if (!Directory.Exists(uploadFolder))
                        Directory.CreateDirectory(uploadFolder);

                    // Create unique file name
                    string fileName = Guid.NewGuid() + Path.GetExtension(categoryImageFile.FileName);
                    string filePath = Path.Combine(uploadFolder, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await categoryImageFile.CopyToAsync(stream);
                    }

                    // Save relative path to DB
                    savedImagePath = "/uploads/categories/" + fileName;
                }

                using (var con = new NpgsqlConnection(DbConnection))
                {
                    await con.OpenAsync();

                    // 2. Check Duplicate Category

                    string checkQuery = @"SELECT COUNT(*) FROM categories WHERE LOWER(category_name) = LOWER(@name)";

                    using (var checkCmd = new NpgsqlCommand(checkQuery, con))
                    {
                        checkCmd.Parameters.AddWithValue("@name", categoryName);
                        long exists = (long)await checkCmd.ExecuteScalarAsync();

                        if (exists > 0)
                        {
                            return BadRequest(new
                            {
                                success = false,
                                message = "Category name already exists"
                            });
                        }
                    }


                    // 3. Insert Category

                    string insertQuery = @"
                INSERT INTO categories (category_name, description, is_active, category_image)
                VALUES (@name, @desc, @active, @img)
            ";

                    using (var cmd = new NpgsqlCommand(insertQuery, con))
                    {
                        cmd.Parameters.AddWithValue("@name", categoryName);
                        cmd.Parameters.AddWithValue("@desc", string.IsNullOrEmpty(description) ? DBNull.Value : description);
                        cmd.Parameters.AddWithValue("@active", isActive);
                        cmd.Parameters.AddWithValue("@img", savedImagePath ?? (object)DBNull.Value);

                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                return Ok(new { success = true, message = "Category inserted successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = $"Internal server error! {ex.Message}" });
            }
        }

        public async Task<IActionResult> UpdataCategory(int Id, IFormCollection form)
        {
            try
            {
                if (form == null || form.Count == 0)
                {
                    return BadRequest(new { success = false, message = "Form data is missing" });
                }

                string categoryName = form["categoryName"];
                string description = form["description"];
                bool isActive = form.ContainsKey("isActive") && form["isActive"] == "true";

                if (string.IsNullOrWhiteSpace(categoryName))
                {
                    return BadRequest(new { success = false, message = "Category name is required" });
                }

                // IMAGE FILE
                IFormFile categoryImageFile = form.Files["categoryImage"];
                string savedImagePath = null;

                using (var con = new NpgsqlConnection(DbConnection))
                {
                    await con.OpenAsync();

                    // -------------------------------
                    // 1. CHECK IF CATEGORY EXISTS
                    // -------------------------------
                    string fetchQuery = "SELECT category_image FROM categories WHERE id = @Id";
                    string? oldImagePath = null;

                    using (var fetchCmd = new NpgsqlCommand(fetchQuery, con))
                    {
                        fetchCmd.Parameters.AddWithValue("@Id", Id);
                        var reader = await fetchCmd.ExecuteReaderAsync();

                        if (!reader.HasRows)
                        {
                            return NotFound(new { success = false, message = "Category not found!" });
                        }

                        if (await reader.ReadAsync())
                        {
                            oldImagePath = reader["category_image"]?.ToString();
                        }

                        reader.Close();
                    }

                    // -------------------------------
                    // 2. UPLOAD IMAGE IF PROVIDED
                    // -------------------------------
                    if (categoryImageFile != null && categoryImageFile.Length > 0)
                    {
                        string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/categoryImages");

                        if (!Directory.Exists(uploadsFolder))
                            Directory.CreateDirectory(uploadsFolder);

                        string uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(categoryImageFile.FileName);
                        string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await categoryImageFile.CopyToAsync(stream);
                        }

                        savedImagePath = $"/categoryImages/{uniqueFileName}";

                        // OPTIONAL: DELETE OLD IMAGE
                        /*
                        if (!string.IsNullOrEmpty(oldImagePath))
                        {
                            var oldImageFullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot" + oldImagePath);
                            if (System.IO.File.Exists(oldImageFullPath))
                                System.IO.File.Delete(oldImageFullPath);
                        }
                        */
                    }
                    else
                    {
                        // Keep old image if new file not provided
                        savedImagePath = oldImagePath;
                    }

                    // -------------------------------
                    // 3. UPDATE QUERY
                    // -------------------------------
                    string updateQuery = @"
                UPDATE categories SET
                    category_name = @CategoryName,
                    description = @Description,
                    category_image = @CategoryImage,
                    is_active = @IsActive,
                    updated_at = NOW()
                WHERE id = @Id;
            ";

                    using (var cmd = new NpgsqlCommand(updateQuery, con))
                    {
                        cmd.Parameters.AddWithValue("@Id", Id);
                        cmd.Parameters.AddWithValue("@CategoryName", categoryName);
                        cmd.Parameters.AddWithValue("@Description",
                            string.IsNullOrWhiteSpace(description) ? (object)DBNull.Value : description);
                        cmd.Parameters.AddWithValue("@CategoryImage",
                            string.IsNullOrWhiteSpace(savedImagePath) ? (object)DBNull.Value : savedImagePath);
                        cmd.Parameters.AddWithValue("@IsActive", isActive);

                        int rows = await cmd.ExecuteNonQueryAsync();

                        if (rows > 0)
                        {
                            return Ok(new { success = true, message = "Category updated successfully!" });
                        }
                        else
                        {
                            return BadRequest(new { success = false, message = "Category update failed!" });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Internal server error! {ex.Message}" });
            }
        }




    }
}
