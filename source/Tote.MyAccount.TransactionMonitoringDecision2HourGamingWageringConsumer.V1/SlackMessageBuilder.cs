using System.Text;

namespace Tote.MyAccount.TransactionMonitoringDecision2HourGamingWageringConsumer.V1;

public interface ISlackMessageBuilder
{
    ISlackMessageKeyValueBuilder AppendKey(string key);
    string Build();
}

public interface ISlackMessageKeyValueBuilder
{
    ISlackMessageBuilder AppendStringValue(string value);
    ISlackMessageBuilder AppendLink(string displayText, Uri url);
}

public class SlackMessageBuilder : ISlackMessageBuilder
{
    private readonly StringBuilder _builder;

    public SlackMessageBuilder()
    {
        _builder = new StringBuilder();
    }

    private SlackMessageBuilder(StringBuilder builder)
    {
        _builder = builder;
    }

    public ISlackMessageKeyValueBuilder AppendKey(string key)
    {
        if (_builder.Length > 0) _builder.AppendLine();
        _builder.Append($"*{key}*: ");
        return new SlackMessageKeyValueBuilder(_builder);
    }

    public string Build() => _builder.ToString();

    private sealed class SlackMessageKeyValueBuilder : ISlackMessageKeyValueBuilder
    {
        private readonly StringBuilder _builder;
        
        internal SlackMessageKeyValueBuilder(StringBuilder builder)
        {
            _builder = builder;
        }

        public ISlackMessageBuilder AppendStringValue(string value)
        {
            _builder.Append(value);
            return new SlackMessageBuilder(_builder);
        }

        public ISlackMessageBuilder AppendLink(string displayText, Uri url)
        {
            _builder.Append('<').Append(url).Append('|').Append(displayText).Append('>');
            return new SlackMessageBuilder(_builder);
        }
    }
}