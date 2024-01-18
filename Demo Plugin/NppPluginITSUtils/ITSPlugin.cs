// NPP plugin platform for .Net v0.91.57 by Kasper B. Graversen etc.
using System;
using System.IO;
using System.Text;
using Kbg.NppPluginNET.PluginInfrastructure;
using System.Windows.Forms;
using System.Diagnostics.Eventing.Reader;
using System.Windows.Forms.VisualStyles;
using JSON_Tools.Utils;
using System.Runtime;
using System.Text.RegularExpressions;
using static System.Net.Mime.MediaTypeNames;
using System.Linq.Expressions;
using static ITS.Utils.ITSENums;
using static ITS.Utils.ITSConstants;
using ITS.Utils;
using System.Resources;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using System.Net.NetworkInformation;

namespace Kbg.NppPluginNET
{
    /// <summary>
    /// Integration layer as the demo app uses the pluginfiles as soft-links files.
    /// This is different to normal plugins that would use the project template and get the files directly.
    /// </summary>
    class Main
    {
        static internal void CommandMenuInit() 
        {
            Kbg.Demo.Namespace.Main.CommandMenuInit();
        }

        static internal void PluginCleanUp()
        {
            Kbg.Demo.Namespace.Main.PluginCleanUp();
        }

        static internal void SetToolBarIcon()
        {
            Kbg.Demo.Namespace.Main.SetToolBarIcon();
        }

        public static void OnNotification(ScNotification notification)
        {
        }

        internal static string PluginName { get { return Kbg.Demo.Namespace.Main.PluginName; }}
    }
}

namespace Kbg.Demo.Namespace
{
    class Main
    {
        #region " Fields "
        internal const string PluginName = "ITSPlugin";
        static string iniFilePath = null;
        // general stuff things
        public static JSON_Tools.Utils.Settings settings = new JSON_Tools.Utils.Settings();

        static IScintillaGateway editor = new ScintillaGateway(PluginBase.GetCurrentScintilla());
        static INotepadPPGateway notepad = new NotepadPPGateway();

        struct ITSStatus
        {
            public bool errorOccured;
            public int errorNum;
            public string errorText;
        }


        #endregion

        #region " Startup/CleanUp "

        static internal void CommandMenuInit()
        {
            // Initialization of your plugin commands
            // You should fill your plugins commands here

            //
            // Firstly we get the parameters from your plugin config file (if any)
            //

            // get path of plugin configuration
            StringBuilder sbIniFilePath = new StringBuilder(Win32.MAX_PATH);
            Win32.SendMessage(PluginBase.nppData._nppHandle, (uint)NppMsg.NPPM_GETPLUGINSCONFIGDIR, Win32.MAX_PATH, sbIniFilePath);
            iniFilePath = sbIniFilePath.ToString();

            // if config path doesn't exist, we create it
            if (!Directory.Exists(iniFilePath))
            {
                Directory.CreateDirectory(iniFilePath);
            }

            // make your plugin config file full file path name
            iniFilePath = Path.Combine(iniFilePath, PluginName + ".ini");

            // with function :
            // SetCommand(int index,                            // zero based number to indicate the order of command
            //            string commandName,                   // the command name that you want to see in plugin menu
            //            NppFuncItemDelegate functionPointer,  // the symbol of function (function pointer) associated with this command. The body should be defined below. See Step 4.
            //            ShortcutKey *shortcut,                // optional. Define a shortcut to trigger this command
            //            bool check0nInit                      // optional. Make this menu item be checked visually
            //            );
            PluginBase.SetCommand(0, "Settings", OpenSettings);
            PluginBase.SetCommand(1, "---", null);
            PluginBase.SetCommand(2, "Toggle COBOL Comment", toggleCobolComment);
            PluginBase.SetCommand(3, "---", null);
            PluginBase.SetCommand(4, "View COBOL Proc", loadCOBOLProc);
            PluginBase.SetCommand(5, "View DMS Schema Record", loadDMSSchemaRecord);
            PluginBase.SetCommand(5, "View Program/Element from Lcl Workspace.", loadEltFromWorkspace);
            PluginBase.SetCommand(5, "View Program/Element from Env SRC file.", loadEltFromSRCFile);
        }

        static internal void SetToolBarIcon()
        { }
        public static void OnNotification(ScNotification notification)
        { }
        static internal void PluginCleanUp()
        { }
        #endregion

