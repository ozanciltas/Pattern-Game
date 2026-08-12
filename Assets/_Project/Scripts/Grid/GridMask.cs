using System;
using System.Text;

namespace PatternGame.Grid
{
    public readonly struct GridMask : IEquatable<GridMask>
    {
        public const int Width = 4;
        public const int Height = 5;
        public const int CellCount = Width * Height;

        const uint AllCellsBits = (1u << CellCount) - 1u;

        readonly uint bits;

        public GridMask(uint bits)
        {
            this.bits = bits & AllCellsBits;
        }

        public static GridMask Empty => new GridMask(0u);

        public static GridMask Full => new GridMask(AllCellsBits);

        public uint Bits => bits;

        public bool IsEmpty => bits == 0u;

        public bool IsFull => bits == AllCellsBits;

        public int Count => PopCount(bits);

        public GridMask Complement => new GridMask(~bits);

        public static bool IsValidCell(int column, int row)
        {
            return column >= 0 && column < Width && row >= 0 && row < Height;
        }

        public static int ToIndex(int column, int row)
        {
            ThrowIfCellIsInvalid(column, row);
            return row * Width + column;
        }

        public static int ColumnOf(int index)
        {
            ThrowIfIndexIsInvalid(index);
            return index % Width;
        }

        public static int RowOf(int index)
        {
            ThrowIfIndexIsInvalid(index);
            return index / Width;
        }

        public bool IsOccupied(int column, int row)
        {
            return (bits & (1u << ToIndex(column, row))) != 0u;
        }

        public GridMask WithCell(int column, int row)
        {
            return new GridMask(bits | (1u << ToIndex(column, row)));
        }

        public GridMask WithoutCell(int column, int row)
        {
            return new GridMask(bits & ~(1u << ToIndex(column, row)));
        }

        public bool Overlaps(GridMask other)
        {
            return (bits & other.bits) != 0u;
        }

        public bool Contains(GridMask other)
        {
            return (bits & other.bits) == other.bits;
        }

        public bool TryGetBounds(out int minColumn, out int minRow, out int maxColumn, out int maxRow)
        {
            minColumn = 0;
            minRow = 0;
            maxColumn = 0;
            maxRow = 0;

            if (bits == 0u)
            {
                return false;
            }

            minColumn = Width - 1;
            minRow = Height - 1;
            maxColumn = 0;
            maxRow = 0;

            for (int index = 0; index < CellCount; index++)
            {
                if ((bits & (1u << index)) == 0u)
                {
                    continue;
                }

                int column = index % Width;
                int row = index / Width;

                if (column < minColumn) minColumn = column;
                if (column > maxColumn) maxColumn = column;
                if (row < minRow) minRow = row;
                if (row > maxRow) maxRow = row;
            }

            return true;
        }

        public bool TryTranslate(int deltaColumn, int deltaRow, out GridMask result)
        {
            uint translated = 0u;

            for (int index = 0; index < CellCount; index++)
            {
                if ((bits & (1u << index)) == 0u)
                {
                    continue;
                }

                int column = (index % Width) + deltaColumn;
                int row = (index / Width) + deltaRow;

                if (!IsValidCell(column, row))
                {
                    result = Empty;
                    return false;
                }

                translated |= 1u << (row * Width + column);
            }

            result = new GridMask(translated);
            return true;
        }

        public static GridMask operator |(GridMask left, GridMask right)
        {
            return new GridMask(left.bits | right.bits);
        }

        public static GridMask operator &(GridMask left, GridMask right)
        {
            return new GridMask(left.bits & right.bits);
        }

        public static GridMask operator ^(GridMask left, GridMask right)
        {
            return new GridMask(left.bits ^ right.bits);
        }

        public static GridMask operator ~(GridMask mask)
        {
            return mask.Complement;
        }

        public static bool operator ==(GridMask left, GridMask right)
        {
            return left.bits == right.bits;
        }

        public static bool operator !=(GridMask left, GridMask right)
        {
            return left.bits != right.bits;
        }

        public bool Equals(GridMask other)
        {
            return bits == other.bits;
        }

        public override bool Equals(object obj)
        {
            return obj is GridMask other && Equals(other);
        }

        public override int GetHashCode()
        {
            return (int)bits;
        }

        public override string ToString()
        {
            var builder = new StringBuilder(Height * (Width * 2 + 1));

            for (int row = Height - 1; row >= 0; row--)
            {
                for (int column = 0; column < Width; column++)
                {
                    builder.Append(IsOccupied(column, row) ? '#' : '.');

                    if (column < Width - 1)
                    {
                        builder.Append(' ');
                    }
                }

                if (row > 0)
                {
                    builder.Append('\n');
                }
            }

            return builder.ToString();
        }

        static int PopCount(uint value)
        {
            value -= (value >> 1) & 0x55555555u;
            value = (value & 0x33333333u) + ((value >> 2) & 0x33333333u);
            value = (value + (value >> 4)) & 0x0F0F0F0Fu;
            return (int)((value * 0x01010101u) >> 24);
        }

        static void ThrowIfCellIsInvalid(int column, int row)
        {
            if (!IsValidCell(column, row))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(column),
                    $"Cell ({column}, {row}) is outside the {Width}x{Height} grid.");
            }
        }

        static void ThrowIfIndexIsInvalid(int index)
        {
            if (index < 0 || index >= CellCount)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(index),
                    $"Index {index} is outside the range 0..{CellCount - 1}.");
            }
        }
    }
}
