using Microsoft.AspNetCore.Mvc;
using Npgsql;
using System.Security.Claims;

namespace CareerCracker.DataBaseLayer
{
    public interface IDataBaseLayer_Reviews
    {
        Task<IActionResult> AddReview(string userEmail, IFormCollection form);
        Task<IActionResult> AddReviewByAdmin( IFormCollection form);
        Task<IActionResult> UpdateReview(int id, string userEmail, IFormCollection form);
        Task<IActionResult> UpdateReviewByAdmin(int id, IFormCollection form);
        Task<IActionResult> GetReviews(int courseId);
        Task<IActionResult> DeleteReview(int id);
        Task<IActionResult> ToggleReview(int id);
        Task<IActionResult> getAllReviewByAdmin();
    }

    public partial interface IDataBaseLayer : IDataBaseLayer_Reviews { }

    public partial class DataBaseLayer
    {
        public async Task<IActionResult> AddReview(string userEmail, IFormCollection form)
        {
            try
            {
                if (string.IsNullOrEmpty(userEmail))
                {
                    return BadRequest(new { success = false, message = "User email is required" });
                }

                // ------------------------------------------
                // 1️⃣ Get Form Data
                // ------------------------------------------
                string courseIdStr = form["course_id"];
                string ratingStr = form["rating"];
                string title = form["title"];
                string reviewText = form["review_text"];

                if (!int.TryParse(courseIdStr, out int courseId))
                    return BadRequest(new { success = false, message = "Invalid course_id" });

                if (!int.TryParse(ratingStr, out int rating) || rating < 1 || rating > 5)
                    return BadRequest(new { success = false, message = "Rating must be 1 to 5" });

                using (var con = new NpgsqlConnection(DbConnection))
                {
                    await con.OpenAsync();

                    // ------------------------------------------
                    // 2️⃣ GET user_id FROM AspNetUsers BY EMAIL
                    // ------------------------------------------
                    string getUserQuery = @"SELECT ""Id"" FROM ""AspNetUsers"" WHERE ""Email"" = @Email LIMIT 1";

                    string userId = null;

                    using (var userCmd = new NpgsqlCommand(getUserQuery, con))
                    {
                        userCmd.Parameters.AddWithValue("@Email", userEmail);

                        var result = await userCmd.ExecuteScalarAsync();
                        if (result == null)
                        {
                            return BadRequest(new
                            {
                                success = false,
                                message = "User not found for given email"
                            });
                        }

                        userId = result.ToString();
                    }

                    // ------------------------------------------
                    // 3️⃣ CHECK DUPLICATE REVIEW
                    // ------------------------------------------
                    string checkQuery = @"
                SELECT COUNT(*) FROM reviews 
                WHERE user_id = @user_id 
                AND course_id = @course_id";

                    using (var checkCmd = new NpgsqlCommand(checkQuery, con))
                    {
                        checkCmd.Parameters.AddWithValue("@user_id", userId);
                        checkCmd.Parameters.AddWithValue("@course_id", courseId);

                        long exists = (long)await checkCmd.ExecuteScalarAsync();

                        if (exists > 0)
                        {
                            return BadRequest(new
                            {
                                success = false,
                                message = "You have already submitted a review for this course"
                            });
                        }
                    }

                    // ------------------------------------------
                    // 4️⃣ INSERT REVIEW
                    // ------------------------------------------
                    string insertQuery = @"
                INSERT INTO reviews 
                (user_id, course_id, rating, title, review_text)
                VALUES 
                (@user_id, @course_id, @rating, @title, @review_text)
                RETURNING id;
            ";

                    using (var cmd = new NpgsqlCommand(insertQuery, con))
                    {
                        cmd.Parameters.AddWithValue("@user_id", userId);
                        cmd.Parameters.AddWithValue("@course_id", courseId);
                        cmd.Parameters.AddWithValue("@rating", rating);
                        cmd.Parameters.AddWithValue("@title", (object)title ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@review_text", (object)reviewText ?? DBNull.Value);

                        var insertedId = await cmd.ExecuteScalarAsync();

                        return Ok(new
                        {
                            success = true,
                            id = insertedId,
                            message = "Review added successfully!"
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = $"Internal server error: {ex.Message}"
                });
            }
        }

        public async Task<IActionResult> AddReviewByAdmin( IFormCollection form)
        {
            try
            {
                string userEmail = form["email"];

                if (string.IsNullOrEmpty(userEmail))
                {
                    return BadRequest(new { success = false, message = "User email is required" });
                }

                // ------------------------------------------
                // 1️⃣ Get Form Data
                // ------------------------------------------
                string courseIdStr = form["course_id"];
                string ratingStr = form["rating"];
                string title = form["title"];
                string reviewText = form["review_text"];

                if (!int.TryParse(courseIdStr, out int courseId))
                    return BadRequest(new { success = false, message = "Invalid course_id" });

                if (!int.TryParse(ratingStr, out int rating) || rating < 1 || rating > 5)
                    return BadRequest(new { success = false, message = "Rating must be 1 to 5" });

                using (var con = new NpgsqlConnection(DbConnection))
                {
                    await con.OpenAsync();

                    // ------------------------------------------
                    // 2️⃣ GET user_id FROM AspNetUsers BY EMAIL
                    // ------------------------------------------
                    string getUserQuery = @"SELECT ""Id"" FROM ""AspNetUsers"" WHERE ""Email"" = @Email LIMIT 1";

                    string userId = null;

                    using (var userCmd = new NpgsqlCommand(getUserQuery, con))
                    {
                        userCmd.Parameters.AddWithValue("@Email", userEmail);

                        var result = await userCmd.ExecuteScalarAsync();
                        if (result == null)
                        {
                            return BadRequest(new
                            {
                                success = false,
                                message = "User not found for given email"
                            });
                        }

                        userId = result.ToString();
                    }

                    // ------------------------------------------
                    // 3️⃣ CHECK DUPLICATE REVIEW
                    // ------------------------------------------
                    string checkQuery = @"
                SELECT COUNT(*) FROM reviews 
                WHERE user_id = @user_id 
                AND course_id = @course_id";

                    using (var checkCmd = new NpgsqlCommand(checkQuery, con))
                    {
                        checkCmd.Parameters.AddWithValue("@user_id", userId);
                        checkCmd.Parameters.AddWithValue("@course_id", courseId);

                        long exists = (long)await checkCmd.ExecuteScalarAsync();

                        if (exists > 0)
                        {
                            return BadRequest(new
                            {
                                success = false,
                                message = "You have already submitted a review for this course"
                            });
                        }
                    }

                    // ------------------------------------------
                    // 4️⃣ INSERT REVIEW
                    // ------------------------------------------
                    string insertQuery = @"
                INSERT INTO reviews 
                (user_id, course_id, rating, title, review_text)
                VALUES 
                (@user_id, @course_id, @rating, @title, @review_text)
                RETURNING id;
            ";

                    using (var cmd = new NpgsqlCommand(insertQuery, con))
                    {
                        cmd.Parameters.AddWithValue("@user_id", userId);
                        cmd.Parameters.AddWithValue("@course_id", courseId);
                        cmd.Parameters.AddWithValue("@rating", rating);
                        cmd.Parameters.AddWithValue("@title", (object)title ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@review_text", (object)reviewText ?? DBNull.Value);

                        var insertedId = await cmd.ExecuteScalarAsync();

                        return Ok(new
                        {
                            success = true,
                            id = insertedId,
                            message = "Review added successfully!"
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = $"Internal server error: {ex.Message}"
                });
            }
        }

        public async Task<IActionResult> UpdateReview(int id, string userEmail, IFormCollection form)
        {
            try
            {
                if (string.IsNullOrEmpty(userEmail))
                {
                    return BadRequest(new { success = false, message = "User email is required" });
                }

                using (var con = new NpgsqlConnection(DbConnection))
                {
                    await con.OpenAsync();

                    // ------------------------------------------
                    // 1️⃣ GET user_id FROM AspNetUsers BY EMAIL
                    // ------------------------------------------
                    string getUserQuery = @"SELECT ""Id"" FROM ""AspNetUsers"" WHERE ""Email"" = @Email LIMIT 1";

                    string userId = null;

                    using (var userCmd = new NpgsqlCommand(getUserQuery, con))
                    {
                        userCmd.Parameters.AddWithValue("@Email", userEmail);

                        var result = await userCmd.ExecuteScalarAsync();
                        if (result == null)
                        {
                            return BadRequest(new
                            {
                                success = false,
                                message = "User not found for given email"
                            });
                        }

                        userId = result.ToString();
                    }

                    // ------------------------------------------
                    // 2️⃣ CHECK IF REVIEW EXISTS & BELONGS TO USER
                    // ------------------------------------------
                    string checkQuery = @"SELECT COUNT(*) FROM reviews WHERE id=@id AND user_id=@user_id";

                    using (var checkCmd = new NpgsqlCommand(checkQuery, con))
                    {
                        checkCmd.Parameters.AddWithValue("@id", id);
                        checkCmd.Parameters.AddWithValue("@user_id", userId);

                        long exists = (long)await checkCmd.ExecuteScalarAsync();
                        if (exists == 0)
                        {
                            return BadRequest(new
                            {
                                success = false,
                                message = "Review not found or you are not authorized to update this review"
                            });
                        }
                    }

                    // ------------------------------------------
                    // 3️⃣ GET FORM FIELDS
                    // ------------------------------------------
                    string ratingStr = form["rating"];
                    string title = form["title"];
                    string reviewText = form["review_text"];

                    int? rating = null;

                    if (!string.IsNullOrEmpty(ratingStr))
                    {
                        if (!int.TryParse(ratingStr, out int parsedRating) || parsedRating < 1 || parsedRating > 5)
                        {
                            return BadRequest(new
                            {
                                success = false,
                                message = "Rating must be between 1 and 5"
                            });
                        }

                        rating = parsedRating;
                    }

                    // ------------------------------------------
                    // 4️⃣ UPDATE QUERY
                    // ------------------------------------------
                    string updateQuery = @"
                UPDATE reviews SET
                    rating = COALESCE(@rating, rating),
                    title = COALESCE(@title, title),
                    review_text = COALESCE(@review_text, review_text),
                    updated_at = CURRENT_TIMESTAMP
                WHERE id=@id AND user_id=@user_id
            ";

                    using (var cmd = new NpgsqlCommand(updateQuery, con))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        cmd.Parameters.AddWithValue("@user_id", userId);
                        cmd.Parameters.AddWithValue("@rating", (object)rating ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@title", (object)title ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@review_text", (object)reviewText ?? DBNull.Value);

                        await cmd.ExecuteNonQueryAsync();

                        return Ok(new
                        {
                            success = true,
                            message = "Review updated successfully!"
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = $"Internal server error: {ex.Message}"
                });
            }
        }

        public async Task<IActionResult> UpdateReviewByAdmin(int id, IFormCollection form)
        {
            try
            {
                string userEmail = form["email"];

                if (string.IsNullOrEmpty(userEmail))
                {
                    return BadRequest(new { success = false, message = "User email is required" });
                }

                using (var con = new NpgsqlConnection(DbConnection))
                {
                    await con.OpenAsync();

                    // ------------------------------------------
                    // 1️⃣ GET user_id FROM AspNetUsers BY EMAIL
                    // ------------------------------------------
                    string getUserQuery = @"SELECT ""Id"" FROM ""AspNetUsers"" WHERE ""Email"" = @Email LIMIT 1";

                    string userId = null;

                    using (var userCmd = new NpgsqlCommand(getUserQuery, con))
                    {
                        userCmd.Parameters.AddWithValue("@Email", userEmail);

                        var result = await userCmd.ExecuteScalarAsync();
                        if (result == null)
                        {
                            return BadRequest(new
                            {
                                success = false,
                                message = "User not found for given email"
                            });
                        }

                        userId = result.ToString();
                    }

                    // ------------------------------------------
                    // 2️⃣ CHECK IF REVIEW EXISTS & BELONGS TO USER
                    // ------------------------------------------
                    string checkQuery = @"SELECT COUNT(*) FROM reviews WHERE id=@id AND user_id=@user_id";

                    using (var checkCmd = new NpgsqlCommand(checkQuery, con))
                    {
                        checkCmd.Parameters.AddWithValue("@id", id);
                        checkCmd.Parameters.AddWithValue("@user_id", userId);

                        long exists = (long)await checkCmd.ExecuteScalarAsync();
                        if (exists == 0)
                        {
                            return BadRequest(new
                            {
                                success = false,
                                message = "Review not found or you are not authorized to update this review"
                            });
                        }
                    }

                    // ------------------------------------------
                    // 3️⃣ GET FORM FIELDS
                    // ------------------------------------------
                    string ratingStr = form["rating"];
                    string title = form["title"];
                    string reviewText = form["review_text"];

                    int? rating = null;

                    if (!string.IsNullOrEmpty(ratingStr))
                    {
                        if (!int.TryParse(ratingStr, out int parsedRating) || parsedRating < 1 || parsedRating > 5)
                        {
                            return BadRequest(new
                            {
                                success = false,
                                message = "Rating must be between 1 and 5"
                            });
                        }

                        rating = parsedRating;
                    }

                    // ------------------------------------------
                    // 4️⃣ UPDATE QUERY
                    // ------------------------------------------
                    string updateQuery = @"
                UPDATE reviews SET
                    rating = COALESCE(@rating, rating),
                    title = COALESCE(@title, title),
                    review_text = COALESCE(@review_text, review_text),
                    updated_at = CURRENT_TIMESTAMP
                WHERE id=@id AND user_id=@user_id
            ";

                    using (var cmd = new NpgsqlCommand(updateQuery, con))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        cmd.Parameters.AddWithValue("@user_id", userId);
                        cmd.Parameters.AddWithValue("@rating", (object)rating ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@title", (object)title ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@review_text", (object)reviewText ?? DBNull.Value);

                        await cmd.ExecuteNonQueryAsync();

                        return Ok(new
                        {
                            success = true,
                            message = "Review updated successfully!"
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = $"Internal server error: {ex.Message}"
                });
            }
        }

        public async Task<IActionResult> GetReviews(int courseId)
        {
            try
            {
                using (var con = new NpgsqlConnection(DbConnection))
                {
                    await con.OpenAsync();

                    string query = @"
                SELECT 
                    r.id,
                    r.rating,
                    r.title,
                    r.review_text,
                    r.created_at,
                    r.updated_at,
                    u.""FirstName"" AS first_name,
                    u.""LastName"" AS last_name,
                    u.""Email"" AS user_email
                FROM reviews r
                INNER JOIN ""AspNetUsers"" u ON r.user_id = u.""Id""
                WHERE r.course_id = @courseId
                AND r.is_active = TRUE
                ORDER BY r.created_at DESC
            ";

                    using (var cmd = new NpgsqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@courseId", courseId);

                        var reviews = new List<object>();

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                reviews.Add(new
                                {
                                    id = reader["id"],
                                    rating = reader["rating"],
                                    title = reader["title"],
                                    review_text = reader["review_text"],
                                    created_at = reader["created_at"],
                                    updated_at = reader["updated_at"],
                                    first_name = reader["first_name"],
                                    last_name = reader["last_name"],
                                    user_email = reader["user_email"]
                                });
                            }
                        }

                        return Ok(new
                        {
                            success = true,
                            message = "Reviews fetched successfully",
                            total = reviews.Count,
                            reviews
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = $"Internal server error: {ex.Message}"
                });
            }
        }

        public async Task<IActionResult> DeleteReview(int id)
        {
            try
            {
                using (var con = new NpgsqlConnection(DbConnection))
                {
                    await con.OpenAsync();

                    string query = "DELETE FROM reviews WHERE id = @id";

                    using (var cmd = new NpgsqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@id", id);

                        int rows = await cmd.ExecuteNonQueryAsync();

                        if (rows == 0)
                        {
                            return NotFound(new
                            {
                                success = false,
                                message = "Review not found"
                            });
                        }

                        return Ok(new
                        {
                            success = true,
                            message = "Review deleted successfully"
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Internal server error: " + ex.Message
                });
            }
        }

        public async Task<IActionResult> ToggleReview(int id)
        {
            try
            {
                using (var con = new NpgsqlConnection(DbConnection))
                {
                    await con.OpenAsync();

                    // First get current status
                    string selectQuery = "SELECT is_active FROM reviews WHERE id = @id";

                    bool? currentStatus = null;

                    using (var selectCmd = new NpgsqlCommand(selectQuery, con))
                    {
                        selectCmd.Parameters.AddWithValue("@id", id);

                        var result = await selectCmd.ExecuteScalarAsync();

                        if (result == null)
                        {
                            return NotFound(new
                            {
                                success = false,
                                message = "Review not found"
                            });
                        }

                        currentStatus = (bool)result;
                    }

                    // Toggle status
                    bool newStatus = !currentStatus.Value;

                    string updateQuery = @"
                UPDATE reviews 
                SET is_active = @newStatus, updated_at = CURRENT_TIMESTAMP
                WHERE id = @id
            ";

                    using (var updateCmd = new NpgsqlCommand(updateQuery, con))
                    {
                        updateCmd.Parameters.AddWithValue("@newStatus", newStatus);
                        updateCmd.Parameters.AddWithValue("@id", id);

                        await updateCmd.ExecuteNonQueryAsync();
                    }

                    return Ok(new
                    {
                        success = true,
                        message = "Review status updated successfully",
                        new_status = newStatus
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Internal server error: " + ex.Message
                });
            }
        }

        public async Task<IActionResult> getAllReviewByAdmin()
        {
            try
            {
                using (var con = new NpgsqlConnection(DbConnection))
                {
                    await con.OpenAsync();

                    string query = @"
                SELECT 
                    r.id,
                    r.rating,
                    r.title,
                    r.review_text,
                    r.created_at,
                    r.updated_at,
                    u.""FirstName"" AS first_name,
                    u.""LastName"" AS last_name,
                    u.""Email"" AS user_email
                FROM reviews r
                INNER JOIN ""AspNetUsers"" u 
                    ON r.user_id = u.""Id""
                WHERE r.is_active = TRUE
                ORDER BY r.created_at DESC
            ";

                    using (var cmd = new NpgsqlCommand(query, con))
                    {
                        var reviews = new List<object>();

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                reviews.Add(new
                                {
                                    id = reader["id"],
                                    rating = reader["rating"],
                                    title = reader["title"],
                                    review_text = reader["review_text"],
                                    created_at = reader["created_at"],
                                    updated_at = reader["updated_at"],
                                    first_name = reader["first_name"],
                                    last_name = reader["last_name"],
                                    user_email = reader["user_email"]
                                });
                            }
                        }

                        return Ok(new
                        {
                            success = true,
                            message = "Reviews fetched successfully",
                            total = reviews.Count,
                            reviews
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = $"Internal server error: {ex.Message}"
                });
            }
        }
    }
}
