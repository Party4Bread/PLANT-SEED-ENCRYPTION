using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace CryptCs
{
    public class Crypt
    {
        public const int BLOCK_SIZE = 16;
        public static int Progress = 0;

        private byte[] initialVector = new byte[BLOCK_SIZE];
        private byte[] securityKey = new byte[BLOCK_SIZE];
        private uint[] seedRoundKey = new uint[48];


        public Crypt(byte[] securityKey, byte[] initialVector)
        {
            this.securityKey = securityKey;
            this.initialVector = initialVector;

            SeedCs.SEED.SeedEncRoundKey(ref seedRoundKey, securityKey);
        }
        public int encrypt(string plainFile, string encFile)
        {
            long fileSize;
            long encFileSize = 0;
            int temp;
            bool hasPadded = false;
            int inputSize, padCh;
            byte[] buffer = new byte[BLOCK_SIZE];
            char[] charBuff = new char[BLOCK_SIZE];
            byte[] cipher = new byte[BLOCK_SIZE];

            FileStream inputFile;
            FileStream outputFile;

            inputFile = File.OpenRead(plainFile);
            outputFile = new FileStream(encFile, FileMode.Append);
            initialVector.CopyTo(cipher, 0);

            FileInfo info = new FileInfo(plainFile);
            fileSize = info.Length;
            temp = (int)fileSize % BLOCK_SIZE;
            fileSize += temp;

            while (true)
            {
                inputSize = inputFile.Read(buffer, 0, BLOCK_SIZE);
                if (inputSize == 0)
                {
                    break;
                }

                Progress += inputSize;

                if (inputSize < BLOCK_SIZE)

                {
                    hasPadded = true;
                    padCh = BLOCK_SIZE - inputSize;
                    padFromWithIn(inputSize, padCh, buffer);
                }
                cbcEncryptOneBlock(buffer, cipher);
                encFileSize += BLOCK_SIZE;
                outputFile.Write(buffer, 0, BLOCK_SIZE);
            }
            if (hasPadded == false)
            {
                padCh = BLOCK_SIZE;
                padFromWithIn(0, padCh, buffer);
                cbcEncryptOneBlock(buffer, cipher);
                outputFile.Write(buffer, 0, BLOCK_SIZE);
            }
            inputFile.Close();
            outputFile.Close();
            return 0;

        }



        private static void padFromWithIn(int from, int with, byte[] buffer)
        {
            for (int i = from; i < BLOCK_SIZE; i++)
            {
                buffer[i] = (byte)with;
            }
        }

        private void cbcEncryptOneBlock(byte[] buffer, byte[] cipher)
        {
            blockXOR(buffer, cipher);
            encryptOneBlock(buffer);
            buffer.CopyTo(cipher, 0);
        }

        private void encryptOneBlock(byte[] buffer)
        {
            SeedCs.SEED.SeedEncryptBlock(ref buffer, seedRoundKey);
        }

        public int decrypt(string encFile, string decFile, int offset = 0)
        {
            long fileSizeForProgress;
            long decFileSize = 0;
            bool lastBlock = false;
            int chPad;
            byte[] buffer = new byte[BLOCK_SIZE];
            byte[] decBuff = new byte[BLOCK_SIZE];
            char[] charBuff = new char[BLOCK_SIZE];
            byte[] cipher = new byte[BLOCK_SIZE];

            FileInfo info = new FileInfo(encFile);

            long fileSize = info.Length - offset;   // offset : another file info
            fileSizeForProgress = fileSize;
            if (fileSize % BLOCK_SIZE != 0) return 1;

            if (File.Exists(decFile))
            {
                File.Delete(decFile);
            }
            FileStream inputFile = File.OpenRead(encFile);
            FileStream outputFile = File.Create(decFile);

            inputFile.Seek(offset, SeekOrigin.Begin);

            initialVector.CopyTo(cipher, 0);
            while (true)
            {
                inputFile.Read(buffer, 0, BLOCK_SIZE);
                buffer.CopyTo(decBuff, 0);
                cbcDecryptOneBlock(buffer, decBuff, cipher);
                decFileSize += BLOCK_SIZE;
                Progress += BLOCK_SIZE;

                if (fileSize <= BLOCK_SIZE)
                {
                    chPad = decBuff[BLOCK_SIZE - 1];
                    lastBlock = true;
                    outputFile.Write(decBuff, 0, BLOCK_SIZE - chPad);
                }
                else
                {
                    outputFile.Write(decBuff, 0, BLOCK_SIZE);
                }
                if (lastBlock) break;
                fileSize -= BLOCK_SIZE;
            }

            inputFile.Close();
            outputFile.Close();

            return 0;
        }

        private void cbcDecryptOneBlock(byte[] buffer, byte[] decBuff, byte[] cipher)
        {
            DecryptOneBlock(decBuff);
            blockXOR(decBuff, cipher);
            buffer.CopyTo(cipher, 0);
        }

        private static void blockXOR(byte[] decBuff, byte[] cipher)
        {
            for (int i = 0; i < BLOCK_SIZE; i++)
            {
                decBuff[i] ^= cipher[i];
            }
        }

        private void DecryptOneBlock(byte[] decBuff)
        {
            SeedCs.SEED.SeedDecryptBlock(ref decBuff, seedRoundKey);
        }
    }
}
