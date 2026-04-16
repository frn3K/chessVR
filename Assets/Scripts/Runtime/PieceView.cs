using ChessVR.Domain;
using UnityEngine;

namespace ChessVR.Runtime
{
    public sealed class PieceView : MonoBehaviour
    {
        [SerializeField] private string squareNotation;
        [SerializeField] private PieceType pieceType;
        [SerializeField] private PieceColor pieceColor;

        public void Apply(Piece piece, BoardSquare square)
        {
            squareNotation = square.ToString();
            pieceType = piece.Type;
            pieceColor = piece.Color;
            gameObject.name = $"{pieceColor} {pieceType} [{squareNotation}]";
        }
    }
}
