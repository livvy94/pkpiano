using System.Collections.Generic;

namespace PK_Piano.SPC_File_Editing
{
    class Instrument
    {
        public byte Index { get; set; }
        public byte ADSR1 { get; set; }
        public byte ADSR2 { get; set; }
        public byte GAIN { get; set; }
        public byte TuningMultiplier { get; set; }
        public byte TuningSub { get; set; }

        public Instrument(byte index, byte adsr1, byte adsr2, byte gain, byte multiplier, byte sub)
        {
            Index = index;
            ADSR1 = adsr1;
            ADSR2 = adsr2;
            GAIN = gain;
            TuningMultiplier = multiplier;
            TuningSub = sub;
        }

        public override string ToString()
        {
            //This gets used in the ListBox
            //This formatting is meant to match the formatting in EBMusEd's instrument viewer!
            string result = $"{Index:X2}: {ADSR1:X2} {ADSR2:X2} {GAIN:X2}  {TuningMultiplier:X2}{TuningSub:X2}";
            return result;
        }

        public static byte[] MakeHex(List<Instrument> instruments)
        {
            var result = new List<byte>();

            foreach (var instrument in instruments)
            {
                result.Add(instrument.Index);
                result.Add(instrument.ADSR1);
                result.Add(instrument.ADSR2);
                result.Add(instrument.GAIN);
                result.Add(instrument.TuningMultiplier);
                result.Add(instrument.TuningSub);
            }

            return result.ToArray();
        }
    }
}
