using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Ionic.Crc
{
	[Guid("ebc25cf6-9120-4283-b972-0e5520d0000C")]
	[ComVisible(true)]
	[ClassInterface(ClassInterfaceType.AutoDispatch)]
	public class CRC32
	{
		private uint dwPolynomial;

		private long _TotalBytesRead;

		private bool reverseBits;

		private uint[] crc32Table;

		private const int BUFFER_SIZE = 8192;

		private uint _register = uint.MaxValue;

		public long TotalBytesRead => _TotalBytesRead;

		public int Crc32Result => (int)(~_register);

		public int GetCrc32(Stream input)
		{
			return GetCrc32AndCopy(input, null);
		}

		public int GetCrc32AndCopy(Stream input, Stream output)
		{
			if (input == null)
			{
				throw new Exception("The input stream must not be null.");
			}
			byte[] buffer = new byte[8192];
			int readSize = 8192;
			_TotalBytesRead = 0L;
			int count = input.Read(buffer, 0, readSize);
			output?.Write(buffer, 0, count);
			_TotalBytesRead += count;
			while (count > 0)
			{
				SlurpBlock(buffer, 0, count);
				count = input.Read(buffer, 0, readSize);
				output?.Write(buffer, 0, count);
				_TotalBytesRead += count;
			}
			return (int)(~_register);
		}

		public int ComputeCrc32(int W, byte B)
		{
			return _InternalComputeCrc32((uint)W, B);
		}

		internal int _InternalComputeCrc32(uint W, byte B)
		{
			return (int)(crc32Table[(W ^ B) & 0xFF] ^ (W >> 8));
		}

		public void SlurpBlock(byte[] block, int offset, int count)
		{
			if (block == null)
			{
				throw new Exception("The data buffer must not be null.");
			}
			for (int i = 0; i < count; i++)
			{
				int x = offset + i;
				byte b = block[x];
				if (reverseBits)
				{
					uint temp2 = (_register >> 24) ^ b;
					_register = (_register << 8) ^ crc32Table[temp2];
				}
				else
				{
					uint temp = (_register & 0xFFu) ^ b;
					_register = (_register >> 8) ^ crc32Table[temp];
				}
			}
			_TotalBytesRead += count;
		}

		public void UpdateCRC(byte b)
		{
			if (reverseBits)
			{
				uint temp2 = (_register >> 24) ^ b;
				_register = (_register << 8) ^ crc32Table[temp2];
			}
			else
			{
				uint temp = (_register & 0xFFu) ^ b;
				_register = (_register >> 8) ^ crc32Table[temp];
			}
		}

		public void UpdateCRC(byte b, int n)
		{
			while (n-- > 0)
			{
				if (reverseBits)
				{
					uint temp2 = (_register >> 24) ^ b;
					_register = (_register << 8) ^ crc32Table[(temp2 >= 0) ? temp2 : (temp2 + 256)];
				}
				else
				{
					uint temp = (_register & 0xFFu) ^ b;
					_register = (_register >> 8) ^ crc32Table[(temp >= 0) ? temp : (temp + 256)];
				}
			}
		}

		private static uint ReverseBits(uint data)
		{
			uint ret = data;
			ret = ((ret & 0x55555555) << 1) | ((ret >> 1) & 0x55555555u);
			ret = ((ret & 0x33333333) << 2) | ((ret >> 2) & 0x33333333u);
			ret = ((ret & 0xF0F0F0F) << 4) | ((ret >> 4) & 0xF0F0F0Fu);
			return (ret << 24) | ((ret & 0xFF00) << 8) | ((ret >> 8) & 0xFF00u) | (ret >> 24);
		}

		private static byte ReverseBits(byte data)
		{
			int num = data * 131586;
			uint i = 17055760u;
			uint s = (uint)num & i;
			uint t = (uint)(num << 2) & (i << 1);
			return (byte)(16781313 * (s + t) >> 24);
		}

		private void GenerateLookupTable()
		{
			crc32Table = new uint[256];
			byte i = 0;
			do
			{
				uint dwCrc = i;
				for (byte j = 8; j > 0; j = (byte)(j - 1))
				{
					dwCrc = (((dwCrc & 1) != 1) ? (dwCrc >> 1) : ((dwCrc >> 1) ^ dwPolynomial));
				}
				if (reverseBits)
				{
					crc32Table[ReverseBits(i)] = ReverseBits(dwCrc);
				}
				else
				{
					crc32Table[i] = dwCrc;
				}
				i = (byte)(i + 1);
			}
			while (i != 0);
		}

		private uint gf2_matrix_times(uint[] matrix, uint vec)
		{
			uint sum = 0u;
			int i = 0;
			while (vec != 0)
			{
				if ((vec & 1) == 1)
				{
					sum ^= matrix[i];
				}
				vec >>= 1;
				i++;
			}
			return sum;
		}

		private void gf2_matrix_square(uint[] square, uint[] mat)
		{
			for (int i = 0; i < 32; i++)
			{
				square[i] = gf2_matrix_times(mat, mat[i]);
			}
		}

		public void Combine(int crc, int length)
		{
			uint[] even = new uint[32];
			uint[] odd = new uint[32];
			if (length == 0)
			{
				return;
			}
			uint crc2 = ~_register;
			odd[0] = dwPolynomial;
			uint row = 1u;
			for (int i = 1; i < 32; i++)
			{
				odd[i] = row;
				row <<= 1;
			}
			gf2_matrix_square(even, odd);
			gf2_matrix_square(odd, even);
			uint len2 = (uint)length;
			do
			{
				gf2_matrix_square(even, odd);
				if ((len2 & 1) == 1)
				{
					crc2 = gf2_matrix_times(even, crc2);
				}
				len2 >>= 1;
				if (len2 == 0)
				{
					break;
				}
				gf2_matrix_square(odd, even);
				if ((len2 & 1) == 1)
				{
					crc2 = gf2_matrix_times(odd, crc2);
				}
				len2 >>= 1;
			}
			while (len2 != 0);
			crc2 ^= (uint)crc;
			_register = ~crc2;
		}

		public CRC32()
			: this(reverseBits: false)
		{
		}

		public CRC32(bool reverseBits)
			: this(-306674912, reverseBits)
		{
		}

		public CRC32(int polynomial, bool reverseBits)
		{
			this.reverseBits = reverseBits;
			dwPolynomial = (uint)polynomial;
			GenerateLookupTable();
		}

		public void Reset()
		{
			_register = uint.MaxValue;
		}
	}
}
