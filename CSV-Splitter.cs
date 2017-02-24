using MySql.Data.MySqlClient;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Configuration;
using System.Data;

namespace EasyQuoteCsvSplitter
{
    class Program
    {

        private String masterRulesFile = ConfigurationManager.AppSettings.Get("masterRulesPath");
        private String sourceFolder = ConfigurationManager.AppSettings.Get("csvSourcePath");
        private String destFolder = ConfigurationManager.AppSettings.Get("csvDestinationPath");

        private String sherburnAll = ConfigurationManager.AppSettings.Get("csvSherburnAllPath");
        private String sherburnAuto = ConfigurationManager.AppSettings.Get("csvSherburnAutoPath");
        private String sherburnStema = ConfigurationManager.AppSettings.Get("csvSherburnStemaPath");
        private String sherburnPanel = ConfigurationManager.AppSettings.Get("csvSherburnPanelPath");

        private String connectionString = ConfigurationManager.ConnectionStrings["EasyQuote"].ConnectionString;
        private Dictionary<string, Dictionary<string, string>> products = new Dictionary<string, Dictionary<string, string>>();
        private int ordersSplit = 0;

        static void Main(string[] args)
        {

            Program myProg = new Program();

            myProg.ParseMasterRules();
            myProg.ClearDirectories();
            myProg.CopyFilesFromSourceDirectory();
            myProg.ProcessDestinationDirectory();

            Console.ReadLine();
           
        }

        private void ClearDirectories()
        {

            DeleteFilesInDirectory(new DirectoryInfo(destFolder));
            DeleteFilesInDirectory(new DirectoryInfo(sherburnAll));
            DeleteFilesInDirectory(new DirectoryInfo(sherburnAuto));
            DeleteFilesInDirectory(new DirectoryInfo(sherburnStema));

        }

        private static void DeleteFilesInDirectory(DirectoryInfo destDirectory) {


            foreach (FileInfo file in destDirectory.GetFiles())
            {

                file.Delete();

            }

        }

        private void ParseMasterRules(){

            String[] lines = File.ReadAllLines(masterRulesFile);

            int linesLen = lines.Length;
           
            Char splitChar = ';';

            List<string> fields = ProcessFileFields(lines[0], splitChar);
            
            for (int i = 1; i < linesLen - 1; i++)
            {

                String line = lines[i];

                String[] lineValues = line.Split(splitChar);
                Dictionary<string, string> product = new Dictionary<string, string>();

                for (int x = 0; x < lineValues.Length; x++)
                {

                    String field = fields[x];
                    String value = RemoveQuotes(lineValues[x]);
                    //if there is no value in the 1st column, skip the entire row.
                    if (lineValues[0].Trim().Length == 0) { continue; }
                    
                    product.Add(field, value);

                }

                if (product.Count == 0) { continue; }
                
                products.Add(product["pd_code"], product);

            }

        }

        public void CopyFilesFromSourceDirectory() {

            Console.WriteLine("Copying files for processing...\r\n----------Source Folder: " + sourceFolder + "\r\n----------Dest Folder: " + destFolder);
            
            DirectoryInfo sourceDirectory = new DirectoryInfo(sourceFolder);
            
            foreach (FileInfo file in sourceDirectory.GetFiles()) {
                
                String fileDest = destFolder + "\\" + file.Name;
                file.CopyTo(fileDest, true);

            }
          
        }

        public void ProcessDestinationDirectory() {

            Console.WriteLine("Processing Files...");

            DirectoryInfo destDirectory = new DirectoryInfo(destFolder);

            foreach (FileInfo file in destDirectory.GetFiles()) {

                String orderNumber = Path.GetFileNameWithoutExtension(file.Name);
                Boolean isSherburnOrder = IsSherburnOrder(orderNumber);
                //skip this order if not a sherburn order
                if (!isSherburnOrder) { continue; }

                Console.WriteLine("Processing order: " + orderNumber);
                ProcessFile(file);
              
            }

            Console.WriteLine("We have split " + ordersSplit + " orders!");

        }

        public void ProcessFile(FileInfo file) {

            String[] lines = File.ReadAllLines(file.DirectoryName + "\\" + file.Name);
            
            int linesLen = lines.Length;
            int productLinesLen = (linesLen - 6);

            Dictionary<string, string> header = new Dictionary<string, string>();

            header["start"] = lines[0];
            header["format"] = lines[1];
            header["function"] = lines[2];
            header["param"] = lines[3];
            header["stringDel"] = lines[4];
            header["paramOrder"] = lines[5];
            header["end"] = lines[6];
            
            Char splitChar = ';';

            String paramHeader = header["paramOrder"].Remove(0, 15);
            List<string> fields = ProcessFileFields(paramHeader, splitChar);
            
            Dictionary<int, Dictionary<string, string>> rows = new Dictionary<int, Dictionary<string, string>>();

            for (int i = 7; i < linesLen; i++) {

                String line = lines[i];
                
                String[] lineValues = line.Split(splitChar);
                Dictionary<string, string> columns = new Dictionary<string, string>();

                for (int x = 0; x < lineValues.Length; x++)
                {

                    String field = RemoveQuotes(fields[x]);
                    String value = RemoveQuotes(lineValues[x]);

                    columns.Add(field, value);

                    
                }

                columns.Add("TYPE", "AUTO");
                //add the original line to the list so later when we rebuild the files we can just pop this in.
                columns.Add("ORIGINAL_LINE", line);
                
                rows.Add(i, columns);

            }

            GenerateSplitFiles(file, header, rows);
            
        }
        
