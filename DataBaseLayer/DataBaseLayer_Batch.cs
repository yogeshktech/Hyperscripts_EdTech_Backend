using Microsoft.AspNetCore.Mvc;
using Npgsql;
using System.Data.Common;
using System.Globalization;

namespace CareerCracker.DataBaseLayer
{
    public interface IDataBaseLayer_Batch
    {
        Task<IActionResult> NewBatch(IFormCollection form);
        Task<IActionResult> GetAllBatchs();
        Task<IActionResult> GetByIdBatchs(int courseId);
        Task<IActionResult> UpdateBatch(int batchId, IFormCollection form);
        Task<IActionResult> DeleteBatchs(int batchId);
        Task<IActionResult> GetBatchByUserId(string userEmail);
    }

    public partial interface IDataBaseLayer : IDataBaseLayer_Batch { }

    public partial class DataBaseLayer
    {
        //public async Task<IActionResult> NewBatch(IFormCollection form)
        //{
        //    try
        //    {
        //        // ===============================
        //        // 1️⃣ VALIDATION
        //        // ===============================
        //        if (string.IsNullOrWhiteSpace(form["course_id"]))
        //        {
        //            return new BadRequestObjectResult(new
        //            {
        //                success = false,
        //                message = "course_id is required"
        //            });
        //        }

        //        int courseId = int.Parse(form["course_id"]);
        //        string batchName = form["batch_name"].ToString();
        //        DateTime startDate = DateTime.Parse(form["start_date"]);

        //        DateTime? endDate = string.IsNullOrEmpty(form["end_date"])
        //            ? null
        //            : DateTime.Parse(form["end_date"]);

        //        TimeSpan? startTime = string.IsNullOrEmpty(form["start_time"])
        //            ? null
        //            : TimeSpan.Parse(form["start_time"]);

        //        TimeSpan? endTime = string.IsNullOrEmpty(form["end_time"])
        //            ? null
        //            : TimeSpan.Parse(form["end_time"]);

        //        int? maxStudents = string.IsNullOrEmpty(form["max_students"])
        //            ? null
        //            : int.Parse(form["max_students"]);

        //        using var con = new NpgsqlConnection(DbConnection);
        //        await con.OpenAsync();

        //        // ===============================
        //        // 2️⃣ CHECK EXISTING BATCH
        //        // ===============================
        //        using (var checkCmd = new NpgsqlCommand(@"
        //    SELECT id
        //    FROM batches
        //    WHERE batch_name = @batch_name
        //      AND is_active = TRUE
        //    LIMIT 1
        //", con))
        //        {
        //            checkCmd.Parameters.AddWithValue("@batch_name", batchName);
        //            var existingBatchId = await checkCmd.ExecuteScalarAsync();

        //            if (existingBatchId != null)
        //            {
        //                return new OkObjectResult(new
        //                {
        //                    success = true,
        //                    message = "Batch already exists",
        //                    batch_id = Convert.ToInt32(existingBatchId)
        //                });
        //            }
        //        }

        //        // ===============================
        //        // 3️⃣ CREATE NEW BATCH
        //        // ===============================
        //        using var cmd = new NpgsqlCommand(@"
        //    INSERT INTO batches
        //    (course_id, batch_name, start_date, end_date, start_time, end_time, max_students)
        //    VALUES
        //    (@courseId, @batchName, @startDate, @endDate, @startTime, @endTime, @maxStudents)
        //    RETURNING id
        //", con);

        //        cmd.Parameters.AddWithValue("@courseId", courseId);
        //        cmd.Parameters.AddWithValue("@batchName",
        //            string.IsNullOrWhiteSpace(batchName) ? (object)DBNull.Value : batchName);
        //        cmd.Parameters.AddWithValue("@startDate", startDate);
        //        cmd.Parameters.AddWithValue("@endDate", endDate ?? (object)DBNull.Value);
        //        cmd.Parameters.AddWithValue("@startTime", startTime ?? (object)DBNull.Value);
        //        cmd.Parameters.AddWithValue("@endTime", endTime ?? (object)DBNull.Value);
        //        cmd.Parameters.AddWithValue("@maxStudents", maxStudents ?? (object)DBNull.Value);

        //        int batchId = Convert.ToInt32(await cmd.ExecuteScalarAsync());

