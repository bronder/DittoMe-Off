using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using DittoMeOff.Models;

namespace DittoMeOff.Converters;

/// <summary>
/// Converts text content with syntax highlighting into a FlowDocument for display
/// Uses theme-aware colors from the application resources
/// Also highlights search text if provided
/// </summary>
public class SyntaxHighlightingConverter : IMultiValueConverter
{
    // Helper method to get brush from theme resources with fallback
    private static SolidColorBrush GetBrush(string key, SolidColorBrush fallback)
    {
        if (Application.Current.TryFindResource(key) is SolidColorBrush brush)
            return brush;
        return fallback;
    }

    // Fallback colors (VS Code Dark)
    private static readonly SolidColorBrush DefaultTextBrush = new(Color.FromRgb(212, 212, 212));
    private static readonly SolidColorBrush DefaultCodeKeywordBrush = new(Color.FromRgb(86, 156, 214));
    private static readonly SolidColorBrush DefaultCodeStringBrush = new(Color.FromRgb(206, 145, 120));
    private static readonly SolidColorBrush DefaultCodeCommentBrush = new(Color.FromRgb(106, 153, 85));
    private static readonly SolidColorBrush DefaultCodeNumberBrush = new(Color.FromRgb(181, 206, 168));
    private static readonly SolidColorBrush DefaultCodeKeyBrush = new(Color.FromRgb(156, 220, 254));
    private static readonly SolidColorBrush DefaultAccentBrush = new(Color.FromRgb(0, 120, 212));
    private static readonly SolidColorBrush DefaultSearchHighlightBrush = new(Color.FromArgb(80, 255, 200, 0));

    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 1 || values[0] is not ClipboardItem item)
            return new FlowDocument();

        string searchText = values.Length > 1 ? values[1]?.ToString() ?? "" : "";

        if (string.IsNullOrEmpty(item.Content))
            return new FlowDocument();

        return CreateHighlightedDocument(item.Content, item.FormatType, searchText);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }

    private FlowDocument CreateHighlightedDocument(string content, ContentFormatType formatType, string searchText)
    {
        // Get theme-aware brushes
        var textBrush = GetBrush("PreviewTextBrush", DefaultTextBrush);
        var accentBrush = GetBrush("AccentBrush", DefaultAccentBrush);
        
        var doc = new FlowDocument
        {
            FontFamily = new FontFamily("Cascadia Code, Consolas, Courier New"),
            FontSize = 12,
            PagePadding = new System.Windows.Thickness(8),
            Background = Brushes.Transparent,
            LineHeight = 16
        };

        var paragraph = new Paragraph { Margin = new System.Windows.Thickness(0), LineHeight = 16 };

        // For plain text or PlainText format type, just display as-is with word wrap
        if (formatType == ContentFormatType.PlainText)
        {
            // Display plain text with proper formatting and search highlighting
            HighlightTextWithSearch(content, paragraph, textBrush, accentBrush, searchText);
        }
        else
        {
            switch (formatType)
            {
                case ContentFormatType.Json:
                    HighlightJson(content, paragraph, searchText);
                    break;
                case ContentFormatType.Xml:
                case ContentFormatType.Html:
                case ContentFormatType.HtmlCode:
                    HighlightXml(content, paragraph, searchText);
                    break;
                default:
                    // For other code types, use generic highlighting
                    if (IsCodeFormat(formatType))
                        HighlightGenericCode(content, paragraph, searchText);
                    else
                        HighlightTextWithSearch(content, paragraph, textBrush, accentBrush, searchText);
                    break;
            }
        }

        doc.Blocks.Add(paragraph);
        return doc;
    }

    private void HighlightTextWithSearch(string text, Paragraph paragraph, SolidColorBrush textBrush, 
        SolidColorBrush accentBrush, string searchText)
    {
        if (string.IsNullOrEmpty(searchText))
        {
            foreach (var line in text.Split('\n'))
            {
                paragraph.Inlines.Add(new Run(line) { Foreground = textBrush });
                paragraph.Inlines.Add(new LineBreak());
            }
            return;
        }

        // Highlight search matches
        string lowerText = text.ToLower();
        string lowerSearch = searchText.ToLower();
        int index = 0;

        while (index < lowerText.Length)
        {
            int matchIndex = lowerText.IndexOf(lowerSearch, index, StringComparison.Ordinal);
            
            if (matchIndex == -1)
            {
                if (index < text.Length)
                {
                    paragraph.Inlines.Add(new Run(text.Substring(index)) { Foreground = textBrush });
                }
                break;
            }

            // Add text before match
            if (matchIndex > index)
            {
                paragraph.Inlines.Add(new Run(text.Substring(index, matchIndex - index)) { Foreground = textBrush });
            }

            // Add highlighted match
            var highlightRun = new Run(text.Substring(matchIndex, searchText.Length))
            {
                Foreground = accentBrush,
                FontWeight = FontWeights.Bold,
                Background = DefaultSearchHighlightBrush
            };
            paragraph.Inlines.Add(highlightRun);

            index = matchIndex + searchText.Length;
        }
    }

    private void HighlightJson(string json, Paragraph paragraph, string searchText)
    {
        // Get theme-aware brushes
        var textBrush = GetBrush("PreviewTextBrush", DefaultTextBrush);
        var jsonKeyBrush = GetBrush("PreviewCodeKeyBrush", DefaultCodeKeyBrush);
        var jsonStringBrush = GetBrush("PreviewCodeStringBrush", DefaultCodeStringBrush);
        var jsonNumberBrush = GetBrush("PreviewCodeNumberBrush", DefaultCodeNumberBrush);
        var jsonBoolBrush = GetBrush("PreviewCodeKeywordBrush", DefaultCodeKeywordBrush);
        var accentBrush = GetBrush("AccentBrush", DefaultAccentBrush);

        // Simple JSON highlighting
        bool inString = false;
        bool escapeNext = false;
        bool isKey = false;
        var currentText = "";

        for (int i = 0; i < json.Length; i++)
        {
            char c = json[i];

            if (escapeNext)
            {
                currentText += c;
                escapeNext = false;
                continue;
            }

            if (c == '\\' && inString)
            {
                currentText += c;
                escapeNext = true;
                continue;
            }

            if (c == '"')
            {
                if (!inString)
                {
                    // Check if this is a key (followed by :)
                    int j = i + 1;
                    while (j < json.Length && char.IsWhiteSpace(json[j])) j++;
                    isKey = j < json.Length && json[j] == ':';
                    inString = true;
                    currentText = "\"";
                }
                else
                {
                    currentText += '"';
                    inString = false;

                    // Determine color based on context
                    var brush = isKey ? jsonKeyBrush : jsonStringBrush;
                    // Add text with potential search highlighting
                    AddHighlightedRun(currentText, paragraph, brush, accentBrush, searchText);
                    currentText = "";
                }
                continue;
            }

            if (!inString)
            {
                // Handle non-string characters
                if (currentText.Length > 0)
                {
                    // Check for keywords and numbers
                    if (currentText.Trim() == "true" || currentText.Trim() == "false" || currentText.Trim() == "null")
                    {
                        AddHighlightedRun(currentText, paragraph, jsonBoolBrush, accentBrush, searchText);
                    }
                    else
                    {
                        AddHighlightedRun(currentText, paragraph, textBrush, accentBrush, searchText);
                    }
                    currentText = "";
                }

                if (char.IsDigit(c) || c == '-' || c == '.')
                {
                    // Collect number
                    string num = "";
                    while (i < json.Length && (char.IsDigit(json[i]) || json[i] == '.' || json[i] == '-' || json[i] == 'e' || json[i] == 'E' || json[i] == '+'))
                    {
                        num += json[i];
                        i++;
                    }
                    i--; // Back up one since for loop will increment
                    AddHighlightedRun(num, paragraph, jsonNumberBrush, accentBrush, searchText);
                }
                else
                {
                    AddHighlightedRun(c.ToString(), paragraph, textBrush, accentBrush, searchText);
                }
            }
            else
            {
                currentText += c;
            }
        }

        if (currentText.Length > 0)
        {
            var brush = isKey ? jsonKeyBrush : jsonStringBrush;
            AddHighlightedRun(currentText, paragraph, brush, accentBrush, searchText);
        }
    }

    private void HighlightXml(string xml, Paragraph paragraph, string searchText)
    {
        // Get theme-aware brushes
        var textBrush = GetBrush("PreviewTextBrush", DefaultTextBrush);
        var xmlTagBrush = GetBrush("PreviewCodeKeywordBrush", DefaultCodeKeywordBrush);
        var xmlAttrBrush = GetBrush("PreviewCodeKeyBrush", DefaultCodeKeyBrush);
        var accentBrush = GetBrush("AccentBrush", DefaultAccentBrush);

        // Simple XML highlighting
        bool inTag = false;
        bool inAttribute = false;
        bool inValue = false;
        string currentText = "";

        for (int i = 0; i < xml.Length; i++)
        {
            char c = xml[i];

            if (c == '<' && !inTag)
            {
                if (currentText.Length > 0)
                {
                    AddHighlightedRun(currentText, paragraph, textBrush, accentBrush, searchText);
                    currentText = "";
                }
                inTag = true;
                currentText = "<";
                continue;
            }

            if (c == '>' && inTag)
            {
                currentText += '>';
                inTag = false;
                inAttribute = false;
                inValue = false;

                AddHighlightedRun(currentText, paragraph, xmlTagBrush, accentBrush, searchText);
                currentText = "";
                continue;
            }

            if (inTag)
            {
                if (c == '"' || c == '\'')
                {
                    inValue = !inValue;
                    currentText += c;
                    continue;
                }

                if (inValue)
                {
                    currentText += c;
                    continue;
                }

                if (c == '/' && i + 1 < xml.Length && xml[i + 1] == '>')
                {
                    currentText += '/';
                    continue;
                }

                if (char.IsWhiteSpace(c) && !inAttribute)
                {
                    // End of tag name
                    AddHighlightedRun(currentText, paragraph, xmlTagBrush, accentBrush, searchText);
                    currentText = "";
                    continue;
                }

                if (c == '=')
                {
                    AddHighlightedRun(currentText, paragraph, xmlAttrBrush, accentBrush, searchText);
                    currentText = "";
                    inAttribute = true;
                    continue;
                }

                currentText += c;
            }
            else
            {
                AddHighlightedRun(c.ToString(), paragraph, textBrush, accentBrush, searchText);
            }
        }

        if (currentText.Length > 0)
        {
            AddHighlightedRun(currentText, paragraph, textBrush, accentBrush, searchText);
        }
    }

    private void HighlightGenericCode(string code, Paragraph paragraph, string searchText)
    {
        // Get theme-aware brushes
        var textBrush = GetBrush("PreviewTextBrush", DefaultTextBrush);
        var codeKeywordBrush = GetBrush("PreviewCodeKeywordBrush", DefaultCodeKeywordBrush);
        var codeStringBrush = GetBrush("PreviewCodeStringBrush", DefaultCodeStringBrush);
        var codeCommentBrush = GetBrush("PreviewCodeCommentBrush", DefaultCodeCommentBrush);
        var codeNumberBrush = GetBrush("PreviewCodeNumberBrush", DefaultCodeNumberBrush);
        var accentBrush = GetBrush("AccentBrush", DefaultAccentBrush);

        // Keywords for common languages
        var keywords = new[] { "class", "function", "def", "const", "let", "var", "if", "else", "for", "while", 
                                "return", "import", "export", "from", "async", "await", "public", "private", 
                                "protected", "static", "void", "int", "string", "bool", "true", "false", "null",
                                "new", "this", "try", "catch", "throw", "finally", "switch", "case", "break",
                                "continue", "default", "default", "interface", "type", "enum", "struct", "namespace", "using" };

        bool inString = false;
        char stringChar = '"';
        string currentText = "";

        for (int i = 0; i < code.Length; i++)
        {
            char c = code[i];

            // Handle strings
            if ((c == '"' || c == '\'' || c == '`') && !inString)
            {
                if (currentText.Length > 0)
                {
                    CheckAndAddCode(currentText, paragraph, keywords, codeKeywordBrush, codeNumberBrush, textBrush, accentBrush, searchText);
                    currentText = "";
                }
                inString = true;
                stringChar = c;
                currentText = c.ToString();
                continue;
            }

            if (inString)
            {
                currentText += c;
                if (c == stringChar && (i == 0 || code[i - 1] != '\\'))
                {
                    inString = false;
                    AddHighlightedRun(currentText, paragraph, codeStringBrush, accentBrush, searchText);
                    currentText = "";
                }
                continue;
            }

            // Handle whitespace and operators
            if (char.IsWhiteSpace(c) || "(){}[];,.<>?!@#$%^&*+-=:|".Contains(c))
            {
                if (currentText.Length > 0)
                {
                    CheckAndAddCode(currentText, paragraph, keywords, codeKeywordBrush, codeNumberBrush, textBrush, accentBrush, searchText);
                    currentText = "";
                }
                AddHighlightedRun(c.ToString(), paragraph, textBrush, accentBrush, searchText);
                continue;
            }

            currentText += c;
        }

        if (currentText.Length > 0)
        {
            CheckAndAddCode(currentText, paragraph, keywords, codeKeywordBrush, codeNumberBrush, textBrush, accentBrush, searchText);
        }
    }

    private void CheckAndAddCode(string text, Paragraph paragraph, string[] keywords,
        SolidColorBrush codeKeywordBrush, SolidColorBrush codeNumberBrush, SolidColorBrush textBrush,
        SolidColorBrush accentBrush, string searchText)
    {
        if (keywords.Any(k => k.Equals(text, StringComparison.Ordinal)))
        {
            AddHighlightedRun(text, paragraph, codeKeywordBrush, accentBrush, searchText);
        }
        else if (int.TryParse(text, out _) || double.TryParse(text, out _))
        {
            AddHighlightedRun(text, paragraph, codeNumberBrush, accentBrush, searchText);
        }
        else
        {
            AddHighlightedRun(text, paragraph, textBrush, accentBrush, searchText);
        }
    }

    private void AddHighlightedRun(string text, Paragraph paragraph, SolidColorBrush baseBrush, 
        SolidColorBrush accentBrush, string searchText)
    {
        if (string.IsNullOrEmpty(searchText))
        {
            paragraph.Inlines.Add(new Run(text) { Foreground = baseBrush });
            return;
        }

        string lowerText = text.ToLower();
        string lowerSearch = searchText.ToLower();
        int index = 0;

        while (index < lowerText.Length)
        {
            int matchIndex = lowerText.IndexOf(lowerSearch, index, StringComparison.Ordinal);
            
            if (matchIndex == -1)
            {
                if (index < text.Length)
                {
                    paragraph.Inlines.Add(new Run(text.Substring(index)) { Foreground = baseBrush });
                }
                break;
            }

            // Add text before match
            if (matchIndex > index)
            {
                paragraph.Inlines.Add(new Run(text.Substring(index, matchIndex - index)) { Foreground = baseBrush });
            }

            // Add highlighted match
            var highlightRun = new Run(text.Substring(matchIndex, searchText.Length))
            {
                Foreground = accentBrush,
                FontWeight = FontWeights.Bold,
                Background = DefaultSearchHighlightBrush
            };
            paragraph.Inlines.Add(highlightRun);

            index = matchIndex + searchText.Length;
        }
    }

    private bool IsCodeFormat(ContentFormatType format)
    {
        return format >= ContentFormatType.CSharp;
    }
}
