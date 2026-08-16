using System.Text.RegularExpressions;
using System.Xml.Linq;
using Xml2Doc.Core.Aliasing;
using Xml2Doc.Core.Models;

namespace Xml2Doc.Core.Signatures;

/// <summary>Default depth-aware C#-style signature renderer.</summary>
public sealed class DefaultSignatureRenderer : ISignatureRenderer
{
    private readonly IAliasProvider _aliasProvider;
    private readonly string? _rootNamespaceToTrim;

    /// <summary>Creates a signature renderer with optional aliases and root trimming.</summary>
    /// <param name="aliasProvider">Alias provider used for signature types.</param>
    /// <param name="rootNamespaceToTrim">Optional root namespace removed from type headings.</param>
    public DefaultSignatureRenderer(
        IAliasProvider? aliasProvider = null,
        string? rootNamespaceToTrim = null)
    {
        _aliasProvider = aliasProvider ?? DefaultAliasProvider.Instance;
        _rootNamespaceToTrim = rootNamespaceToTrim;
    }

    /// <inheritdoc />
    public string RenderTypeName(string typeId)
    {
        if (typeId.IndexOf('{') >= 0 ||
            typeId.IndexOf('}') >= 0 ||
            typeId.IndexOf('<') >= 0)
        {
            var display = FormatType(typeId, Array.Empty<string>());
            return TrimRootNamespace(display);
        }

        var id = TrimRootNamespace(typeId);
        var lastDot = id.LastIndexOf('.');
        var simple = lastDot >= 0 ? id.Substring(lastDot + 1) : id;
        return ExpandGenericArity(simple, Array.Empty<string>());
    }

    /// <inheritdoc />
    public string RenderMemberHeader(XMember member, SignatureStyle style)
    {
        if (member is null)
            throw new ArgumentNullException(nameof(member));
        if (style is null)
            throw new ArgumentNullException(nameof(style));

        var typeParameters = member.Element.Elements("typeparam").ToArray();
        var genericNames = style.IncludeConstraints
            ? typeParameters
                .Select(element => (string?)element.Attribute("name"))
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Select(name => name!)
                .ToArray()
            : Array.Empty<string>();
        var name = RenderMemberName(member, style, genericNames);
        var constraints = style.IncludeConstraints
            ? RenderConstraints(typeParameters)
            : string.Empty;
        return $"{KindToWord(member.Kind)}: {name}{constraints}";
    }

    /// <inheritdoc />
    public string RenderCrefLabel(string cref)
    {
        if (string.IsNullOrWhiteSpace(cref))
            return string.Empty;

        var parts = cref.Split(new[] { ':' }, 2);
        var kind = parts.Length == 2 ? parts[0] : string.Empty;
        var id = parts.Length == 2 ? parts[1] : cref;
        if (kind == "T")
            return RenderTypeName(id);

        if (kind == "M")
            return RenderLegacyMethodCrefLabel(id);

        var lastDot = id.LastIndexOf('.');
        return lastDot >= 0 ? id.Substring(lastDot + 1) : id;
    }

    private string RenderLegacyMethodCrefLabel(string id)
    {
        var parenIndex = id.IndexOf('(');
        var cut = parenIndex >= 0
            ? id.LastIndexOf('.', parenIndex)
            : id.LastIndexOf('.');
        var nameAndParameters = cut >= 0 ? id.Substring(cut + 1) : id;
        var paren = nameAndParameters.IndexOf('(');
        var methodName = paren >= 0
            ? nameAndParameters.Substring(0, paren)
            : nameAndParameters;
        methodName = Regex.Replace(methodName, @"``(\d+)", match =>
        {
            var count = int.Parse(match.Groups[1].Value);
            return $"<{string.Join(",", Enumerable.Range(1, count).Select(index => $"T{index}"))}>";
        });

        var parameterList = paren >= 0 &&
                            nameAndParameters.EndsWith(")", StringComparison.Ordinal)
            ? nameAndParameters.Substring(
                paren + 1,
                nameAndParameters.Length - paren - 2)
            : string.Empty;
        var parameters = string.IsNullOrWhiteSpace(parameterList)
            ? string.Empty
            : string.Join(", ",
                SplitTopLevel(parameterList, '{', '}')
                    .Select(FormatLegacyCrefParameter));

        return string.IsNullOrEmpty(parameters)
            ? $"{methodName}()"
            : $"{methodName}({parameters})";
    }

