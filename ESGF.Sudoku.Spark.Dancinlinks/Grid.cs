﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace ESGF.Sudoku.Spark.Dancinlinks

{
    public class Grid
    {
        private const char SpaceCharacter = ' ';
        private const char ZeroCharacter = '0';
        private static readonly char[] ValidChars = { SpaceCharacter, '1', '2', '3', '4', '5', '6', '7', '8', '9' };
        private readonly IImmutableList<IImmutableList<int>> _rows;

        public Grid(IImmutableList<string> rowStrings)
        {
            if (rowStrings == null) throw new ArgumentNullException(nameof(rowStrings));
            if (rowStrings.Count != 9) throw new ArgumentException(nameof(rowStrings));

            var rows = new List<IImmutableList<int>>();

            foreach (var rowString in rowStrings)
            {
                var row = new List<int>();

                if (rowString.Length != 9) throw new ArgumentException(nameof(rowStrings));

                foreach (var ch in rowString)
                {
                    if (!ValidChars.Contains(ch)) throw new ArgumentException(nameof(rowStrings));
                    row.Add(ch == SpaceCharacter ? 0 : ch - ZeroCharacter);
                }

                rows.Add(row.ToImmutableList());
            }

            _rows = rows.ToImmutableList();
        }

        public int ValueAt(int row, int col)
        {
            return _rows[row][col];
        }

        private static void DrawSeparatorLine(string first, string last, string sep)
        {
            var part = string.Concat(Enumerable.Repeat(CentreHorizontal, 3));
            DrawLine(first, last, sep, part, part, part);
        }

        public void Draw()
        {
            DrawSeparatorLine(CornerTopLeft, CornerTopRight, HorizontalAndDown);
            DrawRow(0);
            DrawRow(1);
            DrawRow(2);
            DrawSeparatorLine(VerticalAndRight, VerticalAndLeft, HorizontalAndVertical);
            DrawRow(3);
            DrawRow(4);
            DrawRow(5);
            DrawSeparatorLine(VerticalAndRight, VerticalAndLeft, HorizontalAndVertical);
            DrawRow(6);
            DrawRow(7);
            DrawRow(8);
            DrawSeparatorLine(CornerBottomLeft, CornerBottomRight, HorizontalAndUp);
        }

        //Modification par rapport au code original pour résoudre un problème d'encodage. Passage en signes UTF-8.
        private static readonly string CornerTopLeft = "-";
        private static readonly string CornerTopRight = "-";
        private static readonly string CornerBottomLeft = "-";
        private static readonly string CornerBottomRight = "-";
        private static readonly string CentreHorizontal = "-";
        private static readonly string CentreVertical = "⎜";
        private static readonly string VerticalAndRight = "⎜";
        private static readonly string VerticalAndLeft = "⎜";
        private static readonly string HorizontalAndUp = "-";
        private static readonly string HorizontalAndDown = "-";
        private static readonly string HorizontalAndVertical = "-";
        //Fin de la modification

        private void DrawRow(int row)
        {
            var part1 = FormatThreeValues(row, 0);
            var part2 = FormatThreeValues(row, 3);
            var part3 = FormatThreeValues(row, 6);
            DrawLine(CentreVertical, CentreVertical, CentreVertical, part1, part2, part3);
        }

        private string FormatThreeValues(int row, int skip)
        {
            return string.Concat(_rows[row].Skip(skip).Take(3)).Replace(ZeroCharacter, SpaceCharacter);
        }

        private static void DrawLine(
            string first,
            string last,
            string sep,
            string part1,
            string part2,
            string part3)
        {
            Console.WriteLine($"{first}{part1}{sep}{part2}{sep}{part3}{last}");
        }
    }
}