using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using DlxLib;
using System.IO;
using System.Text;
using System.Collections;

namespace ESGF.Sudoku.Spark.Dancinlinks
{
    internal static class Program
    {
        static readonly string _filePath = Path.Combine("/Users/yassine/Documents/GitHub/5ESGF-BD-2021/", "sudoku.csv");

        private static void Main()
        {

            var sudokus = new List<string>();

            foreach (var fileLine in File.ReadLines(_filePath))
            {
               // Console.WriteLine(fileLine);

                sudokus.Add(fileLine);
            }

            int i = 0;
            foreach (string sudoku in sudokus)
            {

                //var sudokus1 = new List<string>();

                //Console.WriteLine(sudoku);
                //sudokus1.Add(sudoku.Substring(0, 9));
                //sudokus1.Add(sudoku.Substring(8, 9));
                //sudokus1.Add(sudoku.Substring(17, 9));
                //sudokus1.Add(sudoku.Substring(26, 9));
                //sudokus1.Add(sudoku.Substring(35, 9));
                //sudokus1.Add(sudoku.Substring(44, 9));
                //sudokus1.Add(sudoku.Substring(53, 9));
                //sudokus1.Add(sudoku.Substring(62, 9));
                //sudokus1.Add(sudoku.Substring(71, 9));

                i = i + 1;

                Console.WriteLine("sudoku n°" + i);

                var grid = new Grid(ImmutableList.Create(
                sudoku.Substring(0, 9),
                sudoku.Substring(9, 9),
                sudoku.Substring(18, 9),
                sudoku.Substring(27, 9),
                sudoku.Substring(36, 9),
                sudoku.Substring(45, 9),
                sudoku.Substring(54, 9),
                sudoku.Substring(63, 9),
                sudoku.Substring(72, 9)));


                //grid.Draw();

                var internalRows = BuildInternalRowsForGrid(grid);
                var dlxRows = BuildDlxRows(internalRows);
                var solutions = new Dlx()
                    .Solve(dlxRows, d => d, r => r)
                    .Where(solution => VerifySolution(internalRows, solution))
                    .ToImmutableList();

                Console.WriteLine();

                if (solutions.Any())
                {
                    Console.WriteLine($"First solution (of {solutions.Count}):");
                    Console.WriteLine();
                    DrawSolution(internalRows, solutions.First());
                    Console.WriteLine();
                }
                else
                {
                    Console.WriteLine("No solutions found!");
                }
            }


        }

        private static IEnumerable<int> Rows => Enumerable.Range(0, 9);
        private static IEnumerable<int> Cols => Enumerable.Range(0, 9);
        private static IEnumerable<Tuple<int, int>> Locations =>
            from row in Rows
            from col in Cols
            select Tuple.Create(row, col);
        private static IEnumerable<int> Digits => Enumerable.Range(1, 9);

        private static IImmutableList<Tuple<int, int, int, bool>> BuildInternalRowsForGrid(Grid grid)
        {
            var rowsByCols =
                from row in Rows
                from col in Cols
                let value = grid.ValueAt(row, col)
                select BuildInternalRowsForCell(row, col, value);

            return rowsByCols.SelectMany(cols => cols).ToImmutableList();
        }

        private static IImmutableList<Tuple<int, int, int, bool>> BuildInternalRowsForCell(int row, int col, int value)
        {
            if (value >= 1 && value <= 9)
                return ImmutableList.Create(Tuple.Create(row, col, value, true));

            return Digits.Select(v => Tuple.Create(row, col, v, false)).ToImmutableList();
        }

        private static IImmutableList<IImmutableList<int>> BuildDlxRows(
            IEnumerable<Tuple<int, int, int, bool>> internalRows)
        {
            return internalRows.Select(BuildDlxRow).ToImmutableList();
        }

        private static IImmutableList<int> BuildDlxRow(Tuple<int, int, int, bool> internalRow)
        {
            var row = internalRow.Item1;
            var col = internalRow.Item2;
            var value = internalRow.Item3;
            var box = RowColToBox(row, col);

            var posVals = Encode(row, col);
            var rowVals = Encode(row, value - 1);
            var colVals = Encode(col, value - 1);
            var boxVals = Encode(box, value - 1);

            return posVals.Concat(rowVals).Concat(colVals).Concat(boxVals).ToImmutableList();
        }

        private static int RowColToBox(int row, int col)
        {
            return row - (row % 3) + (col / 3);
        }

        private static IEnumerable<int> Encode(int major, int minor)
        {
            var result = new int[81];
            result[major * 9 + minor] = 1;
            return result.ToImmutableList();
        }

        private static bool VerifySolution(
            IReadOnlyList<Tuple<int, int, int, bool>> internalRows,
            Solution solution)
        {
            var solutionInternalRows = solution.RowIndexes
                .Select(rowIndex => internalRows[rowIndex])
                .ToImmutableList();

            var locationsGroupedByRow = Locations.GroupBy(t => t.Item1);
            var locationsGroupedByCol = Locations.GroupBy(t => t.Item2);
            var locationsGroupedByBox = Locations.GroupBy(t => RowColToBox(t.Item1, t.Item2));

            return
                CheckGroupsOfLocations(solutionInternalRows, locationsGroupedByRow, "row") &&
                CheckGroupsOfLocations(solutionInternalRows, locationsGroupedByCol, "col") &&
                CheckGroupsOfLocations(solutionInternalRows, locationsGroupedByBox, "box");
        }

        private static bool CheckGroupsOfLocations(
            IEnumerable<Tuple<int, int, int, bool>> solutionInternalRows,
            IEnumerable<IGrouping<int, Tuple<int, int>>> groupedLocations,
            string tag)
        {
            return groupedLocations.All(grouping =>
                CheckLocations(solutionInternalRows, grouping, grouping.Key, tag));
        }

        private static bool CheckLocations(
            IEnumerable<Tuple<int, int, int, bool>> solutionInternalRows,
            IEnumerable<Tuple<int, int>> locations,
            int key,
            string tag)
        {
            var digits = locations.SelectMany(location =>
                solutionInternalRows
                    .Where(solutionInternalRow =>
                        solutionInternalRow.Item1 == location.Item1 &&
                        solutionInternalRow.Item2 == location.Item2)
                    .Select(t => t.Item3));
            return CheckDigits(digits, key, tag);
        }

        private static bool CheckDigits(
            IEnumerable<int> digits,
            int key,
            string tag)
        {
            var actual = digits.OrderBy(v => v);
            if (actual.SequenceEqual(Digits)) return true;
            var values = string.Concat(actual.Select(n => Convert.ToString(n)));
            Console.WriteLine($"{tag} {key}: {values} !!!");
            return false;
        }

        private static Grid SolutionToGrid(
            IReadOnlyList<Tuple<int, int, int, bool>> internalRows,
            Solution solution)
        {
            var rowStrings = solution.RowIndexes
                .Select(rowIndex => internalRows[rowIndex])
                .OrderBy(t => t.Item1)
                .ThenBy(t => t.Item2)
                .GroupBy(t => t.Item1, t => t.Item3)
                .Select(value => string.Concat(value))
                .ToImmutableList();
            return new Grid(rowStrings);
        }

        private static void DrawSolution(
            IReadOnlyList<Tuple<int, int, int, bool>> internalRows,
            Solution solution)
        {
            SolutionToGrid(internalRows, solution).Draw();
        }
    }
}