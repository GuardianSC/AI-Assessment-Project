using System;
using System.Runtime.InteropServices;

public static class DataUtils
{
    public static int SizeOf<T>()
    {
        return Marshal.SizeOf(typeof(T));
    }
    public static int GetSize(this object obj)
    {
        return Marshal.SizeOf(obj);
    }

    public static byte[] GetBytes(object obj)
    {
        int size = Marshal.SizeOf(obj);
        IntPtr ptr = Marshal.AllocHGlobal(size);

        byte[] data = new byte[size];
        Marshal.StructureToPtr(obj, ptr, true);
        Marshal.Copy(ptr, data, 0, size);
        Marshal.FreeHGlobal(ptr);

        return data;
    }
    public static T FromBytes<T>(byte[] data)
    {
        T copy = default(T);

        int tSize = Marshal.SizeOf(copy);
        IntPtr ptr = Marshal.AllocHGlobal(tSize);

        Marshal.Copy(data, 0, ptr, tSize);
        copy = (T)Marshal.PtrToStructure(ptr, copy.GetType());
        Marshal.FreeHGlobal(ptr);

        return copy;
    }
}