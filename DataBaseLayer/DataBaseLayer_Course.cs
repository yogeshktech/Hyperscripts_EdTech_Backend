using CareerCracker.S3Services;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using NpgsqlTypes;

namespace CareerCracker.DataBaseLayer
{
    public interface IDataBaseLayer_Course
    {
        Task<IActionResult> AddCourse(IFormCollection form);
        Task<IActionResult> UpdateCourse(int id, IFormCollection form);
        Task<IActionResult> GetAllCourses();
        Task<IActionResult> GetCoursesWithFilters(
            int? categoryId,
            string? categorySlug,
            int? languageId,
            string? languageSlug,
            decimal? minAverageRating,
            int? minReviewCount,
            string? search,
            int page,
            int pageSize);
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

                // -----------------------------
                // COURSE LANGUAGE → INT
                // -----------------------------
                int? courseLanguage = ToInt(form["courseLanguage"]);  // <-- Only this changed

                string overview = form["overview"];
                string courseHighlights = form["courseHighlights"];
                string courseDetails = form["courseDetails"];
                string whyChooseUs = form["whyChooseUs"];

                string? savedImageUrl = null;
                IFormFile? courseImageFile = form.Files["courseImage"];

                if (courseImageFile != null && courseImageFile.Length > 0)
                {
                    savedImageUrl = await S3StorageHelper.UploadFileAsync(courseImageFile, "courses");
                    if (string.IsNullOrEmpty(savedImageUrl))
                        return BadRequest(new { success = false, message = "Failed to upload course image" });
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
                        cmd.Parameters.AddWithValue("@img", (object?)savedImageUrl ?? DBNull.Value);

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

                        // ✔ CourseLanguage saved as INT
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

                // 🔥 UPDATED: courseLanguage saved as INT
                int? courseLanguageId = ToInt(form["courseLanguage"]);

                string overview = form["overview"];
                string courseHighlights = form["courseHighlights"];
                string courseDetails = form["courseDetails"];
                string whyChooseUs = form["whyChooseUs"];

                string? newImageUrl = null;
                IFormFile? courseImageFile = form.Files["courseImage"];

                if (courseImageFile != null && courseImageFile.Length > 0)
                {
                    newImageUrl = await S3StorageHelper.UploadFileAsync(courseImageFile, "courses");
                    if (string.IsNullOrEmpty(newImageUrl))
                        return BadRequest(new { success = false, message = "Failed to upload course image" });
                }

                using (var con = new NpgsqlConnection(DbConnection))
                {
                    await con.OpenAsync();

                    // -----------------------------
                    // Check if course exists
                    // -----------------------------
                    string fetchQuery = "SELECT course_image FROM courses WHERE id = @id";
                    string? oldImagePath = null;

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
                    // Duplicate Name Check
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
                    // Duplicate Slug Check
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

                    string finalImagePath = newImageUrl ?? oldImagePath;

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

                        // 🔥 Updated to INT
                        cmd.Parameters.AddWithValue("@language", (object?)courseLanguageId ?? DBNull.Value);

                        cmd.Parameters.AddWithValue("@overview", (object?)overview ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@highlights", (object?)courseHighlights ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@details", (object?)courseDetails ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@why", (object?)whyChooseUs ?? DBNull.Value);

                        cmd.Parameters.AddWithValue("@progress", (object?)progress ?? DBNull.Value);

                        await cmd.ExecuteNonQueryAsync();
                    }

                    if (!string.IsNullOrEmpty(newImageUrl) && !string.IsNullOrEmpty(oldImagePath))
                        await S3StorageHelper.DeleteByPathAsync(oldImagePath);
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
    cat.updated_at AS category_updated_at,

    -- Language Fields
    lang.language_name,
    lang.language_discription,
    lang.language_slug,
    lang.is_active AS language_is_active,
    lang.updated_at AS language_updated_at

FROM courses c
LEFT JOIN categories cat ON c.category_id = cat.id
LEFT JOIN languages lang ON c.course_language = lang.id
ORDER BY c.id DESC";

                    using (var cmd = new NpgsqlCommand(query, con))
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        var list = new List<object>();

                        while (await reader.ReadAsync())
                        {
                            list.Add(new
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
                                course_language_id = reader["course_language"],
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
                                },

                                courseLanguage = new
                                {
                                    language_name = reader["language_name"],
                                    language_discription = reader["language_discription"],
                                    language_slug = reader["language_slug"],
                                    is_active = reader["language_is_active"],
                                    updated_at = reader["language_updated_at"]
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

        public async Task<IActionResult> GetCoursesWithFilters(
            int? categoryId,
            string? categorySlug,
            int? languageId,
            string? languageSlug,
            decimal? minAverageRating,
            int? minReviewCount,
            string? search,
            int page,
            int pageSize)
        {
            try
            {
                page = Math.Max(1, page);
                pageSize = Math.Clamp(pageSize, 1, 100);
                var offset = (page - 1) * pageSize;

                var catSlug = string.IsNullOrWhiteSpace(categorySlug) ? null : categorySlug.Trim();
                var langSlug = string.IsNullOrWhiteSpace(languageSlug) ? null : languageSlug.Trim();
                if (categoryId.HasValue)
                    catSlug = null;
                if (languageId.HasValue)
                    langSlug = null;

                var searchTrim = string.IsNullOrWhiteSpace(search) ? null : search.Trim();
                var safeSearch = searchTrim == null ? null : searchTrim.Replace("%", "").Replace("_", "");
                var likePattern = safeSearch == null ? null : $"%{safeSearch}%";

                using var con = new NpgsqlConnection(DbConnection);
                await con.OpenAsync();

                const string query = @"
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
    cat.updated_at AS category_updated_at,

    lang.language_name,
    lang.language_discription,
    lang.language_slug,
    lang.is_active AS language_is_active,
    lang.updated_at AS language_updated_at,

    rev.avg_rating,
    rev.review_count,
    COUNT(*) OVER() AS _full_count

FROM courses c
LEFT JOIN categories cat ON c.category_id = cat.id
LEFT JOIN languages lang ON c.course_language = lang.id
LEFT JOIN (
    SELECT course_id,
           ROUND(AVG(rating)::numeric, 2) AS avg_rating,
           COUNT(*)::int AS review_count
    FROM reviews
    WHERE is_active = TRUE
    GROUP BY course_id
) rev ON rev.course_id = c.id

WHERE c.is_active = TRUE
  AND (@category_id IS NULL OR c.category_id = @category_id)
  AND (@category_slug IS NULL OR LOWER(cat.category_slug) = LOWER(@category_slug))
  AND (@language_id IS NULL OR c.course_language = @language_id)
  AND (@language_slug IS NULL OR LOWER(lang.language_slug) = LOWER(@language_slug))
  AND (@min_avg_rating IS NULL OR COALESCE(rev.avg_rating, 0) >= @min_avg_rating)
  AND (@min_review_count IS NULL OR COALESCE(rev.review_count, 0) >= @min_review_count)
  AND (
        @search_pattern IS NULL
        OR c.course_name ILIKE @search_pattern
        OR c.course_discription ILIKE @search_pattern
        OR c.course_slug ILIKE @search_pattern
        OR c.overview ILIKE @search_pattern
        OR c.course_highlights ILIKE @search_pattern
        OR c.course_details ILIKE @search_pattern
        OR cat.category_name ILIKE @search_pattern
        OR lang.language_name ILIKE @search_pattern
      )

ORDER BY c.id DESC
LIMIT @page_size OFFSET @offset;
";

                using var cmd = new NpgsqlCommand(query, con);
                // Typed NULLs — PostgreSQL 42P08 otherwise ("could not determine data type of parameter")
                cmd.Parameters.Add(new NpgsqlParameter("@category_id", NpgsqlDbType.Integer)
                { Value = categoryId.HasValue ? categoryId.Value : DBNull.Value });
                cmd.Parameters.Add(new NpgsqlParameter("@category_slug", NpgsqlDbType.Text)
                { Value = catSlug ?? (object)DBNull.Value });
                cmd.Parameters.Add(new NpgsqlParameter("@language_id", NpgsqlDbType.Integer)
                { Value = languageId.HasValue ? languageId.Value : DBNull.Value });
                cmd.Parameters.Add(new NpgsqlParameter("@language_slug", NpgsqlDbType.Text)
                { Value = langSlug ?? (object)DBNull.Value });
                cmd.Parameters.Add(new NpgsqlParameter("@min_avg_rating", NpgsqlDbType.Numeric)
                { Value = minAverageRating.HasValue ? minAverageRating.Value : DBNull.Value });
                cmd.Parameters.Add(new NpgsqlParameter("@min_review_count", NpgsqlDbType.Integer)
                { Value = minReviewCount.HasValue ? minReviewCount.Value : DBNull.Value });
                cmd.Parameters.Add(new NpgsqlParameter("@search_pattern", NpgsqlDbType.Text)
                { Value = likePattern ?? (object)DBNull.Value });
                cmd.Parameters.Add(new NpgsqlParameter("@page_size", NpgsqlDbType.Integer) { Value = pageSize });
                cmd.Parameters.Add(new NpgsqlParameter("@offset", NpgsqlDbType.Integer) { Value = offset });

                var list = new List<object>();
                int totalCount = 0;

                await using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        if (totalCount == 0)
                            totalCount = reader["_full_count"] == DBNull.Value ? 0 : Convert.ToInt32(reader["_full_count"]);

                        list.Add(new
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
                            course_language_id = reader["course_language"],
                            overview = reader["overview"],
                            course_highlights = reader["course_highlights"],
                            course_details = reader["course_details"],
                            why_choose_us = reader["why_choose_us"],
                            progress = reader["progress"],
                            updated_at = reader["updated_at"],
                            average_rating = reader["avg_rating"] == DBNull.Value ? null : reader["avg_rating"],
                            review_count = reader["review_count"] == DBNull.Value ? 0 : reader["review_count"],
                            category = new
                            {
                                category_name = reader["category_name"],
                                category_discription = reader["category_discription"],
                                category_slug = reader["category_slug"],
                                category_image = reader["category_image"],
                                is_active = reader["category_is_active"],
                                updated_at = reader["category_updated_at"]
                            },
                            courseLanguage = new
                            {
                                language_name = reader["language_name"],
                                language_discription = reader["language_discription"],
                                language_slug = reader["language_slug"],
                                is_active = reader["language_is_active"],
                                updated_at = reader["language_updated_at"]
                            }
                        });
                    }
                }

                return Ok(new
                {
                    success = true,
                    page,
                    pageSize,
                    totalCount,
                    totalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                    filters = new
                    {
                        categoryId,
                        categorySlug = catSlug,
                        languageId,
                        languageSlug = langSlug,
                        minAverageRating,
                        minReviewCount,
                        search = searchTrim
                    },
                    data = list
                });
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

            -- Category Info
            cat.category_name, 
            cat.category_discription,
            cat.category_slug,
            cat.category_image,
            cat.is_active AS category_is_active,
            cat.updated_at AS category_updated_at,

            -- Language Info
            lang.language_name,
            lang.language_discription,
            lang.language_slug,
            lang.is_active AS language_is_active,
            lang.updated_at AS language_updated_at

        FROM courses c
        LEFT JOIN categories cat ON c.category_id = cat.id
        LEFT JOIN languages lang ON c.course_language = lang.id
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
                                },

                                courseLanguage = new
                                {
                                    language_name = reader["language_name"],
                                    language_discription = reader["language_discription"],
                                    language_slug = reader["language_slug"],
                                    is_active = reader["language_is_active"],
                                    updated_at = reader["language_updated_at"]
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

                    string? courseImage = null;
                    using (var imgCmd = new NpgsqlCommand("SELECT course_image FROM courses WHERE id = @Id", con))
                    {
                        imgCmd.Parameters.AddWithValue("@Id", id);
                        var imgResult = await imgCmd.ExecuteScalarAsync();
                        if (imgResult != null && imgResult != DBNull.Value)
                            courseImage = imgResult.ToString();
                    }

                    string deleteQuery = "DELETE FROM courses WHERE id = @Id";

                    using (var deleteCmd = new NpgsqlCommand(deleteQuery, con))
                    {
                        deleteCmd.Parameters.AddWithValue("@Id", id);
                        await deleteCmd.ExecuteNonQueryAsync();
                    }

                    if (!string.IsNullOrEmpty(courseImage))
                        await S3StorageHelper.DeleteStoredMediaAsync(courseImage);
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
