using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace CareerCracker.DataBaseLayer
{
    public interface IDataBaseLayer_User
    {
        Task<IActionResult> GetUserDashboard(string userEmail);
        Task<IActionResult> MyCourses(string userEmail);
        Task<IActionResult> MyBatch(int courseId, string userEmail);
        Task<IActionResult> DeleteUser(IFormCollection form);
        Task<IActionResult> UpdateUser(Guid userId, IFormCollection form);
        Task<IActionResult> UpdateUserDetailByUserId(string userEmail, IFormCollection form);
    }

    public partial interface IDataBaseLayer : IDataBaseLayer_User { }

    public partial class DataBaseLayer
    {
        public async Task<IActionResult> GetUserDashboard(string userEmail)
        {
            using var con = new NpgsqlConnection(DbConnection);
            await con.OpenAsync();

            try
            {
                Guid userId;

                using (var userCmd = new NpgsqlCommand(
                    @"SELECT ""Id"" FROM ""AspNetUsers"" WHERE ""Email"" = @Email", con))
                {
                    userCmd.Parameters.AddWithValue("@Email", userEmail);

                    var result = await userCmd.ExecuteScalarAsync();
                    if (result == null)
                    {
                        return new BadRequestObjectResult(new
                        {
                            success = false,
                            message = "User not found"
                        });
                    }

                    userId = Guid.Parse(result.ToString()!);
                }

                int enrolledCourses;
                int activeCourses;
                int completedCourses;

                using (var cmd = new NpgsqlCommand(@"
            SELECT
                COUNT(*)::int AS enrolled_courses,
                COUNT(*) FILTER (
                    WHERE uc.is_active = TRUE
                      AND uc.completed_at IS NULL
                      AND COALESCE(uc.progress_percent, 0) < 100
                )::int AS active_courses,
                COUNT(*) FILTER (
                    WHERE uc.completed_at IS NOT NULL
                       OR COALESCE(uc.progress_percent, 0) >= 100
                )::int AS completed_courses
            FROM user_courses uc
            WHERE uc.user_id = @userId
        ", con))
                {
                    cmd.Parameters.AddWithValue("@userId", userId);

                    using var reader = await cmd.ExecuteReaderAsync();
                    if (!await reader.ReadAsync())
                    {
                        enrolledCourses = 0;
                        activeCourses = 0;
                        completedCourses = 0;
                    }
                    else
                    {
                        enrolledCourses = reader.GetInt32(reader.GetOrdinal("enrolled_courses"));
                        activeCourses = reader.GetInt32(reader.GetOrdinal("active_courses"));
                        completedCourses = reader.GetInt32(reader.GetOrdinal("completed_courses"));
                    }
                }

                return new OkObjectResult(new
                {
                    success = true,
                    stats = new
                    {
                        enrolledCourses,
                        activeCourses,
                        completedCourses
                    }
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

        public async Task<IActionResult> MyCourses(string userEmail)
        {
            using var con = new NpgsqlConnection(DbConnection);
            await con.OpenAsync();

            try
            {
                // ===============================
                // 1️⃣ GET USER ID
                // ===============================
                Guid userId;

                using (var userCmd = new NpgsqlCommand(
                    @"SELECT ""Id"" FROM ""AspNetUsers"" WHERE ""Email"" = @Email", con))
                {
                    userCmd.Parameters.AddWithValue("@Email", userEmail);

                    var result = await userCmd.ExecuteScalarAsync();
                    if (result == null)
                    {
                        return new BadRequestObjectResult(new
                        {
                            success = false,
                            message = "User not found"
                        });
                    }

                    userId = Guid.Parse(result.ToString());
                }

                // ===============================
                // 2️⃣ GET USER COURSES
                // ===============================
                var courses = new List<object>();

                using (var cmd = new NpgsqlCommand(@"
            SELECT
                uc.id,
                uc.course_id,
                uc.order_id,
                uc.access_type,
                uc.is_active,
                uc.progress_percent,
                uc.completed_at,
                uc.valid_till,
                uc.created_at,
                c.course_name,
                c.course_image,
                c.course_slug,
                c.start_class_date,
                c.total_lectures
            FROM user_courses uc
            JOIN courses c ON c.id = uc.course_id
            WHERE uc.user_id = @userId
              AND uc.is_active = TRUE
            ORDER BY uc.created_at DESC
        ", con))
                {
                    cmd.Parameters.AddWithValue("@userId", userId);

                    using var reader = await cmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        courses.Add(new
                        {
                            id = reader["id"],
                            course_id = reader["course_id"],
                            course_name = reader["course_name"],
                            course_image = reader["course_image"],
                            course_slug = reader["course_slug"],
                            start_class_date = reader["start_class_date"],
                            total_lectures = reader["total_lectures"],
                            order_id = reader["order_id"],
                            access_type = reader["access_type"],
                            progress_percent = reader["progress_percent"],
                            valid_till = reader["valid_till"],
                            created_at = reader["created_at"]
                        });
                    }
                }

                // ===============================
                // 3️⃣ RETURN RESPONSE
                // ===============================
                return new OkObjectResult(new
                {
                    success = true,
                    total = courses.Count,
                    data = courses
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

        public async Task<IActionResult> MyBatch(int courseId, string userEmail)
        {
            try
            {
                using var con = new NpgsqlConnection(DbConnection);
                await con.OpenAsync();
                using var tran = await con.BeginTransactionAsync();

                // Step 1: Get user_id from email
                // ===============================
                Guid userId;

                using (var userCmd = new NpgsqlCommand(
                    @"SELECT ""Id"" FROM ""AspNetUsers"" WHERE ""Email""=@Email", con, tran))
                {
                    userCmd.Parameters.AddWithValue("@Email", userEmail);
                    var result = await userCmd.ExecuteScalarAsync();

                    if (result == null)
                        return BadRequest(new { success = false, message = "User not found" });

                    userId = Guid.Parse(result.ToString());
                }

                // Step 2: Get batches for this user and course
                using var cmdBatches = new NpgsqlCommand(@"
            SELECT b.id, b.batch_name, b.start_date, b.end_date, b.start_time, b.end_time, b.max_students, b.is_active, batch_image
            FROM batches b
            INNER JOIN user_batches ub ON b.id = ub.batch_id
            WHERE ub.user_id = @userId
              AND ub.course_id = @courseId
              AND ub.is_active = TRUE
        ", con);

                cmdBatches.Parameters.AddWithValue("@userId", userId);
                cmdBatches.Parameters.AddWithValue("@courseId", courseId);

                var reader = await cmdBatches.ExecuteReaderAsync();
                var batchList = new List<object>();

                while (await reader.ReadAsync())
                {
                    batchList.Add(new
                    {
                        id = reader.GetInt32(0),
                        batch_name = reader.GetString(1),
                        start_date = reader.GetDateTime(2),
                        end_date = reader.IsDBNull(3) ? (DateTime?)null : reader.GetDateTime(3),
                        start_time = reader.IsDBNull(4) ? (TimeSpan?)null : reader.GetTimeSpan(4),
                        end_time = reader.IsDBNull(5) ? (TimeSpan?)null : reader.GetTimeSpan(5),
                        max_students = reader.IsDBNull(6) ? (int?)null : reader.GetInt32(6),
                        is_active = reader.GetBoolean(7),
                        batch_image = reader.GetString(8)
                    });
                }


                return Ok(new { success = true, data = batchList });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        public async Task<IActionResult> DeleteUser(IFormCollection form)
        {
            try
            {
                var email = form["email"].ToString().Trim();

                if (string.IsNullOrWhiteSpace(email))
                    return BadRequest(new { success = false, message = "Email is required" });

                using var con = new NpgsqlConnection(DbConnection);
                await con.OpenAsync();
                using var tran = await con.BeginTransactionAsync();

                // Step 1: Get UserId (Id is stored as TEXT)
                Guid userId;

                using (var userCmd = new NpgsqlCommand(
                    @"SELECT ""Id"" 
              FROM ""AspNetUsers"" 
              WHERE ""Email"" = @Email", con, tran))
                {
                    userCmd.Parameters.AddWithValue("@Email", email);

                    var result = await userCmd.ExecuteScalarAsync();

                    if (result == null)
                        return NotFound(new { success = false, message = "User not found" });

                    userId = Guid.Parse(result.ToString());
                }

                // Step 2: Delete user by Id
                using (var deleteCmd = new NpgsqlCommand(
                    @"DELETE FROM ""AspNetUsers"" WHERE ""Id"" = @UserId", con, tran))
                {
                    deleteCmd.Parameters.AddWithValue("@UserId", userId.ToString());
                    await deleteCmd.ExecuteNonQueryAsync();
                }

                await tran.CommitAsync();

                return Ok(new { success = true, message = "User deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        public async Task<IActionResult> UpdateUser(Guid userId, IFormCollection form)
        {
            using var con = new NpgsqlConnection(DbConnection);
            await con.OpenAsync();

            try
            {
                // ===============================
                // 1️⃣ GET USER ID
                // ===============================
                //Guid userId;

                using (var userCmd = new NpgsqlCommand(
                    @"SELECT ""Id"" FROM ""AspNetUsers"" WHERE ""Email"" = @Email", con))
                {
                    userCmd.Parameters.AddWithValue("@Email", userId);

                    var result = await userCmd.ExecuteScalarAsync();
                    if (result == null)
                    {
                        return new BadRequestObjectResult(new
                        {
                            success = false,
                            message = "User not found"
                        });
                    }

                    userId = Guid.Parse(result.ToString());
                }

                // ===============================
                // 2️⃣ GET USER COURSES
                // ===============================
                var courses = new List<object>();

                using (var cmd = new NpgsqlCommand(@"
            SELECT
                uc.id,
                uc.course_id,
                uc.order_id,
                uc.access_type,
                uc.is_active,
                uc.progress_percent,
                uc.completed_at,
                uc.valid_till,
                uc.created_at,
                c.course_name,
                c.course_image,
                c.course_slug,
                c.start_class_date,
                c.total_lectures
            FROM user_courses uc
            JOIN courses c ON c.id = uc.course_id
            WHERE uc.user_id = @userId
              AND uc.is_active = TRUE
            ORDER BY uc.created_at DESC
        ", con))
                {
                    cmd.Parameters.AddWithValue("@userId", userId);

                    using var reader = await cmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        courses.Add(new
                        {
                            id = reader["id"],
                            course_id = reader["course_id"],
                            course_name = reader["course_name"],
                            course_image = reader["course_image"],
                            course_slug = reader["course_slug"],
                            start_class_date = reader["start_class_date"],
                            total_lectures = reader["total_lectures"],
                            order_id = reader["order_id"],
                            access_type = reader["access_type"],
                            progress_percent = reader["progress_percent"],
                            valid_till = reader["valid_till"],
                            created_at = reader["created_at"]
                        });
                    }
                }

                // ===============================
                // 3️⃣ RETURN RESPONSE
                // ===============================
                return new OkObjectResult(new
                {
                    success = true,
                    total = courses.Count,
                    data = courses
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

        public async Task<IActionResult> UpdateUserDetailByUserId(string userEmail, IFormCollection form)
        {
            try
            {
                var firstName = form["firstName"];
                var lastName = form["last_name"];
                var phoneNumber = form["phone_number"];
                var position = form["position"];
                var experience = form["experience"];
                var specialization = form["specialization"];
                var profileImage = form["profile_image"];
                var gender = form["gender"];
                var address = form["user_address"];
                var dob = form["dob"];
                var subject = form["subject"];
                var salary = form["salary"];

                return Ok(phoneNumber);
            }
            catch(Exception ex)
            {
                return new BadRequestObjectResult(new { success = false, message = ex.Message });
            }
        }
    }
}
