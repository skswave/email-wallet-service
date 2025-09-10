using System.Text.Json;
using System.Text.RegularExpressions;

namespace EmailProcessingService.Utils
{
    public static class AbiConverter
    {
        public static string ConvertSignaturesToFullAbi(string[] signatures)
        {
            var abiItems = new List<object>();

            Console.WriteLine($"Starting ABI conversion with {signatures.Length} signatures...");

            foreach (var signature in signatures)
            {
                try
                {
                    var abiItem = ParseSignature(signature);
                    if (abiItem != null)
                    {
                        abiItems.Add(abiItem);
                        Console.WriteLine($"✓ Converted: {signature.Substring(0, Math.Min(50, signature.Length))}...");
                    }
                    else
                    {
                        Console.WriteLine($"✗ Skipped: {signature.Substring(0, Math.Min(50, signature.Length))}...");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"ERROR parsing signature '{signature.Substring(0, Math.Min(30, signature.Length))}...': {ex.Message}");
                }
            }

            Console.WriteLine($"Final ABI contains {abiItems.Count} items");

            return JsonSerializer.Serialize(abiItems, new JsonSerializerOptions 
            { 
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
        }

        private static object? ParseSignature(string signature)
        {
            signature = signature.Trim();

            // Skip signatures with complex tuple structures that cause parsing issues
            if (ContainsComplexTuple(signature))
            {
                Console.WriteLine($"Skipping complex signature: {signature.Substring(0, Math.Min(50, signature.Length))}...");
                return null;
            }

            if (signature.StartsWith("constructor"))
            {
                return ParseConstructor(signature);
            }
            else if (signature.StartsWith("event"))
            {
                return ParseEvent(signature);
            }
            else if (signature.StartsWith("function"))
            {
                return ParseFunction(signature);
            }

            return null;
        }

        private static bool ContainsComplexTuple(string signature)
        {
            // Only skip signatures with very complex nested structures
            // Allow simple functions like getCreditBalance, registrationFee, etc.
            
            // Skip complex tuple structures with multiple levels
            if (signature.Contains(") (") || // nested structures like func() returns (struct)
                signature.Count(c => c == '(') > 3) // too many nested levels
            {
                return true;
            }
            
            // Skip specific problematic patterns but allow simple functions
            var problematicPatterns = new[]
            {
                "emailWallets(",
                "attachmentWallets(",
                "processAuthorizedRequest",
                "getEmailWallet(", // has complex return type
                "getAttachmentWallet(", // has complex return type
                "authRequests(", // has complex struct
                "authorizations(", // has complex struct
            };
            
            return problematicPatterns.Any(pattern => signature.Contains(pattern));
        }

        private static object ParseConstructor(string signature)
        {
            // constructor(uint256 _registrationFee, address _initialOwner)
            var match = Regex.Match(signature, @"constructor\s*\(([^)]*)\)");
            
            var inputs = new List<object>();
            if (match.Success && !string.IsNullOrEmpty(match.Groups[1].Value))
            {
                inputs = ParseParameters(match.Groups[1].Value);
            }

            return new
            {
                Type = "constructor",
                Inputs = inputs,
                StateMutability = "nonpayable"
            };
        }

        private static object ParseEvent(string signature)
        {
            // event EmailWalletRegistered(address indexed wallet, bytes32 indexed registrationId, ...)
            var match = Regex.Match(signature, @"event\s+(\w+)\s*\(([^)]*)\)");
            
            if (!match.Success)
                throw new ArgumentException($"Invalid event signature: {signature}");

            var eventName = match.Groups[1].Value;
            var inputs = new List<object>();
            
            if (!string.IsNullOrEmpty(match.Groups[2].Value))
            {
                inputs = ParseEventParameters(match.Groups[2].Value);
            }

            return new
            {
                Type = "event",
                Name = eventName,
                Inputs = inputs,
                Anonymous = false
            };
        }

        private static object ParseFunction(string signature)
        {
            // function registerEmailWallet(...) payable returns (bytes32 registrationId)
            // function getCreditBalance(address wallet) view returns (uint256)
            // function addEmailBinding(string email)
            
            var match = Regex.Match(signature, @"function\s+(\w+)\s*\(([^)]*)\)(?:\s+(view|pure|payable|nonpayable))?(?:\s+returns\s*\(([^)]*)\))?");
            
            if (!match.Success)
                throw new ArgumentException($"Invalid function signature: {signature}");

            var functionName = match.Groups[1].Value;
            var stateMutability = match.Groups[3].Value;
            if (string.IsNullOrEmpty(stateMutability))
            {
                stateMutability = "nonpayable";
            }

            var inputs = new List<object>();
            if (!string.IsNullOrEmpty(match.Groups[2].Value))
            {
                inputs = ParseParameters(match.Groups[2].Value);
            }

            var outputs = new List<object>();
            if (!string.IsNullOrEmpty(match.Groups[4].Value))
            {
                outputs = ParseParameters(match.Groups[4].Value);
            }

            return new
            {
                Type = "function",
                Name = functionName,
                Inputs = inputs,
                Outputs = outputs,
                StateMutability = stateMutability
            };
        }

        private static List<object> ParseParameters(string paramString)
        {
            var parameters = new List<object>();
            
            if (string.IsNullOrWhiteSpace(paramString))
                return parameters;

            // Handle complex tuple types and arrays more carefully
            var parts = SplitParametersAdvanced(paramString);
            
            foreach (var part in parts)
            {
                var trimmed = part.Trim();
                if (string.IsNullOrEmpty(trimmed))
                    continue;

                // Check if this is a tuple parameter
                if (trimmed.StartsWith("(") && trimmed.Contains(")"))
                {
                    var tupleParam = ParseTupleParameter(trimmed);
                    if (tupleParam != null)
                    {
                        parameters.Add(tupleParam);
                        continue;
                    }
                }

                var spaceIndex = trimmed.LastIndexOf(' ');
                string type, name;
                
                if (spaceIndex > 0)
                {
                    type = trimmed.Substring(0, spaceIndex).Trim();
                    name = trimmed.Substring(spaceIndex + 1).Trim();
                }
                else
                {
                    type = trimmed;
                    name = "";
                }

                // Handle tuple types by converting to simplified types that Nethereum can understand
                type = SimplifyComplexType(type);

                parameters.Add(new
                {
                    Name = name,
                    Type = type,
                    InternalType = type
                });
            }

            return parameters;
        }

        private static object? ParseTupleParameter(string tupleString)
        {
            try
            {
                // Extract the tuple contents and the parameter name
                // Format: (bool spfPass, bool dkimValid, bool dmarcPass, string dkimSignature) authResults
                
                var match = Regex.Match(tupleString, @"\(([^)]+)\)\s+(\w+)");
                if (!match.Success)
                    return null;
                
                var tupleContents = match.Groups[1].Value;
                var paramName = match.Groups[2].Value;
                
                // Parse the tuple components
                var components = new List<object>();
                var parts = SplitParametersAdvanced(tupleContents);
                
                foreach (var part in parts)
                {
                    var trimmed = part.Trim();
                    if (string.IsNullOrEmpty(trimmed))
                        continue;
                        
                    var spaceIndex = trimmed.LastIndexOf(' ');
                    string type, name;
                    
                    if (spaceIndex > 0)
                    {
                        type = trimmed.Substring(0, spaceIndex).Trim();
                        name = trimmed.Substring(spaceIndex + 1).Trim();
                    }
                    else
                    {
                        type = trimmed;
                        name = "";
                    }
                    
                    components.Add(new
                    {
                        Name = name,
                        Type = type,
                        InternalType = type
                    });
                }
                
                return new
                {
                    Name = paramName,
                    Type = "tuple",
                    InternalType = "tuple",
                    Components = components
                };
            }
            catch
            {
                return null;
            }
        }

        private static List<object> ParseEventParameters(string paramString)
        {
            var parameters = new List<object>();
            
            if (string.IsNullOrWhiteSpace(paramString))
                return parameters;

            var parts = SplitParametersAdvanced(paramString);
            
            foreach (var part in parts)
            {
                var trimmed = part.Trim();
                if (string.IsNullOrEmpty(trimmed))
                    continue;

                bool indexed = false;
                if (trimmed.Contains("indexed"))
                {
                    indexed = true;
                    trimmed = trimmed.Replace("indexed", "").Trim();
                }

                var spaceIndex = trimmed.LastIndexOf(' ');
                string type, name;
                
                if (spaceIndex > 0)
                {
                    type = trimmed.Substring(0, spaceIndex).Trim();
                    name = trimmed.Substring(spaceIndex + 1).Trim();
                }
                else
                {
                    type = trimmed;
                    name = "";
                }

                // Handle tuple types by converting to simplified types that Nethereum can understand
                type = SimplifyComplexType(type);

                // Always include the Indexed property for event parameters
                parameters.Add(new
                {
                    Name = name,
                    Type = type,
                    InternalType = type,
                    Indexed = indexed
                });
            }

            return parameters;
        }

        private static List<string> SplitParametersAdvanced(string paramString)
        {
            var parts = new List<string>();
            var current = "";
            int parentheses = 0;
            int brackets = 0;
            bool inTuple = false;

            for (int i = 0; i < paramString.Length; i++)
            {
                char c = paramString[i];
                
                if (c == '(')
                {
                    parentheses++;
                    inTuple = true;
                }
                else if (c == ')')
                {
                    parentheses--;
                    if (parentheses == 0)
                        inTuple = false;
                }
                else if (c == '[')
                    brackets++;
                else if (c == ']')
                    brackets--;
                else if (c == ',' && parentheses == 0 && brackets == 0 && !inTuple)
                {
                    parts.Add(current.Trim());
                    current = "";
                    continue;
                }

                current += c;
            }

            if (!string.IsNullOrEmpty(current.Trim()))
                parts.Add(current.Trim());

            return parts;
        }

        private static string SimplifyComplexType(string type)
        {
            // Handle tuple types by parsing them properly for specific known tuples
            if (type.StartsWith("(") && type.Contains(")"))
            {
                // Check if this is the authResults tuple we know about
                if (type.Contains("bool") && type.Contains("string"))
                {
                    // This is likely the authResults tuple: (bool spfPass, bool dkimValid, bool dmarcPass, string dkimSignature)
                    return "tuple";  // Use proper tuple type for Nethereum
                }
                
                // For other tuple types, we'll use bytes32 as a placeholder
                return "bytes32";
            }

            // Handle array types
            if (type.EndsWith("[]"))
            {
                var baseType = type.Substring(0, type.Length - 2);
                baseType = SimplifyComplexType(baseType); // Recursively handle nested complex types
                return baseType + "[]";
            }

            // Return the type as-is for simple types
            return type;
        }
    }
}