using CareerCracker.S3Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace CareerCracker.DataBaseLayer
{
    public interface IDataBaseLayer_Testimonial
    {
        Task<IActionResult> AddTestimonial(IFormCollection form);
        Task<IActionResult> GetAllTestimonials();
        Task<IActionResult> GetTestimonialById(int id);
        Task<IActionResult> UpdateTestimonial(int id, IFormCollection form);
        Task<IActionResult> DeleteTestimonial(int id);
        Task<IActionResult> ToggleStatus(int id);
    }

    public partial interface IDataBaseLayer : IDataBaseLayer_Testimonial { }

    public partial class DataBaseLayer
    {
        public async Task<IActionResult> AddTestimonial(IFormCollection form)
        {
            try
            {
                string name = form["test_name"];
                string description = form["discription"];
                string content = form["test_content"];
                string slug = GenerateSlug(name);

                if (string.IsNullOrEmpty(name))
                    return BadRequest(new { success = false, message = "Name is required" });

                // --------------------------------------------
                // 1️⃣ CHECK SLUG UNIQUE
                // --------------------------------------------
                using (var con = new NpgsqlConnection(DbConnection))
                {
                    await con.OpenAsync();

                    string checkSlugQuery = @"SELECT COUNT(*) FROM testimonial WHERE slug LIKE @slugPattern";

                    using (var checkCmd = new NpgsqlCommand(checkSlugQuery, con))
                    {
                        checkCmd.Parameters.AddWithValue("@slugPattern", slug + "%");

                        var count = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());

                        if (count > 0)
                        {
                            slug = $"{slug}-{count + 1}";
                        }
                    }
                }

                // --------------------------------------------
                // ✅ IMAGE UPLOAD TO S3 (UPDATED)
                // --------------------------------------------
                string imagePath = null;

                if (form.Files.Count > 0)
                {
                    var file = form.Files[0];

                    if (file != null && file.Length > 0)
                    {
                        imagePath = await S3StorageHelper.UploadFileAsync(file, "testimonials");

                        if (string.IsNullOrEmpty(imagePath))
                        {
                            return BadRequest(new
                            {
                                success = false,
                                message = "Failed to upload testimonial image"
                            });
                        }
                    }
                }

                // --------------------------------------------
                // 2️⃣ INSERT DATA
                // --------------------------------------------
                using (var con = new NpgsqlConnection(DbConnection))
                {
                    await con.OpenAsync();

                    string query = @"
            INSERT INTO testimonial(test_name, discription, test_content, slug, image, is_active)
            VALUES(@name, @desc, @content, @slug, @image, TRUE)
            RETURNING id";

                    using (var cmd = new NpgsqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@name", name);
                        cmd.Parameters.AddWithValue("@desc", (object)description ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@content", (object)content ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@slug", slug);
                        cmd.Parameters.AddWithValue("@image", (object)imagePath ?? DBNull.Value);

                        var insertedId = await cmd.ExecuteScalarAsync();

                        return Ok(new
                        {
                            success = true,
                            id = insertedId,
                            slug = slug,
                            image = imagePath, // ✅ return image URL
                            message = "Testimonial added successfully"
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        public async Task<IActionResult> GetAllTestimonials()
            {
            try
            {
                using (var con = new NpgsqlConnection(DbConnection))
                {
                    await con.OpenAsync();

                    string query = @"SELECT * FROM testimonial ORDER BY id DESC";

                    using (var cmd = new NpgsqlCommand(query, con))
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        var list = new List<object>();

                        while (await reader.ReadAsync())
                        {
                            list.Add(new
                            {
                                id = reader["id"],
                                test_name = reader["test_name"],
                                discription = reader["discription"],
                                test_content = reader["test_content"],
                                slug = reader["slug"],
                                image = reader["image"],
                                is_active = reader["is_active"],
                                updated_at = reader["updated_at"]
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

        public async Task<IActionResult> GetTestimonialById(int id)
        {
            try
            {
                using (var con = new NpgsqlConnection(DbConnection))
                {
                    await con.OpenAsync();

                    string query = @"SELECT * FROM testimonial WHERE id=@id";

                    using (var cmd = new NpgsqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@id", id);

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (!reader.Read())
                                return NotFound(new { success = false, message = "Testimonial not found" });

                            var result = new
                            {
                                id = reader["id"],
                                test_name = reader["test_name"],
                                discription = reader["discription"],
                                test_content = reader["test_content"],
                                slug = reader["slug"],
                                image = reader["image"],
                                is_active = reader["is_active"],
                                updated_at = reader["updated_at"]
                            };

                            return Ok(new { success = true, data = result });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        public async Task<IActionResult> UpdateTestimonial(int id, IFormCollection form)
        {
            try
            {
                string name = form["test_name"];
                string description = form["discription"];
                string content = form["test_content"];
                string newSlug = GenerateSlug(name);

                if (string.IsNullOrEmpty(name))
                    return BadRequest(new { success = false, message = "Name is required" });

                using (var con = new NpgsqlConnection(DbConnection))
                {
                    await con.OpenAsync();

                    // -------------------------------------------------
                    // 🔍 1️⃣ CHECK SLUG UNIQUE
                    // -------------------------------------------------
                    string checkSlugQuery = @"SELECT COUNT(*) FROM testimonial 
                                     WHERE slug=@slug AND id<>@id";

                    using (var checkCmd = new NpgsqlCommand(checkSlugQuery, con))
                    {
                        checkCmd.Parameters.AddWithValue("@slug", newSlug);
                        checkCmd.Parameters.AddWithValue("@id", id);

                        long exists = (long)await checkCmd.ExecuteScalarAsync();

                        if (exists > 0)
                        {
                            newSlug += "-" + Guid.NewGuid().ToString().Substring(0, 8);
                        }
                    }

                    // -------------------------------------------------
                    // 📥 2️⃣ GET OLD IMAGE (OPTIONAL - for delete)
                    // -------------------------------------------------
                    string oldImage = null;
                    string getOldQuery = "SELECT image FROM testimonial WHERE id=@id";

                    using (var oldCmd = new NpgsqlCommand(getOldQuery, con))
                    {
                        oldCmd.Parameters.AddWithValue("@id", id);
                        var result = await oldCmd.ExecuteScalarAsync();
                        oldImage = result?.ToString();
                    }

                    // -------------------------------------------------
                    // ✅ 3️⃣ IMAGE UPLOAD TO S3
                    // -------------------------------------------------
                    string imagePath = null;

                    if (form.Files.Count > 0)
                    {
                        var file = form.Files[0];

                        if (file != null && file.Length > 0)
                        {
                            imagePath = await S3StorageHelper.UploadFileAsync(file, "testimonials");

                            if (string.IsNullOrEmpty(imagePath))
                            {
                                return BadRequest(new
                                {
                                    success = false,
                                    message = "Failed to upload testimonial image"
                                });
                            }

                            // 🧹 OPTIONAL: Delete old image from S3
                            if (!string.IsNullOrEmpty(oldImage))
                            {
                                await S3StorageHelper.DeleteFileAsync(oldImage);
                            }
                        }
                    }

                    // -------------------------------------------------
                    // 📝 4️⃣ UPDATE DATA
                    // -------------------------------------------------
                    string query = @"
                UPDATE testimonial
                SET test_name=@name,
                    discription=@desc,
                    test_content=@content,
                    slug=@slug,
                    image = COALESCE(@image, image),
                    updated_at = CURRENT_TIMESTAMP
                WHERE id=@id";

                    using (var cmd = new NpgsqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        cmd.Parameters.AddWithValue("@name", name);
                        cmd.Parameters.AddWithValue("@desc", (object)description ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@content", (object)content ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@slug", newSlug);
                        cmd.Parameters.AddWithValue("@image", (object)imagePath ?? DBNull.Value);

                        await cmd.ExecuteNonQueryAsync();
                    }

                    return Ok(new
                    {
                        success = true,
                        image = imagePath, // new image (if updated)
                        message = "Testimonial updated successfully!"
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        public async Task<IActionResult> DeleteTestimonial(int id)
        {
            try
            {
                using (var con = new NpgsqlConnection(DbConnection))
                {
                    await con.OpenAsync();

                    // 1️⃣ Check if exists
                    string checkQuery = "SELECT COUNT(*) FROM testimonial WHERE id=@id";
                    using (var checkCmd = new NpgsqlCommand(checkQuery, con))
                    {
                        checkCmd.Parameters.AddWithValue("@id", id);
                        long count = (long)await checkCmd.ExecuteScalarAsync();

                        if (count == 0)
                        {
                            return NotFound(new { success = false, message = "Testimonial not found!" });
                        }
                    }

                    // 2️⃣ Delete
                    string query = @"DELETE FROM testimonial WHERE id=@id";

                    using (var cmd = new NpgsqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        await cmd.ExecuteNonQueryAsync();
                    }

                    return Ok(new { success = true, message = "Testimonial deleted successfully" });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        public async Task<IActionResult> ToggleStatus(int id)
        {
            try
            {
                using (var con = new NpgsqlConnection(DbConnection))
                {
                    await con.OpenAsync();

                    // 1️⃣ Check if exists
                    string checkQuery = "SELECT is_active FROM testimonial WHERE id=@id";
                    using (var checkCmd = new NpgsqlCommand(checkQuery, con))
                    {
                        checkCmd.Parameters.AddWithValue("@id", id);

                        var existingStatus = await checkCmd.ExecuteScalarAsync();

                        if (existingStatus == null)
                        {
                            return NotFound(new { success = false, message = "Testimonial not found!" });
                        }
                    }

                    // 2️⃣ Toggle Status
                    string query = @"
                UPDATE testimonial
                SET is_active = NOT is_active,
                    updated_at = CURRENT_TIMESTAMP
                WHERE id=@id
                RETURNING is_active";

                    using (var cmd = new NpgsqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@id", id);

                        var newStatus = await cmd.ExecuteScalarAsync();

                        return Ok(new
                        {
                            success = true,
                            message = "Status updated successfully",
                            new_status = newStatus
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

