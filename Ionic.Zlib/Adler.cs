namespace Ionic.Zlib
{
	public sealed class Adler
	{
		private static readonly uint BASE = 65521u;

		private static readonly int NMAX = 5552;

		public static uint Adler32(uint adler, byte[] buf, int index, int len)
		{
			if (buf == null)
			{
				return 1u;
			}
			uint s1 = adler & 0xFFFFu;
			uint s2 = (adler >> 16) & 0xFFFFu;
			while (len > 0)
			{
				int i = ((len < NMAX) ? len : NMAX);
				len -= i;
				while (i >= 16)
				{
					s1 += buf[index++];
					s2 += s1;
					s1 += buf[index++];
					s2 += s1;
					s1 += buf[index++];
					s2 += s1;
					s1 += buf[index++];
					s2 += s1;
					s1 += buf[index++];
					s2 += s1;
					s1 += buf[index++];
					s2 += s1;
					s1 += buf[index++];
					s2 += s1;
					s1 += buf[index++];
					s2 += s1;
					s1 += buf[index++];
					s2 += s1;
					s1 += buf[index++];
					s2 += s1;
					s1 += buf[index++];
					s2 += s1;
					s1 += buf[index++];
					s2 += s1;
					s1 += buf[index++];
					s2 += s1;
					s1 += buf[index++];
					s2 += s1;
					s1 += buf[index++];
					s2 += s1;
					s1 += buf[index++];
					s2 += s1;
					i -= 16;
				}
				if (i != 0)
				{
					do
					{
						s1 += buf[index++];
						s2 += s1;
					}
					while (--i != 0);
				}
				s1 %= BASE;
				s2 %= BASE;
			}
			return (s2 << 16) | s1;
		}
	}
}
