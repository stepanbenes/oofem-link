using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OofemLink.Common.Encoding
{
	/// <summary>
	/// see: https://github.com/WelcomWeb/zBase32j/blob/master/src/se/welcomweb/utils/ZBase32j.java
	/// </summary>
	public class ZBaseEncoder
	{
		readonly char[] encoding = "ybndrfg8ejkmcpqxot1uwisza345h769".ToCharArray();
		readonly byte[] decoding;

		public ZBaseEncoder()
		{
			this.decoding = new byte[0x80];
			for (int i = 0; i < this.encoding.Length; i++)
			{
				this.decoding[this.encoding[i]] = (byte)i;
			}
		}

		public string Encode(string input)
		{
			byte[] bytes = System.Text.Encoding.UTF8.GetBytes(input);
			return this.Encode(bytes);
		}

		public string Encode(byte[] input)
		{
			StringBuilder output = new StringBuilder();

			int special = input.Length % 5;
			int normal = input.Length - special;

			for (int i = 0; i < normal; i += 5)
			{
				output.Append(
						encoding[((input[i] & 0xff) >> 3) & 0x1f]
					);
				output.Append(
						encoding[(((input[i] & 0xff) << 2) | ((input[i + 1] & 0xff) >> 6)) & 0x1f]
					);
				output.Append(
						encoding[((input[i + 1] & 0xff) >> 1) & 0x1f]
					);
				output.Append(
						encoding[(((input[i + 1] & 0xff) << 4) | ((input[i + 2] & 0xff) >> 4)) & 0x1f]
					);
				output.Append(
						encoding[(((input[i + 2] & 0xff) << 1) | ((input[i + 3] & 0xff) >> 7)) & 0x1f]
					);
				output.Append(
						encoding[((input[i + 3] & 0xff) >> 2) & 0x1f]
					);
				output.Append(
						encoding[(((input[i + 3] & 0xff) << 3) | ((input[i + 4] & 0xff) >> 5)) & 0x1f]
					);
				output.Append(
						encoding[(input[i + 4] & 0xff) & 0x1f]
					);
			}

			switch (special)
			{
				case 1:
					output.Append(
							encoding[((input[normal] & 0xff) >> 3) & 0x1f]
						);
					output.Append(
							encoding[((input[normal] & 0xff) >> 2) & 0x1f]
						);
					output.Append(
							"======"
						);
					break;
				case 2:
					output.Append(
							encoding[((input[normal] & 0xff) >> 3) & 0x1f]
						);
					output.Append(
							encoding[(((input[normal] & 0xff) << 2) | ((input[normal + 1] & 0xff) >> 6)) & 0x1f]
						);
					output.Append(
							encoding[((input[normal + 1] & 0xff) >> 1) & 0x1f]
						);
					output.Append(
							encoding[((input[normal + 1] & 0xff) << 4) & 0x1f]
						);
					output.Append(
							"===="
						);
					break;
				case 3:
					output.Append(
							encoding[((input[normal] & 0xff) >> 3) & 0x1f]
						);
					output.Append(
							encoding[(((input[normal] & 0xff) << 2) | ((input[normal + 1] & 0xff) >> 6)) & 0x1f]
						);
					output.Append(
							encoding[((input[normal + 1] & 0xff) >> 1) & 0x1f]
						);
					output.Append(
							encoding[(((input[normal + 1] & 0xff) << 4) | ((input[normal + 2] & 0xff) >> 4)) & 0x1f]
						);
					output.Append(
							encoding[((input[normal + 2] & 0xff) << 1) & 0x1f]
						);
					output.Append(
							"==="
						);
					break;
				case 4:
					output.Append(
							encoding[((input[normal] & 0xff) >> 3) & 0x1f]
						);
					output.Append(
							encoding[(((input[normal] & 0xff) << 2) | ((input[normal + 1] & 0xff) >> 6)) & 0x1f]
						);
					output.Append(
							encoding[((input[normal + 1] & 0xff) >> 1) & 0x1f]
						);
					output.Append(
							encoding[(((input[normal + 1] & 0xff) << 4) | ((input[normal + 2] & 0xff) >> 4)) & 0x1f]
						);
					output.Append(
							encoding[(((input[normal + 2] & 0xff) << 1) | ((input[normal + 3] & 0xff) >> 7)) & 0x1f]
						);
					output.Append(
							encoding[((input[normal + 3] & 0xff) >> 2) & 0x1f]
						);
					output.Append(
							encoding[((input[normal + 3] & 0xff) << 3) & 0x1f]
						);
					output.Append(
							"="
						);
					break;
			}
			// trim padding at the end, because it can be added later in Decode method
			return output.ToString().TrimEnd('=');
		}

		public string Decode(string input)
		{
			int expOrgSize = (int)Math.Floor(input.Length / 1.6);
			int expPadSize = ((int)Math.Ceiling(expOrgSize / 5.0)) * 8;

			StringBuilder s = new StringBuilder(input);
			for (int i = 0; i < expPadSize; i++)
			{
				s.Append("=");
			}

			char[] data = s.ToString().ToLower().ToCharArray();
			int dataLen = data.Length;
			while (dataLen > 0)
			{
				if (!this.ignore(data[dataLen - 1]))
					break;

				dataLen--;
			}

			List<byte> output = new List<byte>();
			int e = dataLen - 8;
			for (int i = this.next(data, 0, e); i < e; i = this.next(data, i, e))
			{
				byte b1 = decoding[data[i++]];
				i = this.next(data, i, e);
				byte b2 = decoding[data[i++]];
				i = this.next(data, i, e);
				byte b3 = decoding[data[i++]];
				i = this.next(data, i, e);
				byte b4 = decoding[data[i++]];
				i = this.next(data, i, e);
				byte b5 = decoding[data[i++]];
				i = this.next(data, i, e);
				byte b6 = decoding[data[i++]];
				i = this.next(data, i, e);
				byte b7 = decoding[data[i++]];
				i = this.next(data, i, e);
				byte b8 = decoding[data[i++]];

				output.Add((byte)((b1 << 3) | (b2 >> 2)));
				output.Add((byte)((b2 << 6) | (b3 << 1) | (b4 >> 4)));
				output.Add((byte)((b4 << 4) | (b5 >> 1)));
				output.Add((byte)((b5 << 7) | (b6 << 2) | (b7 >> 3)));
				output.Add((byte)((b7 << 5) | b8));
			}

			if (data[dataLen - 6] == '=')
			{
				output.Add((byte)(((decoding[data[dataLen - 8]]) << 3) | (decoding[data[dataLen - 7]] >> 2)));
			}
			else if (data[dataLen - 4] == '=')
			{
				output.Add((byte)(((decoding[data[dataLen - 8]]) << 3) | (decoding[data[dataLen - 7]] >> 2)));
				output.Add((byte)(((decoding[data[dataLen - 7]]) << 6) | (decoding[data[dataLen - 6]] << 1) | (decoding[data[dataLen - 5]] >> 4)));
			}
			else if (data[dataLen - 3] == '=')
			{
				output.Add((byte)(((decoding[data[dataLen - 8]]) << 3) | (decoding[data[dataLen - 7]] >> 2)));
				output.Add((byte)(((decoding[data[dataLen - 7]]) << 6) | (decoding[data[dataLen - 6]] << 1) | (decoding[data[dataLen - 5]] >> 4)));
				output.Add((byte)(((decoding[data[dataLen - 5]]) << 4) | (decoding[data[dataLen - 4]] >> 1)));
			}
			else if (data[dataLen - 1] == '=')
			{
				output.Add((byte)(((decoding[data[dataLen - 8]]) << 3) | (decoding[data[dataLen - 7]] >> 2)));
				output.Add((byte)(((decoding[data[dataLen - 7]]) << 6) | (decoding[data[dataLen - 6]] << 1) | (decoding[data[dataLen - 5]] >> 4)));
				output.Add((byte)(((decoding[data[dataLen - 5]]) << 4) | (decoding[data[dataLen - 4]] >> 1)));
				output.Add((byte)(((decoding[data[dataLen - 4]]) << 7) | (decoding[data[dataLen - 3]] << 2) | (decoding[data[dataLen - 2]] >> 3)));
			}
			else
			{
				output.Add((byte)(((decoding[data[dataLen - 8]]) << 3) | (decoding[data[dataLen - 7]] >> 2)));
				output.Add((byte)(((decoding[data[dataLen - 7]]) << 6) | (decoding[data[dataLen - 6]] << 1) | (decoding[data[dataLen - 5]] >> 4)));
				output.Add((byte)(((decoding[data[dataLen - 5]]) << 4) | (decoding[data[dataLen - 4]] >> 1)));
				output.Add((byte)(((decoding[data[dataLen - 4]]) << 7) | (decoding[data[dataLen - 3]] << 2) | (decoding[data[dataLen - 2]] >> 3)));
				output.Add((byte)(((decoding[data[dataLen - 2]]) << 5) | (decoding[data[dataLen - 1]])));
			}

			byte[] b = this.toPrimitive(output.ToArray());
			return this.trim(System.Text.Encoding.UTF8.GetString(b));
		}

		private string trim(string s)
		{
			char[] c = s.ToCharArray();
			int end = c.Length;

			for (int i = c.Length - 1; i >= 0; i--)
			{
				if (((int)c[i]) != 0)
					break;

				end = i;
			}

			return s.Substring(0, end);
		}

		private int next(char[] data, int i, int e)
		{
			while ((i < e) && this.ignore(data[i]))
				i++;

			return i;
		}

		private bool ignore(char c)
		{
			return (c == '\n') || (c == '\r') || (c == '\t') || (c == ' ') || (c == '-');
		}

		private byte[] toPrimitive(Byte[] bytes)
		{
			byte[] result = new byte[bytes.Length];
			for (int i = 0; i < bytes.Length; i++)
			{
				result[i] = bytes[i];
			}

			return result;
		}
	}
}
