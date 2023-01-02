namespace PK_Piano
{
    //These classes are a very recent addition. TODO: Move more of the logic to classes like this
    class EBM_Note_Data
    {
        public static string GetEBMNote(string input)
        {
            string octave = input.Substring(3, 1); //C-   4
            string note = input.Substring(1, 2);   //Note Octave
            string result = "";

            //no instrument checking because it'll be different anyways
            const string templengthlol = "06 ";
            switch (octave)
            {
                case "1":
                    result = templengthlol + GetHexCode(0x80, note); break;
                case "2":
                    result = templengthlol + GetHexCode(0x8C, note); break;
                case "3":
                    result = templengthlol + GetHexCode(0x98, note); break;
                case "4":
                    result = templengthlol + GetHexCode(0xA4, note); break;
                case "5":
                    result = templengthlol + GetHexCode(0xB0, note); break;
                case "6":
                    result = templengthlol + GetHexCode(0xBC, note); break;
                case ".":
                    result = "C8 "; break; //tie
                case "^":
                    result = templengthlol + "C9 "; break; //rest
                case "=":
                    result = "C8 "; break; //this is a volume envelope fade, for which N-SPC doesn't have an equivalent
            }

            return result;
        }

        static string GetHexCode(int startingHex, string MPTnote)
        {
            int result = 0xFF;
            int offset = 0;
            switch (MPTnote)
            {
                case "C-": offset = 0; break;
                case "C#": offset = 1; break;
                case "D-": offset = 2; break;
                case "D#": offset = 3; break;
                case "E-": offset = 4; break;
                case "F-": offset = 5; break;
                case "F#": offset = 6; break;
                case "G-": offset = 7; break;
                case "G#": offset = 8; break;
                case "A-": offset = 9; break;
                case "A#": offset = 10; break;
                case "B-": offset = 11; break;
            }
            result = startingHex + offset;
            return result.ToString("X") + " ";
        }

        public static bool LengthIsInvalid(int length)
        {
            return length > 0x7F; //numbers higher than 0x7F count as notes and not note lengths
        }

        public static int[] ValidateNoteLength(int length)
        {
            int multiplier = 1;
            int[] result = new int[2];
            //result[0] is the new length
            //result[1] is the appropriate multiplier
            //...should I make this into a class? Probably.

            result[0] = length; //if this makes it to the end, then there was no change necessary
            result[1] = multiplier;

            while (LengthIsInvalid(result[0]))
            {
                multiplier++;
                result[0] = length / multiplier;
                result[1] = multiplier;
            }

            return result;
        }
    }
}
