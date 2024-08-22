using System.Buffers.Binary;
using System.Text;

namespace UndertaleModLib.Util;

// Reimplemented based on DogScepter's implementation
public class FileBinaryReader : IBinaryReader
{
    private readonly byte[] buffer = new byte[16];

    private readonly Encoding encoding = new UTF8Encoding(false);
    public Stream Stream { get; set; }

    private readonly long _length;
    public long Length { get => _length; }

    public long Position
    {
        get => Stream.Position;
        set => Stream.Position = value;
    }

    public FileBinaryReader(Stream stream, Encoding encoding = null)
    {
        _length = stream.Length;
        Stream = stream;

        if (stream.Position != 0)
            stream.Seek(0, SeekOrigin.Begin);

        if (encoding is not null)
            this.encoding = encoding;
    }

    private ReadOnlySpan<byte> ReadToBuffer(int count)
    {
        Stream.ReadExactly(buffer, 0, count);
        return buffer;
    }

    public byte ReadByte()
    {
        return (byte)Stream.ReadByte();
    }

    public virtual bool ReadBoolean()
    {
        return ReadByte() != 0;
    }

    public string ReadChars(int count)
    {
        if (count > 1024)
        {
            byte[] buf = new byte[count];
            Stream.Read(buf, 0, count);

            return encoding.GetString(buf);
        }
        else
        {
            Span<byte> buf = stackalloc byte[count];
            Stream.Read(buf);

            return encoding.GetString(buf);
        }
    }

    public byte[] ReadBytes(int count)
    {
        byte[] val = new byte[count];
        Stream.Read(val, 0, count);
        return val;
    }

    public short ReadInt16()
    {
        return BinaryPrimitives.ReadInt16LittleEndian(ReadToBuffer(2));
    }

    public ushort ReadUInt16()
    {
        return BinaryPrimitives.ReadUInt16LittleEndian(ReadToBuffer(2));
    }

    public int ReadInt24()
    {
        ReadToBuffer(3);
        return buffer[0] | buffer[1] << 8 | (sbyte)buffer[2] << 16;
    }

    public uint ReadUInt24()
    {
        ReadToBuffer(3);
        return (uint)(buffer[0] | buffer[1] << 8 | buffer[2] << 16);
    }

    public int ReadInt32()
    {
        return BinaryPrimitives.ReadInt32LittleEndian(ReadToBuffer(4));
    }

    public uint ReadUInt32()
    {
        return BinaryPrimitives.ReadUInt32LittleEndian(ReadToBuffer(4));
    }

    public float ReadSingle()
    {
        return BitConverter.Int32BitsToSingle(BinaryPrimitives.ReadInt32LittleEndian(ReadToBuffer(4)));
    }

    public double ReadDouble()
    {
        return BinaryPrimitives.ReadDoubleLittleEndian(ReadToBuffer(8));
    }

    public long ReadInt64()
    {
        return BinaryPrimitives.ReadInt64LittleEndian(ReadToBuffer(8));
    }

    public ulong ReadUInt64()
    {
        return BinaryPrimitives.ReadUInt64LittleEndian(ReadToBuffer(8));
    }

    public string ReadGMString()
    {
        int length = BinaryPrimitives.ReadInt32LittleEndian(ReadToBuffer(4));

        string res;
        if (length > 1024)
        {
            byte[] buf = new byte[length];
            Stream.ReadExactly(buf, 0, length);
            res = encoding.GetString(buf);
        }
        else
        {
            Span<byte> buf = stackalloc byte[length];
            Stream.ReadExactly(buf);
            res = encoding.GetString(buf);
        }
        
        // assume null-terminator
        Position++;
        return res;
    }
    public void SkipGMString()
    {
        int length = BinaryPrimitives.ReadInt32LittleEndian(ReadToBuffer(4));
        Position += (uint)length + 1;
    }

    public void Dispose()
    {
        if (Stream?.CanRead == true)
        {
            Stream.Close();
            Stream.Dispose();
        }
    }
}