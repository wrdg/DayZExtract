using System.Text;

namespace KuruExtract.Steam;

internal sealed class VdfNode
{
    private readonly string? _value;
    private readonly Dictionary<string, VdfNode>? _children;

    internal VdfNode(string value) => _value = value;
    internal VdfNode(Dictionary<string, VdfNode> children) => _children = children;

    public VdfNode? this[string key] =>
        _children != null && _children.TryGetValue(key, out var v) ? v : null;

    public IEnumerable<KeyValuePair<string, VdfNode>> Children =>
        (IEnumerable<KeyValuePair<string, VdfNode>>?)_children ?? [];

    public override string ToString() => _value ?? string.Empty;
}

internal static class VdfParser
{
    public static bool TryParse(string text, out VdfNode? node, out string? error)
    {
        node = null;
        error = null;
        try
        {
            int pos = 0;
            node = new VdfNode(ParseChildren(text, ref pos, isRoot: true));
            return true;
        }
        catch (FormatException ex)
        {
            error = ex.Message;
            return false;
        }
    }

    private static Dictionary<string, VdfNode> ParseChildren(string text, ref int pos, bool isRoot = false)
    {
        var result = new Dictionary<string, VdfNode>(StringComparer.OrdinalIgnoreCase);

        while (true)
        {
            SkipWhitespaceAndComments(text, ref pos);
            if (pos >= text.Length) break;
            if (!isRoot && text[pos] == '}') { pos++; break; }

            var key = ReadString(text, ref pos);
            SkipWhitespaceAndComments(text, ref pos);

            if (pos < text.Length && text[pos] == '{')
            {
                pos++;
                result[key] = new VdfNode(ParseChildren(text, ref pos));
            }
            else if (pos < text.Length)
            {
                result[key] = new VdfNode(ReadString(text, ref pos));
            }
        }

        return result;
    }

    private static void SkipWhitespaceAndComments(string text, ref int pos)
    {
        while (pos < text.Length)
        {
            if (char.IsWhiteSpace(text[pos]))
            {
                pos++;
            }
            else if (pos + 1 < text.Length && text[pos] == '/' && text[pos + 1] == '/')
            {
                while (pos < text.Length && text[pos] != '\n') pos++;
            }
            else break;
        }
    }

    private static string ReadString(string text, ref int pos)
    {
        if (pos >= text.Length) throw new FormatException("Unexpected end of VDF");

        if (text[pos] != '"')
        {
            int start = pos;
            while (pos < text.Length && !char.IsWhiteSpace(text[pos])) pos++;
            return text[start..pos];
        }

        pos++; // skip opening quote
        var sb = new StringBuilder();
        while (pos < text.Length && text[pos] != '"')
        {
            if (text[pos] == '\\' && pos + 1 < text.Length)
            {
                pos++;
                sb.Append(text[pos] switch
                {
                    'n'  => '\n',
                    't'  => '\t',
                    '"'  => '"',
                    '\\' => '\\',
                    _    => text[pos]
                });
            }
            else
            {
                sb.Append(text[pos]);
            }
            pos++;
        }
        if (pos < text.Length) pos++; // skip closing quote
        return sb.ToString();
    }
}
