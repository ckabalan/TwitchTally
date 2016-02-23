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

		public static async void PauseAndExecute(Action action, int timeoutInMilliseconds) {
			await Task.Delay(timeoutInMilliseconds);
			action();
		}

		public static string CombineAfterIndex(string[] InputArr, string Glue, int StartIdx) {
			string OutputStr = String.Empty;
			for (int i = StartIdx; i < InputArr.Length; i++) {
				OutputStr += Glue + InputArr[i];
			}
			return OutputStr.Substring(1);
		}


		public static List<string> Parameterize(string Parameters) {
			List<string> ReturnList = new List<string>();
			while (Parameters.Length > 0) {
				if (Parameters.Substring(0, 1) == "\"") {
					Parameters = Parameters.Substring(1);
					if (Parameters.Contains('"')) {
						ReturnList.Add(Parameters.Substring(0, Parameters.IndexOf('"')));
					} else { return null; }
				} else {
					if (Parameters.Contains(' ')) {
						ReturnList.Add(Parameters.Substring(0, Parameters.IndexOf(' ')));
					} else {
						ReturnList.Add(Parameters);
						break;
					}
				}
				Parameters = Parameters.Substring(ReturnList[ReturnList.Count - 1].Length);
				if (Parameters.Substring(0, 1) == "\"") { Parameters = Parameters.Substring(1); }
				Parameters = Parameters.TrimStart();
			}
			return ReturnList;
		}

		/// <summary>
		/// Counts the number of occurances of Needle in Haystack.
		/// </summary>
		/// <param name="Haystack">String being search</param>
		/// <param name="Needle">String being searched for</param>
		/// <returns>Integer representing the number of occurances of Needle in Haystack</returns>
		public static int OccurancesInString(String Haystack, String Needle) {
			return Regex.Matches(Haystack, Needle).Count;
		}

		/// <summary>
		/// Prints an ArrayList object to the console.
		/// </summary>
		/// <param name="InputArray">ArrayList to print.</param>
		public static void PrintArrayList(ArrayList InputArray) {
			Console.WriteLine("Array Output: ");
			for (int i = 0; i < InputArray.Count; i++) {
				Console.WriteLine("[" + i + "] " + InputArray[i]);
			}
		}

		/// <summary>
		/// Dumps a byte[] Array to a file.
		/// </summary>
		/// <param name="inputBytes">byte[] Array to export.</param>
		/// <param name="FileName">Filename to export to.</param>
		public static void DumpByteArrayToFile(byte[] inputBytes, String FileName) {
			File.Delete(FileName);
			FileStream MusicFileFS = new FileStream(FileName, FileMode.Create, FileAccess.ReadWrite);
			if (MusicFileFS.CanWrite) {
				MusicFileFS.Write(inputBytes, 0, inputBytes.Length);
			}
			MusicFileFS.Close();
		}

		/// <summary>
		/// Dumps a String to a file.
		/// </summary>
		/// <param name="inputString">String to export.</param>
		/// <param name="FileName">Filename to export to.</param>
		public static void DumpStringToFile(String inputString, String FileName) {
			File.Delete(FileName);
			FileStream MusicFileFS = new FileStream(FileName, FileMode.Create, FileAccess.ReadWrite);
			if (MusicFileFS.CanWrite) {
				MusicFileFS.Write(Encoding.UTF8.GetBytes(inputString), 0, Encoding.UTF8.GetBytes(inputString).Length);
			}
			MusicFileFS.Close();
		}

		/// <summary>
		/// Prints the ASCII values of every character in a character array delimited by a pipe character to the Console.
		/// </summary>
		/// <param name="inputCharArr">String to be split.</param>
		public static void PrintCharArrASCII(char[] inputCharArr) {
			Console.Write("|");
			for (int i = 0; i < inputCharArr.Length; i++) {
				Console.Write(((int)inputCharArr[i]).ToString() + "|");
			}
			Console.WriteLine();
		}

		/// <summary>
		/// Prints the ASCII values of every character in a string delimited by a pipe character to the Console.
		/// </summary>
		/// <param name="inputStr">String to be split.</param>
		public static void PrintStringASCII(String inputStr) {
			char[] charArr = inputStr.ToCharArray();
			Console.Write("|");
			for (int i = 0; i < charArr.Length; i++) {
				Console.Write(((int)charArr[i]).ToString() + "|");
			}
			Console.WriteLine();
		}

		/// <summary>
		/// Prints the ASCII values of every character in a character array delimited by a pipe character to the Console.
		/// </summary>
		/// <param name="inputByteArr">String to be split.</param>
		public static void PrintByteArrASCII(byte[] inputByteArr) {
			char[] tempChar = Encoding.ASCII.GetChars(inputByteArr);
			Console.Write("|");
			for (int i = 0; i < tempChar.Length; i++) {
				Console.Write(((int)tempChar[i]).ToString() + "|");
			}
			Console.WriteLine();
		}

		public static string ByteArrToASCII(byte[] inputByteArr) {
			StringBuilder TempSB = new StringBuilder();
			char[] tempChar = Encoding.ASCII.GetChars(inputByteArr);
			TempSB.Append("|");
			for (int i = 0; i < tempChar.Length; i++) {
				TempSB.Append(((int)tempChar[i]).ToString() + "|");
			}
			return TempSB.ToString();
		}

		/// <summary>
		/// Removes all newline characters from a string (ASCII 10/13).
		/// </summary>
		/// <param name="inputStr">String to remove characters from.</param>
		/// <returns>String lacking ASCII 10/13.</returns>
		public static String RemoveNewLineChars(String inputStr) {
			if (inputStr != null) {
				return inputStr.Replace(Convert.ToChar(13).ToString(), "").Replace(Convert.ToChar(10).ToString(), "");
			}
			return null;
		}

		/// <summary>
		/// Returns a string containing the inputStrArr String Array with elemented delimited by Delimiter.
		/// </summary>
		/// <param name="inputStrArr">String Array to enumerate.</param>
		/// <param name="Delimiter">Delimiter to interleave between elements.</param>
		/// <returns>String delimited by Delimiter.</returns>
		public static String StringArrToDelimitedStr(String[] inputStrArr, String Delimiter) {
			String returnStr = "";
			foreach (String tempStr in inputStrArr) {
				returnStr += tempStr + Delimiter;
			}
			if (returnStr.Length > Delimiter.Length) {
				returnStr = returnStr.Substring(0, (returnStr.Length - Delimiter.Length));
			}
			return returnStr;
		}

		/// <summary>
		/// Converts bytes into a human readable format rounded to an accuracy.
		/// </summary>
		/// <param name="Bytes">Number of bytes to be converted.</param>
		/// <param name="Accuracy">Decimal accuracy. Use 0 for no decimal point.</param>
		/// <returns>String in the format of [Number][Unit]</returns>
		public static String BytesToHumanReadable(long Bytes, int Accuracy) {
			if (Bytes < 1073741824) {
				return Math.Round((Bytes / 1048576.0), Accuracy).ToString("0.0") + "MB";
			} else if (Bytes < 1099511627776) {
				return Math.Round((Bytes / 1073741824.0), Accuracy).ToString("0.0") + "GB";
			} else {
				return Math.Round((Bytes / 1099511627776.0), Accuracy).ToString("0.0") + "TB";
			}
		}

		/// <summary>
		/// Converts milliseconds to a clock-like format.
		/// </summary>
		/// <param name="Milliseconds">Number of milliseconds to convert</param>
		/// <returns>Returns a hh:mm:ss formated string.</returns>
		public static String MillisecondsToClockFormat(double Milliseconds) {
			TimeSpan tempTS = new TimeSpan(0, 0, 0, 0, Convert.ToInt32(Milliseconds));
			if (tempTS.Hours > 0) {
				return tempTS.Hours.ToString() + ":" + tempTS.Minutes.ToString("00") + ":" + tempTS.Seconds.ToString("00");
			} else {
				return tempTS.Minutes.ToString() + ":" + tempTS.Seconds.ToString("00");
			}
		}

		/// <summary>
		/// Converts milliseconds to a human readable format.
		/// </summary>
		/// <param name="Milliseconds">Number of milliseconds to convert</param>
		/// <returns>Returns a #d #h #m #s string.</returns>
		public static String MillisecondsToHumanReadable(double Milliseconds) {
			TimeSpan tempTS;
			if (Milliseconds > Int32.MaxValue) {
				tempTS = new TimeSpan(0, 0, 0, Convert.ToInt32((Milliseconds - Int32.MaxValue) / 1000), Int32.MaxValue);
			} else {
				tempTS = new TimeSpan(0, 0, 0, 0, Convert.ToInt32(Milliseconds));
			}
			if (tempTS.Days > 0) {
				return tempTS.Days + "d " + tempTS.Hours.ToString() + "h " + tempTS.Minutes.ToString() + "m " + tempTS.Seconds.ToString() + "s";
			} else if (tempTS.Hours > 0) {
				return tempTS.Hours.ToString() + "h " + tempTS.Minutes.ToString() + "m " + tempTS.Seconds.ToString() + "s";
			} else if (tempTS.Minutes > 0) {
				return tempTS.Minutes.ToString() + "m " + tempTS.Seconds.ToString() + "s";
			} else {
				return tempTS.Seconds.ToString() + "s";
			}
		}


	}
}
