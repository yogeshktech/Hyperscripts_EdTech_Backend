using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.Elfie.Diagnostics;
using Npgsql;

namespace CareerCracker.DataBaseLayer
{
    public interface IDataBaseLayer_LiveClasses
    {
        Task<IActionResult> CreateLiveClass( IFormCollection form);
        Task<IActionResult> UpdateLiveClass(int liveClassId, IFormCollection form);
        Task<IActionResult> UpdateRecordingClass(int liveClassId, IFormCollection form);
        Task<IActionResult> GetRecordingClass(int batchId);
        Task<IActionResult> GetAllLiveClasses();
        Task<IActionResult> LiveClassesStatus(int liveclassid);
        Task<IActionResult> GetLiveClassesByBatch(int batchId);
        Task<IActionResult> HardDeleteLiveClass(int liveClassId);
        Task<IActionResult> CreateLiveClassAttendance(int liveClassId, string userEmail);
        Task<IActionResult> UpdateLiveClassAttendance(int attendanceId);
        Task<IActionResult> GetAttendanceByLiveClass(int liveClassId);
        Task<IActionResult> GetAttendanceByUser(Guid userId);
        Task<IActionResult> DeleteLiveClassAttendance(int attendanceId);
    }

    public partial interface IDataBaseLayer : IDataBaseLayer_LiveClasses { }

