using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace CareerCracker.DataBaseLayer
{
    public interface IDataBaseLayer_Languages
    {
        Task<IActionResult> InsertLanguage(IFormCollection form);
        Task<IActionResult> ToggleLanguageStatus(int id);
        Task<IActionResult> DeleteLanguage(int id);
        Task<IActionResult> UpdateLanguage(int id, IFormCollection form);
        Task<IActionResult> GetLanguageById(int id);
        Task<IActionResult> GetAllLanguages();
    }

    public partial interface IDataBaseLayer : IDataBaseLayer_Languages { }

    public partial class DataBaseLayer
    {
        public async Task<IActionResult> InsertLanguage(IFormCollection form)
        {
            try
            {
                string name = form["language_name"];
                string description = form["language_discription"];
                string slug = GenerateSlug(name);
                bool isActive = form.ContainsKey("is_active") && form["is_active"] == "true";

                if (string.IsNullOrWhiteSpace(name))
                    return BadRequest(new { success = false, message = "Language name is required" });

                using (var con = new NpgsqlConnection(DbConnection))
                {
                    await con.OpenAsync();

                    // 1️⃣ Check if language already exists
                    string checkQuery = "SELECT COUNT(*) FROM languages WHERE LOWER(language_name) = LOWER(@name)";

                    using (var checkCmd = new NpgsqlCommand(checkQuery, con))
                    {
                        checkCmd.Parameters.AddWithValue("@name", name);

                        long exists = (long)await checkCmd.ExecuteScalarAsync();

                        if (exists > 0)
                        {
                            return Conflict(new
                            {
                                success = false,
                                message = "Language name already exists"
                            });
                        }
                    }

                    // 2️⃣ Insert New Language
                    string insertQuery = @"
                INSERT INTO languages 
                (language_name, language_discription, language_slug, is_active, updated_at)
                VALUES 
                (@name, @desc, @slug, @active, CURRENT_TIMESTAMP)
                RETURNING id;
            ";

                    using (var cmd = new NpgsqlCommand(insertQuery, con))
                    {
                        cmd.Parameters.AddWithValue("@name", name);
                        cmd.Parameters.AddWithValue("@desc", (object)description ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@slug", (object)slug ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@active", isActive);

                        int newId = (int)await cmd.ExecuteScalarAsync();

                        return Ok(new
                        {
                            success = true,
                            message = "Language added successfully",
                            id = newId
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        public async Task<IActionResult> ToggleLanguageStatus(int id)
        {
            try
            {
                using (var con = new NpgsqlConnection(DbConnection))
                {
                    await con.OpenAsync();

                    // check current status
                    string checkQuery = "SELECT is_active FROM languages WHERE id = @id";

                    bool? current = null;
                    using (var checkCmd = new NpgsqlCommand(checkQuery, con))
                    {
                        checkCmd.Parameters.AddWithValue("@id", id);
                        var result = await checkCmd.ExecuteScalarAsync();

                        if (result == null)
                            return NotFound(new { success = false, message = "Language not found" });

                        current = (bool)result;
                    }

                    bool newStatus = !current.Value;

                    string updateQuery = @"
                        UPDATE languages 
                        SET is_active = @status, updated_at = CURRENT_TIMESTAMP 
                        WHERE id = @id;
                    ";

                    using (var updateCmd = new NpgsqlCommand(updateQuery, con))
                    {
                        updateCmd.Parameters.AddWithValue("@status", newStatus);
                        updateCmd.Parameters.AddWithValue("@id", id);

                        await updateCmd.ExecuteNonQueryAsync();
                    }

                    return Ok(new
                    {
                        success = true,
                        message = "Status updated successfully",
                        newStatus
                    });
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        public async Task<IActionResult> DeleteLanguage(int id)
        {
            try
            {
                using (var con = new NpgsqlConnection(DbConnection))
                {
                    await con.OpenAsync();

                    string query = "DELETE FROM languages WHERE id = @id";

                    using (var cmd = new NpgsqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@id", id);

                        int rows = await cmd.ExecuteNonQueryAsync();

                        if (rows == 0)
                            return NotFound(new { success = false, message = "Language not found" });

                        return Ok(new { success = true, message = "Language deleted successfully" });
                    }
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        public async Task<IActionResult> UpdateLanguage(int id, IFormCollection form)
        {
            try
            {
                string name = form["language_name"];
                string description = form["language_discription"];
                string slug = GenerateSlug(name);
                bool isActive = form.ContainsKey("is_active") && form["is_active"] == "true";

                using (var con = new NpgsqlConnection(DbConnection))
                {
                    await con.OpenAsync();

                    string query = @"
                        UPDATE languages
                        SET language_name = @name,
                            language_discription = @desc,
                            language_slug = @slug,
                            is_active = @active,
                            updated_at = CURRENT_TIMESTAMP
                        WHERE id = @id;
                    ";

                    using (var cmd = new NpgsqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@name", name);
                        cmd.Parameters.AddWithValue("@desc", (object)description ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@slug", (object)slug ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@active", isActive);
                        cmd.Parameters.AddWithValue("@id", id);

                        int rows = await cmd.ExecuteNonQueryAsync();

                        if (rows == 0)
                            return NotFound(new { success = false, message = "Language not found" });

                        return Ok(new { success = true, message = "Language updated successfully" });
                    }
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        public async Task<IActionResult> GetLanguageById(int id)
        {
            try
            {
                using (var con = new NpgsqlConnection(DbConnection))
                {
                    await con.OpenAsync();

                    string query = @"
                        SELECT id, language_name, language_discription, language_slug,
                               is_active, updated_at
                        FROM languages WHERE id = @id;
                    ";

                    using (var cmd = new NpgsqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@id", id);

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (!reader.HasRows)
                                return NotFound(new { success = false, message = "Language not found" });

                            await reader.ReadAsync();

                            return Ok(new
                            {
                                success = true,
                                data = new
                                {
                                    id = reader["id"],
                                    language_name = reader["language_name"],
                                    language_discription = reader["language_discription"],
                                    language_slug = reader["language_slug"],
                                    is_active = reader["is_active"],
                                    updated_at = reader["updated_at"]
                                }
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        public async Task<IActionResult> GetAllLanguages()
        {
            try
            {
                using (var con = new NpgsqlConnection(DbConnection))
                {
                    await con.OpenAsync();

                    string query = @"
                        SELECT id, language_name, language_discription, language_slug,
                               is_active, updated_at
                        FROM languages ORDER BY id DESC;
                    ";

                    using (var cmd = new NpgsqlCommand(query, con))
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        var list = new List<object>();

                        while (await reader.ReadAsync())
                        {
                            list.Add(new
                            {
                                id = reader["id"],
                                language_name = reader["language_name"],
                                language_discription = reader["language_discription"],
                                language_slug = reader["language_slug"],
                                is_active = reader["is_active"],
                                updated_at = reader["updated_at"]
                            });
                        }

                        return Ok(new { success = true, data = list });
                    }
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }


    }
}
