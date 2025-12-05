using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace CareerCracker.DataBaseLayer
{
    public interface IDataBaseLayer_Category
    {
        Task<IActionResult> InserCategory(IFormCollection form);
        Task<IActionResult> UpdataCategory(int Id, IFormCollection form);
        Task<IActionResult> GetAllCategory();
        Task<IActionResult> GetCategoryById(int Id);
        Task<IActionResult> DeleteCategoryById(int Id);
        Task<IActionResult> ToggleCategoryStatus(int Id); 


    }

    public partial interface IDataBaseLayer : IDataBaseLayer_Category { }

    public partial class DataBaseLayer : ControllerBase
    {
        public async Task<IActionResult> InserCategory(IFormCollection form)
        {
            try
            {
                if (form == null || form.Count == 0)
                    return BadRequest(new { success = false, message = "Form data is missing" });

                string categoryName = form["categoryName"];
                string categorySlug = form["categorySlug"];
                string description = form["description"];
                bool isActive = form.ContainsKey("isActive") && form["isActive"] == "true";

                IFormFile categoryImageFile = form.Files["categoryImage"];
                string savedImagePath = null;

                if (string.IsNullOrWhiteSpace(categoryName))
                    return BadRequest(new { success = false, message = "Category name is required" });

                if (string.IsNullOrWhiteSpace(categorySlug))
                    return BadRequest(new { success = false, message = "Category slug is required" });

                // Save Image
                if (categoryImageFile != null && categoryImageFile.Length > 0)
                {
                    string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/categoryImages");
                    if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                    string uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(categoryImageFile.FileName);
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                        await categoryImageFile.CopyToAsync(stream);

                    savedImagePath = $"/categoryImages/{uniqueFileName}";
                }

                using (var con = new NpgsqlConnection(DbConnection))
                {
                    await con.OpenAsync();

                    // Check Duplicate Name
                    string checkNameQuery = @"SELECT COUNT(*) FROM categories WHERE LOWER(category_name) = LOWER(@name)";
                    using (var checkNameCmd = new NpgsqlCommand(checkNameQuery, con))
                    {
                        checkNameCmd.Parameters.AddWithValue("@name", categoryName);
                        long exists = (long)await checkNameCmd.ExecuteScalarAsync();
                        if (exists > 0)
                            return BadRequest(new { success = false, message = "Category name already exists" });
                    }

                    // Check Duplicate Slug
                    string checkSlugQuery = @"SELECT COUNT(*) FROM categories WHERE LOWER(category_slug) = LOWER(@slug)";
                    using (var checkSlugCmd = new NpgsqlCommand(checkSlugQuery, con))
                    {
                        checkSlugCmd.Parameters.AddWithValue("@slug", categorySlug);
                        long slugExists = (long)await checkSlugCmd.ExecuteScalarAsync();
                        if (slugExists > 0)
                            return BadRequest(new { success = false, message = "Category slug already exists" });
                    }

                    // Insert
                    string insertQuery = @"
                INSERT INTO categories 
                (category_name, category_discription, category_slug, is_active, category_image, updated_at)
                VALUES (@name, @desc, @slug, @active, @img, CURRENT_TIMESTAMP)";

                    using (var cmd = new NpgsqlCommand(insertQuery, con))
                    {
                        cmd.Parameters.AddWithValue("@name", categoryName);
                        cmd.Parameters.AddWithValue("@desc", string.IsNullOrEmpty(description) ? (object)DBNull.Value : description);
                        cmd.Parameters.AddWithValue("@slug", categorySlug);
                        cmd.Parameters.AddWithValue("@active", isActive);
                        cmd.Parameters.AddWithValue("@img", string.IsNullOrEmpty(savedImagePath) ? (object)DBNull.Value : savedImagePath);

                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                return Ok(new { success = true, message = "Category inserted successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = $"Internal server error: {ex.Message}" });
            }
        }



        public async Task<IActionResult> UpdataCategory(int Id, IFormCollection form)
        {
            try
            {
                if (form == null || form.Count == 0)
                    return BadRequest(new { success = false, message = "Form data is missing" });

                string categoryName = form["categoryName"];
                string categorySlug = form["categorySlug"];
                string description = form["description"];
                bool isActive = form.ContainsKey("isActive") && form["isActive"] == "true";

                if (string.IsNullOrWhiteSpace(categoryName))
                    return BadRequest(new { success = false, message = "Category name is required" });

                if (string.IsNullOrWhiteSpace(categorySlug))
                    return BadRequest(new { success = false, message = "Category slug is required" });

                IFormFile categoryImageFile = form.Files["categoryImage"];
                string? savedImagePath = null;
                string? oldImagePath = null;

                using (var con = new NpgsqlConnection(DbConnection))
                {
                    await con.OpenAsync();

                    // Fetch old image path
                    string fetchQuery = "SELECT category_image FROM categories WHERE id = @Id";
                    using (var fetchCmd = new NpgsqlCommand(fetchQuery, con))
                    {
                        fetchCmd.Parameters.AddWithValue("@Id", Id);
                        var reader = await fetchCmd.ExecuteReaderAsync();

                        if (!reader.HasRows)
                            return NotFound(new { success = false, message = "Category not found!" });

                        if (await reader.ReadAsync())
                            oldImagePath = reader["category_image"]?.ToString();

                        reader.Close();
                    }

                    // Upload new image
                    if (categoryImageFile != null && categoryImageFile.Length > 0)
                    {
                        string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/categoryImages");
                        if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                        string uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(categoryImageFile.FileName);
                        string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                            await categoryImageFile.CopyToAsync(stream);

                        savedImagePath = $"/categoryImages/{uniqueFileName}";
                    }
                    else
                    {
                        savedImagePath = oldImagePath; // keep old image
                    }

                    // Check duplicate name (exclude current)
                    string checkNameQuery = @"SELECT COUNT(*) FROM categories WHERE LOWER(category_name) = LOWER(@name) AND id <> @Id";
                    using (var checkNameCmd = new NpgsqlCommand(checkNameQuery, con))
                    {
                        checkNameCmd.Parameters.AddWithValue("@name", categoryName);
                        checkNameCmd.Parameters.AddWithValue("@Id", Id);

                        long exists = (long)await checkNameCmd.ExecuteScalarAsync();
                        if (exists > 0)
                            return BadRequest(new { success = false, message = "Category name already exists" });
                    }

                    // Check duplicate slug (exclude current)
                    string checkSlugQuery = @"SELECT COUNT(*) FROM categories WHERE LOWER(category_slug) = LOWER(@slug) AND id <> @Id";
                    using (var checkSlugCmd = new NpgsqlCommand(checkSlugQuery, con))
                    {
                        checkSlugCmd.Parameters.AddWithValue("@slug", categorySlug);
                        checkSlugCmd.Parameters.AddWithValue("@Id", Id);

                        long slugExists = (long)await checkSlugCmd.ExecuteScalarAsync();
                        if (slugExists > 0)
                            return BadRequest(new { success = false, message = "Category slug already exists" });
                    }

                    // Update category
                    string updateQuery = @"
                UPDATE categories SET
                    category_name = @name,
                    category_discription = @desc,
                    category_slug = @slug,
                    category_image = @img,
                    is_active = @active,
                    updated_at = CURRENT_TIMESTAMP
                WHERE id = @Id";

                    using (var cmd = new NpgsqlCommand(updateQuery, con))
                    {
                        cmd.Parameters.AddWithValue("@Id", Id);
                        cmd.Parameters.AddWithValue("@name", categoryName);
                        cmd.Parameters.AddWithValue("@desc", string.IsNullOrEmpty(description) ? (object)DBNull.Value : description);
                        cmd.Parameters.AddWithValue("@slug", categorySlug);
                        cmd.Parameters.AddWithValue("@img", string.IsNullOrEmpty(savedImagePath) ? (object)DBNull.Value : savedImagePath);
                        cmd.Parameters.AddWithValue("@active", isActive);

                        int rows = await cmd.ExecuteNonQueryAsync();
                        if (rows > 0)
                            return Ok(new { success = true, message = "Category updated successfully" });
                        else
                            return BadRequest(new { success = false, message = "Category update failed" });
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Internal server error: {ex.Message}" });
            }
        }




        public async Task<IActionResult> GetAllCategory()
        {
            try
            {
                using (var con = new NpgsqlConnection(DbConnection))
                {
                    await con.OpenAsync();

                    string query = @"
                SELECT id, category_name, category_discription, category_slug, category_image, is_active, updated_at 
                FROM categories 
                ORDER BY id DESC";

                    using (var cmd = new NpgsqlCommand(query, con))
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        var list = new List<object>();

                        while (await reader.ReadAsync())
                        {
                            list.Add(new
                            {
                                id = reader["id"],
                                categoryName = reader["category_name"],
                                categorySlug = reader["category_slug"],       // include slug
                                description = reader["category_discription"], // corrected column name
                                image = reader["category_image"],
                                isActive = reader["is_active"],
                                updatedAt = reader["updated_at"]
                            });
                        }

                        return Ok(new
                        {
                            success = true,
                            message = "Categories fetched successfully",
                            data = list
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Internal server error: {ex.Message}" });
            }
        }


        public async Task<IActionResult> GetCategoryById(int Id)
        {
            try
            {
                using (var con = new NpgsqlConnection(DbConnection))
                {
                    await con.OpenAsync();

                    string query = @"
                SELECT id, category_name, category_discription, category_slug, category_image, is_active, updated_at 
                FROM categories 
                WHERE id = @Id";

                    using (var cmd = new NpgsqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@Id", Id);

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (!reader.HasRows)
                                return NotFound(new { success = false, message = "Category not found!" });

                            await reader.ReadAsync();

                            var category = new
                            {
                                id = reader["id"],
                                categoryName = reader["category_name"],
                                categorySlug = reader["category_slug"],       // include slug
                                description = reader["category_discription"], // corrected column name
                                image = reader["category_image"],
                                isActive = reader["is_active"],
                                updatedAt = reader["updated_at"]
                            };

                            return Ok(new
                            {
                                success = true,
                                message = "Category fetched successfully",
                                data = category
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Internal server error: {ex.Message}" });
            }
        }


        public async Task<IActionResult> DeleteCategoryById(int Id)
        {
            try
            {
                using (var con = new NpgsqlConnection(DbConnection))
                {
                    await con.OpenAsync();

                    // ------------------------------
                    // 1. Check if category exists
                    // ------------------------------
                    string fetchQuery = @"SELECT category_image FROM categories WHERE id = @Id";

                    string? imagePath = null;

                    using (var cmd = new NpgsqlCommand(fetchQuery, con))
                    {
                        cmd.Parameters.AddWithValue("@Id", Id);

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (!reader.HasRows)
                            {
                                return NotFound(new
                                {
                                    success = false,
                                    message = "Category not found!"
                                });
                            }

                            await reader.ReadAsync();
                            imagePath = reader["category_image"]?.ToString();

                            reader.Close();
                        }
                    }

                    // ------------------------------
                    // 2. Delete category from database
                    // ------------------------------
                    string deleteQuery = @"DELETE FROM categories WHERE id = @Id";

                    using (var delCmd = new NpgsqlCommand(deleteQuery, con))
                    {
                        delCmd.Parameters.AddWithValue("@Id", Id);

                        int rows = await delCmd.ExecuteNonQueryAsync();

                        if (rows == 0)
                        {
                            return BadRequest(new { success = false, message = "Failed to delete category!" });
                        }
                    }

                    // ------------------------------
                    // 3. Delete image from server (optional)
                    // ------------------------------
                    if (!string.IsNullOrEmpty(imagePath))
                    {
                        try
                        {
                            string fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", imagePath.TrimStart('/'));

                            if (System.IO.File.Exists(fullPath))
                            {
                                System.IO.File.Delete(fullPath);
                            }
                        }
                        catch
                        {
                            // Ignore if unable to delete image
                        }
                    }

                    return Ok(new
                    {
                        success = true,
                        message = "Category deleted successfully!"
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = $"Internal server error: {ex.Message}"
                });
            }
        }

        public async Task<IActionResult> ToggleCategoryStatus(int Id)
        {
            try
            {
                using (var con = new NpgsqlConnection(DbConnection))
                {
                    await con.OpenAsync();

                    string query = @"
                UPDATE categories 
                SET is_active = NOT is_active, updated_at = NOW()
                WHERE id = @Id
                RETURNING is_active;
            ";

                    using (var cmd = new NpgsqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@Id", Id);

                        var result = await cmd.ExecuteScalarAsync();

                        if (result == null)
                        {
                            return NotFound(new { success = false, message = "Category not found!" });
                        }

                        bool newStatus = (bool)result;

                        return Ok(new
                        {
                            success = true,
                            message = newStatus
                                ? "Category activated successfully!"
                                : "Category deactivated successfully!",
                            isActive = newStatus
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = $"Internal server error: {ex.Message}"
                });
            }
        }



    }
}
