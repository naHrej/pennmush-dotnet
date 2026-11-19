namespace PennMUSH.Core.Presentation;

using System;
using System.Collections.Generic;
using System.Text;

public class TelnetNegotiator
{
    private enum State
    {
        Normal,
        Iac,
        Will,
        Wont,
        Do,
        Dont,
        Subnegotiation,
        SubnegIac
    }

    private State _state = State.Normal;
    private byte _subnegOption = 0;
    private bool _hasSubnegOption = false;
    private readonly List<byte> _subnegBuffer = new();
    private readonly List<byte> _outputBytes = new();
    private readonly StringBuilder _textBuffer = new();

    public ClientCapabilities Capabilities { get; }

    public TelnetNegotiator(ClientCapabilities? capabilities = null)
    {
        Capabilities = capabilities ?? new ClientCapabilities();
    }

    public byte[] GetInitialHandshake()
    {
        return new byte[]
        {
            TelnetConstants.IAC, TelnetConstants.DO, TelnetConstants.TELOPT_NAWS,
            TelnetConstants.IAC, TelnetConstants.DO, TelnetConstants.TELOPT_TTYPE,
            TelnetConstants.IAC, TelnetConstants.WILL, TelnetConstants.TELOPT_SGA,
            TelnetConstants.IAC, TelnetConstants.WILL, TelnetConstants.TELOPT_GMCP
        };
    }

    public string ProcessInput(ReadOnlySpan<byte> input)
    {
        _textBuffer.Clear();

        for (int i = 0; i < input.Length; i++)
        {
            byte b = input[i];

            switch (_state)
            {
                case State.Normal:
                    if (b == TelnetConstants.IAC)
                    {
                        _state = State.Iac;
                    }
                    else
                    {
                        _textBuffer.Append((char)b);
                    }
                    break;

                case State.Iac:
                    switch (b)
                    {
                        case TelnetConstants.IAC:
                            _textBuffer.Append((char)TelnetConstants.IAC);
                            _state = State.Normal;
                            break;
                        case TelnetConstants.WILL:
                            _state = State.Will;
                            break;
                        case TelnetConstants.WONT:
                            _state = State.Wont;
                            break;
                        case TelnetConstants.DO:
                            _state = State.Do;
                            break;
                        case TelnetConstants.DONT:
                            _state = State.Dont;
                            break;
                        case TelnetConstants.SB:
                            _subnegBuffer.Clear();
                            _hasSubnegOption = false;
                            _state = State.Subnegotiation;
                            break;
                        default:
                            _state = State.Normal;
                            break;
                    }
                    break;

                case State.Will:
                    HandleWill(b);
                    _state = State.Normal;
                    break;

                case State.Wont:
                    _state = State.Normal;
                    break;

                case State.Do:
                    HandleDo(b);
                    _state = State.Normal;
                    break;

                case State.Dont:
                    _state = State.Normal;
                    break;

                case State.Subnegotiation:
                    if (b == TelnetConstants.IAC)
                    {
                        _state = State.SubnegIac;
                    }
                    else
                    {
                        if (!_hasSubnegOption)
                        {
                            _subnegOption = b;
                            _hasSubnegOption = true;
                        }
                        else
                        {
                            _subnegBuffer.Add(b);
                        }
                    }
                    break;

                case State.SubnegIac:
                    if (b == TelnetConstants.SE)
                    {
                        ProcessSubnegotiation(_subnegOption, _subnegBuffer.ToArray());
                        _state = State.Normal;
                    }
                    else if (b == TelnetConstants.IAC)
                    {
                        _subnegBuffer.Add(TelnetConstants.IAC);
                        _state = State.Subnegotiation;
                    }
                    else
                    {
                        _state = State.Normal;
                    }
                    break;
            }
        }

        string result = _textBuffer.ToString();
        // Check for Pueblo client identification string
        if (result.Contains("PUEBLOCLIENT", StringComparison.OrdinalIgnoreCase))
        {
            Capabilities.SupportsPueblo = true;
            Capabilities.SupportsHtml = true;
            result = result.Replace("PUEBLOCLIENT", "", StringComparison.OrdinalIgnoreCase).Trim();
        }

        return result;
    }

    private void HandleWill(byte option)
    {
        if (option == TelnetConstants.TELOPT_NAWS)
        {
            // Client will send window size subnegotiation
        }
        else if (option == TelnetConstants.TELOPT_TTYPE)
        {
            // Request terminal type: IAC SB TTYPE SEND IAC SE
            _outputBytes.AddRange(new byte[] {
                TelnetConstants.IAC, TelnetConstants.SB, TelnetConstants.TELOPT_TTYPE, 1, TelnetConstants.IAC, TelnetConstants.SE
            });
        }
        else if (option == TelnetConstants.TELOPT_GMCP)
        {
            Capabilities.SupportsGmcp = true;
        }
    }

    private void HandleDo(byte option)
    {
        if (option == TelnetConstants.TELOPT_GMCP)
            Capabilities.SupportsGmcp = true;
    }

    private void ProcessSubnegotiation(byte option, byte[] data)
    {
        if (option == TelnetConstants.TELOPT_GMCP)
        {
            Capabilities.SupportsGmcp = true;
            return;
        }
        if (option == TelnetConstants.TELOPT_NAWS && data.Length >= 4)
        {
            int width = (data[0] << 8) | data[1];
            int height = (data[2] << 8) | data[3];
            if (width > 0) Capabilities.TerminalWidth = width;
            if (height > 0) Capabilities.TerminalHeight = height;
        }
        else if (option == TelnetConstants.TELOPT_TTYPE && data.Length > 1 && data[0] == 0) // IS
        {
            string ttype = Encoding.ASCII.GetString(data, 1, data.Length - 1).Trim();
            Capabilities.TerminalType = ttype;

            if (ttype.Contains("truecolor", StringComparison.OrdinalIgnoreCase) || ttype.Contains("24bit", StringComparison.OrdinalIgnoreCase))
            {
                Capabilities.ColorMode = ColorDepth.TrueColor;
            }
            else if (ttype.Contains("256", StringComparison.OrdinalIgnoreCase))
            {
                Capabilities.ColorMode = ColorDepth.Color256;
            }
        }
    }

    public byte[] ConsumePendingOutput()
    {
        var bytes = _outputBytes.ToArray();
        _outputBytes.Clear();
        return bytes;
    }
}
