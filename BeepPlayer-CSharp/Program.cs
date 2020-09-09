using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace BeepPlayer
{
	class Program
	{
		static void Main(string[] args) {
			//StreamReader sr = new StreamReader(@"a.txt");
			StreamReader sr = new StreamReader(args[0]);
			string str = sr.ReadToEnd();
			DecodeMusicFile(str, out List<Note> notes, out _);
			notes.ForEach(note => note.Play());

			Console.ReadKey();
		}

		static int BUFFER_COUNT = 32;
		static float CHORD_LENGTH = 0.1f;

		static void DecodeMusicFile(string file, out List<Note> notes, out char[] lyric) {
			int i = 0; //file position
			int notecount = 0;
			int octave = 3;
			int length = 4;
			int noteHeight = 0;
			float tempo = 120, staccato = 10;
			ulong chord = ulong.MaxValue;

			notes = new List<Note>();

			lyric = new char[BUFFER_COUNT];
			lyric[0] = '\n';
			lyric[1] = '\0';

			int lyricSaved = 2;
			int lyricAllocated = BUFFER_COUNT;
			int lyricUsed = 2;

			while (i < file.Length) {
				switch (file[i]) {
					case 'a':
					case 'b':
					case 'c':
					case 'd':
					case 'e':
					case 'f':
					case 'g':
					case 'p':
					case 'A':
					case 'B':
					case 'C':
					case 'D':
					case 'E':
					case 'F':
					case 'G':
					case 'P':
					case '0':
					case '1':
					case '2':
					case '3':
					case '4':
					case '5':
					case '6':
					case '7':
					case '8':
					case '9':
					case '.': {

						string strThisLength = GetNumStr(file, ref i);
						if (double.TryParse(strThisLength, out double thisLength) == false || thisLength == 0)
							thisLength = length;

						double time = (double)240 / tempo / (thisLength > 0 ? thisLength : -thisLength);
						int height = GetNoteHeight(file[i], octave, noteHeight);
						int lyr = GetLyric(lyric, ref lyricSaved, ref lyricUsed);
						Note note = new Note(height, time, staccato, lyr);

						i++;
						if (i < file.Length) {
							bool reading = true;
							do {
								switch (file[i]) {
									case '#':
										if (note.height > 0 && note.height < 84) note.height++;
										i++;
										break;
									case 'b':
										if (note.height > 1) note.height--;
										i++;
										break;
									case '.':
										note.time *= 1.5;
										i++;
										break;
									default:
										reading = false;
										break;
								}
							} while (reading);
							notes.Add(note);
							notecount++;
						}
						break;
					}
					case 'o':
					case 'O'://octave
					{
						i++;
						switch (file[i]) {
							case '#':
								if (octave < 7) octave++;
								i++;
								break;
							case 'b':
								if (octave > 0) octave--;
								i++;
								break;
							case '0':
							case '1':
							case '2':
							case '3':
							case '4':
							case '5':
							case '6':
							case '7':
								octave = file[i] - '0';
								i++;
								break;
							default: break;
						}
						break;
					}
					case 't':
					case 'T'://tempo
					{
						i++;
						string strtempo = GetNumStr(file, ref i);
						if (float.TryParse(strtempo, out float newtempo)) {
							if (newtempo > 1000) {
								tempo = 1000;
							} else if (newtempo < 30) {
								tempo = 30;
							} else {
								tempo = newtempo;
							}
						}
						break;
					}
					case 's':
					case 'S': {
						i++;
						string strstaccato = GetNumStr(file, ref i);
						float newstaccato;
						if (float.TryParse(strstaccato, out newstaccato)) {
							if (newstaccato >= 100) {
								staccato = 1;
							} else if (newstaccato <= 0) {
								staccato = 0;
							} else {
								staccato = newstaccato / 100;
							}
						}
						break;
					}
					case 'l':
					case 'L': {
						i++;
						string strlength = GetNumStr(file, ref i);
						if (int.TryParse(strlength, out int newlength)) {
							if (newlength >= 128) {
								length = 128;
							} else if (newlength <= 1) {
								length = 1;
							} else {
								length = newlength;
							}
						}
						break;
					}
					case 'h':
					case 'H': {
						int newheight;
						i++;
						string strHeight = GetNumStr(file, ref i);
						if (strHeight[0] == '\0') {
							switch (file[i]) {
								case '#':
									newheight = noteHeight + 1;
									i++;
									break;
								case 'b':
									newheight = noteHeight - 1;
									i++;
									break;
								default:
									newheight = noteHeight;
									break;
							}
						} else {
							if (int.TryParse(strHeight, out newheight)) break;
						}

						if (newheight >= 12) {
							noteHeight = 12;
						} else if (newheight <= -12) {
							noteHeight = -12;
						} else {
							noteHeight = newheight;
						}

						break;
					}
					case '*': {
						//setlyric(file, &i, lyric, &lyricSaved, &lyricAllocated, &lyricUsed);
						//i++;
						break;
					}
					case '[': {
						i++;
						chord = (ulong)notes.Count;
						break;
					}
					case ']': {
						i++;
						if (chord == ulong.MaxValue) break;
						for (ulong n = chord; n < (ulong)notes.Count - 1; n++) {
							notes[(int)n].time = CHORD_LENGTH;
						}
						notes.Last().time -= CHORD_LENGTH * ((ulong)notes.Count - chord - 1);
						if (notes.Last().time < CHORD_LENGTH) notes.Last().time = CHORD_LENGTH;
						chord = ulong.MaxValue;
						break;
					}
					case '/': {
						if (file[++i] == '/') {
							do {
								i++;
							} while (file[i] != '\n' && file[i] != '\r');
						}
						//i++;
						break;
					}
					case 'r':
					case 'R': {
						octave = 3;
						length = 4;
						noteHeight = 0;
						tempo = 120;
						staccato = 10;
						i++;
						break;
					}
					default:
						i++;
						break;
				}
			}
			//(*notes)[notecount].time = 0;
			//(*notes)[notecount].lyric = 0;
		}

		static string GetNumStr(string strnote, ref int startpointer) {
			//开始时i指向数字的第一位，结束后指向第一位不是数字的字符
			int numcount = 0;
			bool reading = true;
			StringBuilder sb = new StringBuilder();
			do {
				switch (strnote[startpointer]) {
					case '1':
					case '2':
					case '3':
					case '4':
					case '5':
					case '6':
					case '7':
					case '8':
					case '9':
					case '0':
					case '.':
					case '-':
						//numcount指向下一个写入位置
						sb.Append(strnote[startpointer++]);
						break;
					default:
						reading = false;
						break;
				}
			} while (numcount < 15 && reading);
			return sb.ToString();
		}

		static int[] heighttable = new int[] { 0, 10, 12, 1, 3, 5, 6, 8 };
		static char GetNoteHeight(char notechr, int octave, int noteheight) {
			notechr |= (char)0x20;//to lower case
			Debug.Assert(notechr >= 'a');
			if ((notechr & 0x98) != 0) { //-> notechr > 'g'
				Debug.Assert(notechr == 'p');
				return (char)0;
			}
			char dheight = (char)(heighttable[notechr & 0x7] + 12 * octave + noteheight);
			if (dheight > 84) dheight = (char)84;
			if (dheight < 1) dheight = (char)1;
			return dheight;
		}

		static int GetLyric(char[] lyric, ref int lyricSaved, ref int lyricUsed) {
			//歌词文本用/0分隔
			if (lyricUsed >= lyricSaved) {//未缓存歌词，返回\0地址
				return lyricUsed - 1;
			} else {//返回当前歌词的位置，lyricused指向下一个\0 +1
				int i, j;
				for (i = lyricUsed; lyric[i] != '\0'; i++) ;
				j = lyricUsed;
				lyricUsed = i + 1;
				return j;
			}
		}
	}
}
