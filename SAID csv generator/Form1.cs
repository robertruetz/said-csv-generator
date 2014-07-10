using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Data;

namespace SAID_csv_generator
{
    public partial class Form1 : Form
    {
        public string GuessInflection(string input)
        {
            string guess = "";
            string inp = input;

            //remove any stray quotation marks at the start/end.
            if (inp.StartsWith("\""))
            {
                inp = inp.Remove(0,1);
            }
            if (inp.EndsWith("\""))
            {
                inp = inp.Remove(inp.Length - 1, 1);
            }
            //------------------
            
            //Start guessing inflection.
            else if (!inp.StartsWith("...") && (inp.EndsWith("...") || inp.EndsWith(",")))
            {
                guess = "r";
            }

            else if (inp.StartsWith("...") && inp.EndsWith("."))
            {
                if (inp.EndsWith("..."))
                {
                    guess = "n";
                }

                else
                    guess = "f";
            }

            else if (inp.StartsWith("...") && inp.EndsWith(","))
            {
                guess = "n";
            }

            else if (inp.EndsWith("?"))
            {
                guess = "q";
            }

            else
                guess = "na";

            return guess;
        }

        //Replace any Word chars with c# friendly chars.
        public static string ReplaceWordChars(string text)
        {
            var s = text;

            // smart single quotes and apostrophe
            s = Regex.Replace(s, "[\u2018|\u2019|\u201A]", "'");

            // smart double quotes
            s = Regex.Replace(s, "[\u201C|\u201D|\u201E]", "\"");

            // ellipsis
            s = Regex.Replace(s, "\u2026", "...");

            // dashes
            s = Regex.Replace(s, "[\u2013|\u2014]", "-");

            // circumflex
            s = Regex.Replace(s, "\u02C6", "^");

            // open angle bracket
            s = Regex.Replace(s, "\u2039", "<");

            // close angle bracket
            s = Regex.Replace(s, "\u203A", ">");

            // spaces
            s = Regex.Replace(s, "[\u02DC|\u00A0]", " ");

            return s;

        }

        public string FindBestMatch(string input, Dictionary<string, string> dict)
        {
            if (dict.ContainsKey(input))
                return input;
            else if (input.Length >= 1)
                return FindBestMatch(input.Substring(0, input.Length - 1), dict);
            else
                return "";
        }

        public void statusTimer(string inText, int setTime)
        {
            toolStripStatusLabel1.Text = inText;
            toolStripStatusLabel1.ForeColor = Color.Red;
            this.timer1.Enabled = true;
            this.timer1.Interval = setTime;
        }

        public string AltScript(string inputText)
        {
            StringBuilder build = new StringBuilder(inputText);

            int position = inputText.IndexOf(inputText.First(char.IsLetterOrDigit));

            build.Insert(position, "(ALT)");

            return build.ToString();
        }

        public IEnumerable<string> FindAlts(List<string> inFolder, List<string> inScript)
        {
            IEnumerable<string> alts;
            alts = inFolder.Except(inScript);
            return alts;

        }

        public string SuggestText(string thisID)
        {
            string sugg;

            if (thisID.Contains("-alt"))
            {
                int start = thisID.IndexOf("-alt");
                sugg = thisID.Substring(0, start);
            }

            else
            {
                sugg = thisID;
            }

            return sugg;
        }

        public string ConvertChars(string str)
        {
            string strFormD = str.Normalize(NormalizationForm.FormD);
            StringBuilder sbOutput = new StringBuilder();
            for (int ich = 0; ich < strFormD.Length; ich++)
            {
                UnicodeCategory uc = CharUnicodeInfo.GetUnicodeCategory(strFormD[ich]);
                if (uc != UnicodeCategory.NonSpacingMark)
                {
                    sbOutput.Append(strFormD[ich]);
                }
            }
            return sbOutput.ToString();
        } 
    
        private Form2 frm2 = new Form2();

        //-----------------------
        //variable declarations
        string hotFolder;
        string outFolder;
        string[] fileHolder;
        List<string> fileList = new List<string>();
        List<SaidFile> saidFiles = new List<SaidFile>();
        IEnumerable<string> alternates;
        Dictionary<string, string> dictionary = new Dictionary<string, string>();
        Dictionary<string, string> transDict = new Dictionary<string, string>();
        List<string> scripted = new List<string>();
        DataTable masterTable;
        //-----------------------

