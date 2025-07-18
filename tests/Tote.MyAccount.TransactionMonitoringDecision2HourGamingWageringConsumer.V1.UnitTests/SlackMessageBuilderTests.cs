namespace Tote.MyAccount.TransactionMonitoringDecision2HourGamingWageringConsumer.V1.UnitTests;

public class SlackMessageBuilderTests
{
    [Fact]
    public void AppendKeyValuePair_StringValue_SingleLine()
    {
        var message = new SlackMessageBuilder().AppendKey("Hello World").AppendStringValue("Test").Build();
        Assert.Equal("*Hello World*: Test", message);
    }

    [Fact]
    public void AppendKeyValuePair_StringValue_MultiLine()
    {
        var message = new SlackMessageBuilder().AppendKey("Hello World").AppendStringValue("Test")
                                               .AppendKey("Label").AppendStringValue("Value")
                                               .Build();
        Assert.Equal("*Hello World*: Test\n*Label*: Value", message);
    }

    [Fact]
    public void AppendKeyValuePair_Link_SingleLine()
    {
        var message = new SlackMessageBuilder().AppendKey("Customer ID")
                                               .AppendLink("XXXXXX", new Uri("https://example.com/customer/XXXXXX"))
                                               .Build();
        Assert.Equal("*Customer ID*: <https://example.com/customer/XXXXXX|XXXXXX>", message);
    }

    [Fact]
    public void AppendKeyValuePair_Link_MutliLine()
    {
        var message = new SlackMessageBuilder().AppendKey("Customer ID")
                                               .AppendLink("XXXXXX", new Uri("https://example.com/customer/XXXXXX"))
                                               .AppendKey("Hello World")
                                               .AppendStringValue("Test")
                                               .Build();
        Assert.Equal("*Customer ID*: <https://example.com/customer/XXXXXX|XXXXXX>\n*Hello World*: Test", message);
    }
}
