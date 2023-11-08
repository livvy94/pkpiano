using System;
using System.Text;

namespace PK_Piano
{
    class MPTColumn
    {
        private const string VALID = "Valid!"; //TODO: make this validation thing less janky

        public static string GetEBMdata(string input)
        {
            var result = new StringBuilder();
            var rows = input.Split(new string[] { "\n", "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

            var rowStatus = Validation(rows);
            if (rowStatus != VALID) return rowStatus; //display the error message if something's up

            foreach (var row in rows)
            {
                if (!row.StartsWith("ModPlug Tracker"))
                    result.Append(EBM_Note_Data.GetEBMNote(row)); //convert all of the notes
            }

            return result.ToString();
        }

        //TODO: Figure out what hex values AddMusicK uses and implement it as "GetAMKdate(string input)"

        private static string Validation(string[] input)
        {
            string errorMessage = VALID;

            if (input.Length < 2)
                errorMessage = "(Not enough rows to process)";
            else if (input[0].Length < 15) //this doesn't work if someone pastes a really long thing in from something else...
                errorMessage = "(That doesn't look like something pasted from OpenMPT)";
            else if (input[1].Length > 12)
                errorMessage = "(Clipboard contains more than one column of notes: Length is " + input[1].Length + ")";

            return errorMessage;
        }
    }
}
