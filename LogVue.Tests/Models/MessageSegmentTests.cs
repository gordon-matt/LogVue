using System.Globalization;
using LogVue.Models;

namespace LogVue.Tests.Models;

public class MessageSegmentTests
{
    [Fact]
    public void Parse_TagsPropertyValuesAndAppliesFormats()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        var segments = MessageSegment.Parse(
            "Order {OrderId} for {Customer} took {Elapsed:0.00} ms",
            [new("OrderId", "42"), new("Customer", "Contoso"), new("Elapsed", "12.3456")],
            "unused");

        Assert.Equal(
        [
            new MessageSegment("Order ", MessageSegmentKind.Text),
            new MessageSegment("42", MessageSegmentKind.Number, "OrderId"),
            new MessageSegment(" for ", MessageSegmentKind.Text),
            new MessageSegment("Contoso", MessageSegmentKind.String, "Customer"),
            new MessageSegment(" took ", MessageSegmentKind.Text),
            new MessageSegment("12.35", MessageSegmentKind.Number, "Elapsed"),
            new MessageSegment(" ms", MessageSegmentKind.Text),
        ], segments);
    }

    [Fact]
    public void Parse_MissingProperty_KeepsThePlaceholderAsText()
    {
        var segments = MessageSegment.Parse("Hello {Name}", [], "unused");

        Assert.Equal("Hello {Name}", string.Concat(segments.Select(s => s.Text)));
        Assert.All(segments, s => Assert.Equal(MessageSegmentKind.Text, s.Kind));
    }

    [Fact]
    public void Parse_NoTemplate_FallsBackToTheMessage()
    {
        var segments = MessageSegment.Parse("", [], "Rendered");

        Assert.Equal([new MessageSegment("Rendered", MessageSegmentKind.Text)], segments);
    }
}
