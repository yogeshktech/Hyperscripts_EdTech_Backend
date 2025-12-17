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
                if (imageFile != null && imageFile.Length > 0)
                {
                    string folder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/facultyImages");
                    Directory.CreateDirectory(folder);

                    string fileName = Guid.NewGuid() + Path.GetExtension(imageFile.FileName);
                    string path = Path.Combine(folder, fileName);

                    using var stream = new FileStream(path, FileMode.Create);
                    await imageFile.CopyToAsync(stream);

                    newImagePath = "/uploads/facultyImages/" + fileName;
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



    }

}