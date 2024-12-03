using System.Net;
using System.Reflection;
using System.Text;

namespace StructMessagingNetwork
{
    // See ToByteConverters and FromByteConverter for supported struct member types.
    public class StructParser
    {
        private static readonly Dictionary<Type, Func<object, byte[]>> ToByteConverters = new Dictionary<Type, Func<object, byte[]>>
        {
            // Unmanaged types.
            { typeof(int), obj => BitConverter.GetBytes((int)obj) },
            { typeof(float), obj => BitConverter.GetBytes((float)obj) },
            { typeof(double), obj => BitConverter.GetBytes((double)obj) },
            { typeof(char), obj => BitConverter.GetBytes((char)obj) },

            // Managed types.
            {
                typeof(char[]), obj =>
                {
                    byte[] stringBytes = Encoding.UTF8.GetBytes((char[])obj);
                    byte[] lengthPrefix = BitConverter.GetBytes(stringBytes.Length);
                    byte[] result = new byte[lengthPrefix.Length + stringBytes.Length];
                    lengthPrefix.CopyTo(result, 0);
                    stringBytes.CopyTo(result, lengthPrefix.Length);
                    return result;
                }
            },
            { 
                typeof(string), obj =>
                {
                    byte[] stringBytes = Encoding.UTF8.GetBytes((string)obj);
                    byte[] lengthPrefix = BitConverter.GetBytes(stringBytes.Length);
                    byte[] result = new byte[lengthPrefix.Length + stringBytes.Length];
                    lengthPrefix.CopyTo(result, 0);
                    stringBytes.CopyTo(result, lengthPrefix.Length);
                    return result;
                }
            },
            {
                typeof(IPAddress), obj =>
                {
                    byte[] addressBytes = ((IPAddress)obj).GetAddressBytes();
                    byte[] lengthPrefix = BitConverter.GetBytes(addressBytes.Length);
                    byte[] result = new byte[lengthPrefix.Length + addressBytes.Length];
                    lengthPrefix.CopyTo(result, 0);
                    addressBytes.CopyTo(result, lengthPrefix.Length);
                    return result;
                }
            },
        };

        private static readonly Dictionary<Type, Func<byte[], int, (object, int)>> FromByteConverters = new Dictionary<Type, Func<byte[], int, (object, int)>>
        {
            // Unmanaged types.
            { typeof(int), (data, index) => (BitConverter.ToInt32(data, index), sizeof(int)) },
            { typeof(float), (data, index) => (BitConverter.ToSingle(data, index), sizeof(float)) },
            { typeof(double), (data, index) => (BitConverter.ToDouble(data, index), sizeof(double)) },
            { typeof(char), (data, index) => (BitConverter.ToChar(data, index), sizeof(char)) },

            // Managed types.
            { 
                typeof(char[]), (data, index) =>
                {
                    int length = BitConverter.ToInt32(data, index);
                    char[] result = Encoding.UTF8.GetChars(data, index + sizeof(int), length);
                    return (result, sizeof(int) + length);
                }
            },
            { 
                typeof(string), (data, index) =>
                {
                    int length = BitConverter.ToInt32(data, index);
                    string result = Encoding.UTF8.GetString(data, index + sizeof(int), length);
                    return (result, sizeof(int) + length);
                }
            },
            { 
                typeof(IPAddress), (data, index) =>
                {
                    int length = BitConverter.ToInt32(data, index);
                    byte[] addressBytes = new byte[length];
                    Array.Copy(data, index + sizeof(int), addressBytes, 0, length);
                    IPAddress address = new IPAddress(addressBytes);
                    return (address, sizeof(int) + length);
                }
            },
        };

