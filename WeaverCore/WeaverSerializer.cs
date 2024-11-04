#if UNITY_EDITOR
#endif

using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Reflection;
using System;
using UnityEngine;
using System.Text;
using static UnityEngine.Networking.UnityWebRequest;

namespace WeaverCore
{
    public static class WeaverSerializer
    {
        public class UnityObjectToGUIDConverter : JsonConverter<UnityEngine.Object>
        {
            public IDictionary<string, UnityEngine.Object> JsonTable;

            public UnityObjectToGUIDConverter(IDictionary<string, UnityEngine.Object> jsonTable)
            {
                JsonTable = jsonTable;
            }

            public override UnityEngine.Object ReadJson(JsonReader reader, Type objectType, UnityEngine.Object existingValue, bool hasExistingValue, JsonSerializer serializer)
            {
                var val = reader.Value;

                if (val is string id && JsonTable.TryGetValue(id, out var obj))
                {
                    return obj;
                }
                else
                {
                    return null;
                }
            }

            public override void WriteJson(JsonWriter writer, UnityEngine.Object obj, JsonSerializer serializer)
            {
                if (obj == null)
                {
                    writer.WriteNull();
                    return;
                }

                var id = obj.GetHashCode().ToString();

                if (!JsonTable.ContainsKey(id))
                {
                    JsonTable.Add(id, obj);
                }

                writer.WriteValue(id.ToString());
            }
        }

        //Used to ignore properties, and serialize only fields
        public class IgnorePropertiesResolver : DefaultContractResolver
        {
            protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
            {
                JsonProperty property = base.CreateProperty(member, memberSerialization);

                var nonSerializedDefined = member.IsDefined(typeof(NonSerializedAttribute));

                bool valid = false;

                if (member is FieldInfo field)
                {
                    valid = (field.IsPublic && !nonSerializedDefined) || (field.IsPrivate && !nonSerializedDefined && member.IsDefined(typeof(SerializeField)));
                }

                if (!valid)
                {
                    property.ShouldSerialize = _ => false;
                }
                return property;
            }
        }

        public static void Serialize(object obj, out string jsonOutput, out Dictionary<string, UnityEngine.Object> objectReferences)
        {
            var settings = new JsonSerializerSettings
            {
                ContractResolver = new IgnorePropertiesResolver()
            };


            objectReferences = new Dictionary<string, UnityEngine.Object>();
            settings.Converters.Add(new UnityObjectToGUIDConverter(objectReferences));

            jsonOutput = JsonConvert.SerializeObject(obj, settings);
        }

        public static void Serialize(object obj, out string jsonOutput, out System.Collections.Generic.List<UnityEngine.Object> objReferences)
        {
            Dictionary<string, UnityEngine.Object> objIDs;
            Serialize(obj, out var result, out objIDs);

            var finalResult = new StringBuilder();

            finalResult.Append("IDs: [");

            int index = 0;
            int count = objIDs.Count;

            objReferences = new System.Collections.Generic.List<UnityEngine.Object>();

            foreach (var pair in objIDs)
            {
                objReferences.Add(pair.Value);

                finalResult.Append(pair.Key);
                if (index < count - 1)
                {
                    finalResult.Append(", ");
                }
                index++;
            }

            finalResult.Append("], Data: ");
            finalResult.Append(result);

            jsonOutput = finalResult.ToString();
        }

        public static object Deserialize(Type objType, string json, IDictionary<string, UnityEngine.Object> objectReferences)
        {
            var settings = new JsonSerializerSettings
            {
                ContractResolver = new IgnorePropertiesResolver()
            };

            settings.Converters.Add(new UnityObjectToGUIDConverter(objectReferences));

            return JsonConvert.DeserializeObject(json, objType, settings);
        }

        public static T Deserialize<T>(string json, IDictionary<string, UnityEngine.Object> objectReferences) => (T)Deserialize(typeof(T), json, objectReferences);


        public static object Deserialize(Type objType, string data, System.Collections.Generic.List<UnityEngine.Object> objReferences)
        {
           // WeaverLog.Log("DESERIALIZING = " + data);

            Dictionary<string, UnityEngine.Object> objectReferenceTable = new Dictionary<string, UnityEngine.Object>();

            if (!data.StartsWith("IDs"))
            {
                WeaverLog.LogWarning("WARNING: Data doesn't contain an ID list. Object references will be null");
                WeaverLog.LogWarning(new System.Diagnostics.StackTrace());
            }
            else
            {
                int bracketStart = data.IndexOf('[');
                int bracketEnd = data.IndexOf(']');

                //WeaverLog.Log("Bracket Start = " + bracketStart);
                //WeaverLog.Log("Bracket End = " + bracketEnd);
                //StringBuilder currentReading = new StringBuilder();

                int referenceCounter = 0;

                int startIndex = bracketStart + 1;

                while (startIndex < bracketEnd)
                {
                    var nextComma = data.IndexOf(',', startIndex);

                    if (nextComma == -1)
                    {
                        nextComma = bracketEnd;
                    }

                    //WeaverLog.Log("Start Index = " + startIndex);
                    //WeaverLog.Log("Next Comma = " + nextComma);
                    //WeaverLog.Log("Length = " + (startIndex - nextComma));

                    //WeaverLog.Log("FOUND ID = " + data.Substring(startIndex, nextComma - startIndex));

                    UnityEngine.Object reference = null;

                    if (objReferences != null && referenceCounter < objReferences.Count)
                    {
                        reference = objReferences[referenceCounter];
                    }

                    objectReferenceTable.Add(data.Substring(startIndex, nextComma - startIndex).TrimEnd(']'), reference);

                    referenceCounter++;

                    startIndex = nextComma + 2;
                }
            }

            int dataStart = data.IndexOf("], Data: ");

            if (dataStart >= 0)
            {
                dataStart += "], Data: ".Length;
            }
            else
            {
                dataStart = 0;
            }

            if (dataStart > 0)
            {
                data = data.Substring(dataStart);
            }

            return Deserialize(objType, data, objectReferenceTable);
        }

        public static T Deserialize<T>(string data, System.Collections.Generic.List<UnityEngine.Object> objReferences) => (T)Deserialize(typeof(T), data, objReferences);
    }
}
