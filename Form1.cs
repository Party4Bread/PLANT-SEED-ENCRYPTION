using System;
using System.Windows.Forms;
using System.IO;
using System.Security.Cryptography;
using System.Linq;
using CryptCs;
using System.Threading;
using System.Drawing;
using static Subfunc.Sub;
using System.Text;
using System.Management;

namespace WindowsFormsApplication2
{
    public partial class Form1 : Form
    {
        Crypt crypt;
        bool canClose = true;
        const int MD5_SIZE = 16;
        Thread t;
        const string tempFileName = "_temp.$$$";
        volatile int Progress;
        volatile int Maximum;
        int first = 0;
        public Form1()
        {
            InitializeComponent();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            SaveFileDialog savedlg = new SaveFileDialog();
            savedlg.Filter = "Encrypted Files (*.pse)|*.pse|All Files (*.*)|*.*";
            if (savedlg.ShowDialog() == DialogResult.OK)
            {
                string encFileName = savedlg.FileName;
                string PW = Prompt.ShowDialog("INPUT PASSWORD", "PASSWORD", "");
                if(PW != Prompt.ShowDialog("INPUT PASSWORD AGAIN", "PASSWORD", "")){
                    MessageBox.Show("PASSWORD INCORACT");
                    return;
                }

                t = new Thread(() => encrypt(encFileName, PW));
                t.Start();
                waitForThread();
                if (checkBox1.Checked)
                {
                    delfile();
                }
                listBox1.Items.Clear();
                listBox2.Items.Clear();
            }
        }
        void delfile()
        {
            for (int i = 0; i < listBox2.Items.Count; i++)
            {
                File.Delete(listBox2.Items[i].ToString());
            }
        }
        public String getCPUID()
        {
            string cpuInfo = string.Empty;
            ManagementClass mc = new ManagementClass("win32_processor");
            ManagementObjectCollection moc = mc.GetInstances();

            foreach (ManagementObject mo in moc)
            {
                if (cpuInfo == "")
                {
                    //Get only the first CPU's ID
                    cpuInfo = mo.Properties["processorID"].Value.ToString();
                    break;
                }
            }
            return cpuInfo;
        }

        void waitForThread()
        {
            canClose = false;
            Bitmap bmp = Screenshot.TakeSnapshot(panel1);
            BitmapFilter.GaussianBlur(bmp, 1);
            PictureBox pb = new PictureBox();
            panel1.Controls.Add(pb);
            pb.Image = bmp;
            pb.Dock = DockStyle.Fill;
            pb.BringToFront();
            progressBar1.BringToFront();
            progressBar1.Visible = true;
            button1.Enabled = false;
            button2.Enabled = false;
            button3.Enabled = false;
            listBox1.Enabled = false;
            while (t.IsAlive)
            {
                Application.DoEvents();
                progressBar1.Maximum = Maximum;
                int value = Progress + Crypt.Progress;
                if (value > Maximum) value = Maximum;
                progressBar1.Value = value;
            }
            pb.Hide();
            button1.Enabled = true;
            button2.Enabled = true;
            button3.Enabled = true;
            progressBar1.Visible = false;
            canClose = true;
            File.Delete("_temp.$$$");
            MessageBox.Show("fin");
        }

