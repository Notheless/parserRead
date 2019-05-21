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

        private ConfigurationFile configuration;
        private List<ReadableResultLog> ReadableResult;


        private void LoadConfig()
        {
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

        private async Task BackgroundCapture()
        {
            while (true)
            {
                Task.Delay(1000).Wait();
                var x = 1;
            }
        }

        public MainForm()
        {
            InitializeComponent();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            LoadConfig();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            List<CSVParseModel> parseModels = new List<CSVParseModel>();
            TextReader reader;
            ReadableResult = new List<ReadableResultLog>();
            string filetarget = "C:\\Users\\Alfian\\pso2\\pso2_bin\\damagelogs\\1557058730.csv";
            string path = AppContext.BaseDirectory + "temp.csv";
            File.Copy(filetarget, path, true);

            using (reader = new StreamReader(path))
            {
                var CsvReaderP = new CsvReader(reader);
                CsvReaderP.Configuration.PrepareHeaderForMatch = (string header, int index)=> Regex.Replace(header, @"\s", string.Empty);
                CsvReaderP.Read();
                CsvReaderP.ReadHeader();
                parseModels = CsvReaderP.GetRecords<CSVParseModel>()
                    .OrderBy(x => x.timestamp)
                    .Where(x=>(int.Parse(x.timestamp) > LastTIme&& int.Parse(x.sourceID)>10000))
                    .ToList();
                
            }
            RecordUpdate(parseModels);
            dataGridView1.DataSource = ReadableResult;
            var c = 1;
        }

        private void RecordUpdate(List<CSVParseModel> cSVParseModels)
        {
            LastTIme = int.Parse(cSVParseModels.First().timestamp);
            foreach(var data in cSVParseModels)
            {
                if(!ReadableResult.Any(x=> (string.Compare(x.Id, data.sourceID) == 0 && string.Compare(x.Instance, data.instanceID) == 0))){
                    ReadableResult.Add(new ReadableResultLog
                    {
                        Damage = 0,
                        DPS="0",
                        Id = data.sourceID,
                        Name = data.sourceName,
                        Instance = data.instanceID

                    });
                        
                }
                ReadableResult
                    .Where(x => string.Compare(x.Id, data.sourceID) == 0)
                    .ToList()
                    .ForEach(x => x.Damage += int.Parse(data.damage));
            }
            ReadableResult = ReadableResult.OrderByDescending(x => x.Instance).ThenByDescending(x=>x.Damage).ToList();
            label1.Text = 
                $"Start Time :" + StartTIme.ToString() + "\n" +
                $"Last Time  :" + LastTIme.ToString() + "\n" +
                $"configuration FolderPath :" + configuration.FolderPath.ToString() + "\n" +
                $"configuration HighLight :" + configuration.HighLight.ToString() + "\n" +
                $"configuration ResetTimer :" + configuration.ResetTimer.ToString() + "\n" +
                $"configuration ResetTrigger :" + configuration.ResetTrigger.ToString() + "\n"
                ;

        }


        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void readableResultLogBindingSource_CurrentChanged(object sender, EventArgs e)
        {

        }
    }
}
