using System;
using System.Windows.Forms;
using System.Media;
using System.Text;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System.Collections.Generic;

namespace PK_Piano
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            echoDelay = 0x00;
            InitializeComponent();
        }

        bool sfxEnabled = false;
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

        private void SendNote(byte input)
        {
            //Takes a byte, puts it in the label, and puts it in the clipboard
            FormatNoteLength();
            var note = "[" + input.ToString("X2") + "]";
            DispLabel.Text = note;
            Clipboard.SetText(note);

            UpdateChannelTranspose(input);
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
            if (sfxEnabled) sfxEquipped.Play();
            Clipboard.SetText($"[{input:X2}]");
        }

        private void CopyDoubleHex(byte input1, byte input2)
        {
            if (sfxEnabled) sfxEquipped.Play();
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

        private string FormatNoteLength()
        {
            var multipliedLength = noteLength * multiplier;
            var output = $"[{multipliedLength:X2} {noteStacatto:X}{noteVolume:X}]";

            //show the divide button if the length is bigger than the maximum length a note can have
            btnDividePrompt.Visible = EBM_Note_Data.lengthIsInvalid(multipliedLength);

            LengthDisplay.Text = output;
            Clipboard.SetText(output);
            return output;
        }

        private void btnDividePrompt_Click(object sender, EventArgs e)
        {
            var lengthResult = EBM_Note_Data.validateNoteLength(noteLength * multiplier);
            if (lengthResult[1] == 1) return; //only proceed if division is necessary

            var message = $"Instead of that huge value, use {getWrittenNumber(lengthResult[1])} "
                        + $"notes with this value instead: [{lengthResult[0]:X2}]";

            MessageBox.Show(message, "Divided note length");
        }

        private string getWrittenNumber(int input)
        {
            string writtenNumber;
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
                default:
                    writtenNumber = "ERROR";
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
            if (sfxEnabled) new SoundPlayer(Properties.Resources.ExtraAudio_UpDown_Tick).Play();
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

            var scratchPaper = ""; //Build up the binary number bit by bit

            scratchPaper += getBinaryNumber(checkBox8.Checked);
            scratchPaper += getBinaryNumber(checkBox7.Checked);
            scratchPaper += getBinaryNumber(checkBox6.Checked);
            scratchPaper += getBinaryNumber(checkBox5.Checked);
            scratchPaper += getBinaryNumber(checkBox4.Checked);
            scratchPaper += getBinaryNumber(checkBox3.Checked);
            scratchPaper += getBinaryNumber(checkBox2.Checked);
            scratchPaper += getBinaryNumber(checkBox1.Checked);

            echoChannels = Convert.ToByte(scratchPaper, 2); //convert scratchPaper to a real byte
            CreateEchoCodes();

            if (sfxEnabled) new SoundPlayer(Properties.Resources.ExtraAudio_The_A_Button).Play();
        }

        private string getBinaryNumber(bool input)
        {
            return input ? "1" : "0";
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

        private void btnC_Click(object sender, EventArgs e)
        {
            ToneGenerator.PlayTone(playbackThing, "C", octave);
            switch (octave)
            {
                case 1:
                    SendNote(0x80);
                    break;
                case 2:
                    SendNote(0x8C);
                    break;
                case 3:
                    SendNote(0x98);
                    break;
                case 4:
                    SendNote(0xA4);
                    break;
                case 5:
                    SendNote(0xB0);
                    break;
                case 6:
                    SendNote(0xBC);
                    break;
                default:
                    SendNote("XX");
                    break;
            }
        }

        private void btnCsharp_Click(object sender, EventArgs e)
        {
            ToneGenerator.PlayTone(playbackThing, "C#", octave);
            switch (octave)
            {
                case 1:
                    SendNote(0x81);
                    break;
                case 2:
                    SendNote(0x8D);
                    break;
                case 3:
                    SendNote(0x99);
                    break;
                case 4:
                    SendNote(0xA5);
                    break;
                case 5:
                    SendNote(0xB1);
                    break;
                case 6:
                    SendNote(0xBD);
                    break;
                default:
                    SendNote("XX");
                    break;
            }
        }

        private void btnD_Click(object sender, EventArgs e)
        {
            ToneGenerator.PlayTone(playbackThing, "D", octave);
            switch (octave)
            {
                case 1:
                    SendNote(0x82);
                    break;
                case 2:
                    SendNote(0x8E);
                    break;
                case 3:
                    SendNote(0x9A);
                    break;
                case 4:
                    SendNote(0xA6);
                    break;
                case 5:
                    SendNote(0xB2);
                    break;
                case 6:
                    SendNote(0xBE);
                    break;
                default:
                    SendNote("XX");
                    break;
            }
        }

        private void btnDsharp_Click(object sender, EventArgs e)
        {
            ToneGenerator.PlayTone(playbackThing, "D#", octave);
            switch (octave)
            {
                case 1:
                    SendNote(0x83);
                    break;
                case 2:
                    SendNote(0x8F);
                    break;
                case 3:
                    SendNote(0x9B);
                    break;
                case 4:
                    SendNote(0xA7);
                    break;
                case 5:
                    SendNote(0xB3);
                    break;
                case 6:
                    SendNote(0xBF);
                    break;
                default:
                    SendNote("XX");
                    break;
            }
        }

        private void btnE_Click(object sender, EventArgs e)
        {
            ToneGenerator.PlayTone(playbackThing, "E", octave);
            switch (octave)
            {
                case 1:
                    SendNote(0x84);
                    break;
                case 2:
                    SendNote(0x90);
                    break;
                case 3:
                    SendNote(0x9C);
                    break;
                case 4:
                    SendNote(0xA8);
                    break;
                case 5:
                    SendNote(0xB4);
                    break;
                case 6:
                    SendNote(0xC0);
                    break;
                default:
                    SendNote("XX");
                    break;
            }
        }

        private void btnF_Click(object sender, EventArgs e)
        {
            ToneGenerator.PlayTone(playbackThing, "F", octave);
            switch (octave)
            {
                case 1:
                    SendNote(0x85);
                    break;
                case 2:
                    SendNote(0x91);
                    break;
                case 3:
                    SendNote(0x9D);
                    break;
                case 4:
                    SendNote(0xA9);
                    break;
                case 5:
                    SendNote(0xB5);
                    break;
                case 6:
                    SendNote(0xC1);
                    break;
                default:
                    SendNote("XX");
                    break;
            }
        }

        private void btnFsharp_Click(object sender, EventArgs e)
        {
            ToneGenerator.PlayTone(playbackThing, "F#", octave);
            switch (octave)
            {
                case 1:
                    SendNote(0x86);
                    break;
                case 2:
                    SendNote(0x92);
                    break;
                case 3:
                    SendNote(0x9E);
                    break;
                case 4:
                    SendNote(0xAA);
                    break;
                case 5:
                    SendNote(0xB6);
                    break;
                case 6:
                    SendNote(0xC2);
                    break;
                default:
                    SendNote("XX");
                    break;
            }
        }

        private void btnG_Click(object sender, EventArgs e)
        {
            ToneGenerator.PlayTone(playbackThing, "G", octave);
            switch (octave)
            {
                case 1:
                    SendNote(0x87);
                    break;
                case 2:
                    SendNote(0x93);
                    break;
                case 3:
                    SendNote(0x9F);
                    break;
                case 4:
                    SendNote(0xAB);
                    break;
                case 5:
                    SendNote(0xB7);
                    break;
                case 6:
                    SendNote(0xC3);
                    break;
                default:
                    SendNote("XX");
                    break;
            }
        }

        private void btnGsharp_Click(object sender, EventArgs e)
        {
            ToneGenerator.PlayTone(playbackThing, "G#", octave);
            switch (octave)
            {
                case 1:
                    SendNote(0x88);
                    break;
                case 2:
                    SendNote(0x94);
                    break;
                case 3:
                    SendNote(0xA0);
                    break;
                case 4:
                    SendNote(0xAC);
                    break;
                case 5:
                    SendNote(0xB8);
                    break;
                case 6:
                    SendNote(0xC4);
                    break;
                default:
                    SendNote("XX");
                    break;
            }
        }

        private void btnA_Click(object sender, EventArgs e)
        {
            ToneGenerator.PlayTone(playbackThing, "A", octave);
            switch (octave)
            {
                case 1:
                    SendNote(0x89);
                    break;
                case 2:
                    SendNote(0x95);
                    break;
                case 3:
                    SendNote(0xA1);
                    break;
                case 4:
                    SendNote(0xAD);
                    break;
                case 5:
                    SendNote(0xB9);
                    break;
                case 6:
                    SendNote(0xC5);
                    break;
                default:
                    SendNote("XX");
                    break;
            }
        }

        private void btnAsharp_Click(object sender, EventArgs e)
        {
            ToneGenerator.PlayTone(playbackThing, "A#", octave);
            switch (octave)
            {
                case 1:
                    SendNote(0x8A);
                    break;
                case 2:
                    SendNote(0x96);
                    break;
                case 3:
                    SendNote(0xA2);
                    break;
                case 4:
                    SendNote(0xAE);
                    break;
                case 5:
                    SendNote(0xBA);
                    break;
                case 6:
                    SendNote(0xC6);
                    break;
                default:
                    SendNote("XX");
                    break;
            }
        }

        private void btnB_Click(object sender, EventArgs e)
        {
            ToneGenerator.PlayTone(playbackThing, "B", octave);
            switch (octave)
            {
                case 1:
                    SendNote(0x8B);
                    break;
                case 2:
                    SendNote(0x97);
                    break;
                case 3:
                    SendNote(0xA3);
                    break;
                case 4:
                    SendNote(0xAF);
                    break;
                case 5:
                    SendNote(0xBB);
                    break;
                case 6:
                    SendNote(0xC7);
                    break;
                default:
                    SendNote("XX");
                    break;
            }
        }

        private void btnOctaveDown_Click(object sender, EventArgs e)
        {
            if (octave <= 1) return;
            octave--;
            OctaveLbl.Text = $"Octave: {octave}";
            if (sfxEnabled) new SoundPlayer(Properties.Resources.ExtraAudio_LeftRight).Play();
        }

        private void btnOctaveUp_Click(object sender, EventArgs e)
        {
            if (octave >= 6) return;
            octave++;
            OctaveLbl.Text = $"Octave: {octave}";
            if (sfxEnabled) new SoundPlayer(Properties.Resources.ExtraAudio_LeftRight).Play();
        }

        ///////////////////////////
        //Other button click events
        private void btnChannelTranspose_Click(object sender, EventArgs e)
        {
            if (sfxEnabled) sfxEquipped.Play();
            Clipboard.SetText("[EA " + transposeValue + "]");
        }

        private void btnFinetune1_Click(object sender, EventArgs e)
        {
            //Unfortunately, documenting finetune data in the vanilla ROM is quite an undertaking...
            //Just look at other songs for reference :(
            if (sfxEnabled) sfxEquipped.Play();
            Clipboard.SetText("[F4 00]");
        }

        private void btnCopySlidingPan_Click(object sender, EventArgs e)
        {
            if (sfxEnabled) sfxEquipped.Play();
            FormatNoteLength();
            var pan = Math.Abs(PanningBar.Value);
            var output = $"[E2 {noteLength:X2} {pan:X2}]"; //[E2 length panning]
            Clipboard.SetText(output);
        }

        private void btnCopySlidingVolume_Click(object sender, EventArgs e)
        {
            if (sfxEnabled) sfxEquipped.Play();
            FormatNoteLength();
            var vol = Math.Abs(ChannelVolumeBar.Value);
            var output = $"[EE {noteLength:X2} {vol:X2}]"; //[EE length volume]
            Clipboard.SetText(output);
        }

        private void btnCopySlidingEcho_Click(object sender, EventArgs e)
        {
            if (sfxEnabled) sfxEquipped.Play();
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
            if (sfxEnabled) sfxEquipped.Play();
            Clipboard.SetText("[F1 00 06 01]");
        }

        private void btnPortamentoDown_Click(object sender, EventArgs e)
        {
            //[F2 start length range]
            if (sfxEnabled) sfxEquipped.Play();
            Clipboard.SetText("[F2 00 06 01]");
        }

        private void btnPortamentoOff_Click(object sender, EventArgs e)
        {
            CopySingleHex(0xF3);
        }

        private void btnPortamento_Click(object sender, EventArgs e)
        {
            if (sfxEnabled) sfxEquipped.Play();
            Clipboard.SetText("C8 [F9 00 01 ");
        }

        private void btnVibrato_Click(object sender, EventArgs e)
        {
            if (sfxEnabled) sfxEquipped.Play();
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
            if (sfxEnabled) sfxTextBlip.Play();
        }

        private void ChannelVolumeBar_Scroll(object sender, EventArgs e)
        {
            var volume = ChannelVolumeBar.Value;
            var output = $"[ED {volume:X2}]"; //[ED volume]
            txtChannelVolumeDisplay.Text = output;
            Clipboard.SetText(output);
            if (sfxEnabled) PlayTextTypeSound("huge");
        }

        private void StaccatoBar_Scroll(object sender, EventArgs e)
        {
            noteStacatto = (byte)StaccatoBar.Value;
            FormatNoteLength();
            if (sfxEnabled) sfxTextBlip.Play();
        }

        private void VolBar_Scroll(object sender, EventArgs e)
        {
            noteVolume = (byte)VolBar.Value;
            FormatNoteLength();
            if (sfxEnabled) PlayTextTypeSound("tiny");
        }

        private void btnTremolo_Click(object sender, EventArgs e)
        {
            if (sfxEnabled) sfxEquipped.Play();
            Clipboard.SetText("[EB 0C 1C 32]"); //TODO: change these defaults to be cooler
        }

        private void btnTremoloOff_Click(object sender, EventArgs e)
        {
            CopySingleHex(0xEC);
        }

        private void btnMPTconvert_Click(object sender, EventArgs e)
        {
            //TODO: Validation

            var mptColumnText = Clipboard.GetText();
            if (string.IsNullOrWhiteSpace(mptColumnText)) return;

            var result = MPTColumn.GetEBMdata(mptColumnText); //convert the OpenMPT note data to N-SPC format
            Clipboard.SetText(result);
            if (sfxEnabled) sfxEquipped.Play();
        }

        private void btnC8eraser_Click(object sender, EventArgs e)
        {
            //VALIDATION
            //string clipboardContents = Clipboard.GetText();
            //if (string.IsNullOrWhiteSpace(clipboardContents)) return; //only continue if there's something there
            //if (clipboardContents.Contains("Not enough rows to process")) return;

            //clipboardContents = clipboardContents.Replace("[", "").Replace("]", ""); //get rid of brackets
            //var input = SongPiece.StringToBytes(clipboardContents);
            var input = SongPiece.StringToBytes("0C 7F A7 C8 0C A7 C8");
            var goodVersion = SongPiece.StringToBytes("18 7F A7 18 A7");
            var optimizedBytes = ProcessBytes(input);

            RunTest(optimizedBytes, goodVersion);
            //Clipboard.SetText(result.ToString());
            if (sfxEnabled) sfxEquipped.Play();
        }

        public static List<byte> ProcessBytes(List<byte> bytesInput)
        {
            var result = new List<byte>();
            var piece = new SongPiece();
            bool lastByteWasANote = false;
            bool lastByteWasANoteLength = false;
            bool lastByteWasAnEffect = false;
            int effectSkip = 0;
            foreach (var b in bytesInput)
            {
                if (effectSkip > 0) //for effects with parameters, which shouldn't be processed
                {
                    effectSkip--;
                    piece.tempNoteCollection.Add(b); //add the effect, byte by byte
                    continue;
                }

                if (lastByteWasAnEffect)
                {
                    result.AddRange(piece.ParseHex());
                    piece = new SongPiece(piece.noteLength, piece.noteLengthMultiplier); //keep the old note settings!
                    lastByteWasAnEffect = false;
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
                        continue;
                    }
                    else
                    {
                        piece.tempNoteCollection.Add(b); //add the note to the collection
                    }
                }

                if (SongPiece.IsEffect(b))
                {
                    effectSkip = SongPiece.GetNumberOfParameters(b);
                    piece.tempNoteCollection.Add(b);
                    lastByteWasAnEffect = true;
                }
            }

            //Add any remaining notes to result
            result.AddRange(piece.ParseHex());
            return result;
        }

        public static void RunTest(List<byte> resultBytes, List<byte> goodVersionBytes)
        {
            //DIY unit test
            string result = "";
            foreach (var b in resultBytes)
            {
                result += b.ToString("X2") + " "; //convert the list back into a string
            }

            string goodVersion = "";
            foreach (var b in goodVersionBytes)
            {
                goodVersion += b.ToString("X2") + " "; //convert the list back into a string
            }

            result = result.Trim();
            goodVersion = goodVersion.Trim();

            var message = "";
            if (result == goodVersion)
                message += "*** PASS ***\r\n";
            else
                message += "*** FAIL ***\r\n";
            message += "Result:\r\n" + result + "\r\nDesired output:\r\n" + goodVersion;
            MessageBox.Show(message);
        }


        //Text blip stuff
        //If the sfxEnabled boolean value is set to true, then various parts of the UI will give audio feedback.
        //I'm getting kind of tired of it, though, so I'm going to make it toggleable in the future.
        byte numberOfLettersBeforeSound;
        SoundPlayer sfxTextBlip = new SoundPlayer(Properties.Resources.ExtraAudio_Text_Blip); //adding these so it doesn't make a new instance of SoundPlayer *every* time
        SoundPlayer sfxEquipped = new SoundPlayer(Properties.Resources.ExtraAudio_Equipped_);

        private void PlayTextTypeSound(string type)
        {
            if (!sfxEnabled) return;

            //Text blip logic
            byte amount = 1;
            if (type == "huge") amount = 5;

            if (numberOfLettersBeforeSound > amount)
            {
                numberOfLettersBeforeSound = 0;
                sfxTextBlip.Play();
            }
            else numberOfLettersBeforeSound++;
        }

        private void trackBarEchoVol_Scroll(object sender, EventArgs e)
        {
            SetAllEchoValues();
            CreateEchoCodes();
            if (sfxEnabled) PlayTextTypeSound("huge");
        }

        private void trackBarEchoDelay_Scroll(object sender, EventArgs e)
        {
            SetAllEchoValues();
            CreateEchoCodes();
            if (sfxEnabled) PlayTextTypeSound("tiny");
        }

        private void trackBarEchoFeedback_Scroll(object sender, EventArgs e)
        {
            SetAllEchoValues();
            CreateEchoCodes();
            if (sfxEnabled) PlayTextTypeSound("huge");
        }

        private void trackBarEchoFilter_Scroll(object sender, EventArgs e)
        {
            SetAllEchoValues();
            CreateEchoCodes();
            if (sfxEnabled) PlayTextTypeSound("tiny");
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

        private void chkMiscFeedback_CheckedChanged(object sender, EventArgs e)
        {
            if (chkMiscFeedback.Checked)
            {
                sfxEnabled = true; //this is one of the global variables defined at the beginning
                new SoundPlayer(Properties.Resources.ExtraAudio_LeftRight).Play();
            }
            else sfxEnabled = false;
        }

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
            if (cboWaveform.SelectedIndex == 0)
                playbackThing.type = SignalGeneratorType.Sin;
            else if (cboWaveform.SelectedIndex == 1)
                playbackThing.type = SignalGeneratorType.Square;
            else if (cboWaveform.SelectedIndex == 2)
                playbackThing.type = SignalGeneratorType.SawTooth;
            else if (cboWaveform.SelectedIndex == 3)
                playbackThing.type = SignalGeneratorType.Triangle;
            else
                playbackThing.type = SignalGeneratorType.White;
        }
    }
}