namespace PennMUSH.Core.Communication.Channels;

using PennMUSH.Core.Types;

public class ChannelMember
{
    private string _title = string.Empty;
    private bool _muted;
    private bool _hidden;

    public Action? Changed { get; set; }
    public Dbref PlayerId { get; init; }

    public string Title
    {
        get => _title;
        set
        {
            if (_title == value)
                return;
            _title = value ?? string.Empty;
            Changed?.Invoke();
        }
    }

    public bool IsMuted
    {
        get => _muted;
        set
        {
            if (_muted == value)
                return;
            _muted = value;
            Changed?.Invoke();
        }
    }

    public bool IsHidden
    {
        get => _hidden;
        set
        {
            if (_hidden == value)
                return;
            _hidden = value;
            Changed?.Invoke();
        }
    }

    public ChannelMember(Dbref playerId, string title = "")
    {
        PlayerId = playerId;
        _title = title ?? string.Empty;
    }
}
