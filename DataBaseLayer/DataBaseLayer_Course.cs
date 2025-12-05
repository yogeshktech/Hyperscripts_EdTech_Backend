using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace CareerCracker.DataBaseLayer
{
    public interface IDataBaseLayer_Course
    {
        Task<IActionResult> AddCourse(IFormCollection form);
        Task<IActionResult> UpdateCourse(int id, IFormCollection form);
        Task<IActionResult> GetAllCourses();
        Task<IActionResult> GetCourseById(int id);
        Task<IActionResult> DeleteCourse(int id);
        Task<IActionResult> ToggleCourseStatus(int id);
    }
    public partial interface IDataBaseLayer : IDataBaseLayer_Course { }

    public partial class DataBaseLayer
    {
        public async Task<IActionResult> AddCourse(IFormCollection form)
        {
            try
            {
                if (form == null || form.Count == 0)
                    return BadRequest(new { success = false, message = "Form data is missing" });

                // -----------------------------
                // Helper Functions
                // -----------------------------
                int? ToInt(string value) =>
                    int.TryParse(value, out var v) ? v : (int?)null;

                decimal? ToDecimal(string value) =>
                    decimal.TryParse(value, out var v) ? v : (decimal?)null;

                DateTime? ToDate(string value) =>
                    DateTime.TryParse(value, out var v) ? v : (DateTime?)null;

                // -----------------------------
                // Required Fields
                // -----------------------------
                string courseName = form["courseName"];
                string courseSlug = GenerateSlug(courseName);

                if (string.IsNullOrWhiteSpace(courseName))
                    return BadRequest(new { success = false, message = "Course name is required" });

                // -----------------------------
                // Optional Fields
                // -----------------------------
                string courseDescription = form["courseDescription"];
                bool isActive = form.ContainsKey("isActive") && form["isActive"] == "true";

                int? categoryId = ToInt(form["categoryId"]);
                DateTime? startClassDate = ToDate(form["startClassDate"]);

                string maximumLpa = form["maximumLpa"];
                string minimumLpa = form["minimumLpa"];

                DateTime? demoStartDate = ToDate(form["demoStartDate"]);
                DateTime? demoEndDate = ToDate(form["demoEndDate"]);

                decimal? mrpPrice = ToDecimal(form["mrpPrice"]);
                decimal? salingPrice = ToDecimal(form["salingPrice"]);
                decimal? progress = ToDecimal(form["progress"]);

                string courseLevel = form["courseLevel"];
                string duration = form["duration"];
                string totalLectures = form["totalLectures"];
                string courseLanguage = form["courseLanguage"];

                string overview = form["overview"];
                string courseHighlights = form["courseHighlights"];
                string courseDetails = form["courseDetails"];
                string whyChooseUs = form["whyChooseUs"];

                // -----------------------------
                // Image Upload Handling
                // -----------------------------
                string savedImagePath = null;
                IFormFile courseImageFile = form.Files["courseImage"];

                if (courseImageFile != null && courseImageFile.Length > 0)
                {
                    string uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/courseImages");
                    if (!Directory.Exists(uploadFolder)) Directory.CreateDirectory(uploadFolder);

                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(courseImageFile.FileName);
                    string savePath = Path.Combine(uploadFolder, fileName);

                    using (var stream = new FileStream(savePath, FileMode.Create))
                        await courseImageFile.CopyToAsync(stream);

                    savedImagePath = "/uploads/courseImages/" + fileName;
                }

                using (var con = new NpgsqlConnection(DbConnection))
                {
                    await con.OpenAsync();

                    // -----------------------------
                    // Name Duplicate Check
                    // -----------------------------
                    string checkNameQuery = @"SELECT COUNT(*) FROM courses WHERE LOWER(course_name) = LOWER(@name)";
                    using (var cmd1 = new NpgsqlCommand(checkNameQuery, con))
                    {
                        cmd1.Parameters.AddWithValue("@name", courseName);
                        long exists = (long)await cmd1.ExecuteScalarAsync();
                        if (exists > 0)
                            return BadRequest(new { success = false, message = "Course name already exists" });
                    }

                    // -----------------------------
                    // Slug Duplicate Check
                    // -----------------------------
                    string checkSlugQuery = @"SELECT COUNT(*) FROM courses WHERE LOWER(course_slug) = LOWER(@slug)";
                    using (var cmd2 = new NpgsqlCommand(checkSlugQuery, con))
                    {
                        cmd2.Parameters.AddWithValue("@slug", courseSlug);
                        long slugExists = (long)await cmd2.ExecuteScalarAsync();
                        if (slugExists > 0)
                            return BadRequest(new { success = false, message = "Course slug already exists" });
                    }

                    // -----------------------------
                    // INSERT QUERY
                    // -----------------------------
                    string insertQuery = @"
                INSERT INTO courses
                (
                    course_name, course_discription, course_slug, is_active, course_image,
                    category_id, start_class_date, maximum_lpa, minimum_lpa,
                    demo_start_date, demo_end_date, mrp_price, saling_price,
                    course_level, duration, total_lectures, course_language,
                    overview, course_highlights, course_details, why_choose_us,
                    progress, updated_at
                )
                VALUES (
                    @name, @desc, @slug, @active, @img,
                    @categoryId, @startClass, @maxLpa, @minLpa,
                    @demoStart, @demoEnd, @mrp, @sale,
                    @level, @duration, @lectures, @language,
                    @overview, @highlights, @details, @why,
                    @progress, CURRENT_TIMESTAMP
                )";

                    using (var cmd = new NpgsqlCommand(insertQuery, con))
                    {
                        cmd.Parameters.AddWithValue("@name", courseName);
                        cmd.Parameters.AddWithValue("@desc", string.IsNullOrEmpty(courseDescription) ? (object)DBNull.Value : courseDescription);
                        cmd.Parameters.AddWithValue("@slug", courseSlug);
                        cmd.Parameters.AddWithValue("@active", isActive);
                        cmd.Parameters.AddWithValue("@img", (object?)savedImagePath ?? DBNull.Value);

                        cmd.Parameters.AddWithValue("@categoryId", (object?)categoryId ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@startClass", (object?)startClassDate ?? DBNull.Value);

                        cmd.Parameters.AddWithValue("@maxLpa", (object?)maximumLpa ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@minLpa", (object?)minimumLpa ?? DBNull.Value);

                        cmd.Parameters.AddWithValue("@demoStart", (object?)demoStartDate ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@demoEnd", (object?)demoEndDate ?? DBNull.Value);

                        cmd.Parameters.AddWithValue("@mrp", (object?)mrpPrice ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@sale", (object?)salingPrice ?? DBNull.Value);

                        cmd.Parameters.AddWithValue("@level", (object?)courseLevel ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@duration", (object?)duration ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@lectures", (object?)totalLectures ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@language", (object?)courseLanguage ?? DBNull.Value);

                        cmd.Parameters.AddWithValue("@overview", (object?)overview ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@highlights", (object?)courseHighlights ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@details", (object?)courseDetails ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@why", (object?)whyChooseUs ?? DBNull.Value);

                        cmd.Parameters.AddWithValue("@progress", (object?)progress ?? DBNull.Value);

                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                return Ok(new { success = true, message = "Course inserted successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = $"Internal server error: {ex.Message}" });
            }
        }

        public async Task<IActionResult> UpdateCourse(int id, IFormCollection form)
        {
            try
            {
                if (form == null || form.Count == 0)
                    return BadRequest(new { success = false, message = "Form data is missing" });

                // -----------------------------
                // Helper Functions
                // -----------------------------
                int? ToInt(string value) =>
                    int.TryParse(value, out var v) ? v : (int?)null;

                decimal? ToDecimal(string value) =>
                    decimal.TryParse(value, out var v) ? v : (decimal?)null;

                DateTime? ToDate(string value) =>
                    DateTime.TryParse(value, out var v) ? v : (DateTime?)null;

                // -----------------------------
                // Required Fields
                // -----------------------------
                string courseName = form["courseName"];
                if (string.IsNullOrWhiteSpace(courseName))
                    return BadRequest(new { success = false, message = "Course name is required" });

                string courseSlug = GenerateSlug(courseName);

                // -----------------------------
                // Optional Fields
                // -----------------------------
                string courseDescription = form["courseDescription"];
                bool isActive = form.ContainsKey("isActive") && form["isActive"] == "true";

                int? categoryId = ToInt(form["categoryId"]);
                DateTime? startClassDate = ToDate(form["startClassDate"]);

                string maximumLpa = form["maximumLpa"];
                string minimumLpa = form["minimumLpa"];

                DateTime? demoStartDate = ToDate(form["demoStartDate"]);
                DateTime? demoEndDate = ToDate(form["demoEndDate"]);

                decimal? mrpPrice = ToDecimal(form["mrpPrice"]);
                decimal? salingPrice = ToDecimal(form["salingPrice"]);
                decimal? progress = ToDecimal(form["progress"]);

                string courseLevel = form["courseLevel"];
                string duration = form["duration"];
                string totalLectures = form["totalLectures"];
                string courseLanguage = form["courseLanguage"];

                string overview = form["overview"];
                string courseHighlights = form["courseHighlights"];
                string courseDetails = form["courseDetails"];
                string whyChooseUs = form["whyChooseUs"];

                // -----------------------------
                // Image Upload Handling
                // -----------------------------
                string newImagePath = null;
                IFormFile courseImageFile = form.Files["courseImage"];

                if (courseImageFile != null && courseImageFile.Length > 0)
                {
                    string uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/courseImages");
                    if (!Directory.Exists(uploadFolder))
                        Directory.CreateDirectory(uploadFolder);

                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(courseImageFile.FileName);
                    string savePath = Path.Combine(uploadFolder, fileName);

                    using (var stream = new FileStream(savePath, FileMode.Create))
                        await courseImageFile.CopyToAsync(stream);

                    newImagePath = "/uploads/courseImages/" + fileName;
                }

                using (var con = new NpgsqlConnection(DbConnection))
                {
                    await con.OpenAsync();

                    // -----------------------------
                    // Check if course exists
                    // -----------------------------
                    string fetchQuery = "SELECT course_image FROM courses WHERE id = @id";
                    string oldImagePath = null;

                    using (var cmd0 = new NpgsqlCommand(fetchQuery, con))
                    {
                        cmd0.Parameters.AddWithValue("@id", id);
                        using (var reader = await cmd0.ExecuteReaderAsync())
                        {
                            if (!reader.HasRows)
                                return NotFound(new { success = false, message = "Course not found" });

                            await reader.ReadAsync();
                            oldImagePath = reader["course_image"] == DBNull.Value ? null : reader["course_image"].ToString();
                        }
                    }

                    // -----------------------------
                    // Duplicate Name Check (exclude current id)
                    // -----------------------------
                    string checkNameQuery = @"SELECT COUNT(*) FROM courses 
                                      WHERE LOWER(course_name) = LOWER(@name) AND id <> @id";

                    using (var cmd1 = new NpgsqlCommand(checkNameQuery, con))
                    {
                        cmd1.Parameters.AddWithValue("@name", courseName);
                        cmd1.Parameters.AddWithValue("@id", id);
                        long exists = (long)await cmd1.ExecuteScalarAsync();
                        if (exists > 0)
                            return BadRequest(new { success = false, message = "Course name already exists" });
                    }

                    // -----------------------------
                    // Duplicate Slug Check (exclude current id)
                    // -----------------------------
                    string checkSlugQuery = @"SELECT COUNT(*) FROM courses 
                                      WHERE LOWER(course_slug) = LOWER(@slug) AND id <> @id";

                    using (var cmd2 = new NpgsqlCommand(checkSlugQuery, con))
                    {
                        cmd2.Parameters.AddWithValue("@slug", courseSlug);
                        cmd2.Parameters.AddWithValue("@id", id);
                        long slugExists = (long)await cmd2.ExecuteScalarAsync();
                        if (slugExists > 0)
                            return BadRequest(new { success = false, message = "Course slug already exists" });
                    }

                    // If no new image uploaded → keep old one
                    string finalImagePath = newImagePath ?? oldImagePath;

                    // -----------------------------
                    // UPDATE QUERY
                    // -----------------------------
                    string updateQuery = @"
            UPDATE courses SET
                course_name = @name,
                course_discription = @desc,
                course_slug = @slug,
                is_active = @active,
                course_image = @img,
                category_id = @categoryId,
                start_class_date = @startClass,
                maximum_lpa = @maxLpa,
                minimum_lpa = @minLpa,
                demo_start_date = @demoStart,
                demo_end_date = @demoEnd,
                mrp_price = @mrp,
                saling_price = @sale,
                course_level = @level,
                duration = @duration,
                total_lectures = @lectures,
                course_language = @language,
                overview = @overview,
                course_highlights = @highlights,
                course_details = @details,
                why_choose_us = @why,
                progress = @progress,
                updated_at = CURRENT_TIMESTAMP
            WHERE id = @id";

                    using (var cmd = new NpgsqlCommand(updateQuery, con))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        cmd.Parameters.AddWithValue("@name", courseName);
                        cmd.Parameters.AddWithValue("@desc", string.IsNullOrEmpty(courseDescription) ? (object)DBNull.Value : courseDescription);
                        cmd.Parameters.AddWithValue("@slug", courseSlug);
                        cmd.Parameters.AddWithValue("@active", isActive);
                        cmd.Parameters.AddWithValue("@img", (object?)finalImagePath ?? DBNull.Value);

                        cmd.Parameters.AddWithValue("@categoryId", (object?)categoryId ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@startClass", (object?)startClassDate ?? DBNull.Value);

                        cmd.Parameters.AddWithValue("@maxLpa", (object?)maximumLpa ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@minLpa", (object?)minimumLpa ?? DBNull.Value);

                        cmd.Parameters.AddWithValue("@demoStart", (object?)demoStartDate ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@demoEnd", (object?)demoEndDate ?? DBNull.Value);

                        cmd.Parameters.AddWithValue("@mrp", (object?)mrpPrice ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@sale", (object?)salingPrice ?? DBNull.Value);

                        cmd.Parameters.AddWithValue("@level", (object?)courseLevel ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@duration", (object?)duration ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@lectures", (object?)totalLectures ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@language", (object?)courseLanguage ?? DBNull.Value);

                        cmd.Parameters.AddWithValue("@overview", (object?)overview ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@highlights", (object?)courseHighlights ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@details", (object?)courseDetails ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@why", (object?)whyChooseUs ?? DBNull.Value);

                        cmd.Parameters.AddWithValue("@progress", (object?)progress ?? DBNull.Value);

                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                return Ok(new { success = true, message = "Course updated successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = $"Internal server error: {ex.Message}" });
            }
        }

        public async Task<IActionResult> GetAllCourses()
        {
            try
            {
                using (var con = new NpgsqlConnection(DbConnection))
                {
                    await con.OpenAsync();

                    string query = @"
                SELECT 
                    c.id, c.course_name, c.course_discription, c.course_slug, c.is_active, c.course_image,
                    c.category_id, c.start_class_date, c.maximum_lpa, c.minimum_lpa,
                    c.demo_start_date, c.demo_end_date, c.mrp_price, c.saling_price,
                    c.course_level, c.duration, c.total_lectures, c.course_language,
                    c.overview, c.course_highlights, c.course_details, c.why_choose_us,
                    c.progress, c.updated_at,

                    -- Category Fields
                    cat.category_name, 
                    cat.category_discription,
                    cat.category_slug,
                    cat.category_image,
                    cat.is_active AS category_is_active,
                    cat.updated_at AS category_updated_at

                FROM courses c
                LEFT JOIN categories cat ON c.category_id = cat.id
                ORDER BY c.id DESC";

                    using (var cmd = new NpgsqlCommand(query, con))
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        var list = new List<object>();

                        while (await reader.ReadAsync())
                        {
                            list.Add(new
                            {
                                // Course Fields
                                id = reader["id"],
                                course_name = reader["course_name"],
                                course_discription = reader["course_discription"],
                                course_slug = reader["course_slug"],
                                is_active = reader["is_active"],
                                course_image = reader["course_image"],
                                category_id = reader["category_id"],
                                start_class_date = reader["start_class_date"],
                                maximum_lpa = reader["maximum_lpa"],
                                minimum_lpa = reader["minimum_lpa"],
                                demo_start_date = reader["demo_start_date"],
                                demo_end_date = reader["demo_end_date"],
                                mrp_price = reader["mrp_price"],
                                saling_price = reader["saling_price"],
                                course_level = reader["course_level"],
                                duration = reader["duration"],
                                total_lectures = reader["total_lectures"],
                                course_language = reader["course_language"],
                                overview = reader["overview"],
                                course_highlights = reader["course_highlights"],
                                course_details = reader["course_details"],
                                why_choose_us = reader["why_choose_us"],
                                progress = reader["progress"],
                                updated_at = reader["updated_at"],

                                // Category Details
                                category = new
                                {
                                    category_name = reader["category_name"],
                                    category_discription = reader["category_discription"],
                                    category_slug = reader["category_slug"],
                                    category_image = reader["category_image"],
                                    is_active = reader["category_is_active"],
                                    updated_at = reader["category_updated_at"]
                                }
                            });
                        }

                        return Ok(new { success = true, data = list });
                    }
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = $"Internal server error: {ex.Message}" });
            }
        }

        public async Task<IActionResult> GetCourseById(int id)
        {
            try
            {
                using (var con = new NpgsqlConnection(DbConnection))
                {
                    await con.OpenAsync();

                    string query = @"
                SELECT 
                    c.id, c.course_name, c.course_discription, c.course_slug, c.is_active, c.course_image,
                    c.category_id, c.start_class_date, c.maximum_lpa, c.minimum_lpa,
                    c.demo_start_date, c.demo_end_date, c.mrp_price, c.saling_price,
                    c.course_level, c.duration, c.total_lectures, c.course_language,
                    c.overview, c.course_highlights, c.course_details, c.why_choose_us,
                    c.progress, c.updated_at,

                    cat.category_name, 
                    cat.category_discription,
                    cat.category_slug,
                    cat.category_image,
                    cat.is_active AS category_is_active,
                    cat.updated_at AS category_updated_at

                FROM courses c
                LEFT JOIN categories cat ON c.category_id = cat.id
                WHERE c.id = @Id";

                    using (var cmd = new NpgsqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@Id", id);

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (!reader.HasRows)
                                return NotFound(new { success = false, message = "Course not found" });

                            await reader.ReadAsync();

                            var course = new
                            {
                                id = reader["id"],
                                course_name = reader["course_name"],
                                course_discription = reader["course_discription"],
                                course_slug = reader["course_slug"],
                                is_active = reader["is_active"],
                                course_image = reader["course_image"],
                                category_id = reader["category_id"],
                                start_class_date = reader["start_class_date"],
                                maximum_lpa = reader["maximum_lpa"],
                                minimum_lpa = reader["minimum_lpa"],
                                demo_start_date = reader["demo_start_date"],
                                demo_end_date = reader["demo_end_date"],
                                mrp_price = reader["mrp_price"],
                                saling_price = reader["saling_price"],
                                course_level = reader["course_level"],
                                duration = reader["duration"],
                                total_lectures = reader["total_lectures"],
                                course_language = reader["course_language"],
                                overview = reader["overview"],
                                course_highlights = reader["course_highlights"],
                                course_details = reader["course_details"],
                                why_choose_us = reader["why_choose_us"],
                                progress = reader["progress"],
                                updated_at = reader["updated_at"],

                                category = new
                                {
                                    category_name = reader["category_name"],
                                    category_discription = reader["category_discription"],
                                    category_slug = reader["category_slug"],
                                    category_image = reader["category_image"],
                                    is_active = reader["category_is_active"],
                                    updated_at = reader["category_updated_at"]
                                }
                            };

                            return Ok(new { success = true, data = course });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = $"Internal server error: {ex.Message}" });
            }
        }

        public async Task<IActionResult> DeleteCourse(int id)
        {
            try
            {
                using (var con = new NpgsqlConnection(DbConnection))
                {
                    await con.OpenAsync();

                    // Step 1: Check if course exists
                    string checkQuery = "SELECT COUNT(*) FROM courses WHERE id = @Id";

                    using (var checkCmd = new NpgsqlCommand(checkQuery, con))
                    {
                        checkCmd.Parameters.AddWithValue("@Id", id);

                        long exists = (long)await checkCmd.ExecuteScalarAsync();
                        if (exists == 0)
                            return NotFound(new { success = false, message = "Course not found" });
                    }

                    // Step 2: Delete course
                    string deleteQuery = "DELETE FROM courses WHERE id = @Id";

                    using (var deleteCmd = new NpgsqlCommand(deleteQuery, con))
                    {
                        deleteCmd.Parameters.AddWithValue("@Id", id);
                        await deleteCmd.ExecuteNonQueryAsync();
                    }
                }

                return Ok(new { success = true, message = "Course deleted successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = $"Internal server error: {ex.Message}" });
            }
        }

        public async Task<IActionResult> ToggleCourseStatus(int id)
        {
            try
            {
                using (var con = new NpgsqlConnection(DbConnection))
                {
                    await con.OpenAsync();

                    // 1. Check if course exists
                    string checkQuery = "SELECT is_active FROM courses WHERE id = @id";

                    bool? currentStatus = null;
                    using (var checkCmd = new NpgsqlCommand(checkQuery, con))
                    {
                        checkCmd.Parameters.AddWithValue("@id", id);

                        var result = await checkCmd.ExecuteScalarAsync();
                        if (result == null)
                        {
                            return NotFound(new { success = false, message = "Course not found" });
                        }

                        currentStatus = (bool)result;
                    }

                    // 2. Toggle status
                    bool newStatus = !(bool)currentStatus;

                    string updateQuery = "UPDATE courses SET is_active = @status, updated_at = CURRENT_TIMESTAMP WHERE id = @id";

                    using (var updateCmd = new NpgsqlCommand(updateQuery, con))
                    {
                        updateCmd.Parameters.AddWithValue("@status", newStatus);
                        updateCmd.Parameters.AddWithValue("@id", id);

                        await updateCmd.ExecuteNonQueryAsync();
                    }

                    // 3. Return response
                    return Ok(new
                    {
                        success = true,
                        message = "Course status updated successfully",
                        newStatus = newStatus
                    });
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = $"Internal server error: {ex.Message}" });
            }
        }


    }
}
