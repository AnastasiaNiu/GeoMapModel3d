using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace CommonMethodHelpLib
{
    public static class JsonUtilityHelper
    {
        public static void ToJsonFile(object data, string jsonPath)
        {
            try
            {
                string str = JsonConvert.SerializeObject(data, Newtonsoft.Json.Formatting.Indented);
                File.WriteAllText(jsonPath, str);

            }
            catch (Exception ex)
            {
                throw new Exception($"写入文件出错：{ex.Message}");
            }
        }

        public static T FromJsonFile<T>(string jsonPath)
        {
            if (!File.Exists(jsonPath))
            {
                throw new Exception($"读取的文件不存在：{jsonPath}");
            }

            try
            {
                using (StreamReader reader = new StreamReader(jsonPath))
                {
                    JsonTextReader jsonReader = new JsonTextReader(reader);
                    JsonSerializer jsonSerializer = JsonSerializer.CreateDefault();
                    return jsonSerializer.Deserialize<T>(jsonReader);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"读取文件出错：{ex.Message}");
            }
        }

        public static string JsonEscape(string inputStr)
        {
            string outStr;
            StringBuilder stringBuilder = new StringBuilder();
            for (int i = 0; i < inputStr.Length; i++)
            {
                char c = inputStr[i];
                if (c == '"')
                {
                    stringBuilder.Append('\\');
                }

                stringBuilder.Append(c);

            }
            outStr = stringBuilder.ToString();
            return outStr;
        }

        /// <summary>
        /// 解决读取json文件中文乱码问题
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="jsonPath"></param>
        /// <returns></returns>
        public static T FromJsonFile1<T>(string jsonPath)
        {
            if (!File.Exists(jsonPath))
            {
                throw new Exception($"读取的文件不存在：{jsonPath}");
            }

            try
            {
                using (StreamReader reader = new StreamReader(jsonPath, Encoding.Default))
                {
                    JsonTextReader jsonReader = new JsonTextReader(reader);
                    JsonSerializer jsonSerializer = JsonSerializer.CreateDefault();
                    return jsonSerializer.Deserialize<T>(jsonReader);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"读取文件出错：{ex.Message}");
            }
        }

    }
}