        //        return new OkObjectResult(new
        //        {
        //            success = true,
        //            message = "Batch created successfully",
        //            batch_id = batchId
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        return new BadRequestObjectResult(new
        //        {
        //            success = false,
        //            message = ex.Message
        //        });
        //    }
        //}

        public async Task<IActionResult> NewBatch(IFormCollection form)
        {
            try
            {
                // ===============================
                // 1️⃣ VALIDATION
                // ===============================
                if (string.IsNullOrWhiteSpace(form["course_id"]))
                    return BadRequest(new { success = false, message = "course_id is required" });

                if (string.IsNullOrWhiteSpace(form["batch_name"]))
                    return BadRequest(new { success = false, message = "batch_name is required" });

                if (string.IsNullOrWhiteSpace(form["start_date"]))
                    return BadRequest(new { success = false, message = "start_date is required" });

                // ===============================
                // 2️⃣ PARSE BASIC FIELDS
                // ===============================
                if (!int.TryParse(form["course_id"], out int courseId))
                    return BadRequest(new { success = false, message = "Invalid course_id" });

                string batchName = form["batch_name"];

                // ===============================
                // 3️⃣ DATE PARSING (FIXED)
                // ===============================
                if (!DateTime.TryParseExact(
                        form["start_date"],
                        "dd-MM-yyyy",
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.None,
                        out DateTime startDate))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Invalid start_date format. Use dd-MM-yyyy"
                    });
                }

                DateTime? endDate = null;
                if (!string.IsNullOrEmpty(form["end_date"]))
                {
                    if (!DateTime.TryParseExact(
                            form["end_date"],
                            "dd-MM-yyyy",
                            CultureInfo.InvariantCulture,
                            DateTimeStyles.None,
                            out DateTime parsedEndDate))
                    {
                        return BadRequest(new
                        {
                            success = false,
                            message = "Invalid end_date format. Use dd-MM-yyyy"
                        });
                    }
                    endDate = parsedEndDate;
                }

                // ===============================
                // 4️⃣ TIME PARSING
                // ===============================
                TimeSpan? startTime = null;
                if (!string.IsNullOrEmpty(form["start_time"]))
                {
                    if (!TimeSpan.TryParse(form["start_time"], out TimeSpan st))
                        return BadRequest(new { success = false, message = "Invalid start_time" });

                    startTime = st;
                }

                TimeSpan? endTime = null;
                if (!string.IsNullOrEmpty(form["end_time"]))
                {
                    if (!TimeSpan.TryParse(form["end_time"], out TimeSpan et))
                        return BadRequest(new { success = false, message = "Invalid end_time" });

                    endTime = et;
                }

                int? maxStudents = null;
                if (!string.IsNullOrEmpty(form["max_students"]))
                {
                    if (!int.TryParse(form["max_students"], out int max))
                        return BadRequest(new { success = false, message = "Invalid max_students" });

                    maxStudents = max;
                }

                // ===============================
                // 5️⃣ IMAGE UPLOAD
                // ===============================
                string? imagePath = null;
                var file = form.Files["batch_image"];

                if (file != null && file.Length > 0)
                {
                    var uploadDir = Path.Combine(
                        Directory.GetCurrentDirectory(),
                        "wwwroot",
                        "uploads",
                        "batches"
                    );

                    if (!Directory.Exists(uploadDir))
                        Directory.CreateDirectory(uploadDir);

                    var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
                    var fullPath = Path.Combine(uploadDir, fileName);

                    using var stream = new FileStream(fullPath, FileMode.Create);
                    await file.CopyToAsync(stream);

                    imagePath = $"/uploads/batches/{fileName}";
                }

                await using var con = new NpgsqlConnection(DbConnection);
                await con.OpenAsync();

