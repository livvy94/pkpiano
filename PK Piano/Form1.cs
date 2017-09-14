using System;
using System.Windows.Forms;
using System.Media;


namespace PK_Piano
{
    public partial class Form1: Form
    {
        //Global variables
        byte octave = 4; //use this with the note buttons' if statements

        byte noteLength = 0x18;
        int multiplier = 1;
        byte noteStacatto = 0x7;
        byte noteVolume = 0xF;
        
        byte echoChannels = 0x00;
        byte echoVolume = 0x00; //Change these defaut values to whatever the trackBars' initial values end up to be
        byte echoDelay = 0x00;
        byte echoFeedback = 0x00;
        byte echoFilter = 0x00;

        byte numberOfLettersBeforeSound = 0; //double-check this
        

        public Form1()
        { InitializeComponent(); }

        private void playTextTypeSound(string type)
        { //Text blip logic
            byte amount = 1;

            if (type == "huge") amount = 5;
            
            
            if (numberOfLettersBeforeSound > amount)
            {
                numberOfLettersBeforeSound = 0;
                new SoundPlayer(Properties.Resources.ExtraAudio_Text_Blip).Play();
            }
                
            else
            {
                numberOfLettersBeforeSound++;
            }
            
        }

        private void StaccatoBar_Scroll(object sender, EventArgs e)
        {
            noteStacatto = (byte)StaccatoBar.Value;
            LengthDisplay.Text = formatNoteLength();
            new SoundPlayer(Properties.Resources.ExtraAudio_Text_Blip).Play();
        }

        private void VolBar_Scroll(object sender, EventArgs e)
        {
            noteVolume = (byte)VolBar.Value;
            LengthDisplay.Text = formatNoteLength();
            playTextTypeSound("tiny");
        }

        private string formatNoteLength()
        {
            string output = "["
                 + (noteLength * multiplier).ToString("X2")
                 + " " 
                 + noteStacatto.ToString("X")
                 + noteVolume.ToString("X")
                 + "]";
            Clipboard.SetText(output);
            return output;
        }

        private void cboNoteLength_TextChanged(object sender, EventArgs e)
        {
            //data validation
            try
            {
                int userInput = int.Parse(cboNoteLength.Text, System.Globalization.NumberStyles.HexNumber); //http://stackoverflow.com/questions/13158969/from-string-textbox-to-hex-0x-byte-c-sharp
                noteLength = (byte)userInput;
            }
            catch
            {

                cboNoteLength.Text = noteLength.ToString("X2");
            }
            
            //update other parts of the program that use length
            lengthUpdate();

        }

        private void lengthUpdate()
        {
            LengthDisplay.Text = formatNoteLength();
            //there used to be other textboxes being updated here
        }

