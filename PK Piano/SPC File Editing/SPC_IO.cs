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
            byte[] buffer = new byte[ASM_LENGTH];

            const int ASM_OFFSET = 0x0A6D; //this is the location of the following compiled SPC700 ASM:
            //CF DA 14 60 98 XX 14 98 YY 15
            //where YYXX is the offset we want

            fs.Seek(ASM_OFFSET, SeekOrigin.Begin);
            fs.Read(buffer, 0, ASM_LENGTH);

            int firstbyte = buffer[5];
            int lastbyte = buffer[8];
            int result_offset = (lastbyte << 8) | firstbyte; //turn the two bytes into the offset
            return result_offset + 0x100; //the header in each SPC file containing ID666 tags etc is 0x100 bytes long
        }

        //hex search stuff attempt:
        private int FindASMoffset(string filePath)
        {
            var sequence = new byte[] { 0xCF, 0xDA, 0x14, 0x60, 0x98 };
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                byte[] buffer = new byte[sequence.Length];
                while (fs.Read(buffer, 0, buffer.Length) == buffer.Length)
                {
                    if (buffer.SequenceEqual(sequence))
                    {
                        return (int)fs.Position - buffer.Length;
                    }
                }
            }
            return -1;
        }

        private int FindASMoffset2(string filePath)
        {
            byte[] sequence = new byte[] { 0xC5, 0xDA, 0x14, 0x60, 0x98, 0x14, 0x98, 0x15 };
            var indexes = Enumerable.Range(0, sequence.Length - sequence.Length)
                .Where(i => sequence.Skip(i).Take(5).Skip(1).Take(2).Skip(1).Take(1).SequenceEqual(sequence));

            return indexes.First();
        }

        internal static string ShowOFD()
        {
            var result = string.Empty;
            var ofd = new OpenFileDialog
            {
                Multiselect = false,
                Filter = "SPC files (*.spc)|*.spc"
            };

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                result = ofd.FileName;
            }

            return result; //This probably isn't best-practice...
        }
    }
}