                // ===============================
                // 6️⃣ CHECK EXISTING BATCH
                // ===============================
                await using (var checkCmd = new NpgsqlCommand(@"
            SELECT id 
            FROM batches 
            WHERE batch_name = @batch_name AND is_active = TRUE
            LIMIT 1
        ", con))
                {
                    checkCmd.Parameters.AddWithValue("@batch_name", batchName);

                    var existingId = await checkCmd.ExecuteScalarAsync();
                    if (existingId != null)
                    {
                        return Ok(new
                        {
                            success = true,
                            message = "Batch already exists",
                            batch_id = Convert.ToInt32(existingId)
                        });
                    }
                }

                // ===============================
                // 7️⃣ INSERT NEW BATCH
                // ===============================
                await using var cmd = new NpgsqlCommand(@"
            INSERT INTO batches
            (
                course_id,
                batch_name,
                start_date,
                end_date,
                start_time,
                end_time,
                max_students,
                batch_image
            )
            VALUES
            (
                @course_id,
                @batch_name,
                @start_date,
                @end_date,
                @start_time,
                @end_time,
                @max_students,
                @batch_image
            )
            RETURNING id
        ", con);

                cmd.Parameters.AddWithValue("@course_id", courseId);
                cmd.Parameters.AddWithValue("@batch_name", batchName);
                cmd.Parameters.AddWithValue("@start_date", startDate);
                cmd.Parameters.AddWithValue("@end_date", endDate ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@start_time", startTime ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@end_time", endTime ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@max_students", maxStudents ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@batch_image", imagePath ?? (object)DBNull.Value);

                int batchId = Convert.ToInt32(await cmd.ExecuteScalarAsync());

                return Ok(new
                {
                    success = true,
                    message = "Batch created successfully",
                    batch_id = batchId,
                    image = imagePath
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
                b.batch_image,
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
                        batch_image = reader["batch_image"],
                        is_active = reader["is_active"],
                        created_at = reader["created_at"]
                    });
                }

                return Ok(new
                {
                    success = true,
                    total = batches.Count,
                    data = batches
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        public async Task<IActionResult> GetByIdBatchs(int courseId)
        {
            using var con = new NpgsqlConnection(DbConnection);
            await con.OpenAsync();

            var batches = new List<object>();

            using var cmd = new NpgsqlCommand(@"
        SELECT id, batch_name, start_date, batch_image, is_active
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
                    batch_image = reader["batch_image"],
                    is_active = reader["is_active"]
                });
            }

            return Ok(new { success = true, data = batches });
        }

    public async Task<IActionResult> UpdateBatch(int batchId, IFormCollection form)
    {
        try
        {
            await using var con = new NpgsqlConnection(DbConnection);
            await con.OpenAsync();

            var updates = new List<string>();
            var parameters = new List<NpgsqlParameter>();

            // ===============================
            // 1️⃣ TEXT FIELDS
            // ===============================
            if (!string.IsNullOrWhiteSpace(form["batch_name"]))
            {
                updates.Add("batch_name = @batch_name");
                parameters.Add(new NpgsqlParameter("@batch_name", form["batch_name"].ToString()));
            }

            // ===============================
            // 2️⃣ DATE FIELDS (FIXED)
            // ===============================
            if (!string.IsNullOrWhiteSpace(form["start_date"]))
            {
                if (!DateTime.TryParseExact(
                        form["start_date"],
                        "dd-MM-yyyy",
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.None,
                        out DateTime startDate))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Invalid start_date format. Use dd-MM-yyyy"
                    });
                }

                updates.Add("start_date = @start_date");
                parameters.Add(new NpgsqlParameter("@start_date", startDate));
            }

            if (!string.IsNullOrWhiteSpace(form["end_date"]))
            {
                if (!DateTime.TryParseExact(
                        form["end_date"],
                        "dd-MM-yyyy",
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.None,
                        out DateTime endDate))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Invalid end_date format. Use dd-MM-yyyy"
                    });
                }

