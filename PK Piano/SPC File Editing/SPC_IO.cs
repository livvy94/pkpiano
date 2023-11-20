using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace PK_Piano.SPC_File_Editing
{
    class SPC_IO
    {
        //This was partially coded by ChatGPT. I'm such a hack lol
        const int LENGTH = 6; //how big an instrument table entry is

        public static List<Instrument> LoadInstruments(string path)
        {
            byte[] buffer = new byte[LENGTH];
            var instruments = new List<Instrument>();
            int previousIndex = -1;

            using (var fs = new FileStream(path, FileMode.Open))
            {
                var offset = FindInstrumentTableOffset(fs);

                fs.Seek(offset, SeekOrigin.Begin);
                while (fs.Read(buffer, 0, LENGTH) == LENGTH)
                {
                    byte currentIndex = buffer[0];
                    if (previousIndex != currentIndex - 1) break; //Stop processing if the next entry isn't in sequence

                    instruments.Add(new Instrument(buffer[0], buffer[1], buffer[2], buffer[3], buffer[4], buffer[5]));
                    previousIndex = currentIndex;
                }
            }

            return instruments;
        }

        public static bool SaveInstruments(string path, byte[] newInstrumentTable)
        {
            try
            {
                using (var fs = new FileStream(path, FileMode.Open))
                {
                    var offset = FindInstrumentTableOffset(fs);

                    fs.Seek(offset, SeekOrigin.Begin);
                    foreach (var b in newInstrumentTable)
                    {
                        fs.WriteByte(b); //TODO: find a better way to do this than writing all the bytes individually
                    }
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static int FindInstrumentTableOffset(FileStream fs)
        {
            //TODO: Make this into a hex search thing so it doesn't have to be hard-coded
            const int ASM_LENGTH = 10;
            var buffer = new byte[ASM_LENGTH];

            //Find some compiled ASM which contains the offset of the instrument table.
            //The ASM we are looking for is [CF DA 14 60 98 XX 14 98 YY 15] - YYXX is the offset we want
            byte[] searchPattern = { 0xCF, 0xDA, 0x14, 0x60, 0x98 };

            //Copy the FileStream into a byte array so we can use it in BinaryCounter's search function
            var fileBytes = new byte[fs.Length];
            fs.Read(fileBytes, 0, fileBytes.Length);
            var asmOffset = FindPatternIndexInEnumerable(fileBytes, searchPattern);

            fs.Seek(asmOffset, SeekOrigin.Begin);
            fs.Read(buffer, 0, ASM_LENGTH);

            int firstbyte = buffer[5];
            int lastbyte = buffer[8];
            int result_offset = (lastbyte << 8) | firstbyte; //turn the two bytes into the offset
            return result_offset + 0x100; //the header in each SPC file containing ID666 tags etc is 0x100 bytes long
        }

        static long FindPatternIndexInEnumerable<T>(IEnumerable<T> searchEnumerable, IEnumerable<T> searchPattern)
        {
            //A search function by BinaryCounter (https://github.com/binarycounter)
            for (var i = 0; i <= searchEnumerable.Count() - searchPattern.Count(); i++)
            {
                var sub = searchEnumerable.Skip(i).Take(searchPattern.Count());
                if (Enumerable.SequenceEqual(sub, searchPattern))
                {
                    return i;
                }
            }

            return -1; //Pattern not found
        }

        internal static string ShowOFD()
        {
            using (var ofd = new OpenFileDialog{ Multiselect = false, Filter = "SPC files (*.spc)|*.spc" })
            {
                //return the result, or null if the user hit Cancel
                return ofd.ShowDialog() == DialogResult.OK ? ofd.FileName : null;
            }
        }
    }
}