        private void txtMultiplier_TextChanged(object sender, EventArgs e)
        {
            multiplier = (int)txtMultiplier.Value;
            LengthDisplay.Text = formatNoteLength();
            new SoundPlayer(Properties.Resources.ExtraAudio_UpDown_Tick).Play();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            txtMultiplier.TextChanged += new EventHandler(txtMultiplier_TextChanged);
            //The default click event for txtMultiplier doesn't do anything, so this is the next best alternative


            //Tooltip stuff from http://stackoverflow.com/questions/1339524/c-how-do-i-add-a-tooltip-to-a-control
            // Create the ToolTip and associate with the Form container.
            ToolTip toolTip1 = new ToolTip();

            // Set up the delays for the ToolTip.
            toolTip1.AutoPopDelay = 5000;
            toolTip1.InitialDelay = 300;
            toolTip1.ReshowDelay = 700;
            // Force the ToolTip text to be displayed whether or not the form is active.
            toolTip1.ShowAlways = true;

            toolTip1.SetToolTip(this.trackBarEchoDelay, "If this number is too high, it might glitch the instruments in-game.\r\nBe sure to test it out in an accurate emulator like SNES9X!");
            toolTip1.SetToolTip(this.btnVibrato, "[E3 start speed range]");
            toolTip1.SetToolTip(this.btnPortamentoUp, "Plays the note, THEN bends the pitch.\r\n[F1 start length range]");
            toolTip1.SetToolTip(this.btnPortamentoDown, "Bends the pitch INTO the note.\r\n[F2 start length range]");
            toolTip1.SetToolTip(this.checkBox8, "Watch out! \nThis one's used for sound effects.");
            toolTip1.SetToolTip(this.btnCopySlidingPan, "[E2 length panning]");
            toolTip1.SetToolTip(this.btnCopySlidingVolume, "[EE length volume]");
            toolTip1.SetToolTip(this.btnCopySlidingEcho, "[F8 length lvol rvol]");
            toolTip1.SetToolTip(this.btnPortamento, "C8 [F9 start length (insert note here)] ");
            toolTip1.SetToolTip(this.btnSetFirstDrum, "Sets the first sample used by the CA-DF note system.\r\nThis is useful for making quick drum loops.");
            toolTip1.SetToolTip(this.trackBarEchoVol, "The second half of volume levels invert the waveform!\r\nYou can set the left and right numbers seperately, too.");
            //toolTip1.SetToolTip(this.ANYTHING, "");
            //toolTip1.SetToolTip(this.ANYTHING, "");
            //toolTip1.SetToolTip(this.ANYTHING, "");
            //toolTip1.SetToolTip(this.ANYTHING, "");
            //toolTip1.SetToolTip(this.ANYTHING, "");
            
        }

        private void PanningBar_Scroll(object sender, EventArgs e)
        {
            lengthUpdate();
            int panPosition = PanningBar.Value;
            panPosition = Math.Abs(panPosition); //They're negative numbers, so this makes them positive (takes the absolute value)
            string output = "[E1 " + panPosition.ToString("X2") + "]";
            txtPanningDisplay.Text = output;
            Clipboard.SetText(output);
            new SoundPlayer(Properties.Resources.ExtraAudio_Text_Blip).Play();
        }

        private void ChannelVolumeBar_Scroll(object sender, EventArgs e)
        {
            lengthUpdate();
            //ChannelVolumeBar and txtChannelVolumeDisplay
            int volume = ChannelVolumeBar.Value;
            string output = "[ED " + volume.ToString("X2") + "]";
            txtChannelVolumeDisplay.Text = output;
            Clipboard.SetText(output);
            playTextTypeSound("huge");
        }

        private void btnCopySlidingPan_Click(object sender, EventArgs e)
        {
            new SoundPlayer(Properties.Resources.ExtraAudio_Equipped_).Play();
            lengthUpdate();
            string output = "[E2 "
                + noteLength.ToString("X2") + " "
                + Math.Abs(PanningBar.Value).ToString("X2")
                + "]";
            Clipboard.SetText(output);
        }

        private void btnCopySlidingVolume_Click(object sender, EventArgs e)
        {
            new SoundPlayer(Properties.Resources.ExtraAudio_Equipped_).Play();
            lengthUpdate();
            string output = "[EE "
                + noteLength.ToString("X2") + " "
                + Math.Abs(ChannelVolumeBar.Value).ToString("X2")
                + "]";
            Clipboard.SetText(output);
        }

        private void btnCopySlidingEcho_Click(object sender, EventArgs e)
        {
            new SoundPlayer(Properties.Resources.ExtraAudio_Equipped_).Play();
            lengthUpdate();
            string vol = Math.Abs(ChannelVolumeBar.Value).ToString("X2");
            string output = "[F8 "
                + noteLength.ToString("X2") + " "
                + vol + " "
                + vol
                + "]";
            Clipboard.SetText(output);
        }

        private void btnFinetune1_Click(object sender, EventArgs e)
        {
            new SoundPlayer(Properties.Resources.ExtraAudio_Equipped_).Play();
            Clipboard.SetText("[F4 00]");

            //TODO: Make an Excel document - Instrument description, Finetune value
        }

