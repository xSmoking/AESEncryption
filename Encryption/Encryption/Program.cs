using System;
using System.Collections.Generic;
using System.IO;
using log4net;
using System.Security.Cryptography;
using System.Text;

namespace Encryption
{
    class Program
    {
        private static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private static System.Random rnd = new System.Random();
        enum LogType { Info = 0, Error, Warn, Debug, Fatal };

        private static byte[] GenerateRandomSalt()
        {
            byte[] data = new byte[32];
            using (RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider())
            {
                for (int i = 0; i < 10; i++)
                    rng.GetBytes(data);
            }
            return data;
        }

        private static void AES_Encrypt(string inputFile, string password, int blockSize = 128)
        {
            byte[] salt = GenerateRandomSalt();
            byte[] passwordBytes = System.Text.Encoding.UTF8.GetBytes(password);
            FileStream fsCrypt = new FileStream(inputFile + ".aes", FileMode.Create);
            
            RijndaelManaged AES = new RijndaelManaged();
            AES.KeySize = 256;
            AES.BlockSize = blockSize;
            AES.Padding = PaddingMode.PKCS7;
            
            var key = new Rfc2898DeriveBytes(passwordBytes, salt, 50000);
            AES.Key = key.GetBytes(AES.KeySize / 8);
            AES.IV = key.GetBytes(AES.BlockSize / 8);
            AES.Mode = CipherMode.CFB;
            
            fsCrypt.Write(salt, 0, salt.Length);

            CryptoStream cs = new CryptoStream(fsCrypt, AES.CreateEncryptor(), CryptoStreamMode.Write);
            FileStream fsIn = new FileStream(inputFile, FileMode.Open);
            
            byte[] buffer = new byte[1048576];
            int read;

            try
            {
                while ((read = fsIn.Read(buffer, 0, buffer.Length)) > 0)
                    cs.Write(buffer, 0, read);
                
                fsIn.Close();
            }
            catch (Exception ex)
            {
                ConsoleLog(LogType.Error, "Error: " + ex.Message);
            }
            finally
            {
                File.Delete(@inputFile);
                cs.Close();
                fsCrypt.Close();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("success");
                Console.ForegroundColor = ConsoleColor.White;
            }
        }

        static void AES_Decrypt(string inputFile, string password, int blockSize = 128)
        {
            byte[] passwordBytes = System.Text.Encoding.UTF8.GetBytes(password);
            byte[] salt = new byte[32];

            FileStream fsCrypt = new FileStream(inputFile, FileMode.Open);
            fsCrypt.Read(salt, 0, salt.Length);

            RijndaelManaged AES = new RijndaelManaged();
            AES.KeySize = 256;
            AES.BlockSize = blockSize;
            var key = new Rfc2898DeriveBytes(passwordBytes, salt, 50000);
            AES.Key = key.GetBytes(AES.KeySize / 8);
            AES.IV = key.GetBytes(AES.BlockSize / 8);
            AES.Padding = PaddingMode.PKCS7;
            AES.Mode = CipherMode.CFB;

            string fileExtension = Path.GetExtension(inputFile);
            string fileName = Path.GetFileNameWithoutExtension(inputFile);
            string dirName = Path.GetDirectoryName(inputFile);
            CryptoStream cs = new CryptoStream(fsCrypt, AES.CreateDecryptor(), CryptoStreamMode.Read);
            FileStream fsOut = new FileStream(dirName + "\\" + fileName, FileMode.Create);

            int read;
            byte[] buffer = new byte[1048576];

            try
            {
                while ((read = cs.Read(buffer, 0, buffer.Length)) > 0)
                    fsOut.Write(buffer, 0, read);
            }
            catch (System.Security.Cryptography.CryptographicException ex_CryptographicException)
            {
                ConsoleLog(LogType.Fatal, "CryptographicException error: " + ex_CryptographicException.Message);
            }
            catch (Exception ex)
            {
                ConsoleLog(LogType.Error, "Error: " + ex.Message);
            }

            try
            {
                cs.Close();
            }
            catch (Exception ex)
            {
                ConsoleLog(LogType.Error, "Error by closing CryptoStream: " + ex.Message);
            }
            finally
            {
                File.Delete(@dirName + "\\" + fileName + fileExtension);
                fsOut.Close();
                fsCrypt.Close();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("success");
                Console.ForegroundColor = ConsoleColor.White;
            }
        }

