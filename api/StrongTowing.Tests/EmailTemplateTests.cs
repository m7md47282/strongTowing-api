using StrongTowing.API.Options;
using StrongTowing.API.Services;
using StrongTowing.Application.EmailTemplates;

namespace StrongTowing.Tests;

public class EmailTemplateTests
{
    [Fact]
    public void EmailEventKeys_All_has_14_keys()
    {
        Assert.Equal(14, EmailEventKeys.All.Count);
    }

    [Fact]
    public void EmailTemplateDefinitions_covers_all_event_keys()
    {
        var defKeys = EmailTemplateDefinitions.All.Select(d => d.EventKey).ToHashSet(StringComparer.Ordinal);
        foreach (var k in EmailEventKeys.All)
        {
            Assert.Contains(k, defKeys);
        }
        Assert.Equal(EmailEventKeys.All.Count, EmailTemplateDefinitions.All.Count);
    }

    [Fact]
    public void EmailTemplateMerge_replaces_placeholders()
    {
        var s = EmailTemplateMerge.Apply("Hello {{Name}} #{{JobId}}", new Dictionary<string, string>
        {
            ["Name"] = "Test",
            ["JobId"] = "42"
        });
        Assert.Equal("Hello Test #42", s);
    }

    [Fact]
    public void EmailTemplateMerge_missing_key_becomes_empty()
    {
        var s = EmailTemplateMerge.Apply("x{{Unknown}}y", new Dictionary<string, string>());
        Assert.Equal("xy", s);
    }

    [Fact]
    public void EmailTemplateMerge_LogoUrl_replaced_in_markup()
    {
        var s = EmailTemplateMerge.Apply(
            """<img src="{{LogoUrl}}" alt="x"/>""",
            new Dictionary<string, string> { ["LogoUrl"] = "https://example.com/images/logo.svg" });
        Assert.Contains("https://example.com/images/logo.svg", s, StringComparison.Ordinal);
    }

    [Fact]
    public void EmailLayout_WrapInFormalLayout_includes_inner_and_brand_text_when_no_logo()
    {
        var html = EmailLayout.WrapInFormalLayout(null, "<p>Inner</p>");
        Assert.Contains("Inner", html, StringComparison.Ordinal);
        Assert.Contains(EmailLayout.DefaultCompanyName, html, StringComparison.Ordinal);
        Assert.DoesNotContain("<img", html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void EmailLayout_WrapInFormalLayout_adds_image_when_https_logo()
    {
        var html = EmailLayout.WrapInFormalLayout("https://example.com/logo.png", "<p>x</p>");
        Assert.Contains("https://example.com/logo.png", html, StringComparison.Ordinal);
        Assert.Contains("<img", html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void EmailLayout_HtmlToPlainText_strips_tags()
    {
        var plain = EmailLayout.HtmlToPlainText("<p>a</p><br/>b");
        Assert.Contains("a", plain, StringComparison.Ordinal);
        Assert.Contains("b", plain, StringComparison.Ordinal);
    }

    [Fact]
    public void EmailBrandingOptions_ResolveLogo_joins_base_and_logo_path()
    {
        var o = new EmailBrandingOptions
        {
            PublicWebBaseUrl = "https://example.com",
            LogoPath = "/images/logo.svg"
        };
        Assert.Equal("https://example.com/images/logo.svg", o.ResolveLogoAbsoluteUrl());
    }

    [Fact]
    public void EmailBrandingOptions_ResolveLogo_returns_null_when_base_missing()
    {
        var o = new EmailBrandingOptions { PublicWebBaseUrl = "" };
        Assert.Null(o.ResolveLogoAbsoluteUrl());
    }
}