        public Form1()
        {
            InitializeComponent();
            frm2.AdviseParent += new Form2.AdviseParentEventHandler(SetFromForm2);

            this.Text = "SAID CSV Generator  v " + Assembly.GetEntryAssembly().GetName().Version;

        }

        private void folderBrowserDialog1_HelpRequest(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            //----------------------
            //This section clears the fields populated when this dialog is run.
            listBox1.Items.Clear();
            hotFolder = String.Empty;
            fileHolder = null;
            outFolder = String.Empty;
            fileList.Clear();
            label1.Text = "Choose the folder of your renamed wav files";
            label1.ForeColor = Color.Black;
            //----------------------

            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                hotFolder = folderBrowserDialog1.SelectedPath;

                fileHolder = Directory.GetFiles(hotFolder, "*.wav");
                outFolder = Directory.GetParent(hotFolder).ToString();

                if (fileHolder.Length > 0)
                {
                    label1.Text = hotFolder;
                    label1.ForeColor = Color.Red;
                    foreach (string x in fileHolder)
                    {
                        fileList.Add(Path.GetFileNameWithoutExtension(x));
                        listBox1.Items.Add(Path.GetFileNameWithoutExtension(x));
                    }
                }

                else
                    MessageBox.Show("The folder you selected doesn't appear to contain any audio files.\r\nTry Again.", "Warning!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);

                label2.Text = fileList.Count.ToString();
            }
        }

        private void Form1_Click(object sender, EventArgs e)
        {
            label9.Text = "Status: No Script Loaded.";
            label9.ForeColor = Color.Black;
            frm2.ShowDialog();
        }

