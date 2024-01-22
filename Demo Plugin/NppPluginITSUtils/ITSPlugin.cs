// NPP plugin platform for .Net v0.91.57 by Kasper B. Graversen etc.
using System;
using System.IO;
using System.Text;
using Kbg.NppPluginNET.PluginInfrastructure;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using static ITS.Utils.ITSENums;
using static ITS.Utils.ITSConstants;
using static System.Windows.Forms.LinkLabel;

namespace Kbg.NppPluginNET
{
    /// <summary>
    /// AIA Technologies - 2024
    /// 
    /// This plugin is designed to work in shops running Unisys 2200 
    /// ACOB or UCOB programs.
    /// 
    /// This plugin includes the following features:
    ///   - Togggle COBOL comment
    ///   - Load Proc definitions from configured ACOB proc filess, 
    ///     UCOM proc file, or system proc file (for procs that use DPS, etc).
    ///   - Load DMS record descriptions.   
    ///   - Load program (elements) from applications or personal workspace 
    ///     SRC files.
    /// 
    /// The plug was built using the templates and sample code here:
    /// https://github.com/kbilsted/NotepadPlusPlusPluginPack.Net
    ///   - This code was used as the template
    ///   
    /// https://github.com/molsonkiko/JsonToolsNppPlugin
    ///   - This code was used to get an example of how to display a dialog.
    /// 
    /// 
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
        static string SCREEN_PREFIX = "SCREEN-";
        static string COBP_VERSION = "COBP";
        static char FORWARD_SLASH = '/';
        static char HASH_CHAR = '#';
        static char EQUALS_CHAR = '=';
        static char ASTERISK_CHAR = '*';
        static char PERIOD = '.';

        // general stuff things
        public static JSON_Tools.Utils.Settings settings = new JSON_Tools.Utils.Settings();

        static IScintillaGateway editor = new ScintillaGateway(PluginBase.GetCurrentScintilla());
        static INotepadPPGateway notepad = new NotepadPPGateway();

        // Used on calls to worker functions. 
        struct ITSStatus
        {
            public bool errorOccured;
            public int errorNum;
            public string errorText;
        }

        static bool SET_READ_ONLY = true;
        static bool DO_NOT_SET_READ_ONLY = !SET_READ_ONLY;

        static string ALIAS_FILE_NAME = "aliasEltName.ini";

        #endregion

        #region " Startup/CleanUp "

        /*
         * The function is what contriols what is displayed when the user
         * selects Plugins | ITSPlugin 
         */
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
            PluginBase.SetCommand(0, "Edit Alias File", EditAliasFile);
            PluginBase.SetCommand(1, "---", null);
            PluginBase.SetCommand(2, "Toggle COBOL Comment", toggleCobolComment, new ShortcutKey(true, true, true, Keys.C));
            PluginBase.SetCommand(3, "---", null);
            PluginBase.SetCommand(4, "View ACOB COBOL Proc",   loadACOBCOBOLProc, new ShortcutKey(true, true, true, Keys.A));
            PluginBase.SetCommand(5, "View UCOB COBOL Proc",   loadUCOBCOBOLProc, new ShortcutKey(true, true, true, Keys.U));
            PluginBase.SetCommand(6, "View System Proc (DPS)", loadSystemProc, new ShortcutKey(true, true, true, Keys.S));
            PluginBase.SetCommand(8, "---", null);
            PluginBase.SetCommand(7, "View DMS Schema Record", loadDMSSchemaRecord, new ShortcutKey(true, true, true, Keys.D));
            PluginBase.SetCommand(8, "---", null);
            PluginBase.SetCommand(9, "View Program/Element from Lcl Workspace.", loadEltFromWorkspace, new ShortcutKey(true, true, true, Keys.W));
            PluginBase.SetCommand(10, "View Program/Element from Env SRC file.", loadEltFromSRCFile, new ShortcutKey(true, true, true, Keys.F));
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
        //  Allows user to configure required file names. 
        static void OpenSettings() {
            settings.ShowDialog("Settings for NY ITS NPP Plugin");
        }

        //  Display the settings dialog.
        //  Allows user to configure required file names. 
        static void EditAliasFile() {

            string path = notepad.GetNppPath();
            path = path + ALIAS_FILE_NAME;

            if (!File.Exists(path)) {

                try {
                    // Create the file, or overwrite if the file exists.
                    using (FileStream fs = File.Create(path)) {
                        byte[] info = new UTF8Encoding(true).GetBytes("# Enter Key = Value pairs.");
                        // Add some information to the file.
                        fs.Write(info, 0, info.Length);
                        info = new UTF8Encoding(true).GetBytes("# key = selected text, value = element name (with /version.");
                        // Add some more information to the file.
                        fs.Write(info, 0, info.Length);
                    }
                }
                catch (Exception ex) {
                    Console.WriteLine(ex.ToString());
                }   
            }

            notepad.OpenFile(path);
        }

