namespace PennMUSH.Core.Presentation;

public static class TelnetConstants
{
    public const byte IAC = 255;
    public const byte DONT = 254;
    public const byte DO = 253;
    public const byte WONT = 252;
    public const byte WILL = 251;
    public const byte SB = 250;
    public const byte GA = 249;
    public const byte EL = 248;
    public const byte EC = 247;
    public const byte AYT = 246;
    public const byte AO = 245;
    public const byte IP = 244;
    public const byte BREAK = 243;
    public const byte DM = 242;
    public const byte NOP = 241;
    public const byte SE = 240;

    // Telnet Options
    public const byte TELOPT_BINARY = 0;
    public const byte TELOPT_ECHO = 1;
    public const byte TELOPT_SGA = 3;
    public const byte TELOPT_TTYPE = 24;
    public const byte TELOPT_NAWS = 31;
    public const byte TELOPT_CHARSET = 42;
    public const byte TELOPT_PUEBLO = 77;
    public const byte TELOPT_GMCP = 201;
}