        private void btnTempo_Click(object sender, EventArgs e)
        {
            new SoundPlayer(Properties.Resources.ExtraAudio_Equipped_).Play();
            Clipboard.SetText("[E7 20]");
        }

        private void btnGlobalVolume_Click(object sender, EventArgs e)
        {
            new SoundPlayer(Properties.Resources.ExtraAudio_Equipped_).Play();
            Clipboard.SetText("[E5 F0]");
        }

        private void btnPortamentoUp_Click(object sender, EventArgs e)
        {
            //[F1 start length range]
            new SoundPlayer(Properties.Resources.ExtraAudio_Equipped_).Play();
            Clipboard.SetText("[F1 00 06 01]");
        }

        private void btnPortamentoDown_Click(object sender, EventArgs e)
        {
            //[F2 start length range]
            new SoundPlayer(Properties.Resources.ExtraAudio_Equipped_).Play();
            Clipboard.SetText("[F2 00 06 01]");
        }

        private void btnPortamentoOff_Click(object sender, EventArgs e)
        {
            new SoundPlayer(Properties.Resources.ExtraAudio_Equipped_).Play();
            Clipboard.SetText("[F3]");
        }

        private void btnVibrato_Click(object sender, EventArgs e)
        {
            new SoundPlayer(Properties.Resources.ExtraAudio_Equipped_).Play();
            Clipboard.SetText("[E3 0C 1C 32]");
        }

        private void btnVibratoOff_Click(object sender, EventArgs e)
        {
            new SoundPlayer(Properties.Resources.ExtraAudio_Equipped_).Play();
            Clipboard.SetText("[E4]");
        }

        private void btnChannelTranspose_Click(object sender, EventArgs e)
        {
            new SoundPlayer(Properties.Resources.ExtraAudio_Equipped_).Play();
            Clipboard.SetText("[EA 00]");
        }

        private void sendNote(byte input)
        { //Takes a byte, puts it in the label, and puts it in the clipboard
            lengthUpdate();
            String note = "[" + input.ToString("X2") + "]";
            DispLabel.Text = note;
            Clipboard.SetText(note);
        }

        private void sendNote(string input)
        { //An alternate version for manually setting the string
            lengthUpdate();
            DispLabel.Text = "[" + input + "]";
            Clipboard.SetText(input);
        }

        private void btnRest_Click(object sender, EventArgs e)
        { sendNote(0xC9); }

        private void btnContinue_Click(object sender, EventArgs e)
        { sendNote(0xC8); }

        private void btnC_Click(object sender, EventArgs e)
        {
            if (octave == 1)
            {
                sendNote(0x80);
                new SoundPlayer(Properties.Resources._1C).Play();
            }
            else if (octave == 2)
            {
                sendNote(0x8C);
                new SoundPlayer(Properties.Resources._2C).Play();
            }
            else if (octave == 3)
            {
                sendNote(0x98);
                new SoundPlayer(Properties.Resources._3C).Play();
            }
            else if (octave == 4)
            {
                sendNote(0xA4);
                new SoundPlayer(Properties.Resources._4C).Play();
            }
            else if (octave == 5)
            {
                sendNote(0xB0);
                new SoundPlayer(Properties.Resources._5C).Play();
            }
            else if (octave == 6)
            {
                sendNote(0xBC);
                new SoundPlayer(Properties.Resources._6C).Play();
            }
            else
            {
                sendNote("XX");
            }
        }

