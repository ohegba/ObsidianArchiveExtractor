using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace ObsidianArchiveExtractor
{
    public partial class Form1 : Form
    {
        static ObsidianArchiveFile oaf;

        static OAFEntry exportEntry;

        public Form1()
        {
            
            InitializeComponent();
            TextBoxStreamWriter tw = new TextBoxStreamWriter(textBox1);
            Console.SetOut(tw);
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OAF_finder.ShowDialog();
        }

        private void OAF_finder_FileOk(object sender, CancelEventArgs e)
        {
            listBox1.Items.Clear();
            GC.Collect();
            textBox1.Text = "";
            textBox2.Text = "";
            textBox3.Text = "";
            textBox4.Text = "";
            textBox5.Text = "";
            checkBox1.Checked = false;
            //try
           // {
                oaf = new ObsidianArchiveFile(OAF_finder.FileName);
                foreach (OAFEntry entry in oaf.fileList)
                {
                    listBox1.Items.Add(entry);
                }
                
           // }
          //  catch (Exception ee)
            //{
            //    MessageBox.Show(ee.Message, "A problem!");
            //}
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            Object o = listBox1.SelectedItem;
            if (o == null)
                return;

            OAFEntry ent = (OAFEntry) o;


            checkBox1.Checked = ent.compressed;
            textBox2.Text = ent.uncompressedSize.ToString();
            textBox3.Text = (ent.compressed)?ent.compressedSize.ToString():"N/A";
            textBox4.Text = ent.dataOffset.ToString();

            

        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            Object o = listBox1.SelectedItem;
            if (o == null)
                return;

            OAFEntry ent = (OAFEntry)o;

            exportEntry = ent;

            saveFileDialog1.FileName = Path.GetFileName(ent.name);
            saveFileDialog1.ShowDialog();
        }

        private void saveFileDialog1_FileOk(object sender, CancelEventArgs e)
        {
            OAFEntry ent = exportEntry;

            oaf.br.BaseStream.Seek(ent.dataOffset, SeekOrigin.Begin);

            int size = (ent.compressed) ? ent.compressedSize : ent.uncompressedSize;
            byte[] payload = oaf.br.ReadBytes(size);

            FileStream fs = new FileStream(saveFileDialog1.FileName, FileMode.Create);

            fs.Write(payload, 0, payload.Length);

            fs.Close();

            Console.WriteLine("The contained file " + ent.name + " has been written to " + saveFileDialog1.FileName+".");

        }

        private void button2_Click(object sender, EventArgs e)
        {

        }

        private void textBox5_TextChanged(object sender, EventArgs e)
        {
            if (oaf == null)
                return;

            listBox1.Items.Clear();
            foreach (OAFEntry o in oaf.fileList)
            {
                if (o.name.Contains(textBox5.Text))
                listBox1.Items.Add(o);
            }
        }
    }

    public class TextBoxStreamWriter : TextWriter
    {
        TextBox _output = null;

        public TextBoxStreamWriter(TextBox output)
        {
            _output = output;
        }

        public override void Write(char value)
        {
            _output.AppendText(value.ToString());
        }

        public override Encoding Encoding
        {
            get { throw new NotImplementedException(); }
        }

        
    }

    public class ObsidianArchiveFile
    {
        
        String filePath;
        public BinaryReader br;
        public List<OAFEntry> fileList = new List<OAFEntry>();

        private static String readNullTermString(BinaryReader br)
        {
            StringBuilder sb = new StringBuilder();
            char c = ' ';
            while (br.BaseStream.Position < br.BaseStream.Length && (c = (char)br.ReadByte()) != 0)
            {
                sb.Append(c);
            }
            return sb.ToString();
        }
        
        public ObsidianArchiveFile(String filePath)
        {
            Console.WriteLine("Attempting to open " + filePath + " for analysis.");

            this.filePath = filePath;
            br = new BinaryReader(new FileStream(filePath, FileMode.Open));

            String magic = new String(br.ReadChars(4));

            if (!magic.Equals("OAF!"))
                throw new InvalidDataException("This does not appear to be an Obsidian Archive File (*.OAF).");

            br.ReadBytes(8);

            Int64 fileListPosition = br.ReadInt64();
            Console.WriteLine("File list should be found at offset " + fileListPosition);

            Int32 fileCount = br.ReadInt32();
            Console.WriteLine("Archive claims to house " + fileCount + " files.");

            Int64 position = br.BaseStream.Position;
                      
            br.BaseStream.Seek(fileListPosition, SeekOrigin.Begin);

            while(br.BaseStream.CanRead && br.BaseStream.Position < br.BaseStream.Length)
            {
                String fileName = readNullTermString(br);
                fileList.Add(new OAFEntry(fileName));
            }

            Console.WriteLine("File list complete.");

            br.BaseStream.Seek(position, SeekOrigin.Begin);
            for (int i = 0; i < fileCount; i++)
            {
                fileList[i].magic = br.ReadInt32();
                fileList[i].dataOffset = br.ReadInt32();
                fileList[i].compressed = (br.ReadInt32() == 0x10) ? true : false;
                fileList[i].uncompressedSize = br.ReadInt32();
                fileList[i].compressedSize = br.ReadInt32();
            }

            Console.WriteLine("File records built.");
            

        }

        
    }

    public class OAFEntry
    {
        public String name;
        public Int64 dataOffset;
        public bool compressed;
        public Int32 uncompressedSize;
        public Int32 compressedSize;
        public Int32 magic;

        public OAFEntry(String nName)
        {
            name = nName;
        }


        public override string  ToString()
        {
 	        return name;
        }
        
    }

}
