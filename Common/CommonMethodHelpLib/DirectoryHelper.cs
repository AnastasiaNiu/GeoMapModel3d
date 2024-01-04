using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonMethodHelpLib
{
    public class DirectoryHelper
    {
        public void BuildDirectory(string path) 
        {
            // 检查路径是否存在
            if (Directory.Exists(path))
            {
                Console.WriteLine("Directory already exists.");
            }
            else
            {
                // 如果路径不存在，则创建路径
                Directory.CreateDirectory(path);
                Console.WriteLine("Directory created successfully.");
            }
        }
    }
}
