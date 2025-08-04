using System.Text;

namespace SampleAppWithDeadCode.Utilities;

public static class StringHelper
{
    // Used method
    public static string Reverse(string input)
    {
        if (string.IsNullOrEmpty(input))
        { 
            return input; 
        }

        char[] chars = input.ToCharArray();
        Array.Reverse(chars);
        
        return new string(chars);
    }
    
    // DEAD CODE: Never called
    public static string ToCamelCase(string input)
    {
        if (string.IsNullOrEmpty(input))
        { 
            return input; 
        }

        string[] parts = input.Split(' ', '-', '_');
        StringBuilder result = new StringBuilder(parts[0].ToLower());
        
        for (int i = 1; i < parts.Length; i++)
        {
            if (parts[i].Length > 0)
            {
                result.Append(char.ToUpper(parts[i][0]));
                result.Append(parts[i].Substring(1).ToLower());
            }
        }
        
        return result.ToString();
    }
    
    // DEAD CODE: Never called
    public static bool IsPalindrome(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return true;
        }

        string cleaned = text.Replace(" ", "").ToLower();
        string reversed = Reverse(cleaned);
        
        return cleaned == reversed;
    }
    
    // DEAD CODE: Extension method never used
    public static string Truncate(this string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value))
        { 
            return value; 
        }
            
        return value.Length <= maxLength ? value : value.Substring(0, maxLength) + "...";
    }
    
    // DEAD CODE: Generic method never instantiated
    public static string JoinWithSeparator<T>(IEnumerable<T> items, string separator)
    {
        return string.Join(separator, items);
    }
}