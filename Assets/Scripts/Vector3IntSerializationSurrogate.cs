using UnityEngine;
using System.Runtime.Serialization;
using System.Collections;

public class Vector3IntSerializationSurrogate : ISerializationSurrogate
{
 
    // Method called to serialize a Vector3Int object
    public void GetObjectData(System.Object obj, SerializationInfo info, StreamingContext context)
    {
        Vector3Int vector3Int = (Vector3Int)obj;
        info.AddValue("x", vector3Int.x);
        info.AddValue("y", vector3Int.y);
        info.AddValue("z", vector3Int.z);
    }
 
    // Method called to deserialize a Vector3Int object
    public System.Object SetObjectData(System.Object obj, SerializationInfo info, StreamingContext context, ISurrogateSelector selector)
    {
        Vector3Int vector3Int = (Vector3Int)obj;
        vector3Int.x = (int)info.GetValue("x", typeof(int));
        vector3Int.y = (int)info.GetValue("y", typeof(int));
        vector3Int.z = (int)info.GetValue("z", typeof(int));
        obj = vector3Int;
        return obj;
    }
}