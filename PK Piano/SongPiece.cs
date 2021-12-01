using System;
using System.Collections.Generic;

namespace PK_Piano
{
    class SongPiece
    {
		public byte noteLength;
		public byte noteLengthMultiplier;
		public List<byte> tempNoteCollection;
		public byte noteStyle;

		public SongPiece()
        {
			noteLength = 0xFF;
			noteLengthMultiplier = 1;
			tempNoteCollection = new List<byte>();
			noteStyle = 0xFF;
        }

		public SongPiece(byte length, byte multiplier)
        {
			noteLength = length;
			noteLengthMultiplier = multiplier;
			tempNoteCollection = new List<byte>();
			noteStyle = 0xFF;
		}

		public List<byte> ParseHex()
		{
            var result = new List<byte>
            {
                (byte)(noteLength * noteLengthMultiplier)
            };

            if (noteStyle != 0xFF)
				result.Add(noteStyle);

			result.AddRange(tempNoteCollection);
			return result;
		}

        public static bool IsNoteLength(byte b) => b <= 0x7F;
        public static bool IsNote(byte b) => b >= 0x80 && b <= 0xDF;
		public static bool IsEffect(byte b) => b >= 0xE0;

		public static int GetNumberOfParameters(byte b)
        {
			switch (b)
            {
				case 0xE4:
			    case 0xEC:
				case 0xF3:
				case 0xF6:
				case 0xFC:
				case 0xFD:
				case 0xFE:
				case 0xFF:
					return 0; //these commands have no parameters
				case 0xE0:
				case 0xE1:
				case 0xE5:
				case 0xE7:
				case 0xE9:
				case 0xEA:
				case 0xED:
				case 0xF0:
				case 0xF4:
				case 0xFA:
					return 1;
				case 0xE2:
				case 0xE6:
				case 0xE8:
				case 0xEE:
				case 0xFB:
					return 2;
				case 0xE3:
				case 0xEB:
				case 0xF1:
				case 0xF2:
				case 0xF5:
				case 0xF7:
				case 0xF8:
				case 0xF9:
					return 3;
				case 0xEF:
					throw new Exception("The [EF] command is unimplemented.\r\nPlease use the *subroutine,count syntax instead.");
            }
			return 0;
        }

		//TODO: Switch statement for returning an int of how many parameters an effect has in it!

		public static List<byte> StringToBytes(string input)
		{
			var bytes = input.Split(' ');
			var bytesInput = new List<byte>();
			foreach (var b in bytes)
			{
				bytesInput.Add(Convert.ToByte(b, 16));
			}

			return bytesInput;
		}
	}
}
