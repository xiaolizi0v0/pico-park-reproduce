using UnityEngine;

namespace PicoPark
{
    public static class NetMsg
    {
        public const byte Input = 1;
        public const byte Snapshot = 2;
        public const byte Level = 3;
        public const byte Hello = 4;
    }

    public struct InputSample
    {
        public int tick;
        public Btn buttons;
    }

    public class Snapshot
    {
        public int tick;
        public int levelIndex;
        public byte phase;
        public int playerCount;
        public float timeLeft;
        public byte lightPhase;
        public float lightTimer;
        public bool doorOpen;
        public Vector2 doorPos;
        public int actorCount;
        public Vector2[] pos;
        public Vector2[] vel;
        public byte[] flags;
        public Vector2[] solidPos;
        public byte[] solidFlags;
        public float[] solidTimers;
        public byte[] buttons;

        public void Ensure(int actors, int solids, int buttonsN)
        {
            actorCount = actors;
            if (pos == null || pos.Length < actors)
            {
                pos = new Vector2[actors];
                vel = new Vector2[actors];
                flags = new byte[actors];
            }
            if (solidPos == null || solidPos.Length < solids)
            {
                solidPos = new Vector2[solids];
                solidFlags = new byte[solids];
                solidTimers = new float[solids];
            }
            if (buttons == null || buttons.Length < buttonsN) buttons = new byte[buttonsN];
        }
    }

    /// <summary>Compact little-endian bit packer for the netcode.</summary>
    public class ByteWriter
    {
        public byte[] buf;
        public int len;

        public ByteWriter(int capacity)
        {
            buf = new byte[capacity];
            len = 0;
        }

        public void Reset() { len = 0; }

        public void U8(byte v)
        {
            if (len + 1 > buf.Length) return;
            buf[len++] = v;
        }

        public void I16(int v)
        {
            ushort u = (ushort)(v < 0 ? 0 : (v & 0xFFFF));
            U8((byte)(u & 0xFF));
            U8((byte)((u >> 8) & 0xFF));
        }

        public void I32(int v)
        {
            U8((byte)(v & 0xFF));
            U8((byte)((v >> 8) & 0xFF));
            U8((byte)((v >> 16) & 0xFF));
            U8((byte)((v >> 24) & 0xFF));
        }

        public void Fixed16(float v, float scale)
        {
            I16(Mathf.Clamp(Mathf.RoundToInt(v * scale), -32768, 32767));
        }

        public void Fixed8(float v, float scale)
        {
            int i = Mathf.Clamp(Mathf.RoundToInt(v * scale), -128, 127);
            U8((byte)(i & 0xFF));
        }

        public void F32(float v)
        {
            byte[] b = System.BitConverter.GetBytes(v);
            for (int i = 0; i < 4; i++) U8(b[i]);
        }
    }

    public class ByteReader
    {
        private byte[] buf;
        private int pos;

        public ByteReader(byte[] buffer, int length)
        {
            buf = buffer;
            pos = 0;
        }

        public byte U8() { return pos < buf.Length ? buf[pos++] : (byte)0; }

        public int I16()
        {
            int a = U8();
            int b = U8();
            int v = a | (b << 8);
            return v;
        }

        public int I32()
        {
            int a = U8();
            int b = U8();
            int c = U8();
            int d = U8();
            return a | (b << 8) | (c << 16) | (d << 24);
        }

        public float Fixed16(float scale) { return I16() / scale; }
        public float Fixed8(float scale) { return (sbyte)U8() / scale; }

        public float F32()
        {
            byte[] b = new byte[4];
            for (int i = 0; i < 4; i++) b[i] = U8();
            return System.BitConverter.ToSingle(b, 0);
        }
    }
}