    public partial class DataBaseLayer
    {
        public async Task<IActionResult> CreateLiveClass(IFormCollection form)
        {
            await using var con = new NpgsqlConnection(DbConnection);
            await con.OpenAsync();
            await using var tran = await con.BeginTransactionAsync();

            try
            {
                // ===============================
                // 1️⃣ FORM DATA
                // ===============================
                string facultyEmail = form["email"].FirstOrDefault();
                string topic = form["topic_name"].FirstOrDefault();
                string meetingLink = form["meeting_link"].FirstOrDefault();
                string recordingLink = form["recording_link"].FirstOrDefault();

                if (!int.TryParse(form["batch_id"], out int batchId))
                    return BadRequest(new { success = false, message = "Invalid batch_id" });

                if (!DateTime.TryParse(form["class_date"], out DateTime classDate))
                    return BadRequest(new { success = false, message = "Invalid class_date" });

                if (!TimeSpan.TryParse(form["start_time"], out TimeSpan startTime))
                    return BadRequest(new { success = false, message = "Invalid start_time" });

                if (!TimeSpan.TryParse(form["end_time"], out TimeSpan endTime))
                    return BadRequest(new { success = false, message = "Invalid end_time" });

                if (string.IsNullOrWhiteSpace(facultyEmail))
                    return BadRequest(new { success = false, message = "Faculty email required" });

                // ===============================
                // 2️⃣ GET FACULTY ID (STRING)
                // ===============================
                string facultyId;

                await using (var userCmd = new NpgsqlCommand(
                    @"SELECT ""Id"" FROM ""AspNetUsers"" WHERE ""Email"" = @Email LIMIT 1",
                    con, tran))
                {
                    userCmd.Parameters.AddWithValue("@Email", facultyEmail);

                    var result = await userCmd.ExecuteScalarAsync();

                    if (result == null)
                        return BadRequest(new { success = false, message = "Faculty not found" });

                    facultyId = result.ToString(); // ✅ STRING — NO CAST
                }

                // ===============================
                // 3️⃣ IMAGE (OPTIONAL)
                // ===============================
                string imagePath = null;
                var image = form.Files.FirstOrDefault(f => f.Name == "image");

                if (image != null && image.Length > 0)
                {
                    var ext = Path.GetExtension(image.FileName).ToLower();
                    var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp" };

                    if (!allowed.Contains(ext))
                        return BadRequest(new { success = false, message = "Invalid image type" });

                    var fileName = $"{Guid.NewGuid()}{ext}";
                    var folder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/live-classes");

                    Directory.CreateDirectory(folder);

                    var path = Path.Combine(folder, fileName);
                    await using var fs = new FileStream(path, FileMode.Create);
                    await image.CopyToAsync(fs);

                    imagePath = $"/uploads/live-classes/{fileName}";
                }

                // ===============================
                // 4️⃣ INSERT LIVE CLASS
                // ===============================
                await using (var cmd = new NpgsqlCommand(@"
            INSERT INTO live_classes
            (
                batch_id,
                topic,
                class_date,
                start_time,
                end_time,
                meeting_link,
                recording_link,
                image_path,
                faculty_id,
                status,
                created_at
            )
            VALUES
            (
                @batch_id,
                @topic,
                @class_date,
                @start_time,
                @end_time,
                @meeting_link,
                @recording_link,
                @image_path,
                @faculty_id,
                true,
                NOW()
            )", con, tran))
                {
                    cmd.Parameters.AddWithValue("@batch_id", batchId);
                    cmd.Parameters.AddWithValue("@topic", topic);
                    cmd.Parameters.AddWithValue("@class_date", classDate.Date);
                    cmd.Parameters.AddWithValue("@start_time", startTime);
                    cmd.Parameters.AddWithValue("@end_time", endTime);
                    cmd.Parameters.AddWithValue("@meeting_link", (object?)meetingLink ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@recording_link", (object?)recordingLink ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@image_path", (object?)imagePath ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@faculty_id", facultyId);

                    await cmd.ExecuteNonQueryAsync();
                }

                await tran.CommitAsync();

                return Ok(new
                {
                    success = true,
                    message = "Live class created successfully"
                });
            }
            catch (Exception ex)
            {
                await tran.RollbackAsync();
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        public async Task<IActionResult> UpdateLiveClass(int liveClassId, IFormCollection form)
        {
            await using var con = new NpgsqlConnection(DbConnection);
            await con.OpenAsync();
            await using var tran = await con.BeginTransactionAsync();

            try
            {
                // ===============================
                // 1️⃣ READ FORM VALUES
                // ===============================
                string topicName = form["topic_name"].FirstOrDefault();
                string meetingLink = form["meeting_link"].FirstOrDefault();

                DateTime? classDate = DateTime.TryParse(form["class_date"].FirstOrDefault(), out var cd)
                    ? cd : null;

                TimeSpan? startTime = TimeSpan.TryParse(form["start_time"].FirstOrDefault(), out var st)
                    ? st : null;

                TimeSpan? endTime = TimeSpan.TryParse(form["end_time"].FirstOrDefault(), out var et)
                    ? et : null;

                // ===============================
                // 2️⃣ GET OLD IMAGE (IF ANY)
                // ===============================
                string oldImagePath = null;

                await using (var getImgCmd = new NpgsqlCommand(
                    "SELECT image FROM live_classes WHERE id = @id",
                    con, tran))
                {
                    getImgCmd.Parameters.AddWithValue("@id", liveClassId);
                    oldImagePath = await getImgCmd.ExecuteScalarAsync() as string;

                    if (oldImagePath == null && oldImagePath != "")
                    {
                        // record exists but image may be null – continue
                    }
                }

                // ===============================
                // 3️⃣ HANDLE IMAGE UPLOAD
                // ===============================
                string newImagePath = oldImagePath;
                var imageFile = form.Files.FirstOrDefault(f => f.Name == "image");

                if (imageFile != null && imageFile.Length > 0)
                {
                    var allowedExt = new[] { ".jpg", ".jpeg", ".png", ".webp" };
                    var ext = Path.GetExtension(imageFile.FileName).ToLower();

                    if (!allowedExt.Contains(ext))
                        return BadRequest(new { success = false, message = "Invalid image format" });

                    var fileName = $"{Guid.NewGuid()}{ext}";
                    var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/live-classes");

                    if (!Directory.Exists(folderPath))
                        Directory.CreateDirectory(folderPath);

                    var fullPath = Path.Combine(folderPath, fileName);

                    using var stream = new FileStream(fullPath, FileMode.Create);
                    await imageFile.CopyToAsync(stream);

                    newImagePath = $"/live-classes/{fileName}";

                    // delete old image
                    if (!string.IsNullOrWhiteSpace(oldImagePath))
                    {
                        var oldFullPath = Path.Combine(
                            Directory.GetCurrentDirectory(),
                            "wwwroot",
                            oldImagePath.TrimStart('/')
                        );

                        if (System.IO.File.Exists(oldFullPath))
                            System.IO.File.Delete(oldFullPath);
                    }
                }

                // ===============================
                // 4️⃣ UPDATE LIVE CLASS
                // ===============================
                await using var cmd = new NpgsqlCommand(@"
            UPDATE live_classes
            SET
                topic = @topic,
                class_date = @class_date,
                start_time = @start_time,
                end_time = @end_time,
                meeting_link = @meeting_link,
                image = @image,
                created_at = NOW()
            WHERE id = @id
            RETURNING id;
        ", con, tran);

                cmd.Parameters.AddWithValue("@id", liveClassId);
                cmd.Parameters.AddWithValue("@topic", (object?)topicName ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@class_date", (object?)classDate ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@start_time", (object?)startTime ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@end_time", (object?)endTime ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@meeting_link", (object?)meetingLink ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@image", (object?)newImagePath ?? DBNull.Value);

                var updatedId = await cmd.ExecuteScalarAsync();

                if (updatedId == null)
                {
                    await tran.RollbackAsync();
                    return NotFound(new
                    {
                        success = false,
                        message = "Live class not found"
                    });
                }

                await tran.CommitAsync();

                return Ok(new
                {
                    success = true,
                    message = "Live class updated successfully",
                    liveClassId,
                    image = newImagePath
                });
            }
            catch (Exception ex)
            {
                await tran.RollbackAsync();
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        public async Task<IActionResult> UpdateRecordingClass(int liveClassId, IFormCollection form)
        {
            using var con = new NpgsqlConnection(DbConnection);
            await con.OpenAsync();
            using var tran = await con.BeginTransactionAsync();

            try
            {
                var recordingStatus = form["recording_status"].FirstOrDefault()?.Trim();
                var recordingLink = form["recording_link"].FirstOrDefault()?.Trim();

                // ---------------- VALIDATION ----------------

                if (string.IsNullOrEmpty(recordingStatus))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "recording_status is required"
                    });
                }

                var allowedStatuses = new[] { "pending", "processing", "ready", "failed" };

                if (!allowedStatuses.Contains(recordingStatus))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Invalid recording_status"
                    });
                }

                if (recordingStatus == "ready" && string.IsNullOrEmpty(recordingLink))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "recording_link is required when status is 'ready'"
                    });
                }

                if (recordingStatus != "ready")
                {
                    recordingLink = null; // enforce clean data
                }

                // ---------------- UPDATE ----------------

                using var cmd = new NpgsqlCommand(@"
            UPDATE live_classes
            SET
                recording_status = @recordingStatus,
                recording_link = @recordingLink
            WHERE id = @liveClassId
            RETURNING id;
        ", con, tran);

                cmd.Parameters.AddWithValue("@liveClassId", liveClassId);
                cmd.Parameters.AddWithValue("@recordingStatus", recordingStatus);
                cmd.Parameters.AddWithValue("@recordingLink",
                    recordingLink == null ? (object)DBNull.Value : recordingLink);

                var updatedId = await cmd.ExecuteScalarAsync();

                if (updatedId == null)
                {
                    await tran.RollbackAsync();
                    return NotFound(new
                    {
                        success = false,
                        message = "Live class not found"
                    });
                }

                await tran.CommitAsync();

                return Ok(new
                {
                    success = true,
                    message = "Recording status updated successfully",
                    liveClassId
                });
            }
            catch (Exception ex)
            {
                await tran.RollbackAsync();
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        public async Task<IActionResult> GetRecordingClass(int batchId)
        {
            try
            {
                using var con = new NpgsqlConnection(DbConnection);
                await con.OpenAsync();

                using var cmd = new NpgsqlCommand(@"
            SELECT
                id,
                topic,
                class_date,
                start_time,
                end_time,
                recording_status,
                recording_link
            FROM live_classes
            WHERE batch_id = @batchId
              AND recording_status = 'ready'
              AND recording_link IS NOT NULL
            ORDER BY class_date DESC, start_time DESC;
        ", con);

                cmd.Parameters.AddWithValue("@batchId", batchId);

                using var reader = await cmd.ExecuteReaderAsync();

                var recordings = new List<object>();

                while (await reader.ReadAsync())
                {
                    recordings.Add(new
                    {
                        liveClassId = reader.GetInt32(0),
                        topic = reader.GetString(1),
                        classDate = reader.GetDateTime(2).ToString("yyyy-MM-dd"),
                        startTime = reader.GetTimeSpan(3).ToString(@"hh\:mm"),
                        endTime = reader.GetTimeSpan(4).ToString(@"hh\:mm"),
                        recordingStatus = reader.GetString(5),
                        recordingLink = reader.GetString(6)
                    });
                }

                return Ok(new
                {
                    success = true,
                    count = recordings.Count,
                    data = recordings
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

        //        public async Task<IActionResult> GetAllLiveClasses()
        //        {
        //            await using var con = new NpgsqlConnection(DbConnection);
        //            await con.OpenAsync();

        //            try
        //            {
        //                await using var cmd = new NpgsqlCommand(@"
        //            SELECT
        //    lc.id,
        //    lc.batch_id,
        //    lc.topic,
        //    lc.class_date,
        //    lc.start_time,
        //    lc.end_time,
        //    lc.meeting_link,
        //    lc.recording_link,
        //    lc.created_at,

        //    bf.faculties_id,

        //    anu.""Id""        AS user_id,
        //    anu.""FirstName"",
        //    anu.""LastName"",
        //    anu.""Email"",
        //    anu.""PhoneNumber"",
        //    anu.""UserName""
        //FROM live_classes lc
        //INNER JOIN batch_faculties bf
        //    ON lc.batch_id = bf.batch_id
        //INNER JOIN ""AspNetUsers"" anu
        //    ON anu.""Id""::uuid = bf.faculties_id
        //ORDER BY lc.class_date DESC, lc.start_time ASC;

        //        ", con);

        //                await using var reader = await cmd.ExecuteReaderAsync();

        //                var result = new List<object>();

        //                while (await reader.ReadAsync())
        //                {
        //                    result.Add(new
        //                    {
        //                        id = reader.GetInt32(reader.GetOrdinal("id")),
        //                        batchId = reader.GetInt32(reader.GetOrdinal("batch_id")),
        //                        topic = reader["topic"] as string,
        //                        classDate = reader["class_date"] as DateTime?,
        //                        startTime = reader["start_time"] as TimeSpan?,
        //                        endTime = reader["end_time"] as TimeSpan?,
        //                        meetingLink = reader["meeting_link"] as string,
        //                        recordingLink = reader["recording_link"] as string,
        //                        createdAt = reader.GetDateTime(reader.GetOrdinal("created_at")),

        //                        faculty = new
        //                        {
        //                            id = reader["user_id"].ToString(),   // ✅ FIX
        //                            firstName = reader["FirstName"] as string,
        //                            lastName = reader["LastName"] as string,
        //                            email = reader["Email"] as string,
        //                            phoneNumber = reader["PhoneNumber"] as string,
        //                            userName = reader["UserName"] as string
        //                        }

        //                    });
        //                }

        //                return Ok(new
        //                {
        //                    success = true,
        //                    count = result.Count,
        //                    data = result
        //                });
        //            }
        //            catch (Exception ex)
        //            {
        //                return BadRequest(new
        //                {
        //                    success = false,
        //                    message = ex.Message
        //                });
        //            }
        //        }

        public async Task<IActionResult> GetAllLiveClasses()    
        {
            await using var con = new NpgsqlConnection(DbConnection);
            await con.OpenAsync();

            try
            {
                // ===============================
                // SQL QUERY (image_path aliased as image)
                // ===============================
                await using var cmd = new NpgsqlCommand(@"
                         SELECT
                        lc.id,
                        lc.batch_id,
                        lc.topic,
                        lc.class_date,
                        lc.start_time,
                        lc.end_time,
                        lc.meeting_link,
                        lc.recording_link,
                        lc.image_path AS image,
                        lc.created_at,

                        anu.""Id""           AS user_id,
                        anu.""FirstName"",
                        anu.""LastName"",
                        anu.""Email"",
                        anu.""PhoneNumber"",
                        anu.""UserName""
                    FROM live_classes lc
                    LEFT JOIN ""AspNetUsers"" anu
                        ON anu.""Id"" = lc.faculty_id
                    WHERE lc.status = true
                    ORDER BY lc.class_date DESC, lc.start_time ASC;

        ", con);

                await using var reader = await cmd.ExecuteReaderAsync();

                var result = new List<object>();

                // ===============================
                // READ DATA
                // ===============================
                while (await reader.ReadAsync())
                {
                    result.Add(new
                    {
                        id = reader.GetInt32(reader.GetOrdinal("id")),
                        batchId = reader.GetInt32(reader.GetOrdinal("batch_id")),
                        topic = reader["topic"] as string,
                        classDate = reader["class_date"] == DBNull.Value
                                    ? (DateTime?)null
                                    : reader.GetDateTime(reader.GetOrdinal("class_date")),
                        startTime = reader["start_time"] == DBNull.Value
                                    ? (TimeSpan?)null
                                    : reader.GetTimeSpan(reader.GetOrdinal("start_time")),
                        endTime = reader["end_time"] == DBNull.Value
                                    ? (TimeSpan?)null
                                    : reader.GetTimeSpan(reader.GetOrdinal("end_time")),
                        meetingLink = reader["meeting_link"] as string,
                        recordingLink = reader["recording_link"] as string,
                        image = reader["image"] == DBNull.Value ? null : reader["image"].ToString(),
                        createdAt = reader.GetDateTime(reader.GetOrdinal("created_at")),

                        faculty = new
                        {
                            id = reader["user_id"].ToString(),
                            firstName = reader["FirstName"] as string,
                            lastName = reader["LastName"] as string,
                            email = reader["Email"] as string,
                            phoneNumber = reader["PhoneNumber"] as string,
                            userName = reader["UserName"] as string
                        }
                    });
                }

                return Ok(new
                {
                    success = true,
                    count = result.Count,
                    data = result
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

        public async Task<IActionResult> LiveClassesStatus(int liveclassid)
        {
            await using var con = new NpgsqlConnection(DbConnection);
            await con.OpenAsync();
            await using var tran = await con.BeginTransactionAsync();

            try
            {
                // ===============================
                // TOGGLE STATUS & RETURN NEW VALUE
                // ===============================
                await using var cmd = new NpgsqlCommand(@"
            UPDATE live_classes
            SET status = NOT status
            WHERE id = @id
            RETURNING status;
        ", con, tran);

                cmd.Parameters.AddWithValue("@id", liveclassid);

                var result = await cmd.ExecuteScalarAsync();

                if (result == null)
                {
                    await tran.RollbackAsync();
                    return NotFound(new
                    {
                        success = false,
                        message = "Live class not found"
                    });
                }

                bool status = Convert.ToBoolean(result);

                await tran.CommitAsync();

                return Ok(new
                {
                    success = true,
                    message = status ? "Live class activated" : "Live class deactivated",
                    liveClassId = liveclassid,
                    status
                });
            }
            catch (Exception ex)
            {
                await tran.RollbackAsync();
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }


        //public async Task<IActionResult> GetLiveClassesByBatch(int batchId)
        //{
        //    using var con = new NpgsqlConnection(DbConnection);
        //    await con.OpenAsync();

        //    try
        //    {
        //        using var cmd = new NpgsqlCommand(@"
        //    SELECT
        //        id,
        //        batch_id,
        //        topic,
        //        class_date,
        //        start_time,
        //        end_time,
        //        meeting_link,
        //        recording_link,
        //        created_at
        //    FROM live_classes
        //    WHERE batch_id = @batchId
        //    ORDER BY class_date DESC, start_time ASC;
        //", con);

        //        cmd.Parameters.AddWithValue("@batchId", batchId);

        //        using var reader = await cmd.ExecuteReaderAsync();

        //        var result = new List<object>();

        //        while (await reader.ReadAsync())
        //        {
        //            result.Add(new
        //            {
        //                id = reader.GetInt32(0),
        //                batchId = reader.GetInt32(1),
        //                topic = reader.IsDBNull(2) ? null : reader.GetString(2),
        //                classDate = reader.IsDBNull(3) ? (DateTime?)null : reader.GetDateTime(3),
        //                startTime = reader.IsDBNull(4) ? (TimeSpan?)null : reader.GetTimeSpan(4),
        //                endTime = reader.IsDBNull(5) ? (TimeSpan?)null : reader.GetTimeSpan(5),
        //                meetingLink = reader.IsDBNull(6) ? null : reader.GetString(6),
        //                recordingLink = reader.IsDBNull(7) ? null : reader.GetString(7),
        //                createdAt = reader.GetDateTime(8)
        //            });
        //        }

        //        return Ok(new
        //        {
        //            success = true,
        //            count = result.Count,
        //            data = result
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        return BadRequest(new { success = false, message = ex.Message });
        //    }
        //}

        public async Task<IActionResult> GetLiveClassesByBatch(int batchId)
        {
            using var con = new NpgsqlConnection(DbConnection);
            await con.OpenAsync();

            try
            {
                using var cmd = new NpgsqlCommand(@"
        SELECT
            lc.id,
            lc.batch_id,
            lc.topic,
            lc.class_date,
            lc.start_time,
            lc.end_time,
            lc.meeting_link,
            lc.recording_link,
            lc.created_at,
            b.batch_image
        FROM live_classes lc
        JOIN batches b ON b.id = lc.batch_id
        WHERE lc.batch_id = @batchId
        ORDER BY lc.class_date DESC, lc.start_time ASC;
        ", con);

                cmd.Parameters.AddWithValue("@batchId", batchId);

                using var reader = await cmd.ExecuteReaderAsync();

                var result = new List<object>();

                while (await reader.ReadAsync())
                {
                    result.Add(new
                    {
                        id = reader.GetInt32(0),
                        batchId = reader.GetInt32(1),
                        topic = reader.IsDBNull(2) ? null : reader.GetString(2),
                        classDate = reader.IsDBNull(3) ? (DateTime?)null : reader.GetDateTime(3),
                        startTime = reader.IsDBNull(4) ? (TimeSpan?)null : reader.GetTimeSpan(4),
                        endTime = reader.IsDBNull(5) ? (TimeSpan?)null : reader.GetTimeSpan(5),
                        meetingLink = reader.IsDBNull(6) ? null : reader.GetString(6),
                        recordingLink = reader.IsDBNull(7) ? null : reader.GetString(7),
                        createdAt = reader.GetDateTime(8),
                        batchImage = reader.IsDBNull(9) ? null : reader.GetString(9)
                    });
                }

                return Ok(new
                {
                    success = true,
                    count = result.Count,
                    data = result
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

        public async Task<IActionResult> HardDeleteLiveClass(int liveClassId)
        {
            using var con = new NpgsqlConnection(DbConnection);
            await con.OpenAsync();
            using var tran = await con.BeginTransactionAsync();

            try
            {
                // 🔹 STEP 1: Check if live class exists
                using (var checkCmd = new NpgsqlCommand(
                    "SELECT id FROM live_classes WHERE id = @liveClassId;",
                    con, tran))
                {
                    checkCmd.Parameters.AddWithValue("@liveClassId", liveClassId);

                    var exists = await checkCmd.ExecuteScalarAsync();

                    if (exists == null)
                    {
                        await tran.RollbackAsync();
                        return NotFound(new
                        {
                            success = false,
                            message = "Live class not found"
                        });
                    }
                }

                // 🔹 STEP 2: Delete live class
                using var deleteCmd = new NpgsqlCommand(@"
            DELETE FROM live_classes
            WHERE id = @liveClassId
            RETURNING id;
        ", con, tran);

                deleteCmd.Parameters.AddWithValue("@liveClassId", liveClassId);

                var deletedId = await deleteCmd.ExecuteScalarAsync();

                await tran.CommitAsync();

                return Ok(new
                {
                    success = true,
                    message = "Live class permanently deleted",
                    liveClassId = Convert.ToInt32(deletedId)
                });
            }
            catch (Exception ex)
            {
                await tran.RollbackAsync();
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        public async Task<IActionResult> CreateLiveClassAttendance(int liveClassId, string userEmail)
        {
            using var con = new NpgsqlConnection(DbConnection);
            await con.OpenAsync();
            using var tran = await con.BeginTransactionAsync();

            try
            {
                // 🔹 STEP 1: Get userId by email
                Guid userId;

                using (var userCmd = new NpgsqlCommand(
                    @"SELECT ""Id"" FROM ""AspNetUsers"" WHERE ""Email"" = @Email LIMIT 1",
                    con, tran))
                {
                    userCmd.Parameters.AddWithValue("@Email", userEmail);

                    var result = await userCmd.ExecuteScalarAsync();

                    if (result == null)
                    {
                        await tran.RollbackAsync();
                        return BadRequest(new
                        {
                            success = false,
                            message = "User not found"
                        });
                    }

                    userId = Guid.Parse(result.ToString());
                }


                // 🔹 STEP 3: Insert attendance
                using var cmd = new NpgsqlCommand(@"
            INSERT INTO live_class_attendance
            (live_class_id, user_id, joined_at, attended)
            VALUES
            (@liveClassId, @userId, NOW(), TRUE)
            RETURNING id;
        ", con, tran);

                cmd.Parameters.AddWithValue("@liveClassId", liveClassId);
                cmd.Parameters.AddWithValue("@userId", userId);

                int attendanceId = Convert.ToInt32(await cmd.ExecuteScalarAsync());

                await tran.CommitAsync();

                return Ok(new
                {
                    success = true,
                    message = "Attendance created successfully",
                    attendanceId
                });
            }
            catch (Exception ex)
            {
                await tran.RollbackAsync();
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        public async Task<IActionResult> UpdateLiveClassAttendance(int attendanceId)
        {
            using var con = new NpgsqlConnection(DbConnection);
            await con.OpenAsync();
            using var tran = await con.BeginTransactionAsync();

            try
            {
                using var cmd = new NpgsqlCommand(@"
            UPDATE live_class_attendance
            SET
                left_at = NOW()
            WHERE id = @attendanceId
              AND left_at IS NULL
            RETURNING id, joined_at, left_at;
        ", con, tran);

                cmd.Parameters.AddWithValue("@attendanceId", attendanceId);

                using var reader = await cmd.ExecuteReaderAsync();

                if (!await reader.ReadAsync())
                {
                    await tran.RollbackAsync();
                    return NotFound(new
                    {
                        success = false,
                        message = "Attendance not found or already left"
                    });
                }

                var joinedAt = reader.GetDateTime(1);
                var leftAt = reader.GetDateTime(2);

                await reader.CloseAsync();
                await tran.CommitAsync();

                return Ok(new
                {
                    success = true,
                    message = "Attendance updated successfully",
                    attendanceId,
                    joinedAt,
                    leftAt
                });
            }
            catch (Exception ex)
            {
                await tran.RollbackAsync();
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        public async Task<IActionResult> GetAttendanceByLiveClass(int liveClassId)
        {
            using var con = new NpgsqlConnection(DbConnection);
            await con.OpenAsync();

            try
            {
                using var cmd = new NpgsqlCommand(@"
            SELECT
                a.id,
                a.user_id,
                a.joined_at,
                a.left_at,
                a.attended
            FROM live_class_attendance a
            WHERE a.live_class_id = @liveClassId
            ORDER BY a.joined_at;
        ", con);

                cmd.Parameters.AddWithValue("@liveClassId", liveClassId);

                using var reader = await cmd.ExecuteReaderAsync();
                var result = new List<object>();

                while (await reader.ReadAsync())
                {
                    result.Add(new
                    {
                        id = reader.GetInt32(0),
                        userId = reader.GetGuid(1),
                        joinedAt = reader.IsDBNull(2) ? (DateTime?)null : reader.GetDateTime(2),
                        leftAt = reader.IsDBNull(3) ? (DateTime?)null : reader.GetDateTime(3),
                        attended = reader.GetBoolean(4)
                    });
                }

                return Ok(new
                {
                    success = true,
                    count = result.Count,
                    data = result
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        public async Task<IActionResult> GetAttendanceByUser(Guid userId)
        {
            await using var con = new NpgsqlConnection(DbConnection);
            await con.OpenAsync();

            try
            {
                await using var cmd = new NpgsqlCommand(@"
            SELECT
                a.id,
                a.live_class_id,
                a.joined_at,
                a.left_at,
                a.attended,

                u.""Id""          AS user_id,
                u.""FirstName""   AS first_name,
                u.""LastName""    AS last_name,
                u.""Email""       AS email,
                u.""PhoneNumber"" AS mobile,
                u.profile_image   AS profile_image
            FROM live_class_attendance a
            JOIN ""AspNetUsers"" u
                ON u.""Id""::uuid = a.user_id
            WHERE a.user_id = @userId
            ORDER BY a.joined_at DESC;
        ", con);

                cmd.Parameters.AddWithValue("@userId", userId);

                await using var reader = await cmd.ExecuteReaderAsync();
                var result = new List<object>();

                while (await reader.ReadAsync())
                {
                    result.Add(new
                    {
                        id = reader.GetInt32(0),
                        liveClassId = reader.GetInt32(1),
                        joinedAt = reader.IsDBNull(2) ? (DateTime?)null : reader.GetDateTime(2),
                        leftAt = reader.IsDBNull(3) ? (DateTime?)null : reader.GetDateTime(3),
                        attended = reader.GetBoolean(4),

                        user = new
                        {
                            id = reader.GetString(5),
                            first_name = reader.IsDBNull(6) ? null : reader.GetString(6),
                            last_name = reader.IsDBNull(7) ? null : reader.GetString(7),
                            email = reader.GetString(8),
                            mobile = reader.IsDBNull(9) ? null : reader.GetString(9),
                            image = reader.IsDBNull(10) ? null : reader.GetString(10)
                        }
                    });
                }

                return Ok(new
                {
                    success = true,
                    count = result.Count,
                    data = result
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        public async Task<IActionResult> DeleteLiveClassAttendance(int attendanceId)
        {
            using var con = new NpgsqlConnection(DbConnection);
            await con.OpenAsync();
            using var tran = await con.BeginTransactionAsync();

            try
            {
                using var cmd = new NpgsqlCommand(@"
            DELETE FROM live_class_attendance
            WHERE id = @attendanceId
            RETURNING id;
        ", con, tran);

                cmd.Parameters.AddWithValue("@attendanceId", attendanceId);

                var deletedId = await cmd.ExecuteScalarAsync();

                if (deletedId == null)
                {
                    await tran.RollbackAsync();
                    return NotFound(new { success = false, message = "Attendance not found" });
                }

                await tran.CommitAsync();

                return Ok(new
                {
                    success = true,
                    message = "Attendance deleted successfully",
                    attendanceId
                });
            }
            catch (Exception ex)
            {
                await tran.RollbackAsync();
                return BadRequest(new { success = false, message = ex.Message });
            }
        }


    }
}
