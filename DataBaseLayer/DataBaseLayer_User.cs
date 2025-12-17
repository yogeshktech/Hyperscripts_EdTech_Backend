using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace CareerCracker.DataBaseLayer
{
    public interface IDataBaseLayer_User
    {
        Task<IActionResult> MyCourses(string userEmail);
    }

    public partial interface IDataBaseLayer : IDataBaseLayer_User { }

    public partial class DataBaseLayer
    {
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

    }
}
