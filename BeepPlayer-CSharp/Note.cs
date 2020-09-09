using System;
using System.Threading;

namespace BeepPlayer
{
	class Note
	{
		//0=sleep
		public int height;
		//in seconds
		public double time;
		//0~1
		public float staccato;
		//uint16_t lyriccoloroff, lyriccoloron;
		//offset of lyric
		public int lyric;
		public Note() { }
		public Note(int height, double time, float staccato, int lyric) {
			this.height = height;
			this.time = time;
			this.staccato = staccato;
			this.lyric = lyric;
		}
		public void Play() {
			if (this.height > 0) {
				Console.Beep(
					(int)(440 * Math.Pow(2, (float)(this.height - 34) / 12)),
					(int)(this.time * 10 * (100 - this.staccato))
				);
				Thread.Sleep((int)(this.time * 10 * this.staccato));
			} else {
				Thread.Sleep((int)this.time * 1000);
			}
		}
	}
}