        private void btnCsharp_Click(object sender, EventArgs e)
        {
            if (octave == 1)
            {
                sendNote(0x81);
                new SoundPlayer(Properties.Resources._1Csharp).Play();
            }
            else if (octave == 2)
            {
                sendNote(0x8D);
                new SoundPlayer(Properties.Resources._2Csharp).Play();
            }
            else if (octave == 3)
            {
                sendNote(0x99);
                new SoundPlayer(Properties.Resources._3Csharp).Play();
            }
            else if (octave == 4)
            {
                sendNote(0xA5);
                new SoundPlayer(Properties.Resources._4Csharp).Play();
            }
            else if (octave == 5)
            {
                sendNote(0xB1);
                new SoundPlayer(Properties.Resources._5Csharp).Play();
            }
            else if (octave == 6)
            {
                sendNote(0xBD);
                new SoundPlayer(Properties.Resources._6Csharp).Play();
            }
            else
            {
                sendNote("XX");
            }
        }

        private void btnD_Click(object sender, EventArgs e)
        {
            if (octave == 1)
            {
                sendNote(0x82);
                new SoundPlayer(Properties.Resources._1D).Play();
            }
            else if (octave == 2)
            {
                sendNote(0x8E);
                new SoundPlayer(Properties.Resources._2D).Play();
            }
            else if (octave == 3)
            {
                sendNote(0x9A);
                new SoundPlayer(Properties.Resources._3D).Play();
            }
            else if (octave == 4)
            {
                sendNote(0xA6);
                new SoundPlayer(Properties.Resources._4D).Play();
            }
            else if (octave == 5)
            {
                sendNote(0xB2);
                new SoundPlayer(Properties.Resources._5D).Play();
            }
            else if (octave == 6)
            {
                sendNote(0xBE);
                new SoundPlayer(Properties.Resources._6D).Play();
            }
            else
            {
                sendNote("XX");
            }
        }

        private void btnDsharp_Click(object sender, EventArgs e)
        {
            if (octave == 1)
            {
                sendNote(0x83);
                new SoundPlayer(Properties.Resources._1Dsharp).Play();
            }
            else if (octave == 2)
            {
                sendNote(0x8F);
                new SoundPlayer(Properties.Resources._2Dsharp).Play();
            }
            else if (octave == 3)
            {
                sendNote(0x9B);
                new SoundPlayer(Properties.Resources._3Dsharp).Play();
            }
            else if (octave == 4)
            {
                sendNote(0xA7);
                new SoundPlayer(Properties.Resources._4Dsharp).Play();
            }
            else if (octave == 5)
            {
                sendNote(0xB3);
                new SoundPlayer(Properties.Resources._5Dsharp).Play();
            }
            else if (octave == 6)
            {
                sendNote(0xBF);
                new SoundPlayer(Properties.Resources._6Dsharp).Play();
            }
            else
            {
                sendNote("XX");
            }
        }

        private void btnE_Click(object sender, EventArgs e)
        {
            if (octave == 1)
            {
                sendNote(0x84);
                new SoundPlayer(Properties.Resources._1E).Play();
            }
            else if (octave == 2)
            {
                sendNote(0x90);
                new SoundPlayer(Properties.Resources._2E).Play();
            }
            else if (octave == 3)
            {
                sendNote(0x9C);
                new SoundPlayer(Properties.Resources._3E).Play();
            }
            else if (octave == 4)
            {
                sendNote(0xA8);
                new SoundPlayer(Properties.Resources._4E).Play();
            }
            else if (octave == 5)
            {
                sendNote(0xB4);
                new SoundPlayer(Properties.Resources._5E).Play();
            }
            else if (octave == 6)
            {
                sendNote(0xC0);
                new SoundPlayer(Properties.Resources._6E).Play();
            }
            else
            {
                sendNote("XX");
            }
        }

        private void btnF_Click(object sender, EventArgs e)
        {
            if (octave == 1)
            {
                sendNote(0x85);
                new SoundPlayer(Properties.Resources._1F).Play();
            }
            else if (octave == 2)
            {
                sendNote(0x91);
                new SoundPlayer(Properties.Resources._2F).Play();
            }
            else if (octave == 3)
            {
                sendNote(0x9D);
                new SoundPlayer(Properties.Resources._3F).Play();
            }
            else if (octave == 4)
            {
                sendNote(0xA9);
                new SoundPlayer(Properties.Resources._4F).Play();
            }
            else if (octave == 5)
            {
                sendNote(0xB5);
                new SoundPlayer(Properties.Resources._5F).Play();
            }
            else if (octave == 6)
            {
                sendNote(0xC1);
                new SoundPlayer(Properties.Resources._6F).Play();
            }
            else
            {
                sendNote("XX");
            }
        }

