using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;

public class PSC_ServiceDetailItemView : MonoBehaviour
{
    [Header("Column Titles")]
    [SerializeField] private TMP_Text serviceHeaderText;
    [SerializeField] private TMP_Text channelHeaderText;
    [SerializeField] private TMP_Text contactHeaderText;
    [SerializeField] private TMP_Text durationHeaderText;

    [Header("Column Bodies")]
    [SerializeField] private TMP_Text serviceBodyText;
    [SerializeField] private TMP_Text channelBodyText;
    [SerializeField] private TMP_Text contactBodyText;
    [SerializeField] private TMP_Text durationBodyText;

    [Header("Language")]
    [SerializeField] private bool useEnglish;

    private PSC_ServiceItemDto _data;

    private struct NumberedBlock
    {
        public string Number;
        public string Body;
    }

    private void Awake()
    {
        ApplyHeaders();
    }

    public void Bind(PSC_ServiceItemDto data)
    {
        _data = data;

        ApplyHeaders();
        BindBodies();
    }

    private void ApplyHeaders()
    {
        if (serviceHeaderText != null)
            serviceHeaderText.text = useEnglish ? "Service" : "งานบริการ";

        if (channelHeaderText != null)
            channelHeaderText.text = useEnglish ? "Service Channel" : "ช่องทางการรับบริการ";

        if (contactHeaderText != null)
            contactHeaderText.text = useEnglish ? "Contact Channel" : "ช่องทางการติดต่อ";

        if (durationHeaderText != null)
            durationHeaderText.text = useEnglish ? "Service Duration" : "ระยะเวลาการให้บริการ";
    }

    private void BindBodies()
    {
        if (_data == null)
        {
            SetTexts(string.Empty, string.Empty, string.Empty, string.Empty);
            return;
        }

        string serviceRaw = useEnglish
            ? FirstNotEmpty(_data.serviceEn, _data.service)
            : FirstNotEmpty(_data.service, _data.serviceEn);

        string channelRaw = useEnglish
            ? FirstNotEmpty(_data.channelEn, _data.channel)
            : FirstNotEmpty(_data.channel, _data.channelEn);

        string contactRaw = useEnglish
            ? FirstNotEmpty(_data.contactEn, _data.contact)
            : FirstNotEmpty(_data.contact, _data.contactEn);

        string durationRaw = useEnglish
            ? FirstNotEmpty(_data.durationEn, _data.duration)
            : FirstNotEmpty(_data.duration, _data.durationEn);

        SetTexts(
            FormatServiceText(serviceRaw),
            FormatChannelText(channelRaw),
            FormatContactText(contactRaw),
            FormatDurationText(durationRaw)
        );
    }

    private void SetTexts(string service, string channel, string contact, string duration)
    {
        if (serviceBodyText != null)
            serviceBodyText.text = service;

        if (channelBodyText != null)
            channelBodyText.text = channel;

        if (contactBodyText != null)
            contactBodyText.text = contact;

        if (durationBodyText != null)
            durationBodyText.text = duration;
    }

