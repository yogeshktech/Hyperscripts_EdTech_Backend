using CareerCracker.Models;
using CareerCracker.S3Services;
using Humanizer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.Elfie.Diagnostics;
using Microsoft.EntityFrameworkCore;
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

        Task<bool> BlogExists(int blogId);

        // Identity
        Task<string?> GetUserIdByEmail(string email);

        // Blog Comment
        Task<IActionResult> BlogAddComment(
            int blogId,
            string userId,
            AddCommentDto dto);
        Task<IActionResult> GetCommentsByBlogId(int blogId);




    }
    public partial interface IDataBaseLayer : IDataBaseLayer_Blogs {
    
    }

    public partial class DataBaseLayer
    {
        //public async Task<IActionResult> AddBlogs(IFormCollection form)
        //{
        //    try
        //    {
        //        string name = form["blogName"];
        //        string slug = GenerateSlug(name);
        //        string blogDescription = form["blogDescription"];
        //        bool isActive = form.ContainsKey("is_active") && form["is_active"] == "true";

        //        if (string.IsNullOrEmpty(name))
        //        {
        //            return BadRequest(new { success = false, message = "Blog name is required!" });
        //        }

        //        // -------------------------
        //        // IMAGE UPLOAD
        //        // -------------------------
        //        string savedImagePath = null;
        //        IFormFile blogImageFile = form.Files["blogImage"];

        //        if (blogImageFile != null && blogImageFile.Length > 0)
        //        {
        //            string uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/blogs");
        //            if (!Directory.Exists(uploadFolder)) Directory.CreateDirectory(uploadFolder);

        //            string fileName = Guid.NewGuid().ToString() + Path.GetExtension(blogImageFile.FileName);
        //            string savePath = Path.Combine(uploadFolder, fileName);

        //            using (var stream = new FileStream(savePath, FileMode.Create))
        //            {
        //                await blogImageFile.CopyToAsync(stream);
        //            }

        //            savedImagePath = "/uploads/blogs/" + fileName;
        //        }

        //        using (var conn = new NpgsqlConnection(DbConnection))
        //        {
        //            await conn.OpenAsync();

        //            // -------------------------
        //            // Check for duplicate name
        //            // -------------------------
        //            string checkQuery = @"SELECT COUNT(*) FROM blogs WHERE LOWER(blogs_name) = LOWER(@name)";
        //            using (var checkCmd = new NpgsqlCommand(checkQuery, conn))
        //            {
        //                checkCmd.Parameters.AddWithValue("@name", name);
        //                long exists = (long)await checkCmd.ExecuteScalarAsync();

        //                if (exists > 0)
        //                {
        //                    return Conflict(new
        //                    {
        //                        success = false,
        //                        message = "Blog name already exists"
        //                    });
        //                }
        //            }

        //            // -------------------------
        //            // Insert blog record
        //            // -------------------------
        //            string insertQuery = @"
        //        INSERT INTO blogs 
        //        (blogs_name, blogs_discription, blogs_slug, blogs_image, is_active, updated_at)
        //        VALUES 
        //        (@name, @desc, @slug, @image, @active, CURRENT_TIMESTAMP)
        //        RETURNING id";

        //            using (var insertCmd = new NpgsqlCommand(insertQuery, conn))
        //            {
        //                insertCmd.Parameters.AddWithValue("@name", name);
        //                insertCmd.Parameters.AddWithValue("@desc", (object)blogDescription ?? DBNull.Value);
        //                insertCmd.Parameters.AddWithValue("@slug", slug);
        //                insertCmd.Parameters.AddWithValue("@image", (object)savedImagePath ?? DBNull.Value);
        //                insertCmd.Parameters.AddWithValue("@active", isActive);

        //                int newId = (int)await insertCmd.ExecuteScalarAsync();

        //                return Ok(new
        //                {
        //                    success = true,
        //                    message = "Blog added successfully",
        //                    id = newId,
        //                    image = savedImagePath
        //                });
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        return BadRequest(new { success = false, message = ex.Message });
        //    }
        //}


        public async Task<IActionResult> AddBlogs(IFormCollection form)
        {
            try
            {
                // Safe conversion from StringValues to string
                string name = form["blogName"].ToString().Trim();
                string blogDescription = form["blogDescription"].ToString().Trim();
                bool isActive = form.ContainsKey("is_active") &&
                                form["is_active"].ToString().Equals("true", StringComparison.OrdinalIgnoreCase);

                if (string.IsNullOrWhiteSpace(name))
                {
                    return BadRequest(new { success = false, message = "Blog name is required!" });
                }

                string slug = GenerateSlug(name);

                // ====================== IMAGE UPLOAD TO MINIO/S3 ======================
                string? savedImageUrl = null;
                IFormFile? blogImageFile = form.Files["blogImage"];

                if (blogImageFile != null && blogImageFile.Length > 0)
                {
                    savedImageUrl = await S3StorageHelper.UploadFileAsync(blogImageFile, "blogs");

                    if (string.IsNullOrEmpty(savedImageUrl))
                    {
                        return BadRequest(new { success = false, message = "Failed to upload blog image" });
                    }
                }

                using var conn = new NpgsqlConnection(DbConnection);
                await conn.OpenAsync();

                // Check duplicate blog name
                string checkQuery = @"SELECT COUNT(*) FROM blogs WHERE LOWER(blogs_name) = LOWER(@name)";
                using (var checkCmd = new NpgsqlCommand(checkQuery, conn))
                {
                    checkCmd.Parameters.AddWithValue("@name", name);
                    long count = (long)await checkCmd.ExecuteScalarAsync();

                    if (count > 0)
                    {
                        return Conflict(new { success = false, message = "Blog name already exists" });
                    }
                }

                // Insert Blog
                string insertQuery = @"
            INSERT INTO blogs 
            (blogs_name, blogs_discription, blogs_slug, blogs_image, is_active, updated_at)
            VALUES 
            (@name, @desc, @slug, @image, @active, CURRENT_TIMESTAMP)
            RETURNING id";

                using var insertCmd = new NpgsqlCommand(insertQuery, conn);

                insertCmd.Parameters.AddWithValue("@name", name);
                insertCmd.Parameters.AddWithValue("@desc", string.IsNullOrWhiteSpace(blogDescription) ? DBNull.Value : blogDescription);
                insertCmd.Parameters.AddWithValue("@slug", slug);
                insertCmd.Parameters.AddWithValue("@image", (object?)savedImageUrl ?? DBNull.Value);
                insertCmd.Parameters.AddWithValue("@active", isActive);

                int newId = (int)await insertCmd.ExecuteScalarAsync();

                return Ok(new
                {
                    success = true,
                    message = "Blog added successfully",
                    blog = new
                    {
                        id = newId,
                        name = name,
                        description = blogDescription,
                        slug = slug,
                        image = S3StorageHelper.ToPublicUrl(savedImageUrl) ?? "",
                        isActive = isActive,
                        updatedAt = DateTime.UtcNow
                    }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
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

                string? newImageUrl = null;
                IFormFile? blogImageFile = form.Files["blogImage"];

                if (blogImageFile != null && blogImageFile.Length > 0)
                {
                    newImageUrl = await S3StorageHelper.UploadFileAsync(blogImageFile, "blogs");
                    if (string.IsNullOrEmpty(newImageUrl))
                    {
                        return BadRequest(new { success = false, message = "Failed to upload blog image" });
                    }
                }

                using (var conn = new NpgsqlConnection(DbConnection))
                {
                    await conn.OpenAsync();

                    string? oldImageUrl = null;
                    string fetchQuery = @"SELECT blogs_image FROM blogs WHERE id = @id";
                    using (var fetchCmd = new NpgsqlCommand(fetchQuery, conn))
                    {
                        fetchCmd.Parameters.AddWithValue("@id", id);
                        using (var reader = await fetchCmd.ExecuteReaderAsync())
                        {
                            if (!await reader.ReadAsync())
                            {
                                return NotFound(new { success = false, message = "Blog not found" });
                            }

                            if (!reader.IsDBNull(0))
                                oldImageUrl = reader.GetString(0);
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
                        updateCmd.Parameters.AddWithValue("@image", (object?)newImageUrl ?? DBNull.Value);
                        updateCmd.Parameters.AddWithValue("@active", isActive);
                        updateCmd.Parameters.AddWithValue("@id", id);

                        await updateCmd.ExecuteNonQueryAsync();

                        if (!string.IsNullOrEmpty(newImageUrl) && !string.IsNullOrEmpty(oldImageUrl))
                            await S3StorageHelper.DeleteByPathAsync(oldImageUrl);

                        return Ok(new
                        {
                            success = true,
                            message = "Blog updated successfully",
                            id = id,
                            image = S3StorageHelper.ToPublicUrl(newImageUrl ?? oldImageUrl)
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
                                image = reader.IsDBNull(4) ? null : S3StorageHelper.ToPublicUrl(reader.GetString(4)),
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
                                image = reader.IsDBNull(4) ? null : S3StorageHelper.ToPublicUrl(reader.GetString(4)),
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

                    if (!string.IsNullOrEmpty(imagePath))
                        await S3StorageHelper.DeleteStoredMediaAsync(imagePath);

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


        public async Task<bool> BlogExists(int blogId)
        {
            using var conn = new NpgsqlConnection(DbConnection);
            await conn.OpenAsync();

            string query = "SELECT COUNT(*) FROM blogs WHERE id = @id";
            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@id", blogId);

            var result = (long)await cmd.ExecuteScalarAsync();
            return result > 0;
        }

        // --------------------------
        // Get userId by email
        // --------------------------
        public async Task<string?> GetUserIdByEmail(string email)
        {
            using var conn = new NpgsqlConnection(DbConnection);
            await conn.OpenAsync();

            string query = @"SELECT ""Id"" FROM ""AspNetUsers"" WHERE ""Email"" = @Email";
            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@Email", email);

            var result = await cmd.ExecuteScalarAsync();
            return result?.ToString();
        }



        // --------------------------
        // Add comment / reply
        // --------------------------
        public async Task<IActionResult> BlogAddComment(
            int blogId,
            string userId,
            AddCommentDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Comment))
                return new BadRequestObjectResult("Comment is required");

            using var conn = new NpgsqlConnection(DbConnection);
            await conn.OpenAsync();

            // Insert comment
            string insertQuery = @"
                INSERT INTO blog_comments
                (blog_id, user_id, comment, parent_comment_id, created_at)
                VALUES
                (@blogId, @userId, @comment, @parentCommentId, CURRENT_TIMESTAMP)
                RETURNING id";

            using var cmd = new NpgsqlCommand(insertQuery, conn);
            cmd.Parameters.AddWithValue("@blogId", blogId);
            cmd.Parameters.AddWithValue("@userId", userId);
            cmd.Parameters.AddWithValue("@comment", dto.Comment);
            cmd.Parameters.AddWithValue("@parentCommentId", (object?)dto.ParentCommentId ?? DBNull.Value);

            var newId = (int)await cmd.ExecuteScalarAsync();

            return new OkObjectResult(new
            {
                message = "Comment added successfully",
                commentId = newId
            });
        }


        //public async Task<IActionResult> GetCommentsByBlogId(int blogId)
        //{
        //    using var conn = new NpgsqlConnection(DbConnection);
        //    await conn.OpenAsync();

        //    // Check if blog exists
        //    string blogExistsQuery = @"SELECT COUNT(*) FROM blogs WHERE id = @blogId";
        //    using (var cmdCheck = new NpgsqlCommand(blogExistsQuery, conn))
        //    {
        //        cmdCheck.Parameters.AddWithValue("@blogId", blogId);
        //        long exists = (long)await cmdCheck.ExecuteScalarAsync();
        //        if (exists == 0)
        //            return new NotFoundObjectResult("Blog not found");
        //    }

        //    // Select comments
        //    string query = @"
        //SELECT bc.id, bc.comment, bc.parent_comment_id, bc.created_at,
        //       u.""Id"" as userId, u.""UserName"" as userName, u.""Email"" as email
        //FROM blog_comments bc
        //JOIN ""AspNetUsers"" u ON u.""Id"" = bc.user_id
        //WHERE bc.blog_id = @blogId
        //ORDER BY bc.created_at ASC";

        //    using var cmd = new NpgsqlCommand(query, conn);
        //    cmd.Parameters.AddWithValue("@blogId", blogId);

        //    var comments = new List<object>();

        //    using var reader = await cmd.ExecuteReaderAsync();
        //    while (await reader.ReadAsync())
        //    {
        //        comments.Add(new
        //        {
        //            commentId = reader.GetInt32(0),
        //            comment = reader.GetString(1),
        //            parentCommentId = reader.IsDBNull(2) ? (int?)null : reader.GetInt32(2),
        //            createdAt = reader.GetDateTime(3),
        //            user = new
        //            {
        //                id = reader.GetString(4),
        //                userName = reader.GetString(5),
        //                email = reader.GetString(6)
        //            }
        //        });
        //    }

        //    return new OkObjectResult(new
        //    {
        //        success = true,
        //        data = comments
        //    });
        //}


        public async Task<IActionResult> GetCommentsByBlogId(int blogId)
        {
            using var conn = new NpgsqlConnection(DbConnection);
            await conn.OpenAsync();

            string query = @"
        SELECT 
            bc.id,
            bc.comment,
            bc.parent_comment_id,
            bc.created_at,
            u.""Id"",
            u.""UserName"",
            u.""Email""
        FROM blog_comments bc
        JOIN ""AspNetUsers"" u ON u.""Id"" = bc.user_id
        WHERE bc.blog_id = @blogId
        ORDER BY bc.created_at ASC";

            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@blogId", blogId);

            var flatList = new List<(int Id, string Comment, int? ParentId, DateTime CreatedAt, UserDto User)>();

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                flatList.Add((
                    reader.GetInt32(0),
                    reader.GetString(1),
                    reader.IsDBNull(2) ? null : reader.GetInt32(2),
                    reader.GetDateTime(3),
                    new UserDto
                    {
                        Id = reader.GetString(4),
                        UserName = reader.GetString(5),
                        Email = reader.GetString(6)
                    }
                ));
            }

            // 🔁 BUILD NESTED STRUCTURE
            var lookup = flatList.ToDictionary(
                x => x.Id,
                x => new BlogCommentResponse
                {
                    CommentId = x.Id,
                    Comment = x.Comment,
                    CreatedAt = x.CreatedAt,
                    User = x.User
                });

            var result = new List<BlogCommentResponse>();

            foreach (var item in flatList)
            {
                if (item.ParentId == null)
                {
                    result.Add(lookup[item.Id]);
                }
                else if (lookup.ContainsKey(item.ParentId.Value))
                {
                    lookup[item.ParentId.Value].Replies.Add(lookup[item.Id]);
                }
            }

            return new OkObjectResult(new
            {
                success = true,
                data = result
            });
        }

    }
}
