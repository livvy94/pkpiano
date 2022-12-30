using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace PK_Piano
{
    class ToneGenerator
    {
        //https://en.wikipedia.org/wiki/Piano_key_frequencies
        private static readonly double[] cFreqs = new double[]      { 16.35160, 32.70320, 65.40639, 130.8128, 261.6256, 523.2511, 1046.502, 2093.005, 4186.009 };
        private static readonly double[] cSharpFreqs = new double[] { 17.32391, 34.64783, 69.29566, 138.5913, 277.1826, 554.3653, 1108.731, 2217.461, 4434.922 };
        private static readonly double[] dFreqs = new double[]      { 18.35405, 36.70810, 73.41619, 146.8324, 293.6648, 587.3295, 1174.659, 2349.318, 4698.636 };
        private static readonly double[] dSharpFreqs = new double[] { 19.44544, 38.89087, 77.78175, 155.5635, 311.1270, 622.2540, 1244.508, 2489.016, 4978.032 };
        private static readonly double[] eFreqs = new double[]      { 20.60172, 41.20344, 82.40689, 164.8138, 329.6276, 659.2551, 1318.510, 2637.020, 5274.041 };
        private static readonly double[] fFreqs = new double[]      { 21.82676, 43.65353, 87.30706, 174.6141, 349.2282, 698.4565, 1396.913, 2793.826, 5587.652 };
        private static readonly double[] fSharpFreqs = new double[] { 23.12465, 46.24930, 92.49861, 184.9972, 369.9944, 739.9888, 1479.978, 2959.955, 5919.911 };
        private static readonly double[] gFreqs = new double[]      { 24.49971, 48.99943, 97.99886, 195.9977, 391.9954, 783.9909, 1567.982, 3135.963, 6271.927 };
        private static readonly double[] gSharpFreqs = new double[] { 25.95654, 51.91309, 103.8262, 207.6523, 415.3047, 830.6094, 1661.219, 3322.438, 6644.875 };
        private static readonly double[] aFreqs = new double[]      { 27.50000, 55.00000, 110.0000, 220.0000, 440.0000, 880.0000, 1760.000, 3520.000, 7040.000 };
        private static readonly double[] aSharpFreqs = new double[] { 29.13524, 58.27047, 116.5409, 233.0819, 466.1638, 932.3275, 1864.655, 3729.310, 7458.620 };
        private static readonly double[] bFreqs = new double[]      { 30.86771, 61.73541, 123.4708, 246.9417, 493.8833, 987.7666, 1975.533, 3951.066, 7902.133 };


        public static void PlayTone(PlaybackThing playbackThing, string note, int octave)
        {
            var Waveform = new SignalGenerator()
            {
                Gain = 0.3,
                Frequency = GetHz(note, octave),
                Type = playbackThing.type,
            };

            if (playbackThing.fadeout)
            {
                playbackThing.wo.Stop();
                var fade = new FadeInOutSampleProvider(Waveform, true);
                fade.BeginFadeOut(500);
                playbackThing.wo.Init(fade);
                playbackThing.wo.Play();
            }
            else
            {
                if (playbackThing.wo.PlaybackState == PlaybackState.Playing)
                    playbackThing.wo.Stop();
                else
                {
                    playbackThing.wo.Init(Waveform);
                    playbackThing.wo.Play();
                }
            }
        }

        private static double GetHz(string note, int octave)
        {
            var result = new double[0];

            if (note == "C")
                result = cFreqs;
            else if (note == "C#")
                result = cSharpFreqs;
            else if (note == "D")
                result = dFreqs;
            else if (note == "D#")
                result = dSharpFreqs;
            else if (note == "E")
                result = eFreqs;
            else if (note == "F")
                result = fFreqs;
            else if (note == "F#")
                result = fSharpFreqs;
            else if (note == "G")
                result = gFreqs;
            else if (note == "G#")
                result = gSharpFreqs;
            else if (note == "A")
                result = aFreqs;
            else if (note == "A#")
                result = aSharpFreqs;
            else if (note == "B")
                result = bFreqs;

            return result[octave]; //wow that's a lotta freqs
        }

        internal class PlaybackThing //this cuts down on the number of parameters passed into PlayTone
        {
            public WaveOutEvent wo;
            public SignalGeneratorType type;
            public bool fadeout;
        }
    }
}
