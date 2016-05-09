using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public static class GameObjectInterfaces
{
    public static T GetInterface<T>(this GameObject inObj) where T : class
    {
        if (!typeof(T).IsInterface)
        {
            Debug.LogError(typeof(T).ToString() + ": is not an actual interface!");
            return null;
        }

        return inObj.GetComponents<Component>().OfType<T>().FirstOrDefault();
    }

    public static IEnumerable<T> GetInterfaces<T>(this GameObject inObj) where T : class
    {
        if (!typeof(T).IsInterface)
        {
            Debug.LogError(typeof(T).ToString() + ": is not an actual interface!");
            return Enumerable.Empty<T>();
        }

        return inObj.GetComponents<Component>().OfType<T>();
    }
}