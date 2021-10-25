using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace SWLOR.CLI
{
    public static class ChecksumUtil
    {
        public static string ChecksumFolder(string folderPath)
        {
            var files = Directory.GetFiles(folderPath, "*.*", SearchOption.AllDirectories)
                .OrderBy(p => p).ToList();

            var md5 = MD5.Create();

            if (files.Count == 0) md5.TransformFinalBlock(new byte[0], 0, 0);

            for (int i = 0; i < files.Count; i++)
            {
                string file = files[i];

                string relativePath = file.Substring(folderPath.Length + 1);
                byte[] pathBytes = Encoding.UTF8.GetBytes(relativePath.ToLower());
                md5.TransformBlock(pathBytes, 0, pathBytes.Length, pathBytes, 0);

                byte[] contentBytes = File.ReadAllBytes(file);
                if (i == files.Count - 1)
                    md5.TransformFinalBlock(contentBytes, 0, contentBytes.Length);
                else
                    md5.TransformBlock(contentBytes, 0, contentBytes.Length, contentBytes, 0);
            }

            return BitConverter.ToString(md5.Hash).Replace("-", "").ToLower();
        }

        public static string ReadChecksumFile(string filePath)
        {
            return File.ReadAllText(filePath);
        }

        public static void WriteChecksumFile(string filePath, string checksum)
        {
            File.WriteAllText(filePath, checksum);
        }
    }
}