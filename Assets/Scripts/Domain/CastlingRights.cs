using System;

namespace ChessVR.Domain
{
    [Flags]
    public enum CastlingRights
    {
        None = 0,
        WhiteKingSide = 1 << 0,
        WhiteQueenSide = 1 << 1,
        BlackKingSide = 1 << 2,
        BlackQueenSide = 1 << 3,
        WhiteBoth = WhiteKingSide | WhiteQueenSide,
        BlackBoth = BlackKingSide | BlackQueenSide,
        All = WhiteBoth | BlackBoth
    }
}