        public void SetFromForm2(DataTable heresIt)
        {
            //------------------------
            //This section clears our variables for new data
            saidFiles.Clear();
            dictionary.Clear();
            label3.Text = "0";
            masterTable = null;
            //------------------------

            masterTable = heresIt;
            
            for (int yo = 0; yo < heresIt.Rows.Count - 1; yo++)
            {
                //pull the first element from our table as the fID.
                String fileIDTemp = ReplaceWordChars(heresIt.Rows[yo][0].ToString().Trim());
                //remove commas from fileID as this will throw off the SAID import.
                fileIDTemp = fileIDTemp.Replace(',', '_');

                //pull the second element from our table as the script.
                String scriptTemp = ReplaceWordChars(heresIt.Rows[yo][1].ToString().Trim());

                //create new SaidFile object with our table data.
                SaidFile temp = new SaidFile(fileIDTemp, scriptTemp);

                //add file ID to our scripted list for later comparisons.
                scripted.Add(temp.FileID);

                //If there is a translation we need to include that in our object
                if (heresIt.Columns.Count > 2)
                {
                    temp.Translation = ReplaceWordChars(heresIt.Rows[yo][2].ToString().Trim());
                    temp.HasTranslation = true;
                    if (!transDict.ContainsKey(temp.FileID))
                        transDict.Add(temp.FileID, temp.Translation);
                    //If there is a translation, we want to guess inflections with the trans instead of script.
                    temp.Inflection = GuessInflection(temp.Translation);
                }
                else
                {
                    //If there is no translation, guess based on the Script field.
                    temp.Inflection = GuessInflection(temp.Script);
                }


                //check for duplicate IDs.
                if (!dictionary.ContainsKey(temp.FileID))
                {
                    dictionary.Add(temp.FileID, temp.Script);
                    saidFiles.Add(temp);
                }
                else
                {
                    MessageBox.Show(String.Format("File ID {0} appears to be duplicated on the script.\r\n This file can be dealt with as an Alternate", temp.FileID), "Warning!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }

            if (fileList.Count != 0)
            {
                listBox2.Items.Clear();

                //find the alts
                alternates = FindAlts(fileList, scripted);
                List<string> altList = alternates.ToList();
                if (altList.Count > 0)
                {
                    statusTimer("Script Loaded. -- " + altList.Count + " ALTs Found.", 5000);

                    foreach (string alt in alternates)
                    {
                        listBox2.Items.Add(alt);
                    }
                }

                else
                {
                    statusTimer("Script Loaded. -- No ALTs Found.", 5000);
                }   

                if (listBox2.Items.Count != 0)
                {
                    listBox2.SetSelected(0, true);
                }
            }

            else
                MessageBox.Show("Data Missing. Could not search for Alts.", "Warning!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            
            label3.Text = saidFiles.Count.ToString();

            label9.Text = "Script Loaded.";
            label9.ForeColor = Color.Red;
        }

        private void listBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            textBox1.Text = listBox2.SelectedItem.ToString();
            string theKey = FindBestMatch(textBox1.Text, dictionary);
            textBox3.Text = theKey;

            if (dictionary.ContainsKey(theKey))
            {
                textBox2.Text = AltScript(dictionary[theKey]);
            }
            else
            {
                textBox2.Text = "No match was found. Please enter your own script in this box.";
            }
        }

        private static string AddDifference(string newScript, string oldScript, string oldTrans)
        {
            string tempString = newScript.Replace(oldScript, "");
            string outString = tempString + oldTrans;
            return outString;
        }

        private void button4_Click(object sender, EventArgs e)
        {
            if (textBox1.Text != "" && textBox2.Text != "")
            {
                string newTrans = "";
                if (masterTable.Columns.Count > 2)
                {
                    string tempTrans = transDict[textBox3.Text];
                    string tempScript = dictionary[textBox3.Text];
                    newTrans = AddDifference(textBox2.Text, tempScript, tempTrans);
                }

                SaidFile altHandler = new SaidFile(textBox1.Text, newTrans, textBox2.Text);
                altHandler.Inflection = GuessInflection(altHandler.Script);
                saidFiles.Add(altHandler);


                scripted.Add(textBox1.Text);
                statusTimer(String.Format("{0} was added", textBox1.Text), 3000);

                textBox1.Clear();
                textBox2.Clear();
                textBox3.Clear();

                listBox2.Items.Clear();

                //find the alts again
                alternates = FindAlts(fileList, scripted);
                if (alternates != null)
                {
                    foreach (string alt in alternates)
                    {
                        listBox2.Items.Add(alt);
                    } 
                }

                label3.Text = saidFiles.Count.ToString();

                if (listBox2.Items.Count !=0)
                {
                    listBox2.SetSelected(0, true);
                }
            }

            else
            {
                MessageBox.Show("Info is missing. File can't be added.", "Warning!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }


        private void button6_Click(object sender, EventArgs e)
        {
            if (saidFiles.Count != 0)
            {
                try
                {
                    System.IO.StreamWriter toText = new System.IO.StreamWriter(Path.Combine(outFolder, "forSaidUpload.csv"), false, Encoding.UTF8);
                    if (saidFiles[0].HasTranslation)
                    {
                        toText.WriteLine("File ID,Inflection,Translation,Script");
                        foreach (SaidFile file in saidFiles)
                        {

                            file.Script = BadChars(file.Script);
                            file.Translation = BadChars(file.Translation);
                            toText.WriteLine(String.Format("{0},{1},{2},{3}", file.FileID, file.Inflection, file.Translation, file.Script));
                        }
                    }

                    else
                    {
                        toText.WriteLine("File ID,Inflection,Script");

                        foreach (SaidFile file in saidFiles)
                        {
                            file.Script = BadChars(file.Script);
                            toText.WriteLine(String.Format("{0},{1},{2}", file.FileID, file.Inflection, file.Script));
                        }
                    }

                    toText.Close();

                    toolStripStatusLabel1.Text = "CSV Created Successfully.";
                    this.timer1.Enabled = false;
                }
                catch (IOException exc)
                {
                    MessageBox.Show("Can't write CSV file. Make sure you don't have it open.", "Warning!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    Console.WriteLine(exc.Message);
                    return;
                }
            }

            else
            {
                statusTimer("CSV Not Created.", 5000);
                MessageBox.Show("Data missing, cannot create CSV. Try again!", "Warning!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        private static string BadChars(string file)
        {
            //Checks for chars that might break the csv and escapes them
            char[] csvTokens = new char[] { '\"', ',', '\n', '\r' };
            string s = file;
            if (s.IndexOfAny(csvTokens) >= 0)
            {
                s = "\"" + s.Replace("\"", "\"\"") + "\"";
            }

            return s;
        }

        private void button5_Click(object sender, EventArgs e)
        {
            DialogResult dResult = MessageBox.Show("Are you sure you want to reset?", "Reset Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (dResult == DialogResult.Yes)
            {
                this.Controls.Clear();
                this.InitializeComponent();
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            ToolTip folderImportTip = new ToolTip();
            folderImportTip.SetToolTip(this.button1, "Select the folder containing your audio files.");

            ToolTip scriptImportTip = new ToolTip();
            scriptImportTip.SetToolTip(this.button3, "Import your script from a Word document.");

            ToolTip resetFormTip = new ToolTip();
            resetFormTip.SetToolTip(this.button5, "Reset the form completely.");

            ToolTip fixAltsTip = new ToolTip();
            fixAltsTip.SetToolTip(this.button4, "Fix the alternate listed above based on the suggested script.");

            ToolTip createCSVTip = new ToolTip();
            createCSVTip.SetToolTip(this.button6, "Create a CSV file based on this form.");
            
            ToolTip iGotThisTip = new ToolTip();
            iGotThisTip.SetToolTip(this.button7, "Change the suggestion text and hit this button to find matching script.");
        }

        private void button7_Click(object sender, EventArgs e)
        {
            if (textBox3.Text.Length > 0)
            {
                string theKey = SuggestText(textBox3.Text);

                if (dictionary.ContainsKey(theKey))
                {
                    textBox2.Text = dictionary[theKey];
                }

                else
                {
                    textBox2.Text = "No match was found. Please enter your own script in this box.";
                }
            }

            else
                MessageBox.Show("There doesn't appear to be an ALT file selected.", "Warning!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            toolStripStatusLabel1.Text = "";
        }

        private void listBox1_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop, false))
            {
                e.Effect = DragDropEffects.All;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        private void listBox1_DragDrop(object sender, DragEventArgs e)
        {
            listBox1.Items.Clear();
            hotFolder = String.Empty;
            fileHolder = null;
            outFolder = String.Empty;
            fileList.Clear();
            toolStripStatusLabel1.Text = "";

            foreach (var s in (string[])e.Data.GetData(DataFormats.FileDrop, false))
            {
                if (Directory.Exists(s))
                {
                    fileHolder = Directory.GetFiles(s, "*.wav");
                }
                else
                {
                    toolStripStatusLabel1.ForeColor = Color.Red;
                    toolStripStatusLabel1.Text = "Must drag a FOLDER of wav files!";
                    label1.Text = "";
                    return;
                }

            }

            foreach (string xx in fileHolder)
            {
                fileList.Add(Path.GetFileNameWithoutExtension(xx));
            }

            if (fileList.Count > 0)
            {
                hotFolder = Directory.GetParent(fileHolder[0]).ToString();
                outFolder = Directory.GetParent(hotFolder).ToString();

                toolStripStatusLabel1.Text = String.Format("Folder Chosen: {0}", hotFolder);
                toolStripStatusLabel1.ForeColor = Color.Red;
            }
            else
            {
                toolStripStatusLabel1.ForeColor = Color.Red;
                toolStripStatusLabel1.Text = "Folder does not contain wav files.";
            }


            foreach (string x in fileHolder)
            {
                listBox1.Items.Add(Path.GetFileName(x));
            }

            label2.Text = fileList.Count.ToString();
        }

    }

    public class SaidFile
    {
        private string fileID;
        private string script;
        private string inflection;
        private string translation;
        private bool hasTranslation;

        public SaidFile(string fID, string scrpt)
        {
            FileID = fID;
            Script = scrpt;
            Inflection = null;
            HasTranslation = false;
        }

        public SaidFile(string fID, string trans, string scrpt)
        {
            FileID = fID;
            Translation = trans;
            Script = scrpt;
            Inflection = null;
            HasTranslation = true;
        }

        public string FileID
        {
            get { return this.fileID; }
            set { this.fileID = value; }
        }

        public string Script
        {
            get { return this.script; }
            set { this.script = value; }
        }

        public string Inflection
        {
            get { return this.inflection; }
            set { this.inflection = value; }
        }

        public string Translation
        {
            get { return this.translation; }
            set { this.translation = value; }
        }

        public bool HasTranslation
        {
            get { return this.hasTranslation; }
            set { this.hasTranslation = value; }
        }

    }
}
