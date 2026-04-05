using System.Text.RegularExpressions;
using DittoMeOff.Models;

namespace DittoMeOff.Services;

/// <summary>
/// Detects the format type of text content (JSON, XML, code languages, etc.)
/// </summary>
public static class ContentFormatDetector
{
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromMilliseconds(100);

    private static readonly Regex JsonObjectPattern = new Regex(@"^\s*[\[{]", RegexOptions.Compiled, RegexTimeout);
    private static readonly Regex JsonArrayPattern = new Regex(@"^\s*\[", RegexOptions.Compiled, RegexTimeout);
    private static readonly Regex XmlPattern = new Regex(@"^\s*<[\w:]+\s*[^>]*>.*?</[\w:]+>|<[\w:]+\s*/\s*>", RegexOptions.Compiled | RegexOptions.Singleline, RegexTimeout);
    private static readonly Regex XmlDeclarationPattern = new Regex(@"^\s*<\?xml", RegexOptions.Compiled | RegexOptions.IgnoreCase, RegexTimeout);

    // Code language patterns — all with timeouts to prevent ReDoS on large clipboard content
    private static readonly (string Name, ContentFormatType Format, Regex Pattern)[] CodePatterns = new (string, ContentFormatType, Regex)[]
    {
        ("CSharp", ContentFormatType.CSharp, new Regex(@"\b(class|struct|interface|namespace|using|public|private|protected|internal|virtual|override|async|await|var\s+\w+\s*=|\.Select\(|\.Where\(|\.FirstOrDefault\))\b", RegexOptions.Compiled, RegexTimeout)),
        ("JavaScript", ContentFormatType.JavaScript, new Regex(@"\b(const|let|var|function|=>|console\.|require\(|module\.exports|import\s+.*\s+from)\b", RegexOptions.Compiled, RegexTimeout)),
        ("TypeScript", ContentFormatType.TypeScript, new Regex(@"\b(interface\s+\w+|type\s+\w+\s*=|:\s*(string|number|boolean|any|void)\b|\<[\w,]+\>)\b", RegexOptions.Compiled, RegexTimeout)),
        ("Python", ContentFormatType.Python, new Regex(@"\b(def\s+\w+\s*\(|import\s+\w+|from\s+\w+\s+import|print\(|if\s+__name__|elif\s+|self\.|lambda\s+)", RegexOptions.Compiled, RegexTimeout)),
        ("Java", ContentFormatType.Java, new Regex(@"\b(public\s+class|private\s+\w+\s+\w+;|System\.out\.|import\s+java\.|extends\s+\w+|@Override)\b", RegexOptions.Compiled, RegexTimeout)),
        ("Cpp", ContentFormatType.Cpp, new Regex(@"\b(#include\s*<|std::|cout\s*<<|cin\s*>>|nullptr|template\s*<|::\w+)\b", RegexOptions.Compiled, RegexTimeout)),
        ("Sql", ContentFormatType.Sql, new Regex(@"\b(SELECT\s+.*\s+FROM|INSERT\s+INTO|UPDATE\s+.*\s+SET|DELETE\s+FROM|CREATE\s+TABLE|ALTER\s+TABLE|JOIN\s+)\b", RegexOptions.Compiled | RegexOptions.IgnoreCase, RegexTimeout)),
        ("Bash", ContentFormatType.Bash, new Regex(@"(^#!/bin/bash|^\s*\$\s*|echo\s+|grep\s+|awk\s+|sed\s+|chmod\s+|export\s+\w+=)", RegexOptions.Compiled, RegexTimeout)),
        ("PowerShell", ContentFormatType.PowerShell, new Regex(@"\b(\$\w+|Write-Host|Get-Item|Set-Item|ForEach-Object|Where-Object|-eq\s|-ne\s|Get-ChildItem)\b", RegexOptions.Compiled, RegexTimeout)),
        ("HTML", ContentFormatType.HtmlCode, new Regex(@"<\s*(html|head|body|div|span|p|a|ul|li|table|tr|td|form|input|script|style)\b", RegexOptions.Compiled | RegexOptions.IgnoreCase, RegexTimeout)),
        ("Css", ContentFormatType.Css, new Regex(@"\{[^{}]{0,500}?(?:color:|background:|margin:|padding:|font-size:|display:|position:|width:|height:)", RegexOptions.Compiled, RegexTimeout)),
        ("Ruby", ContentFormatType.Ruby, new Regex(@"\b(def\s+\w+|end\s*$|puts\s+|require\s+|attr_accessor\b|@@\w+)\b", RegexOptions.Compiled, RegexTimeout)),
        ("Go", ContentFormatType.Go, new Regex(@"\b(package\s+main|func\s+\w+\(|fmt\.(Print|Scan)|go\s+\w+|chan\s+|interface\s*\{)\b", RegexOptions.Compiled, RegexTimeout)),
        ("Rust", ContentFormatType.Rust, new Regex(@"\b(fn\s+\w+|let\s+mut|impl\s+\w+|pub\s+fn|->|::new\(\)|println!|vec!|Option<|Result<)\b", RegexOptions.Compiled, RegexTimeout)),
        ("PHP", ContentFormatType.Php, new Regex(@"(<\?php|\$\w+\s*=|\bfunction\s+\w+\(|echo\s+|require(_once)?\(|->\w+\(\))", RegexOptions.Compiled, RegexTimeout)),
        ("Swift", ContentFormatType.Swift, new Regex(@"\b(func\s+\w+|var\s+\w+:|let\s+\w+:|struct\s+\w+|class\s+\w+|guard\s+let|if\s+let|import\s+Foundation|import\s+UIKit)\b", RegexOptions.Compiled, RegexTimeout)),
        ("Kotlin", ContentFormatType.Kotlin, new Regex(@"\b(fun\s+\w+|val\s+\w+|var\s+\w+:|data\s+class|object\s+\w+|companion\s+object|sealed\s+class)\b", RegexOptions.Compiled, RegexTimeout)),
        ("Yaml", ContentFormatType.Yaml, new Regex(@"^(?!\s*https?://|ftp://|file://)[\w-]+:\s+[""']?\S|^\s*-\s+\w+:|^\s*\w+:\s*$", RegexOptions.Compiled | RegexOptions.Multiline, RegexTimeout)),
        ("Markdown", ContentFormatType.Markdown, new Regex(@"(^#{1,6}\s+|^\*\s+\w+|^\d+\.\s+\w+|\*\*\w+\*\*|```|```\w+|\[.+\]\(.+\)|!\[.+\]\(.+\))", RegexOptions.Compiled, RegexTimeout))
    };