        /// <summary>
        /// You may register custom parsers through this function.
        /// </summary>
        /// <typeparam name="T">The type of the parsed object.</typeparam>
        /// <param name="toByte">This parsing function should take the original object and turn it into an array of bytes.</param>
        /// <param name="fromByte">This parsing function should take the buffer and the current offset in the buffer as parameter, and return the reconstructed object as well as the new buffer offset.</param>
        /// <param name="overwrite">Set this to true if you wish to overwrite the existing parser for the type.</param>
        /// <returns>True if registration was successful, false if a parser already existed for the type. If you wish to overwrite parsers, set the optional parameter to True.</returns>
        public static bool RegisterCustomParser<T>(Func<object, byte[]> toByte, Func<byte[], int, (object, int)> fromByte, bool overwrite = false)
        {
            if (ToByteConverters.ContainsKey(typeof(T)))
            {
                if (overwrite)
                {
                    ToByteConverters.Remove(typeof(T));
                    FromByteConverters.Remove(typeof(T));
                }
                else
                {
                    return false;
                }
            }

            ToByteConverters.Add(typeof(T), toByte);
            FromByteConverters.Add(typeof(T), fromByte);
            return true;
        }

        public static bool TryEncode<T>(T value, out byte[] result) where T : struct
        {
            result = Array.Empty<byte>();
            bool success = false;
            
            try 
            {
                string? assemblyName = value.GetType().AssemblyQualifiedName;
                if (assemblyName == null)
                {
                    Console.WriteLine("[SMN error] Struct Parser: Struct '" + value.GetType() + " has an invalid AssemblyQualifiedName.");
                    return success;
                }

                List<byte> bytes = new List<byte>();

                // Adding assembly name to sent bytes.
                bytes.AddRange(ToByteConverters[typeof(string)].Invoke(assemblyName));

                foreach (var field in value.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance))
                {
                    if (ToByteConverters.TryGetValue(field.FieldType, out var converter))
                    {
#pragma warning disable CS8604 // Safe to ignore.
                        byte[] fieldBytes = converter(field.GetValue(value));
#pragma warning restore CS8604
                        bytes.AddRange(fieldBytes);
                    }
                    else
                    {
                        Console.WriteLine("[SMN warning] Struct Parser: Type '" + field.FieldType + "' is not supported for encoding.");
                    }
                }

                result = bytes.ToArray();
                success = true;
            }
            catch (Exception e)
            {
                Console.WriteLine("[SMN error] Struct Parser (TryEncode): " + e.Message);
            }

            return success;
        }

        public static bool TryDecode(byte[] data, out (Type? type, object? value) result)
        {
            bool success = false;
            result = (null, null);

            int index = 0;
            if (FromByteConverters.TryGetValue(typeof(string), out var converter))
            {
                try 
                {
                    var (assemblyNameObj, fieldSize) = converter(data, index);
                    string assemblyName = (string)assemblyNameObj;

                    Type? type = Type.GetType(assemblyName);

                    if (type == null)
                    {
                        Console.WriteLine("[SMN error] Struct Parser: Could not manage to find Type with specified name '" + assemblyName + "'.");
                        return false;
                    }

                    result.type = type;
                    index += fieldSize;

                    if (TryDecode(result.type, data, index, out object? value))
                    {
                        if (value != null)
                        {
                            result.value = value;
                            return true;
                        }
                    }
                }
                catch(Exception e)
                {
                    Console.WriteLine("[SMN error] Struct Parser (TryDecode): " + e.Message);
                    return false;
                }
            }

            

            return success;
        }

        private static bool TryDecode(Type type, byte[] data, int index, out object? result)
        {
            bool success = false;
            result = Activator.CreateInstance(type);

            foreach(var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                if (FromByteConverters.TryGetValue(field.FieldType, out var converter))
                {
                    try 
                    {
                        var (fieldValue, fieldSize) = converter(data, index);
                        field.SetValue(result, fieldValue);
                        index += fieldSize;
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("[SMN error] Struct Parser: " + e.Message);
                        return false;
                    }
                }
                else
                    Console.WriteLine("[SMN warning] Struct Parser: Type '" + field.GetType() + "' is not supported for decoding.");
            }

            success = true;
            return success;
        }
    }
}