        #region " Menu functions "

        //  Display the settings dialog.
        static void OpenSettings()
        {
            settings.ShowDialog("Settings for NY ITS NPP Plugin");
        }

        /*
        * This function iterates through the lines selected and toggle the 
        * comment character in column.  If the line length is <= 6 the 
        * line is skipped. If the character at pos 7 is a space then
        * set asterisk and vv.
        */
        static void toggleCobolComment()
        {
            // Get selection start and end positions.
            // If selStr and selEnd are equal then there is no selection,
            // just process the line the caret is on.
            var strSel = editor.GetSelectionStart();
            var endSel = editor.GetSelectionEnd();

            // Get the line numbers associated with the start and end positions.
            // The strLine and endLine may be the same line.
            var strLine = editor.LineFromPosition(strSel);
            var endLine = editor.LineFromPosition(endSel);

            string lineText = "";
            char[] lineTextAsChars;

            int lineLen;

            int strPos;
            int endPos;

            // Loop through each line and toggle the comment char "*"
            // in column 7.  Skip lines that are shorter than 7 characters.
            for (var lineNum = strLine; lineNum <= endLine; lineNum++)
            {
                // Calculate line size by getting the line start and end positions
                strPos = editor.PositionFromLine(lineNum);
                endPos = editor.GetLineEndPosition(lineNum);

                // Get the current line length
                lineLen = endPos - strPos + 1;

                // Skip lines that are empty.
                if (lineLen <= 6)
                    continue;

                // Get line text and convert to char array
                editor.SetTargetRange(strPos, endPos);
                lineText = editor.GetTargetText();
                lineTextAsChars = lineText.ToCharArray();

                // Toggle comment character.
                if (lineTextAsChars[6] == '*')
                    lineTextAsChars[6] = ' ';
                else if (lineTextAsChars[6] == ' ')
                    lineTextAsChars[6] = '*';
                else
                    // The character in column 6 is not a space or asterisk.
                    // Just skip this line. 
                    continue;

                // Convert char array back to string 
                // and update line in editor. 
                lineText = new string(lineTextAsChars);
                editor.ReplaceTarget(lineText.Length, lineText);
            }

            return;
        }

        static void loadEltFromWorkspace() {
            ITSStatus status;

            string fileName = settings.WorkSpaceSRCFile;
            status = validateFileName(fileName);
            if (status.errorOccured) {
                showError(status.errorText);
                return;
            }

            string elementName = getSelectedText();
            status = validateElementName(elementName);
            if (status.errorOccured) {
                showError(status.errorText);
                return;
            }

            status = loadEltFrmFile(fileName, elementName);
            if (status.errorOccured) {
                showError(status.errorText);
                return;
            }

            if (!status.errorOccured) {
                // Set to Tab Color 2
                Win32.SendMessage(PluginBase.nppData._nppHandle, (uint)NppMsg.NPPM_MENUCOMMAND, 0, NppMenuCmd.IDM_VIEW_TAB_COLOUR_1);
            }
        }

        static void loadEltFromSRCFile() {

            ITSStatus status = new ITSStatus();
            string fileName;

            switch (settings.workingEnvt)
            {
                case ENVIRONMENT.Development:
                    fileName = settings.DEVSRCFile;
                    break;
                case ENVIRONMENT.UserTest:
                    fileName = settings.TSTSRCFile;
                    break;
                case ENVIRONMENT.Pseudo:
                    fileName = settings.PSDSRCFile;
                    break;
                default:
                    status.errorOccured = true;
                    // Environment not specified.
                    status.errorNum = (int) ERRORS.ITSERR007;
                    status.errorText = errorText[status.errorNum];
                    return;
            }
            status = validateFileName(fileName);
            if (status.errorOccured) {
                showError(status.errorText);
                return;
            }

            string elementName = getSelectedText();
            status = validateElementName(elementName);
            if (status.errorOccured) {
                showError(status.errorText);
                return;
            }

            status = loadEltFrmFile(fileName, elementName);
            if (status.errorOccured) {
                showError(status.errorText);
                return;
            }


            if (!status.errorOccured) {
                // Set to Tab Color 2
                Win32.SendMessage(PluginBase.nppData._nppHandle, (uint)NppMsg.NPPM_MENUCOMMAND, 0, NppMenuCmd.IDM_VIEW_TAB_COLOUR_1);
            }

        }

