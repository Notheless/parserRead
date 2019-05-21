using System;
using System.IO;

namespace parserRead
{
    partial class MainForm
    {
        static string APPDATA_PATH = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        static string CFGFOLDER_PATH = Path.Combine(APPDATA_PATH, "ParserSystem");
        static string CFGFILE_PATH = Path.Combine(CFGFOLDER_PATH, "config.cfg");

        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;
        private string FolderPath;
        private long ResetTimer;
        private bool ResetTrigger;
        private bool HighLight;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Text = "Form1";
            LoadConfig();
        }

        private void LoadConfig()
        {
            if (File.Exists(CFGFILE_PATH))
            {
                using(TextReader textReader = new StreamReader(CFGFILE_PATH))
                {

                }
            }
        }

        #endregion
    }
}

