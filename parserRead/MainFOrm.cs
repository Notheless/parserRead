using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;
using parserRead.Model;
using CsvHelper;
using System.Text.RegularExpressions;
using System.Threading;

namespace parserRead
{
    public partial class MainForm : Form
    {
        static string APPDATA_PATH = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        static string CFGFOLDER_PATH = Path.Combine(APPDATA_PATH, "ParserSystem");
        static string CFGFILE_PATH = Path.Combine(CFGFOLDER_PATH, "config.cfg");

        /// <summary>
        /// Required designer variable.
        /// </summary>

        private long StartTIme;
        private long LastTIme;
        private long Lenght;
        private TimeSpan LenghtTime;
        private long latestInstance;
        private bool bgrun;

        private string filetarget;
        private string path = AppContext.BaseDirectory + "src.csv";

        private ConfigurationFile configuration;
        private List<ReadableResultLog> ReadableResult;


        private void LoadConfig()
        {
            bgrun = false;
            ReadableResult = new List<ReadableResultLog>();
            bool AllFilled = true;
            if (File.Exists(CFGFILE_PATH))
            {
                using (TextReader textReader = new StreamReader(CFGFILE_PATH))
                {
                    string temp = textReader.ReadToEnd();
                    configuration = JsonConvert.DeserializeObject<ConfigurationFile>(temp);
                    if (configuration.ResetTimer == 0) AllFilled = false;

                }
            }
            if (!File.Exists(CFGFILE_PATH) || !AllFilled)
            {
                configuration = new ConfigurationFile()
                {
                    FolderPath = "",
                    HighLight = true,
                    ResetTrigger = false,
                    ResetTimer = 30
                };

                SaveConfig(configuration);
            }
        }

        private void SaveConfig(ConfigurationFile configuration)
        {
            if (!Directory.Exists(CFGFOLDER_PATH)) Directory.CreateDirectory(CFGFOLDER_PATH);

            using (TextWriter textWriter = new StreamWriter(CFGFILE_PATH))
            {
                string temp = JsonConvert.SerializeObject(configuration);
                textWriter.Write(temp);
            }
        }

        public MainForm()
        {
            InitializeComponent();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            LoadConfig();

            textBox1.ReadOnly = true;
            openFileDialog1.InitialDirectory = configuration.FolderPath;
            openFileDialog1.Filter = "CSV Files | *.csv";
        }

        private void button1_Click(object sender, EventArgs e)
        {
            OpenFile();
        }


        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void readableResultLogBindingSource_CurrentChanged(object sender, EventArgs e)
        {

        }

        private void TextBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void Button2_Click(object sender, EventArgs e)
        {
            try
            {
                if (bgrun) throw new Exception(" Already running");

                if(!File.Exists(filetarget))OpenFile();

                File.Copy(filetarget, path, true);
                var bgw = new BackgroundWorker();
                bgrun = true;
                bgw.DoWork += (_, __) =>
                {
                    RecordUpdate();
                };
                bgw.RunWorkerAsync();
            }
            catch { }
        }

        private int RecordUpdate()
        {
            label1.Invoke(new Action(() => { label1.Text = "AAAA"; }));
            try
            {
                while (true)
                {

                    File.Copy(filetarget, path, true);
                    List<CSVParseModel> parseModels = new List<CSVParseModel>();
                    TextReader reader;

                    using (reader = new StreamReader(path))
                    {
                        var CsvReaderP = new CsvReader(reader);
                        CsvReaderP.Configuration.PrepareHeaderForMatch = (string header, int index) => Regex.Replace(header, @"\s", string.Empty);
                        CsvReaderP.Read();
                        CsvReaderP.ReadHeader();
                        parseModels = CsvReaderP.GetRecords<CSVParseModel>()
                            .OrderBy(x => x.timestamp)
                            .Where(x => (int.Parse(x.sourceID) > 100000 && int.Parse(x.damage) > 0))
                            .ToList();

                    }

                    try
                    {
                        LastTIme = int.Parse(parseModels.Last().timestamp);
                        if (StartTIme == 0) StartTIme = LastTIme;
                        else if (StartTIme != LastTIme)
                        {
                            Lenght = LastTIme - StartTIme;
                            LenghtTime = new TimeSpan(Lenght);
                        }
                    }
                    catch { }

                    parseModels = parseModels.Where(x => int.Parse(x.timestamp) >= LastTIme).ToList();

                    foreach (var data in parseModels)
                    {
                        if (!ReadableResult.Any(x => (string.Compare(x.Id, data.sourceID) == 0)))
                        {
                            ReadableResult.Add(new ReadableResultLog
                            {
                                Damage = 0,
                                DPS = "0",
                                Id = data.sourceID,
                                Name = data.sourceName,
                            });
                        }
                        if (int.Parse(data.timestamp) > LastTIme)
                        {
                            var y = ReadableResult.Where(x => x.Id == data.sourceID).First().Damage;
                            y += int.Parse(data.damage);
                            ReadableResult.Where(x => x.Id == data.sourceID).First().Damage = y;

                            ReadableResult.Where(x => x.Id == data.sourceID).First().DPS = (y / Lenght * 1000).ToString() + " K";
                        }


                    }

                    dataGridView1.Invoke(new Action(() => { dataGridView1.DataSource = ReadableResult; }));


                    ReadableResult = ReadableResult.OrderByDescending(x => x.Damage).ToList();
                    var temp =
                        $"Start Time :" + StartTIme.ToString() + "\n" +
                        $"Last Time  :" + LastTIme.ToString() + "\n" +
                        $"length  :" + Lenght.ToString() + "\n" +
                        $"lenght Time  :" + LenghtTime.ToString() + "\n" +
                        $"latest Instance  :" + latestInstance.ToString() + "\n" +
                        $"configuration FolderPath :" + configuration.FolderPath.ToString() + "\n" +
                        $"configuration HighLight :" + configuration.HighLight.ToString() + "\n" +
                        $"configuration ResetTimer :" + configuration.ResetTimer.ToString() + "\n" +
                        $"configuration ResetTrigger :" + configuration.ResetTrigger.ToString() + "\n"
                        ;
                    label1.Invoke(new Action(() => { label1.Text = temp; }));
                    Task.Delay(1000).Wait();
                }
            }
            catch { }
            return 1;
        }

        private void OpenFile()
        {

            int size = -1;
            DialogResult result = openFileDialog1.ShowDialog(); // Show the dialog.
            if (result == DialogResult.OK) // Test result.
            {
                string file = openFileDialog1.FileName;
                try
                {
                    configuration.FolderPath = file.Replace(openFileDialog1.SafeFileName, "");
                    SaveConfig(configuration);
                    textBox1.Text = file;
                    filetarget = file;
                }
                catch (IOException)
                {
                }
            }
        }

        private void Button3_Click(object sender, EventArgs e)
        {
            StartTIme = 0;
            ReadableResult = new List<ReadableResultLog>();
        }
    }
}
