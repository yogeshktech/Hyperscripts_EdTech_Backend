using Microsoft.AspNetCore.Mvc;
using Npgsql;
using System.Data.Common;

namespace CareerCracker.DataBaseLayer
{
    public interface IDataBaseLayer_Batch
    {
        Task<IActionResult> NewBatch(IFormCollection form);
        Task<IActionResult> GetAllBatchs();
        Task<IActionResult> GetByIdBatchs(int courseId);
        Task<IActionResult> UpdateBatch(int batchId, IFormCollection form);
    }

    public partial interface IDataBaseLayer : IDataBaseLayer_Batch { }

    public partial class DataBaseLayer
    {
        public async Task<IActionResult> NewBatch(IFormCollection form)
        {
            try
            {
                // ===============================
                // 1️⃣ VALIDATION
                // ===============================
                if (string.IsNullOrWhiteSpace(form["course_id"]))
                {
                    return new BadRequestObjectResult(new
                    {
                        success = false,
                        message = "course_id is required"
                    });
                }

                int courseId = int.Parse(form["course_id"]);
                string batchName = form["batch_name"].ToString();
                DateTime startDate = DateTime.Parse(form["start_date"]);

                DateTime? endDate = string.IsNullOrEmpty(form["end_date"])
                    ? null
                    : DateTime.Parse(form["end_date"]);

                TimeSpan? startTime = string.IsNullOrEmpty(form["start_time"])
                    ? null
                    : TimeSpan.Parse(form["start_time"]);

                TimeSpan? endTime = string.IsNullOrEmpty(form["end_time"])
                    ? null
                    : TimeSpan.Parse(form["end_time"]);

                int? maxStudents = string.IsNullOrEmpty(form["max_students"])
                    ? null
                    : int.Parse(form["max_students"]);

                using var con = new NpgsqlConnection(DbConnection);
                await con.OpenAsync();

                // ===============================
                // 2️⃣ CHECK EXISTING BATCH
                // ===============================
                using (var checkCmd = new NpgsqlCommand(@"
            SELECT id
            FROM batches
            WHERE batch_name = @batch_name
              AND is_active = TRUE
            LIMIT 1
        ", con))
                {
                    checkCmd.Parameters.AddWithValue("@batch_name", batchName);
                    var existingBatchId = await checkCmd.ExecuteScalarAsync();

                    if (existingBatchId != null)
                    {
                        return new OkObjectResult(new
                        {
                            success = true,
                            message = "Batch already exists",
                            batch_id = Convert.ToInt32(existingBatchId)
                        });
                    }
                }

                // ===============================
                // 3️⃣ CREATE NEW BATCH
                // ===============================
                using var cmd = new NpgsqlCommand(@"
            INSERT INTO batches
            (course_id, batch_name, start_date, end_date, start_time, end_time, max_students)
            VALUES
            (@courseId, @batchName, @startDate, @endDate, @startTime, @endTime, @maxStudents)
            RETURNING id
        ", con);

                cmd.Parameters.AddWithValue("@courseId", courseId);
                cmd.Parameters.AddWithValue("@batchName",
                    string.IsNullOrWhiteSpace(batchName) ? (object)DBNull.Value : batchName);
                cmd.Parameters.AddWithValue("@startDate", startDate);
                cmd.Parameters.AddWithValue("@endDate", endDate ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@startTime", startTime ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@endTime", endTime ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@maxStudents", maxStudents ?? (object)DBNull.Value);

                int batchId = Convert.ToInt32(await cmd.ExecuteScalarAsync());

                return new OkObjectResult(new
                {
                    success = true,
                    message = "Batch created successfully",
                    batch_id = batchId
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

        public async Task<IActionResult> GetAllBatchs()
        {
            try
            {
                using var con = new NpgsqlConnection(DbConnection);
                await con.OpenAsync();

                var batches = new List<object>();

                using var cmd = new NpgsqlCommand(@"
            SELECT 
                b.id,
                b.course_id,
                c.course_name,
                b.batch_name,
                b.start_date,
                b.end_date,
                b.start_time,
                b.end_time,
                b.max_students,
                b.is_active,
                b.created_at
            FROM batches b
            JOIN courses c ON c.id = b.course_id
            ORDER BY b.created_at DESC
        ", con);

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    batches.Add(new
                    {
                        id = reader["id"],
                        course_id = reader["course_id"],
                        course_name = reader["course_name"],
                        batch_name = reader["batch_name"],
                        start_date = reader["start_date"],
                        end_date = reader["end_date"],
                        start_time = reader["start_time"],
                        end_time = reader["end_time"],
                        max_students = reader["max_students"],
                        is_active = reader["is_active"],
                        created_at = reader["created_at"]
                    });
                }

                return new OkObjectResult(new
                {
                    success = true,
                    total = batches.Count,
                    data = batches
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

        public async Task<IActionResult> GetByIdBatchs(int courseId)
        {
            using var con = new NpgsqlConnection(DbConnection);
            await con.OpenAsync();

            var batches = new List<object>();

            using var cmd = new NpgsqlCommand(@"
                SELECT id, batch_name, start_date, is_active
                FROM batches
                WHERE course_id = @courseId
                ORDER BY created_at DESC
            ", con);

            cmd.Parameters.AddWithValue("@courseId", courseId);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                batches.Add(new
                {
                    id = reader["id"],
                    batch_name = reader["batch_name"],
                    start_date = reader["start_date"],
                    is_active = reader["is_active"]
                });
            }

            return new OkObjectResult(new
            {
                success = true,
                data = batches
            });
        }

        public async Task<IActionResult> UpdateBatch(int batchId, IFormCollection form)
        {
            try
            {
                using var con = new NpgsqlConnection(DbConnection);
                await con.OpenAsync();

                var updates = new List<string>();
                var parameters = new List<NpgsqlParameter>();

                if (!string.IsNullOrWhiteSpace(form["batch_name"]))
                {
                    updates.Add("batch_name = @batch_name");
                    parameters.Add(new NpgsqlParameter("@batch_name", form["batch_name"].ToString()));
                }

                if (!string.IsNullOrWhiteSpace(form["start_date"]))
                {
                    updates.Add("start_date = @start_date");
                    parameters.Add(new NpgsqlParameter("@start_date",
                        DateTime.Parse(form["start_date"])));
                }

                if (!string.IsNullOrWhiteSpace(form["end_date"]))
                {
                    updates.Add("end_date = @end_date");
                    parameters.Add(new NpgsqlParameter("@end_date",
                        DateTime.Parse(form["end_date"])));
                }

                if (!string.IsNullOrWhiteSpace(form["start_time"]))
                {
                    updates.Add("start_time = @start_time");
                    parameters.Add(new NpgsqlParameter("@start_time",
                        TimeSpan.Parse(form["start_time"])));
                }

                if (!string.IsNullOrWhiteSpace(form["end_time"]))
                {
                    updates.Add("end_time = @end_time");
                    parameters.Add(new NpgsqlParameter("@end_time",
                        TimeSpan.Parse(form["end_time"])));
                }

                if (!string.IsNullOrWhiteSpace(form["max_students"]))
                {
                    updates.Add("max_students = @max_students");
                    parameters.Add(new NpgsqlParameter("@max_students",
                        int.Parse(form["max_students"])));
                }

                if (!string.IsNullOrWhiteSpace(form["is_active"]))
                {
                    updates.Add("is_active = @is_active");
                    parameters.Add(new NpgsqlParameter("@is_active",
                        bool.Parse(form["is_active"])));
                }

                if (updates.Count == 0)
                {
                    return new BadRequestObjectResult(new
                    {
                        success = false,
                        message = "No fields provided to update"
                    });
                }

                string query = $@"
            UPDATE batches
            SET {string.Join(", ", updates)}
            WHERE id = @batchId
        ";

                using var cmd = new NpgsqlCommand(query, con);
                cmd.Parameters.AddWithValue("@batchId", batchId);
                cmd.Parameters.AddRange(parameters.ToArray());

                int rows = await cmd.ExecuteNonQueryAsync();

                if (rows == 0)
                {
                    return new NotFoundObjectResult(new
                    {
                        success = false,
                        message = "Batch not found"
                    });
                }

                return new OkObjectResult(new
                {
                    success = true,
                    message = "Batch updated successfully"
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
