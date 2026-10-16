using System.Collections.Generic;
using UnityEngine;

// The 8 x 8 gem matrix and its rules, with no Unity objects in it. Cell (row, col) is index row * 8 + col;
// a value is a gem colour 0..6, or Empty. A "chunk" is 3 or more equal gems in a row or a column;
// a "big chunk" is a cell that starts a run both ways (an L / T / cross).
public class Board
{
	public const int Size = 8, Colors = 7, Empty = -1;

	public readonly int[] cells = new int[Size * Size];

	public int this[int row, int col] { get => cells[row * Size + col]; set => cells[row * Size + col] = value; }

	public void Swap(int r1, int c1, int r2, int c2)
	{
		int t = this[r1, c1];
		this[r1, c1] = this[r2, c2];
		this[r2, c2] = t;
	}

	bool Same(int color, int r, int c) => this[r, c] != Empty && this[r, c] == color;

	// how many equal gems are connected to (r, c) along its row / its column, (r, c) included
	public int RowRun(int r, int c)
	{
		int u = this[r, c], n = 1;
		for (int t = c; t > 0 && Same(u, r, t - 1); t--) n++;
		for (int t = c; t < Size - 1 && Same(u, r, t + 1); t++) n++;
		return n;
	}

	public int ColRun(int r, int c)
	{
		int u = this[r, c], n = 1;
		for (int t = r; t > 0 && Same(u, t - 1, c); t--) n++;
		for (int t = r; t < Size - 1 && Same(u, t + 1, c); t++) n++;
		return n;
	}

	bool IsChunkAt(int r, int c, bool big) => big ? RowRun(r, c) > 2 && ColRun(r, c) > 2 : RowRun(r, c) > 2 || ColRun(r, c) > 2;

	public bool HasChunk(bool big = false)
	{
		for (int r = Size - 1; r >= 0; r--)
			for (int c = 0; c < Size; c++)
				if (IsChunkAt(r, c, big)) return true;
		return false;
	}

	// the cells of the first chunk found, searching from the bottom row up, in the order the old code visited them
	// (left run, right run, up run, down run, then the cell itself). Empty list when there is none.
	public List<int> FindChunk(bool big)
	{
		var found = new List<int>();
		for (int r = Size - 1; r >= 0; r--)
			for (int c = 0; c < Size; c++)
			{
				if (!IsChunkAt(r, c, big)) continue;
				int u = this[r, c];
				if (RowRun(r, c) > 2)
				{
					for (int t = c; t > 0 && Same(u, r, t - 1); t--) found.Add(r * Size + t - 1);
					for (int t = c; t < Size - 1 && Same(u, r, t + 1); t++) found.Add(r * Size + t + 1);
				}
				if (ColRun(r, c) > 2)
				{
					for (int t = r; t > 0 && Same(u, t - 1, c); t--) found.Add((t - 1) * Size + c);
					for (int t = r; t < Size - 1 && Same(u, t + 1, c); t++) found.Add((t + 1) * Size + c);
				}
				found.Add(r * Size + c);
				return found;
			}
		return found;
	}

	// a random full board with no chunk on it
	public void FillRandom()
	{
		for (int i = 0; i < cells.Length; i++) cells[i] = Empty;
		for (int r = 0; r < Size; r++)
			for (int c = 0; c < Size; c++)
				do this[r, c] = Random.Range(0, Colors); while (RowRun(r, c) > 2 || ColRun(r, c) > 2);
	}

	// fills every empty cell with a gem that makes no chunk; returns the cells it filled
	public List<int> FillGaps()
	{
		var filled = new List<int>();
		for (int r = 0; r < Size; r++)
			for (int c = 0; c < Size; c++)
				if (this[r, c] == Empty)
				{
					do this[r, c] = Random.Range(0, Colors); while (RowRun(r, c) > 2 || ColRun(r, c) > 2);
					filled.Add(r * Size + c);
				}
		return filled;
	}

	// for every gem that will fall: (from cell, to cell). Row 0 is the top; gems fall to higher rows.
	public List<(int from, int to)> Falls()
	{
		var falls = new List<(int, int)>();
		for (int r = 0; r < Size - 1; r++)
			for (int c = 0; c < Size; c++)
				if (this[r, c] != Empty)
				{
					int gaps = 0;
					for (int k = r + 1; k < Size; k++) if (this[k, c] == Empty) gaps++;
					if (gaps > 0) falls.Add((r * Size + c, (r + gaps) * Size + c));
				}
		return falls;
	}

	// moves everything down into the empty cells (the old collapseMatrix, same result)
	public void Collapse()
	{
		for (int c = 0; c < Size; c++)
		{
			int write = Size - 1;
			for (int r = Size - 1; r >= 0; r--)
				if (this[r, c] != Empty) this[write--, c] = this[r, c];
			for (; write >= 0; write--) this[write, c] = Empty;
		}
	}

	// is there any swap of two neighbours that makes a chunk?
	public bool HasMove() => FindMove(out _, out _);

	// the first swap (cell a with the cell right of it or below it) that makes a chunk, searched as the old hint did
	public bool FindMove(out int a, out int b)
	{
		for (int r = 0; r < Size; r++)
			for (int c = 0; c < Size; c++)
			{
				if (r < Size - 1 && MakesChunk(r, c, r + 1, c)) { a = r * Size + c; b = (r + 1) * Size + c; return true; }
				if (c < Size - 1 && MakesChunk(r, c, r, c + 1)) { a = r * Size + c; b = r * Size + c + 1; return true; }
			}
		a = b = -1;
		return false;
	}

	bool MakesChunk(int r1, int c1, int r2, int c2)
	{
		Swap(r1, c1, r2, c2);
		bool yes = RowRun(r1, c1) > 2 || ColRun(r1, c1) > 2 || RowRun(r2, c2) > 2 || ColRun(r2, c2) > 2;
		Swap(r1, c1, r2, c2);
		return yes;
	}

	// what a chunk of n gems is worth: 3 -> 100, 4 -> 200, 5 -> 400, 6 -> 800, 7 -> 1600
	public static int ChunkScore(int n) => n >= 3 && n <= 7 ? 100 << (n - 3) : 0;
}
