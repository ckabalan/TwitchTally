// <copyright file="Functions.cs" company="SpectralCoding.com">
//     Copyright (c) 2016 SpectralCoding
// </copyright>
// <license>
// This file is part of TwitchTally.
// 
// TwitchTally is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// TwitchTally is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with TwitchTally.  If not, see <http://www.gnu.org/licenses/>.
// </license>
// <author>Caesar Kabalan</author>

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace TwitchTallyShared {
	public static class Functions {
		public static async void PauseAndExecute(Action action, Int32 timeoutInMilliseconds) {
			await Task.Delay(timeoutInMilliseconds);
			action();
		}

		public static String CombineAfterIndex(String[] inputArr, String glue, Int32 startIdx) {
			String outputStr = String.Empty;
			for (Int32 i = startIdx; i < inputArr.Length; i++) {
				outputStr += glue + inputArr[i];
			}
			return outputStr.Substring(1);
		}

		public static List<String> Parameterize(String parameters) {
			List<String> returnList = new List<String>();
			while (parameters.Length > 0) {
				if (parameters.Substring(0, 1) == "\"") {
					parameters = parameters.Substring(1);
					if (parameters.Contains('"')) {
						returnList.Add(parameters.Substring(0, parameters.IndexOf('"')));
					} else { return null; }
				} else {
					if (parameters.Contains(' ')) {
						returnList.Add(parameters.Substring(0, parameters.IndexOf(' ')));
					} else {
						returnList.Add(parameters);
						break;
					}
				}
				parameters = parameters.Substring(returnList[returnList.Count - 1].Length);
				if (parameters.Substring(0, 1) == "\"") { parameters = parameters.Substring(1); }
				parameters = parameters.TrimStart();
			}
			return returnList;
		}

		/// <summary>
		/// Counts the number of occurances of Needle in Haystack.
		/// </summary>
		/// <param name="haystack">String being search</param>
		/// <param name="needle">String being searched for</param>
		/// <returns>Integer representing the number of occurances of Needle in Haystack</returns>
		public static Int32 OccurancesInString(String haystack, String needle) {
			return Regex.Matches(haystack, needle).Count;
		}

		/// <summary>
		/// Prints an ArrayList object to the console.
		/// </summary>
		/// <param name="inputArray">ArrayList to print.</param>
		public static void PrintArrayList(ArrayList inputArray) {
			Console.WriteLine("Array Output: ");
			for (Int32 i = 0; i < inputArray.Count; i++) {
				Console.WriteLine("[" + i + "] " + inputArray[i]);
			}
		}

		/// <summary>
		/// Dumps a byte[] Array to a file.
		/// </summary>
		/// <param name="inputBytes">byte[] Array to export.</param>
		/// <param name="fileName">Filename to export to.</param>
		public static void DumpByteArrayToFile(Byte[] inputBytes, String fileName) {
			File.Delete(fileName);
			FileStream fileStream = new FileStream(fileName, FileMode.Create, FileAccess.ReadWrite);
			if (fileStream.CanWrite) {
				fileStream.Write(inputBytes, 0, inputBytes.Length);
			}
			fileStream.Close();
		}

		/// <summary>
		/// Dumps a String to a file.
		/// </summary>
		/// <param name="inputString">String to export.</param>
		/// <param name="fileName">Filename to export to.</param>
		public static void DumpStringToFile(String inputString, String fileName) {
			File.Delete(fileName);
			FileStream fileStream = new FileStream(fileName, FileMode.Create, FileAccess.ReadWrite);
			if (fileStream.CanWrite) {
				fileStream.Write(Encoding.UTF8.GetBytes(inputString), 0, Encoding.UTF8.GetBytes(inputString).Length);
			}
			fileStream.Close();
		}

		/// <summary>
		/// Prints the ASCII values of every character in a character array delimited by a pipe character to the Console.
		/// </summary>
		/// <param name="inputCharArr">String to be split.</param>
		public static void PrintCharArrAscii(Char[] inputCharArr) {
			Console.Write("|");
			foreach (Char curChar in inputCharArr) {
				Console.Write((Int32)curChar + "|");
			}
			Console.WriteLine();
		}

		/// <summary>
		/// Prints the ASCII values of every character in a string delimited by a pipe character to the Console.
		/// </summary>
		/// <param name="inputStr">String to be split.</param>
		public static void PrintStringAscii(String inputStr) {
			Char[] charArr = inputStr.ToCharArray();
			Console.Write("|");
			foreach (Char curChar in charArr) {
				Console.Write((Int32)curChar + "|");
			}
			Console.WriteLine();
		}

		/// <summary>
		/// Prints the ASCII values of every character in a character array delimited by a pipe character to the Console.
		/// </summary>
		/// <param name="inputByteArr">String to be split.</param>
		public static void PrintByteArrAscii(Byte[] inputByteArr) {
			Char[] tempChar = Encoding.ASCII.GetChars(inputByteArr);
			Console.Write("|");
			foreach (Char curChar in tempChar) {
				Console.Write((Int32)curChar + "|");
			}
			Console.WriteLine();
		}

		public static String ByteArrToAscii(Byte[] inputByteArr) {
			StringBuilder stringBuilder = new StringBuilder();
			Char[] tempChar = Encoding.ASCII.GetChars(inputByteArr);
			stringBuilder.Append("|");
			foreach (Char curChar in tempChar) {
				stringBuilder.Append((Int32)curChar + "|");
			}
			return stringBuilder.ToString();
		}

		/// <summary>
		/// Removes all newline characters from a string (ASCII 10/13).
		/// </summary>
		/// <param name="inputStr">String to remove characters from.</param>
		/// <returns>String lacking ASCII 10/13.</returns>
		public static String RemoveNewLineChars(String inputStr) {
			return inputStr?.Replace(Convert.ToChar(13).ToString(), "").Replace(Convert.ToChar(10).ToString(), "");
		}

		/// <summary>
		/// Returns a string containing the inputStrArr String Array with elemented delimited by Delimiter.
		/// </summary>
		/// <param name="inputStrArr">String Array to enumerate.</param>
		/// <param name="delimiter">Delimiter to interleave between elements.</param>
		/// <returns>String delimited by Delimiter.</returns>
		public static String StringArrToDelimitedStr(String[] inputStrArr, String delimiter) {
			String returnStr = "";
			// ReSharper disable once LoopCanBeConvertedToQuery
			foreach (String tempStr in inputStrArr) {
				returnStr += tempStr + delimiter;
			}
			if (returnStr.Length > delimiter.Length) {
				returnStr = returnStr.Substring(0, returnStr.Length - delimiter.Length);
			}
			return returnStr;
		}

		/// <summary>
		/// Converts bytes into a human readable format rounded to an accuracy.
		/// </summary>
		/// <param name="bytes">Number of bytes to be converted.</param>
		/// <param name="accuracy">Decimal accuracy. Use 0 for no decimal point.</param>
		/// <returns>String in the format of [Number][Unit]</returns>
		public static String BytesToHumanReadable(Int64 bytes, Int32 accuracy) {
			if (bytes < 1073741824) {
				return Math.Round(bytes / 1048576.0, accuracy).ToString("0.0") + "MB";
			}
			if (bytes < 1099511627776) {
				return Math.Round(bytes / 1073741824.0, accuracy).ToString("0.0") + "GB";
			}
			return Math.Round(bytes / 1099511627776.0, accuracy).ToString("0.0") + "TB";
		}

		/// <summary>
		/// Converts milliseconds to a clock-like format.
		/// </summary>
		/// <param name="milliseconds">Number of milliseconds to convert</param>
		/// <returns>Returns a hh:mm:ss formated string.</returns>
		public static String MillisecondsToClockFormat(Double milliseconds) {
			TimeSpan tempTs = new TimeSpan(0, 0, 0, 0, Convert.ToInt32(milliseconds));
			if (tempTs.Hours > 0) {
				return tempTs.Hours + ":" + tempTs.Minutes.ToString("00") + ":" + tempTs.Seconds.ToString("00");
			}
			return tempTs.Minutes + ":" + tempTs.Seconds.ToString("00");
		}

		/// <summary>
		/// Converts milliseconds to a human readable format.
		/// </summary>
		/// <param name="milliseconds">Number of milliseconds to convert</param>
		/// <returns>Returns a #d #h #m #s string.</returns>
		public static String MillisecondsToHumanReadable(Double milliseconds) {
			TimeSpan tempTs;
			// ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
			if (milliseconds > Int32.MaxValue) {
				tempTs = new TimeSpan(0, 0, 0, Convert.ToInt32((milliseconds - Int32.MaxValue) / 1000), Int32.MaxValue);
			} else {
				tempTs = new TimeSpan(0, 0, 0, 0, Convert.ToInt32(milliseconds));
			}
			if (tempTs.Days > 0) {
				return tempTs.Days + "d " + tempTs.Hours + "h " + tempTs.Minutes + "m " + tempTs.Seconds + "s";
			}
			if (tempTs.Hours > 0) {
				return tempTs.Hours + "h " + tempTs.Minutes + "m " + tempTs.Seconds + "s";
			}
			if (tempTs.Minutes > 0) {
				return tempTs.Minutes + "m " + tempTs.Seconds + "s";
			}
			return tempTs.Seconds + "s";
		}
	}
}