        private List<string> ProcessFileFields(String fieldHeadersStr, Char splitChar) {

            List<string> fields = new List<string>();

            String[] fds = fieldHeadersStr.Split(splitChar);

            foreach (String fd in fds) {
                
                fields.Add(RemoveQuotes(fd));

            }

            return fields;

        }

        private void GenerateSplitFiles(FileInfo file, Dictionary<string, string> header, Dictionary<int, Dictionary<string, string>> rows)
        {

            Dictionary<string, string> autoItems = new Dictionary<string, string>();
            Dictionary<string, string> stemaItems = new Dictionary<string, string>();
            Dictionary<string, string> panelItems = new Dictionary<string, string>();

            foreach (KeyValuePair<int, Dictionary<string, string>> row in rows)
            {

                Dictionary<string, string> cell = row.Value;
                
                Boolean hasPriorityCabinetColour = HasPriorityCabinetColour(ref cell);
                
                if (!hasPriorityCabinetColour) {

                    FixProductCodeWithHanding(ref cell);
                    SetupDefaultType(ref cell);
                    IsDryAssembled(ref cell);
                    HasChangesFromOriginalProduct(ref cell);

                }
				//could also be done with switch
                if (cell["TYPE"] == "AUTO") {

                    autoItems.Add(cell["ITEM"], cell["ORIGINAL_LINE"]);

                }

                if (cell["TYPE"] == "STEMA")
                {

                    stemaItems.Add(cell["ITEM"], cell["ORIGINAL_LINE"]);

                }

                if (cell["TYPE"] == "PANEL")
                {

                    panelItems.Add(cell["ITEM"], cell["ORIGINAL_LINE"]);

                }

            }
            
            StringBuilder fileHeader = BuildFileHeader(header);
            StringBuilder autoItemsBuilder = BuildFileItems(autoItems);
            StringBuilder stemaItemsBuilder = BuildFileItems(stemaItems);
            StringBuilder panelItemsBuilder = BuildFileItems(panelItems);

            StringBuilder sBuilder = new StringBuilder();
            StreamWriter csvFile;

            if (autoItems.Count > 0 && stemaItems.Count > 0) {

                ordersSplit++;

            }

            if (autoItems.Count > 0) {

                sBuilder.Clear();
                sBuilder.Append(fileHeader);
                sBuilder.Append(autoItemsBuilder);

                csvFile = new StreamWriter(@sherburnAuto + Path.GetFileNameWithoutExtension(file.Name) + "_auto.csv");
                csvFile.Write(sBuilder);
                csvFile.Close();

            }

            if (stemaItems.Count > 0) {

                sBuilder.Clear();
                sBuilder.Append(fileHeader);
                sBuilder.Append(stemaItemsBuilder);

                csvFile = new StreamWriter(@sherburnStema + Path.GetFileNameWithoutExtension(file.Name) + "_stema.csv");
                csvFile.Write(sBuilder);
                csvFile.Close();

            }

            if (panelItems.Count > 0) {

                sBuilder.Clear();
                sBuilder.Append(fileHeader);
                sBuilder.Append(panelItemsBuilder);

                csvFile = new StreamWriter(@sherburnPanel + Path.GetFileNameWithoutExtension(file.Name) + "_panel.csv");
                csvFile.Write(sBuilder);
                csvFile.Close();

            }

            sBuilder.Clear();
            sBuilder.Append(fileHeader);
            sBuilder.Append(autoItemsBuilder);
            sBuilder.Append(stemaItemsBuilder);
            sBuilder.Append(panelItemsBuilder);

            csvFile = new StreamWriter(@sherburnAll + Path.GetFileNameWithoutExtension(file.Name) + "_all.csv");
            csvFile.Write(sBuilder);
            csvFile.Close();

        }
        
        public static void FixProductCodeWithHanding(ref Dictionary<string,string> cell) {

            if (cell["HANDING_DESCRIPTION"] == "LH" && cell["CORPUSID"].EndsWith("L")) {

                cell["CORPUSID"] = cell["CORPUSID"].TrimEnd('L');
                    
            }

            if (cell["HANDING_DESCRIPTION"] == "RH" && cell["CORPUSID"].EndsWith("R"))
            {

                cell["CORPUSID"] = cell["CORPUSID"].TrimEnd('R');

            }

        }

