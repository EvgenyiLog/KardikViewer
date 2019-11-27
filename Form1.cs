using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Threading;


namespace KardikFilter
{
    public partial class Form1 : Form
    {
        string Vers = "version 2.7.16";
        byte file_ver = 2;

        FileStream raw_file;
        int prc;
        double[] bufferA, bufferB, bufferC;
        Thread myFileThread;
        Int64 position = 0;
        string mes = "";
        string savefilename;
        int Headoffs = 0;
        long tiks;
        Int64 cont_total;
        int FREQ = 0;
        DateTime fdate;

        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {

            OpenFileDialog openFileDialog1 = new OpenFileDialog
            {
               // InitialDirectory = @"D:\",
                Title = "Выбор файла записанной кардиограммы",

                CheckFileExists = true,
                CheckPathExists = true,

                DefaultExt = "krw",
                Filter = "RAW DATA(*.krw)|*.krw|All files(*.*)|*.*",
                FilterIndex = 1,
                RestoreDirectory = true,

                ReadOnlyChecked = true,
                ShowReadOnly = true
            };


            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                Int64 flen = 0;
                UInt64 cont_total = 0;

                string filename = openFileDialog1.FileName;

                saveFileDialog1.Filter = "KARDI DATA(*.kdg)|*.kdg|All files(*.*)|*.*";
                if (saveFileDialog1.ShowDialog() == DialogResult.Cancel)
                    return;
                // получаем выбранный файл
                savefilename = saveFileDialog1.FileName;

                listBox1.Items.Clear();

                raw_file = new FileStream(filename, FileMode.Open);
                flen = raw_file.Length;

                raw_file.Close();

                listBox1.Items.Add("Открываем файл " + filename);
                listBox1.Items.Add("Размер файла " + flen.ToString() + " байт");

                BinaryReader Head_reader = new BinaryReader(File.Open(filename, FileMode.Open));
                string sHead;
                byte fw;
                
                sHead=Head_reader.ReadString();
                if (sHead == "KRW")
                {
                    listBox1.Items.Add("Обнаружен заголовок *.krw");
                    fw = Head_reader.ReadByte();
                    listBox1.Items.Add("Версия структуры файла "+fw.ToString());
                    tiks = Head_reader.ReadInt64();
                    fdate = DateTime.FromBinary(tiks);
                    listBox1.Items.Add("Дата записи " + fdate.ToLongDateString()+ "  "+fdate.ToLongTimeString());
                    FREQ = Head_reader.ReadInt32();
                    listBox1.Items.Add("Частота дескретизации " + FREQ.ToString() + " Гц");
                    Headoffs = sHead.Length + sizeof(byte) + sizeof(long) + sizeof(int)+1;
                }
                else
                {
                    listBox1.Items.Add("Заголовок *.krw не обнаружен");
                    Headoffs = 0;
                    FREQ = 12700;
                    fdate = File.GetCreationTime(filename);
                    tiks = fdate.ToBinary();
                    listBox1.Items.Add("Дата записи " + fdate.ToLongDateString() + "  " + fdate.ToLongTimeString());
                    listBox1.Items.Add("Частота дескретизации " + FREQ.ToString() + " Гц");
                }

                Head_reader.Close();

                cont_total = (UInt64)((flen- Headoffs) / 6);

                listBox1.Items.Add("Обнаружено отсчетов " + cont_total.ToString());

                myFileThread = new Thread(new ParameterizedThreadStart(FileRead));
                myFileThread.IsBackground = true;
                myFileThread.Start(filename); // запускаем поток
                listBox1.Items.Add("Идет обработка...");
                progressBar1.Visible = true;
                timer1.Enabled = true;
                button1.Enabled = false;
                checkBox1.Enabled = false;
                checkBox2.Enabled = false;
                checkBox3.Enabled = false;
                ZETlab.Enabled = false;

            }

        }


        public void FileRead(object x)
      
