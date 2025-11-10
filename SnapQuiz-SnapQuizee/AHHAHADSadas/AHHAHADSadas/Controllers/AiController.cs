using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace AHHAHADSadas.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AIController : ControllerBase
    {
        private readonly HttpClient _http;

        private readonly string _ocrKey = "YOUR_OCR.SPACE_API_KEY";
        private readonly string _geminiKey = "YOUR_OCR.SPACE_API_KEY";
        private readonly string _deepseekKey = "YOUR_OCR.SPACE_API_KEY";
        private readonly string _imagekitPrivate = "YOUR_OCR.SPACE_API_KEY";
        private readonly string _imagekitUrl = "YOUR_OCR.SPACE_API_KEY";

        public AIController(HttpClient http)
        {
            _http = http;
        }

        // ============================
        // 🧠 OCR SPACE - Extract text
        // ============================
        [HttpPost("ocr")]
        public async Task<IActionResult> ExtractText([FromBody] Base64Request req)
        {
            var form = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["base64Image"] = "data:image/jpeg;base64," + req.Base64,
                ["language"] = "eng",
                ["apikey"] = _ocrKey
            });

            var response = await _http.PostAsync("https://api.ocr.space/parse/image", form);
            var result = await response.Content.ReadAsStringAsync();
            return Content(result, "application/json");
        }

        // ============================
        // 🤖 GEMINI - Generate Q&A
        // ============================
        [HttpPost("gemini")]
        public async Task<IActionResult> AskGemini([FromBody] AIRequest req)
        {
            string promptText = req.QuestionType switch
            {
                "TrueFalse" => $"Based on the text below, generate exactly {req.QuestionCount} True/False statements. Return ONLY a JSON array with no additional text or markdown. Each object must have \"Question\" (the statement) and \"Answer\" (either \"True\" or \"False\") properties.\n\nExample format:\n[{{\"Question\":\"X is the capital of Y\",\"Answer\":\"True\"}},{{\"Question\":\"Z happened in 1990\",\"Answer\":\"False\"}}]\n\nTEXT:\n{req.Text}",
                "Define" => $"Based on the text below, generate exactly {req.QuestionCount} definition questions. Return ONLY a JSON array with no additional text or markdown. Each object must have \"Question\" (asking to define a term) and \"Answer\" (the definition) properties.\n\nExample format:\n[{{\"Question\":\"Define: Photosynthesis\",\"Answer\":\"The process by which plants...\"}},{{\"Question\":\"Define: Democracy\",\"Answer\":\"A system of government...\"}}]\n\nTEXT:\n{req.Text}",
                _ => $"Based on the text below, generate exactly {req.QuestionCount} question-answer pairs. Return ONLY a JSON array with no additional text or markdown. Each object must have \"Question\" and \"Answer\" properties (capital Q and A).\n\nExample format:\n[{{\"Question\":\"What is X?\",\"Answer\":\"X is...\"}},{{\"Question\":\"When is Y?\",\"Answer\":\"Y is...\"}}]\n\nTEXT:\n{req.Text}"
            };

            var body = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = promptText }
                        }
                    }
                }
            };

            var json = JsonSerializer.Serialize(body);
            var response = await _http.PostAsync(
                $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent?key={_geminiKey}",
                new StringContent(json, Encoding.UTF8, "application/json")
            );

            var result = await response.Content.ReadAsStringAsync();
            return Content(result, "application/json");
        }

        // ============================
        // 🧠 DEEPSEEK - Backup AI
        // ============================
        [HttpPost("deepseek")]
        public async Task<IActionResult> AskDeepseek([FromBody] AIRequest req)
        {
            string promptText = req.QuestionType switch
            {
                "TrueFalse" => $"Based on the text below, generate exactly {req.QuestionCount} True/False statements. Return ONLY a JSON array with no additional text or markdown. Each object must have \"Question\" (the statement) and \"Answer\" (either \"True\" or \"False\") properties.\n\nExample format:\n[{{\"Question\":\"X is the capital of Y\",\"Answer\":\"True\"}},{{\"Question\":\"Z happened in 1990\",\"Answer\":\"False\"}}]\n\nTEXT:\n{req.Text}",
                "Define" => $"Based on the text below, generate exactly {req.QuestionCount} definition questions. Return ONLY a JSON array with no additional text or markdown. Each object must have \"Question\" (asking to define a term) and \"Answer\" (the definition) properties.\n\nExample format:\n[{{\"Question\":\"Define: Photosynthesis\",\"Answer\":\"The process by which plants...\"}},{{\"Question\":\"Define: Democracy\",\"Answer\":\"A system of government...\"}}]\n\nTEXT:\n{req.Text}",
                _ => $"Based on the text below, generate exactly {req.QuestionCount} question-answer pairs. Return ONLY a JSON array with no additional text or markdown. Each object must have \"Question\" and \"Answer\" properties (capital Q and A).\n\nExample format:\n[{{\"Question\":\"What is X?\",\"Answer\":\"X is...\"}},{{\"Question\":\"When is Y?\",\"Answer\":\"Y is...\"}}]\n\nTEXT:\n{req.Text}"
            };

            var body = new
            {
                model = "deepseek-chat",
                messages = new[]
                {
                    new { role = "user", content = promptText }
                }
            };

            var json = JsonSerializer.Serialize(body);
            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.deepseek.com/v1/chat/completions");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _deepseekKey);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _http.SendAsync(request);
            var result = await response.Content.ReadAsStringAsync();
            return Content(result, "application/json");
        }

        // ============================
        // 🖼️ IMAGEKIT - Compress Upload
        // ============================
        [HttpPost("imagekit")]
        public async Task<IActionResult> UploadImage([FromForm] IFormFile file)
        {
            using var form = new MultipartFormDataContent();
            var fileContent = new StreamContent(file.OpenReadStream());
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
            form.Add(fileContent, "file", file.FileName);

            form.Add(new StringContent(file.FileName), "fileName");
            form.Add(new StringContent("true"), "useUniqueFileName");

            var request = new HttpRequestMessage(HttpMethod.Post, "https://upload.imagekit.io/api/v1/files/upload")
            {
                Content = form
            };
            var auth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_imagekitPrivate}:"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", auth);

            var response = await _http.SendAsync(request);
            var result = await response.Content.ReadAsStringAsync();
            return Content(result, "application/json");
        }

        // ============================
        // 🗑️ IMAGEKIT CLEANUP
        // ============================
        [HttpGet("cleanup-imagekit")]
        public async Task<IActionResult> CleanupImageKit()
        {
            try
            {
                // Get list of files from ImageKit
                var request = new HttpRequestMessage(HttpMethod.Get, "https://api.imagekit.io/v1/files");
                var authValue = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_imagekitPrivate}:"));
                request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authValue);

                var response = await _http.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    return StatusCode(500, new { message = "Failed to fetch ImageKit files" });
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(responseContent);

                var files = doc.RootElement.EnumerateArray();
                var cutoffTime = DateTime.UtcNow.AddHours(-5); // Delete files older than 5 hours

                var filesToDelete = new List<string>();

                foreach (var file in files)
                {
                    if (file.TryGetProperty("createdAt", out var createdAtProp) &&
                        file.TryGetProperty("fileId", out var fileIdProp))
                    {
                        var createdAtStr = createdAtProp.GetString();
                        var fileId = fileIdProp.GetString();

                        if (DateTime.TryParse(createdAtStr, out var createdAt) &&
                            createdAt < cutoffTime &&
                            !string.IsNullOrEmpty(fileId))
                        {
                            filesToDelete.Add(fileId);
                        }
                    }
                }

                // Delete old files
                foreach (var fileId in filesToDelete)
                {
                    await DeleteImageKitFile(fileId);
                }

                return Ok(new { message = $"Cleanup completed. Deleted {filesToDelete.Count} old files." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Cleanup error: {ex.Message}" });
            }
        }

        private async Task DeleteImageKitFile(string fileId)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Delete, $"https://api.imagekit.io/v1/files/{fileId}");
                var authValue = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_imagekitPrivate}:"));
                request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authValue);

                await _http.SendAsync(request);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting ImageKit file {fileId}: {ex.Message}");
            }
        }

        // ============================
        // 📦 Models
        // ============================
        public class Base64Request
        {
            public string Base64 { get; set; } = "";
        }

        public class AIRequest
        {
            public string Text { get; set; } = "";
            public int QuestionCount { get; set; } = 5;
            public string QuestionType { get; set; } = "Questions";
        }
    }
}
