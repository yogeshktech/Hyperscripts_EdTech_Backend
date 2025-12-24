using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.Elfie.Diagnostics;
using Npgsql;

namespace CareerCracker.DataBaseLayer
{
    public interface IDataBaseLayer_LiveClasses
    {
        Task<IActionResult> CreateLiveClass(int batchId, IFormCollection form);
        Task<IActionResult> UpdateLiveClass(int liveClassId, IFormCollection form);
        Task<IActionResult> UpdateRecordingClass(int liveClassId, IFormCollection form);
        Task<IActionResult> GetRecordingClass(int batchId);
        Task<IActionResult> GetAllLiveClasses();
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
        public async Task<IActionResult> CreateLiveClass(int batchId, IFormCollection form)
        {
            using var con = new NpgsqlConnection(DbConnection);
            await con.OpenAsync();
            using var tran = await con.BeginTransactionAsync();

            try
            {
                var topicName = form["topic_name"].FirstOrDefault();
                var classDate = form["class_date"].FirstOrDefault();
                var startTime = form["start_time"].FirstOrDefault();
                var classEndTime = form["end_time"].FirstOrDefault();
                var meetingLink = form["meeting_link"].FirstOrDefault();

                using var cmd = new NpgsqlCommand(@"
            INSERT INTO live_classes
            (batch_id, topic, class_date, start_time, end_time, meeting_link, created_at)
            VALUES
            (@batchId, @topicName, @classDate, @startTime, @classEndTime, @meetingLink, NOW())
            RETURNING id;
        ", con, tran);

                cmd.Parameters.AddWithValue("@batchId", batchId);
                cmd.Parameters.AddWithValue("@topicName",
                    string.IsNullOrWhiteSpace(topicName) ? (object)DBNull.Value : topicName);

                cmd.Parameters.AddWithValue("@classDate",
                    string.IsNullOrWhiteSpace(classDate) ? (object)DBNull.Value : DateTime.Parse(classDate));

                cmd.Parameters.AddWithValue("@startTime",
                    string.IsNullOrWhiteSpace(startTime) ? (object)DBNull.Value : TimeSpan.Parse(startTime));

                cmd.Parameters.AddWithValue("@classEndTime",
                    string.IsNullOrWhiteSpace(classEndTime) ? (object)DBNull.Value : TimeSpan.Parse(classEndTime));

                cmd.Parameters.AddWithValue("@meetingLink",
                    string.IsNullOrWhiteSpace(meetingLink) ? (object)DBNull.Value : meetingLink);

                int liveClassId = Convert.ToInt32(await cmd.ExecuteScalarAsync());

                await tran.CommitAsync();

                return Ok(new
                {
                    success = true,
                    message = "Live class created successfully",
                    liveClassId
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
            using var con = new NpgsqlConnection(DbConnection);
            await con.OpenAsync();
            using var tran = await con.BeginTransactionAsync();

            try
            {
                var topicName = form["topic_name"].FirstOrDefault();
                var classDate = form["class_date"].FirstOrDefault();
                var startTime = form["start_time"].FirstOrDefault();
                var classEndTime = form["end_time"].FirstOrDefault();
                var meetingLink = form["meeting_link"].FirstOrDefault();

                using var cmd = new NpgsqlCommand(@"
            UPDATE live_classes
            SET
                topic = @topicName,
                class_date = @classDate,
                start_time = @startTime,
                end_time = @classEndTime,
                meeting_link = @meetingLink,
                created_at = NOW()
            WHERE id = @liveClassId
            RETURNING id;
        ", con, tran);

                cmd.Parameters.AddWithValue("@liveClassId", liveClassId);

                cmd.Parameters.AddWithValue("@topicName",
                    string.IsNullOrWhiteSpace(topicName) ? (object)DBNull.Value : topicName);

                cmd.Parameters.AddWithValue("@classDate",
                    string.IsNullOrWhiteSpace(classDate) ? (object)DBNull.Value : DateTime.Parse(classDate));

                cmd.Parameters.AddWithValue("@startTime",
                    string.IsNullOrWhiteSpace(startTime) ? (object)DBNull.Value : TimeSpan.Parse(startTime));

                cmd.Parameters.AddWithValue("@classEndTime",
                    string.IsNullOrWhiteSpace(classEndTime) ? (object)DBNull.Value : TimeSpan.Parse(classEndTime));

                cmd.Parameters.AddWithValue("@meetingLink",
                    string.IsNullOrWhiteSpace(meetingLink) ? (object)DBNull.Value : meetingLink);

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

        public async Task<IActionResult> GetAllLiveClasses()
        {
            using var con = new NpgsqlConnection(DbConnection);
            await con.OpenAsync();

            try
            {
                using var cmd = new NpgsqlCommand(@"
            SELECT
                id,
                batch_id,
                topic,
                class_date,
                start_time,
                end_time,
                meeting_link,
                recording_link,
                created_at
            FROM live_classes
            ORDER BY class_date DESC, start_time ASC;
        ", con);

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
                        createdAt = reader.GetDateTime(8)
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
            using var con = new NpgsqlConnection(DbConnection);
            await con.OpenAsync();

            try
            {
                using var cmd = new NpgsqlCommand(@"
            SELECT
                id,
                live_class_id,
                joined_at,
                left_at,
                attended
            FROM live_class_attendance
            WHERE user_id = @userId
            ORDER BY joined_at DESC;
        ", con);

                cmd.Parameters.AddWithValue("@userId", userId);

                using var reader = await cmd.ExecuteReaderAsync();
                var result = new List<object>();

                while (await reader.ReadAsync())
                {
                    result.Add(new
                    {
                        id = reader.GetInt32(0),
                        liveClassId = reader.GetInt32(1),
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