        private void SetupDefaultType(ref Dictionary<string, string> cell)
        {

            if (products.ContainsKey(cell["CORPUSID"])) {

                Dictionary<string, string> attr = products[cell["CORPUSID"]];
               
                if (attr["auto"] == "Y") {

                    cell["TYPE"] = "AUTO";

                }

                if (attr["stema"] == "Y") {

                    cell["TYPE"] = "STEMA";

                }

                if (attr["panel"] == "Y")
                {

                    cell["TYPE"] = "PANEL";

                }

            }

        }

        public static void IsDryAssembled(ref Dictionary<string, string> cell) {

            if (cell["NOGLUE"].Equals(1) && cell["TYPE"] != "PANEL") {

                cell["TYPE"] = "STEMA";

            }

        }

        public void HasChangesFromOriginalProduct(ref Dictionary<string, string> cell)
        {

            if (products.ContainsKey(cell["CORPUSID"]))
            {

                Dictionary<string, string> attr = products[cell["CORPUSID"]];

                //Regex regx = new Regex("~Hx~Wmm");
                //String productDescription = regx.Replace(attr["pd_desc"], attr["pd_height"] + "x" + attr["pd_width"] + "mm");
               
                bool heightCheck = (int.Parse(cell["HEIGHT"]) > 0 && int.Parse(cell["HEIGHT"]) != int.Parse(attr["pd_height"]));
                bool widthCheck = (int.Parse(cell["WIDTH"]) > 0 && int.Parse(cell["WIDTH"]) != int.Parse(attr["pd_width"]));
                bool depthCheck = (int.Parse(cell["DEPTH"]) > 0 && int.Parse(cell["DEPTH"]) != int.Parse(attr["pd_depth"]));
                //bool descriptionCheck = (!productDescription.Trim().Equals(cell["DESCRIPTION"].Trim()));

                if (heightCheck) {

                    Console.WriteLine("Product [" + cell["CORPUSID"] + "] height on item: " + cell["HEIGHT"] + " Products original height: " + attr["pd_height"]);

                }

                if (widthCheck) {

                    Console.WriteLine("Product [" + cell["CORPUSID"] + "] width on item: " + cell["WIDTH"] + " Products original width: " + attr["pd_width"]);

                }

                if (depthCheck) {

                    Console.WriteLine("Product [" + cell["CORPUSID"] + "] depth on item: " + cell["DEPTH"] + " Products original depth: " + attr["pd_depth"]);

                }

                if (heightCheck || widthCheck || depthCheck) {
                    
                    cell["TYPE"] = "STEMA";

                }

            }

        }

        public static Boolean HasPriorityCabinetColour(ref Dictionary<string, string> columns) {

            List<string> colours = new List<string>();
            colours.Add("Cashmere U702");
            colours.Add("Graphite U961");

            if (colours.Contains(columns["CARCASE_COLOUR"])) {

                columns["TYPE"] = "STEMA";

                return true;

            }

            return false;

        }

        public static String RemoveQuotes(String input) {

            Regex regx = new Regex("\"");
            input = regx.Replace(input, "");

            return input;

        }

        public static StringBuilder BuildFileHeader(Dictionary<string, string> header) {

            StringBuilder sBuilder = new StringBuilder();

            foreach (KeyValuePair<string, string> h in header)
            {

                sBuilder.Append(h.Value);
                sBuilder.AppendLine();

            }

            return sBuilder;

        }

        public static StringBuilder BuildFileItems(Dictionary<string, string> items) {

            StringBuilder sBuilder = new StringBuilder();

            foreach (KeyValuePair<string, string> h in items)
            {

                sBuilder.Append(h.Value);
                sBuilder.AppendLine();

            }

            return sBuilder;

        }

        private bool IsSherburnOrder(String orderNumber) {

            using (MySqlConnection connection = new MySqlConnection(connectionString)) {
				//below query coming from very poor software with poor database design
                MySqlCommand cmd = new MySqlCommand("SELECT COUNT(*) AS theCount FROM orders_more WHERE O_REF = @orderNumber AND O_VEH = 'DEL FROM SHERBURN' LIMIT 1", connection);
                cmd.CommandType = CommandType.Text;
                cmd.Parameters.AddWithValue("@orderNumber", orderNumber);
                cmd.Connection.Open();

                var reader = cmd.ExecuteReader();

                if (reader.HasRows) {

                    while (reader.Read()) {

                        int count = reader.GetInt32(reader.GetOrdinal("theCount"));

                        if (count > 0) {

                            return true;

                        }

                    }
                   
                }

                return false;

            }
           
        }

    }
}