    private string FormatLegacyCrefParameter(string parameter)
    {
        var value = _aliasProvider.ApplyAliases(
            parameter.Trim().Replace('{', '<').Replace('}', '>'));
        var open = value.IndexOf('<');
        var close = value.LastIndexOf('>');

        if (open >= 0 && close > open)
        {
            var head = value.Substring(0, open + 1);
            var inner = value.Substring(open + 1, close - open - 1);
            var tail = value.Substring(close);
            var arguments = SplitTopLevel(inner, '<', '>')
                .Select(argument => argument.IndexOf('<') >= 0
                    ? argument
                    : argument.Split('.').Last());
            value = head + string.Join(", ", arguments) + tail;
        }

        if (value.IndexOf('<') < 0)
            value = value.Split('.').Last();
        return value;
    }

    private string RenderMemberName(
        XMember member,
        SignatureStyle style,
        IReadOnlyList<string> genericNames) =>
        RenderMemberName(
            member.Id,
            member.Element.Elements("param").ToArray(),
            style,
            genericNames,
            member.Kind);

    private string RenderMemberName(
        string id,
        IReadOnlyList<XElement> parameters,
        SignatureStyle style,
        IReadOnlyList<string> genericNames,
        string kind = "M")
    {
        var parenIndex = id.IndexOf('(');
        var cut = parenIndex >= 0
            ? id.LastIndexOf('.', parenIndex)
            : id.LastIndexOf('.');
        var nameAndParameters = cut >= 0 ? id.Substring(cut + 1) : id;
        var openParen = nameAndParameters.IndexOf('(');
        var closeParen = nameAndParameters.EndsWith(")", StringComparison.Ordinal)
            ? nameAndParameters.Length - 1
            : -1;
        var name = openParen >= 0
            ? nameAndParameters.Substring(0, openParen)
            : nameAndParameters;
        name = ExpandGenericArity(name, genericNames);

        var parameterList = openParen >= 0 && closeParen > openParen
            ? nameAndParameters.Substring(openParen + 1, closeParen - openParen - 1)
            : string.Empty;
        var types = string.IsNullOrWhiteSpace(parameterList)
            ? Array.Empty<string>()
            : SplitTopLevel(parameterList, '{', '}').ToArray();
        var renderedParameters = types
            .Select((type, index) => RenderParameter(
                type,
                index < parameters.Count ? parameters[index] : null,
                style,
                genericNames))
            .ToArray();

        if (kind == "P" &&
            string.Equals(name, "Item", StringComparison.Ordinal) &&
            renderedParameters.Length > 0 &&
            (style.IncludeParamNames || style.IncludeDefaultValues))
        {
            return $"this[{string.Join(", ", renderedParameters)}]";
        }

        return openParen >= 0
            ? $"{name}({string.Join(", ", renderedParameters)})"
            : name;
    }

    private string RenderParameter(
        string type,
        XElement? parameter,
        SignatureStyle style,
        IReadOnlyList<string> genericNames)
    {
        var includeName = style.IncludeParamNames || style.IncludeDefaultValues;
        var modifier = includeName
            ? (string?)parameter?.Attribute("modifier")
            : null;
        if (includeName &&
            string.IsNullOrWhiteSpace(modifier) &&
            type.EndsWith("@", StringComparison.Ordinal))
            modifier = "ref";

        var normalizedType = includeName && type.EndsWith("@", StringComparison.Ordinal)
            ? type.Substring(0, type.Length - 1)
            : type;
        var result = FormatType(normalizedType, genericNames);
        if (!string.IsNullOrWhiteSpace(modifier))
            result = modifier + " " + result;

        var name = (string?)parameter?.Attribute("name");
        if (includeName && !string.IsNullOrWhiteSpace(name))
            result += " " + name;

        var defaultValue = (string?)parameter?.Attribute("default");
        if (style.IncludeDefaultValues && !string.IsNullOrWhiteSpace(defaultValue))
            result += " = " + defaultValue;

        return result;
    }