            static void loadCOBOLProc()
        {
            ITSStatus status = new ITSStatus();

            string procFile1;
            string procFile2;

            string procFiles = "";   // Used for error reporting.. 

            switch (settings.workingEnvt)
            {
                case ENVIRONMENT.Development:
                    procFile1 = settings.DEVproc1File;
                    procFile2 = settings.DEVproc2File;
                    break;
                case ENVIRONMENT.UserTest:
                    procFile1 = settings.TSTproc1File;
                    procFile2 = settings.TSTproc2File;
                    break;
                case ENVIRONMENT.Pseudo:
                    procFile1 = settings.PSDproc1File;
                    procFile2 = settings.PSDproc2File;
                    break;
                default:
                    status.errorOccured = true;
                    status.errorText = errorText[(int)ERRORS.ITSERR008];
                    showError(status.errorText);
                    return;
            }
            string elementName = getSelectedText();
            status = validateElementName(elementName);
            if (status.errorOccured) {
                showError(status.errorText);
                return;
            }

            if ((procFile1 == null || procFile1.Length == 0)  &&
                (procFile2 == null || procFile2.Length == 0)) {
                status.errorOccured = true;
                status.errorText = errorText[(int) ERRORS.ITSERR009];
                showError(status.errorText);
                return;
            }

            if (procFile1 != null && procFile1.Length > 0) {
                procFiles = procFile1; // procFiles - needed for an error message
                status = validateFileName(procFile1);
                if (status.errorOccured) {
                    showError(status.errorText);
                    return;
                }

                status = loadEltFrmFile(procFile1, elementName);
                if (status.errorOccured) {
                    if (status.errorNum != (int) ERRORS.ITSERR010) {
                        showError(status.errorText);
                        return;
                    }
                }
            }

            if (procFile2 != null && procFile2.Length > 0) {
                // procFiles is needed for error handling
                if (procFiles.Length == 0)
                    procFiles = procFile2;
                else
                    procFiles += ", " + procFile2;

                status = validateFileName(procFile2);
                if (status.errorOccured)
                {
                    showError(status.errorText);
                    return;
                }

                status = loadEltFrmFile(procFile2, elementName);
                if (status.errorOccured) {
                    if (status.errorNum != (int) ERRORS.ITSERR010) {
                        showError(status.errorText);
                        return;
                    }
                }
            }

            // The only error that will come here is Not Found.. 
            if (status.errorOccured) {
                status.errorText = string.Format(errorText[(int)ERRORS.ITSERR010], procFiles);
                showError(status.errorText);
                return;
            }

            if (!status.errorOccured)
            {
                // Set to Tab Color 2
                Win32.SendMessage(PluginBase.nppData._nppHandle, (uint)NppMsg.NPPM_MENUCOMMAND, 0, NppMenuCmd.IDM_VIEW_TAB_COLOUR_2);
            }


            return;
        }

        static ITSStatus loadEltFrmFile(string pFileName, string pElementName) {
            ITSStatus status = new ITSStatus();

            string path = getFilePath(pFileName, pElementName);

            if (!File.Exists(path)) {
                status.errorOccured = true;
                status.errorNum = (int) ERRORS.ITSERR012;
                status.errorText = string.Format(errorText[status.errorNum], pFileName + "." + pElementName, path);
                return status;
            }

            // Open the proc file in a new window.
            notepad.OpenFile(path);

            // Set Proc Language to COBOL
            notepad.SetCurrentLanguage(LangType.L_COBOL);

            // Set ReadOnly on the proc just loaded.
            Win32.SendMessage(PluginBase.nppData._nppHandle, (uint)NppMsg.NPPM_MENUCOMMAND, 0, NppMenuCmd.IDM_EDIT_SETREADONLY);

            return status;
        }



