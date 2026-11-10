using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

public static class C8Eraser
{
    private const byte TieByte = 0xC8;
    private const byte RestByte = 0xC9;
    private const byte CallSubroutine = 0xEF;
    private const int MaxLength = 0x7F;

    private static readonly Dictionary<byte, int> EffectParams = new Dictionary<byte, int>()
    {
        [0xE0] = 1, //Effect parameter info!
        [0xE1] = 1,
        [0xE2] = 2,
        [0xE3] = 3,
        [0xE4] = 0,
        [0xE5] = 1,
        [0xE6] = 2,
        [0xE7] = 1,
        [0xE8] = 2,
        [0xE9] = 1,
        [0xEA] = 1,
        [0xEB] = 3,
        [0xEC] = 0,
        [0xED] = 1,
        [0xEE] = 2,
        [0xEF] = 3,
        [0xF0] = 1,
        [0xF1] = 3,
        [0xF2] = 3,
        [0xF3] = 0,
        [0xF4] = 1,
        [0xF5] = 3,
        [0xF6] = 0,
        [0xF7] = 3,
        [0xF8] = 3,
        [0xF9] = 3,
        [0xFA] = 1,
        [0xFB] = 2,
        [0xFC] = 0,
        [0xFD] = 0,
        [0xFE] = 0,
        [0xFF] = 0,
    };

    private enum EvKind { Note, Tie, Rest, Command }

    private sealed class Ev
    {
        public EvKind Kind;
        public byte[] Bytes = new byte[0]; // the note byte, or an effect command + its parameters
        public int Duration; // ticks (notes, ties, rests)
        public byte? Q; // 2nd byte of the length command in effect (duration/velocity)
    }

    public static string Optimize(string text)
    {
        return Emit(BuildEvents(ParseBytes(text)));
    }

    // PARSING

    private static List<byte> ParseBytes(string text)
    {
        var result = new List<byte>();
        var tokens = text.Split(new[] { ' ', '\t', '\r', '\n', '[', ']' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var t in tokens)
        {
            if (t.Length != 2 ||
                !byte.TryParse(t, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out var b))
                throw new FormatException($"\"{t}\" isn't a two-digit hex byte.");
            result.Add(b);
        }
        return result;
    }

    private static List<Ev> BuildEvents(List<byte> bytes)
    {
        var events = new List<Ev>();
        int length = 0;   // current note length; 0 = not set yet
        byte? q = null;   // current 2nd length byte; null = not set yet

        for (int i = 0; i < bytes.Count; i++)
        {
            byte b = bytes[i];

            if (b == 0x00 || b >= 0xE0)
            {
                // Effect command (or 00): copy through untouched, including its parameters.
                int paramCount = 0;
                if (b != 0x00 && !EffectParams.TryGetValue(b, out paramCount))
                    throw new FormatException(
                        $"Unknown effect command {b:X2}. Add its parameter count to EffectParams.");
                if (i + paramCount >= bytes.Count)
                    throw new FormatException($"Command {b:X2} is missing its parameter bytes.");

                events.Add(new Ev { Kind = EvKind.Command, Bytes = bytes.GetRange(i, paramCount + 1).ToArray() });
                i += paramCount;

                // A subroutine can change the length state, so nothing can be assumed after it.
                if (b == CallSubroutine) { length = 0; q = null; }
            }
            else if (b <= MaxLength)
            {
                // Note style
                length = b;
                if (i + 1 < bytes.Count && bytes[i + 1] >= 0x01 && bytes[i + 1] <= MaxLength)
                    q = bytes[++i];
            }
            else
            {
                // 80-C7 notes, C8 tie, C9 rest, CA-DF drum notes
                if (length == 0)
                    throw new FormatException("ERROR: A note appears before any note length!");

                var kind = b == TieByte ? EvKind.Tie : b == RestByte ? EvKind.Rest : EvKind.Note;
                var last = events.Count > 0 ? events[events.Count - 1] : null;

                bool merge =
                    // rest + rest
                    (kind == EvKind.Rest && last?.Kind == EvKind.Rest) ||
                    // tie after a rest is just more silence
                    // tie after a note/tie extends it, but only if it doesn't change duration/velocity mid-note
                    (kind == EvKind.Tie && last != null &&
                        (last.Kind == EvKind.Rest ||
                        ((last.Kind == EvKind.Note || last.Kind == EvKind.Tie) && last.Q == q)));

                if (merge)
                    last.Duration += length;
                else
                    events.Add(new Ev { Kind = kind, Bytes = new[] { b }, Duration = length, Q = q });
            }
        }

        // Rests don't care about note style, so let them adopt whatever the next
        // sounding event needs. That way one note length can cover both
        byte? nextQ = null;
        for (int i = events.Count - 1; i >= 0; i--)
        {
            if (events[i].Kind == EvKind.Note || events[i].Kind == EvKind.Tie) nextQ = events[i].Q;
            else if (events[i].Kind == EvKind.Rest) events[i].Q = nextQ;
        }

        return events;
    }

    // EMITTING
    private static string Emit(List<Ev> events)
    {
        var sb = new StringBuilder();
        int outLen = 0; // length state the *output* has established so far
        byte? outQ = null;

        void Append(string s)
        {
            if (sb.Length > 0) sb.Append(' ');
            sb.Append(s);
        }

        // Writes a length command only if the output's current state doesn't already match
        void SetLength(int len, byte? wantQ, bool qRequired)
        {
            bool lenChanged = outLen != len;
            bool qDiffers = wantQ.HasValue && wantQ != outQ;
            if (!lenChanged && !(qDiffers && qRequired)) return;

            Append(qDiffers ? $"[{len:X2} {wantQ.Value:X2}]" : $"[{len:X2}]");
            outLen = len;
            if (qDiffers) outQ = wantQ;
        }

        foreach (var ev in events)
        {
            if (ev.Kind == EvKind.Command)
            {
                Append("[" + string.Join(" ", ev.Bytes.Select(x => x.ToString("X2"))) + "]");
                if (ev.Bytes[0] == CallSubroutine) { outLen = 0; outQ = null; }
                continue;
            }

            byte first = ev.Kind == EvKind.Note ? ev.Bytes[0] : ev.Kind == EvKind.Tie ? TieByte : RestByte;
            byte cont = ev.Kind == EvKind.Rest ? RestByte : TieByte;
            bool isFirst = true;

            // Lengths max out at 7F, so very long notes/rests have to be chunked
            foreach (int chunk in Split(ev.Duration))
            {
                SetLength(chunk, ev.Q, qRequired: ev.Kind != EvKind.Rest);
                Append((isFirst ? first : cont).ToString("X2"));
                isFirst = false;
            }
        }

        return sb.ToString();
    }

    // Splits a duration into the fewest chunks of at most 7F, as evenly as possible
    // Like the "try two of these instead!" messages
    private static IEnumerable<int> Split(int total)
    {
        int parts = (total + MaxLength - 1) / MaxLength;
        int baseLen = total / parts;
        int extra = total % parts;
        for (int i = 0; i < parts; i++)
            yield return baseLen + (i < extra ? 1 : 0);
    }
}
