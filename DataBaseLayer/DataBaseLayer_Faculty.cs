using CareerCracker.S3Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using NpgsqlTypes;
using System.Collections.Generic;
using System.Data.Common;


namespace CareerCracker.DataBaseLayer
{
    public interface IDataBaseLayer_Faculty
    {
        Task<IActionResult> InsertFaculty(IFormCollection form);
        //Task<IActionResult> UpdateFacultyBySlug(string slug, IFormCollection form);
        Task<IActionResult> UpdateFaculty(string id, IFormCollection form);
        Task<IActionResult> GetAllFaculties();
        Task<IActionResult> GetFacultyBySlug(string slug);
        Task<IActionResult> DeleteFacultyBySlug(string slug);
        Task<IActionResult> ToggleFacultyStatusBySlug(string slug);
        Task<IActionResult> AsignBatch(int batchId, IFormCollection form);
        Task<IActionResult> UpdateAssignedFaculty(int assignId, IFormCollection form);
        Task<IActionResult> GetAssignedFaculty(IFormCollection form);
        Task<IActionResult> GetAssignedFacultyEmail(string userEmail);
        Task<IActionResult> DeleteAssignedFaculty(int facultyAssignId);
        Task<IActionResult> SoftDeleteAssignedFaculty(int facultyAssignId);
    }

    public partial interface IDataBaseLayer : IDataBaseLayer_Faculty { }

