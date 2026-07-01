using System;
public class ByteArray
{
    //默认大小
    const int DEFAULT_SIZE = 1024;
    //初始大小
    int initSize = 0;
    //缓冲区
    public byte[] bytes;
    //读写位置
    public int readIndex = 0;
    public int writeIndex = 0;
    //容量
    private int capacity = 0;
    //剩余空间
    public int remain { get{ return capacity - writeIndex; } }
    //数据长度
    public int length {get { return writeIndex - readIndex; } }
    //构造函数
    public ByteArray(int size=DEFAULT_SIZE)
    {
        bytes = new byte[size];
        capacity = size;
        initSize = size;
        readIndex = 0;
        writeIndex = 0;
    }
    //构造函数
    public ByteArray(byte[] defaultBytes)
    {
        bytes = defaultBytes;
        capacity = defaultBytes.Length;
        initSize = defaultBytes.Length;
        readIndex = 0;
        writeIndex = defaultBytes.Length;
    }
    //重设尺寸
    public void ReSize(int size)
    {
        if (size < length) return;
        if (size < initSize) return;
        int n = 1;
        while (n < size) n *= 2;
        capacity = n;
        byte[] newBytes = new byte[capacity];
        Array.Copy(bytes, readIndex, newBytes, 0, writeIndex - readIndex);
        bytes = newBytes;
        writeIndex = length;
        readIndex = 0;
    }
    //检查并移动数据
    public void CheckAndMoveBytes()
    {
        if (length<8)
        {
            MoveBytes();
        }
        writeIndex = length;
        readIndex = 0;
    }
    //移动数据
    public void MoveBytes()
    {
        if (length > 0)
        {
            Array.Copy(bytes,readIndex,bytes,0,length);
        }
        writeIndex = length;
        readIndex = 0;
    }
    //写入数据
    public int Write(byte[] bs,int offset,int count)
    {
        if(remain<count) ReSize(length+count);
        Array.Copy(bs,offset,bytes,writeIndex,count);
        writeIndex += count;
        return count;
    }
    //读取数据
    public int Read(byte[] bs,int offset,int count)
    {
        count = Math.Min(count,length);
        Array.Copy(bytes, readIndex, bs, offset, count);
        readIndex+= count;
        CheckAndMoveBytes();
        return count;
    }
    //读取Int16
    public Int16 ReadInt16()
    {
        if (length < 2) return 0;
        Int16 ret = (Int16)((bytes[readIndex + 1] << 8) | bytes[readIndex]);
        readIndex += 2;
        CheckAndMoveBytes();
        return ret;
    }
    //读取Int32
    public Int32 ReadInt32()
    {
        if (length < 4) return 0;
        Int32 ret = (Int32)((bytes[readIndex + 3] << 24) | (bytes[readIndex + 2] << 16) | (bytes[readIndex+ 1] << 8) | (bytes[readIndex+ 0]));
        readIndex += 4;
        CheckAndMoveBytes();
        return ret;
    }
}