         /*****************************************************************************************
         * Load DMS Record                                                                       *
         * The user selects the record name in the program and then initiates the Load DMS       *
         * record plugin entry point (this function).                                            *
         *                                                                                       *
         * The following steps are performed:                                                    *
         *   1. Get the text selected. This text represents the DMS record name.                 *
         *   2. The record name is validated. It must be from 1 to 60 characters                 *
         *   3. Get the Unisys schema file name from settings (user config).                     *
         *      o - Get schema file name using the selected environment.                         *
         *      o - The file name consists of a file qualifier file name and element name.       *
         *          Example: qualifier*program-file.element-name[/ version]                      *
         *          i.e. DA0*ABS-WMSLDM.S$PROC/WMS-LDMIP-0                                       *
         *   4. The file name is validated.                                                      *
         *      o - Check format. (No edits are performed when the settings are added.)          *
         *                                                                                       *
         *                                                                                       *
         *                                                                                       *
         *                                                                                       *
         *                                                                                       *
         *                                                                                       *
         *****************************************************************************************/
        static void loadDMSSchemaRecord()
        {
            ITSStatus status = new ITSStatus();

            string recordName = getSelectedText();

            status = validateRecordName(recordName);
            if (status.errorOccured) {
                status.errorOccured = true;
                showError(status.errorText);
                return;
            }

            // Get the schema file name based on the environment selected 
            // in the settings. 
            string schemaFile1 = "";
            switch (settings.workingEnvt)
            {
                case ENVIRONMENT.Development:
                    schemaFile1 = settings.DEVschemaFile;
                    break;
                case ENVIRONMENT.UserTest:
                    schemaFile1 = settings.TSTschemaFile;
                    break;
                case ENVIRONMENT.Pseudo:
                    schemaFile1 = settings.PSDschemaFile;
                    break;
                default:
                    status.errorOccured = true;
                    status.errorText = errorText[(int)ERRORS.ITSERR008];
                    showError(status.errorText);
                    return;
            }

            if (schemaFile1 == null || schemaFile1.Length == 0)
            {
                status.errorOccured = true;
                status.errorNum = (int)ERRORS.ITSERR004;
                status.errorText = errorText[status.errorNum];
                    showError(status.errorText);
                return;
            }

            // Seperate the qualifier and file name from the element name (and version)
            // This is done by spliting the file name on the period. If there are no
            // periods or more than one period and error message is displayed.
            string[] fileName = schemaFile1.Split('.');
            if (fileName.Length != 2) {
                status.errorOccured = true;
                status.errorNum = (int) ERRORS.ITSERR013;
                status.errorText = string.Format(errorText[status.errorNum], schemaFile1);
                showError(status.errorText);
                return;
            }

            string path = getFilePath(fileName[0], fileName[1]);

            if (!File.Exists(path))
            {
                status.errorOccured = true;
                status.errorNum = 0;
                status.errorText = string.Format(errorText[(int)ERRORS.ITSERR012], fileName[0] + "." + fileName[1], path);
                return;
            }

            // Schema file exists.  Open and Read.

            Int32 BufferSize = 512;
            using (var fileStream = File.OpenRead(path))
            using (var streamReader = new StreamReader(fileStream, Encoding.UTF8, true, BufferSize))
            {
                string pattern = @"^ *01 *([a-zA-Z_0-9-]+)\. *$";
                Regex r = new Regex(pattern, RegexOptions.IgnoreCase);

                bool editorOpen = false;
                bool eof = false;

                string line;
                bool recFound = false;
                while (!eof)
                {
                    line = streamReader.ReadLine();
                    if (line == null) {
                        eof = true;
                        continue;
                    }

                    // I'll get dinged for this.. 
                    if (recFound)
                    {
                        if (line.Trim().Equals("END"))
                        {
                            eof = true;

                            // Set ReadOnly on the proc just loaded.
                            Win32.SendMessage(PluginBase.nppData._nppHandle, (uint)NppMsg.NPPM_MENUCOMMAND, 0, NppMenuCmd.IDM_EDIT_SETREADONLY);

                            continue;
                        }
                        writeNPPLine(line);
                    }

                    // This code is only executed when the start line has not been found.

                    Match m = r.Match(line);
                    if (!m.Success) {
                        continue;
                    }

                    Group g = m.Groups[1];
                    CaptureCollection cc = g.Captures;
                    Capture c = cc[0];
                    
                    if (!c.ToString().Equals(recordName)) {
                        continue;
                    }

                    // *** DMS RECORD FOUND ***

                    // Start of DMS record found.  Create a new file in NPP, set language to COBOL
                    // and set the tab color to Orange. 
                    recFound = true;
                    if (!editorOpen) {
                        notepad.FileNew();
                        editorOpen = true;
                        // Set Proc Language to COBOL
                        notepad.SetCurrentLanguage(LangType.L_COBOL);
                        // Set to Tab Color 4
                        Win32.SendMessage(PluginBase.nppData._nppHandle, (uint)NppMsg.NPPM_MENUCOMMAND, 0, NppMenuCmd.IDM_VIEW_TAB_COLOUR_3);
                    }

                    writeNPPLine(line);
                }
                
                // Specified record name was not found in the schema file. 
                if (!recFound) {
                    MessageBox.Show("Err LPC005 - DMS Record: " + recordName + " not found at path: " + path);
                    return;
                }
            }
        }