    public partial class DataBaseLayer : ControllerBase
    {
        public async Task<IActionResult> InsertFaculty(IFormCollection form)
        {
            try
            {
                string firstName = form["FirstName"];
                string lastName = form["LastName"];
                string email = form["email"];
                string phoneNumber = form["PhoneNumber"];
                string position = form["position"];
                string experience = form["experience"];
                string specialization = form["specialization"];
                string createdBy = form["created_by"];
                string password = form["Password"];

                if (string.IsNullOrWhiteSpace(firstName) ||
                    string.IsNullOrWhiteSpace(email) ||
                    string.IsNullOrWhiteSpace(password))
                {
                    return BadRequest(new { success = false, message = "Required fields missing" });
                }

                using var con = new NpgsqlConnection(DbConnection);
                await con.OpenAsync();

                // 🔹 Check duplicate email
                string emailCheck = @"SELECT 1 FROM ""AspNetUsers"" WHERE LOWER(""Email"") = LOWER(@email)";
                using (var checkCmd = new NpgsqlCommand(emailCheck, con))
                {
                    checkCmd.Parameters.AddWithValue("@email", email);
                    if (await checkCmd.ExecuteScalarAsync() != null)
                        return BadRequest(new { success = false, message = "Email already exists" });
                }

                // 🔹 Get ADMIN RoleId
                string getRoleIdSql = @"
            SELECT ""Id""
            FROM ""AspNetRoles""
            WHERE ""NormalizedName"" = 'ADMIN'";

                string roleId;
                using (var roleCmd = new NpgsqlCommand(getRoleIdSql, con))
                {
                    roleId = (string?)await roleCmd.ExecuteScalarAsync()
                        ?? throw new Exception("ADMIN role not found");
                }

                // 🔹 Hash password
                var hasher = new PasswordHasher<IdentityUser>();
                var tempUser = new IdentityUser { UserName = email, Email = email };
                string passwordHash = hasher.HashPassword(tempUser, password);

                string userId = Guid.NewGuid().ToString(); // TEXT

                // 🔹 Insert user
                string insertUserSql = @"
            INSERT INTO ""AspNetUsers""
            (
                ""Id"",
                ""FirstName"",
                ""LastName"",
                ""UserName"",
                ""NormalizedUserName"",
                ""Email"",
                ""NormalizedEmail"",
                ""PhoneNumber"",
                ""PasswordHash"",
                ""SecurityStamp"",
                ""EmailConfirmed"",
                ""IsActive"",
                ""CreateDate"",
                slug,
                position,
                experience,
                specialization,
                created_by,
                created_at,
                updated_at
            )
            VALUES
            (
                @id,
                @firstName,
                @lastName,
                @userName,
                UPPER(@userName),
                @email,
                UPPER(@email),
                @phone,
                @passwordHash,
                gen_random_uuid()::text,
                TRUE,
                TRUE,
                NOW(),
                @slug,
                @position,
                @experience,
                @specialization,
                @createdBy,
                NOW(),
                NOW()
            )";

                using (var cmd = new NpgsqlCommand(insertUserSql, con))
                {
                    cmd.Parameters.AddWithValue("@id", userId);
                    cmd.Parameters.AddWithValue("@firstName", firstName);
                    cmd.Parameters.AddWithValue("@lastName", (object?)lastName ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@userName", email);
                    cmd.Parameters.AddWithValue("@email", email);
                    cmd.Parameters.AddWithValue("@phone", (object?)phoneNumber ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@passwordHash", passwordHash);
                    cmd.Parameters.AddWithValue("@slug", GenerateSlug($"{firstName} {lastName}"));
                    cmd.Parameters.AddWithValue("@position", (object?)position ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@experience", (object?)experience ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@specialization", (object?)specialization ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@createdBy", (object?)createdBy ?? DBNull.Value);

                    await cmd.ExecuteNonQueryAsync();
                }

                // 🔹 Assign ADMIN role
                string insertUserRoleSql = @"
            INSERT INTO ""AspNetUserRoles""
            (""UserId"", ""RoleId"")
            VALUES
            (@userId, @roleId)";

                using (var roleInsertCmd = new NpgsqlCommand(insertUserRoleSql, con))
                {
                    roleInsertCmd.Parameters.AddWithValue("@userId", userId);
                    roleInsertCmd.Parameters.AddWithValue("@roleId", roleId);

                    await roleInsertCmd.ExecuteNonQueryAsync();
                }

                return Ok(new { success = true, message = "Faculty added and ADMIN role assigned" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        public async Task<IActionResult> UpdateFaculty(string id, IFormCollection form)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(id))
                    return BadRequest(new { success = false, message = "Faculty ID is required" });

                string firstName = form["FirstName"];
                string lastName = form["LastName"];
                string email = form["Email"];
                string phoneNumber = form["PhoneNumber"];
                string position = form["Position"];
                string experience = form["Experience"];
                string specialization = form["Specialization"];

                if (string.IsNullOrWhiteSpace(firstName))
                    return BadRequest(new { success = false, message = "First name is required" });

                IFormFile imageFile = form.Files["profile_image"];
                string? oldImagePath = null;
                string? newImagePath = null;

                using var con = new NpgsqlConnection(DbConnection);
                await con.OpenAsync();

                // 🔹 FETCH EXISTING IMAGE
                string fetchQuery = @"
            SELECT profile_image
            FROM ""AspNetUsers""
            WHERE ""Id"" = @id";

                using (var fetchCmd = new NpgsqlCommand(fetchQuery, con))
                {
                    fetchCmd.Parameters.Add("@id", NpgsqlDbType.Text).Value = id;

                    using var reader = await fetchCmd.ExecuteReaderAsync();
                    if (!reader.Read())
                        return NotFound(new { success = false, message = "Faculty not found" });

                    oldImagePath = reader["profile_image"]?.ToString();
                }

                // 🔹 IMAGE UPLOAD
                // 🔹 IMAGE UPLOAD (S3)
                if (imageFile != null && imageFile.Length > 0)
                {
                    // ✅ Validate file
                    var ext = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
                    var allowedExt = new[] { ".jpg", ".jpeg", ".png", ".webp" };

                    if (!allowedExt.Contains(ext))
                    {
                        return BadRequest(new
                        {
                            success = false,
                            message = "Only JPG, PNG, WEBP images allowed"
                        });
                    }

                    if (imageFile.Length > 2 * 1024 * 1024)
                    {
                        return BadRequest(new
                        {
                            success = false,
                            message = "Image size must be less than 2MB"
                        });
                    }

                    // ✅ Upload to S3
                    newImagePath = await S3StorageHelper.UploadFileAsync(imageFile, "faculty");

                    if (string.IsNullOrEmpty(newImagePath))
                    {
                        return BadRequest(new
                        {
                            success = false,
                            message = "Failed to upload faculty image"
                        });
                    }

                    // ✅ Delete old image from S3 (optional but recommended)
                    if (!string.IsNullOrEmpty(oldImagePath))
                    {
                        await S3StorageHelper.DeleteFileAsync(oldImagePath);
                    }
                }
                else
                {
                    newImagePath = oldImagePath;
                }
                // 🔹 UPDATE
                string updateQuery = @"
            UPDATE ""AspNetUsers""
            SET
                ""FirstName"" = @firstName,
                ""LastName"" = @lastName,
                ""UserName"" = @email,
                ""NormalizedUserName"" = UPPER(@email),
                ""Email"" = @email,
                ""NormalizedEmail"" = UPPER(@email),
                ""PhoneNumber"" = @phoneNumber,
                position = @position,
                experience = @experience,
                specialization = @specialization,
                profile_image = @image,
                updated_at = NOW()
            WHERE ""Id"" = @id";

                using (var cmd = new NpgsqlCommand(updateQuery, con))
                {
                    cmd.Parameters.Add("@id", NpgsqlDbType.Text).Value = id;
                    cmd.Parameters.Add("@firstName", NpgsqlDbType.Text).Value = firstName;
                    cmd.Parameters.Add("@lastName", NpgsqlDbType.Text).Value = (object?)lastName ?? DBNull.Value;
                    cmd.Parameters.Add("@email", NpgsqlDbType.Text).Value = email;
                    cmd.Parameters.Add("@phoneNumber", NpgsqlDbType.Text).Value = (object?)phoneNumber ?? DBNull.Value;
                    cmd.Parameters.Add("@position", NpgsqlDbType.Text).Value = (object?)position ?? DBNull.Value;
                    cmd.Parameters.Add("@experience", NpgsqlDbType.Text).Value = (object?)experience ?? DBNull.Value;
                    cmd.Parameters.Add("@specialization", NpgsqlDbType.Text).Value = (object?)specialization ?? DBNull.Value;
                    cmd.Parameters.Add("@image", NpgsqlDbType.Text).Value = (object?)newImagePath ?? DBNull.Value;

                    await cmd.ExecuteNonQueryAsync();
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
                using var con = new NpgsqlConnection(DbConnection);
                await con.OpenAsync();

                string query = @"
        SELECT 
            u.""Id"",
            u.""FirstName"",
            u.""LastName"",
            u.""Email"",
            u.""PhoneNumber"",
            u.slug,
            u.position,
            u.experience,
            u.specialization,
            u.profile_image,
            u.status,
            u.created_at,
            u.updated_at
        FROM ""AspNetUsers"" u
        INNER JOIN ""AspNetUserRoles"" ur ON ur.""UserId"" = u.""Id""
        INNER JOIN ""AspNetRoles"" r ON r.""Id"" = ur.""RoleId""
        WHERE 
            u.status = TRUE
            AND r.""NormalizedName"" = 'ADMIN'
        ORDER BY u.created_at DESC;
        ";

                using var cmd = new NpgsqlCommand(query, con);
                using var reader = await cmd.ExecuteReaderAsync();

                var list = new List<object>();

                while (await reader.ReadAsync())
                {
                    list.Add(new
                    {
                        id = reader["Id"],
                        name = $"{reader["FirstName"]} {reader["LastName"]}",
                        slug = reader["slug"],
                        email = reader["Email"],
                        phoneNumber = reader["PhoneNumber"],
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
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        public async Task<IActionResult> GetFacultyBySlug(string slug)
        {
            try
            {
                using var con = new NpgsqlConnection(DbConnection);
                await con.OpenAsync();

                string query = @"
            SELECT 
                ""Id"",
                ""FirstName"",
                ""LastName"",
                ""Email"",
                ""PhoneNumber"",
                slug,
                position,
                experience,
                specialization,
                profile_image,
                status,
                created_at,
                updated_at
            FROM ""AspNetUsers""
            WHERE slug = @slug AND status = TRUE
        ";

                using var cmd = new NpgsqlCommand(query, con);
                cmd.Parameters.AddWithValue("@slug", slug);

                using var reader = await cmd.ExecuteReaderAsync();

                if (!reader.HasRows)
                    return NotFound(new { success = false, message = "Faculty not found" });

                await reader.ReadAsync();

                var faculty = new
                {
                    id = reader["Id"],
                    firstName = reader["FirstName"],
                    lastName = reader["LastName"],
                    name = $"{reader["FirstName"]} {reader["LastName"]}",
                    slug = reader["slug"],
                    email = reader["Email"],
                    phoneNumber = reader["PhoneNumber"],
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

                using var con = new NpgsqlConnection(DbConnection);
                await con.OpenAsync();

                // ---------- Fetch Image ----------
                string fetchQuery = @"
            SELECT profile_image 
            FROM ""AspNetUsers"" 
            WHERE slug = @slug
        ";

                using (var fetchCmd = new NpgsqlCommand(fetchQuery, con))
                {
                    fetchCmd.Parameters.AddWithValue("@slug", slug);

                    using var reader = await fetchCmd.ExecuteReaderAsync();

                    if (!reader.HasRows)
                        return NotFound(new { success = false, message = "Faculty not found" });

                    await reader.ReadAsync();
                    imagePath = reader["profile_image"]?.ToString();
                }

                // ---------- Delete User ----------
                string deleteQuery = @"DELETE FROM ""AspNetUsers"" WHERE slug = @slug";

                using (var deleteCmd = new NpgsqlCommand(deleteQuery, con))
                {
                    deleteCmd.Parameters.AddWithValue("@slug", slug);
                    await deleteCmd.ExecuteNonQueryAsync();
                }

                // ---------- Delete Image File ----------
                if (!string.IsNullOrWhiteSpace(imagePath))
                {
                    string fullPath = Path.Combine(
                        Directory.GetCurrentDirectory(),
                        "wwwroot",
                        imagePath.TrimStart('/')
                    );

                    if (System.IO.File.Exists(fullPath))
                        System.IO.File.Delete(fullPath);
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
                using var con = new NpgsqlConnection(DbConnection);
                await con.OpenAsync();

                string query = @"
            UPDATE faculties
            SET status = NOT status,
                updated_at = NOW()
            WHERE slug = @slug
            RETURNING status;
        ";

                using var cmd = new NpgsqlCommand(query, con);
                cmd.Parameters.AddWithValue("@slug", slug);

                var result = await cmd.ExecuteScalarAsync();

                if (result == null)
                    return NotFound(new { success = false, message = "Faculty not found" });

                bool newStatus = Convert.ToBoolean(result);

                return Ok(new
                {
                    success = true,
                    message = newStatus ? "Faculty Activated" : "Faculty Deactivated",
                    status = newStatus
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        public async Task<IActionResult> AsignBatch(int batchId, IFormCollection form)
        {
            try
            {
                using var con = new NpgsqlConnection(DbConnection);
                await con.OpenAsync();

                string userEmail = form["email"];

                if (string.IsNullOrEmpty(userEmail))
                    return BadRequest(new { success = false, message = "User Email required!" });

                // -------------------------------------------------------
                // 1️⃣ Check batch exists
                // -------------------------------------------------------
                string batchCheckQuery = @"SELECT 1 FROM batches WHERE id = @batchId LIMIT 1";
                using (var batchCmd = new NpgsqlCommand(batchCheckQuery, con))
                {
                    batchCmd.Parameters.AddWithValue("@batchId", batchId);
                    var batchExists = await batchCmd.ExecuteScalarAsync();

                    if (batchExists == null)
                    {
                        return BadRequest(new
                        {
                            success = false,
                            message = "Batch does not exist. Please create batch before assigning faculty."
                        });
                    }
                }

                // -------------------------------------------------------
                // 2️⃣ Get faculty ID using email
                // -------------------------------------------------------
                Guid facultyId;

                string userQuery = @"SELECT ""Id"" FROM ""AspNetUsers"" WHERE ""Email"" = @Email LIMIT 1";
                using (var userCmd = new NpgsqlCommand(userQuery, con))
                {
                    userCmd.Parameters.AddWithValue("@Email", userEmail);
                    var result = await userCmd.ExecuteScalarAsync();

                    if (result == null)
                        return BadRequest(new { success = false, message = "Faculty not found!" });

                    facultyId = Guid.Parse(result.ToString()!);
                }

                // -------------------------------------------------------
                // 3️⃣ Check faculty already assigned to batch
                // -------------------------------------------------------
                string existsQuery = @"
            SELECT 1 
            FROM batch_faculties 
            WHERE batch_id = @batchId 
              AND faculties_id = @facultyId
            LIMIT 1";

                using (var existsCmd = new NpgsqlCommand(existsQuery, con))
                {
                    existsCmd.Parameters.AddWithValue("@batchId", batchId);
                    existsCmd.Parameters.AddWithValue("@facultyId", facultyId);

                    if (await existsCmd.ExecuteScalarAsync() != null)
                    {
                        return BadRequest(new
                        {
                            success = false,
                            message = "Faculty is already assigned to this batch."
                        });
                    }
                }

                // -------------------------------------------------------
                // 4️⃣ Assign faculty to batch
                // -------------------------------------------------------
                string insertQuery = @"
            INSERT INTO batch_faculties (batch_id, faculties_id, assigned_at, is_active)
            VALUES (@batchId, @facultyId, NOW(), TRUE)
            RETURNING id;
        ";

                using var cmd = new NpgsqlCommand(insertQuery, con);
                cmd.Parameters.AddWithValue("@batchId", batchId);
                cmd.Parameters.AddWithValue("@facultyId", facultyId);

                int assignId = Convert.ToInt32(await cmd.ExecuteScalarAsync());

                return Ok(new
                {
                    success = true,
                    message = "Faculty assigned to batch successfully",
                    assign_id = assignId
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        public async Task<IActionResult> UpdateAssignedFaculty(int assignId, IFormCollection form)
        {
            try
            {
                using var con = new NpgsqlConnection(DbConnection);
                await con.OpenAsync();

                string userEmail = form["email"];

                if (string.IsNullOrEmpty(userEmail))
                    return BadRequest(new { success = false, message = "Faculty Email required!" });

                // -------------------------------------------------------
                // 1️⃣ Check assignment exists & get batch_id
                // -------------------------------------------------------
                int batchId;
                string assignCheckQuery = @"
            SELECT batch_id 
            FROM batch_faculties 
            WHERE id = @assignId
            LIMIT 1";

                using (var assignCmd = new NpgsqlCommand(assignCheckQuery, con))
                {
                    assignCmd.Parameters.AddWithValue("@assignId", assignId);
                    var result = await assignCmd.ExecuteScalarAsync();

                    if (result == null)
                    {
                        return BadRequest(new
                        {
                            success = false,
                            message = "Batch faculty assignment not found!"
                        });
                    }

                    batchId = Convert.ToInt32(result);
                }

                // -------------------------------------------------------
                // 2️⃣ Get new faculty ID using email
                // -------------------------------------------------------
                Guid facultyId;
                string userQuery = @"SELECT ""Id"" FROM ""AspNetUsers"" WHERE ""Email""=@Email LIMIT 1";

                using (var userCmd = new NpgsqlCommand(userQuery, con))
                {
                    userCmd.Parameters.AddWithValue("@Email", userEmail);
                    var result = await userCmd.ExecuteScalarAsync();

                    if (result == null)
                        return BadRequest(new { success = false, message = "Faculty not found!" });

                    facultyId = Guid.Parse(result.ToString()!);
                }

                // -------------------------------------------------------
                // 3️⃣ Prevent duplicate faculty in same batch
                // -------------------------------------------------------
                string duplicateQuery = @"
            SELECT 1
            FROM batch_faculties
            WHERE batch_id = @batchId
              AND faculties_id = @facultyId
              AND id <> @assignId
            LIMIT 1";

                using (var dupCmd = new NpgsqlCommand(duplicateQuery, con))
                {
                    dupCmd.Parameters.AddWithValue("@batchId", batchId);
                    dupCmd.Parameters.AddWithValue("@facultyId", facultyId);
                    dupCmd.Parameters.AddWithValue("@assignId", assignId);

                    if (await dupCmd.ExecuteScalarAsync() != null)
                    {
                        return BadRequest(new
                        {
                            success = false,
                            message = "This faculty is already assigned to this batch."
                        });
                    }
                }

                // -------------------------------------------------------
                // 4️⃣ Update faculty_id only
                // -------------------------------------------------------
                string updateQuery = @"
            UPDATE batch_faculties
            SET faculties_id = @facultyId,
                assigned_at = NOW()
            WHERE id = @assignId
            RETURNING id;
        ";

                using var updateCmd = new NpgsqlCommand(updateQuery, con);
                updateCmd.Parameters.AddWithValue("@facultyId", facultyId);
                updateCmd.Parameters.AddWithValue("@assignId", assignId);

                int updatedId = Convert.ToInt32(await updateCmd.ExecuteScalarAsync());

                return Ok(new
                {
                    success = true,
                    message = "Faculty updated successfully",
                    assign_id = updatedId
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        public async Task<IActionResult> GetAssignedFaculty(IFormCollection form)
        {
            try
            {
                using var con = new NpgsqlConnection(DbConnection);
                await con.OpenAsync();

                string userEmail = form["email"];

                if (string.IsNullOrEmpty(userEmail))
                    return BadRequest(new { success = false, message = "Faculty Email required!" });

                // ===============================
                // 1️⃣ GET FACULTY ID
                // ===============================
                Guid facultyId;

                using (var userCmd = new NpgsqlCommand(
                    @"SELECT ""Id"" FROM ""AspNetUsers"" WHERE ""Email"" = @Email LIMIT 1",
                    con))
                {
                    userCmd.Parameters.AddWithValue("@Email", userEmail);
                    var result = await userCmd.ExecuteScalarAsync();

                    if (result == null)
                        return BadRequest(new { success = false, message = "Faculty not found!" });

                    facultyId = Guid.Parse(result.ToString()!);
                }

                // ===============================
                // 2️⃣ GET ASSIGNED BATCHES
                // ===============================
                string query = @"
            SELECT 
                bf.id            AS assign_id,      -- 0
                b.id             AS batch_id,       -- 1
                b.batch_name,                       -- 2
                b.start_date,                       -- 3
                b.batch_image,                      -- 4
                b.end_date,                         -- 5
                b.start_time,                       -- 6
                b.end_time,                         -- 7
                bf.is_active,                       -- 8
                bf.assigned_at                      -- 9
            FROM batch_faculties bf
            JOIN batches b ON b.id = bf.batch_id
            WHERE bf.faculties_id = @facultyId
            ORDER BY bf.assigned_at DESC;
        ";

                using var cmd = new NpgsqlCommand(query, con);
                cmd.Parameters.AddWithValue("@facultyId", facultyId);

                var list = new List<object>();

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    list.Add(new
                    {
                        assign_id = reader.GetInt32(0),
                        batch_id = reader.GetInt32(1),
                        batch_name = reader.GetString(2),
                        start_date = reader.GetDateTime(3),

                        batch_image = reader.IsDBNull(4)
                            ? null
                            : reader.GetString(4),

                        end_date = reader.IsDBNull(5)
                            ? (DateTime?)null
                            : reader.GetDateTime(5),

                        start_time = reader.IsDBNull(6)
                            ? (TimeSpan?)null
                            : reader.GetTimeSpan(6),

                        end_time = reader.IsDBNull(7)
                            ? (TimeSpan?)null
                            : reader.GetTimeSpan(7),

                        is_active = reader.GetBoolean(8),
                        assigned_at = reader.GetDateTime(9)
                    });
                }

                return Ok(new
                {
                    success = true,
                    total = list.Count,
                    data = list
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


        public async Task<IActionResult> GetAssignedFacultyEmail(string userEmail)
        {
            try
            {
                using var con = new NpgsqlConnection(DbConnection);
                await con.OpenAsync();

                if (string.IsNullOrEmpty(userEmail))
                    return BadRequest(new { success = false, message = "Faculty Email required!" });

                // -------------------------------------------------------
                // 1️⃣ Get faculty ID
                // -------------------------------------------------------
                Guid facultyId;

                string userQuery = @"SELECT ""Id"" FROM ""AspNetUsers"" WHERE ""Email""=@Email LIMIT 1";

                using (var userCmd = new NpgsqlCommand(userQuery, con))
                {
                    userCmd.Parameters.AddWithValue("@Email", userEmail);

                    var result = await userCmd.ExecuteScalarAsync();
                    if (result == null)
                        return BadRequest(new { success = false, message = "Faculty not found!" });

                    facultyId = Guid.Parse(result.ToString()!);
                }

                // -------------------------------------------------------
                // 2️⃣ Get active assigned batches
                // -------------------------------------------------------
                string query = @"
            SELECT 
                bf.id,
                b.id,
                b.batch_name,
                b.start_date,
                b.end_date,
                b.start_time,
                b.end_time,
                b.batch_image,
                bf.is_active,
                bf.assigned_at
            FROM batch_faculties bf
            JOIN batches b ON b.id = bf.batch_id
            WHERE bf.faculties_id = @facultyId
              AND bf.is_active = TRUE
            ORDER BY bf.assigned_at DESC;
        ";

                using var cmd = new NpgsqlCommand(query, con);
                cmd.Parameters.AddWithValue("@facultyId", facultyId);

                var list = new List<object>();

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    list.Add(new
                    {
                        assign_id = reader.GetInt32(0),
                        batch_id = reader.GetInt32(1),
                        batch_name = reader.GetString(2),
                        start_date = reader.GetDateTime(3),

                        end_date = reader.IsDBNull(4)
                                    ? (DateTime?)null
                                    : reader.GetDateTime(4),

                        start_time = reader.IsDBNull(5)
                                    ? (TimeSpan?)null
                                    : reader.GetTimeSpan(5),

                        end_time = reader.IsDBNull(6)
                                    ? (TimeSpan?)null
                                    : reader.GetTimeSpan(6),

                        batch_image = reader.IsDBNull(7)
                                    ? null
                                    : reader.GetString(7),

                        is_active = reader.GetBoolean(8),
                        assigned_at = reader.GetDateTime(9)
                    });
                }

                return Ok(new
                {
                    success = true,
                    total = list.Count,
                    data = list
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        public async Task<IActionResult> DeleteAssignedFaculty(int facultyAssignId)
        {
            try
            {
                using var con = new NpgsqlConnection(DbConnection);
                await con.OpenAsync();

                // -------------------------------------------------------
                // 1️⃣ Check assignment exists
                // -------------------------------------------------------
                string checkQuery = @"
            SELECT 1 
            FROM batch_faculties 
            WHERE id = @id
            LIMIT 1";

                using (var checkCmd = new NpgsqlCommand(checkQuery, con))
                {
                    checkCmd.Parameters.AddWithValue("@id", facultyAssignId);
                    if (await checkCmd.ExecuteScalarAsync() == null)
                    {
                        return BadRequest(new
                        {
                            success = false,
                            message = "Faculty assignment not found!"
                        });
                    }
                }

                // -------------------------------------------------------
                // 2️⃣ Delete assignment
                // -------------------------------------------------------
                string deleteQuery = @"
            DELETE FROM batch_faculties
            WHERE id = @id
            RETURNING id;";

                using var deleteCmd = new NpgsqlCommand(deleteQuery, con);
                deleteCmd.Parameters.AddWithValue("@id", facultyAssignId);

                int deletedId = Convert.ToInt32(await deleteCmd.ExecuteScalarAsync());

                return Ok(new
                {
                    success = true,
                    message = "Faculty assignment deleted successfully",
                    assign_id = deletedId
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        public async Task<IActionResult> SoftDeleteAssignedFaculty(int facultyAssignId)
        {
            try
            {
                using var con = new NpgsqlConnection(DbConnection);
                await con.OpenAsync();

                // -------------------------------------------------------
                // 1️⃣ Check assignment exists
                // -------------------------------------------------------
                string checkQuery = @"
            SELECT 1 
            FROM batch_faculties 
            WHERE id = @id
            LIMIT 1";

                using (var checkCmd = new NpgsqlCommand(checkQuery, con))
                {
                    checkCmd.Parameters.AddWithValue("@id", facultyAssignId);
                    if (await checkCmd.ExecuteScalarAsync() == null)
                    {
                        return BadRequest(new
                        {
                            success = false,
                            message = "Faculty assignment not found!"
                        });
                    }
                }

                // -------------------------------------------------------
                // 2️⃣ Soft delete (Deactivate)
                // -------------------------------------------------------
                string updateQuery = @"
            UPDATE batch_faculties
            SET is_active = FALSE
            WHERE id = @id
            RETURNING id;";

                using var updateCmd = new NpgsqlCommand(updateQuery, con);
                updateCmd.Parameters.AddWithValue("@id", facultyAssignId);

                int updatedId = Convert.ToInt32(await updateCmd.ExecuteScalarAsync());

                return Ok(new
                {
                    success = true,
                    message = "Faculty assignment deactivated successfully",
                    assign_id = updatedId
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

    }

}