        /*
        * This function iterates through the lines selected and toggle the 
        * comment character in column 7.  If the line length is <= 6 the 
        * line is skipped. If the character at column 7 is a space or 
        * asterisk then toggle the value. If the character is neither a 
        * space or asterisk then skip line.
        */
        static void toggleCobolComment() {
            // Get selection start and end positions.
            // If selStr and selEnd are equal then there is no selection,
            // just process the line the caret is on.
            var strSel = editor.GetSelectionStart();
            var endSel = editor.GetSelectionEnd();

            // Get the line numbers associated with the start and end positions.
            // The strLine and endLine may be the same line.
            var strLine = editor.LineFromPosition(strSel);
            var endLine = editor.LineFromPosition(endSel);

            string lineText;
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
                if (lineTextAsChars[6] == ASTERISK_CHAR)
                    lineTextAsChars[6] = ' ';
                else if (lineTextAsChars[6] == ' ')
                    lineTextAsChars[6] = ASTERISK_CHAR;
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

        /*
         *  This is used to load a program (or other) element into notepad++
         * 
         */
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

            status = loadEltFrmFile(fileName, elementName, DO_NOT_SET_READ_ONLY);
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

        static void loadACOBCOBOLProc() {
            ITSStatus status = new ITSStatus();

            string procFile1;
            string procFile2;

            switch (settings.workingEnvt)
            {
                case ENVIRONMENT.Development:
                    procFile1 = settings.DEVACOBproc1File;
                    procFile2 = settings.DEVACOBproc2File;
                    break;
                case ENVIRONMENT.UserTest:
                    procFile1 = settings.TSTACOBproc1File;
                    procFile2 = settings.TSTACOBproc2File;
                    break;
                case ENVIRONMENT.Pseudo:
                    procFile1 = settings.PSDACOBproc1File;
                    procFile2 = settings.PSDACOBproc2File;
                    break;
                default:
                    status.errorOccured = true;
                    status.errorNum = (int) ERRORS.ITSERR008;
                    status.errorText = errorText[status.errorNum];
                    showError(status.errorText);
                    return;
            }

            loadCOBOLProc(PROC_TYPE.ACOB_Proc, procFile1, procFile2);
        }

        static void loadUCOBCOBOLProc() {

            ITSStatus status = new ITSStatus();

            string procFile1 = "";
            string procFile2 = "";

            switch (settings.workingEnvt)
            {
                case ENVIRONMENT.Development:
                    procFile1 = settings.DEVUCOBprocFile;
                    break;
                case ENVIRONMENT.UserTest:
                    procFile1 = settings.TSTUCOBprocFile;
                    break;
                case ENVIRONMENT.Pseudo:
                    procFile1 = settings.PSDUCOBprocFile;
                    break;
                default:
                    status.errorOccured = true;
                    status.errorNum = (int)ERRORS.ITSERR008;
                    status.errorText = errorText[status.errorNum];
                    showError(status.errorText);
                    return;
            }

            loadCOBOLProc(PROC_TYPE.UCOB_Proc, procFile1, procFile2);
        }

        static void loadSystemProc()
        {

            // ITSStatus status = new ITSStatus(); not needed just now.. 

            string procFile1 = "";
            string procFile2 = "";

            procFile1 = settings.systemProcFile;

            loadCOBOLProc(PROC_TYPE.System_Proc, procFile1, procFile2);
        }


        /*
         * The function reads proc elements from several configured files.
         * 
         * 
         * 
         */

        static void loadCOBOLProc(PROC_TYPE procType, string procFile1, string procFile2) {
            ITSStatus status = new ITSStatus();

            string procFiles = "";   // Used for error reporting.. 


            // Get the selected text.
            string elementName = getSelectedText();
            status = validateElementName(elementName);
            if (status.errorOccured) {
                showError(status.errorText);
                return;
            }

            // Check if requesting a DPS screen.
            if (procType == PROC_TYPE.ACOB_Proc || procType == PROC_TYPE.UCOB_Proc) {
                if (elementName.StartsWith(SCREEN_PREFIX) && (!elementName.Contains(FORWARD_SLASH.ToString()))) {
                    string[] elementNamePart = elementName.Split('-');
                    if (elementName.Length > 2) {
                        elementName = elementNamePart[0] + "-" + elementNamePart[elementNamePart.Length - 1] + FORWARD_SLASH + COBP_VERSION;
                    }
                }
            }

            // DPS Procs can have a version name like /COBP 
            if (procType == PROC_TYPE.System_Proc) {
                // If there is no fwd slash then concatenate "/COBP" to elemenent name. 
                if (!elementName.Contains(FORWARD_SLASH.ToString())) {
                    elementName = elementName + FORWARD_SLASH + COBP_VERSION;
                }
            }

            // If both procfiles are empty then show error and exit. 
            if ((procFile1 == null || procFile1.Length == 0)  &&
                (procFile2 == null || procFile2.Length == 0)) {
                status.errorOccured = true;

                // SHow error associated with the type of proc requested.
                switch (procType)
                {
                    case PROC_TYPE.ACOB_Proc:
                        status.errorNum = (int)ERRORS.ITSERR009;
                        break;
                    case PROC_TYPE.UCOB_Proc:
                        status.errorNum = (int)ERRORS.ITSERR014;
                        break;
                    case PROC_TYPE.System_Proc:
                        status.errorNum = (int)ERRORS.ITSERR015;
                        break;
                    default:
                        break;
                }

                status.errorText = errorText[(int) status.errorNum];
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
                if (status.errorOccured) {
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
                status.errorNum = (int)ERRORS.ITSERR010;
                status.errorText = string.Format(errorText[status.errorNum], procFiles);
                showError(status.errorText);
                return; }
            else {
                // Set Tab Color 2
                Win32.SendMessage(PluginBase.nppData._nppHandle, (uint)NppMsg.NPPM_MENUCOMMAND, 0, NppMenuCmd.IDM_VIEW_TAB_COLOUR_2);
            }

            return;
        }

        /* This opens a new NPP window with the specified file pFileName.pElementName
         * pFileName will include the file qualifier and file name. i.e. qual*file.
         * The element name may or may not have a version field.  elt or elt/xxxxxx
         * 
         * The read only attribute will only be set when setRadOnly == true (default).
         */
        static ITSStatus loadEltFrmFile(string pFileName, string pElementName, bool setReadOnly = true) {
            ITSStatus status = new ITSStatus();

            string elementName = pElementName.Trim();

            string path = getFilePath(pFileName, elementName);

            if (!File.Exists(path)) {
                // Remove version (i.e. Sys Proc routine adds /COBP)
                int fwdSlashIdx = elementName.IndexOf(FORWARD_SLASH); // Remove version
                if (fwdSlashIdx > 0)
                    elementName = elementName.Substring(0, fwdSlashIdx);
                string alias = getAliasEltName(elementName);
                if (alias != null && alias.Length > 0) {
                    elementName = alias.Replace(FORWARD_SLASH, PERIOD).Trim();
                    path = getFilePath(pFileName, elementName);
                    if (!File.Exists(path)) {
                        status.errorOccured = true;
                        status.errorNum = (int)ERRORS.ITSERR012;
                        status.errorText = string.Format(errorText[status.errorNum], pFileName + PERIOD + elementName, path);
                        return status;
                    }
                } else {
                    status.errorOccured = true;
                    status.errorNum = (int)ERRORS.ITSERR012;
                    status.errorText = string.Format(errorText[status.errorNum], pFileName + PERIOD + elementName, path);
                    return status;
                }
            }

            // Open the element in a new window.
            notepad.OpenFile(path);


            // Set Language to COBOL
            notepad.SetCurrentLanguage(LangType.L_COBOL);

            // Conditionally set ReadOnly on the element just loaded.
            if (setReadOnly) {
                Win32.SendMessage(PluginBase.nppData._nppHandle, (uint)NppMsg.NPPM_MENUCOMMAND, 0, NppMenuCmd.IDM_EDIT_SETREADONLY);
            }
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
        static void loadDMSSchemaRecord_old()
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

            // Display message if scheama file is not set for selected envt
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
            string[] fileName = schemaFile1.Split(PERIOD);
            if (fileName.Length != 2) {
                status.errorOccured = true;
                status.errorNum = (int) ERRORS.ITSERR013;
                status.errorText = string.Format(errorText[status.errorNum], schemaFile1);
                showError(status.errorText);
                return;
            }

            string path = getFilePath(fileName[0], fileName[1]);

            if (!File.Exists(path)) {
                status.errorOccured = true;
                status.errorNum = (int)ERRORS.ITSERR012;
                status.errorText = string.Format(errorText[status.errorNum], fileName[0] + PERIOD + fileName[1], path);
                showError(status.errorText);
                return;
            }

            // Schema file exists.  Open and Read.

            Int32 BufferSize = 1028;
            using (var fileStream = File.OpenRead(path))
            using (var streamReader = new StreamReader(fileStream, Encoding.UTF8, true, BufferSize))
            {
                string pattern = @"^ *RECORD NAME IS ([a-zA-Z_-]+) *$";
                Regex r = new Regex(pattern, RegexOptions.IgnoreCase);

                bool eof = false;

                string line;
                bool recFound = false;
                // Loop through DMS Schema File
                while (!eof)
                {
                    // Get next line
                    line = streamReader.ReadLine();
                    if (line == null) {
                        eof = true;
                        if (recFound) {
                            Win32.SendMessage(PluginBase.nppData._nppHandle, (uint)NppMsg.NPPM_MENUCOMMAND, 0, NppMenuCmd.IDM_EDIT_SETREADONLY);
                            editor.GotoLine(0);
                            editor.GrabFocus();
                        }
                        continue;
                    }

                    // If the record has been found determine at begining of a new record. 
                    if (recFound) {
                        // Begining of new record?
                        if (r.Match(line).Success) {

                            // Set ReadOnly on the new file just opened.
                            Win32.SendMessage(PluginBase.nppData._nppHandle, (uint)NppMsg.NPPM_MENUCOMMAND, 0, NppMenuCmd.IDM_EDIT_SETREADONLY);
                            editor.GotoLine(0);
                            editor.GrabFocus();
                            eof = true;
                            continue;
                        }
                        else {
                            writeNPPLine(line);
                            continue;
                        }
                    } else {
                        // Record has not been found yet.. 

                        // Is this the begining of the requested record?
                        Match m = r.Match(line);
                        if (!m.Success) {
                            // Not a match... Keep looking.
                            continue;
                        }


                        Group g = m.Groups[1];
                        CaptureCollection cc = g.Captures;
                        Capture c = cc[0];

                        // If it does not match, skip and get next line. 
                        if (!c.ToString().Equals(recordName)) {
                            continue;
                        }

                        // *** DMS RECORD FOUND ***

                        // Start of DMS record found.  Create a new file in NPP, set language to COBOL
                        // and set the tab color to Orange. 
                        recFound = true;
                        notepad.FileNew();
                        // Set Proc Language to COBOL
                        notepad.SetCurrentLanguage(LangType.L_COBOL);
                        // Cursor to home
                        // Set to Tab Color 4
                        Win32.SendMessage(PluginBase.nppData._nppHandle, (uint)NppMsg.NPPM_MENUCOMMAND, 0, NppMenuCmd.IDM_VIEW_TAB_COLOUR_3);

                        writeNPPLine(line);
                    }
                }

                // Specified record name was not found in the schema file. 
                if (!recFound) {
                    status.errorOccured = true;
                    status.errorNum = (int) ERRORS.ITSERR016;
                    status.errorText = string.Format(errorText[status.errorNum], fileName[0] + PERIOD + fileName[1], path);
                    showError(status.errorText);
                    return;
                }
                return;

            }
        }
        static void loadDMSSchemaRecord() {
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
            switch (settings.workingEnvt) {
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

            // Display message if scheama file is not set for selected envt
            if (schemaFile1 == null || schemaFile1.Length == 0) {
                status.errorOccured = true;
                status.errorNum = (int)ERRORS.ITSERR004;
                status.errorText = errorText[status.errorNum];
                showError(status.errorText);
                return;
            }

            // Seperate the qualifier and file name from the element name (and version)
            // This is done by spliting the file name on the period. If there are no
            // periods or more than one period and error message is displayed.
            string[] fileName = schemaFile1.Split(PERIOD);
            if (fileName.Length != 2) {
                status.errorOccured = true;
                status.errorNum = (int)ERRORS.ITSERR013;
                status.errorText = string.Format(errorText[status.errorNum], schemaFile1);
                showError(status.errorText);
                return;
            }

            string path = getFilePath(fileName[0], fileName[1]);

            if (!File.Exists(path)) {
                status.errorOccured = true;
                status.errorNum = (int)ERRORS.ITSERR012;
                status.errorText = string.Format(errorText[status.errorNum], fileName[0] + PERIOD + fileName[1], path);
                showError(status.errorText);
                return;
            }

            // Schema file exists.  Open and Read.

            Int32 BufferSize = 1028;
            using (var fileStream = File.OpenRead(path))
            using (var streamReader = new StreamReader(fileStream, Encoding.UTF8, true, BufferSize)) {
                string pattern = @"^ *RECORD NAME IS ([a-zA-Z_-]+) *$";
                Regex r = new Regex(pattern, RegexOptions.IgnoreCase);

                bool eof = false;

                string line;
                bool recFound = false;
                // Loop through DMS Schema File
                int lineNum = 0;
                while (!eof) {
                    // Get next line
                    line = streamReader.ReadLine();
                    lineNum += 1;
                    if (line == null) {
                        eof = true;
                        continue;
                    }

                    // If the record has been found determine at begining of a new record. 
                    if (!recFound) {
                        // Is this the begining of the requested record?
                        Match m = r.Match(line);
                        if (!m.Success) {
                            // Not a match... Keep looking.
                            continue;
                        }

                        Group g = m.Groups[1];
                        CaptureCollection cc = g.Captures;
                        Capture c = cc[0];

                        // If it does not match, skip and get next line. 
                        if (!c.ToString().Equals(recordName)) {
                            continue;
                        }

                        // *** DMS RECORD FOUND *** 

                        // Start of DMS record found.  Create a new file in NPP, set language to COBOL
                        // and set the tab color to Orange. 
                        recFound = true;
                        eof = true;
                        notepad.OpenFile(path);
                        // Set Proc Language to COBOL
                        notepad.SetCurrentLanguage(LangType.L_COBOL);
                        // Display line number where record found. 
                        editor.SetFirstVisibleLine(lineNum - 1);
                        // Set read only    
                        Win32.SendMessage(PluginBase.nppData._nppHandle, (uint)NppMsg.NPPM_MENUCOMMAND, 0, NppMenuCmd.IDM_EDIT_SETREADONLY);
                        // Set to Tab Color 4
                        Win32.SendMessage(PluginBase.nppData._nppHandle, (uint)NppMsg.NPPM_MENUCOMMAND, 0, NppMenuCmd.IDM_VIEW_TAB_COLOUR_3);
                    }
                }

                // Specified record name was not found in the schema file. 
                if (!recFound) {
                    status.errorOccured = true;
                    status.errorNum = (int)ERRORS.ITSERR016;
                    status.errorText = string.Format(errorText[status.errorNum], fileName[0] + PERIOD + fileName[1], path);
                    showError(status.errorText);
                    return;
                }
                return;

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

            string[] fileParts = pFileName.Split(ASTERISK_CHAR);
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



         static string getFilePath(string programFileName, string elementName) {
            string path = "";

            // Replace * in program file name with a back slash
            string pgmFileFmt = programFileName.Replace(ASTERISK_CHAR, '\\'); 

            // Replace a forward slash with a "." to indicate extension (version in Unisys)
            string eltFileFmt = elementName.Replace(FORWARD_SLASH, PERIOD);

            // Build path to file/element by adding the mapped drive letter.
            char driveLetter = (char)('A' + settings.mappedDrive);
            path = (driveLetter + ":") + "\\" + pgmFileFmt + "\\" + eltFileFmt;

            return path;
        }


        static string getAliasEltName(string pEltName) {

            string alias = null;

            string path = notepad.GetNppPath();
            path = path + ALIAS_FILE_NAME;

            // If there is no alias file then just exit with alias = null
            if (!File.Exists(path)) {
                return alias;
            }

            string eltName = pEltName.Trim().ToLower();

            Int32 BufferSize = 512;

            // Read alias file and search for key = pEltName. 
            // Wehn found return alias.
            try {
                using (var fileStream = File.OpenRead(path))
                using (var streamReader = new StreamReader(fileStream, Encoding.UTF8, true, BufferSize)) {
                    string line;
                    while (true) {
                        line = streamReader.ReadLine();
                        if (line == null) {
                            break;
                        }

                        if (line.Trim().Length == 0 || line.TrimStart().StartsWith(HASH_CHAR.ToString())) {
                            continue; // skip this line 
                        }

                        line = line.Trim();
                        string[] names = line.Split(EQUALS_CHAR);

                        if (names.Length != 2) {
                            continue;
                        }

                        if (names[0].Trim().ToLower().Equals(eltName)) {
                            alias = names[1].Trim();
                            break;
                        }
                    }
                    return alias;
                }
            } catch (Exception e) {
                MessageBox.Show(e.Message);
                return alias;
            }
        }

    static void showError(string pErrorTxt) {
                    MessageBox.Show(pErrorTxt);
                }

        static void writeNPPLine(string pLine) {

            int lineNum = editor.GetCurrentLineNumber();

            editor.NewLine();

            if (pLine != null && pLine != "") {

                int strPos = editor.PositionFromLine(lineNum);
                int endPos = editor.GetLineEndPosition(lineNum);

                editor.SetTargetRange(strPos, endPos);
                editor.ReplaceTarget(pLine.Length, pLine);
            }
        }


        /*
         * Used for testing.
         */
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
