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

        private DateTime LastTimeUpdate;
        private long AtkPosisitionIndicator;
        private long RecPosisitionIndicator;
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

            AtkPosisitionIndicator = 0;
            RecPosisitionIndicator = 0;
            dataGridView1.DataSource = new List<DataShow>();
            LastTimeUpdate = DateTime.Now;
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
        private void TextBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void Button3_Click(object sender, EventArgs e)
        {
            ParserReset();
        }


        private void CheckBox1_CheckedChanged(object sender, EventArgs e)
        {
            numericUpDown1.Enabled = checkBox1.Checked;
            configuration.ResetTrigger = checkBox1.Checked;
            SaveConfig(configuration);
        }

        private void Label2_Click(object sender, EventArgs e)
        {

        }

        private void NumericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            configuration.ResetTimer = (long)numericUpDown1.Value;
            SaveConfig(configuration);
        }

        private void Button4_Click_1(object sender, EventArgs e)
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
            try
            {
                long AttacknewIndicator, RecievenewIndicator;
                List<CSVParseModel> PlayerAttack = new List<CSVParseModel>();
                List<CSVParseModel> PlayerRecieve = new List<CSVParseModel>();
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
                        var allData = CsvReaderP.GetRecords<CSVParseModel>();
                        PlayerAttack = allData
                            .Where(x => 
                                int.Parse(x.sourceID) > 100000 &&
                                !IgnoreId.Any(ignore=>string.Compare(ignore,x.targetID)==0)
                                )
                            .ToList();


                        AttacknewIndicator = PlayerAttack.LongCount();
                        if (AtkPosisitionIndicator == 0) AtkPosisitionIndicator = AttacknewIndicator;
                        
                    }

                    using (reader = new StreamReader(path))
                    {
                        var CsvReaderP = new CsvReader(reader);
                        CsvReaderP.Configuration.PrepareHeaderForMatch = (string header, int index) => Regex.Replace(header, @"\s", string.Empty);
                        CsvReaderP.Read();
                        CsvReaderP.ReadHeader();
                        var allData = CsvReaderP.GetRecords<CSVParseModel>();

                        PlayerRecieve = allData
                            .Where(x =>
                                int.Parse(x.targetID) > 100000 &&
                                int.Parse(x.damage) > 0
                                )
                            .ToList();


                        RecievenewIndicator = PlayerRecieve.LongCount();
                        if (RecPosisitionIndicator == 0) RecPosisitionIndicator = RecievenewIndicator;
                    }

                    int Atkdifference = (int)(AttacknewIndicator - AtkPosisitionIndicator);
                    int Recdifference = (int)(RecievenewIndicator - RecPosisitionIndicator);
                    PlayerAttack = PlayerAttack.GetRange((int)AtkPosisitionIndicator, Atkdifference).ToList();
                    PlayerRecieve = PlayerRecieve.GetRange((int)RecPosisitionIndicator, Recdifference).ToList();

                    foreach (var data in PlayerAttack)
                    {
                        if(!ReadableResult.Any(player => string.Compare( player.Id,data.sourceID)==0))
                        {
                            var newData = new ReadableResultLog()
                            {
                                Id = data.sourceID,
                                Name = data.sourceName,
                                DPS = 0.0,
                                Damage = 0,
                                RecievedDamage =0,
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
                                if (UpdateData.PlayersSkill.Any(x => string.Compare(x.Kind, NamePA) == 0))
                                {
                                    UpdateData.PlayersSkill.Find(x => string.Compare(x.Kind, NamePA) == 0).Damage += double.Parse(data.damage);
                                    UpdateData.PlayersSkill.Find(x => string.Compare(x.Kind, NamePA) == 0).Hit += 1;
                                }
                                else
                                {
                                    var skillPA = new playersSkillLog()
                                    {
                                        Damage = double.Parse(data.damage),
                                        Kind = NamePA,
                                        Hit =1,
                                    };
                                }

                            }

                            else UpdateData.Heal -= double.Parse(data.damage);

                            UpdateData.LastHit = DateTime.Now;
                            LastTimeUpdate = DateTime.Now;
                            ReadableResult.Add(UpdateData);
                        }
                    }
                    foreach (var data in PlayerRecieve)
                    {
                        if (!ReadableResult.Any(player => string.Compare(player.Id, data.targetID) == 0))
                        {
                            var newData = new ReadableResultLog()
                            {
                                Id = data.sourceID,
                                Name = data.sourceName,
                                DPS = 0.0,
                                Damage = 0,
                                RecievedDamage = 0,
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
                        var UpdateData = ReadableResult.Where(player => string.Compare(player.Id, data.targetID) == 0).First();

                        if (UpdateData != null)
                        {
                            ReadableResult.Remove(UpdateData);

                            if (double.Parse(data.damage) > 0)
                            {

                                UpdateData.RecievedDamage += double.Parse(data.damage);

                            }

                            UpdateData.LastHit = DateTime.Now;
                            LastTimeUpdate = DateTime.Now;

                            ReadableResult.Add(UpdateData);
                        }
                    }
                    foreach (var data in ReadableResult)
                    {
                        data.DPS = (data.Damage / data.TimeLenght.TotalSeconds)/1000;
                    }

                    ReadableResult = ReadableResult.OrderByDescending(x => x.Damage).ToList();
                    dataGridView1.Invoke(new Action(() => { dataGridView1.DataSource = DataShows(); }));


                    foreach (DataGridViewColumn column in dataGridView1.Columns)
                    {
                        column.SortMode = DataGridViewColumnSortMode.Automatic;
                    }

                    AtkPosisitionIndicator = AttacknewIndicator;
                    RecPosisitionIndicator = RecievenewIndicator;

                    if (checkBox1.Checked)
                    {
                        TimeSpan timeSpan = DateTime.Now - LastTimeUpdate;
                        double tot = timeSpan.TotalSeconds - (double)numericUpDown1.Value;
                        //if (tot>0) ParserReset();
                    }
                    Task.Delay(1000).Wait();
                }
            }
            catch { }
            return 1;
        }
        private void LogRecord(string msg)
        {
            textBox2.Invoke(new Action(() => textBox2.Text += msg + "\n"));
        }
        private List<DataShow> DataShows()
        {
            var result = new List<DataShow>();
            foreach(var data in ReadableResult)
            {
                if (data.NumberOfHits == 0) data.NumberOfHits = 1;
                var MainSkill = data.PlayersSkill.Find(x=> x.Damage == data.PlayersSkill.Max(y => y.Damage));
                var playerData = new DataShow()
                {
                    Name = data.Name,
                    Damage = (data.Damage).ToString("0.##"),
                    RecievedDamage = data.RecievedDamage.ToString(),
                    Heal = data.Heal.ToString(),
                    DPS = data.DPS.ToString("0.##") + "K",
                    CritRate = (100 * data.NumberOfCrit / data.NumberOfHits).ToString() + "%",
                    JARate = (100 * data.NumberOfJA / data.NumberOfHits).ToString() + "%",
                    MainSkill = ""
                };
                if(MainSkill!=null)playerData.MainSkill = $"{MainSkill.Kind} : {MainSkill.Damage}";
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

        private void ParserReset()
        {
            if (ReadableResult.Count > 0)
            {
                if (!Directory.Exists(AppContext.BaseDirectory + "Results"))
                {
                    Directory.CreateDirectory(AppContext.BaseDirectory + "Results");
                }

                string Result = "========================\n";

                foreach (var data in ReadableResult)
                {
                    Result += $"Name : {data.Name}\t|| Damage Done : {data.Damage}\t|| Damage Recieved : {data.RecievedDamage}\t|| Heal : {data.Heal}\t|| DPS : {data.DPS}K \n";
                }
                Result += "========================\n";

                foreach (var data in ReadableResult)
                {
                    Result += $"Name            : {data.Name}\n";
                    Result += $"Damage Done     : {data.Damage}\n";
                    Result += $"Damage Recieved : {data.RecievedDamage}\n";
                    Result += $"Heal            : {data.Heal}\n";
                    Result += $"DPS             : {data.DPS}\n";
                    Result += $"JA Rate         : {data.NumberOfJA}\n";
                    Result += $"Crit rate       : {data.NumberOfCrit}\n";
                    Result += $"Detailed Skill  : \n";

                    foreach (var skill in data.PlayersSkill)
                    {
                        Result += $"{skill.Kind}\t|| Total Damage : {skill.Damage} \t|| Hit : {skill.Hit} times \t|| Average :{skill.Damage / skill.Hit}\n";
                    }
                    Result += "========================\n";
                }
                string FileName = AppContext.BaseDirectory + "Result\\" + DateTime.Now.ToString("ss-mm-hh_dd-MM-yyy") + ".txt";
                if (!Directory.Exists(AppContext.BaseDirectory + "Result\\")) Directory.CreateDirectory(AppContext.BaseDirectory + "Result\\");
                using (StreamWriter streamWriter = new StreamWriter(FileName, true, Encoding.ASCII))
                {
                    streamWriter.Write(Result);
                }
                LogRecord("record : " + ReadableResult.Count.ToString());
                AtkPosisitionIndicator = 0;
                RecPosisitionIndicator = 0;
                ReadableResult = new List<ReadableResultLog>();
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

            checkBox1.Checked = configuration.ResetTrigger;
            numericUpDown1.Enabled = checkBox1.Checked;
            numericUpDown1.Value = configuration.ResetTimer;

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
                    CsvReaderP.Read();
                    CsvReaderP.ReadHeader();
                    skillMap = CsvReaderP.GetRecords<SkillMappingModel>().ToList();
                }
            }
            else { skillMap = new List<SkillMappingModel>(); }


        }

        private void TextBox2_TextChanged(object sender, EventArgs e)
        {

        }
    }
}