        static string getSelectedText()
        {
            // Get selection start and end positions.
            // If selStr and selEnd are equal then there is no selection,
            // just process the line the caret is on.
            var strSel = editor.GetSelectionStart();
            var endSel = editor.GetSelectionEnd();

            // Get line text and convert to char array
            editor.SetTargetRange(strSel, endSel);
            return editor.GetTargetText().Trim();
        }

        static ITSStatus validateFileName(string pFileName)
        {
            ITSStatus status = new ITSStatus();

            if (pFileName == null || pFileName.Length == 0)
            {
                status.errorOccured = true;
                status.errorNum = (int)ERRORS.ITSERR007;
                status.errorText = string.Format(errorText[status.errorNum], pFileName);
                return status;
            }

            string[] fileParts = pFileName.Split('*');
            int numAsterisks = fileParts.Length - 1;
            if (numAsterisks == 0)
            {
                status.errorOccured = true;
                status.errorNum = (int)ERRORS.ITSERR005;
                status.errorText = string.Format(errorText[status.errorNum], pFileName);
                return status;
            }
            else if (numAsterisks > 1)
            {
                status.errorOccured = true;
                status.errorNum = (int)ERRORS.ITSERR006;
                status.errorText = string.Format(errorText[status.errorNum], pFileName);
                return status;
            }

            status.errorOccured = false;
            return status;

        }

        static ITSStatus validateElementName(string pElementName)
        {
            ITSStatus status = new ITSStatus();

            if (pElementName == null || pElementName.Length == 0)
            {
                status.errorOccured = true;
                status.errorNum = (int)ERRORS.ITSERR011;
                status.errorText = string.Format(errorText[status.errorNum], pElementName);
                return status;
            }

            status.errorOccured = false;
            return status;

        }

        static ITSStatus validateRecordName(string pRecordName)
        {
            ITSStatus status = new ITSStatus();

            if (pRecordName == null || pRecordName.Length == 0)
            {
                status.errorOccured = true;
                status.errorNum = (int)ERRORS.ITSERR011;
                status.errorText = string.Format(errorText[status.errorNum], pRecordName);
                return status;
            }

            status.errorOccured = false;
            return status;

        }



         static string getFilePath(string programFileName, string elementName)
        {
            string path = "";

            // Replace * in program file name with a back slash
            string pgmFileFmt = programFileName.Replace('*', '\\');

            // Replace a forward slash with a "." to indicate extension (version in Unisys)
            string eltFileFmt = elementName.Replace('/', '.');

            // Build path to file/element by adding the mapped drive letter.
            char driveLetter = (char)('A' + settings.mappedDrive);
            path = (driveLetter + ":") + "\\" + pgmFileFmt + "\\" + eltFileFmt;

            return path;
        }

        static void showError(string pErrorTxt) {
            MessageBox.Show(pErrorTxt);
        }

        static void writeNPPLine(string pLine) {

            int lineNum = editor.GetCurrentLineNumber();

            editor.NewLine();

            int strPos = editor.PositionFromLine(lineNum);
            int endPos = editor.GetLineEndPosition(lineNum);

            editor.SetTargetRange(strPos, endPos);
            editor.ReplaceTarget(pLine.Length, pLine);
        }


        static void updateLine(IScintillaGateway pEditor)
        {

            int strPos = pEditor.PositionFromLine(0);
            int endPos = pEditor.GetLineEndPosition(0);

            string lineText = "This line has been updated";
            pEditor.SetTargetRange(strPos, endPos);
            pEditor.ReplaceTarget(lineText.Length, lineText);
            return;
        }

        #endregion
    }

}   