    /// <summary>
    /// Detects the format type of the given text content
    /// </summary>
    public static ContentFormatType Detect(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return ContentFormatType.PlainText;

        var trimmed = text.TrimStart();

        // Check for URL (before other patterns, as URLs can contain colons that match YAML)
        if (IsUrl(trimmed))
            return ContentFormatType.Url;

        // Check for JSON first (most common structured format)
        if (IsJson(trimmed))
            return ContentFormatType.Json;

        // Check for XML
        if (IsXml(trimmed))
            return ContentFormatType.Xml;

        // Check for HTML (before other code, as HTML has distinct tags)
        if (IsHtml(trimmed))
            return ContentFormatType.Html;

        // Check for code patterns
        foreach (var (name, format, pattern) in CodePatterns)
        {
            if (pattern.IsMatch(text))
                return format;
        }

        return ContentFormatType.PlainText;
    }

    private static bool IsUrl(string text)
    {
        // Single-line content that starts with a common URL scheme
        var trimmed = text.Trim();
        if (trimmed.Contains('\n'))
            return false;

        return trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("ftp://", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("ftps://", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("www.", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsJson(string text)
    {
        // Quick heuristics: should start with { or [
        if (!JsonObjectPattern.IsMatch(text) && !JsonArrayPattern.IsMatch(text))
            return false;

        // Try to parse it
        try
        {
            // Simple check for balanced braces/brackets
            int braces = 0, brackets = 0;
            bool inString = false;
            bool escaped = false;

            foreach (char c in text)
            {
                if (escaped)
                {
                    escaped = false;
                    continue;
                }

                if (c == '\\' && inString)
                {
                    escaped = true;
                    continue;
                }

                if (c == '"')
                {
                    inString = !inString;
                    continue;
                }

                if (!inString)
                {
                    switch (c)
                    {
                        case '{': braces++; break;
                        case '}': braces--; break;
                        case '[': brackets++; break;
                        case ']': brackets--; break;
                    }
                }
            }

            // JSON should have balanced braces and brackets
            return braces == 0 && brackets == 0;
        }
        catch
        {
            return false;
        }
    }

    private static bool IsXml(string text)
    {
        // Quick check: should start with < or <?xml
        if (!text.StartsWith("<"))
            return false;

        // Check for common XML patterns
        if (XmlDeclarationPattern.IsMatch(text))
            return true;

        // Check for balanced tags
        return XmlPattern.IsMatch(text);
    }

    private static bool IsHtml(string text)
    {
        // HTML has more varied structure, check for common tags
        var htmlTags = new[] { "html", "head", "body", "div", "span", "p", "a", "ul", "li", "table", "form", "script" };
        var lowerText = text.ToLowerInvariant();

        foreach (var tag in htmlTags)
        {
            if (lowerText.Contains($"<{tag}") || lowerText.Contains($"</{tag}>"))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Gets the display name for a format type
    /// </summary>
    public static string GetFormatDisplayName(ContentFormatType format)
    {
        return format switch
        {
            ContentFormatType.PlainText => "Text",
            ContentFormatType.Json => "JSON",
            ContentFormatType.Xml => "XML",
            ContentFormatType.Html => "HTML",
            ContentFormatType.CSharp => "C#",
            ContentFormatType.JavaScript => "JS",
            ContentFormatType.TypeScript => "TS",
            ContentFormatType.Python => "PY",
            ContentFormatType.Java => "Java",
            ContentFormatType.Cpp => "C++",
            ContentFormatType.Sql => "SQL",
            ContentFormatType.Bash => "Bash",
            ContentFormatType.PowerShell => "PS",
            ContentFormatType.HtmlCode => "HTML",
            ContentFormatType.Css => "CSS",
            ContentFormatType.Ruby => "Ruby",
            ContentFormatType.Go => "Go",
            ContentFormatType.Rust => "Rust",
            ContentFormatType.Php => "PHP",
            ContentFormatType.Swift => "Swift",
            ContentFormatType.Kotlin => "Kotlin",
            ContentFormatType.Yaml => "YAML",
            ContentFormatType.Markdown => "MD",
            ContentFormatType.Url => "URL",
            _ => "Text"
        };
    }

    /// <summary>
    /// Gets the icon character for a format type
    /// </summary>
    public static string GetFormatIcon(ContentFormatType format)
    {
        return format switch
        {
            ContentFormatType.Json => "{ }",
            ContentFormatType.Xml => "</>",
            ContentFormatType.Html => "<>",
            ContentFormatType.CSharp => "C#",
            ContentFormatType.JavaScript => "JS",
            ContentFormatType.TypeScript => "TS",
            ContentFormatType.Python => "PY",
            ContentFormatType.Java => "Ja",
            ContentFormatType.Cpp => "C+",
            ContentFormatType.Sql => "SQL",
            ContentFormatType.Bash => "SH",
            ContentFormatType.PowerShell => "PS",
            ContentFormatType.HtmlCode => "<>",
            ContentFormatType.Css => "CSS",
            ContentFormatType.Ruby => "Rb",
            ContentFormatType.Go => "Go",
            ContentFormatType.Rust => "Rs",
            ContentFormatType.Php => "PHP",
            ContentFormatType.Swift => "Sw",
            ContentFormatType.Kotlin => "Kt",
            ContentFormatType.Yaml => "YML",
            ContentFormatType.Markdown => "MD",
            ContentFormatType.Url => "URL",
            ContentFormatType.PlainText => "Txt",
            _ => ""
        };
    }
}