        private void btnFsharp_Click(object sender, EventArgs e)
        {
            if (octave == 1)
            {
                sendNote(0x86);
                new SoundPlayer(Properties.Resources._1Fsharp).Play();
            }
            else if (octave == 2)
            {
                sendNote(0x92);
                new SoundPlayer(Properties.Resources._2Fsharp).Play();
            }
            else if (octave == 3)
            {
                sendNote(0x9E);
                new SoundPlayer(Properties.Resources._3Fsharp).Play();
            }
            else if (octave == 4)
            {
                sendNote(0xAA);
                new SoundPlayer(Properties.Resources._4Fsharp).Play();
            }
            else if (octave == 5)
            {
                sendNote(0xB6);
                new SoundPlayer(Properties.Resources._5Fsharp).Play();
            }
            else if (octave == 6)
            {
                sendNote(0xC2);
                new SoundPlayer(Properties.Resources._6Fsharp).Play();
            }
            else
            {
                sendNote("XX");
            }
        }

        private void btnG_Click(object sender, EventArgs e)
        {
            if (octave == 1)
            {
                sendNote(0x87);
                new SoundPlayer(Properties.Resources._1G).Play();
            }
            else if (octave == 2)
            {
                sendNote(0x93);
                new SoundPlayer(Properties.Resources._2G).Play();
            }
            else if (octave == 3)
            {
                sendNote(0x9F);
                new SoundPlayer(Properties.Resources._3G).Play();
            }
            else if (octave == 4)
            {
                sendNote(0xAB);
                new SoundPlayer(Properties.Resources._4G).Play();
            }
            else if (octave == 5)
            {
                sendNote(0xB7);
                new SoundPlayer(Properties.Resources._5G).Play();
            }
            else if (octave == 6)
            {
                sendNote(0xC3);
                new SoundPlayer(Properties.Resources._6G).Play();
            }
            else
            {
                sendNote("XX");
            }
        }

        private void btnGsharp_Click(object sender, EventArgs e)
        {
            if (octave == 1)
            {
                sendNote(0x88);
                new SoundPlayer(Properties.Resources._1Gsharp).Play();
            }
            else if (octave == 2)
            {
                sendNote(0x94);
                new SoundPlayer(Properties.Resources._2Gsharp).Play();
            }
            else if (octave == 3)
            {
                sendNote(0xA0);
                new SoundPlayer(Properties.Resources._3Gsharp).Play();
            }
            else if (octave == 4)
            {
                sendNote(0xAC);
                new SoundPlayer(Properties.Resources._4Gsharp).Play();
            }
            else if (octave == 5)
            {
                sendNote(0xB8);
                new SoundPlayer(Properties.Resources._5Gsharp).Play();
            }
            else if (octave == 6)
            {
                sendNote(0xC4);
                new SoundPlayer(Properties.Resources._6Gsharp).Play();
            }
            else
            {
                sendNote("XX");
            }
        }

        private void btnA_Click(object sender, EventArgs e)
        {
            if (octave == 1)
            {
                sendNote(0x89);
                new SoundPlayer(Properties.Resources._1zA).Play();
            }
            else if (octave == 2)
            {
                sendNote(0x95);
                new SoundPlayer(Properties.Resources._2zA).Play();
            }
            else if (octave == 3)
            {
                sendNote(0xA1);
                new SoundPlayer(Properties.Resources._3zA).Play();
            }
            else if (octave == 4)
            {
                sendNote(0xAD);
                new SoundPlayer(Properties.Resources._4zA).Play();
            }
            else if (octave == 5)
            {
                sendNote(0xB9);
                new SoundPlayer(Properties.Resources._5zA).Play();
            }
            else if (octave == 6)
            {
                sendNote(0xC5);
                new SoundPlayer(Properties.Resources._6zA).Play();
            }
            else
            {
                sendNote("XX");
            }
        }

