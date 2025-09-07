using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EmailProcessingService.Services
{
    public interface IIpfsService
    {
        Task<IpfsUploadResult> UploadFileAsync(byte[] content, string fileName, Dictionary<string, object>? metadata = null);
        Task<IpfsUploadResult> UploadJsonAsync<T>(T data, string fileName);
        Task<byte[]> DownloadFileAsync(string ipfsHash);
        Task<T?> DownloadJsonAsync<T>(string ipfsHash);
        Task<bool> IsPinnedAsync(string ipfsHash);
        Task<bool> PinFileAsync(string ipfsHash);
        Task<IpfsMetadata> GetFileMetadataAsync(string ipfsHash);
        Task<bool> TestConnectionAsync();
        Task<IpfsValidationResult> ValidateIPFSProofAsync(string ipfsHash);
    }

    public class IpfsService : IIpfsService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<IpfsService> _logger;
        private readonly IpfsConfiguration _config;

        public IpfsService(HttpClient httpClient, ILogger<IpfsService> logger, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _logger = logger;
            _config = configuration.GetSection("IPFS").Get<IpfsConfiguration>() ?? new IpfsConfiguration();
            
            ConfigureHttpClient();
        }

        private void ConfigureHttpClient()
        {
            _httpClient.BaseAddress = new Uri(_config.ApiUrl);
            _httpClient.Timeout = TimeSpan.FromMinutes(_config.TimeoutMinutes);

            // Use JWT if available (preferred method)
            if (!string.IsNullOrEmpty(_config.PinataJWT))
            {
                _httpClient.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _config.PinataJWT);
                _logger.LogInformation("IPFS configured with Pinata JWT authentication");
            }
            // Fallback to API keys
            else if (!string.IsNullOrEmpty(_config.PinataApiKey) && !string.IsNullOrEmpty(_config.PinataSecretKey))
            {
                _httpClient.DefaultRequestHeaders.Add("pinata_api_key", _config.PinataApiKey);
                _httpClient.DefaultRequestHeaders.Add("pinata_secret_api_key", _config.PinataSecretKey);
                _logger.LogInformation("IPFS configured with Pinata API key authentication");
            }
            else
            {
                _logger.LogWarning("IPFS not configured - missing Pinata credentials");
            }
        }

        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                _logger.LogInformation("Testing IPFS connection to Pinata");
                var response = await _httpClient.GetAsync("https://api.pinata.cloud/data/testAuthentication");
                
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("IPFS connection test successful");
                    return true;
                }
                else
                {
                    _logger.LogWarning("IPFS connection test failed: {StatusCode}", response.StatusCode);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "IPFS connection test failed");
                return false;
            }
        }

        public async Task<IpfsUploadResult> UploadFileAsync(byte[] content, string fileName, Dictionary<string, object>? metadata = null)
        {
            try
            {
                _logger.LogInformation("Uploading file to IPFS: {FileName}, Size: {Size} bytes", fileName, content.Length);

                var formData = new MultipartFormDataContent();
                var fileContent = new ByteArrayContent(content);
                
                fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
                formData.Add(fileContent, "file", fileName);

                // Add Pinata metadata if provided (Pinata requires string/number values only)
                if (metadata != null || !string.IsNullOrEmpty(fileName))
                {
                    // Convert metadata to Pinata-compatible format (strings/numbers only)
                    var pinataKeyValues = new Dictionary<string, string>();
                    
                    if (metadata != null)
                    {
                        foreach (var kvp in metadata)
                        {
                            // Convert all values to strings for Pinata compatibility
                            pinataKeyValues[kvp.Key] = kvp.Value?.ToString() ?? "";
                        }
                    }
                    
                    // Add default metadata as strings
                    if (!pinataKeyValues.ContainsKey("fileName"))
                        pinataKeyValues["fileName"] = fileName;
                    if (!pinataKeyValues.ContainsKey("uploadedAt"))
                        pinataKeyValues["uploadedAt"] = DateTime.UtcNow.ToString("O");
                    if (!pinataKeyValues.ContainsKey("source"))
                        pinataKeyValues["source"] = "EmailDataWalletService";
                    if (!pinataKeyValues.ContainsKey("fileSize"))
                        pinataKeyValues["fileSize"] = content.Length.ToString();

                    var pinataMetadata = new
                    {
                        name = fileName,
                        keyvalues = pinataKeyValues
                    };

                    var metadataJson = JsonSerializer.Serialize(pinataMetadata);
                    formData.Add(new StringContent(metadataJson), "pinataMetadata");
                }

                // Add Pinata options for better performance
                var pinataOptions = new
                {
                    cidVersion = 1
                };
                var optionsJson = JsonSerializer.Serialize(pinataOptions);
                formData.Add(new StringContent(optionsJson), "pinataOptions");

                var response = await _httpClient.PostAsync("/pinning/pinFileToIPFS", formData);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"IPFS upload failed: {response.StatusCode} - {errorContent}");
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<JsonElement>(responseContent);

                var ipfsHash = result.GetProperty("IpfsHash").GetString();
                var pinSize = result.GetProperty("PinSize").GetInt64();

                var uploadResult = new IpfsUploadResult
                {
                    Success = true,
                    IpfsHash = ipfsHash,
                    FileName = fileName,
                    FileSize = content.Length,
                    UploadedAt = DateTime.UtcNow,
                    GatewayUrl = $"{_config.GatewayUrl}/{ipfsHash}",
                    Metadata = metadata,
                    PinSize = pinSize
                };

                _logger.LogInformation("Successfully uploaded file to IPFS: {Hash}, Pin Size: {PinSize}", ipfsHash, pinSize);
                return uploadResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upload file to IPFS: {FileName}", fileName);
                return new IpfsUploadResult
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                    FileName = fileName,
                    FileSize = content.Length,
                    UploadedAt = DateTime.UtcNow
                };
            }
        }

        public async Task<IpfsUploadResult> UploadJsonAsync<T>(T data, string fileName)
        {
            try
            {
                var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
                var content = Encoding.UTF8.GetBytes(json);

                var metadata = new Dictionary<string, object>
                {
                    { "contentType", "application/json" },
                    { "dataType", typeof(T).Name },
                    { "uploadType", "json" },
                    { "emailWalletService", "v2.0" }
                };

                return await UploadFileAsync(content, fileName, metadata);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upload JSON to IPFS: {FileName}", fileName);
                return new IpfsUploadResult
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                    FileName = fileName,
                    UploadedAt = DateTime.UtcNow
                };
            }
        }

        public async Task<byte[]> DownloadFileAsync(string ipfsHash)
        {
            try
            {
                _logger.LogInformation("Downloading file from IPFS: {Hash}", ipfsHash);

                // Try multiple gateways for reliability
                var gateways = new[]
                {
                    $"{_config.GatewayUrl}/{ipfsHash}",
                    $"https://ipfs.io/ipfs/{ipfsHash}",
                    $"https://cloudflare-ipfs.com/ipfs/{ipfsHash}",
                    $"https://dweb.link/ipfs/{ipfsHash}"
                };

                foreach (var gateway in gateways)
                {
                    try
                    {
                        var response = await _httpClient.GetAsync(gateway);
                        if (response.IsSuccessStatusCode)
                        {
                            var content = await response.Content.ReadAsByteArrayAsync();
                            _logger.LogInformation("Successfully downloaded file from IPFS via {Gateway}: {Hash}, Size: {Size} bytes", 
                                gateway, ipfsHash, content.Length);
                            return content;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to download from gateway {Gateway}", gateway);
                    }
                }

                throw new Exception("Failed to download from all available IPFS gateways");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to download file from IPFS: {Hash}", ipfsHash);
                throw;
            }
        }

        public async Task<T?> DownloadJsonAsync<T>(string ipfsHash)
        {
            try
            {
                var content = await DownloadFileAsync(ipfsHash);
                var json = Encoding.UTF8.GetString(content);
                return JsonSerializer.Deserialize<T>(json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to download and parse JSON from IPFS: {Hash}", ipfsHash);
                throw;
            }
        }

        public async Task<bool> IsPinnedAsync(string ipfsHash)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/data/pinList?hashContains={ipfsHash}");
                
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    
                    if (result.TryGetProperty("rows", out var rows))
                    {
                        return rows.GetArrayLength() > 0;
                    }
                }
                
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check pin status for IPFS hash: {Hash}", ipfsHash);
                return false;
            }
        }

        public async Task<bool> PinFileAsync(string ipfsHash)
        {
            try
            {
                _logger.LogInformation("Pinning file to IPFS: {Hash}", ipfsHash);

                var pinData = new
                {
                    hashToPin = ipfsHash,
                    pinataMetadata = new
                    {
                        name = $"EmailWallet-{ipfsHash}",
                        keyvalues = new
                        {
                            service = "EmailDataWallet",
                            pinnedAt = DateTime.UtcNow.ToString("O")
                        }
                    }
                };

                var jsonContent = JsonSerializer.Serialize(pinData);
                var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/pinning/pinByHash", httpContent);
                
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Successfully pinned file to IPFS: {Hash}", ipfsHash);
                    return true;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Failed to pin file to IPFS: {Hash}, Error: {Error}", ipfsHash, errorContent);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to pin file to IPFS: {Hash}", ipfsHash);
                return false;
            }
        }

        public async Task<IpfsMetadata> GetFileMetadataAsync(string ipfsHash)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/data/pinList?hashContains={ipfsHash}");
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<JsonElement>(responseContent);

                if (result.TryGetProperty("rows", out var rows) && rows.GetArrayLength() > 0)
                {
                    var firstRow = rows[0];
                    return new IpfsMetadata
                    {
                        Hash = ipfsHash,
                        Size = firstRow.TryGetProperty("size", out var sizeElement) ? sizeElement.GetInt64() : 0,
                        PinnedAt = firstRow.TryGetProperty("date_pinned", out var dateElement) 
                            ? DateTime.Parse(dateElement.GetString() ?? DateTime.UtcNow.ToString()) 
                            : DateTime.UtcNow,
                        RetrievedAt = DateTime.UtcNow
                    };
                }

                return new IpfsMetadata
                {
                    Hash = ipfsHash,
                    RetrievedAt = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get metadata for IPFS hash: {Hash}", ipfsHash);
                throw;
            }
        }

        public async Task<IpfsValidationResult> ValidateIPFSProofAsync(string ipfsHash)
        {
            try
            {
                _logger.LogInformation("Validating IPFS proof: {Hash}", ipfsHash);

                // Download the content
                var content = await DownloadFileAsync(ipfsHash);
                var textContent = Encoding.UTF8.GetString(content);

                var validation = new IpfsValidationResult
                {
                    Success = true,
                    IPFSHash = ipfsHash,
                    RawContent = textContent,
                    RetrievedAt = DateTime.UtcNow
                };

                // Check if content is JSON
                JsonElement proofData;
                bool isJsonContent = false;
                
                try
                {
                    proofData = JsonSerializer.Deserialize<JsonElement>(textContent);
                    isJsonContent = true;
                    _logger.LogInformation("Content detected as JSON proof structure");
                }
                catch (JsonException)
                {
                    _logger.LogInformation("Content detected as plain text, not a JSON proof structure");
                    // Handle plain text content
                    validation.ValidationResults = new List<string>
                    {
                        "ℹ️ Content is plain text, not a JSON proof structure",
                        $"✓ Content successfully retrieved from IPFS",
                        $"✓ Content size: {content.Length} bytes",
                        $"✓ Content preview: {(textContent.Length > 50 ? textContent.Substring(0, 50) + "..." : textContent)}"
                    };
                    validation.IsValid = true; // Plain text is valid content
                    validation.ContentVerified = true;
                    return validation;
                }

                // Process JSON proof structure  
                var validationResults = new List<string>();

                // Validate structure and extract data
                if (proofData.TryGetProperty("userKey", out var userKeyProp))
                {
                    validation.UserKey = userKeyProp.GetString();
                    validationResults.Add("✓ User key present");
                }
                else
                {
                    validationResults.Add("✗ Missing user key");
                }

                if (proofData.TryGetProperty("walletAddress", out var walletProp))
                {
                    validation.WalletAddress = walletProp.GetString();
                    if (validation.WalletAddress?.StartsWith("0x") == true && validation.WalletAddress.Length == 42)
                    {
                        validationResults.Add("✓ Valid wallet address format");
                    }
                    else
                    {
                        validationResults.Add("✗ Invalid wallet address format");
                    }
                }

                if (proofData.TryGetProperty("contentHash", out var contentHashProp))
                {
                    validation.ContentHash = contentHashProp.GetString();
                    
                    // Verify content hash matches content
                    if (proofData.TryGetProperty("claudeResponse", out var responseProp))
                    {
                        var responseContent = responseProp.GetString();
                        var calculatedHash = CalculateContentHash(responseContent);
                        
                        if (validation.ContentHash == calculatedHash)
                        {
                            validationResults.Add("✓ Content hash verified - authentic");
                            validation.ContentVerified = true;
                        }
                        else
                        {
                            validationResults.Add("✗ Content hash mismatch - possible tampering");
                            validation.ContentVerified = false;
                        }
                    }
                }

                // Validate deterministic wallet generation (using MVP logic)
                if (!string.IsNullOrEmpty(validation.UserKey) && !string.IsNullOrEmpty(validation.WalletAddress))
                {
                    var expectedWallet = GenerateWalletFromKey(validation.UserKey);
                    if (validation.WalletAddress == expectedWallet)
                    {
                        validationResults.Add("✓ Wallet address matches deterministic generation");
                        validation.WalletVerified = true;
                    }
                    else
                    {
                        validationResults.Add("✗ Wallet address mismatch");
                        validation.WalletVerified = false;
                    }
                }

                validation.ValidationResults = validationResults;
                validation.IsValid = validation.ContentVerified && validation.WalletVerified;

                _logger.LogInformation("IPFS proof validation completed: {Hash}, Valid: {IsValid}", ipfsHash, validation.IsValid);
                return validation;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to validate IPFS proof: {Hash}", ipfsHash);
                return new IpfsValidationResult
                {
                    Success = false,
                    Error = $"Validation failed: {ex.Message}"
                };
            }
        }

        private string CalculateContentHash(string content)
        {
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(content));
            return "0x" + Convert.ToHexString(hash).ToLowerInvariant();
        }

        private string GenerateWalletFromKey(string userKey)
        {
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var input = $"{userKey}rivetz-deterministic-v2";
            var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
            var addressBytes = new byte[20];
            Array.Copy(hash, hash.Length - 20, addressBytes, 0, 20);
            return "0x" + Convert.ToHexString(addressBytes).ToLowerInvariant();
        }
    }

    // Enhanced configuration class
    public class IpfsConfiguration
    {
        public string ApiUrl { get; set; } = "https://api.pinata.cloud";
        public string GatewayUrl { get; set; } = "https://gateway.pinata.cloud/ipfs";
        public string? PinataApiKey { get; set; }
        public string? PinataSecretKey { get; set; }
        public string? PinataJWT { get; set; }
        public bool BackupPinning { get; set; } = true;
        public long MaxUploadSize { get; set; } = 104857600; // 100MB
        public int TimeoutMinutes { get; set; } = 5;
        public List<string> AlternativeGateways { get; set; } = new();
    }

    // Enhanced result classes
    public class IpfsUploadResult
    {
        public bool Success { get; set; }
        public string? IpfsHash { get; set; }
        public string? GatewayUrl { get; set; }
        public string FileName { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public long PinSize { get; set; }
        public DateTime UploadedAt { get; set; }
        public string? ErrorMessage { get; set; }
        public Dictionary<string, object>? Metadata { get; set; }
    }

    public class IpfsMetadata
    {
        public string Hash { get; set; } = string.Empty;
        public long Size { get; set; }
        public int Links { get; set; }
        public long BlockSize { get; set; }
        public DateTime PinnedAt { get; set; }
        public DateTime RetrievedAt { get; set; }
    }

    public class IpfsValidationResult
    {
        public bool Success { get; set; }
        public string? Error { get; set; }
        public string IPFSHash { get; set; } = string.Empty;
        public string? GatewayUsed { get; set; }
        public string? RawContent { get; set; }
        public DateTime RetrievedAt { get; set; }
        
        // Extracted data
        public string? UserKey { get; set; }
        public string? WalletAddress { get; set; }
        public string? TransactionHash { get; set; }
        public string? ContentHash { get; set; }
        public string? Version { get; set; }
        public DateTime? ProofTimestamp { get; set; }
        
        // Validation results
        public bool IsValid { get; set; }
        public bool ContentVerified { get; set; }
        public bool WalletVerified { get; set; }
        public List<string> ValidationResults { get; set; } = new();
    }
}