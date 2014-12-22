using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using UnityEngine;

namespace ConsolePP
{
	public static class GeneralUtil
	{
		/// <summary>
		/// Method that limits the length of text to a defined length.
		/// </summary>
		/// <param name="source">The source text.</param>
		/// <param name="maxLength">The maximum limit of the string to return.</param>
		public static string LimitLength(this string source, int maxLength)
		{
			if( maxLength <= 0 )
				return "";
			
			if (source.Length <= maxLength)
				return source;
			
			return source.Substring(0, maxLength);
		}
		public static bool IsNullOrEmpty(this string s)
		{
			return string.IsNullOrEmpty(s);
		}

		public static bool IsNotNullOrEmpty(this string s)
		{
			return !s.IsNullOrEmpty();
		}

		public static string Clip(this string s, int maxVisibleChars)
		{
			return s.Clip(maxVisibleChars, false);
		}

		public static string Clip(this string s, int maxVisibleChars, bool ellipsis)
		{
			string newS = "";

			if (maxVisibleChars > 0)
			{
				newS += s.Substring(0, Math.Min(s.Length, maxVisibleChars));
				if (ellipsis) newS += s.Length - maxVisibleChars > 0 ? "..." : "";
			}
			else if (maxVisibleChars < 0)
			{
				if (ellipsis) newS += s.Length - Math.Abs(maxVisibleChars) > 0 ? "..." : "";
				newS += s.Substring(Math.Max(0, s.Length - Math.Abs(maxVisibleChars)));
			}

			return newS;
		}
		
		public static string Last(this string s, int length)
		{
			if (length >= s.Length)
				return s;
			return s.Substring(s.Length - length);
		}
		
		public static int Count(this string s, char match, int startIndex)
		{
			return s.Substring(startIndex).Count(match);
		}
		
		public static int Count(this string s, char match, int startIndex, int lastIndex)
		{
			return s.Substring(startIndex, lastIndex - startIndex).Count(match);
		}
		
		public static int Count(this string s, char match)
		{
			int count = 0;
			foreach (char c in s)
			{
				if (c == match) ++count;
			}
			return count;
		}

		public static string ToHex(this Color color)
		{
			string hex = ((int)(color.r * 255)).ToString("X2") + ((int)(color.g * 255)).ToString("X2") + ((int)(color.b * 255)).ToString("X2");
			return hex;
		}
		
		public static bool FromHex(this Color c, string hex)
		{
			c.r = byte.Parse(hex.Substring(0,2), System.Globalization.NumberStyles.HexNumber);
			c.g = byte.Parse(hex.Substring(2,2), System.Globalization.NumberStyles.HexNumber);
			c.b = byte.Parse(hex.Substring(4,2), System.Globalization.NumberStyles.HexNumber);

			return true;
		}
		public static Color RandomColor()
		{
			float r = UnityEngine.Random.value / 2.0f + 0.5f;
			float g = UnityEngine.Random.value / 2.0f + 0.5f;
			float b = UnityEngine.Random.value / 2.0f + 0.5f;

			return new Color(r, g, b);
		}
	}
}

