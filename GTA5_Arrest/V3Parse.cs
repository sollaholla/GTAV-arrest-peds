﻿using System;
using GTA;
using GTA.Math;

namespace GTA5_Arrest
{
	public static class V3Parse
	{
		/// <summary>
		/// Read a <see cref="Vector3"/> from a <see cref="string"/> representation. Format should be "X:1 Y:2 Z:3". If any number fails to be parsed,
		/// or an error occurs, this will return <see cref="Vector3.Zero"/>.
		/// </summary>
		/// <param name="str"></param>
		/// <returns></returns>
		public static Vector3 Read(string str)
		{
			try
			{
				// if we passed an empty string then just forget it.
				if (string.IsNullOrEmpty(str))
				{
					return Vector3.Zero;
				}

				// start at the 0 mark of the string.
				int currentIndex = 0;

				// we're gonna use the helper function to get our values, we made a helper to avoid code duplication.
				string x = GetNum(str, 'X', ref currentIndex);
				string y = GetNum(str, 'Y', ref currentIndex);
				string z = GetNum(str, 'Z', ref currentIndex);

				// we'll use these as the new values.
				float newX;
				float newY;
				float newZ;

				// if we succeed in parsing them all, then we return the vector3.
				if (float.TryParse(x, out newX) && float.TryParse(y, out newY) && float.TryParse(z, out newZ))
				{
					return new Vector3(newX, newY, newZ);
				}

				return Vector3.Zero;
			}
			catch (Exception)
			{
				// couldn't parse the string so we just return an empty vector3.
				return Vector3.Zero;
			}
		}

		// val: 'X', 'Y', or 'Z'
		// str: the string representation of the vector3, should be "X:1 Y:2: Z:3"
		// currentIndex: we use this to move up to the next number after parsing the first.
		private static string GetNum(string str, char val, ref int currentIndex)
		{
			// we're gonna start at the index of the character we want.
			int startVal = str.IndexOf(val, currentIndex);

			// then we go to the index of the space so now we should have this as a string (e.g.) "X:-1.5 "
			int endVal = str.IndexOf(' ', currentIndex) - startVal;

			// this indicated we're probably at the end of the string (we assume).
			if (endVal == -1)
				endVal = str.Length - startVal;

			// then we trim off the other letters so we're left with (e.g.) "-1.5"
			string strVal = str.Substring(startVal, endVal).Trim(val, ':', ' ');

			// we increment the end index to that we can move to the next part of the string!
			currentIndex = endVal + 1;

			// then we return the string value we got earlier.
			return strVal;
		}
	}
}
