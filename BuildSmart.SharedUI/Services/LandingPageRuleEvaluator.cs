using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace BuildSmart.SharedUI.Services
{
    public class LandingPageCopyRule
    {
        public string Condition { get; set; } = string.Empty;
        public string TitleKey { get; set; } = string.Empty;
        public string SubtitleKey { get; set; } = string.Empty;
        public string CtaKey { get; set; } = string.Empty;
        public string CtaAction { get; set; } = string.Empty;
        public bool ShowLoginPrompt { get; set; }
    }

    public static class LandingPageRuleEvaluator
    {
        public static List<LandingPageCopyRule> ParseRules(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return new List<LandingPageCopyRule>();
            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                return JsonSerializer.Deserialize<List<LandingPageCopyRule>>(json, options) ?? new List<LandingPageCopyRule>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LandingPageRuleEvaluator] Parse Error: {ex.Message}");
                return new List<LandingPageCopyRule>();
            }
        }

        public static bool EvaluateCondition(
            string condition, 
            bool isGuest, 
            bool hasProjects, 
            bool isTradesman, 
            bool isHomeowner,
            bool hasDraftProject,
            bool hasActiveProject)
        {
            if (string.IsNullOrWhiteSpace(condition)) return false;
            
            // Normalize condition string: remove spaces
            string normalized = condition.Replace(" ", "");
            
            // Split by OR ('||')
            var orParts = normalized.Split(new[] { "||" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var orPart in orParts)
            {
                // For each OR part, all AND parts must be true
                var andParts = orPart.Split(new[] { "&&" }, StringSplitOptions.RemoveEmptyEntries);
                bool orResult = true;
                foreach (var andPart in andParts)
                {
                    bool partValue = false;
                    string key = andPart;
                    bool negate = false;
                    if (key.StartsWith("!"))
                    {
                        negate = true;
                        key = key.Substring(1);
                    }
                    
                    if (string.Equals(key, "isGuest", StringComparison.OrdinalIgnoreCase))
                    {
                        partValue = isGuest;
                    }
                    else if (string.Equals(key, "hasProjects", StringComparison.OrdinalIgnoreCase))
                    {
                        partValue = hasProjects;
                    }
                    else if (string.Equals(key, "isTradesman", StringComparison.OrdinalIgnoreCase))
                    {
                        partValue = isTradesman;
                    }
                    else if (string.Equals(key, "isHomeowner", StringComparison.OrdinalIgnoreCase))
                    {
                        partValue = isHomeowner;
                    }
                    else if (string.Equals(key, "hasDraftProject", StringComparison.OrdinalIgnoreCase))
                    {
                        partValue = hasDraftProject;
                    }
                    else if (string.Equals(key, "hasActiveProject", StringComparison.OrdinalIgnoreCase))
                    {
                        partValue = hasActiveProject;
                    }
                    else
                    {
                        partValue = false;
                    }
                    
                    if (negate)
                    {
                        partValue = !partValue;
                    }
                    
                    if (!partValue)
                    {
                        orResult = false;
                        break;
                    }
                }
                
                if (orResult)
                {
                    return true;
                }
            }
            
            return false;
        }
    }
}
