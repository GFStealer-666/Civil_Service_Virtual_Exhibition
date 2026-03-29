using System;
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

    [Header("Header Localization")]
    [SerializeField] private string serviceHeaderTh = "งานบริการ";
    [SerializeField] private string serviceHeaderEn = "Service";
    [SerializeField] private string channelHeaderTh = "ช่องทางการรับบริการ";
    [SerializeField] private string channelHeaderEn = "Service Channel";
    [SerializeField] private string contactHeaderTh = "ช่องทางการติดต่อ";
    [SerializeField] private string contactHeaderEn = "Contact Channel";
    [SerializeField] private string durationHeaderTh = "ระยะเวลาการให้บริการ";
    [SerializeField] private string durationHeaderEn = "Service Duration";

    private PSC_ServiceItemDto _data;
    private string _lastLocaleCode = string.Empty;

    private struct NumberedBlock
    {
        public string Number;
        public string Body;
    }

    private void Awake()
    {
        RefreshLocalization();
    }

    private void OnEnable()
    {
        RefreshLocalization();
    }

    private void Update()
    {
        string currentLocaleCode = GetLocaleCode();
        if (string.Equals(_lastLocaleCode, currentLocaleCode, StringComparison.OrdinalIgnoreCase))
            return;

        RefreshLocalization();
    }

    public void Bind(PSC_ServiceItemDto data)
    {
        _data = data;
        RefreshLocalization();
    }

    public void RefreshLocalization()
    {
        _lastLocaleCode = GetLocaleCode();
        ApplyHeaders();
        BindBodies();
    }

    private void ApplyHeaders()
    {
        if (serviceHeaderText != null)
            serviceHeaderText.text = Pick(serviceHeaderTh, serviceHeaderEn, "Service");

        if (channelHeaderText != null)
            channelHeaderText.text = Pick(channelHeaderTh, channelHeaderEn, "Service Channel");

        if (contactHeaderText != null)
            contactHeaderText.text = Pick(contactHeaderTh, contactHeaderEn, "Contact Channel");

        if (durationHeaderText != null)
            durationHeaderText.text = Pick(durationHeaderTh, durationHeaderEn, "Service Duration");
    }

    private void BindBodies()
    {
        if (_data == null)
        {
            SetTexts(string.Empty, string.Empty, string.Empty, string.Empty);
            return;
        }

        string serviceRaw = PickRaw(_data.service, _data.serviceEn);
        string channelRaw = PickRaw(_data.channel, _data.channelEn);
        string contactRaw = PickRaw(_data.contact, _data.contactEn);
        string durationRaw = PickRaw(_data.duration, _data.durationEn);

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
                sb.AppendLine(NormalizeBlockBody(blocks[i].Body, true));
            }
            return sb.ToString().TrimEnd();
        }

        List<string> parts = SplitFallbackParts(raw, true);
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

    private string FormatChannelText(string raw)
    {
        List<NumberedBlock> blocks = ExtractTopLevelNumberedBlocks(raw);
        if (blocks.Count > 0)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < blocks.Count; i++)
            {
                sb.Append("• ");
                sb.AppendLine(NormalizeBlockBody(blocks[i].Body, true));
            }
            return sb.ToString().TrimEnd();
        }

        List<string> parts = SplitFallbackParts(raw, false);
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
                sb.AppendLine(NormalizeBlockBody(blocks[i].Body, false));
            }
            return sb.ToString().TrimEnd();
        }

        return NormalizeText(raw);
    }

    private string FormatDurationText(string raw)
    {
        return NormalizeText(raw);
    }

    private List<NumberedBlock> ExtractTopLevelNumberedBlocks(string raw)
    {
        List<NumberedBlock> result = new List<NumberedBlock>();

        string text = NormalizeText(raw);
        if (string.IsNullOrWhiteSpace(text))
            return result;

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
            string body = match.Groups["body"].Value.Trim();

            if (string.IsNullOrWhiteSpace(body))
                continue;

            result.Add(new NumberedBlock
            {
                Number = number,
                Body = body
            });
        }

        return result;
    }

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

    private string NormalizeBlockBody(string body, bool collapseInternalNewlines)
    {
        if (string.IsNullOrWhiteSpace(body))
            return string.Empty;

        string value = NormalizeText(body);

        if (collapseInternalNewlines)
            value = Regex.Replace(value, @"\s*\n\s*", " ");
        else
            value = Regex.Replace(value, @"\n{3,}", "\n\n");

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
            char next = i < value.Length - 1 ? value[i + 1] : '\0';

            bool hasSpaceBefore = char.IsWhiteSpace(previous);
            bool hasSpaceAfter = char.IsWhiteSpace(next);
            bool isUrlScheme = previous == ':';
            bool isDoubleSlash = next == '/';

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

    private string PickRaw(string thai, string english)
    {
        bool useEnglish = IsEnglish();
        return useEnglish
            ? FirstNotEmpty(english, thai)
            : FirstNotEmpty(thai, english);
    }

    private static string Pick(string thai, string english, string fallback)
    {
        bool useEnglish = IsEnglish();

        string primary = useEnglish ? english : thai;
        if (!string.IsNullOrWhiteSpace(primary))
            return primary.Trim();

        string secondary = useEnglish ? thai : english;
        if (!string.IsNullOrWhiteSpace(secondary))
            return secondary.Trim();

        return fallback;
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

    private static bool IsEnglish()
    {
        return GetLocaleCode().StartsWith("en", StringComparison.OrdinalIgnoreCase);
    }

    private static string GetLocaleCode()
    {
        string code = LocalizationService.CurrentLocaleCode;
        return string.IsNullOrWhiteSpace(code) ? "th" : code;
    }
}