        private void btnAsharp_Click(object sender, EventArgs e)
        {
            if (octave == 1)
            {
                sendNote(0x8A);
                new SoundPlayer(Properties.Resources._1zAsharp).Play();
            }
            else if (octave == 2)
            {
                sendNote(0x96);
                new SoundPlayer(Properties.Resources._2zAsharp).Play();
            }
            else if (octave == 3)
            {
                sendNote(0xA2);
                new SoundPlayer(Properties.Resources._3zAsharp).Play();
            }
            else if (octave == 4)
            {
                sendNote(0xAE);
                new SoundPlayer(Properties.Resources._4zAsharp).Play();
            }
            else if (octave == 5)
            {
                sendNote(0xBA);
                new SoundPlayer(Properties.Resources._5zAsharp).Play();
            }
            else if (octave == 6)
            {
                sendNote(0xC6);
                new SoundPlayer(Properties.Resources._6zAsharp).Play();
            }
            else
            {
                sendNote("XX");
            }
        }

        private void btnB_Click(object sender, EventArgs e)
        {
            if (octave == 1)
            {
                sendNote(0x8B);
                new SoundPlayer(Properties.Resources._1zB).Play();
            }
            else if (octave == 2)
            {
                sendNote(0x97);
                new SoundPlayer(Properties.Resources._2zB).Play();
            }
            else if (octave == 3)
            {
                sendNote(0xA3);
                new SoundPlayer(Properties.Resources._3zB).Play();
            }
            else if (octave == 4)
            {
                sendNote(0xAF);
                new SoundPlayer(Properties.Resources._4zB).Play();
            }
            else if (octave == 5)
            {
                sendNote(0xBB);
                new SoundPlayer(Properties.Resources._5zB).Play();
            }
            else if (octave == 6)
            {
                sendNote(0xC7);
                new SoundPlayer(Properties.Resources._6zB).Play();
            }
            else
            {
                sendNote("XX");
            }
        }

        private void btnOctaveDown_Click(object sender, EventArgs e)
        {
            if (octave > 1)
            {
                octave--;
                OctaveLbl.Text = "Octave: " + octave.ToString();
                new SoundPlayer(Properties.Resources.ExtraAudio_LeftRight).Play();
            }
        }

        private void btnOctaveUp_Click(object sender, EventArgs e)
        {
            if (octave < 6)
            {
                octave++;
                OctaveLbl.Text = "Octave: " + octave.ToString();
                new SoundPlayer(Properties.Resources.ExtraAudio_LeftRight).Play();
            }
        }

        //Checkboxes all redirect to calculateEchoChannelCode()
        private void checkBox1_CheckedChanged(object sender, EventArgs e) { calculateEchoChannelCode(); }
        private void checkBox2_CheckedChanged(object sender, EventArgs e) { calculateEchoChannelCode(); }
        private void checkBox3_CheckedChanged(object sender, EventArgs e) { calculateEchoChannelCode(); }
        private void checkBox4_CheckedChanged(object sender, EventArgs e) { calculateEchoChannelCode(); }
        private void checkBox5_CheckedChanged(object sender, EventArgs e) { calculateEchoChannelCode(); }
        private void checkBox6_CheckedChanged(object sender, EventArgs e) { calculateEchoChannelCode(); }
        private void checkBox7_CheckedChanged(object sender, EventArgs e) { calculateEchoChannelCode(); }
        private void checkBox8_CheckedChanged(object sender, EventArgs e) { calculateEchoChannelCode(); }