    private static string RenderConstraints(IEnumerable<XElement> typeParameters)
    {
        var constraints = typeParameters
            .Select(element => new
            {
                Name = (string?)element.Attribute("name"),
                Constraint = (string?)element.Attribute("constraint")
            })
            .Where(item =>
                !string.IsNullOrWhiteSpace(item.Name) &&
                !string.IsNullOrWhiteSpace(item.Constraint))
            .Select(item => $" where {item.Name} : {item.Constraint}");
        return string.Concat(constraints);
    }

    private string FormatType(string full, IReadOnlyList<string> genericNames)
    {
        if (string.IsNullOrWhiteSpace(full))
            return string.Empty;

        var value = full.Trim().Replace('{', '<').Replace('}', '>');
        value = Regex.Replace(value, @"``(\d+)", match =>
        {
            var index = int.Parse(match.Groups[1].Value);
            return index < genericNames.Count ? genericNames[index] : $"T{index + 1}";
        });
        value = Regex.Replace(value, @"`(\d+)", match =>
            $"T{int.Parse(match.Groups[1].Value) + 1}");

        var open = value.IndexOf('<');
        if (open < 0)
            return ShortenSimpleType(value);

        var close = FindMatchingAngle(value, open);
        if (close < 0)
            return value;

        var head = ShortenSimpleType(value.Substring(0, open));
        var inner = value.Substring(open + 1, close - open - 1);
        var tail = value.Substring(close + 1);
        var arguments = SplitTopLevel(inner, '<', '>')
            .Select(argument => FormatType(argument, genericNames));
        return $"{head}<{string.Join(", ", arguments)}>{tail}";
    }

    private string ShortenSimpleType(string value)
    {
        var result = _aliasProvider.ApplyAliases(value);
        if (result.IndexOf('.') >= 0)
            result = result.Split('.').Last();
        return result.Replace("System.Collections.Generic.", string.Empty)
            .Replace("System.", string.Empty);
    }

    private string TrimRootNamespace(string value)
    {
        if (!string.IsNullOrWhiteSpace(_rootNamespaceToTrim) &&
            value.StartsWith(_rootNamespaceToTrim + ".", StringComparison.Ordinal))
        {
            return value.Substring(_rootNamespaceToTrim.Length + 1);
        }

        return value;
    }

    private static string ExpandGenericArity(
        string value,
        IReadOnlyList<string> genericNames) =>
        Regex.Replace(value, @"``?(\d+)", match =>
        {
            var count = int.Parse(match.Groups[1].Value);
            var names = genericNames.Count == count
                ? genericNames
                : Enumerable.Range(1, count).Select(index => $"T{index}");
            return $"<{string.Join(",", names)}>";
        });

    private static int FindMatchingAngle(string value, int openIndex)
    {
        var depth = 0;
        for (var index = openIndex; index < value.Length; index++)
        {
            if (value[index] == '<')
                depth++;
            else if (value[index] == '>' && --depth == 0)
                return index;
        }

        return -1;
    }

    private static IEnumerable<string> SplitTopLevel(
        string value,
        char open,
        char close)
    {
        var depth = 0;
        var start = 0;
        for (var index = 0; index < value.Length; index++)
        {
            if (value[index] == open)
                depth++;
            else if (value[index] == close)
                depth--;
            else if (value[index] == ',' && depth == 0)
            {
                yield return value.Substring(start, index - start).Trim();
                start = index + 1;
            }
        }

        if (start <= value.Length)
            yield return value.Substring(start).Trim();
    }

    private static string KindToWord(string kind) => kind switch
    {
        "M" => "Method",
        "P" => "Property",
        "F" => "Field",
        "E" => "Event",
        "T" => "Type",
        _ => kind
    };
}