        static string GenerateKey(int bytes)
        {
            char[] letters = new char[] { 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'y', 'x', 'z' };
            char[] lettersUpper = new char[] { 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'Y', 'X', 'Z' };
            char[] numbers = new char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };
            char[] symbols = new char[] { '!', '@', '#', '$', '%', '&', '*', '=', ']', '[', '/', '+', '~' };
            string key = string.Empty;

            for (int i = 0; i < bytes; ++i)
            {
                int random = rnd.Next(4);
                if (random == 0)
                {
                    int slot = rnd.Next(26);
                    key += letters[slot];
                }
                else if (random == 1)
                {
                    int slot = rnd.Next(10);
                    key += numbers[slot];
                }
                else if (random == 2)
                {
                    int slot = rnd.Next(26);
                    key += lettersUpper[slot];
                }
                else
                {
                    int slot = rnd.Next(13);
                    key += symbols[slot];
                }
            }
            return key;
        }

        static void ConsoleLog(LogType logType, string message)
        {
            switch (logType)
            {
                case LogType.Info:
                    {
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Log.Info(message);
                    }
                    break;
                case LogType.Debug:
                    {
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Log.Debug(message);
                    }
                    break;
                case LogType.Error:
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Log.Error(message);
                    }
                    break;
                case LogType.Fatal:
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Log.Fatal(message);
                    }
                    break;
                case LogType.Warn:
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Log.Warn(message);
                    }
                    break;
            }
            Console.ForegroundColor = ConsoleColor.White;
        }

        static void Main(string[] args)
        {
            string username = System.Security.Principal.WindowsIdentity.GetCurrent().Name;
            Console.ForegroundColor = ConsoleColor.White;
            string key = string.Empty;
            int blockSize = 0;

            Console.WriteLine();
            Console.WriteLine("==================== AES Encryption ====================");
            Console.WriteLine("               AES File Encryptor v0.0.2");
            Console.WriteLine("               BlockSizes: 128, 192, 256");
            Console.WriteLine("                 Developed by xSmoking");
            Console.WriteLine("       BitCoin: 17s6D2prtrB4iT8TCSFZZVgfBQZNcB7PVV");
            Console.WriteLine("========================================================\n");
            Console.WriteLine("Enumeration:");
            Console.WriteLine("    --set\t\tSet the key or block size for encryption");
            Console.WriteLine("    --key128\t\tGenerate a secure hash of 16 bytes (128 Bit)");
            Console.WriteLine("    --key256\t\tGenerate a secure hash of 32 bytes (256 Bit)");
            Console.WriteLine("    --key512\t\tGenerate a secure hash of 64 bytes (512 Bit)");
            Console.WriteLine("    --block\t\tSet the block size (128, 192, 256)");
            Console.WriteLine("    -k, --key\t\tUse your own key");
            Console.WriteLine("    -e, --encrypt\tEncrypt a folder/file");
            Console.WriteLine("    -d, --decrypt\tDecrypt a folder/file");
            Console.WriteLine("    --exit\t\tExit the program\n");
            Console.WriteLine("Examples:");
            Console.WriteLine("    --set --key128");
            Console.WriteLine("    --set --block 256");
            Console.WriteLine("    --encrypt C:\\Users\\PDU\\Documents\\");
            Console.WriteLine("    -d C:\\Users\\PDU\\Documents\\\n");

            for (;;)
            {
                Console.Write(username + ">");
                string input = Console.ReadLine();

                if (input.Length > 0)
                {
                    List<string> commandList = new List<string>(input.Split(' '));

                    if (commandList[0] == "--set")
                    {
                        if (commandList.Count > 1)
                        {
                            bool valid = true;
                            if (commandList[1] == "--key128")
                                key = GenerateKey(16);
                            else if (commandList[1] == "--key256")
                                key = GenerateKey(32);
                            else if (commandList[1] == "--key512")
                                key = GenerateKey(64);
                            else if (commandList[1] == "-k" || commandList[1] == "--key")
                            {
                                if (commandList.Count > 2)
                                {
                                    if (commandList[2].Length > 0)
                                        key = commandList[2];
                                    else
                                    {
                                        valid = false;
                                        ConsoleLog(LogType.Error, "Key cannot be empty\n");
                                    }
                                }
                                else
                                {
                                    valid = false;
                                    ConsoleLog(LogType.Error, "Missing commands or parameters\n");
                                }
                            }
                            else if (commandList[1] == "--block")
                            {
                                valid = false;
                                if (commandList.Count > 2)
                                {
                                    if (commandList[2] == "128")
                                        blockSize = 128;
                                    else if (commandList[2] == "192")
                                        blockSize = 192;
                                    else if (commandList[2] == "256")
                                        blockSize = 256;
                                    else
                                        ConsoleLog(LogType.Error, "'" + commandList[2] + "' is not a valid block size\n");
                                }
                                else
                                    ConsoleLog(LogType.Error, "Missing commands or parameters\n");
                            }
                            else
                            {
                                valid = false;
                                ConsoleLog(LogType.Error, "'" + commandList[1] + "' is not a command\n");
                            }

                            if (valid)
                            {
                                ConsoleLog(LogType.Warn, "Save the key below to decrypt your files.");
                                ConsoleLog(LogType.Info, "Key: " + key + "\n");
                            }
                        }
                        else
                            ConsoleLog(LogType.Error, "Missing commands or parameters\n");
                    }
                    else if (commandList[0] == "-e" || commandList[0] == "--encrypt")
                    {
                        if (key.Length > 0)
                        {
                            if (commandList.Count > 1)
                            {
                                bool keepGoing = true;
                                if (blockSize == 0)
                                {
                                    Console.Write("BlockSize not set. Wanna proceed with the default (128 Bit)? [y/n]>");
                                    string option = Console.ReadLine();
                                    if (option == "Y" || option == "y")
                                        blockSize = 128;
                                    else
                                    {
                                        ConsoleLog(LogType.Info, "Operation aborted by user\n");
                                        keepGoing = false;
                                    }
                                }

                                if (keepGoing)
                                {
                                    if (Directory.Exists(commandList[1]))
                                    {
                                        foreach (string file in Directory.EnumerateFiles(commandList[1]))
                                        {
                                            Console.Write("Encrypting " + file + " - result: ");
                                            AES_Encrypt(file, key, blockSize);
                                        }
                                        Console.WriteLine();
                                    }
                                    else
                                        ConsoleLog(LogType.Error, "'" + commandList[1] + "' is not a valid path\n");
                                }
                            }
                            else
                                ConsoleLog(LogType.Error, "Missing commands or parameters\n");
                        }
                        else
                            ConsoleLog(LogType.Error, "You do not have any key assigned, set the key first\n");
                    }
                    else if (commandList[0] == "-d" || commandList[0] == "--decrypt")
                    {
                        if (key.Length > 0)
                        {
                            if (commandList.Count > 1)
                            {
                                bool keepGoing = true;
                                if (blockSize == 0)
                                {
                                    Console.Write("BlockSize not set. Wanna proceed with the default (128 Bit)? [y/n]>");
                                    string option = Console.ReadLine();
                                    if (option == "Y" || option == "y")
                                        blockSize = 128;
                                    else
                                    {
                                        ConsoleLog(LogType.Info, "Operation aborted by user\n");
                                        keepGoing = false;
                                    }
                                }

                                if (keepGoing)
                                {
                                    if (Directory.Exists(commandList[1]))
                                    {
                                        foreach (string file in Directory.EnumerateFiles(commandList[1], "*.aes"))
                                        {
                                            Console.Write("Decrypting " + file + " - result: ");
                                            AES_Decrypt(file, key);
                                        }
                                        Console.WriteLine();
                                    }
                                    else
                                        ConsoleLog(LogType.Error, "'" + commandList[1] + "' is not a valid path\n");
                                }
                            }
                            else
                                ConsoleLog(LogType.Error, "Missing commands or parameters\n");
                        }
                        else
                            ConsoleLog(LogType.Error, "You do not have any key assigned, set the key first\n");
                    }
                    else
                        ConsoleLog(LogType.Error, "'" + commandList[0] + "' is not a command\n");
                }
                else
                    ConsoleLog(LogType.Error, "No commands received\n");
            }
        }
    }
}