                updates.Add("end_date = @end_date");
                parameters.Add(new NpgsqlParameter("@end_date", endDate));
            }

            // ===============================
            // 3️⃣ TIME FIELDS
            // ===============================
            if (!string.IsNullOrWhiteSpace(form["start_time"]))
            {
                if (!TimeSpan.TryParse(form["start_time"], out TimeSpan st))
                    return BadRequest(new { success = false, message = "Invalid start_time" });

                updates.Add("start_time = @start_time");
                parameters.Add(new NpgsqlParameter("@start_time", st));
            }

            if (!string.IsNullOrWhiteSpace(form["end_time"]))
            {
                if (!TimeSpan.TryParse(form["end_time"], out TimeSpan et))
                    return BadRequest(new { success = false, message = "Invalid end_time" });

                updates.Add("end_time = @end_time");
                parameters.Add(new NpgsqlParameter("@end_time", et));
            }

            // ===============================
            // 4️⃣ OTHER FIELDS
            // ===============================
            if (!string.IsNullOrWhiteSpace(form["max_students"]))
            {
                if (!int.TryParse(form["max_students"], out int max))
                    return BadRequest(new { success = false, message = "Invalid max_students" });

                updates.Add("max_students = @max_students");
                parameters.Add(new NpgsqlParameter("@max_students", max));
            }

            if (!string.IsNullOrWhiteSpace(form["is_active"]))
            {
                if (!bool.TryParse(form["is_active"], out bool active))
                    return BadRequest(new { success = false, message = "Invalid is_active" });

                updates.Add("is_active = @is_active");
                parameters.Add(new NpgsqlParameter("@is_active", active));
            }

            // ===============================
            // 5️⃣ IMAGE UPDATE
            // ===============================
            var file = form.Files["batch_image"];
            if (file != null && file.Length > 0)
            {
                string? oldImage = null;

                await using (var imgCmd = new NpgsqlCommand(
                    "SELECT batch_image FROM batches WHERE id = @id", con))
                {
                    imgCmd.Parameters.AddWithValue("@id", batchId);
                    var result = await imgCmd.ExecuteScalarAsync();
                    oldImage = result == DBNull.Value ? null : result?.ToString();
                }

                if (!string.IsNullOrEmpty(oldImage))
                {
                    var oldPath = Path.Combine(
                        Directory.GetCurrentDirectory(),
                        "wwwroot",
                        oldImage.TrimStart('/')
                    );

                    if (System.IO.File.Exists(oldPath))
                        System.IO.File.Delete(oldPath);
                }

                var uploadDir = Path.Combine("wwwroot", "uploads", "batches");
                Directory.CreateDirectory(uploadDir);

                var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                var allowedExt = new[] { ".jpg", ".jpeg", ".png", ".webp" };

                if (!allowedExt.Contains(ext))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Only JPG, PNG, WEBP images allowed"
                    });
                }

                if (file.Length > 2 * 1024 * 1024)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Image size must be less than 2MB"
                    });
                }

                var fileName = $"{Guid.NewGuid()}{ext}";
                var fullPath = Path.Combine(uploadDir, fileName);

                using var stream = new FileStream(fullPath, FileMode.Create);
                await file.CopyToAsync(stream);

                updates.Add("batch_image = @batch_image");
                parameters.Add(
                    new NpgsqlParameter("@batch_image", $"/uploads/batches/{fileName}")
                );
            }

            // ===============================
            // 6️⃣ NO UPDATE CHECK
            // ===============================
            if (updates.Count == 0)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "No fields provided to update"
                });
            }

            // ===============================
            // 7️⃣ UPDATE QUERY
            // ===============================
            string query = $@"
            UPDATE batches
            SET {string.Join(", ", updates)}
            WHERE id = @batchId
        ";

            await using var cmd = new NpgsqlCommand(query, con);
            cmd.Parameters.AddWithValue("@batchId", batchId);
            cmd.Parameters.AddRange(parameters.ToArray());

            int rows = await cmd.ExecuteNonQueryAsync();

            if (rows == 0)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Batch not found"
                });
            }

            return Ok(new
            {
                success = true,
                message = "Batch updated successfully"
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


    public async Task<IActionResult> DeleteBatchs(int batchId)
        {
            try
            {
                using var con = new NpgsqlConnection(DbConnection);
                await con.OpenAsync();
                using var tran = await con.BeginTransactionAsync();

                // ===============================
                // 1️⃣ GET BATCH IMAGE
                // ===============================
                string? imagePath = null;
                using (var imgCmd = new NpgsqlCommand(
                    "SELECT batch_image FROM batches WHERE id = @id",
                    con, tran))
                {
                    imgCmd.Parameters.AddWithValue("@id", batchId);
                    var result = await imgCmd.ExecuteScalarAsync();
                    imagePath = result == DBNull.Value ? null : result?.ToString();
                }




                // ===============================
                // 5️⃣ DELETE BATCH
                // ===============================
                using (var cmd = new NpgsqlCommand(
                    "DELETE FROM batches WHERE id = @id",
                    con, tran))
                {
                    cmd.Parameters.AddWithValue("@id", batchId);

                    int rows = await cmd.ExecuteNonQueryAsync();
                    if (rows == 0)
                    {
                        await tran.RollbackAsync();
                        return NotFound(new { success = false, message = "Batch not found" });
                    }
                }

                await tran.CommitAsync();

                // ===============================
                // 6️⃣ DELETE IMAGE FILE
                // ===============================
                if (!string.IsNullOrEmpty(imagePath))
                {
                    var fullPath = Path.Combine(
                        Directory.GetCurrentDirectory(),
                        "wwwroot",
                        imagePath.TrimStart('/')
                    );

                    if (System.IO.File.Exists(fullPath))
                        System.IO.File.Delete(fullPath);
                }

                return Ok(new
                {
                    success = true,
                    message = "Batch deleted successfully"
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

        public async Task<IActionResult> GetBatchByUserId(string userEmail)
        {
            try
            {
                using var con = new NpgsqlConnection(DbConnection);
                await con.OpenAsync();
                using var tran = await con.BeginTransactionAsync();


                return Ok();
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

    }

}
