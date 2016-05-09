using System;

// source: http://stackoverflow.com/questions/18180958/does-code-exist-for-shifting-list-elements-to-left-or-right-by-specified-amount

public static class ArrayExtensions
{
    public static void ShiftLeft<T>(this T[] arr, int shifts)
    {
        Array.Copy(arr, shifts, arr, 0, arr.Length - shifts);
        Array.Clear(arr, arr.Length - shifts, shifts);
    }

    public static void ShiftRight<T>(this T[] arr, int shifts)
    {
        Array.Copy(arr, 0, arr, shifts, arr.Length - shifts);
        Array.Clear(arr, 0, shifts);
    }
}
