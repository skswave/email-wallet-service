using EmailProcessingService.Utils;
using System.Text.Json;

namespace EmailProcessingService.Tests
{
    public class AbiConversionTest
    {
        public static void TestAbiConversion()
        {
            try
            {
                Console.WriteLine("Testing ABI Conversion...");
                
                // Load your actual EmailWalletRegistration ABI
                var abiPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "abis", "EmailWalletRegistration.json");
                
                if (File.Exists(abiPath))
                {
                    var abiContent = File.ReadAllText(abiPath);
                    var signatures = JsonSerializer.Deserialize<string[]>(abiContent);
                    
                    if (signatures != null && signatures.Length > 0)
                    {
                        Console.WriteLine($"Found {signatures.Length} function signatures");
                        
                        // Convert to full ABI
                        var fullAbi = AbiConverter.ConvertSignaturesToFullAbi(signatures);
                        
                        Console.WriteLine("Conversion successful!");
                        Console.WriteLine("Sample converted ABI (first 500 chars):");
                        Console.WriteLine(fullAbi.Substring(0, Math.Min(500, fullAbi.Length)) + "...");
                        
                        // Save converted ABI for inspection
                        var outputPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "abis", "EmailWalletRegistration_Converted.json");
                        File.WriteAllText(outputPath, fullAbi);
                        Console.WriteLine($"Full converted ABI saved to: {outputPath}");
                    }
                    else
                    {
                        Console.WriteLine("No signatures found in ABI file");
                    }
                }
                else
                {
                    Console.WriteLine($"ABI file not found at: {abiPath}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during ABI conversion test: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
    }
}