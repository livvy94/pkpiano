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

            switch (octave)
            {
                case "1":
                    result = GetHexCode(0x80, note);
                    break;
                case "2":
                    result = GetHexCode(0x8C, note);
                    break;
                case "3":
                    result = GetHexCode(0x98, note);
                    break;
                case "4":
                    result = GetHexCode(0xA4, note);
                    break;
                case "5":
                    result = GetHexCode(0xB0, note);
                    break;
                case "6":
                    result = GetHexCode(0xBC, note);
                    break;
                case ".":
                    result = "C8 "; //tie
                    break;
                case "^":
                    result = "C9 "; //rest
                    break;
                case "=":
                    result = "C8 "; //this is a volume envelope fade, for which N-SPC doesn't have an equivalent
                    break;
            }

            return result;
        }

        static string GetHexCode(int startingHex, string MPTnote)
        {
            int result = 0xFF;
            switch (MPTnote)
            {
                case "C-": //those '-' characters are just placeholders for when a sharp is not present
                    result = startingHex;
                    break;
                case "C#":
                    result = startingHex + 0x01;
                    break;
                case "D-":
                    result = startingHex + 0x02;
                    break;
                case "D#":
                    result = startingHex + 0x03;
                    break;
                case "E-":
                    result = startingHex + 0x04;
                    break;
                case "F-":
                    result = startingHex + 0x05;
                    break;
                case "F#":
                    result = startingHex + 0x06;
                    break;
                case "G-":
                    result = startingHex + 0x07;
                    break;
                case "G#":
                    result = startingHex + 0x08;
                    break;
                case "A-":
                    result = startingHex + 0x09;
                    break;
                case "A#":
                    result = startingHex + 0x0A;
                    break;
                case "B-":
                    result = startingHex + 0x0B;
                    break;
            }

            return result.ToString("X") + " ";
        }

        public static bool lengthIsInvalid(int length)
        {
            if (length > 0x7F)
                return true; //numbers higher than 0x7F count as notes and not note lengths

            return false;
        }

        public static int[] validateNoteLength(int length)
        {
            int multiplier = 1;
            int[] result = new int[2];
            //result[0] is the new length
            //result[1] is the appropriate multiplier
            //...should I make this into a class? Probably.
            
            result[0] = length; //if this makes it to the end, then there was no change necessary
            result[1] = multiplier;
            
            while (lengthIsInvalid(result[0]))
            {
                multiplier++;
                result[0] = length / multiplier;
                result[1] = multiplier;
            }

            return result;
        }
    }
}
