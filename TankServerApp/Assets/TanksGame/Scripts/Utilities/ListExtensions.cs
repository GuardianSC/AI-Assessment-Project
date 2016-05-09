using System.Collections.Generic;

public static class ListExtensions
{
    public static void ShiftLeft<T>(this List<T> lst, int shifts)
    {
        for (int i = shifts; i < lst.Count; i++)
        {
            lst[i - shifts] = lst[i];
        }

        for (int i = lst.Count - shifts; i < lst.Count; i++)
        {
            lst[i] = default(T);
        }
    }

    public static void ShiftRight<T>(this List<T> lst, int shifts)
    {
        for (int i = lst.Count - shifts - 1; i >= 0; i--)
        {
            lst[i + shifts] = lst[i];
        }

        for (int i = 0; i < shifts; i++)
        {
            lst[i] = default(T);
        }
    }
}