        private void setAllEchoValues()
        {
            echoVolume = (byte)trackBarEchoVol.Value;
            echoDelay = (byte)trackBarEchoDelay.Value;
            echoFeedback = (byte)trackBarEchoFeedback.Value;
            echoFilter = (byte)trackBarEchoFilter.Value;
        }

        private void calculateEchoChannelCode()
        {
            //I keep getting 00s until I move one of the sliders, which is annoying. Hopefully this should fix it.
            setAllEchoValues();

            string scratchPaper = ""; //Build up the binary number bit by bit
            if (checkBox8.Checked) 
                scratchPaper = scratchPaper + "1";
            else 
                scratchPaper = scratchPaper + "0";

            if (checkBox7.Checked)
                scratchPaper = scratchPaper + "1";
            else
                scratchPaper = scratchPaper + "0";

            if (checkBox6.Checked)
                scratchPaper = scratchPaper + "1";
            else
                scratchPaper = scratchPaper + "0";

            if (checkBox5.Checked)
                scratchPaper = scratchPaper + "1";
            else
                scratchPaper = scratchPaper + "0";

            if (checkBox4.Checked)
                scratchPaper = scratchPaper + "1";
            else
                scratchPaper = scratchPaper + "0";

            if (checkBox3.Checked)
                scratchPaper = scratchPaper + "1";
            else
                scratchPaper = scratchPaper + "0";

            if (checkBox2.Checked)
                scratchPaper = scratchPaper + "1";
            else
                scratchPaper = scratchPaper + "0";

            if (checkBox1.Checked)
                scratchPaper = scratchPaper + "1";
            else
                scratchPaper = scratchPaper + "0";

            //convert scratchPaper to a real byte
            echoChannels = (byte)Convert.ToInt32(scratchPaper, 2);
            
            createEchoCodes();
            new SoundPlayer(Properties.Resources.ExtraAudio_The_A_Button).Play();
        }

        private void createEchoCodes()
        {
            //Updates txtEchoDisplay with:
            //[F5 XX YY YY] [F7 XX YY ZZ]
            //F5 echoChannels echoVolume echoVolume
            //F7 echoDelay echoFeedback echoFilter
            string output 
                = "[F5 "
                + echoChannels.ToString("X2") + " "
                + echoVolume.ToString("X2") + " "
                + echoVolume.ToString("X2") + "] "
                
                + "[F7 "
                + echoDelay.ToString("X2") + " "
                + echoFeedback.ToString("X2") + " "
                + echoFilter.ToString("X2") + "]";

            Clipboard.SetText(output);
            txtEchoDisplay.Text = output;
        }

        private void btnEchoOff_Click(object sender, EventArgs e)
        {
            new SoundPlayer(Properties.Resources.ExtraAudio_Equipped_).Play();
            Clipboard.SetText("[F6]");
        }

        private void trackBarEchoVol_Scroll(object sender, EventArgs e)
        {
            setAllEchoValues();
            createEchoCodes();
            playTextTypeSound("huge");
        }

        private void trackBarEchoDelay_Scroll(object sender, EventArgs e)
        {
            setAllEchoValues();
            createEchoCodes();
            playTextTypeSound("tiny");
        }

        private void trackBarEchoFeedback_Scroll(object sender, EventArgs e)
        {
            setAllEchoValues();
            createEchoCodes();
            playTextTypeSound("huge");
        }

        private void trackBarEchoFilter_Scroll(object sender, EventArgs e)
        {
            setAllEchoValues();
            createEchoCodes();
            playTextTypeSound("tiny");
        }

        private void btnPortamento_Click(object sender, EventArgs e)
        {
            new SoundPlayer(Properties.Resources.ExtraAudio_Equipped_).Play();
            Clipboard.SetText("C8 [F9 00 01 ");
        }

        private void btnSetFirstDrum_Click(object sender, EventArgs e)
        {
            new SoundPlayer(Properties.Resources.ExtraAudio_Equipped_).Play();
            Clipboard.SetText("[FA XX]");
        }

        
    }
}