        {
            Int64 flen = 0;
            string filename = (string)x;
            string name;
            int i;
            BinaryWriter Writerraw;
            StreamWriter f;

            cont_total = 0;


            try
            {
                raw_file = new FileStream(filename, FileMode.Open);

                for (i = 0; i < Headoffs; i++) raw_file.ReadByte();

                flen = raw_file.Length;
                cont_total = (Int64)((flen-Headoffs) / 6);
               // cont = (Int64)(12700 * numericUpDown1.Value);//12700

                bufferA = new double[cont_total];
                bufferB = new double[cont_total];
                bufferC = new double[cont_total];
                raw_file.Close();

                position = 0;

                using (BinaryReader reader = new BinaryReader(File.Open(filename, FileMode.Open)))
                {
                    prc = 0;

                    using (BinaryWriter Creator = new BinaryWriter(File.Open("LINEA_LOW.dat", FileMode.Create)))
                    {
                        Creator.Close();
                    }

                    using (BinaryWriter Creator = new BinaryWriter(File.Open("LINEB_LOW.dat", FileMode.Create)))
                    {
                        Creator.Close();
                    }

                    using (BinaryWriter Creator = new BinaryWriter(File.Open("LINEC_LOW.dat", FileMode.Create)))
                    {
                        Creator.Close();
                    }

                    using (BinaryWriter Creator = new BinaryWriter(File.Open("LINEA_HI.dat", FileMode.Create)))
                    {
                        Creator.Close();
                    }

                    using (BinaryWriter Creator = new BinaryWriter(File.Open("LINEB_HI.dat", FileMode.Create)))
                    {
                        Creator.Close();
                    }

                    using (BinaryWriter Creator = new BinaryWriter(File.Open("LINEC_HI.dat", FileMode.Create)))
                    {
                        Creator.Close();
                    }

                    for (i = 0; i < Headoffs; i++) reader.ReadByte();

                    while (position  < cont_total)
                    {
                            bufferA[position] = (double)reader.ReadInt16();
                            bufferB[position] = (double)reader.ReadInt16();
                            bufferC[position] = (double)reader.ReadInt16();
                            position++;

                        prc = (int)((float)position / (float)cont_total * 20.0);
                     }

                    /******************************/
                    if (ZETlab.Checked)
                    {
                            name = savefilename.Remove(savefilename.Length - 4, 4);
                        if (checkBox1.Checked)
                        {
                            mes = "Создаем файл " + name + "_RAW_I.ana";
                            Thread.Sleep(130);
                            Writerraw = new BinaryWriter(File.Open(name + "_RAW_I.ana", FileMode.Create));
                            for (i=0;i<cont_total;i++) Writerraw.Write((Single)bufferA[i]);
                           Writerraw.Close();

                            mes = "Создаем файл " + name + "_RAW_I.anp";
                            Thread.Sleep(130);
                            f = new StreamWriter(name + "_RAW_I.anp");
                            f.WriteLine(name + "_RAW_I");
                            f.WriteLine("FRQ " + FREQ.ToString());
                            f.WriteLine("FORMAT f2");
                            f.WriteLine("START  " + fdate.ToLongTimeString());
                            f.WriteLine("DATE " + fdate.ToShortDateString());
                            f.Close();
                        }

                        if (checkBox2.Checked)
                        {
                            mes = "Создаем файл " + name + "_RAW_II.ana";
                            Thread.Sleep(130);
                            Writerraw = new BinaryWriter(File.Open(name + "_RAW_II.ana", FileMode.Create));
                            for (i = 0; i < cont_total; i++) Writerraw.Write((Single)bufferB[i]);
                            Writerraw.Close();

                            mes = "Создаем файл " + name + "_RAW_II.anp";
                            Thread.Sleep(130);
                            f = new StreamWriter(name + "_RAW_II.anp");
                            f.WriteLine(name + "_RAW_II");
                            f.WriteLine("FRQ " + FREQ.ToString());
                            f.WriteLine("FORMAT f2");
                            f.WriteLine("START  " + fdate.ToLongTimeString());
                            f.WriteLine("DATE " + fdate.ToShortDateString());
                            f.Close();
                        }

                        if (checkBox3.Checked)
                        {
                            mes = "Создаем файл " + name + "_RAW_III.ana";
                            Thread.Sleep(130);
                            Writerraw = new BinaryWriter(File.Open(name + "_RAW_III.ana", FileMode.Create));
                            for (i = 0; i < cont_total; i++) Writerraw.Write((Single)bufferC[i]);
                            Writerraw.Close();

                            mes = "Создаем файл " + name + "_RAW_III.anp";
                            Thread.Sleep(130);
                            f = new StreamWriter(name + "_RAW_III.anp");
                            f.WriteLine(name + "_RAW_III");
                            f.WriteLine("FRQ " + FREQ.ToString());
                            f.WriteLine("FORMAT f2");
                            f.WriteLine("START  " + fdate.ToLongTimeString());
                            f.WriteLine("DATE " + fdate.ToShortDateString());
                            f.Close();
                        }

                    }

                    /******************************/

                    Thread myFilterThread = new Thread(new ThreadStart(LineAF));
                        myFilterThread.IsBackground = true;
                        if (checkBox1.Checked)
                            myFilterThread.Start(); // запускаем поток

                        Thread myFilterThread1 = new Thread(new ThreadStart(LineBF));
                        myFilterThread1.IsBackground = true;
                        if (checkBox2.Checked)
                            myFilterThread1.Start(); // запускаем поток

                        Thread myFilterThread2 = new Thread(new ThreadStart(LineCF));
                        myFilterThread2.IsBackground = true;
                        if (checkBox3.Checked)
                            myFilterThread2.Start(); // запускаем поток


                        while ((myFilterThread.IsAlive)|| (myFilterThread1.IsAlive) || (myFilterThread2.IsAlive))
                        {
                            Thread.Sleep(100);
                        }

                    GC.Collect();
                    GC.WaitForPendingFinalizers();

                   
                    mes = "Принято к обработке отсчетов " + position.ToString();
                    Thread.Sleep(130);
                    mes = "Собираем результаты обработки в файл...";
                    BinaryReader readerAL = new BinaryReader(File.Open("LINEA_LOW.dat", FileMode.Open));
                    BinaryReader readerBL = new BinaryReader(File.Open("LINEB_LOW.dat", FileMode.Open));
                    BinaryReader readerCL = new BinaryReader(File.Open("LINEC_LOW.dat", FileMode.Open));
                    BinaryReader readerAH = new BinaryReader(File.Open("LINEA_HI.dat", FileMode.Open));
                    BinaryReader readerBH = new BinaryReader(File.Open("LINEB_HI.dat", FileMode.Open));
                    BinaryReader readerCH = new BinaryReader(File.Open("LINEC_HI.dat", FileMode.Open));

                    mes = "Создаем файл " + savefilename;
                    BinaryWriter Writer = new BinaryWriter(File.Open(savefilename, FileMode.Create));
                    Writer.Write("KDG");
                    Writer.Write(file_ver);
                    Writer.Write(tiks);
                    Writer.Write(FREQ);
                   
                    while (position>0)
                    {
                        if (checkBox1.Checked)
                            Writer.Write((double)readerAL.ReadDouble());
                        else
                            Writer.Write((double)0);

                        if (checkBox2.Checked)
                            Writer.Write((double)readerBL.ReadDouble());
                        else
                            Writer.Write((double)0);

                        if (checkBox3.Checked)
                            Writer.Write((double)readerCL.ReadDouble());
                        else
                            Writer.Write((double)0);


                        if (checkBox1.Checked)
                            Writer.Write((double)readerAH.ReadDouble());
                        else
                            Writer.Write((double)0);

                        if (checkBox2.Checked)
                            Writer.Write((double)readerBH.ReadDouble());
                        else
                            Writer.Write((double)0);

                        if (checkBox3.Checked)
                            Writer.Write((double)readerCH.ReadDouble());
                        else
                            Writer.Write((double)0);

 
                        position--;
                    }
                    Writer.Close();
                    readerAL.Close();
                    readerBL.Close();
                    readerCL.Close();
                    readerAH.Close();
                    readerBH.Close();
                    readerCH.Close();

                    Thread.Sleep(100);
                    if (ZETlab.Checked == false)
                    {
                        mes = "Удаляем временные файлы...";

                        System.IO.File.Delete(@"LINEA_LOW.dat");
                        System.IO.File.Delete(@"LINEB_LOW.dat");
                        System.IO.File.Delete(@"LINEC_LOW.dat");
                        System.IO.File.Delete(@"LINEA_HI.dat");
                        System.IO.File.Delete(@"LINEB_HI.dat");
                        System.IO.File.Delete(@"LINEC_HI.dat");
                    }
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка: " + ex.Message);
               // listBox1.Items.Add("При открытии файла " + filename + " произошла ошибка! ");
            }
            prc = 100;
            GC.Collect();
            GC.WaitForPendingFinalizers();

        }

        public void LineAF()
        {
            
            int i;

            UInt64 MaxPoint = (UInt64)bufferA.Length;

            alglib.complex[] fft_data = new alglib.complex[MaxPoint];
            alglib.complex[] fft_dataCopy = new alglib.complex[MaxPoint];

            alglib.fft.fftr1d(bufferA, (int)MaxPoint, ref fft_data, null);

            prc += 8;
            double[] RDA = new double[MaxPoint];

            //***********************
            //           fft[] fft_dataCopy = new fft[MaxPoint];
            for (i = 0; i < (int)MaxPoint; i++)
            {
                fft_dataCopy[i].x = fft_data[i].x;
                fft_dataCopy[i].y = fft_data[i].y;
            }
            //***********************

            filter(ref fft_data, ref fft_dataCopy);

            alglib.fft.fftr1dinv(fft_data, (int)MaxPoint, ref RDA, null);

            prc += 8;

            using (BinaryWriter Writer = new BinaryWriter(File.Open("LINEA_LOW.dat", FileMode.Append)))
            {
                for (i = 0; i < RDA.Length; i++)
                    Writer.Write(RDA[i]);
               
                Writer.Close();

            }

            alglib.fft.fftr1dinv(fft_dataCopy, (int)MaxPoint, ref RDA, null);

            using (BinaryWriter Writer = new BinaryWriter(File.Open("LINEA_HI.dat", FileMode.Append)))
            {
                for (i = 0; i < RDA.Length; i++)
                    Writer.Write(RDA[i]);
                
                Writer.Close();
            }
            prc += 8;
        }

 
         public void LineBF()
        {

            int i;

            UInt64 MaxPoint = (UInt64)bufferB.Length;

            alglib.complex[] fft_data = new alglib.complex[MaxPoint];
            alglib.complex[] fft_dataCopy = new alglib.complex[MaxPoint];

            alglib.fft.fftr1d(bufferB, (int)MaxPoint, ref fft_data, null);
            prc += 8;

            double[] RDA = new double[MaxPoint];

            //***********************
            //           fft[] fft_dataCopy = new fft[MaxPoint];
            for (i = 0; i < (int)MaxPoint; i++)
            {
                fft_dataCopy[i].x = fft_data[i].x;
                fft_dataCopy[i].y = fft_data[i].y;
            }
            //***********************

            filter(ref fft_data, ref fft_dataCopy);

            alglib.fft.fftr1dinv(fft_data, (int)MaxPoint, ref RDA, null);

            using (BinaryWriter Writer = new BinaryWriter(File.Open("LINEB_LOW.dat", FileMode.Append)))
            {
                for (i = 0; i < RDA.Length; i++)
                    Writer.Write(RDA[i]);

                Writer.Close();

            }

            prc += 8;

            alglib.fft.fftr1dinv(fft_dataCopy, (int)MaxPoint, ref RDA, null);

            using (BinaryWriter Writer = new BinaryWriter(File.Open("LINEB_HI.dat", FileMode.Append)))
            {
                for (i = 0; i < RDA.Length; i++)
                    Writer.Write(RDA[i]);
                Writer.Close();
            }

            prc += 8;
        }


        public void LineCF()
        {

            int i;

            UInt64 MaxPoint = (UInt64)bufferC.Length;

            alglib.complex[] fft_data = new alglib.complex[MaxPoint];
            alglib.complex[] fft_dataCopy = new alglib.complex[MaxPoint];

            alglib.fft.fftr1d(bufferC, (int)MaxPoint, ref fft_data, null);

            prc += 8;

            double[] RDA = new double[MaxPoint];

            //***********************
            //           fft[] fft_dataCopy = new fft[MaxPoint];
            for (i = 0; i < (int)MaxPoint; i++)
            {
                fft_dataCopy[i].x = fft_data[i].x;
                fft_dataCopy[i].y = fft_data[i].y;
            }
            //***********************

            filter(ref fft_data, ref fft_dataCopy);

            alglib.fft.fftr1dinv(fft_data, (int)MaxPoint, ref RDA, null);

            using (BinaryWriter Writer = new BinaryWriter(File.Open("LINEC_LOW.dat", FileMode.Append)))
            {
                for (i = 0; i < RDA.Length; i++)
                    Writer.Write((double)RDA[i]);

                Writer.Close();

            }

            alglib.fft.fftr1dinv(fft_dataCopy, (int)MaxPoint, ref RDA, null);
            prc += 8;

            using (BinaryWriter Writer = new BinaryWriter(File.Open("LINEC_HI.dat", FileMode.Append)))
            {
                for (i = 0; i < RDA.Length; i++)
                    Writer.Write((double)RDA[i]);
                Writer.Close();
            }
            prc += 8;
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            string name;
            long pos;
            BinaryReader reader;
            BinaryWriter Writer;
            StreamWriter f;

            progressBar1.Value = prc;
            
            if (mes!="")
            {
                listBox1.Items.Add(mes);
                mes = "";
            }

            if (myFileThread.IsAlive == false)
            {
                timer1.Enabled = false;
                listBox1.Items.Add("Обработка успешно завершена!");

                if (ZETlab.Checked)
                {
                    listBox1.Items.Add("Экспорт данных в ZETlab...");
                    progressBar1.Value = 90;

                    name = savefilename.Remove(savefilename.Length - 4, 4);
                    if (checkBox1.Checked)
                    {
                        reader = new BinaryReader(File.Open("LINEA_LOW.dat", FileMode.Open));
                        listBox1.Items.Add("Создаем файл " + name + "_LOW_I.ana");
                        Writer = new BinaryWriter(File.Open(name + "_LOW_I.ana", FileMode.Create));
                        pos = cont_total;
                        while (pos>0)
                        {
                            Writer.Write((Single)reader.ReadDouble());
                            pos--;
                      //      progressBar1.Value = 0 + (int)(15.0/cont_total*pos);
                        }
                        Writer.Close();
                        reader.Close();

                        listBox1.Items.Add("Создаем файл " + name + "_LOW_I.anp");
                        f = new StreamWriter(name + "_LOW_I.anp");
                        f.WriteLine(name + "_LOW_I");
                        f.WriteLine("FRQ " + FREQ.ToString());
                        f.WriteLine("FORMAT f2");
                        f.WriteLine("START  " + fdate.ToLongTimeString());
                        f.WriteLine("DATE " + fdate.ToShortDateString());
                        f.Close();

                        reader = new BinaryReader(File.Open("LINEA_HI.dat", FileMode.Open));
                        listBox1.Items.Add("Создаем файл " + name + "_HI_I.ana");
                        Writer = new BinaryWriter(File.Open(name + "_HI_I.ana", FileMode.Create));
                        pos = cont_total;
                        while (pos > 0)
                        {
                            Writer.Write((Single)reader.ReadDouble());
                            pos--;
                       //     progressBar1.Value = 15 + (int)(15.0 / cont_total * pos);
                        }
                        Writer.Close();
                        reader.Close();

                        listBox1.Items.Add("Создаем файл " + name + "_HI_I.anp");
                        f = new StreamWriter(name + "_HI_I.anp");
                        f.WriteLine(name + "_HI_I");
                        f.WriteLine("FRQ " + FREQ.ToString());
                        f.WriteLine("FORMAT f2");
                        f.WriteLine("START  " + fdate.ToLongTimeString());
                        f.WriteLine("DATE " + fdate.ToShortDateString());
                        f.Close();

                    }
                   // else
                      ///  progressBar1.Value = 30;

                    if (checkBox2.Checked)
                    {
                        reader = new BinaryReader(File.Open("LINEB_LOW.dat", FileMode.Open));
                        listBox1.Items.Add("Создаем файл " + name + "_LOW_II.ana");
                        Writer = new BinaryWriter(File.Open(name + "_LOW_II.ana", FileMode.Create));
                        pos = cont_total;
                        while (pos > 0)
                        {
                            Writer.Write((Single)reader.ReadDouble());
                            pos--;
                          //  progressBar1.Value = 30 + (int)(15.0 / cont_total * pos);
                        }
                        Writer.Close();
                        reader.Close();

                        listBox1.Items.Add("Создаем файл " + name + "_LOW_II.anp");
                        f = new StreamWriter(name + "_LOW_II.anp");
                        f.WriteLine(name + "_LOW_II");
                        f.WriteLine("FRQ " + FREQ.ToString());
                        f.WriteLine("FORMAT f2");
                        f.WriteLine("START  " + fdate.ToLongTimeString());
                        f.WriteLine("DATE " + fdate.ToShortDateString());
                        f.Close();

                        reader = new BinaryReader(File.Open("LINEB_HI.dat", FileMode.Open));
                        listBox1.Items.Add("Создаем файл " + name + "_HI_II.ana");
                        Writer = new BinaryWriter(File.Open(name + "_HI_II.ana", FileMode.Create));
                        pos = cont_total;
                        while (pos > 0)
                        {
                            Writer.Write((Single)reader.ReadDouble());
                            pos--;
                        //    progressBar1.Value = 45 + (int)(15.0 / cont_total * pos);
                        }
                        Writer.Close();
                        reader.Close();

                        listBox1.Items.Add("Создаем файл " + name + "_HI_II.anp");
                        f = new StreamWriter(name + "_HI_II.anp");
                        f.WriteLine(name + "_HI_II");
                        f.WriteLine("FRQ " + FREQ.ToString());
                        f.WriteLine("FORMAT f2");
                        f.WriteLine("START  " + fdate.ToLongTimeString());
                        f.WriteLine("DATE " + fdate.ToShortDateString());
                        f.Close();

                    }
                  //  else
                     //   progressBar1.Value = 60;

                    if (checkBox3.Checked)
                    {
                        reader = new BinaryReader(File.Open("LINEC_LOW.dat", FileMode.Open));
                        listBox1.Items.Add("Создаем файл " + name + "_LOW_III.ana");
                        Writer = new BinaryWriter(File.Open(name + "_LOW_III.ana", FileMode.Create));
                        pos = cont_total;
                        while (pos > 0)
                        {
                            Writer.Write((Single)reader.ReadDouble());
                            pos--;
                         //   progressBar1.Value = 60 + (int)(15.0 / cont_total * pos);
                        }
                        Writer.Close();
                        reader.Close();

                        listBox1.Items.Add("Создаем файл " + name + "_LOW_III.anp");
                        f = new StreamWriter(name + "_LOW_III.anp");
                        f.WriteLine(name + "_LOW_III");
                        f.WriteLine("FRQ " + FREQ.ToString());
                        f.WriteLine("FORMAT f2");
                        f.WriteLine("START  " + fdate.ToLongTimeString());
                        f.WriteLine("DATE " + fdate.ToShortDateString());
                        f.Close();

                        reader = new BinaryReader(File.Open("LINEC_HI.dat", FileMode.Open));
                        listBox1.Items.Add("Создаем файл " + name + "_HI_III.ana");
                        Writer = new BinaryWriter(File.Open(name + "_HI_III.ana", FileMode.Create));
                        pos = cont_total;
                        while (pos > 0)
                        {
                            Writer.Write((Single)reader.ReadDouble());
                            pos--;
                        //    progressBar1.Value = 75 + (int)(15.0 / cont_total * pos);
                        }
                        Writer.Close();
                        reader.Close();

                        listBox1.Items.Add("Создаем файл " + name + "_HI_III.anp");
                        f = new StreamWriter(name + "_HI_III.anp");
                        f.WriteLine(name + "_HI_III");
                        f.WriteLine("FRQ " + FREQ.ToString());
                        f.WriteLine("FORMAT f2");
                        f.WriteLine("START  " + fdate.ToLongTimeString());
                        f.WriteLine("DATE " + fdate.ToShortDateString());
                        f.Close();

                    }
                  //  else
                      //  progressBar1.Value = 90;

                    mes = "Удаляем временные файлы...";
                    System.IO.File.Delete(@"LINEA_LOW.dat");
                    System.IO.File.Delete(@"LINEB_LOW.dat");
                    System.IO.File.Delete(@"LINEC_LOW.dat");
                    System.IO.File.Delete(@"LINEA_HI.dat");
                    System.IO.File.Delete(@"LINEB_HI.dat");
                    System.IO.File.Delete(@"LINEC_HI.dat");
                    progressBar1.Value = 100;
                }

                progressBar1.Visible = false;
                
                button1.Enabled = true;
                checkBox1.Enabled = true;
                checkBox2.Enabled = true;
                checkBox3.Enabled = true;
                ZETlab.Enabled = true;

                this.Refresh();

            }
        }


        private void Form1_Load(object sender, EventArgs e)
        {
            this.Text = "Фильтрационный обработчик       " + Vers;
        }

        private void filter(ref alglib.complex[] fft_dataA, ref alglib.complex[] fft_dataB)
        {
            ulong MaxPoint = (ulong)fft_dataA.Length;
            ulong i;
            double F;
            //------------------------------
            for (i = 0; i < MaxPoint; i++)
            {
                F = ((float)FREQ / MaxPoint * i);                if (F < 0.3) { fft_dataA[i].x *= 0; fft_dataA[i].y *= 0; }
                else
                if ((F > 33) && (F < 37)) { fft_dataA[i].x = 0; fft_dataA[i].y *= 0; }
                else
                if ((F > 45) && (F < 55)) { fft_dataA[i].x = 0; fft_dataA[i].y *= 0; }
                else
                if ((F > 95) && (F < 105)) { fft_dataA[i].x = 0; fft_dataA[i].y *= 0; }
                else
                if (F > 140) { fft_dataA[i].x = 0; fft_dataA[i].y *= 0; }
                else { fft_dataA[i].x *= 2; }


                //-----------------------------------
                //-----------------------------

                if (F < 160) { fft_dataB[i].x = 0; fft_dataB[i].y *= 0; }
                else
                    if (F > 1500) { fft_dataB[i].x = 0; fft_dataB[i].y *= 0; }
                else
                {
                    fft_dataB[i].x *= 2000;
                }

                            //-----------------------------
            }

        }

 

    }
}
