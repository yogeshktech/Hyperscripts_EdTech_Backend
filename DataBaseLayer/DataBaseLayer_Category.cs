using CareerCracker.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using System.Text.RegularExpressions;
using System.Xml.Linq;

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


        // ------------------------------ sub category functionns --------------------------------------------

        Task<IActionResult> InsertSubCategory(IFormCollection form);
        Task<IActionResult> GetAllSubCategories();
        Task<IActionResult> GetSubCategoryById(int id);
        Task<IActionResult> UpdateSubCategory(int id, IFormCollection form);
        Task<IActionResult> DeleteSubCategory(int id);
        Task<IActionResult> ToggleSubCategoryStatus(int Id);

    }

    public partial interface IDataBaseLayer : IDataBaseLayer_Category
    {
        Task<List<AdminOrderModel>> GetAllAdminOrders();
    }

    public partial class DataBaseLayer : ControllerBase
    {
        public async Task<IActionResult> InserCategory(IFormCollection form)
        {
            try
            {
                if (form == null || form.Count == 0)
                    return BadRequest(new { success = false, message = "Form data is missing" });

                string categoryName = form["categoryName"];
                string categorySlug = GenerateSlug(categoryName); 
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
                string categorySlug = GenerateSlug(categoryName);
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

        // ------------------------------ sub category functions---------------------------------------

        public async Task<IActionResult> InsertSubCategory(IFormCollection form)
        {
            try
            {
                string name = form["subCategoryName"];
                string slug = GenerateSlug(name);  // ⬅ AUTO-SLUG
                string description = form["description"];
                int categoryId = int.Parse(form["categoryId"]);
                bool isActive = form.ContainsKey("isActive") && form["isActive"] == "true";

                var subCategoryImageFile = form.Files["subCategoryImage"];
                string savedImagePath = null;

                using (var con = new NpgsqlConnection(DbConnection))
                {
                    await con.OpenAsync();

                    // 🔍 Check Name Exists
                    string checkNameQuery = @"SELECT COUNT(*) FROM subcategories WHERE sub_category_name = @name";

                    using (var checkNameCmd = new NpgsqlCommand(checkNameQuery, con))
                    {
                        checkNameCmd.Parameters.AddWithValue("@name", name);

                        long nameExists = (long)await checkNameCmd.ExecuteScalarAsync();
                        if (nameExists > 0)
                            return BadRequest(new { success = false, message = "Sub Category Name already exists!" });
                    }

                    // 🔍 Check Slug Exists
                    string checkSlugQuery = @"SELECT COUNT(*) FROM subcategories WHERE sub_category_slug = @slug";

                    using (var checkSlugCmd = new NpgsqlCommand(checkSlugQuery, con))
                    {
                        checkSlugCmd.Parameters.AddWithValue("@slug", slug);

                        long slugExists = (long)await checkSlugCmd.ExecuteScalarAsync();
                        if (slugExists > 0)
                            return BadRequest(new { success = false, message = "Sub Category Slug already exists!" });
                    }

                    // Save Image
                    if (subCategoryImageFile != null && subCategoryImageFile.Length > 0)
                    {
                        string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/subCategoryImages");
                        if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                        string uniqueFileName = Guid.NewGuid() + Path.GetExtension(subCategoryImageFile.FileName);
                        string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                            await subCategoryImageFile.CopyToAsync(stream);

                        savedImagePath = $"/subCategoryImages/{uniqueFileName}";
                    }

                    // Insert Query
                    string query = @"
                INSERT INTO subcategories
                (category_id, sub_category_name, sub_category_slug, description, sub_category_image, is_active, updated_at)
                VALUES
                (@category_id, @name, @slug, @desc, @image, @active, CURRENT_TIMESTAMP)";

                    using (var cmd = new NpgsqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@category_id", categoryId);
                        cmd.Parameters.AddWithValue("@name", name);
                        cmd.Parameters.AddWithValue("@slug", slug);
                        cmd.Parameters.AddWithValue("@desc", description ?? "");
                        cmd.Parameters.AddWithValue("@image", (object?)savedImagePath ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@active", isActive);

                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                return Ok(new { success = true, message = "Sub Category Added Successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        public async Task<IActionResult> GetAllSubCategories()
        {
            try
            {
                using (var con = new NpgsqlConnection(DbConnection))
                {
                    await con.OpenAsync();

                    string query = @"
            SELECT s.id, s.sub_category_name, s.sub_category_slug,
                   s.description, s.sub_category_image,
                   s.is_active, s.updated_at,
                   c.category_name,c.category_discription,
                   c.category_slug,c.category_image,
                   c.is_active
            FROM subcategories s
            LEFT JOIN categories c ON s.category_id = c.id
            ORDER BY s.id DESC";

                    using (var cmd = new NpgsqlCommand(query, con))
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        var list = new List<object>();
                        while (await reader.ReadAsync())
                        {
                            list.Add(new
                            {
                                id = reader["id"],
                                subCategoryName = reader["sub_category_name"],
                                slug = reader["sub_category_slug"],
                                description = reader["description"],
                                image = reader["sub_category_image"],
                                isActive = reader["is_active"],
                                updatedAt = reader["updated_at"],
                                categoryName = reader["category_name"],
                                categoryDescription = reader["category_discription"],
                                categorySlug = reader["category_slug"],
                                categoryImage = reader["category_image"],
                                categoryStatus = reader["is_active"]
                            });
                        }
                        return Ok(new { success = true, data = list });
                    }
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        public async Task<IActionResult> GetSubCategoryById(int id)
        {
            try
            {
                using (var con = new NpgsqlConnection(DbConnection))
                {
                    await con.OpenAsync();

                    string query = @"
                SELECT 
                    s.id,
                    s.category_id,
                    s.sub_category_name,
                    s.sub_category_slug,
                    s.description,
                    s.sub_category_image,
                    s.is_active AS sub_active,
                    s.updated_at,

                    c.category_name,
                    c.category_discription,
                    c.category_slug,
                    c.category_image,
                    c.is_active AS category_active
                FROM subcategories s
                LEFT JOIN categories c 
                    ON s.category_id = c.id
                WHERE s.id = @id";

                    using (var cmd = new NpgsqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@id", id);

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (!await reader.ReadAsync())
                                return NotFound(new { success = false, message = "Sub Category Not Found" });

                            return Ok(new
                            {
                                id = reader["id"],
                                categoryId = reader["category_id"],
                                name = reader["sub_category_name"],
                                slug = reader["sub_category_slug"],
                                description = reader["description"],
                                image = reader["sub_category_image"],
                                active = reader["sub_active"],
                                updatedAt = reader["updated_at"],

                                categoryName = reader["category_name"],
                                categoryDescription = reader["category_discription"],
                                categorySlug = reader["category_slug"],
                                categoryImage = reader["category_image"],
                                categoryStatus = reader["category_active"]
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        public async Task<IActionResult> UpdateSubCategory(int id, IFormCollection form)
        {
            try
            {
                string name = form["subCategoryName"];
                string slug = GenerateSlug(name);   // ⬅ AUTO-SLUG
                string description = form["description"];
                int categoryId = int.Parse(form["categoryId"]);
                bool isActive = form.ContainsKey("isActive") && form["isActive"] == "true";

                var subCategoryImageFile = form.Files["subCategoryImage"];
                string savedImagePath = null;
                string oldImagePath = null;

                using (var con = new NpgsqlConnection(DbConnection))
                {
                    await con.OpenAsync();

                    // 🔍 Check Name Exists (ignore current id)
                    //string checkNameQuery = @"SELECT COUNT(*) FROM subcategories 
                    //                  WHERE sub_category_name = @name AND id != @id";

                    //using (var checkNameCmd = new NpgsqlCommand(checkNameQuery, con))
                    //{
                    //    checkNameCmd.Parameters.AddWithValue("@name", name);
                    //    checkNameCmd.Parameters.AddWithValue("@id", id);

                    //    long nameExists = (long)await checkNameCmd.ExecuteScalarAsync();
                    //    if (nameExists > 0)
                    //        return BadRequest(new { success = false, message = "Sub Category Name already exists!" });
                    //}

                    // Get old image
                    string getOldImageQuery = @"SELECT sub_category_image FROM subcategories WHERE id = @id";

                    using (var getCmd = new NpgsqlCommand(getOldImageQuery, con))
                    {
                        getCmd.Parameters.AddWithValue("@id", id);
                        var result = await getCmd.ExecuteScalarAsync();
                        if (result != null) oldImagePath = result.ToString();
                    }

                    // Upload new image
                    if (subCategoryImageFile != null && subCategoryImageFile.Length > 0)
                    {
                        string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/subCategoryImages");

                        if (!Directory.Exists(uploadsFolder))
                            Directory.CreateDirectory(uploadsFolder);

                        string uniqueFileName = Guid.NewGuid() + Path.GetExtension(subCategoryImageFile.FileName);
                        string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                            await subCategoryImageFile.CopyToAsync(stream);

                        savedImagePath = $"/subCategoryImages/{uniqueFileName}";

                        // Delete old image
                        if (!string.IsNullOrEmpty(oldImagePath))
                        {
                            string oldFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", oldImagePath.TrimStart('/'));
                            if (System.IO.File.Exists(oldFilePath))
                                System.IO.File.Delete(oldFilePath);
                        }
                    }
                    else
                    {
                        savedImagePath = oldImagePath; // keep old
                    }

                    // Update Query
                    string updateQuery = @"
                UPDATE subcategories SET
                    category_id = @categoryId,
                    sub_category_name = @name,
                    sub_category_slug = @slug,
                    description = @desc,
                    sub_category_image = @image,
                    is_active = @active,
                    updated_at = CURRENT_TIMESTAMP
                WHERE id = @id";

                    using (var cmd = new NpgsqlCommand(updateQuery, con))
                    {
                        cmd.Parameters.AddWithValue("@categoryId", categoryId);
                        cmd.Parameters.AddWithValue("@name", name);
                        cmd.Parameters.AddWithValue("@slug", slug);
                        cmd.Parameters.AddWithValue("@desc", description ?? "");
                        cmd.Parameters.AddWithValue("@image", (object?)savedImagePath ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@active", isActive);
                        cmd.Parameters.AddWithValue("@id", id);

                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                return Ok(new { success = true, message = "Sub Category Updated Successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        public async Task<IActionResult> DeleteSubCategory(int id)
        {
            try
            {
                using (var con = new NpgsqlConnection(DbConnection))
                {
                    await con.OpenAsync();

                    string query = "DELETE FROM subcategories WHERE id = @id";

                    using (var cmd = new NpgsqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                return Ok(new { success = true, message = "Sub Category Deleted Successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        public async Task<IActionResult> ToggleSubCategoryStatus(int Id)
        {
            try
            {
                using (var con = new NpgsqlConnection(DbConnection))
                {
                    await con.OpenAsync();

                    string query = @"
                UPDATE subcategories 
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

        private string GenerateSlug(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "";

            // Convert to lower-case
            string slug = name.ToLower();

            // Replace invalid characters with hyphens
            slug = Regex.Replace(slug, @"[^a-z0-9\s-]", "");

            // Replace spaces with hyphens
            slug = Regex.Replace(slug, @"\s+", "-").Trim('-');

            return slug;
        }



    }
}
