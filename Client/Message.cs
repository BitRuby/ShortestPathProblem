using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace ShortestPathResolver
{
    [Serializable]
    class Message
    {
        public int[,] Mat { get; set; }
        public int Type { get; set; }
        public String Text { get; set; }
        public int RangeFrom { get; set; }
        public int RangeTo { get; set; }

        public Message(int[,] Mat = null, int Type = 0, String Text = null, int RangeFrom = 0, int RangeTo = 0)
        {
            this.Mat = Mat;
            this.Type = Type;
            this.Text = Text;
            this.RangeFrom = RangeFrom;
            this.RangeTo = RangeTo;
        }

        public static byte[] Serialize(object o)
        {
            IFormatter formatter = new BinaryFormatter();
            Stream stream = new MemoryStream();
            formatter.Serialize(stream, o);
            return ((MemoryStream)stream).ToArray();
        }

        public static object Deserialize(MemoryStream stream)
        {
            IFormatter formatter = new BinaryFormatter();
            stream.Seek(0, SeekOrigin.Begin);
            object o = formatter.Deserialize(stream);
            return o;
        }
    }
}
