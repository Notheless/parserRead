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
using System.Net;
using MoreLinq;

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
        private long LatestInstance;
        private long PosisitionIndicator;
        private bool bgrun;
        private List<string> IgnoreId;

        private string filetarget;
        private string path = AppContext.BaseDirectory + "src.csv";

        private ConfigurationFile configuration;
        private List<ReadableResultLog> ReadableResult;
        private List<SkillMappingModel> skillMap;




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

        private void Button3_Click(object sender, EventArgs e)
        {
            StartTIme = 0;
            PosisitionIndicator = 0;
            ReadableResult = new List<ReadableResultLog>();
        }

        private void Button4_Click(object sender, EventArgs e)
        {
        }

        private void Label1_Click(object sender, EventArgs e)
        {

        }
        private void Button2_Click(object sender, EventArgs e)
        {
            try
            {
                if (bgrun) throw new Exception(" Already running");

                if(!File.Exists(filetarget))OpenFile();
                File.Copy(filetarget, path, true);

                label1.Invoke(new Action(() => { label1.Text = "BBB"; }));
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
                long newIndicator;
                List<CSVParseModel> parseModels = new List<CSVParseModel>();
                TextReader reader;
                while (true)
                {

                    File.Copy(filetarget, path, true);

                    using (reader = new StreamReader(path))
                    {
                        var CsvReaderP = new CsvReader(reader);
                        CsvReaderP.Configuration.PrepareHeaderForMatch = (string header, int index) => Regex.Replace(header, @"\s", string.Empty);
                        CsvReaderP.Read();
                        CsvReaderP.ReadHeader();
                        parseModels = CsvReaderP.GetRecords<CSVParseModel>()
                            .Where(x => 
                                int.Parse(x.sourceID) > 100000 &&
                                !IgnoreId.Any(ignore=>string.Compare(ignore,x.targetID)==0)
                                )
                            .ToList();
                        newIndicator = parseModels.LongCount();
                        if (PosisitionIndicator == 0) PosisitionIndicator = newIndicator;
                        
                    }
                    int difference = (int)(newIndicator - PosisitionIndicator);
                    parseModels = parseModels.GetRange((int)PosisitionIndicator, difference).ToList() ;
                    foreach(var data in parseModels)
                    {
                        if(!ReadableResult.Any(player => string.Compare( player.Id,data.sourceID)==0))
                        {
                            var newData = new ReadableResultLog()
                            {
                                Id = data.sourceID,
                                Name = data.sourceName,
                                DPS = 0.0,
                                Damage = 0,
                                Heal = 0,
                                NumberOfCrit = 0,
                                NumberOfHits = 0,
                                NumberOfJA = 0,
                                PlayersSkill = new List<playersSkillLog>(),
                                Start = DateTime.Now,
                                LastHit = DateTime.Now
                            };
                            ReadableResult.Add(newData);
                        }
                        var UpdateData = ReadableResult.Where(player => string.Compare(player.Id, data.sourceID) == 0).First();

                        if (UpdateData != null)
                        {
                            ReadableResult.Remove(UpdateData);

                            if (double.Parse(data.damage) > 0)
                            {

                                UpdateData.Damage += double.Parse(data.damage);
                                UpdateData.NumberOfHits += 1;
                                if (string.Compare(data.IsCrit, "1") == 0) UpdateData.NumberOfCrit += 1;
                                if (string.Compare(data.IsJA, "1") == 0) UpdateData.NumberOfJA += 1;

                                var NamePA = skillMap.Find(x => string.Compare(x.ID, data.attackID) == 0).Kind;
                                if (UpdateData.PlayersSkill.Any(x => string.Compare(x.Kind, NamePA) == 0)){
                                    UpdateData.PlayersSkill.Find(x => string.Compare(x.Kind, NamePA)==0).Damage += double.Parse(data.damage);
                                }
                                else
                                {
                                    var skillPA = new playersSkillLog()
                                    {
                                        Damage = double.Parse(data.damage),
                                        Kind = NamePA,
                                    };
                                }

                            }

                            else UpdateData.Heal -= double.Parse(data.damage);

                            ReadableResult.Add(UpdateData);
                        }
                    }
                    foreach(var data in ReadableResult)
                    {
                        data.LastHit = DateTime.Now;
                        data.DPS = (data.Damage / data.TimeLenght.TotalSeconds)/1000;

                    }

                    ReadableResult = ReadableResult.OrderByDescending(x => x.Damage).ToList();
                    dataGridView1.Invoke(new Action(() => { dataGridView1.DataSource = DataShows(); }));

                    var temp =
                        $"Start Time :" + StartTIme.ToString() + "\n" +
                        $"Last Time  :" + LastTIme.ToString() + "\n" +
                        $"length  :" + Lenght.ToString() + "\n" +
                        $"lenght Time  :" + LenghtTime.ToString() + "\n" +
                        $"latest Instance  :" + LatestInstance.ToString() + "\n" +
                        $"PosisitionIndicator  :" + PosisitionIndicator.ToString() + "\n" +
                        $"newIndicator  :" + newIndicator.ToString() + "\n" +
                        $"";
                    label1.Invoke(new Action(() => { label1.Text = temp; }));

                    foreach (DataGridViewColumn column in dataGridView1.Columns)
                    {
                        column.SortMode = DataGridViewColumnSortMode.Automatic;
                    }

                    PosisitionIndicator = newIndicator;
                    Task.Delay(1000).Wait();
                }
            }
            catch { }
            return 1;
        }
        private List<DataShow> DataShows()
        {
            var result = new List<DataShow>();
            foreach(var data in ReadableResult)
            {
                var MainSkill = data.PlayersSkill.Find(x=> x.Damage == data.PlayersSkill.Max(y => y.Damage));
                var playerData = new DataShow()
                {
                    Name = data.Name,
                    Damage = (data.Damage).ToString(),
                    Heal = data.Heal.ToString(),
                    DPS = data.DPS.ToString() + "K",
                    CritRate = (100 * data.NumberOfCrit / data.NumberOfHits).ToString() + "%",
                    JARate = (100 * data.NumberOfJA / data.NumberOfHits).ToString() + "%",
                    MainSkill = $"{MainSkill.Kind} : {MainSkill.Damage}"
                };
                result.Add(playerData);
            }
            return result;
        }
        private void OpenFile()
        {

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

        private void SaveConfig(ConfigurationFile configuration)
        {
            if (!Directory.Exists(CFGFOLDER_PATH)) Directory.CreateDirectory(CFGFOLDER_PATH);

            using (TextWriter textWriter = new StreamWriter(CFGFILE_PATH))
            {
                string temp = JsonConvert.SerializeObject(configuration);
                textWriter.Write(temp);
            }
        }

        private void LoadConfig()
        {
            //Ignore certain enemies as target
            IgnoreId = new List<string>()
            {
                "401",
            };


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

            TextReader reader;

            try
            {
                WebClient client = new WebClient();
                Stream stream = client.OpenRead("https://raw.githubusercontent.com/VariantXYZ/PSO2ACT/master/PSO2ACT/skills.csv");
                StreamReader webreader = new StreamReader(stream);
                String content = webreader.ReadToEnd();

                File.WriteAllText(AppContext.BaseDirectory + "skills.csv", content);
            }
            catch { }

            if (File.Exists(AppContext.BaseDirectory + "skills.csv"))
            {

                using (reader = new StreamReader(AppContext.BaseDirectory + "skills.csv"))
                {
                    var CsvReaderP = new CsvReader(reader);
                    CsvReaderP.Configuration.Delimiter = ",";
                    CsvReaderP.Configuration.IgnoreQuotes = true;
                    CsvReaderP.Configuration.BadDataFound = x => { textBox2.AppendText(x.RawRecord); };
                    CsvReaderP.Read();
                    CsvReaderP.ReadHeader();
                    skillMap = CsvReaderP.GetRecords<SkillMappingModel>().ToList();
                }
            }
            else { skillMap = new List<SkillMappingModel>(); }


        }
    }
}