    // ── Service: numbered list  1. 2. 3. ─────────────────────────────────────
    private string FormatServiceText(string raw)
    {
        List<NumberedBlock> blocks = ExtractTopLevelNumberedBlocks(raw);
        if (blocks.Count > 0)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < blocks.Count; i++)
            {
                sb.Append(blocks[i].Number);
                sb.Append(". ");
                // Collapse internal line-breaks so each entry stays on one line
                sb.AppendLine(NormalizeBlockBody(blocks[i].Body, collapseInternalNewlines: true));
            }
            return sb.ToString().TrimEnd();
        }

        // Fallback: no explicit numbers found — auto-number by paragraph / slash
        List<string> parts = SplitFallbackParts(raw, allowSafeSlashSplit: true);
        if (parts.Count == 0)
            return string.Empty;

        StringBuilder fb = new StringBuilder();
        for (int i = 0; i < parts.Count; i++)
        {
            fb.Append(i + 1);
            fb.Append(". ");
            fb.AppendLine(parts[i]);
        }
        return fb.ToString().TrimEnd();
    }

    // ── Channel: bullet list  •  ──────────────────────────────────────────────
    private string FormatChannelText(string raw)
    {
        List<NumberedBlock> blocks = ExtractTopLevelNumberedBlocks(raw);
        if (blocks.Count > 0)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < blocks.Count; i++)
            {
                sb.Append("• ");
                // Collapse internal line-breaks so each bullet stays on one line
                sb.AppendLine(NormalizeBlockBody(blocks[i].Body, collapseInternalNewlines: true));
            }
            return sb.ToString().TrimEnd();
        }

        // Fallback: bullet every paragraph
        List<string> parts = SplitFallbackParts(raw, allowSafeSlashSplit: false);
        if (parts.Count == 0)
            return string.Empty;

        StringBuilder fb = new StringBuilder();
        for (int i = 0; i < parts.Count; i++)
        {
            fb.Append("• ");
            fb.AppendLine(parts[i]);
        }
        return fb.ToString().TrimEnd();
    }

    // ── Contact: numbered list 1. 2. 3.  (sub-items 1.1 1.2 preserved) ───────
    private string FormatContactText(string raw)
    {
        List<NumberedBlock> blocks = ExtractTopLevelNumberedBlocks(raw);
        if (blocks.Count > 0)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < blocks.Count; i++)
            {
                sb.Append(blocks[i].Number);
                sb.Append(". ");
                // Preserve internal newlines — addresses span multiple lines
                sb.AppendLine(NormalizeBlockBody(blocks[i].Body, collapseInternalNewlines: false));
            }
            return sb.ToString().TrimEnd();
        }

        // Fallback: show as plain normalised text
        return NormalizeText(raw);
    }

    // ── Duration: plain text, no splitting ───────────────────────────────────
    private string FormatDurationText(string raw)
    {
        return NormalizeText(raw);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Core parser
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Extracts top-level numbered blocks (1. / 1) at the start of any line).
    /// Works whether items are separated by a single \n or multiple \n\n\n.
    /// </summary>
    private List<NumberedBlock> ExtractTopLevelNumberedBlocks(string raw)
    {
        List<NumberedBlock> result = new List<NumberedBlock>();

        string text = NormalizeText(raw);
        if (string.IsNullOrWhiteSpace(text))
            return result;

        // (?:^|\n)   — start of string or any newline (not just blank lines)
        // (?<num>\d+)[.)] — integer followed by . or )
        // (?<body>.*?)   — lazy body
        // (?=\n\d+[.)]|\z) — stops at the next numbered item or end of string
        MatchCollection matches = Regex.Matches(
            text,
            @"(?:^|\n)(?<num>\d+)[.)]\s*(?<body>.*?)(?=\n\d+[.)]|\z)",
            RegexOptions.Singleline
        );

        for (int i = 0; i < matches.Count; i++)
        {
            Match match = matches[i];
            if (!match.Success)
                continue;

            string number = match.Groups["num"].Value.Trim();
            string body   = match.Groups["body"].Value.Trim();

            if (string.IsNullOrWhiteSpace(body))
                continue;

            result.Add(new NumberedBlock { Number = number, Body = body });
        }

        return result;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────────────

    private List<string> SplitFallbackParts(string raw, bool allowSafeSlashSplit)
    {
        List<string> result = new List<string>();

        string working = NormalizeText(raw);
        if (string.IsNullOrWhiteSpace(working))
            return result;

        if (allowSafeSlashSplit)
            working = SplitSlashAsDelimiterOnly(working);

        string[] paragraphs = Regex.Split(working, @"\n\s*\n");

        for (int i = 0; i < paragraphs.Length; i++)
        {
            string part = paragraphs[i].Trim();
            if (!string.IsNullOrWhiteSpace(part))
                result.Add(part);
        }

        return result;
    }

    /// <summary>
    /// Cleans up a block body.
    /// collapseInternalNewlines = true  → all \n inside the body become a space
    ///                                     (good for channel bullets / service lines)
    /// collapseInternalNewlines = false → single \n are kept; only 3+ \n are
    ///                                     reduced to \n\n  (good for contact addresses)
    /// </summary>
    private string NormalizeBlockBody(string body, bool collapseInternalNewlines)
    {
        if (string.IsNullOrWhiteSpace(body))
            return string.Empty;

        string value = NormalizeText(body);

        if (collapseInternalNewlines)
        {
            // Collapse every newline (and surrounding whitespace) into a single space
            value = Regex.Replace(value, @"\s*\n\s*", " ");
        }
        else
        {
            // Just remove excessive blank lines, keep single newlines intact
            value = Regex.Replace(value, @"\n{3,}", "\n\n");
        }

        return value.Trim();
    }

    private string SplitSlashAsDelimiterOnly(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        StringBuilder builder = new StringBuilder();

        for (int i = 0; i < value.Length; i++)
        {
            char current = value[i];

            if (current != '/')
            {
                builder.Append(current);
                continue;
            }

            char previous = i > 0 ? value[i - 1] : '\0';
            char next     = i < value.Length - 1 ? value[i + 1] : '\0';

            bool hasSpaceBefore = char.IsWhiteSpace(previous);
            bool hasSpaceAfter  = char.IsWhiteSpace(next);
            bool isUrlScheme    = previous == ':';
            bool isDoubleSlash  = next == '/';

            if ((hasSpaceBefore || hasSpaceAfter) && !isUrlScheme && !isDoubleSlash)
                builder.Append('\n');
            else
                builder.Append('/');
        }

        return builder.ToString();
    }

    private string NormalizeText(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        return value
            .Replace("\\n", "\n")
            .Replace("\r\n", "\n")
            .Replace("\r", "\n")
            .Trim();
    }

    private string FirstNotEmpty(params string[] values)
    {
        for (int i = 0; i < values.Length; i++)
        {
            if (!string.IsNullOrWhiteSpace(values[i]))
                return values[i];
        }
        return string.Empty;
    }
}