        private void button1_Click(object sender, EventArgs e)
        {
            OpenFileDialog opendlg = new OpenFileDialog();
            if (opendlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                String b = opendlg.FileName;
                int c;
                bool canw = true;
                c = b.LastIndexOf("\\") + 1;
                b = b.Substring(c, b.Length - c);
                if (listBox2.Items.Count != 0)
                {
                    for (int i = 0; i < listBox1.Items.Count; i++)
                    {
                        if (listBox2.Items[i].ToString() == opendlg.FileName)
                        {
                            canw = false;
                        }
                    }
                }
                if (canw)
                {
                    listBox1.Items.Add(b);
                    listBox2.Items.Add(opendlg.FileName);
                    first = 1;
                }
                else
                {
                    MessageBox.Show("There is same file");
                }
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            OpenFileDialog opendlg = new OpenFileDialog();
            opendlg.Filter = "Encrypted Files (*.pse)|*.pse|Encrypted Image (*.jpg)|*.jpg";
            if (opendlg.ShowDialog() != DialogResult.OK)
            {
                return;
            }
            string filename = opendlg.FileName;
            FolderBrowserDialog openf = new FolderBrowserDialog();
            if (openf.ShowDialog() != DialogResult.OK)
            {
                return;
            }
            string path = openf.SelectedPath;
            FileStream inputFile = File.OpenRead(filename);
            string PW = Prompt.ShowDialog("INPUT PASSWORD", "PASSWORD", "");
            byte[] userPW_MD5 = GetByteMD5(PW);
            byte[] filePW_MD5 = new byte[MD5_SIZE];
            inputFile.Read(filePW_MD5, 0, MD5_SIZE);
            inputFile.Close();


            if (!userPW_MD5.SequenceEqual(filePW_MD5))
            {
                MessageBox.Show("INCORRECT PASSWORD");
                return;
            }
            else
            {
                MessageBox.Show("CORRECT PASSWORD");
            }
            t = new Thread(() => decryption(filename, userPW_MD5, PW, path));
            t.Start();
            waitForThread();
        }
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!canClose)
            {
                e.Cancel = true;
                MessageBox.Show("while program working you can't exit");
            }
        }
        private void encrypt(string encFileName, string PW)
        {

            File.Delete(tempFileName);
            FileStream inputFile;
            FileStream outputFile = File.Create(tempFileName);
            string fInfoBase64 = getFileInfoBase64();
            byte[] fInfoBase64Array = stringToByteArray(fInfoBase64);
            outputFile.Write(fInfoBase64Array, 0, fInfoBase64Array.Length);
            outputFile.WriteByte(Convert.ToByte('|'));
            FileInfo info = null;

            int readBytes;
            byte[] readBuffer = new byte[10000];
            Maximum = 0;
            for (int filec = 0; filec < listBox2.Items.Count; filec++)
            {
                info = new FileInfo(listBox2.Items[filec].ToString());
                Maximum += (int)info.Length;
            }
            Maximum *= 2;   // 파일 합치기, 암호화

            Progress = Crypt.Progress = 0;

            for (int filec = 0; filec < listBox2.Items.Count; filec++)
            {
                inputFile = File.OpenRead(listBox2.Items[filec].ToString());

                while (true)
                {
                    readBytes = inputFile.Read(readBuffer, 0, 10000);
                    if (readBytes <= 0) break;

                    outputFile.Write(readBuffer, 0, readBytes);

                    Progress = Progress + readBytes;
                    /*
					info = new FileInfo(listBox2.Items[filec].ToString());
					maximum = (int)info.Length;
					for (long i = 0; i < info.Length; i++)
					{
						int toWrite = inputFile.ReadByte();
						if (toWrite < 0) break;
						outputFile.WriteByte((byte)toWrite);
						progress = progress + 1;

					}
					*/
                }
                inputFile.Close();
                Thread.Sleep(10);
            }

            outputFile.Close();

            FileStream encFile = File.Create(encFileName);
            encFile.Write(GetByteMD5(PW), 0, MD5_SIZE);
            encFile.Close();
            RIPEMD160 myRIPEMD160 = RIPEMD160.Create();
            byte[] keyArray = GetByteMD5(PW);
            byte[] ivArray = myRIPEMD160.ComputeHash(stringToByteArray(PW));
            byte[] inputIV = new byte[MD5_SIZE];
            Array.Copy(ivArray, inputIV, MD5_SIZE);
            if (checkBox2.Checked)
            {
                inputIV = stringToByteArray(getCPUID());
            }
            crypt = new Crypt(keyArray, inputIV);
            crypt.encrypt(tempFileName, encFileName);

            File.Delete(tempFileName);

            Progress = Maximum;

            canClose = true;

            Progress = Crypt.Progress = 0;
        }
        private void endDecryption()
        {
            canClose = true;
            File.Delete(tempFileName);

            Progress = Crypt.Progress = 0;
        }
        private void decryption(string encFilename, byte[] userPW_MD5, string PW, string path)
        {
            try
            {
                RIPEMD160 myRIPEMD160 = RIPEMD160.Create();
                byte[] ivArray = myRIPEMD160.ComputeHash(stringToByteArray(PW));
                byte[] inputIV = new byte[MD5_SIZE];
                Array.Copy(ivArray, inputIV, MD5_SIZE);
                if (checkBox2.Checked)
                {
                    inputIV = stringToByteArray(getCPUID());
                }
                Crypt crypt = new Crypt(userPW_MD5, inputIV);

                FileInfo info = new FileInfo(encFilename);
                Maximum = (int)info.Length * 2;     // 복호화, 파일 나누기
                Progress = Crypt.Progress = 0;
                crypt.decrypt(encFilename, tempFileName, MD5_SIZE);

                int delimPos = getPostionFileChar(tempFileName, '|');
                if (delimPos == -1)
                {
                    MessageBox.Show("INCORRECT FILE FORMAT");
                    endDecryption();
                    return;
                }

                byte[] fInfoByte = new byte[delimPos];
                FileStream inputFile = File.OpenRead(tempFileName);
                inputFile.Read(fInfoByte, 0, delimPos);
                //			inputTempFile.Close();
                Progress += delimPos;

                string fInfoBase64 = ByteArrayToString(fInfoByte);
                string fileInfo = Base64Decoding(fInfoBase64);

                //	FileStream inputFile = File.OpenRead(Filename);
                //			using (FileStream fsSource = new FileStream(Filename, FileMode.Open, FileAccess.Read))
                //		{


                int filecount = int.Parse(fileInfo.Substring(0, fileInfo.IndexOf("\"")));
                string[] filename = new string[filecount];
                string[] checksum = new string[filecount];
                int[] filesize = new int[filecount];
                fileInfo = fileInfo.Remove(0, fileInfo.IndexOf("\"") + 1);
                int i = 0;
                for (i = 0; i < filecount; i++)
                {
                    filename[i] = fileInfo.Substring(0, fileInfo.IndexOf("\\"));
                    fileInfo = fileInfo.Remove(0, fileInfo.IndexOf("\\") + 1);
                    filesize[i] = int.Parse(fileInfo.Substring(0, fileInfo.IndexOf("\\")));
                    fileInfo = fileInfo.Remove(0, fileInfo.IndexOf("\\") + 1);
                    checksum[i] = fileInfo.Substring(0, fileInfo.IndexOf("\""));
                    fileInfo = fileInfo.Remove(0, fileInfo.IndexOf("\"") + 1);
                }

                if (inputFile.ReadByte() != Convert.ToByte('|'))
                {
                    inputFile.Close();
                    MessageBox.Show("FILE INFOMATION ERROR");
                    endDecryption();
                    return;
                }

                //				inputFile.Seek(getPostionFileChar(tempFileName, '|') + 1, SeekOrigin.Begin);
                for (i = 0; i < filecount; i++)
                {
                    int readBytes, readTryBytes, leftBytes;
                    byte[] readBuffer = new byte[10000];

                    FileStream outputFile = File.Create(path + "\\" + filename[i]);
                    leftBytes = filesize[i];

                    while (leftBytes > 0)
                    {
                        readTryBytes = (leftBytes > 10000) ? 10000 : leftBytes;
                        readBytes = inputFile.Read(readBuffer, 0, readTryBytes);
                        if (readBytes != readTryBytes)
                        {
                            inputFile.Close();
                            outputFile.Close();
                            MessageBox.Show("FILE SIZE ERROR");
                            endDecryption();
                            return;
                        }
                        outputFile.Write(readBuffer, 0, readBytes);

                        Progress += readBytes;
                        leftBytes -= readBytes;
                    }
                    outputFile.Close();
                    if (checksum[i] != GetfileMD5(path + "\\" + filename[i]))
                    {
                        inputFile.Close();
                        MessageBox.Show("FILE CRASHED");
                        endDecryption();
                        return;
                    }
                }
                inputFile.Close();

                Progress = Maximum;
                endDecryption();
            }
            catch (Exception e)
            {
                MessageBox.Show("There is some problem to decrypt \n maybe it using CPU ID Lock");
            }
        }
        private string getFileInfoBase64()
        {

            string fInfo = "";
            fInfo += listBox1.Items.Count;
            fInfo += "\"";
            for (int i = 0; i < listBox2.Items.Count; i++)
            {
                string path = listBox2.Items[i].ToString();
                FileInfo info = new FileInfo(path);
                fInfo += listBox1.Items[i].ToString();
                fInfo += "\\";
                fInfo += info.Length;
                fInfo += "\\";
                fInfo += GetfileMD5(path);
                fInfo += "\"";
            }
            return Base64Encoding(fInfo);
        }
        public int getPostionFileChar(string filename, char FindChar)
        {
            int retFileByte, retPos = -1;
            byte FileByte;
            byte FindByte = Convert.ToByte(FindChar);

            FileStream inputFile = File.OpenRead(filename);

            for (int i = 0; ; i++)
            {
                retFileByte = inputFile.ReadByte();
                if (retFileByte == -1) break;

                FileByte = (byte)retFileByte;
                if (FileByte == FindByte)
                {
                    retPos = i;
                    break;
                }
            }
            inputFile.Close();
            return retPos;
        }

        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void button4_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog openfolder = new FolderBrowserDialog();
            string[] filePaths = null;
            bool canwrite = true;
            if (openfolder.ShowDialog() == DialogResult.OK)
            {
                filePaths = Directory.GetFiles(openfolder.SelectedPath, "*.*", SearchOption.AllDirectories);
                int c = listBox2.Items.Count;
                if (first == 0)
                {
                    for (int j = 0; j < filePaths.Length; j++)
                    {
                        listBox1.Items.Add(filePaths[j].Substring(filePaths[j].LastIndexOf("\\") + 1, filePaths[j].Length - filePaths[j].LastIndexOf("\\") - 1));
                        listBox2.Items.Add(filePaths[j]);
                    }
                    first = 1;
                    return;
                }
                else
                {
                    for (int i = 0; i < c; i++)
                    {
                        for (int j = 0; j < filePaths.Length; j++)
                        {
                            if (listBox2.Items[i].ToString() == filePaths[j])
                            {
                                canwrite = false;
                            }
                            if(canwrite)
                            {
                                listBox1.Items.Add(filePaths[j].Substring(filePaths[j].LastIndexOf("\\") + 1, filePaths[j].Length - filePaths[j].LastIndexOf("\\") - 1));
                                listBox2.Items.Add(filePaths[j]);
                            }                            
                        }
                    }
                }
            }
        }
    }
}
