using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using PK_Piano.SPC_File_Editing;
using System;
using System.Collections.Generic;
using System.Media;
using System.Windows.Forms;

namespace PK_Piano
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            echoDelay = 0x00;
            InitializeComponent();
        }

        byte octave = 4; //used in the note buttons' if statements
        byte lastNote;
        string transposeValue = "00"; //this is what will be copied to the clipboard for channel transpose

        byte noteLength = 0x18;
        int multiplier = 1;
        byte noteStacatto = 0x7; //The N-SPC note length format is [length], [staccato and volume], which might look like [18 7F]
        byte noteVolume = 0xF;

        byte echoChannels;
        byte echoVolume; //Change these defaut values to whatever the trackBars' initial values end up to be...
        byte echoDelay;
        byte echoFeedback;
        byte echoFilter;
        readonly ToneGenerator.PlaybackThing playbackThing = new ToneGenerator.PlaybackThing();

        string currentSPCfilepath = string.Empty;
        List<Instrument> loadedInstruments = new List<Instrument>();

        private void SendNote(byte firstOctaveNote)
        {
            //The number we're giving this function is that of the first octave
            //So, apply the octave global variable to get the right value!
            //Except for C8 or C9. Those shouldn't be affected by octave stuff.
            byte result;
            if (firstOctaveNote < 0xC8)
                result = (byte)(firstOctaveNote + (octave - 1) * 0xC);
            else
                result = firstOctaveNote;

            //Takes a byte, puts it in the label, and puts it in the clipboard
            FormatNoteLength();
            var note = $"[{result:X2}]";
            DispLabel.Text = note;
            Clipboard.SetText(note);

            UpdateChannelTranspose(result);
        }

        private void SendNote(string input)
        {
            //An alternate version for manually setting the string
            //Probably only needed for the "XX" ones, which should hopefully never happen
            FormatNoteLength();
            DispLabel.Text = $"[{input}]";
            Clipboard.SetText(input);
        }

        private void CopySingleHex(byte input)
        {
            Clipboard.SetText($"[{input:X2}]");
        }

        private void CopyDoubleHex(byte input1, byte input2)
        {
            Clipboard.SetText($"[{input1:X2} {input2:X2}]");
        }

        private void UpdateChannelTranspose(byte input)
        {
            if (lastNote != 0)
            {
                transposeValue = (input - lastNote).ToString("X2");

                if (transposeValue.Length == 8)
                    transposeValue = transposeValue.Substring(6, 2);

                btnChannelTranspose.Text = $"Transpose (last one was [{transposeValue}])";
            }

            lastNote = input;
        }

        private void FormatNoteLength()
        {
            var multipliedLength = noteLength * multiplier;
            var output = $"[{multipliedLength:X2} {noteStacatto:X}{noteVolume:X}]";

            //show the divide button if the length is bigger than the maximum length a note can have
            btnDividePrompt.Visible = EBM_Note_Data.LengthIsInvalid(multipliedLength);

            LengthDisplay.Text = output;
            Clipboard.SetText(output);
        }

        private void btnDividePrompt_Click(object sender, EventArgs e)
        {
            var lengthResult = EBM_Note_Data.ValidateNoteLength(noteLength * multiplier);
            if (lengthResult[1] == 1) return; //only proceed if division is necessary

            var message = $"Instead of that huge value, use {getWrittenNumber(lengthResult[1])} "
                        + $"notes with this value instead: [{lengthResult[0]:X2}]";

            MessageBox.Show(message, "Divided note length");
        }

        private string getWrittenNumber(int input)
        {
            string writtenNumber = "ERROR";
            switch (input) //this will only be used in note length multipliers, so only these three numbers will ever be needed
            {
                case 2:
                    writtenNumber = "two";
                    break;
                case 3:
                    writtenNumber = "three";
                    break;
                case 4:
                    writtenNumber = "four";
                    break;
            }
            return writtenNumber;
        }

        private void cboNoteLength_TextChanged(object sender, EventArgs e)
        {
            //data validation
            try
            {
                //this might be useful in future projects: http://stackoverflow.com/questions/13158969/from-string-textbox-to-hex-0x-byte-c-sharp
                var userInput = int.Parse(cboNoteLength.Text, System.Globalization.NumberStyles.HexNumber);
                noteLength = (byte)userInput;
            }
            catch
            {
                cboNoteLength.Text = noteLength.ToString("X2");
            }

            FormatNoteLength(); //update other parts of the program that use length
        }

        private void txtMultiplier_TextChanged(object sender, EventArgs e)
        {
            multiplier = (int)txtMultiplier.Value;
            FormatNoteLength();
        }

        private void SetAllEchoValues()
        {
            echoVolume = (byte)trackBarEchoVol.Value;
            echoDelay = (byte)trackBarEchoDelay.Value;
            echoFeedback = (byte)trackBarEchoFeedback.Value;
            echoFilter = (byte)trackBarEchoFilter.Value;
        }

        private void CalculateEchoChannelCode()
        {
            SetAllEchoValues(); //I keep getting 00s until I move one of the sliders, which is annoying. Hopefully this should fix it.

            echoChannels = GetNumberFromBooleans(checkBox8.Checked, checkBox7.Checked, checkBox6.Checked, checkBox5.Checked, checkBox4.Checked, checkBox3.Checked, checkBox2.Checked, checkBox1.Checked);
            CreateEchoCodes();

        }

        private byte GetNumberFromBooleans(bool b1, bool b2, bool b3, bool b4, bool b5, bool b6, bool b7, bool b8)
        {
            byte result = 0; //this converts the bools to a number by adding their respective binary weights
            if (b1) result += 1;
            if (b2) result += 2;
            if (b3) result += 4;
            if (b4) result += 8;
            if (b5) result += 16;
            if (b6) result += 32;
            if (b7) result += 64;
            if (b8) result += 128;
            return result;
        }

        private void CreateEchoCodes()
        {
            //Updates txtEchoDisplay with:
            //[F5 XX YY YY] [F7 XX YY ZZ]
            //Control code syntax:
            //F5 echoChannels volume_L volume_R
            //F7 delay feedback filter
            var output = $"[F5 {echoChannels:X2} {echoVolume:X2} {echoVolume:X2}] "
                       + $"[F7 {echoDelay:X2} {echoFeedback:X2} {echoFilter:X2}]";

            Clipboard.SetText(output);
            txtEchoDisplay.Text = output;
        }

        //Note-related button click events
        private void btnRest_Click(object sender, EventArgs e)
        {
            SendNote(0xC9);
        }

        private void btnContinue_Click(object sender, EventArgs e)
        {
            SendNote(0xC8);
        }

        //These button click events are MASSIVELY simplified from before.
        //Now, the hex value for the note of the lowest octave is passed
        //and it adds it up appropriately!

        private void btnC_Click(object sender, EventArgs e)
        {
            ToneGenerator.PlayTone(playbackThing, "C", octave);
            SendNote(0x80);
        }

        private void btnCsharp_Click(object sender, EventArgs e)
        {
            ToneGenerator.PlayTone(playbackThing, "C#", octave);
            SendNote(0x81);
        }

        private void btnD_Click(object sender, EventArgs e)
        {
            ToneGenerator.PlayTone(playbackThing, "D", octave);
            SendNote(0x82);
        }

        private void btnDsharp_Click(object sender, EventArgs e)
        {
            ToneGenerator.PlayTone(playbackThing, "D#", octave);
            SendNote(0x83);
        }

        private void btnE_Click(object sender, EventArgs e)
        {
            ToneGenerator.PlayTone(playbackThing, "E", octave);
            SendNote(0x84);
        }

        private void btnF_Click(object sender, EventArgs e)
        {
            ToneGenerator.PlayTone(playbackThing, "F", octave);
            SendNote(0x85);
        }

        private void btnFsharp_Click(object sender, EventArgs e)
        {
            ToneGenerator.PlayTone(playbackThing, "F#", octave);
            SendNote(0x86);
        }

        private void btnG_Click(object sender, EventArgs e)
        {
            ToneGenerator.PlayTone(playbackThing, "G", octave);
            SendNote(0x87);
        }

        private void btnGsharp_Click(object sender, EventArgs e)
        {
            ToneGenerator.PlayTone(playbackThing, "G#", octave);
            SendNote(0x88);
        }

        private void btnA_Click(object sender, EventArgs e)
        {
            ToneGenerator.PlayTone(playbackThing, "A", octave);
            SendNote(0x89);
        }

        private void btnAsharp_Click(object sender, EventArgs e)
        {
            ToneGenerator.PlayTone(playbackThing, "A#", octave);
            SendNote(0x8A);
        }

        private void btnB_Click(object sender, EventArgs e)
        {
            ToneGenerator.PlayTone(playbackThing, "B", octave);
            SendNote(0x8B);
        }

        private void btnOctaveDown_Click(object sender, EventArgs e)
        {
            octave = (byte)Math.Max(1, octave - 1);
            OctaveLbl.Text = $"Octave: {octave}";
        }

        private void btnOctaveUp_Click(object sender, EventArgs e)
        {
            octave = (byte)Math.Min(6, octave + 1);
            OctaveLbl.Text = $"Octave: {octave}";
        }

        ///////////////////////////
        //Other button click events
        private void btnChannelTranspose_Click(object sender, EventArgs e)
        {
            Clipboard.SetText($"[EA {transposeValue}]");
        }

        private void btnFinetune1_Click(object sender, EventArgs e)
        {
            //Unfortunately, documenting finetune data in the vanilla ROM is quite an undertaking...
            //Just look at other songs for reference :(
            Clipboard.SetText("[F4 00]");
        }

        private void btnCopySlidingPan_Click(object sender, EventArgs e)
        {
            FormatNoteLength();
            var pan = Math.Abs(PanningBar.Value);
            var output = $"[E2 {noteLength:X2} {pan:X2}]"; //[E2 length panning]
            Clipboard.SetText(output);
        }

        private void btnCopySlidingVolume_Click(object sender, EventArgs e)
        {
            FormatNoteLength();
            var vol = Math.Abs(ChannelVolumeBar.Value);
            var output = $"[EE {noteLength:X2} {vol:X2}]"; //[EE length volume]
            Clipboard.SetText(output);
        }

        private void btnCopySlidingEcho_Click(object sender, EventArgs e)
        {
            FormatNoteLength();
            var vol = Math.Abs(trackBarEchoVol.Value);
            var output = $"[F8 {noteLength:X2} {vol:X2} {vol:X2}]"; //[F8 length vol vol]
            Clipboard.SetText(output);
        }

        private void btnEchoOff_Click(object sender, EventArgs e)
        {
            CopySingleHex(0xF6);
        }

        private void btnTempo_Click(object sender, EventArgs e)
        {
            CopyDoubleHex(0xE7, 0x20);
        }

        private void btnGlobalVolume_Click(object sender, EventArgs e)
        {
            CopyDoubleHex(0xE5, 0xF0);
        }

        private void btnSetFirstDrum_Click(object sender, EventArgs e)
        {
            CopyDoubleHex(0xFA, 0x00);
        }

        private void btnPortamentoUp_Click(object sender, EventArgs e)
        {
            //[F1 start length range]
            Clipboard.SetText("[F1 00 06 01]");
        }

        private void btnPortamentoDown_Click(object sender, EventArgs e)
        {
            //[F2 start length range]
            Clipboard.SetText("[F2 00 06 01]");
        }

        private void btnPortamentoOff_Click(object sender, EventArgs e)
        {
            CopySingleHex(0xF3);
        }

        private void btnPortamento_Click(object sender, EventArgs e)
        {
            Clipboard.SetText("C8 [F9 00 01 ");
        }

        private void btnVibrato_Click(object sender, EventArgs e)
        {
            Clipboard.SetText("[E3 0C 1C 32]");
        }

        private void btnVibratoOff_Click(object sender, EventArgs e)
        {
            CopySingleHex(0xE4);
        }

        private void PanningBar_Scroll(object sender, EventArgs e)
        {
            FormatNoteLength();
            var panPosition = PanningBar.Value;
            panPosition = Math.Abs(panPosition); //They're negative numbers, so this makes them positive (takes the absolute value)
            var output = $"[E1 {panPosition:X2}]"; //[E1 panning]
            txtPanningDisplay.Text = output;
            Clipboard.SetText(output);
        }

        private void ChannelVolumeBar_Scroll(object sender, EventArgs e)
        {
            var volume = ChannelVolumeBar.Value;
            var output = $"[ED {volume:X2}]"; //[ED volume]
            txtChannelVolumeDisplay.Text = output;
            Clipboard.SetText(output);
        }

        private void StaccatoBar_Scroll(object sender, EventArgs e)
        {
            noteStacatto = (byte)StaccatoBar.Value;
            FormatNoteLength();
        }

        private void VolBar_Scroll(object sender, EventArgs e)
        {
            noteVolume = (byte)VolBar.Value;
            FormatNoteLength();
        }

        private void btnTremolo_Click(object sender, EventArgs e)
        {
            Clipboard.SetText("[EB 0C 1C 32]"); //TODO: change these defaults to be cooler
        }

        private void btnTremoloOff_Click(object sender, EventArgs e)
        {
            CopySingleHex(0xEC);
        }

        private void btnMPTconvert_Click(object sender, EventArgs e)
        {
            var mptColumnText = Clipboard.GetText(); //get clipboard contents and validate it
            if (string.IsNullOrWhiteSpace(mptColumnText)) return;
            if (!mptColumnText.Contains("ModPlug Tracker"))
            {
                MessageBox.Show("Please copy something from OpenMPT!");
                return;
            }

            var result = MPTColumn.GetEBMdata(mptColumnText); //convert the OpenMPT note data to N-SPC format
            Clipboard.SetText(result);
        }

        private void btnC8eraser_Click(object sender, EventArgs e)
        {
            //VALIDATION
            string clipboardContents = Clipboard.GetText();
            if (string.IsNullOrWhiteSpace(clipboardContents)) return; //only continue if there's something there
            if (clipboardContents.Contains("Not enough rows to process")) return;
            clipboardContents = clipboardContents.Replace("[", "").Replace("]", ""); //get rid of brackets

            List<byte> input;

            //Get the note sequence from the clipboard
            //try
            //{
            //    input = SongPiece.StringToBytes(clipboardContents);
            //}
            //catch(Exception ex)
            //{
            //    MessageBox.Show(ex.Message);
            //    return;
            //}

            input = SongPiece.StringToBytes("0C A7 C8 C8"); //DIY Unit Test
            List<byte> goodVersion = SongPiece.StringToBytes("24 A7");
            List<byte> optimizedBytes = SongPiece.StringToBytes(ProcessBytes(input));
            RunTest(optimizedBytes, goodVersion);

            Clipboard.SetText(optimizedBytes.ToString());
        }

        public static string ProcessBytes(List<byte> bytesInput)
        {
            const string NO_NOTE_LENGTH_ERROR = "In order for the C8 Eraser function to work correctly, each note needs a note length in front of it.";
            var result = new List<byte>();
            var piece = new SongPiece();
            bool lastByteWasANote = false;
            bool lastByteWasANoteLength = false;
            foreach (var b in bytesInput)
            {
                if (piece.tempNoteCollection.Count > 1)
                {
                    throw new Exception(NO_NOTE_LENGTH_ERROR);
                }

                if (SongPiece.IsNoteLength(b))
                {
                    //Skip redundant note lengths
                    if (b == piece.noteLength && piece.noteLengthMultiplier == 1)
                        continue;

                    if (lastByteWasANote)
                    {
                        //Add the contents of tempNoteCollection to the result
                        result.AddRange(piece.ParseHex());
                        lastByteWasANote = false;
                        piece = new SongPiece(); //clear out everything and start from scratch
                    }

                    if (lastByteWasANoteLength)
                    {
                        piece.noteStyle = b; //It's a note style, like 7F
                        lastByteWasANoteLength = false;
                    }
                    else
                    {
                        piece.noteLength = b;
                        lastByteWasANoteLength = true;
                    }

                    continue;
                }

                if (SongPiece.IsNote(b))
                {
                    lastByteWasANote = true;
                    lastByteWasANoteLength = false; //just in case
                    if (b == 0xC8)
                    {
                        piece.noteLengthMultiplier++; //keep track of how many C8s there have been
                    }
                    else
                    {
                        piece.tempNoteCollection.Add(b); //add the note to the collection
                    }
                    continue;
                }

                if (SongPiece.IsEffect(b))
                {
                    //I'm abandoning this functionality, but if anyone would like to try
                    //implementing it, I'd greatly appreciate it.
                    throw new Exception($"Effects such as {b:X2} are not supported yet.");
                }
            }

            if (piece.noteLength == 0xFF)
            {
                throw new Exception(NO_NOTE_LENGTH_ERROR);
            }

            //Add any remaining notes to result
            result.AddRange(piece.ParseHex());

            //Convert the byte array to a string
            var resultString = "";
            foreach (var num in result)
            {
                resultString += num.ToString("X2") + ' ';
            }

            return resultString.TrimEnd(' ');
        }

        public static void RunTest(List<byte> resultBytes, List<byte> goodVersionBytes)
        {
            //DIY Unit Test
            var result = string.Empty;
            foreach (var b in resultBytes)
            {
                result += b.ToString("X2") + " "; //convert the list back into a string
            }

            var goodVersion = string.Empty;
            foreach (var b in goodVersionBytes)
            {
                goodVersion += b.ToString("X2") + " "; //convert the list back into a string
            }

            result = result.Trim();
            goodVersion = goodVersion.Trim();

            var message = string.Empty;
            if (result == goodVersion)
                message += "*** PASS ***\r\n";
            else
                message += "*** FAIL ***\r\n";
            message += "Result:\r\n" + result + "\r\nDesired output:\r\n" + goodVersion;
            MessageBox.Show(message);
        }

        private void trackBarEchoVol_Scroll(object sender, EventArgs e)
        {
            SetAllEchoValues();
            CreateEchoCodes();
        }

        private void trackBarEchoDelay_Scroll(object sender, EventArgs e)
        {
            SetAllEchoValues();
            CreateEchoCodes();
        }

        private void trackBarEchoFeedback_Scroll(object sender, EventArgs e)
        {
            SetAllEchoValues();
            CreateEchoCodes();
        }

        private void trackBarEchoFilter_Scroll(object sender, EventArgs e)
        {
            SetAllEchoValues();
            CreateEchoCodes();
        }


        //All of the echo-related checkboxes redirect to calculateEchoChannelCode() to provide immediate feedback
        private void checkBox1_CheckedChanged(object sender, EventArgs e) { CalculateEchoChannelCode(); }
        private void checkBox2_CheckedChanged(object sender, EventArgs e) { CalculateEchoChannelCode(); }
        private void checkBox3_CheckedChanged(object sender, EventArgs e) { CalculateEchoChannelCode(); }
        private void checkBox4_CheckedChanged(object sender, EventArgs e) { CalculateEchoChannelCode(); }
        private void checkBox5_CheckedChanged(object sender, EventArgs e) { CalculateEchoChannelCode(); }
        private void checkBox6_CheckedChanged(object sender, EventArgs e) { CalculateEchoChannelCode(); }
        private void checkBox7_CheckedChanged(object sender, EventArgs e) { CalculateEchoChannelCode(); }
        private void checkBox8_CheckedChanged(object sender, EventArgs e) { CalculateEchoChannelCode(); }

        private void Form1_Load(object sender, EventArgs e)
        {
            playbackThing.wo = new WaveOutEvent();
            playbackThing.fadeout = true;

            cboWaveform.SelectedIndex = 1;
            txtMultiplier.TextChanged += txtMultiplier_TextChanged; //The default click event for txtMultiplier doesn't do anything, so this is the next best alternative

            //Tooltip stuff from http://stackoverflow.com/questions/1339524/c-how-do-i-add-a-tooltip-to-a-control
            var toolTip1 = new ToolTip
            {
                AutoPopDelay = 5000,
                InitialDelay = 300,
                ReshowDelay = 700,
                ShowAlways = true
            };

            toolTip1.SetToolTip(trackBarEchoVol, "The second half of volume levels invert the waveform!\r\nYou can set the left and right numbers separately, too.");
            toolTip1.SetToolTip(trackBarEchoDelay, "The higher this is, the less space you have for samples and note data.\r\nBe sure to test your song in an accurate emulator!");

            toolTip1.SetToolTip(checkBox8, "Watch out! \nThis one's used for sound effects.");
            toolTip1.SetToolTip(btnCopySlidingEcho, "[F8 length lvol rvol]\r\nThis uses the current echo volume slider value.");
            toolTip1.SetToolTip(btnSetFirstDrum, "Sets the first sample used by the CA-DF note system.\r\nThis is useful for making quick drum loops.");

            toolTip1.SetToolTip(btnPortamentoUp, "Plays the note, THEN bends the pitch.\r\n[F1 start length range]");
            toolTip1.SetToolTip(btnPortamentoDown, "Bends the pitch INTO the note.\r\n[F2 start length range]");
            toolTip1.SetToolTip(btnVibrato, "[E3 start speed range]");
            toolTip1.SetToolTip(btnPortamento, "C8 [F9 start length (insert note here)] ");

            toolTip1.SetToolTip(btnCopySlidingPan, "[E2 length panning]");
            toolTip1.SetToolTip(btnCopySlidingVolume, "[EE length volume]");

            toolTip1.SetToolTip(btnTremolo, "[EB start speed range]");
            toolTip1.SetToolTip(btnChannelTranspose, "Transpose a channel by a number of semitones.");
            toolTip1.SetToolTip(btnMPTconvert, "Convert a single channel of notes copied from an OpenMPT module.");
            toolTip1.SetToolTip(btnC8eraser, "Take a bunch of notes in the clipboard and attempt to optimize them.");

            toolTip1.SetToolTip(btnTempo, "[E7 tempo]");
            toolTip1.SetToolTip(btnGlobalVolume, "[E5 volume]");

            //toolTip1.SetToolTip(ANYTHING, "");
            //toolTip1.SetToolTip(ANYTHING, "");
            //toolTip1.SetToolTip(ANYTHING, "");
            //toolTip1.SetToolTip(ANYTHING, "");
            //toolTip1.SetToolTip(ANYTHING, "");
        }

        private void btnTuning_Click(object sender, EventArgs e)
        {
            playbackThing.fadeout = false;
            ToneGenerator.PlayTone(playbackThing, "C", octave);
            playbackThing.fadeout = true;
        }

        private void cboWaveform_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch (cboWaveform.SelectedIndex)
            {
                case 0:
                    playbackThing.type = SignalGeneratorType.Sin; break;
                case 1:
                    playbackThing.type = SignalGeneratorType.Square; break;
                case 2:
                    playbackThing.type = SignalGeneratorType.SawTooth; break;
                case 3:
                    playbackThing.type = SignalGeneratorType.Triangle; break;
                default:
                    playbackThing.type = SignalGeneratorType.White; break;
            }
        }

        //ADSR STUFF
        //TODO: ORGANIZE THIS MESS

        private void lblSPCFilename_Click(object sender, EventArgs e)
        {
            //TODO: show just the filename in the title bar. Like PK Piano - foo.spc

            //Clear everything so there isn't any junk data hanging around
            lstInstruments.Items.Clear();
            loadedInstruments.Clear();
            currentSPCfilepath = string.Empty;
            lblSPCFilename.Text = "Click here to load an SPC file...";
            UpdateADSRTextboxes(null);

            currentSPCfilepath = SPC_IO.ShowOFD();
            if (NoFileLoaded()) return; //bad result from the ofd

            loadedInstruments = SPC_IO.LoadInstruments(currentSPCfilepath);

            UpdateInstruments();
        }

        private void UpdateInstruments()
        {
            if (NoFileLoaded()) return;

            foreach (var instrument in loadedInstruments)
            {
                lstInstruments.Items.Add(instrument);
            }

            lblSPCFilename.Text = currentSPCfilepath;
        }

        private bool NoFileLoaded() => currentSPCfilepath == string.Empty;

        private void lstInstruments_SelectedIndexChanged(object sender, EventArgs e)
        {
            //if (lstInstruments.Items.Count < 1) return; //will I need this?
            var currentInstrument = (Instrument)lstInstruments.SelectedItem;
            UpdateADSRTextboxes(currentInstrument);
        }

        private void UpdateADSRTextboxes(Instrument instrument)
        {
            if (instrument != null)
            {
                txtADSR1.Text = instrument.ADSR1.ToString("X2");
                txtADSR2.Text = instrument.ADSR2.ToString("X2");
                txtGAIN.Text = instrument.GAIN.ToString("X2");
                txtTuningMult.Text = instrument.TuningMultiplier.ToString("X2");
                txtTuningSub.Text = instrument.TuningSub.ToString("X2");
            }
            else
            {
                txtADSR1.Clear();
                txtADSR2.Clear();
                txtGAIN.Clear();
                txtTuningMult.Clear();
                txtTuningSub.Clear();
            }
        }

        private void btnApply_Click(object sender, EventArgs e)
        {
            SaveToSPC(currentSPCfilepath);
        }

        private void btnApplyToOtherSPC_Click(object sender, EventArgs e)
        {
            var tempSPCfilepath = SPC_IO.ShowOFD();
            SaveToSPC(tempSPCfilepath);
        }

        private void btnCopyInstrumentTable_Click(object sender, EventArgs e)
        {
            //TODO: Generate a config.txt-style data table and copy it to the clipboard
            var result = string.Empty;
            foreach (var instrument in loadedInstruments)
            {
                result += $"  \"{instrument.Index:X2}.brr\"  ${instrument.ADSR1:X2} ${instrument.ADSR2:X2} ${instrument.GAIN:X2}  ${instrument.TuningMultiplier:X2} ${instrument.TuningSub:X2}\r";
            }

            Clipboard.SetText(result);
        }

        private void SaveToSPC(string path)
        {
            SaveInstrumentChanges();

            var NewInstTable = Instrument.MakeHex(loadedInstruments);
            var itSavedCorrectly = SPC_IO.SaveInstruments(path, NewInstTable);

            if (!itSavedCorrectly)
                MessageBox.Show("Something went wrong while saving to the SPC file!");
        }

        private void SaveInstrumentChanges()
        {
            if (lstInstruments.SelectedIndex == -1)
                return;

            //Change the selected item to match the textboxes!
            Instrument selectedInstrument = (Instrument)lstInstruments.SelectedItem;
            var result = new Instrument(selectedInstrument.Index, HexBox(txtADSR1), HexBox(txtADSR2), HexBox(txtGAIN), HexBox(txtTuningMult), HexBox(txtTuningSub));
            ChangeSelectedInstrument(lstInstruments, result);
        }

        private void ChangeSelectedInstrument(ListBox listbox, Instrument newInstrument)
        {
            //Replace the currently-selected item with an Instrument object based on the textboxes
            var i = listbox.SelectedIndex;
            if (i != -1)
            {
                listbox.Items[i] = newInstrument;
            }
            listbox.SelectedItem = newInstrument;

            //update the global loadedInstruments thing
            loadedInstruments.Clear();
            foreach (var item in listbox.Items)
            {
                loadedInstruments.Add((Instrument)item);
            }
        }

        private byte HexBox(TextBox textbox)
        {
            return byte.Parse(textbox.Text, System.Globalization.NumberStyles.HexNumber);
        }

        private bool ValidateHexBox(TextBox textbox)
        {
            if (textbox.Text == string.Empty)
                return false;

            if (!byte.TryParse(textbox.Text, System.Globalization.NumberStyles.HexNumber, null, out _))
            {
                textbox.Clear();
                return false;
            }

            return true;
        }

        public bool AllTextBoxesAreValid()
        {
            var ADSR1isValid = ValidateHexBox(txtADSR1);
            var ADSR2isValid = ValidateHexBox(txtADSR2);
            var GAINisValid = ValidateHexBox(txtGAIN);
            var multIsValid = ValidateHexBox(txtTuningMult);
            var subIsValid = ValidateHexBox(txtTuningSub);

            return ADSR1isValid && ADSR2isValid && GAINisValid && multIsValid && subIsValid;
        }

        private void txtADSR1_TextChanged(object sender, EventArgs e) => AllTextBoxesAreValid();
        private void txtADSR2_TextChanged(object sender, EventArgs e) => AllTextBoxesAreValid();
        private void txtGAIN_TextChanged(object sender, EventArgs e) => AllTextBoxesAreValid();
        private void txtTuningMult_TextChanged(object sender, EventArgs e) => AllTextBoxesAreValid();
        private void txtTuningSub_TextChanged(object sender, EventArgs e) => AllTextBoxesAreValid();

        private void ADSRhelp_Click(object sender, EventArgs e)
        {
            Clipboard.SetText("https://vince94.neocities.org/cool/ADSR"); //TODO: who the eff is Vince?
            MessageBox.Show("Go to the URL in your clipboard!");
        }
    }
}